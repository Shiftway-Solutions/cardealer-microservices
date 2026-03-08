using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Application.Interfaces;
using NotificationService.Application.UseCases.SendWhatsAppNotification;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Interfaces.External;
using NotificationService.Domain.Interfaces.Repositories;
using Xunit;

namespace NotificationService.Tests.Unit.UseCases;

public class SendWhatsAppNotificationCommandHandlerTests
{
    private readonly Mock<IConfigurationServiceClient> _configClientMock;
    private readonly Mock<IWhatsAppProvider> _whatsAppProviderMock;
    private readonly Mock<INotificationRepository> _notificationRepoMock;
    private readonly Mock<INotificationLogRepository> _logRepoMock;
    private readonly Mock<ILogger<SendWhatsAppNotificationCommandHandler>> _loggerMock;
    private readonly SendWhatsAppNotificationCommandHandler _handler;

    public SendWhatsAppNotificationCommandHandlerTests()
    {
        _configClientMock = new Mock<IConfigurationServiceClient>();
        _whatsAppProviderMock = new Mock<IWhatsAppProvider>();
        _notificationRepoMock = new Mock<INotificationRepository>();
        _logRepoMock = new Mock<INotificationLogRepository>();
        _loggerMock = new Mock<ILogger<SendWhatsAppNotificationCommandHandler>>();

        _whatsAppProviderMock.Setup(x => x.ProviderName).Returns("TwilioWhatsApp");
        _configClientMock.Setup(x => x.IsEnabledAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _handler = new SendWhatsAppNotificationCommandHandler(
            _configClientMock.Object,
            _whatsAppProviderMock.Object,
            _notificationRepoMock.Object,
            _logRepoMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_FreeFormMessage_SendsSuccessfully()
    {
        // Arrange
        var command = new SendWhatsAppNotificationCommand(
            To: "+18095551234",
            Message: "Hola, tu vehículo ha sido aprobado en OKLA");

        _whatsAppProviderMock
            .Setup(x => x.SendMessageAsync(
                command.To,
                command.Message!,
                It.IsAny<Dictionary<string, object>?>()))
            .ReturnsAsync((true, "wa-msg-123", (string?)null));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.MessageId.Should().Be("wa-msg-123");
        result.Provider.Should().Be("TwilioWhatsApp");

        _whatsAppProviderMock.Verify(
            x => x.SendMessageAsync(command.To, command.Message!, It.IsAny<Dictionary<string, object>?>()), 
            Times.Once);
        _whatsAppProviderMock.Verify(
            x => x.SendTemplateAsync(It.IsAny<string>(), It.IsAny<string>(), 
                It.IsAny<Dictionary<string, string>?>(), It.IsAny<string?>(), 
                It.IsAny<Dictionary<string, object>?>()), 
            Times.Never);
    }

    [Fact]
    public async Task Handle_TemplateMessage_SendsViaTemplate()
    {
        // Arrange
        var templateParams = new Dictionary<string, string>
        {
            { "vehicleName", "Toyota Camry 2022" },
            { "price", "RD$1,200,000" }
        };

        var command = new SendWhatsAppNotificationCommand(
            To: "+18095551234",
            TemplateName: "vehicle_approved",
            TemplateParameters: templateParams,
            LanguageCode: "es");

        _whatsAppProviderMock
            .Setup(x => x.SendTemplateAsync(
                command.To,
                "vehicle_approved",
                It.IsAny<Dictionary<string, string>?>(),
                "es",
                It.IsAny<Dictionary<string, object>?>()))
            .ReturnsAsync((true, "wa-tmpl-456", (string?)null));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.MessageId.Should().Be("wa-tmpl-456");

        _whatsAppProviderMock.Verify(
            x => x.SendTemplateAsync(
                command.To, "vehicle_approved",
                It.IsAny<Dictionary<string, string>?>(),
                "es",
                It.IsAny<Dictionary<string, object>?>()), 
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhatsAppDisabled_ReturnsFalseWithError()
    {
        // Arrange
        _configClientMock.Setup(x => x.IsEnabledAsync("whatsapp.enabled", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new SendWhatsAppNotificationCommand(
            To: "+18095551234",
            Message: "Test message");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("disabled");

        _whatsAppProviderMock.Verify(
            x => x.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>(), 
                It.IsAny<Dictionary<string, object>?>()), 
            Times.Never);
        _notificationRepoMock.Verify(
            x => x.AddAsync(It.IsAny<Notification>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ProviderFails_ReturnsFalseAndLogsFailure()
    {
        // Arrange
        var command = new SendWhatsAppNotificationCommand(
            To: "+18095551234",
            Message: "Test message");

        _whatsAppProviderMock
            .Setup(x => x.SendMessageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>?>()))
            .ReturnsAsync((false, (string?)null, "WhatsApp API rate limit exceeded"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("rate limit");

        _logRepoMock.Verify(
            x => x.AddAsync(It.Is<NotificationLog>(log => log.Action == "FAILED")), Times.Once);
    }

    [Fact]
    public async Task Handle_SuccessfulSend_CreatesNotificationWithWhatsAppType()
    {
        // Arrange
        var command = new SendWhatsAppNotificationCommand(
            To: "+18095551234",
            Message: "Mensaje de prueba");

        Notification? capturedNotification = null;

        _notificationRepoMock
            .Setup(x => x.AddAsync(It.IsAny<Notification>()))
            .Callback<Notification>(n => capturedNotification = n)
            .Returns(Task.CompletedTask);

        _whatsAppProviderMock
            .Setup(x => x.SendMessageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>?>()))
            .ReturnsAsync((true, "wa-789", (string?)null));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedNotification.Should().NotBeNull();
        capturedNotification!.Type.Should().Be(NotificationType.WhatsApp);
        capturedNotification.Recipient.Should().Be("+18095551234");
        capturedNotification.Provider.Should().Be(NotificationProvider.TwilioWhatsApp);
    }

    [Fact]
    public async Task Handle_SuccessfulSend_LogsSentAction()
    {
        // Arrange
        var command = new SendWhatsAppNotificationCommand(
            To: "+18095551234",
            Message: "Test");

        _whatsAppProviderMock
            .Setup(x => x.SendMessageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>?>()))
            .ReturnsAsync((true, "wa-log-test", (string?)null));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _logRepoMock.Verify(
            x => x.AddAsync(It.Is<NotificationLog>(log =>
                log.Action == "SENT" &&
                log.ProviderMessageId == "wa-log-test")),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithMetadata_PassesMetadataThrough()
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            { "dealerId", "dealer-123" },
            { "source", "admin-panel" }
        };

        var command = new SendWhatsAppNotificationCommand(
            To: "+18095551234",
            Message: "Test",
            Metadata: metadata);

        Notification? capturedNotification = null;

        _notificationRepoMock
            .Setup(x => x.AddAsync(It.IsAny<Notification>()))
            .Callback<Notification>(n => capturedNotification = n)
            .Returns(Task.CompletedTask);

        _whatsAppProviderMock
            .Setup(x => x.SendMessageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>?>()))
            .ReturnsAsync((true, "wa-meta-test", (string?)null));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedNotification.Should().NotBeNull();
        capturedNotification!.Metadata.Should().ContainKey("dealerId");
        capturedNotification.Metadata.Should().ContainKey("source");
    }
}
