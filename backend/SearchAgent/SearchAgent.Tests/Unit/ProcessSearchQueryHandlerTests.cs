using SearchAgent.Application.Features.Search;
using SearchAgent.Application.DTOs;
using SearchAgent.Domain.Interfaces;
using SearchAgent.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace SearchAgent.Tests.Unit;

public class ProcessSearchQueryHandlerTests
{
    private readonly Mock<IClaudeSearchService> _claudeServiceMock;
    private readonly Mock<ISearchCacheService> _cacheServiceMock;
    private readonly Mock<ISearchAgentConfigRepository> _configRepoMock;
    private readonly Mock<ISearchQueryRepository> _queryRepoMock;
    private readonly Mock<ILogger<ProcessSearchQueryHandler>> _loggerMock;
    private readonly ProcessSearchQueryHandler _handler;

    public ProcessSearchQueryHandlerTests()
    {
        _claudeServiceMock = new Mock<IClaudeSearchService>();
        _cacheServiceMock = new Mock<ISearchCacheService>();
        _configRepoMock = new Mock<ISearchAgentConfigRepository>();
        _queryRepoMock = new Mock<ISearchQueryRepository>();
        _loggerMock = new Mock<ILogger<ProcessSearchQueryHandler>>();

        _handler = new ProcessSearchQueryHandler(
            _claudeServiceMock.Object,
            _cacheServiceMock.Object,
            _configRepoMock.Object,
            _queryRepoMock.Object,
            _loggerMock.Object
        );

        // Default config
        _configRepoMock.Setup(r => r.GetActiveConfigAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchAgentConfig
            {
                Id = Guid.NewGuid(),
                IsEnabled = true,
                ModelName = "claude-haiku-4-5-20251001",
                Temperature = 0.2,
                MaxTokens = 1024,
                MinResults = 8,
                MaxResults = 50,
                EnableAutoRelaxation = true,
                EnableSponsoredResults = true,
                SponsoredMinAffinity = 0.45,
                SponsoredMaxPercentage = 0.25,
                SponsoredPositions = "1,5,10",
                CacheEnabled = true,
                CacheTtlMinutes = 60,
            });
    }

    [Fact]
    public async Task Handle_WhenServiceDisabled_ReturnsDisabledResult()
    {
        // Arrange
        _configRepoMock.Setup(r => r.GetActiveConfigAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchAgentConfig { IsEnabled = false });

        var command = new ProcessSearchQuery("Toyota Corolla 2020");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsAiSearchEnabled.Should().BeFalse();
        _claudeServiceMock.Verify(
            s => s.ProcessSearchQueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SearchAgentConfig>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCacheHit_ReturnsCachedResult()
    {
        // Arrange
        var cachedResponse = new SearchAgentResponse
        {
            FiltrosExactos = new SearchFilters { Marca = "Toyota", Modelo = "Corolla" },
            Confianza = 0.95,
            QueryReformulada = "Toyota Corolla",
        };

        _cacheServiceMock.Setup(c => c.GetCachedResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedResponse);

        var command = new ProcessSearchQuery("Toyota Corolla 2020");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.WasCached.Should().BeTrue();
        result.AiFilters.Should().NotBeNull();
        result.AiFilters.FiltrosExactos!.Marca.Should().Be("Toyota");

        _claudeServiceMock.Verify(
            s => s.ProcessSearchQueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SearchAgentConfig>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNoCacheHit_CallsClaude()
    {
        // Arrange
        _cacheServiceMock.Setup(c => c.GetCachedResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SearchAgentResponse?)null);

        var claudeResponse = new SearchAgentResponse
        {
            FiltrosExactos = new SearchFilters { Marca = "Honda", Modelo = "Civic" },
            Confianza = 0.90,
            QueryReformulada = "Honda Civic",
            NivelFiltrosActivo = 1,
        };

        _claudeServiceMock.Setup(s => s.ProcessSearchQueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SearchAgentConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(claudeResponse);

        var command = new ProcessSearchQuery("Busco un Civic de Honda");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.WasCached.Should().BeFalse();
        result.IsAiSearchEnabled.Should().BeTrue();
        result.AiFilters.FiltrosExactos!.Marca.Should().Be("Honda");

        _claudeServiceMock.Verify(
            s => s.ProcessSearchQueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SearchAgentConfig>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("a")]
    public async Task Validator_RejectsInvalidQueries(string query)
    {
        // Arrange
        var validator = new ProcessSearchQueryValidator();

        // Act
        var result = await validator.ValidateAsync(new ProcessSearchQuery(query));

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validator_AcceptsValidQuery()
    {
        // Arrange
        var validator = new ProcessSearchQueryValidator();

        // Act
        var result = await validator.ValidateAsync(new ProcessSearchQuery("Toyota Corolla 2020 automática"));

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
