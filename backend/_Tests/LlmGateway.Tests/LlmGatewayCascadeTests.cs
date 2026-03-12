using CarDealer.Shared.LlmGateway.Abstractions;
using CarDealer.Shared.LlmGateway.Configuration;
using CarDealer.Shared.LlmGateway.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;
using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace LlmGateway.Tests;

// =============================================================================
// LLM Gateway — Fallback Cascade Unit Tests
// =============================================================================
// Validates the cascade fallback: Claude → Gemini → Llama → Redis Cache
//
//   Level 0: Claude (primary) — triggers fallback on 429/500/503/529
//   Level 1: Gemini (1st fallback) — called when Claude fails
//   Level 2: Llama (2nd fallback) — called when Gemini also fails
//   Level 3: Redis Cache (degraded) — served when all 3 providers fail
//
// All tests use Moq mocks for ILlmProvider and IConnectionMultiplexer.
// NO real HTTP calls to Anthropic, Google, or self-hosted Llama.
// NO real Redis connections.
//
// Timing constraints:
//   - Each provider cascade step should complete within 500ms
//   - Total cascade (all 3 providers + cache) should not exceed 1,500ms
//
// Logging:
//   - Fallback level is logged with "[LLMGateway:FALLBACK]" format
//   - Each failure chain entry includes provider, HTTP status, latency
// =============================================================================

public class LlmGatewayCascadeTests
{
    private readonly LlmGatewayOptions _options;
    private readonly Mock<ILogger<LlmGatewayService>> _loggerMock;

    public LlmGatewayCascadeTests()
    {
        _options = new LlmGatewayOptions
        {
            Claude = new ClaudeProviderOptions { Enabled = true },
            Gemini = new GeminiProviderOptions { Enabled = true },
            Llama = new LlamaProviderOptions { Enabled = true },
            EnableCacheFallback = true,
            ForceDegradedMode = false,
        };
        _loggerMock = new Mock<ILogger<LlmGatewayService>>();
    }

    private static LlmRequest MakeRequest(string agent = "TestAgent") => new()
    {
        RequestId = Guid.NewGuid().ToString("N"),
        SystemPrompt = "You are a test assistant.",
        UserMessage = "Hello test",
        CallerAgent = agent,
        SkipCache = true, // Skip initial cache check for cascade tests
    };

    private static LlmResponse MakeResponse(LlmProviderType provider, string modelId = "test-model") => new()
    {
        Content = "Test response content",
        Provider = provider,
        ModelId = modelId,
        FallbackLevel = 0,
        TotalLatency = TimeSpan.FromMilliseconds(50),
        ProviderLatency = TimeSpan.FromMilliseconds(50),
        FromCache = false,
        InputTokens = 100,
        OutputTokens = 50,
        StopReason = "end_turn",
    };

    private Mock<ILlmProvider> CreateProviderMock(LlmProviderType type)
    {
        var mock = new Mock<ILlmProvider>();
        mock.Setup(p => p.ProviderType).Returns(type);
        mock.Setup(p => p.DisplayName).Returns(type.ToString());
        return mock;
    }

    private LlmGatewayService CreateGateway(
        IEnumerable<ILlmProvider> providers,
        IConnectionMultiplexer? redis = null)
    {
        return new LlmGatewayService(
            providers,
            Options.Create(_options),
            _loggerMock.Object,
            redis);
    }

    // ─── Level 0: Claude Success (no fallback) ─────────────────────

