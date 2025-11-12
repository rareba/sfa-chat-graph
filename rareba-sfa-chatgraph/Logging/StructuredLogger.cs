using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SfaChatGraph.Server.Logging;

public class StructuredLogger
{
    private readonly ILogger _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public StructuredLogger(ILogger logger, IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public void LogRequest(string method, string path, int statusCode, TimeSpan duration)
    {
        var context = _httpContextAccessor.HttpContext;
        var logEntry = new
        {
            Timestamp = DateTime.UtcNow,
            CorrelationId = context?.TraceIdentifier,
            Method = method,
            Path = path,
            StatusCode = statusCode,
            DurationMs = duration.TotalMilliseconds,
            UserId = context?.User?.Identity?.Name
        };

        _logger.LogInformation("HTTP Request {LogEntry}", JsonSerializer.Serialize(logEntry));
    }

    public void LogChatInteraction(Guid chatId, string action, string details = "")
    {
        var context = _httpContextAccessor.HttpContext;
        var logEntry = new
        {
            Timestamp = DateTime.UtcNow,
            CorrelationId = context?.TraceIdentifier,
            ChatId = chatId,
            Action = action,
            Details = details,
            UserId = context?.User?.Identity?.Name
        };

        _logger.LogInformation("Chat Interaction {LogEntry}", JsonSerializer.Serialize(logEntry));
    }

    public void LogSparqlQuery(string query, int resultCount, TimeSpan duration)
    {
        var context = _httpContextAccessor.HttpContext;
        var logEntry = new
        {
            Timestamp = DateTime.UtcNow,
            CorrelationId = context?.TraceIdentifier,
            QueryLength = query.Length,
            ResultCount = resultCount,
            DurationMs = duration.TotalMilliseconds
        };

        _logger.LogInformation("SPARQL Query Executed {LogEntry}", JsonSerializer.Serialize(logEntry));
    }

    public void LogSecurityEvent(string eventType, string details)
    {
        var context = _httpContextAccessor.HttpContext;
        var logEntry = new
        {
            Timestamp = DateTime.UtcNow,
            CorrelationId = context?.TraceIdentifier,
            EventType = eventType,
            Details = details,
            IpAddress = context?.Connection?.RemoteIpAddress?.ToString()
        };

        _logger.LogWarning("Security Event {LogEntry}", JsonSerializer.Serialize(logEntry));
    }
}

public class StructuredLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<StructuredLoggingMiddleware> _logger;

    public StructuredLoggingMiddleware(RequestDelegate next, ILogger<StructuredLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        await _next(context);

        stopwatch.Stop();

        var structuredLogger = new StructuredLogger(_logger, context.RequestServices.GetRequiredService<IHttpContextAccessor>());
        structuredLogger.LogRequest(context.Request.Method, context.Request.Path, context.Response.StatusCode, stopwatch.Elapsed);
    }
}
