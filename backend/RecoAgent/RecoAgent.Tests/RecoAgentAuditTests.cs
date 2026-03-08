using System.Text.Json;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using RecoAgent.Application.Features.Recommend.Queries;
using RecoAgent.Application.DTOs;
using RecoAgent.Domain.Entities;
using RecoAgent.Domain.Interfaces;

namespace RecoAgent.Tests;

/// <summary>
/// Comprehensive audit tests for RecoAgent: personalization, diversification,
/// Dominican Spanish quality, transient errors, and business rules.
/// </summary>
public class RecoAgentAuditTests
{
    private readonly Mock<IClaudeRecoService> _claudeServiceMock;
    private readonly Mock<IRecoCacheService> _cacheServiceMock;
    private readonly Mock<IRecoAgentConfigRepository> _configRepoMock;
    private readonly Mock<IRecommendationLogRepository> _logRepoMock;
    private readonly Mock<ILogger<GenerateRecommendationsQueryHandler>> _loggerMock;
    private readonly RecoAgentConfig _defaultConfig;

    public RecoAgentAuditTests()
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
            MaxTokens = 1536,
            MinRecommendations = 8,
            MaxRecommendations = 12,
            SponsoredAffinityThreshold = 0.50f,
            SponsoredPositions = "2,6,11",
            SponsoredLabel = "Destacado",
            MaxSameBrandPercent = 0.40f,
            MaxSamePriceRangePercent = 0.50f,
            MaxSameTypePercent = 0.60f,
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

