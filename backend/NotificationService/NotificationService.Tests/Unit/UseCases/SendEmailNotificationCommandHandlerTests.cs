using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Application.DTOs;
using NotificationService.Application.UseCases.SendEmailNotification;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Interfaces.External;
using NotificationService.Domain.Interfaces.Repositories;
using NotificationService.Application.Interfaces;
using ErrorService.Shared.Exceptions;
using Xunit;

namespace NotificationService.Tests.Unit.UseCases;

public class SendEmailNotificationCommandHandlerTests
{
    private readonly Mock<INotificationRepository> _notificationRepositoryMock;
    private readonly Mock<INotificationLogRepository> _logRepositoryMock;
    private readonly Mock<IEmailProvider> _emailProviderMock;
    private readonly Mock<IConfigurationServiceClient> _configClientMock;
    private readonly Mock<ILogger<SendEmailNotificationCommandHandler>> _loggerMock;
    private readonly SendEmailNotificationCommandHandler _handler;

    public SendEmailNotificationCommandHandlerTests()
    {
        _notificationRepositoryMock = new Mock<INotificationRepository>();
        _logRepositoryMock = new Mock<INotificationLogRepository>();
        _emailProviderMock = new Mock<IEmailProvider>();
        _configClientMock = new Mock<IConfigurationServiceClient>();
        _loggerMock = new Mock<ILogger<SendEmailNotificationCommandHandler>>();

        _emailProviderMock.Setup(x => x.ProviderName).Returns("TestProvider");
        _configClientMock.Setup(x => x.IsEnabledAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _handler = new SendEmailNotificationCommandHandler(
            _notificationRepositoryMock.Object,
            _logRepositoryMock.Object,
            _emailProviderMock.Object,
            _configClientMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidEmail_SendsSuccessfully()
    {
        // Arrange
        var request = new SendEmailNotificationRequest(
            To: "user@example.com",
            Subject: "Test Subject",
            Body: "<p>Test Body</p>",
            IsHtml: true);

        var command = new SendEmailNotificationCommand(request);

        _emailProviderMock
            .Setup(x => x.SendAsync(
                request.To,
                request.Subject,
                request.Body,
                request.IsHtml,
                It.IsAny<Dictionary<string, object>?>()))
            .ReturnsAsync((true, "msg-123", null));

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
    public async Task Handle_EmailProviderFails_ThrowsServiceUnavailableException()
    {
        // Arrange
        var request = new SendEmailNotificationRequest(
            To: "user@example.com",
            Subject: "Test Subject",
            Body: "Test Body");

        var command = new SendEmailNotificationCommand(request);
        var errorMessage = "SMTP connection failed";

        _emailProviderMock
            .Setup(x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
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
    public async Task Handle_ValidEmail_CreatesNotificationWithCorrectType()
    {
        // Arrange
        var request = new SendEmailNotificationRequest(
            To: "user@example.com",
            Subject: "Test Subject",
            Body: "Test Body");

        var command = new SendEmailNotificationCommand(request);
        Notification? capturedNotification = null;

        _notificationRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Notification>()))
            .Callback<Notification>(n => capturedNotification = n)
            .Returns(Task.CompletedTask);

        _emailProviderMock
            .Setup(x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<Dictionary<string, object>?>()))
            .ReturnsAsync((true, "msg-123", null));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedNotification.Should().NotBeNull();
        capturedNotification!.Type.Should().Be(NotificationType.Email);
        capturedNotification.Recipient.Should().Be(request.To);
        capturedNotification.Subject.Should().Be(request.Subject);
        capturedNotification.Content.Should().Be(request.Body);
    }

    [Fact]
    public async Task Handle_WithMetadata_PassesMetadataToProvider()
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            { "userId", "user-123" },
            { "orderId", "order-456" }
        };

        var request = new SendEmailNotificationRequest(
            To: "user@example.com",
            Subject: "Order Confirmation",
            Body: "Your order is confirmed",
            Metadata: metadata);

        var command = new SendEmailNotificationCommand(request);
        Dictionary<string, object>? capturedMetadata = null;

        _emailProviderMock
            .Setup(x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<Dictionary<string, object>?>()))
            .Callback<string, string, string, bool, Dictionary<string, object>?>(
                (_, _, _, _, m) => capturedMetadata = m)
            .ReturnsAsync((true, "msg-123", null));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedMetadata.Should().NotBeNull();
        capturedMetadata.Should().ContainKey("userId");
        capturedMetadata.Should().ContainKey("orderId");
    }

    [Fact]
    public async Task Handle_ProviderThrowsException_ThrowsServiceUnavailableException()
    {
        // Arrange
        var request = new SendEmailNotificationRequest(
            To: "user@example.com",
            Subject: "Test Subject",
            Body: "Test Body");

        var command = new SendEmailNotificationCommand(request);

        _emailProviderMock
            .Setup(x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<Dictionary<string, object>?>()))
            .ThrowsAsync(new Exception("Provider crashed"));

        // Act & Assert
        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ServiceUnavailableException>()
            .WithMessage("*unexpected error*");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_IsHtmlParameter_PassedCorrectlyToProvider(bool isHtml)
    {
        // Arrange
        var request = new SendEmailNotificationRequest(
            To: "user@example.com",
            Subject: "Test",
            Body: isHtml ? "<p>HTML</p>" : "Plain text",
            IsHtml: isHtml);

        var command = new SendEmailNotificationCommand(request);
        bool capturedIsHtml = false;

        _emailProviderMock
            .Setup(x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<Dictionary<string, object>?>()))
            .Callback<string, string, string, bool, Dictionary<string, object>?>(
                (_, _, _, html, _) => capturedIsHtml = html)
            .ReturnsAsync((true, "msg-123", null));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedIsHtml.Should().Be(isHtml);
    }
}
