namespace Tevscare.Application.Common;

public class AppException : Exception
{
    public int StatusCode { get; }

    public IDictionary<string, string[]>? Errors { get; }

    public AppException(string message, int statusCode = 400, IDictionary<string, string[]>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }

    public static AppException NotFound(string message) => new(message, 404);

    public static AppException Forbidden(string message = "You do not have access to this resource.") => new(message, 403);

    public static AppException Unauthorized(string message = "Sign in is required.") => new(message, 401);
}