    private static RecoAgentRequest CreateDefaultRequest(int candidateCount = 20) => new()
    {
        Perfil = new UserProfile
        {
            UserId = "test-user-1",
            ColdStartLevel = 3,
            EtapaCompra = "comparador",
            TiposPreferidos = ["suv"],
            MarcasPreferidas = ["Toyota", "Hyundai"],
            PrecioPerfilMax = 25000,
            MonedaPreferida = "USD"
        },
        Candidatos = Enumerable.Range(1, candidateCount).Select(i => new VehicleCandidate
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
            FotosCount = 5 + (i % 10),
            Ubicacion = i % 3 == 0 ? "Santiago" : i % 3 == 1 ? "Santo Domingo" : "La Altagracia"
        }).ToList()
    };

    private static RecoAgentResponse CreateResponse(int count, string? allSameBrand = null)
    {
        return new RecoAgentResponse
        {
            Recomendaciones = Enumerable.Range(1, count).Select(i => new RecommendationItem
            {
                VehiculoId = $"VH-{i:D3}",
                Posicion = i,
                RazonRecomendacion = $"Recomendación #{i} para tu perfil",
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
                TiposIncluidos = ["suv", "sedan"]
            },
            EtapaCompraDetectada = "comparador",
            ColdStartNivel = 3,
            ConfianzaRecomendaciones = 0.91f,
            ProximaActualizacion = DateTime.UtcNow.AddHours(4)
        };
    }

    private static string Serialize(RecoAgentResponse r) => JsonSerializer.Serialize(r);

    // ══════════════════════════════════════════════════════════════
    // 1. BRAND DIVERSIFICATION — Actually Enforced
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Diversification_WhenAllSameBrand_ReplacesExcessWithOtherBrands()
    {
        // Arrange: Claude returns 8 recommendations ALL pointing to Toyota (VH-004, VH-008, etc.)
        // but only 3 should be Toyota (40% of 8)
        var request = CreateDefaultRequest();
        var response = CreateResponse(8);

        // Point all 8 to Toyota vehicles (VH-004, VH-008, VH-012, VH-016, VH-020 are Toyota in our setup)
        // Make 6 of them Toyota by changing IDs
        response.Recomendaciones[0].VehiculoId = "VH-004"; // Toyota
        response.Recomendaciones[1].VehiculoId = "VH-008"; // Toyota
        response.Recomendaciones[2].VehiculoId = "VH-012"; // Toyota
        response.Recomendaciones[3].VehiculoId = "VH-016"; // Toyota
        response.Recomendaciones[4].VehiculoId = "VH-020"; // Toyota
        response.Recomendaciones[5].VehiculoId = "VH-001"; // Hyundai
        response.Recomendaciones[6].VehiculoId = "VH-002"; // Honda
        response.Recomendaciones[7].VehiculoId = "VH-003"; // Kia

        _claudeServiceMock.Setup(c => c.GenerateRecommendationsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Serialize(response));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(
            new GenerateRecommendationsQuery(request, "user1", "127.0.0.1"),
            CancellationToken.None);

        // Assert: No brand should exceed 40% (max 4 out of 8 = ceil(8*0.4) = 4)
        var brands = result.Response.Recomendaciones
            .Select(r => request.Candidatos.FirstOrDefault(c =>
                c.Id.Equals(r.VehiculoId, StringComparison.OrdinalIgnoreCase))?.Marca)
            .Where(m => m != null)
            .GroupBy(m => m!)
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var kv in brands)
        {
            kv.Value.Should().BeLessThanOrEqualTo(4,
                $"Brand {kv.Key} has {kv.Value} recommendations — max 40% (4 of 8) allowed");
        }
    }

    [Fact]
    public async Task Diversification_SponsoredItemsNeverReplaced()
    {
        var request = CreateDefaultRequest();
        var response = CreateResponse(8);

        // 5 Toyotas, but one of them is sponsored with high affinity
        response.Recomendaciones[0].VehiculoId = "VH-004"; // Toyota
        response.Recomendaciones[0].EsPatrocinado = true;
        response.Recomendaciones[0].ScoreAfinidadPerfil = 0.90f;
        response.Recomendaciones[1].VehiculoId = "VH-008"; // Toyota
        response.Recomendaciones[2].VehiculoId = "VH-012"; // Toyota
        response.Recomendaciones[3].VehiculoId = "VH-016"; // Toyota
        response.Recomendaciones[4].VehiculoId = "VH-020"; // Toyota
        response.Recomendaciones[5].VehiculoId = "VH-001"; // Hyundai
        response.Recomendaciones[6].VehiculoId = "VH-002"; // Honda
        response.Recomendaciones[7].VehiculoId = "VH-003"; // Kia

        _claudeServiceMock.Setup(c => c.GenerateRecommendationsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Serialize(response));

        var handler = CreateHandler();
        var result = await handler.Handle(
            new GenerateRecommendationsQuery(request, "user1", "127.0.0.1"),
            CancellationToken.None);

        // The sponsored Toyota should still be in the list
        result.Response.Recomendaciones
            .Should().Contain(r => r.VehiculoId == "VH-004" && r.EsPatrocinado,
                "Sponsored items must NEVER be replaced during diversification");
    }

    [Fact]
    public async Task Diversification_ReplacedItemsMarkedAsDescubrimiento()
    {
        var request = CreateDefaultRequest();
        var response = CreateResponse(8);

        // 6 Toyotas → will need to replace at least 2
        response.Recomendaciones[0].VehiculoId = "VH-004"; // Toyota
        response.Recomendaciones[1].VehiculoId = "VH-008"; // Toyota
        response.Recomendaciones[2].VehiculoId = "VH-012"; // Toyota
        response.Recomendaciones[3].VehiculoId = "VH-016"; // Toyota
        response.Recomendaciones[4].VehiculoId = "VH-020"; // Toyota
        // VH-005 is Hyundai (5%4=1)
        response.Recomendaciones[5].VehiculoId = "VH-005"; // Hyundai
        response.Recomendaciones[6].VehiculoId = "VH-001"; // Hyundai
        response.Recomendaciones[7].VehiculoId = "VH-003"; // Kia

        _claudeServiceMock.Setup(c => c.GenerateRecommendationsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Serialize(response));

        var handler = CreateHandler();
        var result = await handler.Handle(
            new GenerateRecommendationsQuery(request, "user1", "127.0.0.1"),
            CancellationToken.None);

        // At least one item should be marked as "descubrimiento" (a replacement)
        result.Response.Recomendaciones
            .Should().Contain(r => r.TipoRecomendacion == "descubrimiento",
                "Replacement items during diversification should be marked as 'descubrimiento'");
    }

    // ══════════════════════════════════════════════════════════════
    // 2. DOMINICAN SPANISH QUALITY
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task DominicanSpanish_EmptyExplanations_GetDominicanFallbacks()
    {
        var request = CreateDefaultRequest();
        var response = CreateResponse(8);
        // Clear all explanations
        foreach (var reco in response.Recomendaciones)
            reco.RazonRecomendacion = "";

        _claudeServiceMock.Setup(c => c.GenerateRecommendationsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Serialize(response));

        var handler = CreateHandler();
        var result = await handler.Handle(
            new GenerateRecommendationsQuery(request, "user1", "127.0.0.1"),
            CancellationToken.None);

        result.Response.Recomendaciones.Should().AllSatisfy(r =>
        {
            r.RazonRecomendacion.Should().NotBeNullOrWhiteSpace();
            // Should NOT be the old generic fallback
            r.RazonRecomendacion.Should().NotBe("Vehículo recomendado según tu perfil de navegación en OKLA",
                "Fallback explanations should use Dominican Spanish, not generic text");
        });
    }

    [Fact]
    public async Task DominicanSpanish_FallbacksAreVaried()
    {
        var request = CreateDefaultRequest();
        var response = CreateResponse(8);
        foreach (var reco in response.Recomendaciones)
            reco.RazonRecomendacion = "";

        _claudeServiceMock.Setup(c => c.GenerateRecommendationsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Serialize(response));

        var handler = CreateHandler();
        var result = await handler.Handle(
            new GenerateRecommendationsQuery(request, "user1", "127.0.0.1"),
            CancellationToken.None);

        // At least 3 distinct fallback explanations should be used for 8 items
        var distinctExplanations = result.Response.Recomendaciones
            .Select(r => r.RazonRecomendacion)
            .Distinct()
            .Count();

        distinctExplanations.Should().BeGreaterThanOrEqualTo(3,
            "Fallback explanations should be varied, not all the same text");
    }

    // ══════════════════════════════════════════════════════════════
    // 3. ANTI-HALLUCINATION
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task AntiHallucination_FakeIds_AreRemoved()
    {
        var request = CreateDefaultRequest();
        var response = CreateResponse(10);
        // Add fake IDs that don't exist in candidates
        response.Recomendaciones[8].VehiculoId = "FAKE-001";
        response.Recomendaciones[9].VehiculoId = "FAKE-002";

        _claudeServiceMock.Setup(c => c.GenerateRecommendationsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Serialize(response));

        var handler = CreateHandler();
        var result = await handler.Handle(
            new GenerateRecommendationsQuery(request, "user1", "127.0.0.1"),
            CancellationToken.None);

        result.Response.Recomendaciones.Should().NotContain(r => r.VehiculoId == "FAKE-001");
        result.Response.Recomendaciones.Should().NotContain(r => r.VehiculoId == "FAKE-002");
        result.Response.Recomendaciones.Count.Should().Be(8, "Only 8 valid IDs should remain");
    }

    [Fact]
    public async Task AntiHallucination_WhenFakeIdsRemoved_ConfidenceIsReduced()
    {
        var request = CreateDefaultRequest();
        var response = CreateResponse(10);
        response.ConfianzaRecomendaciones = 0.90f;
        response.Recomendaciones[8].VehiculoId = "FAKE-001";
        response.Recomendaciones[9].VehiculoId = "FAKE-002";

        _claudeServiceMock.Setup(c => c.GenerateRecommendationsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Serialize(response));

        var handler = CreateHandler();
        var result = await handler.Handle(
            new GenerateRecommendationsQuery(request, "user1", "127.0.0.1"),
            CancellationToken.None);

        result.Response.ConfianzaRecomendaciones.Should().BeLessThan(0.90f,
            "Confidence should be reduced when hallucinated IDs are removed");
    }

    // ══════════════════════════════════════════════════════════════
    // 4. SPONSORED RULES
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Sponsored_LabelAlwaysDestacado()
    {
        var request = CreateDefaultRequest();
        var response = CreateResponse(8);

        _claudeServiceMock.Setup(c => c.GenerateRecommendationsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Serialize(response));

        var handler = CreateHandler();
        var result = await handler.Handle(
            new GenerateRecommendationsQuery(request, "user1", "127.0.0.1"),
            CancellationToken.None);

        result.Response.PatrocinadosConfig.Should().NotBeNull();
        result.Response.PatrocinadosConfig!.Label.Should().Be("Destacado");
    }

    [Fact]
    public async Task Sponsored_MaxCappedAt25Percent()
    {
        var request = CreateDefaultRequest();
        var response = CreateResponse(8);
        // Make 5 out of 8 sponsored (>25%)
        for (int i = 0; i < 5; i++)
        {
            response.Recomendaciones[i].EsPatrocinado = true;
            response.Recomendaciones[i].ScoreAfinidadPerfil = 0.80f;
        }

        _claudeServiceMock.Setup(c => c.GenerateRecommendationsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Serialize(response));

        var handler = CreateHandler();
        var result = await handler.Handle(
            new GenerateRecommendationsQuery(request, "user1", "127.0.0.1"),
            CancellationToken.None);

        var sponsoredCount = result.Response.Recomendaciones.Count(r => r.EsPatrocinado);
        sponsoredCount.Should().BeLessThanOrEqualTo(2,
            "Max 25% of 8 recommendations can be sponsored = 2");
    }

    // ══════════════════════════════════════════════════════════════
    // 5. POSITIONS ARE SEQUENTIAL
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Positions_AlwaysSequential()
    {
        var request = CreateDefaultRequest();
        var response = CreateResponse(8);

        _claudeServiceMock.Setup(c => c.GenerateRecommendationsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Serialize(response));

        var handler = CreateHandler();
        var result = await handler.Handle(
            new GenerateRecommendationsQuery(request, "user1", "127.0.0.1"),
            CancellationToken.None);

        for (int i = 0; i < result.Response.Recomendaciones.Count; i++)
        {
            result.Response.Recomendaciones[i].Posicion.Should().Be(i + 1,
                $"Position should be sequential (expected {i + 1})");
        }
    }

    // ══════════════════════════════════════════════════════════════
    // 6. CACHE BEHAVIOR
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Cache_Hit_SkipsClaude()
    {
        var request = CreateDefaultRequest();
        var cachedResponse = CreateResponse(8);
        var cachedJson = Serialize(cachedResponse);

        _cacheServiceMock
            .Setup(c => c.GetCachedResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedJson);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new GenerateRecommendationsQuery(request, "user1", "127.0.0.1"),
            CancellationToken.None);

        result.WasCached.Should().BeTrue();
        _claudeServiceMock.Verify(
            s => s.GenerateRecommendationsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RealTimeMode_UsesShortCacheTTL()
    {
        var request = CreateDefaultRequest();
        request.InstruccionesAdicionales = "Quiero algo deportivo";

        var response = CreateResponse(8);
        _claudeServiceMock.Setup(c => c.GenerateRecommendationsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Serialize(response));

        var handler = CreateHandler();
        var result = await handler.Handle(
            new GenerateRecommendationsQuery(request, "user1", "127.0.0.1"),
            CancellationToken.None);

        result.Mode.Should().Be("real-time");
        _cacheServiceMock.Verify(
            c => c.SetCachedResponseAsync(It.IsAny<string>(), It.IsAny<string>(),
                _defaultConfig.RealTimeCacheTtlSeconds, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ══════════════════════════════════════════════════════════════
    // 7. DISABLED SERVICE
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task DisabledService_ReturnsImmediately()
    {
        _defaultConfig.IsEnabled = false;
        _configRepoMock.Setup(r => r.GetActiveConfigAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_defaultConfig);

        var handler = CreateHandler();
        var request = CreateDefaultRequest();

        var result = await handler.Handle(
            new GenerateRecommendationsQuery(request, "user1", "127.0.0.1"),
            CancellationToken.None);

        result.Mode.Should().Be("disabled");
        _claudeServiceMock.Verify(
            s => s.GenerateRecommendationsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ══════════════════════════════════════════════════════════════
    // 8. DIVERSIFICATION METRICS POPULATED
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Diversification_MetricsArePopulatedCorrectly()
    {
        var request = CreateDefaultRequest();
        var response = CreateResponse(8);

        _claudeServiceMock.Setup(c => c.GenerateRecommendationsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Serialize(response));

        var handler = CreateHandler();
        var result = await handler.Handle(
            new GenerateRecommendationsQuery(request, "user1", "127.0.0.1"),
            CancellationToken.None);

        result.Response.DiversificacionAplicada.Should().NotBeNull();
        result.Response.DiversificacionAplicada!.MarcasDistintas.Should().BeGreaterThan(0);
        result.Response.DiversificacionAplicada.TiposIncluidos.Should().NotBeEmpty();
        result.Response.DiversificacionAplicada.MaxMismaMarcaPorcentaje.Should().BeLessThanOrEqualTo(0.40f);
    }
}
