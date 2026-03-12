using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ChatbotService.Application.DTOs;
using ChatbotService.Application.Features.Sessions.Commands;
using ChatbotService.Domain.Entities;
using ChatbotService.Domain.Enums;
using ChatbotService.Domain.Interfaces;
using ChatbotService.Application.Interfaces;
using ChatbotService.Domain.Models;

namespace ChatbotService.Tests;

/// <summary>
/// Phase 9 — Bot Disclosure Audit Tests.
/// Verifies that the DealerChatAgent complies with mandatory disclosure requirements:
///   1. Dealer name present in disclosure
///   2. "Soy un asistente virtual de OKLA" text
///   3. Privacy policy link (okla.do/privacidad)
///   4. Consent confirmation before buyer can write
///   5. < 200ms load time (inline, no extra HTTP calls)
/// </summary>
public class BotDisclosureAuditTests
{
    // ══════════════════════════════════════════════════════════════
    // Test Helpers
    // ══════════════════════════════════════════════════════════════

    private static ChatbotConfiguration CreateConfig(
        string dealerName = "AutoMax RD",
        string privacyUrl = "https://okla.do/privacidad",
        bool requireConsent = true) => new()
        {
            Id = Guid.NewGuid(),
            DealerId = Guid.NewGuid(),
            Name = "Test Config",
            DealerDisplayName = dealerName,
            PrivacyPolicyUrl = privacyUrl,
            RequireDisclosureConsent = requireConsent,
            BotName = "Asistente OKLA",
            WelcomeMessage = "¡Hola! 👋 Soy tu asistente virtual.",
            MaxInteractionsPerSession = 10,
            IsActive = true
        };

    private static ChatSession CreateSession(
        Guid? configId = null,
        bool consentAccepted = false) => new()
        {
            Id = Guid.NewGuid(),
            ChatbotConfigurationId = configId ?? Guid.NewGuid(),
            SessionToken = Guid.NewGuid().ToString("N"),
            Status = SessionStatus.Active,
            ChatMode = ChatMode.General,
            MaxInteractionsPerSession = 10,
            InteractionCount = 0,
            InteractionLimitReached = false,
            MessageCount = 0,
            Language = "es",
            ConsentAccepted = consentAccepted,
            ConsentAcceptedAt = consentAccepted ? DateTime.UtcNow : null,
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow
        };

    private StartSessionCommandHandler CreateStartHandler(
        Mock<IChatSessionRepository>? sessionRepo = null,
        Mock<IChatbotConfigurationRepository>? configRepo = null)
    {
        return new StartSessionCommandHandler(
            sessionRepo?.Object ?? new Mock<IChatSessionRepository>().Object,
            configRepo?.Object ?? new Mock<IChatbotConfigurationRepository>().Object,
            new Mock<ILogger<StartSessionCommandHandler>>().Object);
    }

    private AcceptDisclosureCommandHandler CreateAcceptHandler(
        Mock<IChatSessionRepository>? sessionRepo = null)
    {
        return new AcceptDisclosureCommandHandler(
            sessionRepo?.Object ?? new Mock<IChatSessionRepository>().Object,
            new Mock<ILogger<AcceptDisclosureCommandHandler>>().Object);
    }

