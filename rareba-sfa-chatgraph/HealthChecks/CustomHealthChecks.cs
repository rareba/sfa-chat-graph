using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver;
using StackExchange.Redis;

namespace SfaChatGraph.Server.HealthChecks;

public class MongoDbHealthCheck : IHealthCheck
{
    private readonly IMongoClient _mongoClient;
    private readonly ILogger<MongoDbHealthCheck> _logger;

    public MongoDbHealthCheck(IMongoClient mongoClient, ILogger<MongoDbHealthCheck> logger)
    {
        _mongoClient = mongoClient;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await _mongoClient.ListDatabaseNamesAsync(cancellationToken);
            return HealthCheckResult.Healthy("MongoDB is responding");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MongoDB health check failed");
            return HealthCheckResult.Unhealthy("MongoDB is not responding", ex);
        }
    }
}

public class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisHealthCheck> _logger;

    public RedisHealthCheck(IConnectionMultiplexer redis, ILogger<RedisHealthCheck> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var database = _redis.GetDatabase();
            await database.PingAsync();
            return HealthCheckResult.Healthy("Redis is responding");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check failed");
            return HealthCheckResult.Unhealthy("Redis is not responding", ex);
        }
    }
}

public class SparqlEndpointHealthCheck : IHealthCheck
{
    private readonly ISparqlEndpoint _endpoint;
    private readonly ILogger<SparqlEndpointHealthCheck> _logger;

    public SparqlEndpointHealthCheck(ISparqlEndpoint endpoint, ILogger<SparqlEndpointHealthCheck> logger)
    {
        _endpoint = endpoint;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = "SELECT (COUNT(*) as ?count) WHERE { ?s ?p ?o } LIMIT 1";
            await _endpoint.QueryAsync(query, cancellationToken);
            return HealthCheckResult.Healthy("SPARQL endpoint is responding");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SPARQL endpoint health check failed");
            return HealthCheckResult.Unhealthy("SPARQL endpoint is not responding", ex);
        }
    }
}

public class JupyterHealthCheck : IHealthCheck
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<JupyterHealthCheck> _logger;

    public JupyterHealthCheck(IHttpClientFactory httpClientFactory, ILogger<JupyterHealthCheck> logger)
    {
        _httpClient = httpClientFactory.CreateClient("jupyter");
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("Jupyter is responding");
            }
            return HealthCheckResult.Degraded($"Jupyter returned status code: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Jupyter health check failed");
            return HealthCheckResult.Unhealthy("Jupyter is not responding", ex);
        }
    }
}
