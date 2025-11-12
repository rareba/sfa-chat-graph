using System.Collections.Concurrent;

namespace SfaChatGraph.Server.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly ConcurrentDictionary<string, Queue<DateTime>> _requests = new();

    private const int MaxRequests = 100;
    private static readonly TimeSpan TimeWindow = TimeSpan.FromMinutes(1);

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientId = GetClientIdentifier(context);
        var now = DateTime.UtcNow;

        _requests.AddOrUpdate(clientId,
            _ => new Queue<DateTime>(new[] { now }),
            (_, queue) =>
            {
                while (queue.Count > 0 && now - queue.Peek() > TimeWindow)
                {
                    queue.Dequeue();
                }

                if (queue.Count >= MaxRequests)
                {
                    _logger.LogWarning("Rate limit exceeded for client: {ClientId}", clientId);
                    context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.Response.Headers["Retry-After"] = "60";
                    await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
                    return queue;
                }

                queue.Enqueue(now);
                return queue;
            });

        if (context.Response.StatusCode != StatusCodes.Status429TooManyRequests)
        {
            await _next(context);
        }
    }

    private string GetClientIdentifier(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

public static class RateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RateLimitingMiddleware>();
    }
}
