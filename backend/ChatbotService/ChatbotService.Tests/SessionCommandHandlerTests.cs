using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ChatbotService.Application.Features.Sessions.Commands;
using ChatbotService.Domain.Entities;
using ChatbotService.Domain.Enums;
using ChatbotService.Domain.Interfaces;
using ChatbotService.Domain.Models;
using ChatbotService.Application.Interfaces;

namespace ChatbotService.Tests;

/// <summary>
/// Unit tests for SendMessageCommandHandler — covers LLM integration,
/// RAG inventory injection, quick response matching, and interaction limits.
/// </summary>
public class SendMessageCommandHandlerTests
{
    private readonly Mock<IChatSessionRepository> _sessionRepo;
    private readonly Mock<IChatMessageRepository> _messageRepo;
    private readonly Mock<IChatbotConfigurationRepository> _configRepo;
    private readonly Mock<IQuickResponseRepository> _quickResponseRepo;
    private readonly Mock<IChatModeStrategyFactory> _strategyFactory;
    private readonly Mock<IChatModeStrategy> _mockStrategy;
    private readonly Mock<ILlmService> _llmService;
    private readonly Mock<ILlmResponseCacheService> _cacheService;
    private readonly Mock<ILogger<SendMessageCommandHandler>> _logger;
    private readonly SendMessageCommandHandler _handler;

