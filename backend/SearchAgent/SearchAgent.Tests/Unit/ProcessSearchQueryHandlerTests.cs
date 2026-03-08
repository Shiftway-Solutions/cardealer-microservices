using SearchAgent.Application.Features.Search.Queries;
using SearchAgent.Application.Features.Search.Validators;
using SearchAgent.Application.DTOs;
using SearchAgent.Domain.Interfaces;
using SearchAgent.Domain.Models;
using SearchAgent.Domain.Entities;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;

namespace SearchAgent.Tests.Unit;

public class ProcessSearchQueryHandlerTests
{
    private readonly Mock<IClaudeSearchService> _claudeServiceMock;
    private readonly Mock<ISearchCacheService> _cacheServiceMock;
    private readonly Mock<ISearchAgentConfigRepository> _configRepoMock;
    private readonly Mock<ISearchQueryRepository> _queryRepoMock;
    private readonly Mock<ILogger<ProcessSearchQueryHandler>> _loggerMock;
    private readonly Mock<IMemoryCache> _memoryCacheMock;
    private readonly ProcessSearchQueryHandler _handler;

    private static readonly SearchAgentConfig DefaultConfig = new()
    {
        Id = Guid.NewGuid(),
        IsEnabled = true,
        Model = "claude-haiku-4-5-20251001",
        Temperature = 0.2f,
        MaxTokens = 1024,
        MinResultsPerPage = 8,
        MaxResultsPerPage = 50,
        SponsoredAffinityThreshold = 0.45f,
        MaxSponsoredPercentage = 0.25f,
        SponsoredPositions = "1,5,10",
        SponsoredLabel = "Patrocinado",
        EnableCache = true,
        CacheTtlSeconds = 3600,
    };

    public ProcessSearchQueryHandlerTests()
    {
        _claudeServiceMock = new Mock<IClaudeSearchService>();
        _cacheServiceMock = new Mock<ISearchCacheService>();
        _configRepoMock = new Mock<ISearchAgentConfigRepository>();
        _queryRepoMock = new Mock<ISearchQueryRepository>();
        _loggerMock = new Mock<ILogger<ProcessSearchQueryHandler>>();

        _configRepoMock
            .Setup(r => r.GetActiveConfigAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(DefaultConfig);

        _queryRepoMock
            .Setup(r => r.SaveAsync(It.IsAny<SearchQuery>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _memoryCacheMock = new Mock<IMemoryCache>();
        var cacheEntry = Mock.Of<ICacheEntry>();
        _memoryCacheMock.Setup(m => m.CreateEntry(It.IsAny<object>())).Returns(cacheEntry);
        _handler = new ProcessSearchQueryHandler(
            _claudeServiceMock.Object,
            _cacheServiceMock.Object,
            _configRepoMock.Object,
            _queryRepoMock.Object,
            _loggerMock.Object,
            _memoryCacheMock.Object
        );
    }

    private static ProcessSearchQuery MakeQuery(string text)
        => new(text, null, 1, 20, null, null);

    [Fact]
    public async Task Handle_WhenServiceDisabled_ReturnsDisabledResult()
    {
        // Arrange
        _configRepoMock
            .Setup(r => r.GetActiveConfigAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchAgentConfig { IsEnabled = false });

        var query = MakeQuery("Toyota Corolla 2020");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsAiSearchEnabled.Should().BeFalse();
        _claudeServiceMock.Verify(
            s => s.ProcessQueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCacheHit_ReturnsCachedResult()
    {
        // Arrange
        var cachedResponse = new SearchAgentResponse
        {
            FiltrosExactos = new SearchFilters { Marca = "Toyota", Modelo = "Corolla" },
            Confianza = 0.95f,
            QueryReformulada = "Toyota Corolla",
        };
        var cachedJson = JsonSerializer.Serialize(cachedResponse);

        _cacheServiceMock
            .Setup(c => c.GetCachedResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedJson);

        var query = MakeQuery("Toyota Corolla 2020");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.WasCached.Should().BeTrue();
        result.AiFilters.Should().NotBeNull();
        result.AiFilters.FiltrosExactos!.Marca.Should().Be("Toyota");
        _claudeServiceMock.Verify(
            s => s.ProcessQueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNoCacheHit_CallsClaude()
    {
        // Arrange
        _cacheServiceMock
            .Setup(c => c.GetCachedResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var claudeResponse = new SearchAgentResponse
        {
            FiltrosExactos = new SearchFilters { Marca = "Honda", Modelo = "Civic" },
            Confianza = 0.90f,
            QueryReformulada = "Honda Civic",
            NivelFiltrosActivo = 1,
        };

        _claudeServiceMock
            .Setup(s => s.ProcessQueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(claudeResponse);

        var query = MakeQuery("Busco un Civic de Honda");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.WasCached.Should().BeFalse();
        result.IsAiSearchEnabled.Should().BeTrue();
        result.AiFilters.FiltrosExactos!.Marca.Should().Be("Honda");
        _claudeServiceMock.Verify(
            s => s.ProcessQueryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("a")]
    public async Task Validator_RejectsInvalidQueries(string text)
    {
        var validator = new ProcessSearchQueryValidator();
        var result = await validator.ValidateAsync(MakeQuery(text));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validator_AcceptsValidQuery()
    {
        var validator = new ProcessSearchQueryValidator();
        var result = await validator.ValidateAsync(MakeQuery("Toyota Corolla 2020 automática"));
        result.IsValid.Should().BeTrue();
    }
}
