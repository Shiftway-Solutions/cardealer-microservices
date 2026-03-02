using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SupportAgent.Application.DTOs;
using SupportAgent.Application.Features.Chat.Commands;
using SupportAgent.Domain.Entities;
using SupportAgent.Domain.Interfaces;
using Xunit;

namespace SupportAgent.Tests;

public class SendMessageCommandHandlerTests
{
    private readonly Mock<IClaudeSupportService> _claudeServiceMock;
    private readonly Mock<IChatSessionRepository> _sessionRepoMock;
    private readonly Mock<ISupportAgentConfigRepository> _configRepoMock;
    private readonly Mock<ILogger<SendMessageCommandHandler>> _loggerMock;
    private readonly SendMessageCommandHandler _handler;

    public SendMessageCommandHandlerTests()
    {
        _claudeServiceMock = new Mock<IClaudeSupportService>();
        _sessionRepoMock = new Mock<IChatSessionRepository>();
        _configRepoMock = new Mock<ISupportAgentConfigRepository>();
        _loggerMock = new Mock<ILogger<SendMessageCommandHandler>>();

        _configRepoMock.Setup(x => x.GetActiveConfigAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SupportAgentConfig());

        _handler = new SendMessageCommandHandler(
            _claudeServiceMock.Object,
            _sessionRepoMock.Object,
            _configRepoMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_NewSession_CreatesSessionAndReturnsResponse()
    {
        // Arrange
        var command = new SendMessageCommand("Hola, necesito ayuda", null, "user-123", "127.0.0.1");

        _sessionRepoMock.Setup(x => x.GetBySessionIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChatSession?)null);

        _sessionRepoMock.Setup(x => x.CreateAsync(It.IsAny<ChatSession>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChatSession s, CancellationToken _) => s);

        _sessionRepoMock.Setup(x => x.GetMessagesAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatMessage>());

        _claudeServiceMock.Setup(x => x.SendMessageAsync(
                It.IsAny<string>(), It.IsAny<List<ConversationMessage>>(),
                It.IsAny<string>(), It.IsAny<float>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClaudeSupportResponse(
                "¡Hola! 👋 ¿En qué puedo ayudarte hoy?", 100, 50, "end_turn"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Response.Should().Contain("Hola");
        result.SessionId.Should().NotBeNullOrEmpty();
        result.DetectedModule.Should().Be("conversacional");

        _sessionRepoMock.Verify(x => x.CreateAsync(It.IsAny<ChatSession>(), It.IsAny<CancellationToken>()), Times.Once);
        _sessionRepoMock.Verify(x => x.AddMessageAsync(It.IsAny<ChatMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_ExistingSession_ReusesSession()
    {
        // Arrange
        var existingSession = new ChatSession
        {
            Id = Guid.NewGuid(),
            SessionId = "existing-session",
            LastActivityAt = DateTime.UtcNow.AddMinutes(-5)
        };

        var command = new SendMessageCommand("¿Cómo me registro?", "existing-session", null, "127.0.0.1");

        _sessionRepoMock.Setup(x => x.GetBySessionIdAsync("existing-session", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSession);

        _sessionRepoMock.Setup(x => x.GetMessagesAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatMessage>());

        _claudeServiceMock.Setup(x => x.SendMessageAsync(
                It.IsAny<string>(), It.IsAny<List<ConversationMessage>>(),
                It.IsAny<string>(), It.IsAny<float>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClaudeSupportResponse(
                "Para registrarte ve a okla.com.do/registro", 150, 80, "end_turn"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.SessionId.Should().Be("existing-session");
        result.DetectedModule.Should().Be("soporte_tecnico");
        _sessionRepoMock.Verify(x => x.CreateAsync(It.IsAny<ChatSession>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("¿Cómo sé si un vendedor es de fiar?", "orientacion_comprador")]
    [InlineData("¿Cómo me registro en OKLA?", "soporte_tecnico")]
    [InlineData("Hola", "conversacional")]
    [InlineData("¿Cómo me registro en OKLA y qué documentos necesito para comprar un carro?", "mixto")]
    [InlineData("El vendedor me pide dinero antes de ver el carro", "orientacion_comprador")]
    [InlineData("¿Cómo activo el 2FA?", "soporte_tecnico")]
    [InlineData("Me rechazaron el KYC", "soporte_tecnico")]
    public async Task Handle_DetectsCorrectModule(string message, string expectedModule)
    {
        // Arrange
        var command = new SendMessageCommand(message, null, null, "127.0.0.1");

        _sessionRepoMock.Setup(x => x.GetBySessionIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChatSession?)null);
        _sessionRepoMock.Setup(x => x.CreateAsync(It.IsAny<ChatSession>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChatSession s, CancellationToken _) => s);
        _sessionRepoMock.Setup(x => x.GetMessagesAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatMessage>());

        _claudeServiceMock.Setup(x => x.SendMessageAsync(
                It.IsAny<string>(), It.IsAny<List<ConversationMessage>>(),
                It.IsAny<string>(), It.IsAny<float>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClaudeSupportResponse("Respuesta", 100, 50, "end_turn"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.DetectedModule.Should().Be(expectedModule);
    }
}
