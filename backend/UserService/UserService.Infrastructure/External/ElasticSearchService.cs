using UserService.Domain.Entities;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace UserService.Infrastructure.External
{
    public class ElasticSearchSettings
    {
        public string ConnectionString { get; set; } = "http://localhost:9200";
        public string DefaultIndex { get; set; } = "Roles";
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool Enable { get; set; } = false;
    }

    public class ElasticSearchService
    {
        private readonly ElasticsearchClient? _client; // HACER NULLABLE
        private readonly ILogger<ElasticSearchService> _logger;
        private readonly ElasticSearchSettings _settings;

        public ElasticSearchService(IOptions<ElasticSearchSettings> settings, ILogger<ElasticSearchService> logger)
        {
            _settings = settings.Value;
            _logger = logger;

            if (!_settings.Enable)
            {
                _client = null;
                return;
            }

            try
            {
                var config = new ElasticsearchClientSettings(new Uri(_settings.ConnectionString))
                    .DefaultIndex(_settings.DefaultIndex);

                // Configurar autenticación si se proporciona
                if (!string.IsNullOrEmpty(_settings.Username) && !string.IsNullOrEmpty(_settings.Password))
                {
                    config.Authentication(new BasicAuthentication(_settings.Username, _settings.Password));
                }

                _client = new ElasticsearchClient(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Elasticsearch client");
                _client = null;
            }
        }

        public async Task IndexErrorAsync(Role Role)
        {
            if (_client == null || !_settings.Enable)
            {
                _logger.LogDebug("Elasticsearch is disabled, skipping error indexing.");
                return;
            }

            try
            {
                var response = await _client.IndexAsync(Role, _settings.DefaultIndex);

                if (!response.IsValidResponse)
                {
                    _logger.LogWarning("Failed to index error log in Elasticsearch: {DebugInfo}", response.DebugInformation);
                }
                else
                {
                    _logger.LogInformation("Successfully indexed error log {ErrorId} in Elasticsearch", Role.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error indexing error log in Elasticsearch");
            }
        }

        public async Task<IEnumerable<Role>> SearchErrorsAsync(string searchTerm, int page = 1, int pageSize = 50)
        {
            if (_client == null || !_settings.Enable)
            {
                _logger.LogDebug("Elasticsearch is disabled, returning empty search results.");
                return Enumerable.Empty<Role>();
            }

            try
            {
                var response = await _client.SearchAsync<Role>(s => s
                    .Index(_settings.DefaultIndex)
                    .From((page - 1) * pageSize)
                    .Size(pageSize)
                    .Query(q => q
                        .MultiMatch(m => m
                            .Fields(new[] { "message", "exceptionType", "serviceName" })
                            .Query(searchTerm)
                        )
                    )
                );

                if (response.IsValidResponse)
                {
                    return response.Documents;
                }

                _logger.LogWarning("Elasticsearch search failed: {DebugInfo}", response.DebugInformation);
                return Enumerable.Empty<Role>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching error logs in Elasticsearch");
                return Enumerable.Empty<Role>();
            }
        }

        public async Task<bool> IsHealthyAsync()
        {
            if (_client == null || !_settings.Enable)
                return false;

            try
            {
                var response = await _client.PingAsync();
                return response.IsValidResponse;
            }
            catch
            {
                return false;
            }
        }
    }
}