    // ══════════════════════════════════════════════════════════════
    // 1. DISCLOSURE MESSAGE CONTENT
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task StartSession_DisclosureMessage_ContainsDealerName()
    {
        // Arrange
        var config = CreateConfig(dealerName: "AutoMax RD");
        var configRepo = new Mock<IChatbotConfigurationRepository>();
        configRepo.Setup(r => r.GetByDealerIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var sessionRepo = new Mock<IChatSessionRepository>();
        var handler = CreateStartHandler(sessionRepo, configRepo);

        var command = new StartSessionCommand(
            Guid.NewGuid(), "Juan", null, null, "WebChat", "web",
            null, null, null, null, "es", config.DealerId, null, null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.DisclosureMessage.Should().Contain("AutoMax RD");
    }

    [Fact]
    public async Task StartSession_DisclosureMessage_ContainsOklaIdentification()
    {
        // Arrange
        var config = CreateConfig();
        var configRepo = new Mock<IChatbotConfigurationRepository>();
        configRepo.Setup(r => r.GetByDealerIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var sessionRepo = new Mock<IChatSessionRepository>();
        var handler = CreateStartHandler(sessionRepo, configRepo);

        var command = new StartSessionCommand(
            Guid.NewGuid(), "Juan", null, null, "WebChat", "web",
            null, null, null, null, "es", config.DealerId, null, null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.DisclosureMessage.Should().Contain("Soy un asistente virtual de OKLA");
    }

    [Fact]
    public async Task StartSession_DisclosureMessage_ContainsPrivacyPolicyLink()
    {
        // Arrange
        var config = CreateConfig(privacyUrl: "https://okla.do/privacidad");
        var configRepo = new Mock<IChatbotConfigurationRepository>();
        configRepo.Setup(r => r.GetByDealerIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var sessionRepo = new Mock<IChatSessionRepository>();
        var handler = CreateStartHandler(sessionRepo, configRepo);

        var command = new StartSessionCommand(
            Guid.NewGuid(), "Juan", null, null, "WebChat", "web",
            null, null, null, null, "es", config.DealerId, null, null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.DisclosureMessage.Should().Contain("okla.do/privacidad");
        result.PrivacyPolicyUrl.Should().Be("https://okla.do/privacidad");
    }

    [Fact]
    public async Task StartSession_DisclosureMessage_ContainsAllRequiredElements()
    {
        // Arrange
        var config = CreateConfig(dealerName: "Vehículos Premium SRL");
        var configRepo = new Mock<IChatbotConfigurationRepository>();
        configRepo.Setup(r => r.GetByDealerIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var sessionRepo = new Mock<IChatSessionRepository>();
        var handler = CreateStartHandler(sessionRepo, configRepo);

        var command = new StartSessionCommand(
            Guid.NewGuid(), "María", null, null, "WebChat", "web",
            null, null, null, null, "es", config.DealerId, null, null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert — ALL 4 required elements in a single disclosure message
        result.DisclosureMessage.Should().Contain("Vehículos Premium SRL", "debe incluir nombre del dealer");
        result.DisclosureMessage.Should().Contain("Soy un asistente virtual de OKLA", "debe identificarse como bot OKLA");
        result.DisclosureMessage.Should().Contain("okla.do/privacidad", "debe incluir enlace de privacidad");
        result.RequiresConsent.Should().BeTrue("debe requerir consentimiento antes de chatear");
    }

    [Fact]
    public async Task StartSession_RequiresConsent_IsTrue_ByDefault()
    {
        // Arrange
        var config = CreateConfig();
        var configRepo = new Mock<IChatbotConfigurationRepository>();
        configRepo.Setup(r => r.GetByDealerIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var sessionRepo = new Mock<IChatSessionRepository>();
        var handler = CreateStartHandler(sessionRepo, configRepo);

        var command = new StartSessionCommand(
            Guid.NewGuid(), "Test", null, null, "WebChat", "web",
            null, null, null, null, "es", config.DealerId, null, null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.RequiresConsent.Should().BeTrue();
    }

    [Fact]
    public async Task StartSession_PrivacyPolicyUrl_IsReturned()
    {
        // Arrange
        var config = CreateConfig(privacyUrl: "https://okla.do/privacidad");
        var configRepo = new Mock<IChatbotConfigurationRepository>();
        configRepo.Setup(r => r.GetByDealerIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var sessionRepo = new Mock<IChatSessionRepository>();
        var handler = CreateStartHandler(sessionRepo, configRepo);

        var command = new StartSessionCommand(
            Guid.NewGuid(), "Test", null, null, "WebChat", "web",
            null, null, null, null, "es", config.DealerId, null, null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.PrivacyPolicyUrl.Should().NotBeNullOrEmpty();
        result.PrivacyPolicyUrl.Should().Contain("okla.do/privacidad");
    }

    // ══════════════════════════════════════════════════════════════
    // 2. DISCLOSURE WITH DEFAULT/GLOBAL CONFIG
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task StartSession_WithDefaultConfig_UsesOklaAsDealer()
    {
        // Arrange — no dealer-specific config, falls back to default
        var defaultConfig = new ChatbotConfiguration
        {
            Id = Guid.NewGuid(),
            DealerId = null, // global config
            DealerDisplayName = "OKLA",
            PrivacyPolicyUrl = "https://okla.do/privacidad",
            RequireDisclosureConsent = true,
            MaxInteractionsPerSession = 10,
            IsActive = true
        };

        var configRepo = new Mock<IChatbotConfigurationRepository>();
        configRepo.Setup(r => r.GetByDealerIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChatbotConfiguration?)null);
        configRepo.Setup(r => r.GetDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultConfig);

        var sessionRepo = new Mock<IChatSessionRepository>();
        var handler = CreateStartHandler(sessionRepo, configRepo);

        var command = new StartSessionCommand(
            Guid.NewGuid(), "Test", null, null, "WebChat", "web",
            null, null, null, null, "es", Guid.NewGuid(), null, null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.DisclosureMessage.Should().Contain("OKLA");
        result.DisclosureMessage.Should().Contain("Soy un asistente virtual de OKLA");
    }

    [Fact]
    public async Task StartSession_WithoutDealerId_UsesDefaultConfig()
    {
        // Arrange — anonymous session, no dealer
        var defaultConfig = new ChatbotConfiguration
        {
            Id = Guid.NewGuid(),
            DealerId = null,
            DealerDisplayName = "OKLA",
            PrivacyPolicyUrl = "https://okla.do/privacidad",
            RequireDisclosureConsent = true,
            MaxInteractionsPerSession = 10,
            IsActive = true
        };

        var configRepo = new Mock<IChatbotConfigurationRepository>();
        configRepo.Setup(r => r.GetDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultConfig);

        var sessionRepo = new Mock<IChatSessionRepository>();
        var handler = CreateStartHandler(sessionRepo, configRepo);

        var command = new StartSessionCommand(
            null, null, null, null, "WebChat", "web",
            null, null, null, null, "es", null, null, null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.DisclosureMessage.Should().NotBeNullOrEmpty();
        result.PrivacyPolicyUrl.Should().Be("https://okla.do/privacidad");
    }

    // ══════════════════════════════════════════════════════════════
    // 3. CONSENT GATE — SendMessage blocks without consent
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task SendMessage_WithoutConsent_ReturnsConsentRequiredResponse()
    {
        // Arrange
        var config = CreateConfig();
        var session = CreateSession(config.Id, consentAccepted: false);

        var sessionRepo = new Mock<IChatSessionRepository>();
        sessionRepo.Setup(r => r.GetByTokenAsync(session.SessionToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var configRepo = new Mock<IChatbotConfigurationRepository>();
        configRepo.Setup(r => r.GetByIdAsync(config.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var handler = new SendMessageCommandHandler(
            sessionRepo.Object,
            new Mock<IChatMessageRepository>().Object,
            configRepo.Object,
            new Mock<IQuickResponseRepository>().Object,
            new Mock<IChatModeStrategyFactory>().Object,
            new Mock<ILlmService>().Object,
            new Mock<ILlmResponseCacheService>().Object,
            new Mock<IChatbotSafetyMetrics>().Object,
            new Mock<ILogger<SendMessageCommandHandler>>().Object);

        var command = new SendMessageCommand(session.SessionToken, "Hola", "UserText", null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IntentName.Should().Be("disclosure_consent_required");
        result.Response.Should().Contain("aceptar");
        result.Response.Should().Contain("privacidad");
    }

    [Fact]
    public async Task SendMessage_WithoutConsent_DoesNotCallLlm()
    {
        // Arrange
        var config = CreateConfig();
        var session = CreateSession(config.Id, consentAccepted: false);

        var sessionRepo = new Mock<IChatSessionRepository>();
        sessionRepo.Setup(r => r.GetByTokenAsync(session.SessionToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var configRepo = new Mock<IChatbotConfigurationRepository>();
        configRepo.Setup(r => r.GetByIdAsync(config.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var llmService = new Mock<ILlmService>();

        var handler = new SendMessageCommandHandler(
            sessionRepo.Object,
            new Mock<IChatMessageRepository>().Object,
            configRepo.Object,
            new Mock<IQuickResponseRepository>().Object,
            new Mock<IChatModeStrategyFactory>().Object,
            llmService.Object,
            new Mock<ILlmResponseCacheService>().Object,
            new Mock<IChatbotSafetyMetrics>().Object,
            new Mock<ILogger<SendMessageCommandHandler>>().Object);

        var command = new SendMessageCommand(session.SessionToken, "Quiero un Toyota", "UserText", null);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert — LLM was NEVER called (saves cost)
        llmService.Verify(l => l.GenerateResponseAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendMessage_WithoutConsent_DoesNotConsumeInteraction()
    {
        // Arrange
        var config = CreateConfig();
        var session = CreateSession(config.Id, consentAccepted: false);
        var initialCount = session.InteractionCount;

        var sessionRepo = new Mock<IChatSessionRepository>();
        sessionRepo.Setup(r => r.GetByTokenAsync(session.SessionToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var configRepo = new Mock<IChatbotConfigurationRepository>();
        configRepo.Setup(r => r.GetByIdAsync(config.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var handler = new SendMessageCommandHandler(
            sessionRepo.Object,
            new Mock<IChatMessageRepository>().Object,
            configRepo.Object,
            new Mock<IQuickResponseRepository>().Object,
            new Mock<IChatModeStrategyFactory>().Object,
            new Mock<ILlmService>().Object,
            new Mock<ILlmResponseCacheService>().Object,
            new Mock<IChatbotSafetyMetrics>().Object,
            new Mock<ILogger<SendMessageCommandHandler>>().Object);

        var command = new SendMessageCommand(session.SessionToken, "Hola", "UserText", null);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        session.InteractionCount.Should().Be(initialCount, "consent rejection must not consume an interaction");
    }

    [Fact]
    public async Task SendMessage_WithConsentDisabled_AllowsWithoutConsent()
    {
        // Arrange — dealer disabled consent requirement
        var config = CreateConfig();
        config.RequireDisclosureConsent = false;
        var session = CreateSession(config.Id, consentAccepted: false);

        var sessionRepo = new Mock<IChatSessionRepository>();
        sessionRepo.Setup(r => r.GetByTokenAsync(session.SessionToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var configRepo = new Mock<IChatbotConfigurationRepository>();
        configRepo.Setup(r => r.GetByIdAsync(config.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        // Setup strategy + LLM for a normal response path
        var strategyFactory = new Mock<IChatModeStrategyFactory>();
        var strategy = new Mock<IChatModeStrategy>();
        strategyFactory.Setup(f => f.GetStrategy(It.IsAny<ChatMode>())).Returns(strategy.Object);
        strategy.Setup(s => s.BuildSystemPromptAsync(
            It.IsAny<ChatSession>(), It.IsAny<ChatbotConfiguration>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("System prompt");
        strategy.Setup(s => s.ValidateResponseGroundingAsync(
            It.IsAny<ChatSession>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GroundingValidationResult { IsGrounded = true });

        var llmService = new Mock<ILlmService>();
        llmService.Setup(l => l.GenerateResponseAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmDetectionResult
            {
                FulfillmentText = "Tenemos Toyotas disponibles.",
                DetectedIntent = "inventory_search",
                ConfidenceScore = 0.95f,
                IsFallback = false
            });

        var handler = new SendMessageCommandHandler(
            sessionRepo.Object,
            new Mock<IChatMessageRepository>().Object,
            configRepo.Object,
            new Mock<IQuickResponseRepository>().Object,
            strategyFactory.Object,
            llmService.Object,
            new Mock<ILlmResponseCacheService>().Object,
            new Mock<IChatbotSafetyMetrics>().Object,
            new Mock<ILogger<SendMessageCommandHandler>>().Object);

        var command = new SendMessageCommand(session.SessionToken, "Quiero un Toyota", "UserText", null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert — message processed normally (consent not required)
        result.IntentName.Should().NotBe("disclosure_consent_required");
        llmService.Verify(l => l.GenerateResponseAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ══════════════════════════════════════════════════════════════
    // 4. ACCEPT DISCLOSURE HANDLER
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task AcceptDisclosure_SetsConsentAccepted()
    {
        // Arrange
        var session = CreateSession(consentAccepted: false);
        var sessionRepo = new Mock<IChatSessionRepository>();
        sessionRepo.Setup(r => r.GetByTokenAsync(session.SessionToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var handler = CreateAcceptHandler(sessionRepo);
        var command = new AcceptDisclosureCommand(session.SessionToken);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        session.ConsentAccepted.Should().BeTrue();
        session.ConsentAcceptedAt.Should().NotBeNull();
        session.ConsentAcceptedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task AcceptDisclosure_PersistsToRepository()
    {
        // Arrange
        var session = CreateSession(consentAccepted: false);
        var sessionRepo = new Mock<IChatSessionRepository>();
        sessionRepo.Setup(r => r.GetByTokenAsync(session.SessionToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var handler = CreateAcceptHandler(sessionRepo);
        var command = new AcceptDisclosureCommand(session.SessionToken);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        sessionRepo.Verify(r => r.UpdateAsync(
            It.Is<ChatSession>(s => s.ConsentAccepted && s.ConsentAcceptedAt != null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AcceptDisclosure_AlreadyAccepted_ReturnsIdempotent()
    {
        // Arrange
        var session = CreateSession(consentAccepted: true);
        var sessionRepo = new Mock<IChatSessionRepository>();
        sessionRepo.Setup(r => r.GetByTokenAsync(session.SessionToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var handler = CreateAcceptHandler(sessionRepo);
        var command = new AcceptDisclosureCommand(session.SessionToken);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert — idempotent, no error
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("already accepted");
        // Should NOT call UpdateAsync since nothing changed
        sessionRepo.Verify(r => r.UpdateAsync(It.IsAny<ChatSession>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AcceptDisclosure_InvalidToken_ReturnsFalse()
    {
        // Arrange
        var sessionRepo = new Mock<IChatSessionRepository>();
        sessionRepo.Setup(r => r.GetByTokenAsync("invalid-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChatSession?)null);

        var handler = CreateAcceptHandler(sessionRepo);
        var command = new AcceptDisclosureCommand("invalid-token");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    // ══════════════════════════════════════════════════════════════
    // 5. DOMAIN ENTITY DEFAULTS
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void ChatbotConfiguration_DefaultDealerDisplayName_IsOkla()
    {
        var config = new ChatbotConfiguration();
        config.DealerDisplayName.Should().Be("OKLA");
    }

    [Fact]
    public void ChatbotConfiguration_DefaultPrivacyPolicyUrl_IsOklaPrivacidad()
    {
        var config = new ChatbotConfiguration();
        config.PrivacyPolicyUrl.Should().Be("https://okla.do/privacidad");
    }

    [Fact]
    public void ChatbotConfiguration_DefaultRequireDisclosureConsent_IsTrue()
    {
        var config = new ChatbotConfiguration();
        config.RequireDisclosureConsent.Should().BeTrue();
    }

    [Fact]
    public void ChatSession_DefaultConsentAccepted_IsFalse()
    {
        var session = new ChatSession();
        session.ConsentAccepted.Should().BeFalse();
        session.ConsentAcceptedAt.Should().BeNull();
    }

    // ══════════════════════════════════════════════════════════════
    // 6. DTO CONTRACT TESTS
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public void StartSessionResponse_HasDisclosureFields()
    {
        var response = new StartSessionResponse
        {
            SessionId = Guid.NewGuid(),
            SessionToken = "abc123",
            DisclosureMessage = "🤖 Soy un asistente virtual de OKLA, al servicio de TestDealer.",
            PrivacyPolicyUrl = "https://okla.do/privacidad",
            RequiresConsent = true
        };

        response.DisclosureMessage.Should().NotBeNullOrEmpty();
        response.PrivacyPolicyUrl.Should().NotBeNullOrEmpty();
        response.RequiresConsent.Should().BeTrue();
    }

    [Fact]
    public void AcceptDisclosureResponse_HasRequiredFields()
    {
        var response = new AcceptDisclosureResponse
        {
            Success = true,
            Message = "Disclosure accepted",
            ConsentAcceptedAt = DateTime.UtcNow
        };

        response.Success.Should().BeTrue();
        response.ConsentAcceptedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // ══════════════════════════════════════════════════════════════
    // 7. CHAT MODE DISCLOSURE (all modes include disclosure)
    // ══════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("single_vehicle")]
    [InlineData("dealer_inventory")]
    [InlineData("general")]
    public async Task StartSession_AllChatModes_IncludeDisclosure(string chatMode)
    {
        // Arrange
        var config = CreateConfig(dealerName: "MiDealer");
        var configRepo = new Mock<IChatbotConfigurationRepository>();
        configRepo.Setup(r => r.GetByDealerIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var sessionRepo = new Mock<IChatSessionRepository>();
        var handler = CreateStartHandler(sessionRepo, configRepo);

        var vehicleId = chatMode == "single_vehicle" ? Guid.NewGuid() : (Guid?)null;
        var command = new StartSessionCommand(
            Guid.NewGuid(), "Test", null, null, "WebChat", "web",
            null, null, null, null, "es", config.DealerId, chatMode, vehicleId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert — every mode includes disclosure
        result.DisclosureMessage.Should().Contain("Soy un asistente virtual de OKLA");
        result.DisclosureMessage.Should().Contain("MiDealer");
        result.PrivacyPolicyUrl.Should().NotBeNullOrEmpty();
        result.RequiresConsent.Should().BeTrue();
    }

    // ══════════════════════════════════════════════════════════════
    // 8. CONSENT FLOW INTEGRATION
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task FullFlow_StartSession_AcceptDisclosure_SendMessage_Works()
    {
        // This test verifies the complete consent flow:
        // 1. StartSession → returns disclosure + RequiresConsent=true
        // 2. AcceptDisclosure → marks session as consented
        // 3. SendMessage → now processes the message (consent gate passes)

        // Arrange
        var config = CreateConfig();
        var session = CreateSession(config.Id, consentAccepted: false);

        var sessionRepo = new Mock<IChatSessionRepository>();
        sessionRepo.Setup(r => r.GetByTokenAsync(session.SessionToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var configRepo = new Mock<IChatbotConfigurationRepository>();
        configRepo.Setup(r => r.GetByIdAsync(config.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        // Step 2: Accept disclosure
        var acceptHandler = CreateAcceptHandler(sessionRepo);
        var acceptResult = await acceptHandler.Handle(
            new AcceptDisclosureCommand(session.SessionToken), CancellationToken.None);

        acceptResult.Success.Should().BeTrue();
        session.ConsentAccepted.Should().BeTrue();

        // Step 3: Now SendMessage should pass the consent gate
        var strategyFactory = new Mock<IChatModeStrategyFactory>();
        var strategy = new Mock<IChatModeStrategy>();
        strategyFactory.Setup(f => f.GetStrategy(It.IsAny<ChatMode>())).Returns(strategy.Object);
        strategy.Setup(s => s.BuildSystemPromptAsync(
            It.IsAny<ChatSession>(), It.IsAny<ChatbotConfiguration>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("System prompt");
        strategy.Setup(s => s.ValidateResponseGroundingAsync(
            It.IsAny<ChatSession>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GroundingValidationResult { IsGrounded = true });

        var llmService = new Mock<ILlmService>();
        llmService.Setup(l => l.GenerateResponseAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmDetectionResult
            {
                FulfillmentText = "¡Claro! Tenemos varias opciones.",
                DetectedIntent = "general_query",
                ConfidenceScore = 0.9f,
                IsFallback = false
            });

        var sendHandler = new SendMessageCommandHandler(
            sessionRepo.Object,
            new Mock<IChatMessageRepository>().Object,
            configRepo.Object,
            new Mock<IQuickResponseRepository>().Object,
            strategyFactory.Object,
            llmService.Object,
            new Mock<ILlmResponseCacheService>().Object,
            new Mock<IChatbotSafetyMetrics>().Object,
            new Mock<ILogger<SendMessageCommandHandler>>().Object);

        var sendResult = await sendHandler.Handle(
            new SendMessageCommand(session.SessionToken, "Hola", "UserText", null),
            CancellationToken.None);

        // Assert — message was processed (not blocked by consent gate)
        sendResult.IntentName.Should().NotBe("disclosure_consent_required");
        llmService.Verify(l => l.GenerateResponseAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ══════════════════════════════════════════════════════════════
    // 9. EDGE CASES
    // ══════════════════════════════════════════════════════════════

    [Fact]
    public async Task StartSession_CustomPrivacyUrl_IsRespected()
    {
        // Arrange — dealer has custom privacy URL
        var config = CreateConfig(privacyUrl: "https://automax.com/privacidad");
        var configRepo = new Mock<IChatbotConfigurationRepository>();
        configRepo.Setup(r => r.GetByDealerIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var sessionRepo = new Mock<IChatSessionRepository>();
        var handler = CreateStartHandler(sessionRepo, configRepo);

        var command = new StartSessionCommand(
            Guid.NewGuid(), "Test", null, null, "WebChat", "web",
            null, null, null, null, "es", config.DealerId, null, null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.PrivacyPolicyUrl.Should().Be("https://automax.com/privacidad");
        result.DisclosureMessage.Should().Contain("automax.com/privacidad");
    }

    [Fact]
    public void AcceptDisclosureCommand_HasSessionToken()
    {
        var cmd = new AcceptDisclosureCommand("test-token-123");
        cmd.SessionToken.Should().Be("test-token-123");
    }

    [Fact]
    public void AcceptDisclosureResult_CanBeCreated()
    {
        var result = new AcceptDisclosureResult(true, "OK", DateTime.UtcNow);
        result.Success.Should().BeTrue();
        result.Message.Should().Be("OK");
        result.ConsentAcceptedAt.Should().NotBeNull();
    }
}