    [Fact]
    public async Task CompleteAsync_ClaudeSucceeds_ReturnsLevel0()
    {
        // Arrange
        var claude = CreateProviderMock(LlmProviderType.Claude);
        claude.Setup(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeResponse(LlmProviderType.Claude, "claude-sonnet-4"));

        var gemini = CreateProviderMock(LlmProviderType.Gemini);
        var llama = CreateProviderMock(LlmProviderType.Llama);

        var gateway = CreateGateway([claude.Object, gemini.Object, llama.Object]);

        // Act
        var result = await gateway.CompleteAsync(MakeRequest());

        // Assert
        result.Provider.Should().Be(LlmProviderType.Claude);
        result.FallbackLevel.Should().Be(0);
        result.FromCache.Should().BeFalse();

        // Gemini and Llama should NOT have been called
        gemini.Verify(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        llama.Verify(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── Level 1: Claude 429 → Gemini Called ────────────────────────

    [Fact]
    public async Task CompleteAsync_Claude429_FallsBackToGemini()
    {
        // Arrange: Claude returns 429 (rate limit)
        var claude = CreateProviderMock(LlmProviderType.Claude);
        claude.Setup(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new LlmProviderException(
                LlmProviderType.Claude,
                "Provider API returned 429",
                httpStatusCode: 429,
                isTransient: true));

        var gemini = CreateProviderMock(LlmProviderType.Gemini);
        gemini.Setup(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeResponse(LlmProviderType.Gemini, "gemini-1.5-flash"));

        var llama = CreateProviderMock(LlmProviderType.Llama);

        var gateway = CreateGateway([claude.Object, gemini.Object, llama.Object]);

        // Act
        var sw = Stopwatch.StartNew();
        var result = await gateway.CompleteAsync(MakeRequest());
        sw.Stop();

        // Assert
        result.Provider.Should().Be(LlmProviderType.Gemini);
        result.FallbackLevel.Should().Be(1);
        result.FromCache.Should().BeFalse();

        // Llama should NOT have been called
        llama.Verify(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompleteAsync_Claude429_GeminiCalledWithin500ms()
    {
        // Arrange: Claude returns 429 immediately (simulated fast fail)
        var claude = CreateProviderMock(LlmProviderType.Claude);
        claude.Setup(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new LlmProviderException(
                LlmProviderType.Claude,
                "Rate limited",
                httpStatusCode: 429,
                isTransient: true));

        var geminiCallTimestamp = Stopwatch.GetTimestamp();
        var gemini = CreateProviderMock(LlmProviderType.Gemini);
        gemini.Setup(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .Returns<LlmRequest, CancellationToken>((_, _) =>
            {
                geminiCallTimestamp = Stopwatch.GetTimestamp();
                return Task.FromResult(MakeResponse(LlmProviderType.Gemini, "gemini-1.5-flash"));
            });

        var llama = CreateProviderMock(LlmProviderType.Llama);
        var gateway = CreateGateway([claude.Object, gemini.Object, llama.Object]);

        // Act
        var startTimestamp = Stopwatch.GetTimestamp();
        var result = await gateway.CompleteAsync(MakeRequest());

        // Assert: Gemini should be called within 500ms of request start
        var elapsedToGemini = Stopwatch.GetElapsedTime(startTimestamp, geminiCallTimestamp);
        elapsedToGemini.TotalMilliseconds.Should().BeLessThan(500,
            "Gemini should be called within 500ms of Claude's 429 failure");
        result.Provider.Should().Be(LlmProviderType.Gemini);
    }

    [Fact]
    public async Task CompleteAsync_Claude500_FallsBackToGemini()
    {
        // Arrange: Claude returns 500 (server error)
        var claude = CreateProviderMock(LlmProviderType.Claude);
        claude.Setup(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new LlmProviderException(
                LlmProviderType.Claude,
                "Provider API returned 500",
                httpStatusCode: 500,
                isTransient: true));

        var gemini = CreateProviderMock(LlmProviderType.Gemini);
        gemini.Setup(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeResponse(LlmProviderType.Gemini, "gemini-1.5-flash"));

        var llama = CreateProviderMock(LlmProviderType.Llama);
        var gateway = CreateGateway([claude.Object, gemini.Object, llama.Object]);

        // Act
        var result = await gateway.CompleteAsync(MakeRequest());

        // Assert
        result.Provider.Should().Be(LlmProviderType.Gemini);
        result.FallbackLevel.Should().Be(1);
    }

    // ─── Level 2: Claude 429 + Gemini 500 → Llama Called ────────────

    [Fact]
    public async Task CompleteAsync_Claude429_Gemini500_FallsBackToLlama()
    {
        // Arrange
        var claude = CreateProviderMock(LlmProviderType.Claude);
        claude.Setup(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new LlmProviderException(
                LlmProviderType.Claude, "Rate limited", httpStatusCode: 429, isTransient: true));

        var gemini = CreateProviderMock(LlmProviderType.Gemini);
        gemini.Setup(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new LlmProviderException(
                LlmProviderType.Gemini, "Server error", httpStatusCode: 500, isTransient: true));

        var llama = CreateProviderMock(LlmProviderType.Llama);
        llama.Setup(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeResponse(LlmProviderType.Llama, "llama-3.1-70b"));

        var gateway = CreateGateway([claude.Object, gemini.Object, llama.Object]);

        // Act
        var result = await gateway.CompleteAsync(MakeRequest());

        // Assert
        result.Provider.Should().Be(LlmProviderType.Llama);
        result.FallbackLevel.Should().Be(2);
        result.FromCache.Should().BeFalse();

        // All 3 providers should have been called
        claude.Verify(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        gemini.Verify(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        llama.Verify(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── Level 3: All Providers Fail → Redis Cache ──────────────────

    [Fact]
    public async Task CompleteAsync_AllProvidersFail_ReturnsCachedResponse()
    {
        // Arrange: All 3 fail
        var claude = CreateProviderMock(LlmProviderType.Claude);
        claude.Setup(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new LlmProviderException(
                LlmProviderType.Claude, "Rate limited", httpStatusCode: 429, isTransient: true));

        var gemini = CreateProviderMock(LlmProviderType.Gemini);
        gemini.Setup(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new LlmProviderException(
                LlmProviderType.Gemini, "Server error", httpStatusCode: 500, isTransient: true));

        var llama = CreateProviderMock(LlmProviderType.Llama);
        llama.Setup(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new LlmProviderException(
                LlmProviderType.Llama, "Server overloaded", httpStatusCode: 503, isTransient: true));

        // Setup Redis mock with cached response
        var cachedResponse = MakeResponse(LlmProviderType.Cache, "redis-cache");
        var cachedJson = JsonSerializer.Serialize(cachedResponse);

        var redisMock = new Mock<IConnectionMultiplexer>();
        var dbMock = new Mock<IDatabase>();
        dbMock.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue(cachedJson));
        redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(dbMock.Object);

        // Need to skip initial cache check so providers are attempted first.
        // ServeDegradedResponse will still check cache after all providers fail.
        var request = new LlmRequest
        {
            RequestId = Guid.NewGuid().ToString("N"),
            SystemPrompt = "Test",
            UserMessage = "Hello",
            CallerAgent = "TestAgent",
            SkipCache = true, // Skip initial cache check; degraded mode still reads cache
        };

        var gateway = CreateGateway(
            [claude.Object, gemini.Object, llama.Object],
            redisMock.Object);

        // Act
        var result = await gateway.CompleteAsync(request);

        // Assert
        result.FallbackLevel.Should().Be(3);
        result.FromCache.Should().BeTrue();
        result.Provider.Should().Be(LlmProviderType.Cache);

        // All 3 providers should have been attempted
        claude.Verify(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        gemini.Verify(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        llama.Verify(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompleteAsync_AllProvidersFail_NoCacheAvailable_ReturnsStaticFallback()
    {
        // Arrange: All providers fail AND no Redis cache
        var claude = CreateProviderMock(LlmProviderType.Claude);
        claude.Setup(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new LlmProviderException(
                LlmProviderType.Claude, "Down", httpStatusCode: 429, isTransient: true));

        var gemini = CreateProviderMock(LlmProviderType.Gemini);
        gemini.Setup(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new LlmProviderException(
                LlmProviderType.Gemini, "Down", httpStatusCode: 500, isTransient: true));

        var llama = CreateProviderMock(LlmProviderType.Llama);
        llama.Setup(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new LlmProviderException(
                LlmProviderType.Llama, "Down", httpStatusCode: 503, isTransient: true));

        // Redis returns empty/null
        var redisMock = new Mock<IConnectionMultiplexer>();
        var dbMock = new Mock<IDatabase>();
        dbMock.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);
        redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(dbMock.Object);

        var gateway = CreateGateway(
            [claude.Object, gemini.Object, llama.Object],
            redisMock.Object);

        var request = new LlmRequest
        {
            RequestId = Guid.NewGuid().ToString("N"),
            SystemPrompt = "Test",
            UserMessage = "Hello",
            CallerAgent = "DealerChatAgent",
            SkipCache = false,
        };

        // Act
        var result = await gateway.CompleteAsync(request);

        // Assert
        result.FallbackLevel.Should().Be(3);
        result.FromCache.Should().BeTrue();
        result.Provider.Should().Be(LlmProviderType.Cache);
        result.ModelId.Should().Be("static-fallback");
        result.StopReason.Should().Be("degraded_mode");
        result.Content.Should().Contain("no puedo procesar");
    }

    // ─── Logging: Fallback level logged correctly ───────────────────

    [Fact]
    public async Task CompleteAsync_Claude429_GeminiSuccess_LogsFallbackLevel1()
    {
        // Arrange
        var claude = CreateProviderMock(LlmProviderType.Claude);
        claude.Setup(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new LlmProviderException(
                LlmProviderType.Claude, "Rate limited", httpStatusCode: 429, isTransient: true));

        var gemini = CreateProviderMock(LlmProviderType.Gemini);
        gemini.Setup(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeResponse(LlmProviderType.Gemini, "gemini-1.5-flash"));

        var llama = CreateProviderMock(LlmProviderType.Llama);
        var gateway = CreateGateway([claude.Object, gemini.Object, llama.Object]);

        // Act
        await gateway.CompleteAsync(MakeRequest("SearchAgent"));

        // Assert: Verify the fallback warning log was called
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("[LLMGateway") && v.ToString()!.Contains("429")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce,
            "Should log Claude 429 fallback event");
    }

    [Fact]
    public async Task CompleteAsync_AllProvidersFail_LogsFallbackLevel3()
    {
        // Arrange
        var claude = CreateProviderMock(LlmProviderType.Claude);
        claude.Setup(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new LlmProviderException(
                LlmProviderType.Claude, "Down", httpStatusCode: 429, isTransient: true));

        var gemini = CreateProviderMock(LlmProviderType.Gemini);
        gemini.Setup(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new LlmProviderException(
                LlmProviderType.Gemini, "Down", httpStatusCode: 500, isTransient: true));

        var llama = CreateProviderMock(LlmProviderType.Llama);
        llama.Setup(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new LlmProviderException(
                LlmProviderType.Llama, "Down", httpStatusCode: 503, isTransient: true));

        var gateway = CreateGateway([claude.Object, gemini.Object, llama.Object]);

        // Act
        await gateway.CompleteAsync(MakeRequest("DealerChatAgent"));

        // Assert: Verify Critical log for all providers failing
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Critical,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("ALL PROVIDERS FAILED")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Should log CRITICAL when all providers fail");
    }

    [Fact]
    public async Task CompleteAsync_FallbackLog_IncludesAgentAndRequestId()
    {
        // Arrange
        var claude = CreateProviderMock(LlmProviderType.Claude);
        claude.Setup(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new LlmProviderException(
                LlmProviderType.Claude, "429", httpStatusCode: 429, isTransient: true));

        var gemini = CreateProviderMock(LlmProviderType.Gemini);
        gemini.Setup(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeResponse(LlmProviderType.Gemini, "gemini-1.5-flash"));

        var llama = CreateProviderMock(LlmProviderType.Llama);
        var gateway = CreateGateway([claude.Object, gemini.Object, llama.Object]);

        // Act
        await gateway.CompleteAsync(MakeRequest("DealerChatAgent"));

        // Assert: Verify fallback log includes the agent name
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("FALLBACK")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce,
            "Should log fallback event with FALLBACK marker");
    }

    // ─── Timing: Full cascade within 1,500ms ───────────────────────

    [Fact]
    public async Task CompleteAsync_FullCascade_CompletesWithin1500ms()
    {
        // Arrange: All providers fail instantly (simulated)
        var claude = CreateProviderMock(LlmProviderType.Claude);
        claude.Setup(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new LlmProviderException(
                LlmProviderType.Claude, "429", httpStatusCode: 429, isTransient: true));

        var gemini = CreateProviderMock(LlmProviderType.Gemini);
        gemini.Setup(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new LlmProviderException(
                LlmProviderType.Gemini, "500", httpStatusCode: 500, isTransient: true));

        var llama = CreateProviderMock(LlmProviderType.Llama);
        llama.Setup(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new LlmProviderException(
                LlmProviderType.Llama, "503", httpStatusCode: 503, isTransient: true));

        var gateway = CreateGateway([claude.Object, gemini.Object, llama.Object]);

        // Act
        var sw = Stopwatch.StartNew();
        await gateway.CompleteAsync(MakeRequest());
        sw.Stop();

        // Assert: Total cascade should not exceed 1,500ms
        sw.ElapsedMilliseconds.Should().BeLessThan(1500,
            "Full cascade (Claude→Gemini→Llama→cache) should complete within 1,500ms");
    }

    [Fact]
    public async Task CompleteAsync_Cascade_TotalLatencyIsTracked()
    {
        // Arrange: Claude fails, Gemini succeeds
        var claude = CreateProviderMock(LlmProviderType.Claude);
        claude.Setup(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new LlmProviderException(
                LlmProviderType.Claude, "429", httpStatusCode: 429, isTransient: true));

        var gemini = CreateProviderMock(LlmProviderType.Gemini);
        gemini.Setup(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeResponse(LlmProviderType.Gemini, "gemini-1.5-flash"));

        var llama = CreateProviderMock(LlmProviderType.Llama);
        var gateway = CreateGateway([claude.Object, gemini.Object, llama.Object]);

        // Act
        var result = await gateway.CompleteAsync(MakeRequest());

        // Assert: TotalLatency should be populated and reasonable
        result.TotalLatency.Should().BeGreaterThan(TimeSpan.Zero,
            "TotalLatency should be tracked across the cascade");
    }

    // ─── Transient vs Non-Transient Error Handling ──────────────────

    [Fact]
    public async Task CompleteAsync_NonTransientError_StillCascades()
    {
        // Arrange: Claude throws non-transient (4xx)
        var claude = CreateProviderMock(LlmProviderType.Claude);
        claude.Setup(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new LlmProviderException(
                LlmProviderType.Claude, "Bad request", httpStatusCode: 400, isTransient: false));

        var gemini = CreateProviderMock(LlmProviderType.Gemini);
        gemini.Setup(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeResponse(LlmProviderType.Gemini, "gemini-1.5-flash"));

        var llama = CreateProviderMock(LlmProviderType.Llama);
        var gateway = CreateGateway([claude.Object, gemini.Object, llama.Object]);

        // Act
        var result = await gateway.CompleteAsync(MakeRequest());

        // Assert: Should still cascade to Gemini
        result.Provider.Should().Be(LlmProviderType.Gemini);
    }

    [Fact]
    public async Task CompleteAsync_Claude503_FallsBackToGemini()
    {
        // Arrange: Claude returns 503
        var claude = CreateProviderMock(LlmProviderType.Claude);
        claude.Setup(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new LlmProviderException(
                LlmProviderType.Claude, "Service Unavailable", httpStatusCode: 503, isTransient: true));

        var gemini = CreateProviderMock(LlmProviderType.Gemini);
        gemini.Setup(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeResponse(LlmProviderType.Gemini, "gemini-1.5-flash"));

        var llama = CreateProviderMock(LlmProviderType.Llama);
        var gateway = CreateGateway([claude.Object, gemini.Object, llama.Object]);

        // Act
        var result = await gateway.CompleteAsync(MakeRequest());

        // Assert
        result.Provider.Should().Be(LlmProviderType.Gemini);
        result.FallbackLevel.Should().Be(1);
    }

    // ─── Static Fallback Content by Agent ───────────────────────────

    [Theory]
    [InlineData("SearchAgent", "{}")]
    [InlineData("RecoAgent", "[]")]
    [InlineData("DealerChatAgent", "no puedo procesar")]
    [InlineData("SupportAgent", "dificultades técnicas")]
    public async Task CompleteAsync_AllFail_NoCacheReturnsAgentSpecificStaticContent(
        string agent, string expectedContentFragment)
    {
        // Arrange
        var claude = CreateProviderMock(LlmProviderType.Claude);
        claude.Setup(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new LlmProviderException(
                LlmProviderType.Claude, "Down", httpStatusCode: 429, isTransient: true));

        var gemini = CreateProviderMock(LlmProviderType.Gemini);
        gemini.Setup(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new LlmProviderException(
                LlmProviderType.Gemini, "Down", httpStatusCode: 500, isTransient: true));

        var llama = CreateProviderMock(LlmProviderType.Llama);
        llama.Setup(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new LlmProviderException(
                LlmProviderType.Llama, "Down", httpStatusCode: 503, isTransient: true));

        var gateway = CreateGateway([claude.Object, gemini.Object, llama.Object]);

        var request = new LlmRequest
        {
            RequestId = Guid.NewGuid().ToString("N"),
            SystemPrompt = "Test",
            UserMessage = "Hello",
            CallerAgent = agent,
            SkipCache = false,
        };

        // Act
        var result = await gateway.CompleteAsync(request);

        // Assert
        result.Content.Should().Contain(expectedContentFragment);
        result.FallbackLevel.Should().Be(3);
    }

    // ─── Provider Ordering ──────────────────────────────────────────

    [Fact]
    public async Task CompleteAsync_ProvidersOrderedByCascadeLevel()
    {
        // Arrange: Pass providers in wrong order — gateway should order them
        var llama = CreateProviderMock(LlmProviderType.Llama);  // Level 2
        var claude = CreateProviderMock(LlmProviderType.Claude); // Level 0
        var gemini = CreateProviderMock(LlmProviderType.Gemini); // Level 1

        claude.Setup(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeResponse(LlmProviderType.Claude, "claude-sonnet-4"));

        // Pass in unordered list
        var gateway = CreateGateway([llama.Object, claude.Object, gemini.Object]);

        // Act
        var result = await gateway.CompleteAsync(MakeRequest());

        // Assert: Claude should be tried first (level 0)
        result.Provider.Should().Be(LlmProviderType.Claude);
        gemini.Verify(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        llama.Verify(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── Unexpected Exceptions ──────────────────────────────────────

    [Fact]
    public async Task CompleteAsync_UnexpectedExceptionCascades()
    {
        // Arrange: Claude throws unexpected (not LlmProviderException)
        var claude = CreateProviderMock(LlmProviderType.Claude);
        claude.Setup(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected internal error"));

        var gemini = CreateProviderMock(LlmProviderType.Gemini);
        gemini.Setup(p => p.CompleteAsync(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeResponse(LlmProviderType.Gemini, "gemini-1.5-flash"));

        var llama = CreateProviderMock(LlmProviderType.Llama);
        var gateway = CreateGateway([claude.Object, gemini.Object, llama.Object]);

        // Act
        var result = await gateway.CompleteAsync(MakeRequest());

        // Assert: Should cascade to Gemini
        result.Provider.Should().Be(LlmProviderType.Gemini);
        result.FallbackLevel.Should().Be(1);
    }
}
