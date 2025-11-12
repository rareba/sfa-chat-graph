using System.Text.Json;

namespace SfaChatGraph.Server.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");

            var response = context.Response;
            response.ContentType = "application/json";

            var errorResponse = new ErrorResponse
            {
                Message = GetErrorMessage(ex),
                StatusCode = GetStatusCode(ex),
                Timestamp = DateTime.UtcNow
            };

            if (!_environment.IsProduction())
            {
                errorResponse.StackTrace = ex.StackTrace;
                errorResponse.Details = ex.Message;
            }

            response.StatusCode = errorResponse.StatusCode;
            await response.WriteAsJsonAsync(errorResponse, JsonSerializerOptions);
        }
    }

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private string GetErrorMessage(Exception ex)
    {
        return ex switch
        {
            ArgumentException or ArgumentNullException => "Invalid request parameters",
            UnauthorizedAccessException => "Unauthorized access",
            TimeoutException => "Request timed out",
            _ => "An error occurred while processing your request"
        };
    }

    private int GetStatusCode(Exception ex)
    {
        return ex switch
        {
            ArgumentException or ArgumentNullException => StatusCodes.Status400BadRequest,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            TimeoutException => StatusCodes.Status408RequestTimeout,
            NotImplementedException => StatusCodes.Status501NotImplemented,
            _ => StatusCodes.Status500InternalServerError
        };
    }
}

public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public int StatusCode { get; set; }
    public DateTime Timestamp { get; set; }
    public string? StackTrace { get; set; }
    public string? RequestId { get; set; }
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseCustomExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
