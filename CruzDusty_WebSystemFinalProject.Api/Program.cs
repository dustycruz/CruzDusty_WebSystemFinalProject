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

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSettings = jwtSection.Get<JwtSettings>();
if (jwtSettings is null || string.IsNullOrWhiteSpace(jwtSettings.Key))
{
    throw new InvalidOperationException("The 'Jwt' section is missing from appsettings.json.");
}

if (Encoding.UTF8.GetByteCount(jwtSettings.Key) < 32)
{
    throw new InvalidOperationException("Jwt:Key must be at least 32 characters long.");
}

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
if (allowedOrigins is null || allowedOrigins.Length == 0)
{
    allowedOrigins = new[] { "https://localhost:7002" };
}

// ---------------------------------------------------------------------
// Services
// ---------------------------------------------------------------------
builder.Services.Configure<JwtSettings>(jwtSection);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddSingleton<IGradeCalculator, GradeCalculator>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<ISubjectService, SubjectService>();
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
        // Replace the default validation response with { message, statusCode, errors }.
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(entry => entry.Value is not null && entry.Value.Errors.Count > 0)
                .GroupBy(entry => NormalizeKey(entry.Key))
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .SelectMany(entry => entry.Value!.Errors)
                        .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                            ? $"The value for '{group.Key}' is not valid."
                            : error.ErrorMessage)
                        .Distinct()
                        .ToArray());

            var firstMessage = errors.Values
                .SelectMany(messages => messages)
                .FirstOrDefault() ?? "The request is not valid.";

            var body = new ErrorResponseDto
            {
                Message = firstMessage,
                StatusCode = StatusCodes.Status400BadRequest,
                Errors = errors
            };

            return new BadRequestObjectResult(body);
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
        // Keep the claim names exactly as they are written in the token ("role", "studentId").
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
            // 401: no token, bad token or expired token.
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
// Seed login accounts and sample grades
// ---------------------------------------------------------------------
try
{
    await DbSeeder.SeedAsync(app.Services);
}
catch (Exception ex)
{
    app.Logger.LogCritical(
        ex,
        "Database seeding failed. Run Database/CruzDusty_WebSystemFinalProject.sql and check the connection string.");
    throw;
}

app.Run();

// "$.prelim" -> "prelim", "StudentNumber" -> "studentNumber", "" -> "request"
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