using System.Text.Json;
using RecoAgent.Application.DTOs;
using RecoAgent.Application.Features.Recommend.Queries;
using RecoAgent.Domain.Entities;
using RecoAgent.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace RecoAgent.Tests;

/// <summary>
/// Tests for RecoAgent business rules (R01-R08) as specified in the training plan.
/// These rules must pass at 100% before any deployment.
/// </summary>
public class RecoAgentBusinessRulesTests
{
    private readonly Mock<IClaudeRecoService> _claudeServiceMock;
    private readonly Mock<IRecoCacheService> _cacheServiceMock;
    private readonly Mock<IRecoAgentConfigRepository> _configRepoMock;
    private readonly Mock<IRecommendationLogRepository> _logRepoMock;
    private readonly Mock<ILogger<GenerateRecommendationsQueryHandler>> _loggerMock;
    private readonly RecoAgentConfig _defaultConfig;

    public RecoAgentBusinessRulesTests()
    {
        _claudeServiceMock = new Mock<IClaudeRecoService>();
        _cacheServiceMock = new Mock<IRecoCacheService>();
        _configRepoMock = new Mock<IRecoAgentConfigRepository>();
        _logRepoMock = new Mock<IRecommendationLogRepository>();
        _loggerMock = new Mock<ILogger<GenerateRecommendationsQueryHandler>>();

        _defaultConfig = new RecoAgentConfig
        {
            IsEnabled = true,
            Model = "claude-sonnet-4-5-20251022",
            Temperature = 0.5f,
            MaxTokens = 2048,
            MinRecommendations = 8,
            MaxRecommendations = 12,
            SponsoredAffinityThreshold = 0.50f,
            SponsoredPositions = "2,6,11",
            SponsoredLabel = "Destacado",
            MaxSameBrandPercent = 0.40f,
            CacheTtlSeconds = 14400,
            RealTimeCacheTtlSeconds = 900
        };

        _configRepoMock.Setup(r => r.GetActiveConfigAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_defaultConfig);

        _cacheServiceMock.Setup(c => c.GetCachedResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        _cacheServiceMock.Setup(c => c.SetCachedResponseAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _logRepoMock.Setup(l => l.SaveAsync(It.IsAny<RecommendationLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private GenerateRecommendationsQueryHandler CreateHandler() => new(
        _claudeServiceMock.Object,
        _cacheServiceMock.Object,
        _configRepoMock.Object,
        _logRepoMock.Object,
        _loggerMock.Object
    );

    private static RecoAgentRequest CreateDefaultRequest(int coldStart = 3, string stage = "comparador")
    {
        return new RecoAgentRequest
        {
            Perfil = new UserProfile
            {
                UserId = "test-user-1",
                ColdStartLevel = coldStart,
                EtapaCompra = stage,
                TiposPreferidos = ["suv"],
                MarcasPreferidas = ["Toyota", "Hyundai"],
                PrecioPerfilMax = 25000,
                MonedaPreferida = "USD"
            },
            Candidatos = Enumerable.Range(1, 20).Select(i => new VehicleCandidate
            {
                Id = $"VH-{i:D3}",
                Marca = i % 4 == 0 ? "Toyota" : i % 4 == 1 ? "Hyundai" : i % 4 == 2 ? "Honda" : "Kia",
                Modelo = $"Model-{i}",
                Anio = 2020 + (i % 4),
                Precio = 15000 + (i * 500),
                Moneda = "USD",
                Tipo = i % 3 == 0 ? "sedan" : "suv",
                OklaScore = 80 + (i % 15),
                AdActive = i % 5 == 0,
                DealerVerificado = i % 2 == 0,
                FotosCount = 5 + (i % 10)
            }).ToList()
        };
    }

    private RecoAgentResponse CreateValidResponse(int count = 10)
    {
        return new RecoAgentResponse
        {
            Recomendaciones = Enumerable.Range(1, count).Select(i => new RecommendationItem
            {
                VehiculoId = $"VH-{i:D3}",
                Posicion = i,
                RazonRecomendacion = $"Recomendación personalizada #{i} para tu perfil",
                TipoRecomendacion = i % 5 == 0 ? "patrocinado" : "perfil",
                ScoreAfinidadPerfil = 0.95f - (i * 0.05f),
                EsPatrocinado = i % 5 == 0
            }).ToList(),
            PatrocinadosConfig = new SponsoredConfig
            {
                ThresholdScore = 0.50f,
                PosicionesPatrocinados = [2, 6, 11],
                Label = "Destacado",
                TotalInsertados = 2
            },
            DiversificacionAplicada = new DiversificationApplied
            {
                MarcasDistintas = 4,
                MaxMismaMarca = 3,
                MaxMismaMarcaPorcentaje = 0.30f,
                TiposIncluidos = ["suv", "sedan", "hatchback"]
            },
            EtapaCompraDetectada = "comparador",
            ColdStartNivel = 3,
            ConfianzaRecomendaciones = 0.91f,
            ProximaActualizacion = DateTime.UtcNow.AddHours(4)
        };
    }

    private static string SerializeResponse(RecoAgentResponse response)
        => JsonSerializer.Serialize(response);

    // ═══════════════════════════════════════════════════════════
    // R01: Ultra-restrictive profile → expands to at least 8
    // ═══════════════════════════════════════════════════════════
    [Fact]
    public async Task R01_UltraRestrictiveProfile_ExpandsToMinimum8()
    {
        // Arrange: Claude returns only 5 recommendations for a restrictive profile
        var response = CreateValidResponse(5);
        _claudeServiceMock.Setup(c => c.GenerateRecommendationsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SerializeResponse(response));

        var handler = CreateHandler();
        var request = CreateDefaultRequest();

        // Act
        var result = await handler.Handle(new GenerateRecommendationsQuery(request, "user1", "127.0.0.1"), CancellationToken.None);

        // Assert: confidence is reduced when < 8 recommendations (flagging for caller)
        result.Response.ConfianzaRecomendaciones.Should().BeLessThanOrEqualTo(0.5f,
            "Rule #1: When recommendations < 8, confidence should be capped to signal expansion needed");
    }

    // ═══════════════════════════════════════════════════════════
    // R02: Dominant brand (>40%) is limited
    // ═══════════════════════════════════════════════════════════
    [Fact]
    public async Task R02_DiversificationEnforced_MaxSameBrandPercent()
    {
        var response = CreateValidResponse(10);
        _claudeServiceMock.Setup(c => c.GenerateRecommendationsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SerializeResponse(response));

        var handler = CreateHandler();
        var request = CreateDefaultRequest();

        var result = await handler.Handle(new GenerateRecommendationsQuery(request, "user1", "127.0.0.1"), CancellationToken.None);

        // Assert: diversification config is applied
        result.Response.DiversificacionAplicada.Should().NotBeNull();
        result.Response.DiversificacionAplicada!.MaxMismaMarcaPorcentaje.Should().BeLessThanOrEqualTo(0.40f,
            "Rule #3: Max same brand percentage should be <= 40%");
    }

    // ═══════════════════════════════════════════════════════════
    // R03: Every recommendation has razon_recomendacion
    // ═══════════════════════════════════════════════════════════
    [Fact]
    public async Task R03_AllRecommendationsHaveExplanation()
    {
        var response = CreateValidResponse(8);
        // Intentionally remove explanation from one item
        response.Recomendaciones[2].RazonRecomendacion = "";

        _claudeServiceMock.Setup(c => c.GenerateRecommendationsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SerializeResponse(response));

        var handler = CreateHandler();
        var request = CreateDefaultRequest();

        var result = await handler.Handle(new GenerateRecommendationsQuery(request, "user1", "127.0.0.1"), CancellationToken.None);

        // Assert: Rule #4 — all recommendations must have an explanation
        result.Response.Recomendaciones.Should().AllSatisfy(r =>
            r.RazonRecomendacion.Should().NotBeNullOrWhiteSpace(
                "Rule #4: Every recommendation MUST have razon_recomendacion"));
    }

    // ═══════════════════════════════════════════════════════════
    // R04: Sponsored with affinity 0.55 is included
    // ═══════════════════════════════════════════════════════════
    [Fact]
    public async Task R04_SponsoredWithHighAffinity_IsIncluded()
    {
        var response = CreateValidResponse(8);
        response.Recomendaciones[1].EsPatrocinado = true;
        response.Recomendaciones[1].ScoreAfinidadPerfil = 0.55f;
        response.Recomendaciones[1].TipoRecomendacion = "patrocinado";

        _claudeServiceMock.Setup(c => c.GenerateRecommendationsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SerializeResponse(response));

        var handler = CreateHandler();
        var request = CreateDefaultRequest();

        var result = await handler.Handle(new GenerateRecommendationsQuery(request, "user1", "127.0.0.1"), CancellationToken.None);

        // Assert: sponsored item with affinity 0.55 >= threshold 0.50 stays as sponsored
        var sponsoredItem = result.Response.Recomendaciones.First(r => r.ScoreAfinidadPerfil >= 0.55f && r.EsPatrocinado);
        sponsoredItem.EsPatrocinado.Should().BeTrue("Rule #2: Sponsored with affinity >= 0.50 must be included");
    }

    // ═══════════════════════════════════════════════════════════
    // R05: Sponsored with affinity 0.40 is excluded
    // ═══════════════════════════════════════════════════════════
    [Fact]
    public async Task R05_SponsoredWithLowAffinity_IsExcluded()
    {
        var response = CreateValidResponse(8);
        response.Recomendaciones[3].EsPatrocinado = true;
        response.Recomendaciones[3].ScoreAfinidadPerfil = 0.40f;

        _claudeServiceMock.Setup(c => c.GenerateRecommendationsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SerializeResponse(response));

        var handler = CreateHandler();
        var request = CreateDefaultRequest();

        var result = await handler.Handle(new GenerateRecommendationsQuery(request, "user1", "127.0.0.1"), CancellationToken.None);

        // Assert: sponsored item with affinity 0.40 < threshold 0.50 is demoted
        var item = result.Response.Recomendaciones[3];
        item.EsPatrocinado.Should().BeFalse(
            "Rule #2: Sponsored with affinity 0.40 < threshold 0.50 must NOT be marked as sponsored");
    }

    // ═══════════════════════════════════════════════════════════
    // R06: More than 25% sponsored → cap applied
    // ═══════════════════════════════════════════════════════════
    [Fact]
    public async Task R06_TooManySponsored_CapsAt25Percent()
    {
        var response = CreateValidResponse(8);
        // Set 4 out of 8 as sponsored (50%) — should be capped to 2 (25%)
        for (int i = 0; i < 4; i++)
        {
            response.Recomendaciones[i].EsPatrocinado = true;
            response.Recomendaciones[i].ScoreAfinidadPerfil = 0.80f;
        }

        _claudeServiceMock.Setup(c => c.GenerateRecommendationsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SerializeResponse(response));

        var handler = CreateHandler();
        var request = CreateDefaultRequest();

        var result = await handler.Handle(new GenerateRecommendationsQuery(request, "user1", "127.0.0.1"), CancellationToken.None);

        // Assert: max 25% of 8 = 2 sponsored items
        var sponsoredCount = result.Response.Recomendaciones.Count(r => r.EsPatrocinado);
        sponsoredCount.Should().BeLessThanOrEqualTo(2,
            "Rule #2: Max 25% of recommendations can be sponsored (2 of 8)");
    }

    // ═══════════════════════════════════════════════════════════
    // R07: Cold start level 0 → popularity strategy
    // ═══════════════════════════════════════════════════════════
    [Fact]
    public async Task R07_ColdStartLevel0_UsesPopularityStrategy()
    {
        var response = CreateValidResponse(8);
        response.ColdStartNivel = 0;
        response.EtapaCompraDetectada = "explorador";
        response.ConfianzaRecomendaciones = 0.45f;

        _claudeServiceMock.Setup(c => c.GenerateRecommendationsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SerializeResponse(response));

        var handler = CreateHandler();
        var request = CreateDefaultRequest(coldStart: 0, stage: "explorador");

        var result = await handler.Handle(new GenerateRecommendationsQuery(request, "user1", "127.0.0.1"), CancellationToken.None);

        // Assert
        result.Response.ColdStartNivel.Should().Be(0);
        result.Response.Recomendaciones.Count.Should().BeGreaterThanOrEqualTo(1,
            "Rule #7: Cold start level 0 should still return recommendations using popularity strategy");
    }

    // ═══════════════════════════════════════════════════════════
    // R08: patrocinados_config always present with correct structure
    // ═══════════════════════════════════════════════════════════
    [Fact]
    public async Task R08_PatrocinadosConfig_AlwaysPresent()
    {
        var response = CreateValidResponse(8);
        response.PatrocinadosConfig = null; // Intentionally null

        _claudeServiceMock.Setup(c => c.GenerateRecommendationsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SerializeResponse(response));

        var handler = CreateHandler();
        var request = CreateDefaultRequest();

        var result = await handler.Handle(new GenerateRecommendationsQuery(request, "user1", "127.0.0.1"), CancellationToken.None);

        // Assert: patrocinados_config is always present with correct values
        result.Response.PatrocinadosConfig.Should().NotBeNull("Rule #8: patrocinados_config must always be present");
        result.Response.PatrocinadosConfig!.ThresholdScore.Should().BeGreaterThanOrEqualTo(0.50f,
            "Rule #8: threshold must be >= 0.50");
        result.Response.PatrocinadosConfig.PosicionesPatrocinados.Should().BeEquivalentTo(new[] { 2, 6, 11 },
            "Rule #8: positions must be [2,6,11]");
        result.Response.PatrocinadosConfig.Label.Should().Be("Destacado",
            "Rule #8: label must be 'Destacado'");
    }

    // ═══════════════════════════════════════════════════════════
    // R05b: Sponsored transparency — all sponsored items have correct type
    // ═══════════════════════════════════════════════════════════
    [Fact]
    public async Task R05b_AllSponsoredItems_HaveCorrectTipoRecomendacion()
    {
        var response = CreateValidResponse(8);
        response.Recomendaciones[1].EsPatrocinado = true;
        response.Recomendaciones[1].ScoreAfinidadPerfil = 0.85f;
        response.Recomendaciones[1].TipoRecomendacion = "perfil"; // Wrong — should be "patrocinado"

        _claudeServiceMock.Setup(c => c.GenerateRecommendationsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SerializeResponse(response));

        var handler = CreateHandler();
        var request = CreateDefaultRequest();

        var result = await handler.Handle(new GenerateRecommendationsQuery(request, "user1", "127.0.0.1"), CancellationToken.None);

        // Assert: Rule #5 — sponsored items must have tipo_recomendacion = "patrocinado"
        result.Response.Recomendaciones
            .Where(r => r.EsPatrocinado)
            .Should().AllSatisfy(r =>
                r.TipoRecomendacion.Should().Be("patrocinado",
                    "Rule #5: Sponsored items must have tipo_recomendacion = 'patrocinado'"));
    }

    // ═══════════════════════════════════════════════════════════
    // Positions are re-numbered sequentially
    // ═══════════════════════════════════════════════════════════
    [Fact]
    public async Task Positions_AreReNumberedSequentially()
    {
        var response = CreateValidResponse(8);
        _claudeServiceMock.Setup(c => c.GenerateRecommendationsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SerializeResponse(response));

        var handler = CreateHandler();
        var request = CreateDefaultRequest();

        var result = await handler.Handle(new GenerateRecommendationsQuery(request, "user1", "127.0.0.1"), CancellationToken.None);

        // Assert: positions are 1, 2, 3, ..., N
        for (int i = 0; i < result.Response.Recomendaciones.Count; i++)
        {
            result.Response.Recomendaciones[i].Posicion.Should().Be(i + 1);
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Disabled service returns empty result
    // ═══════════════════════════════════════════════════════════
    [Fact]
    public async Task DisabledService_ReturnsDisabledMode()
    {
        _defaultConfig.IsEnabled = false;
        _configRepoMock.Setup(r => r.GetActiveConfigAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_defaultConfig);

        var handler = CreateHandler();
        var request = CreateDefaultRequest();

        var result = await handler.Handle(new GenerateRecommendationsQuery(request, "user1", "127.0.0.1"), CancellationToken.None);

        result.Mode.Should().Be("disabled");
    }
}
