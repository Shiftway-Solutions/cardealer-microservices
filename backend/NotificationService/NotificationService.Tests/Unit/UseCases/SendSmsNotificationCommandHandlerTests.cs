using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Application.DTOs;
using NotificationService.Application.Interfaces;
using NotificationService.Application.UseCases.SendSmsNotification;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces.External;
using NotificationService.Domain.Interfaces.Repositories;
using ErrorService.Shared.Exceptions;
using Xunit;

namespace NotificationService.Tests.Unit.UseCases;

public class SendSmsNotificationCommandHandlerTests
{
    private readonly Mock<INotificationRepository> _notificationRepoMock;
    private readonly Mock<INotificationLogRepository> _logRepoMock;
    private readonly Mock<ISmsProvider> _smsProviderMock;
    private readonly Mock<IConfigurationServiceClient> _configClientMock;
    private readonly Mock<ILogger<SendSmsNotificationCommandHandler>> _loggerMock;
    private readonly SendSmsNotificationCommandHandler _handler;

    public SendSmsNotificationCommandHandlerTests()
    {
        _notificationRepoMock = new Mock<INotificationRepository>();
        _logRepoMock = new Mock<INotificationLogRepository>();
        _smsProviderMock = new Mock<ISmsProvider>();
        _configClientMock = new Mock<IConfigurationServiceClient>();
        _loggerMock = new Mock<ILogger<SendSmsNotificationCommandHandler>>();

        _smsProviderMock.Setup(x => x.ProviderName).Returns("TwilioSms");
        _configClientMock.Setup(x => x.IsEnabledAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _handler = new SendSmsNotificationCommandHandler(
            _notificationRepoMock.Object,
            _logRepoMock.Object,
            _smsProviderMock.Object,
            _configClientMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidSms_SendsSuccessfully()
    {
        // Arrange
        var request = new SendSmsNotificationRequest(
            To: "+18095551234",
            Message: "Tu vehículo ha sido aprobado en OKLA");

        var command = new SendSmsNotificationCommand(request);

        _smsProviderMock
            .Setup(x => x.SendAsync(
                request.To,
                request.Message,
                It.IsAny<Dictionary<string, object>?>()))
            .ReturnsAsync((true, "sms-msg-123", (string?)null));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("Sent");
        result.Message.Should().Contain("successfully");

        _notificationRepoMock.Verify(
            x => x.AddAsync(It.IsAny<Notification>()), Times.Once);
        _logRepoMock.Verify(
            x => x.AddAsync(It.Is<NotificationLog>(log => log.Action == "SENT")), Times.Once);
    }

    [Fact]
    public async Task Handle_SmsDisabled_ReturnsSkipped()
    {
        // Arrange
        _configClientMock.Setup(x => x.IsEnabledAsync("sms.enabled", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var request = new SendSmsNotificationRequest(
            To: "+18095551234",
            Message: "Test message");

        var command = new SendSmsNotificationCommand(request);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("Skipped");
        result.Message.Should().Contain("disabled");

        _smsProviderMock.Verify(
            x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>?>()), 
            Times.Never);
        _notificationRepoMock.Verify(
            x => x.AddAsync(It.IsAny<Notification>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ProviderFails_ThrowsServiceUnavailableException()
    {
        // Arrange
        var request = new SendSmsNotificationRequest(
            To: "+18095551234",
            Message: "Test SMS");

        var command = new SendSmsNotificationCommand(request);
        var errorMessage = "SMS gateway timeout";

        _smsProviderMock
            .Setup(x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>?>()))
            .ReturnsAsync((false, (string?)null, errorMessage));

        // Act & Assert
        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ServiceUnavailableException>()
            .WithMessage($"*{errorMessage}*");

        _logRepoMock.Verify(
            x => x.AddAsync(It.Is<NotificationLog>(log => log.Action == "FAILED")), Times.Once);
    }

    [Fact]
    public async Task Handle_ProviderThrowsException_ThrowsServiceUnavailableException()
    {
        // Arrange
        var request = new SendSmsNotificationRequest(
            To: "+18095551234",
            Message: "Test SMS");

        var command = new SendSmsNotificationCommand(request);

        _smsProviderMock
            .Setup(x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>?>()))
            .ThrowsAsync(new Exception("Provider crashed"));

        // Act & Assert
        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ServiceUnavailableException>()
            .WithMessage("*unexpected error*");
    }

    [Fact]
    public async Task Handle_ValidSms_CreatesNotificationWithSmsType()
    {
        // Arrange
        var request = new SendSmsNotificationRequest(
            To: "+18095559876",
            Message: "Verificación OKLA: Tu código es 123456");

        var command = new SendSmsNotificationCommand(request);
        Notification? capturedNotification = null;

        _notificationRepoMock
            .Setup(x => x.AddAsync(It.IsAny<Notification>()))
            .Callback<Notification>(n => capturedNotification = n)
            .Returns(Task.CompletedTask);

        _smsProviderMock
            .Setup(x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>?>()))
            .ReturnsAsync((true, "sms-456", (string?)null));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedNotification.Should().NotBeNull();
        capturedNotification!.Type.Should().Be(NotificationService.Domain.Enums.NotificationType.Sms);
        capturedNotification.Recipient.Should().Be(request.To);
        capturedNotification.Content.Should().Be(request.Message);
    }

    [Fact]
    public async Task Handle_WithMetadata_PassesMetadataToProvider()
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            { "vehicleId", "veh-123" },
            { "action", "approval" }
        };

        var request = new SendSmsNotificationRequest(
            To: "+18095551234",
            Message: "Tu vehículo fue aprobado",
            Metadata: metadata);

        var command = new SendSmsNotificationCommand(request);
        Dictionary<string, object>? capturedMetadata = null;

        _smsProviderMock
            .Setup(x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>?>()))
            .Callback<string, string, Dictionary<string, object>?>(
                (_, _, m) => capturedMetadata = m)
            .ReturnsAsync((true, "sms-789", (string?)null));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedMetadata.Should().NotBeNull();
        capturedMetadata.Should().ContainKey("vehicleId");
        capturedMetadata.Should().ContainKey("action");
    }
}
