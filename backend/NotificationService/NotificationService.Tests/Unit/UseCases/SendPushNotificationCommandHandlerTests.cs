using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Application.DTOs;
using NotificationService.Application.UseCases.SendPushNotification;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Interfaces.External;
using NotificationService.Domain.Interfaces.Repositories;
using NotificationService.Application.Interfaces;
using ErrorService.Shared.Exceptions;
using Xunit;

namespace NotificationService.Tests.Unit.UseCases;

public class SendPushNotificationCommandHandlerTests
{
    private readonly Mock<INotificationRepository> _notificationRepositoryMock;
    private readonly Mock<INotificationLogRepository> _logRepositoryMock;
    private readonly Mock<IPushNotificationProvider> _pushProviderMock;
    private readonly Mock<IConfigurationServiceClient> _configClientMock;
    private readonly Mock<ILogger<SendPushNotificationCommandHandler>> _loggerMock;
    private readonly SendPushNotificationCommandHandler _handler;

    public SendPushNotificationCommandHandlerTests()
    {
        _notificationRepositoryMock = new Mock<INotificationRepository>();
        _logRepositoryMock = new Mock<INotificationLogRepository>();
        _pushProviderMock = new Mock<IPushNotificationProvider>();
        _configClientMock = new Mock<IConfigurationServiceClient>();
        _loggerMock = new Mock<ILogger<SendPushNotificationCommandHandler>>();

        _pushProviderMock.Setup(x => x.ProviderName).Returns("Firebase");
        _configClientMock.Setup(x => x.IsEnabledAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _handler = new SendPushNotificationCommandHandler(
            _notificationRepositoryMock.Object,
            _logRepositoryMock.Object,
            _pushProviderMock.Object,
            _configClientMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidPushNotification_SendsSuccessfully()
    {
        // Arrange
        var request = new SendPushNotificationRequest(
            DeviceToken: "device-token-abc123",
            Title: "New Message",
            Body: "You have a new message");

        var command = new SendPushNotificationCommand(request);

        _pushProviderMock
            .Setup(x => x.SendAsync(
                request.DeviceToken,
                request.Title,
                request.Body,
                It.IsAny<object?>(),
                It.IsAny<Dictionary<string, object>?>()))
            .ReturnsAsync((true, "push-msg-123", null));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(NotificationStatus.Sent.ToString());
        result.Message.Should().Contain("successfully");

        _notificationRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Notification>()),
            Times.Once);

        _logRepositoryMock.Verify(
            x => x.AddAsync(It.Is<NotificationLog>(log => log.Action == "SENT")),
            Times.Once);
    }

    [Fact]
    public async Task Handle_PushProviderFails_ThrowsServiceUnavailableException()
    {
        // Arrange
        var request = new SendPushNotificationRequest(
            DeviceToken: "invalid-token",
            Title: "Test",
            Body: "Test Body");

        var command = new SendPushNotificationCommand(request);
        var errorMessage = "Invalid device token";

        _pushProviderMock
            .Setup(x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object?>(),
                It.IsAny<Dictionary<string, object>?>()))
            .ReturnsAsync((false, null, errorMessage));

        // Act & Assert
        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ServiceUnavailableException>()
            .WithMessage($"*{errorMessage}*");

        _logRepositoryMock.Verify(
            x => x.AddAsync(It.Is<NotificationLog>(log => log.Action == "FAILED")),
            Times.Once);
    }

    [Fact]
    public async Task Handle_CreatesNotificationWithCorrectType()
    {
        // Arrange
        var request = new SendPushNotificationRequest(
            DeviceToken: "device-token",
            Title: "Alert",
            Body: "Important notification");

        var command = new SendPushNotificationCommand(request);
        Notification? capturedNotification = null;

        _notificationRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Notification>()))
            .Callback<Notification>(n => capturedNotification = n)
            .Returns(Task.CompletedTask);

        _pushProviderMock
            .Setup(x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object?>(),
                It.IsAny<Dictionary<string, object>?>()))
            .ReturnsAsync((true, "msg-123", null));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedNotification.Should().NotBeNull();
        capturedNotification!.Type.Should().Be(NotificationType.Push);
        capturedNotification.Recipient.Should().Be(request.DeviceToken);
        capturedNotification.Subject.Should().Be(request.Title);
        capturedNotification.Content.Should().Be(request.Body);
    }

    [Fact]
    public async Task Handle_WithDataPayload_PassesDataToProvider()
    {
        // Arrange
        var data = new { action = "open_screen", screenId = "orders" };

        var request = new SendPushNotificationRequest(
            DeviceToken: "device-token",
            Title: "Order Update",
            Body: "Your order is ready",
            Data: data);

        var command = new SendPushNotificationCommand(request);
        object? capturedData = null;

        _pushProviderMock
            .Setup(x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object?>(),
                It.IsAny<Dictionary<string, object>?>()))
            .Callback<string, string, string, object?, Dictionary<string, object>?>(
                (_, _, _, d, _) => capturedData = d)
            .ReturnsAsync((true, "msg-123", null));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedData.Should().NotBeNull();
        capturedData.Should().BeEquivalentTo(data);
    }

    [Fact]
    public async Task Handle_WithMetadata_PassesMetadataToProvider()
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            { "userId", "user-123" },
            { "campaignId", "camp-456" }
        };

        var request = new SendPushNotificationRequest(
            DeviceToken: "device-token",
            Title: "Promo",
            Body: "Special offer for you",
            Metadata: metadata);

        var command = new SendPushNotificationCommand(request);
        Dictionary<string, object>? capturedMetadata = null;

        _pushProviderMock
            .Setup(x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object?>(),
                It.IsAny<Dictionary<string, object>?>()))
            .Callback<string, string, string, object?, Dictionary<string, object>?>(
                (_, _, _, _, m) => capturedMetadata = m)
            .ReturnsAsync((true, "msg-123", null));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedMetadata.Should().NotBeNull();
        capturedMetadata.Should().ContainKey("userId");
        capturedMetadata.Should().ContainKey("campaignId");
    }

    [Fact]
    public async Task Handle_ProviderThrowsException_ThrowsServiceUnavailableException()
    {
        // Arrange
        var request = new SendPushNotificationRequest(
            DeviceToken: "device-token",
            Title: "Test",
            Body: "Test Body");

        var command = new SendPushNotificationCommand(request);

        _pushProviderMock
            .Setup(x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object?>(),
                It.IsAny<Dictionary<string, object>?>()))
            .ThrowsAsync(new Exception("Firebase connection timeout"));

        // Act & Assert
        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ServiceUnavailableException>()
            .WithMessage("*unexpected error*");
    }

    [Fact]
    public async Task Handle_SuccessfulSend_UpdatesNotificationStatus()
    {
        // Arrange
        var request = new SendPushNotificationRequest(
            DeviceToken: "device-token",
            Title: "Test",
            Body: "Test Body");

        var command = new SendPushNotificationCommand(request);
        Notification? updatedNotification = null;

        _notificationRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Notification>()))
            .Callback<Notification>(n => updatedNotification = n)
            .Returns(Task.CompletedTask);

        _pushProviderMock
            .Setup(x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object?>(),
                It.IsAny<Dictionary<string, object>?>()))
            .ReturnsAsync((true, "msg-123", null));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _notificationRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<Notification>()),
            Times.Once);

        updatedNotification.Should().NotBeNull();
        updatedNotification!.Status.Should().Be(NotificationStatus.Sent);
        updatedNotification.SentAt.Should().NotBeNull();
    }
}
