namespace CruzDusty_WebSystemFinalProject.Api.Exceptions;

public class ApiException : Exception
{
    public int StatusCode { get; }

    public ApiException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }

    public static ApiException BadRequest(string message)
    {
        return new ApiException(StatusCodes.Status400BadRequest, message);
    }

    public static ApiException Unauthorized(string message)
    {
        return new ApiException(StatusCodes.Status401Unauthorized, message);
    }

    public static ApiException Forbidden(string message)
    {
        return new ApiException(StatusCodes.Status403Forbidden, message);
    }

    public static ApiException NotFound(string message)
    {
        return new ApiException(StatusCodes.Status404NotFound, message);
    }

    public static ApiException Conflict(string message)
    {
        return new ApiException(StatusCodes.Status409Conflict, message);
    }
}