using System.Security.Cryptography;
using System.Text;
using CruzDusty_WebSystemFinalProject.Api.Data;
using CruzDusty_WebSystemFinalProject.Api.Middleware;
using CruzDusty_WebSystemFinalProject.Api.Models;
using CruzDusty_WebSystemFinalProject.Api.Models.DTOs;
using CruzDusty_WebSystemFinalProject.Api.Models.Entities;
using CruzDusty_WebSystemFinalProject.Api.Services.Implementations;
using CruzDusty_WebSystemFinalProject.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

const string CorsPolicyName = "WebClient";

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------
// Configuration
// ---------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection is missing from appsettings.json.");
}

// Jwt:Key is a secret, so it is NOT in appsettings.json. Set it with:
//   dotnet user-secrets set "Jwt:Key" "<at least 32 characters>"
// If it is missing, a temporary random key is generated (tokens stop working after a restart).
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
var usingGeneratedJwtKey = false;

if (string.IsNullOrWhiteSpace(jwtSettings.Key))
{
    jwtSettings.Key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
    usingGeneratedJwtKey = true;
}
else if (Encoding.UTF8.GetByteCount(jwtSettings.Key) < 32)
{
    throw new InvalidOperationException("Jwt:Key must be at least 32 characters long.");
}

if (string.IsNullOrWhiteSpace(jwtSettings.Issuer))
{
    jwtSettings.Issuer = "CruzDusty_WebSystemFinalProject.Api";
}

if (string.IsNullOrWhiteSpace(jwtSettings.Audience))
{
    jwtSettings.Audience = "CruzDusty_WebSystemFinalProject.Web";
}

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
if (allowedOrigins is null || allowedOrigins.Length == 0)
{
    allowedOrigins = new[] { "https://localhost:7002" };
}

// ---------------------------------------------------------------------
// Services
// ---------------------------------------------------------------------
builder.Services.Configure<JwtSettings>(options =>
{
    options.Key = jwtSettings.Key;
    options.Issuer = jwtSettings.Issuer;
    options.Audience = jwtSettings.Audience;
    options.ExpiresMinutes = jwtSettings.ExpiresMinutes;
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// ASP.NET Core Identity: hashed passwords, roles, lockout. No cookies: the API uses JWT.
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;

        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.AllowedForNewUsers = true;

        options.User.RequireUniqueEmail = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddSingleton<IGradeCalculator, GradeCalculator>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IGradeService, GradeService>();

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        // Do not echo parser internals (type names, line numbers) back to the client.
        options.AllowInputFormatterExceptionMessages = false;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        // Every validation failure becomes { message, statusCode, errors }.
        options.InvalidModelStateResponseFactory = context =>
        {
            var parameterNames = new HashSet<string>(
                context.ActionDescriptor.Parameters.Select(parameter => parameter.Name),
                StringComparer.OrdinalIgnoreCase);

            var entries = context.ModelState
                .Where(entry => entry.Value is not null && entry.Value.Errors.Count > 0)
                .ToList();

            // When a field failed (for example $.grade), drop the generic
            // "The dto field is required." entry that ASP.NET Core adds next to it.
            var fieldEntries = entries.Where(entry => !parameterNames.Contains(entry.Key)).ToList();
            if (fieldEntries.Count > 0)
            {
                entries = fieldEntries;
            }

            var errors = entries
                .GroupBy(entry => parameterNames.Contains(entry.Key) ? "request" : NormalizeKey(entry.Key))
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .SelectMany(entry => entry.Value!.Errors)
                        .Select(error => FriendlyMessage(group.Key, error.ErrorMessage))
                        .Distinct()
                        .ToArray());

            var firstMessage = errors.Values.SelectMany(messages => messages).FirstOrDefault()
                ?? "The request is not valid.";

            return new BadRequestObjectResult(new ErrorResponseDto
            {
                Message = firstMessage,
                StatusCode = StatusCodes.Status400BadRequest,
                Errors = errors
            });
        };
    });

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        // Keep claim names exactly as written in the token ("role", "studentId", "facultyId").
        options.MapInboundClaims = false;
        options.SaveToken = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = AppClaimTypes.Username,
            RoleClaimType = AppClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
            // 401: no token, invalid token or expired token.
            OnChallenge = async context =>
            {
                context.HandleResponse();

                if (context.Response.HasStarted)
                {
                    return;
                }

                var expired = context.AuthenticateFailure is SecurityTokenExpiredException;

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new ErrorResponseDto
                {
                    Message = expired
                        ? "Your session has expired. Please sign in again."
                        : "You must sign in to access this resource.",
                    StatusCode = StatusCodes.Status401Unauthorized
                });
            },

            // 403: valid token, wrong role.
            OnForbidden = async context =>
            {
                if (context.Response.HasStarted)
                {
                    return;
                }

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new ErrorResponseDto
                {
                    Message = "Access denied. You do not have permission to perform this action.",
                    StatusCode = StatusCodes.Status403Forbidden
                });
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("Location");
    });
});

// ---------------------------------------------------------------------
// Pipeline
// ---------------------------------------------------------------------
var app = builder.Build();

if (usingGeneratedJwtKey)
{
    app.Logger.LogWarning(
        "Jwt:Key is not configured, so a temporary signing key was generated. Tokens stop working " +
        "when the API restarts. Set a key with: dotnet user-secrets set \"Jwt:Key\" \"<32+ characters>\"");
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseCors(CorsPolicyName);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapFallback(async context =>
{
    context.Response.StatusCode = StatusCodes.Status404NotFound;
    await context.Response.WriteAsJsonAsync(new ErrorResponseDto
    {
        Message = "The requested API endpoint does not exist.",
        StatusCode = StatusCodes.Status404NotFound
    });
});

// ---------------------------------------------------------------------
// Apply Identity migrations; create demo accounts in Development only
// ---------------------------------------------------------------------
try
{
    await DbSeeder.InitializeAsync(app.Services, seedDemoAccounts: app.Environment.IsDevelopment());
}
catch (Exception ex)
{
    app.Logger.LogCritical(
        ex,
        "Database initialization failed. Run Database/StudentGradeManagementDB.sql, create the EF migration, " +
        "and check the connection string.");
    throw;
}

app.Run();

// "$.grade" -> "grade", "SubjectCode" -> "subjectCode", "" or "$" -> "request"
static string NormalizeKey(string key)
{
    if (string.IsNullOrWhiteSpace(key) || key == "$")
    {
        return "request";
    }

    var name = key.StartsWith("$.", StringComparison.Ordinal) ? key.Substring(2) : key;

    if (name.Length == 0)
    {
        return "request";
    }

    return char.ToLowerInvariant(name[0]) + name.Substring(1);
}

// Replaces framework-generated parsing messages with plain sentences.
static string FriendlyMessage(string field, string message)
{
    if (field == "request")
    {
        return "The request body is missing or is not valid JSON.";
    }

    var isTechnical =
        string.IsNullOrWhiteSpace(message) ||
        message.Contains("JSON", StringComparison.OrdinalIgnoreCase) ||
        message.Contains("could not be converted", StringComparison.OrdinalIgnoreCase) ||
        message.StartsWith("The input was not valid", StringComparison.OrdinalIgnoreCase);

    if (!isTechnical)
    {
        return message;
    }

    return field switch
    {
        "grade" => "Grade must be a whole number from 0 to 100.",
        "studentId" => "Student must be a valid student id.",
        _ => $"The value for '{field}' is not valid."
    };
}