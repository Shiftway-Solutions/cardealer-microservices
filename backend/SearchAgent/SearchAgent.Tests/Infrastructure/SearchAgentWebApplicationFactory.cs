using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace SearchAgent.Tests.Infrastructure;

public class SearchAgentWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Use in-memory database for tests
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=searchagent_test;",
                ["ConnectionStrings:RedisConnection"] = "",
                // Claude test config (won't actually call API)
                ["Claude:ApiKey"] = "test-api-key",
                ["Claude:Model"] = "claude-haiku-4-5-20251001",
                ["Claude:MaxTokens"] = "1024",
                ["Claude:Temperature"] = "0.2",
                // JWT config — keys must match MicroserviceSecretsConfiguration.GetJwtConfig() which reads Jwt:Key
                ["Jwt:Key"] = "test-secret-key-for-unit-testing-only-must-be-long-enough",
                ["Jwt:Issuer"] = "okla-api",
                ["Jwt:Audience"] = "okla-clients",
                // Disable external services
                ["RabbitMQ:Enabled"] = "false",
            });
        });

        builder.UseEnvironment("Testing");
    }
}