    public SendMessageCommandHandlerTests()
    {
        _sessionRepo = new Mock<IChatSessionRepository>();
        _messageRepo = new Mock<IChatMessageRepository>();
        _configRepo = new Mock<IChatbotConfigurationRepository>();
        _quickResponseRepo = new Mock<IQuickResponseRepository>();
        _strategyFactory = new Mock<IChatModeStrategyFactory>();
        _mockStrategy = new Mock<IChatModeStrategy>();
        _llmService = new Mock<ILlmService>();
        _cacheService = new Mock<ILlmResponseCacheService>();
        _logger = new Mock<ILogger<SendMessageCommandHandler>>();

        // Default strategy setup: return a grounded response
        _strategyFactory.Setup(f => f.GetStrategy(It.IsAny<ChatMode>()))
            .Returns(_mockStrategy.Object);
        _mockStrategy.Setup(s => s.BuildSystemPromptAsync(
                It.IsAny<ChatSession>(), It.IsAny<ChatbotConfiguration>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("System prompt for testing");
        _mockStrategy.Setup(s => s.ValidateResponseGroundingAsync(
                It.IsAny<ChatSession>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GroundingValidationResult { IsGrounded = true });

        _handler = new SendMessageCommandHandler(
            _sessionRepo.Object,
            _messageRepo.Object,
            _configRepo.Object,
            _quickResponseRepo.Object,
            _strategyFactory.Object,
            _llmService.Object,
            _cacheService.Object,
            new Mock<IChatbotSafetyMetrics>().Object,
            _logger.Object);
    }

    private ChatSession CreateActiveSession(Guid? configId = null) => new()
    {
        Id = Guid.NewGuid(),
        ChatbotConfigurationId = configId ?? Guid.NewGuid(),
        SessionToken = "test-token",
        Status = SessionStatus.Active,
        MaxInteractionsPerSession = 10,
        InteractionCount = 0,
        InteractionLimitReached = false,
        MessageCount = 0,
        Language = "es",
        CreatedAt = DateTime.UtcNow,
        LastActivityAt = DateTime.UtcNow
    };

    private ChatbotConfiguration CreateConfig(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Name = "Test Config",
        SystemPromptText = "Eres Ana, asistente de ventas.",
        WelcomeMessage = "¡Hola!",
        BotName = "Ana",
        MaxInteractionsPerSession = 10,
        IsActive = true
    };

    [Fact]
    public async Task Handle_ShouldCallLlm_WhenNoQuickResponseMatch()
    {
        // Arrange
        var session = CreateActiveSession();
        var config = CreateConfig(session.ChatbotConfigurationId);

        _sessionRepo.Setup(r => r.GetByTokenAsync("test-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _configRepo.Setup(r => r.GetByIdAsync(session.ChatbotConfigurationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);
        _quickResponseRepo.Setup(r => r.FindMatchingAsync(config.Id, "Hola", It.IsAny<CancellationToken>()))
            .ReturnsAsync((QuickResponse?)null);
        _llmService.Setup(s => s.GenerateResponseAsync(
                "test-token", "Hola", "es", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmDetectionResult
            {
                FulfillmentText = "¡Hola! ¿En qué puedo ayudarte?",
                DetectedIntent = "greeting",
                ConfidenceScore = 0.95f,
                IsFallback = false
            });

        var command = new SendMessageCommand("test-token", "Hola", "UserText", null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Response.Should().Be("¡Hola! ¿En qué puedo ayudarte?");
        result.IntentName.Should().Be("greeting");
        result.IsFallback.Should().BeFalse();
        _llmService.Verify(s => s.GenerateResponseAsync(
            "test-token", "Hola", "es", It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        _messageRepo.Verify(r => r.CreateAsync(It.IsAny<ChatMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_ShouldUseQuickResponse_WhenMatched()
    {
        // Arrange
        var session = CreateActiveSession();
        var config = CreateConfig(session.ChatbotConfigurationId);
        var quickResponse = new QuickResponse
        {
            Id = Guid.NewGuid(),
            ChatbotConfigurationId = config.Id,
            Name = "Greeting",
            Response = "¡Hola! Bienvenido a OKLA.",
            IsActive = true
        };

        _sessionRepo.Setup(r => r.GetByTokenAsync("test-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _configRepo.Setup(r => r.GetByIdAsync(session.ChatbotConfigurationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);
        _quickResponseRepo.Setup(r => r.FindMatchingAsync(config.Id, "hola", It.IsAny<CancellationToken>()))
            .ReturnsAsync(quickResponse);

        var command = new SendMessageCommand("test-token", "hola", "UserText", null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Response.Should().Be("¡Hola! Bienvenido a OKLA.");
        result.ConfidenceScore.Should().Be(1.0m);
        _llmService.Verify(s => s.GenerateResponseAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnLimitMessage_WhenInteractionLimitReached()
    {
        // Arrange
        var session = CreateActiveSession();
        session.InteractionLimitReached = true;

        var config = CreateConfig(session.ChatbotConfigurationId);

        _sessionRepo.Setup(r => r.GetByTokenAsync("test-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _configRepo.Setup(r => r.GetByIdAsync(session.ChatbotConfigurationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var command = new SendMessageCommand("test-token", "Hola", "UserText", null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFallback.Should().BeTrue();
        result.RemainingInteractions.Should().Be(0);
        result.Response.Should().Contain("límite");
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenSessionNotFound()
    {
        // Arrange
        _sessionRepo.Setup(r => r.GetByTokenAsync("invalid-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChatSession?)null);

        var command = new SendMessageCommand("invalid-token", "Hola", "UserText", null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldInjectRAG_WhenVehiclesAvailable()
    {
        // Arrange
        var session = CreateActiveSession();
        var config = CreateConfig(session.ChatbotConfigurationId);

        _sessionRepo.Setup(r => r.GetByTokenAsync("test-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _configRepo.Setup(r => r.GetByIdAsync(session.ChatbotConfigurationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);
        _quickResponseRepo.Setup(r => r.FindMatchingAsync(config.Id, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((QuickResponse?)null);
        // Strategy builds the system prompt with inventory data
        _mockStrategy.Setup(s => s.BuildSystemPromptAsync(
                It.IsAny<ChatSession>(), It.IsAny<ChatbotConfiguration>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("System prompt with INVENTARIO DISPONIBLE: Toyota Corolla 2024, Honda CR-V 2023");
        _llmService.Setup(s => s.GenerateResponseAsync(
                "test-token", It.IsAny<string>(), "es",
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmDetectionResult
            {
                FulfillmentText = "Tenemos un Toyota Corolla 2024 y un Honda CR-V 2023.",
                DetectedIntent = "vehicle.search",
                ConfidenceScore = 0.9f,
                IsFallback = false
            });

        var command = new SendMessageCommand("test-token", "¿Qué carros tienen?", "UserText", null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Response.Should().Contain("Toyota");
        // Verify strategy was called to build system prompt
        _mockStrategy.Verify(s => s.BuildSystemPromptAsync(
            It.IsAny<ChatSession>(), It.IsAny<ChatbotConfiguration>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _llmService.Verify(s => s.GenerateResponseAsync(
            "test-token", "¿Qué carros tienen?", "es",
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldIncrementInteractionCount_WhenLlmUsed()
    {
        // Arrange
        var session = CreateActiveSession();
        session.MaxInteractionsPerSession = 10;
        session.InteractionCount = 8;

        var config = CreateConfig(session.ChatbotConfigurationId);

        _sessionRepo.Setup(r => r.GetByTokenAsync("test-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        _configRepo.Setup(r => r.GetByIdAsync(session.ChatbotConfigurationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);
        _quickResponseRepo.Setup(r => r.FindMatchingAsync(config.Id, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((QuickResponse?)null);
        _llmService.Setup(s => s.GenerateResponseAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LlmDetectionResult
            {
                FulfillmentText = "Response",
                DetectedIntent = "general",
                ConfidenceScore = 0.8f,
                IsFallback = false
            });

        var command = new SendMessageCommand("test-token", "Test", "UserText", null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        session.InteractionCount.Should().Be(9);
        session.InteractionLimitReached.Should().BeFalse();
        _sessionRepo.Verify(r => r.UpdateAsync(session, It.IsAny<CancellationToken>()), Times.Once);
    }
}

/// <summary>
/// Unit tests for StartSessionCommandHandler.
/// </summary>
public class StartSessionCommandHandlerTests
{
    private readonly Mock<IChatSessionRepository> _sessionRepo;
    private readonly Mock<IChatbotConfigurationRepository> _configRepo;
    private readonly Mock<ILogger<StartSessionCommandHandler>> _logger;
    private readonly StartSessionCommandHandler _handler;

    public StartSessionCommandHandlerTests()
    {
        _sessionRepo = new Mock<IChatSessionRepository>();
        _configRepo = new Mock<IChatbotConfigurationRepository>();
        _logger = new Mock<ILogger<StartSessionCommandHandler>>();

        _handler = new StartSessionCommandHandler(
            _sessionRepo.Object,
            _configRepo.Object,
            _logger.Object);
    }

    [Fact]
    public async Task Handle_ShouldCreateSession_WithDefaultConfig()
    {
        // Arrange
        var config = new ChatbotConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "Default",
            WelcomeMessage = "¡Bienvenido!",
            BotName = "Ana",
            MaxInteractionsPerSession = 10
        };

        _configRepo.Setup(r => r.GetDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var command = new StartSessionCommand(
            Guid.NewGuid(), "Juan", "juan@test.com", null,
            "WebChat", "web", null, null, null, null, "es", null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.SessionToken.Should().NotBeNullOrEmpty();
        result.WelcomeMessage.Should().Be("¡Bienvenido!");
        result.BotName.Should().Be("Ana");
        result.MaxInteractionsPerSession.Should().Be(10);
        _sessionRepo.Verify(r => r.CreateAsync(It.IsAny<ChatSession>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldUseDealerConfig_WhenDealerIdProvided()
    {
        // Arrange
        var dealerId = Guid.NewGuid();
        var config = new ChatbotConfiguration
        {
            Id = Guid.NewGuid(),
            DealerId = dealerId,
            Name = "Dealer Config",
            WelcomeMessage = "¡Bienvenido al dealer!",
            BotName = "DealerBot",
            MaxInteractionsPerSession = 20
        };

        _configRepo.Setup(r => r.GetByDealerIdAsync(dealerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var command = new StartSessionCommand(
            null, null, null, null,
            "WebChat", "web", null, null, null, null, "es", dealerId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.WelcomeMessage.Should().Be("¡Bienvenido al dealer!");
        result.MaxInteractionsPerSession.Should().Be(20);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenNoConfigFound()
    {
        // Arrange
        _configRepo.Setup(r => r.GetDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChatbotConfiguration?)null);

        var command = new StartSessionCommand(
            null, null, null, null,
            "WebChat", "web", null, null, null, null, "es", null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));
    }
}

/// <summary>
/// Unit tests for EndSessionCommandHandler.
/// </summary>
public class EndSessionCommandHandlerTests
{
    private readonly Mock<IChatSessionRepository> _sessionRepo;
    private readonly Mock<ILogger<EndSessionCommandHandler>> _logger;
    private readonly EndSessionCommandHandler _handler;

    public EndSessionCommandHandlerTests()
    {
        _sessionRepo = new Mock<IChatSessionRepository>();
        _logger = new Mock<ILogger<EndSessionCommandHandler>>();
        _handler = new EndSessionCommandHandler(_sessionRepo.Object, _logger.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnFalse_WhenSessionNotFound()
    {
        _sessionRepo.Setup(r => r.GetByTokenAsync("invalid", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChatSession?)null);

        var result = await _handler.Handle(new EndSessionCommand("invalid", null), CancellationToken.None);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldEndSession_WhenFound()
    {
        var session = new ChatSession
        {
            Id = Guid.NewGuid(),
            SessionToken = "valid-token",
            Status = SessionStatus.Active,
            CreatedAt = DateTime.UtcNow.AddMinutes(-30)
        };

        _sessionRepo.Setup(r => r.GetByTokenAsync("valid-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var result = await _handler.Handle(new EndSessionCommand("valid-token", "Done"), CancellationToken.None);

        result.Should().BeTrue();
        session.Status.Should().Be(SessionStatus.Completed);
        session.EndedAt.Should().NotBeNull();
        session.SessionDurationSeconds.Should().BeGreaterThan(0);
    }
}

/// <summary>
/// Unit tests for TransferToAgentCommandHandler.
/// </summary>
public class TransferToAgentCommandHandlerTests
{
    private readonly Mock<IChatSessionRepository> _sessionRepo;
    private readonly Mock<IChatLeadRepository> _leadRepo;
    private readonly Mock<ILogger<TransferToAgentCommandHandler>> _logger;
    private readonly TransferToAgentCommandHandler _handler;

    public TransferToAgentCommandHandlerTests()
    {
        _sessionRepo = new Mock<IChatSessionRepository>();
        _leadRepo = new Mock<IChatLeadRepository>();
        _logger = new Mock<ILogger<TransferToAgentCommandHandler>>();
        _handler = new TransferToAgentCommandHandler(_sessionRepo.Object, _leadRepo.Object, _logger.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenSessionNotFound()
    {
        _sessionRepo.Setup(r => r.GetByTokenAsync("invalid", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChatSession?)null);

        var result = await _handler.Handle(
            new TransferToAgentCommand("invalid", null, null), CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldCreateLead_WhenContactInfoProvided()
    {
        var session = new ChatSession
        {
            Id = Guid.NewGuid(),
            SessionToken = "valid-token",
            Status = SessionStatus.Active,
            UserName = "Juan",
            UserEmail = "juan@test.com",
            UserPhone = "8095551234"
        };

        _sessionRepo.Setup(r => r.GetByTokenAsync("valid-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var result = await _handler.Handle(
            new TransferToAgentCommand("valid-token", "Need help", null), CancellationToken.None);

        result.Success.Should().BeTrue();
        session.Status.Should().Be(SessionStatus.TransferredToAgent);
        _leadRepo.Verify(r => r.CreateAsync(It.IsAny<ChatLead>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldNotCreateLead_WhenNoContactInfo()
    {
        var session = new ChatSession
        {
            Id = Guid.NewGuid(),
            SessionToken = "valid-token",
            Status = SessionStatus.Active,
            UserName = null,
            UserEmail = null,
            UserPhone = null
        };

        _sessionRepo.Setup(r => r.GetByTokenAsync("valid-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var result = await _handler.Handle(
            new TransferToAgentCommand("valid-token", "Need help", null), CancellationToken.None);

        result.Success.Should().BeTrue();
        _leadRepo.Verify(r => r.CreateAsync(It.IsAny<ChatLead>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
