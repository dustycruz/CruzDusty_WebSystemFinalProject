using CruzDusty_WebSystemFinalProject.Api.Exceptions;
using CruzDusty_WebSystemFinalProject.Api.Models.DTOs;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CruzDusty_WebSystemFinalProject.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private const int SqlUniqueIndexViolation = 2601;
    private const int SqlUniqueConstraintViolation = 2627;
    private const int SqlConstraintConflict = 547;

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ApiException ex)
        {
            await WriteErrorAsync(context, ex.StatusCode, ex.Message);
        }
        catch (DbUpdateException ex)
        {
            var sqlException = FindSqlException(ex);

            if (sqlException is not null &&
                (sqlException.Number == SqlUniqueIndexViolation ||
                 sqlException.Number == SqlUniqueConstraintViolation))
            {
                _logger.LogWarning("Unique constraint violation on {Path}.", context.Request.Path);

                await WriteErrorAsync(
                    context,
                    StatusCodes.Status409Conflict,
                    "A record with the same unique values already exists.");
            }
            else if (sqlException is not null && sqlException.Number == SqlConstraintConflict)
            {
                _logger.LogWarning("Constraint conflict on {Path}.", context.Request.Path);

                await WriteErrorAsync(
                    context,
                    StatusCodes.Status409Conflict,
                    "The change conflicts with related records or a database rule.");
            }
            else
            {
                _logger.LogError(ex, "Database update failed on {Path}.", context.Request.Path);

                await WriteErrorAsync(
                    context,
                    StatusCodes.Status500InternalServerError,
                    "An unexpected error occurred. Please try again later.");
            }
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The browser closed the connection; there is nobody to answer.
        }
        catch (Exception ex)
        {
            // Full details go to the server log only. The client never sees a stack trace.
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}.", context.Request.Method, context.Request.Path);

            await WriteErrorAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred. Please try again later.");
        }
    }

    private static SqlException? FindSqlException(Exception exception)
    {
        Exception? current = exception;

        while (current is not null)
        {
            if (current is SqlException sqlException)
            {
                return sqlException;
            }

            current = current.InnerException;
        }

        return null;
    }

    private static async Task WriteErrorAsync(HttpContext context, int statusCode, string message)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsJsonAsync(new ErrorResponseDto
        {
            Message = message,
            StatusCode = statusCode
        });
    }
}