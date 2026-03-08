using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Application.UseCases.SendAdminAlert;
using NotificationService.Domain.Interfaces;
using Xunit;

namespace NotificationService.Tests.Unit.UseCases;

public class SendAdminAlertCommandHandlerTests
{
    private readonly Mock<IAdminAlertService> _adminAlertServiceMock;
    private readonly Mock<ILogger<SendAdminAlertCommandHandler>> _loggerMock;
    private readonly SendAdminAlertCommandHandler _handler;

    public SendAdminAlertCommandHandlerTests()
    {
        _adminAlertServiceMock = new Mock<IAdminAlertService>();
        _loggerMock = new Mock<ILogger<SendAdminAlertCommandHandler>>();

        _handler = new SendAdminAlertCommandHandler(
            _adminAlertServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidAlert_ReturnsSuccess()
    {
        // Arrange
        var command = new SendAdminAlertCommand(
            AlertType: "security",
            Title: "Intento de acceso no autorizado",
            Message: "Se detectó un intento de acceso sospechoso desde IP 192.168.1.100",
            Severity: "Critical");

        _adminAlertServiceMock
            .Setup(x => x.SendAlertAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Error.Should().BeNull();

        _adminAlertServiceMock.Verify(
            x => x.SendAlertAsync(
                "security",
                "Intento de acceso no autorizado",
                "Se detectó un intento de acceso sospechoso desde IP 192.168.1.100",
                "Critical",
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ServiceThrows_ReturnsFalseWithError()
    {
        // Arrange
        var command = new SendAdminAlertCommand(
            AlertType: "system",
            Title: "Service down",
            Message: "PaymentService is not responding",
            Severity: "Warning");

        _adminAlertServiceMock
            .Setup(x => x.SendAlertAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Teams webhook failed"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Teams webhook failed");
    }

    [Fact]
    public async Task Handle_WithMetadata_PassesMetadataToService()
    {
        // Arrange
        var metadata = new Dictionary<string, string>
        {
            { "service", "AuthService" },
            { "ip", "192.168.1.100" },
            { "userId", "user-456" }
        };

        var command = new SendAdminAlertCommand(
            AlertType: "fraud",
            Title: "Actividad fraudulenta detectada",
            Message: "Múltiples intentos de pago fallidos",
            Severity: "Critical",
            Metadata: metadata);

        Dictionary<string, string>? capturedMetadata = null;

        _adminAlertServiceMock
            .Setup(x => x.SendAlertAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, string, string, Dictionary<string, string>?, CancellationToken>(
                (_, _, _, _, m, _) => capturedMetadata = m)
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedMetadata.Should().NotBeNull();
        capturedMetadata.Should().ContainKey("service");
        capturedMetadata.Should().ContainKey("ip");
        capturedMetadata.Should().ContainKey("userId");
        capturedMetadata!["service"].Should().Be("AuthService");
    }

    [Fact]
    public async Task Handle_DefaultSeverity_UsesInfo()
    {
        // Arrange — command without explicit Severity uses default "Info"
        var command = new SendAdminAlertCommand(
            AlertType: "info",
            Title: "Deployment completado",
            Message: "Nueva versión de VehiclesSaleService desplegada exitosamente");

        _adminAlertServiceMock
            .Setup(x => x.SendAlertAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();

        _adminAlertServiceMock.Verify(
            x => x.SendAlertAsync(
                "info", It.IsAny<string>(), It.IsAny<string>(),
                "Info", // default severity
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
