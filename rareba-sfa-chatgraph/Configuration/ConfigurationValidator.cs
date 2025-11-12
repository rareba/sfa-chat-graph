namespace SfaChatGraph.Server.Configuration;

public static class ConfigurationValidator
{
    public static void Validate(WebApplicationBuilder builder)
    {
        var errors = new List<string>();

        var mongoConnection = builder.Configuration.GetConnectionString("Mongo");
        if (string.IsNullOrEmpty(mongoConnection))
        {
            errors.Add("MongoDB connection string is not configured");
        }

        var sparqlConnection = builder.Configuration.GetConnectionString("Sparql");
        if (string.IsNullOrEmpty(sparqlConnection))
        {
            errors.Add("SPARQL endpoint connection string is not configured");
        }

        var aiConfig = builder.Configuration.GetSection("AiConfig");
        var apiKey = aiConfig["ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            errors.Add("AI API Key is not configured");
        }

        var jupyterOptions = builder.Configuration.GetSection("JupyterOptions");
        var jupyterEndpoint = jupyterOptions["Endpoint"];
        if (string.IsNullOrEmpty(jupyterEndpoint))
        {
            errors.Add("Jupyter endpoint is not configured");
        }

        var skipAuth = builder.Configuration.GetValue<bool>("Auth:SkipAuthentication", false);
        if (!skipAuth)
        {
            var jwtSettings = builder.Configuration.GetSection("Auth:JwtSettings");
            var secretKey = jwtSettings["SecretKey"];
            if (string.IsNullOrEmpty(secretKey))
            {
                errors.Add("JWT SecretKey is not configured");
            }

            var issuer = jwtSettings["Issuer"];
            if (string.IsNullOrEmpty(issuer))
            {
                errors.Add("JWT Issuer is not configured");
            }

            var audience = jwtSettings["Audience"];
            if (string.IsNullOrEmpty(audience))
            {
                errors.Add("JWT Audience is not configured");
            }
        }

        if (errors.Any())
        {
            throw new InvalidOperationException($"Configuration validation failed:\n{string.Join("\n", errors)}");
        }
    }
}
