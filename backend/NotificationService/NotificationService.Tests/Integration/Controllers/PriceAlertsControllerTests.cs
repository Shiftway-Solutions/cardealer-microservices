using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Api.Controllers;
using NotificationService.Application.DTOs;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces.Repositories;
using System.Security.Claims;
using Xunit;

namespace NotificationService.Tests.Integration.Controllers;

public class PriceAlertsControllerTests
{
    private readonly Mock<IPriceAlertRepository> _repositoryMock;
    private readonly Mock<ILogger<PriceAlertsController>> _loggerMock;
    private readonly PriceAlertsController _controller;
    private readonly Guid _testUserId = Guid.NewGuid();

    public PriceAlertsControllerTests()
    {
        _repositoryMock = new Mock<IPriceAlertRepository>();
        _loggerMock = new Mock<ILogger<PriceAlertsController>>();
        _controller = new PriceAlertsController(_loggerMock.Object, _repositoryMock.Object);
        SetupAuthenticatedUser(_testUserId);
    }

    private void SetupAuthenticatedUser(Guid userId)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("sub", userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };
    }

    private void SetupUnauthenticatedUser()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };
    }

    // ========== GetAll ==========

    [Fact]
    public async Task GetAll_WithAuthenticatedUser_ReturnsOkWithPaginatedResults()
    {
        var alerts = new List<PriceAlert>
        {
            PriceAlert.Create(_testUserId, Guid.NewGuid(), "Toyota Camry 2024", 35000m, 30000m),
            PriceAlert.Create(_testUserId, Guid.NewGuid(), "Honda Civic 2023", 28000m, 25000m)
        };

        _repositoryMock.Setup(r => r.GetByUserIdAsync(_testUserId, null, 1, 20))
            .ReturnsAsync(alerts);
        _repositoryMock.Setup(r => r.GetCountByUserIdAsync(_testUserId, null))
            .ReturnsAsync(2);

        var result = await _controller.GetAll();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetAll_WithIsActiveFilter_PassesFilterToRepository()
    {
        _repositoryMock.Setup(r => r.GetByUserIdAsync(_testUserId, true, 1, 20))
            .ReturnsAsync(new List<PriceAlert>());
        _repositoryMock.Setup(r => r.GetCountByUserIdAsync(_testUserId, true))
            .ReturnsAsync(0);

        await _controller.GetAll(isActive: true);

        _repositoryMock.Verify(r => r.GetByUserIdAsync(_testUserId, true, 1, 20), Times.Once);
    }

    [Fact]
    public async Task GetAll_Unauthenticated_ReturnsUnauthorized()
    {
        SetupUnauthenticatedUser();

        var result = await _controller.GetAll();

        result.Should().BeOfType<UnauthorizedResult>();
    }

    // ========== GetById ==========

    [Fact]
    public async Task GetById_WithExistingAlert_ReturnsOk()
    {
        var alertId = Guid.NewGuid();
        var alert = PriceAlert.Create(_testUserId, Guid.NewGuid(), "Test Car", 30000m, 25000m);

        _repositoryMock.Setup(r => r.GetByIdAndUserAsync(alertId, _testUserId))
            .ReturnsAsync(alert);

        var result = await _controller.GetById(alertId);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_WithNonExistingAlert_ReturnsNotFound()
    {
        var alertId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAndUserAsync(alertId, _testUserId))
            .ReturnsAsync((PriceAlert?)null);

        var result = await _controller.GetById(alertId);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetById_Unauthenticated_ReturnsUnauthorized()
    {
        SetupUnauthenticatedUser();

        var result = await _controller.GetById(Guid.NewGuid());

        result.Should().BeOfType<UnauthorizedResult>();
    }

    // ========== Create ==========

    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreatedAtAction()
    {
        var request = new CreatePriceAlertRequest(
            VehicleId: Guid.NewGuid(),
            VehicleTitle: "Toyota Camry 2024",
            CurrentPrice: 35000m,
            TargetPrice: 30000m,
            PriceDropPercentage: 15m,
            NotifyByEmail: true,
            NotifyByPush: true,
            NotifyBySms: false);

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<PriceAlert>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.Create(request);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        createdResult.ActionName.Should().Be(nameof(PriceAlertsController.GetById));
    }

    [Fact]
    public async Task Create_CallsRepositoryAdd()
    {
        var request = new CreatePriceAlertRequest(
            VehicleId: Guid.NewGuid(),
            VehicleTitle: "Test",
            CurrentPrice: 30000m,
            TargetPrice: 25000m);

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<PriceAlert>()))
            .Returns(Task.CompletedTask);

        await _controller.Create(request);

        _repositoryMock.Verify(r => r.AddAsync(It.Is<PriceAlert>(a =>
            a.UserId == _testUserId &&
            a.TargetPrice == 25000m
        )), Times.Once);
    }

    [Fact]
    public async Task Create_Unauthenticated_ReturnsUnauthorized()
    {
        SetupUnauthenticatedUser();
        var request = new CreatePriceAlertRequest(Guid.NewGuid(), "Test", 30000m, 25000m);

        var result = await _controller.Create(request);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    // ========== Update ==========

    [Fact]
    public async Task Update_WithExistingAlert_ReturnsOk()
    {
        var alertId = Guid.NewGuid();
        var alert = PriceAlert.Create(_testUserId, Guid.NewGuid(), "Test", 30000m, 25000m);
        var request = new UpdatePriceAlertRequest(TargetPrice: 22000m);

        _repositoryMock.Setup(r => r.GetByIdAndUserAsync(alertId, _testUserId))
            .ReturnsAsync(alert);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<PriceAlert>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.Update(alertId, request);

        result.Should().BeOfType<OkObjectResult>();
        alert.TargetPrice.Should().Be(22000m);
    }

    [Fact]
    public async Task Update_WithNonExistingAlert_ReturnsNotFound()
    {
        var alertId = Guid.NewGuid();
        var request = new UpdatePriceAlertRequest(TargetPrice: 22000m);

        _repositoryMock.Setup(r => r.GetByIdAndUserAsync(alertId, _testUserId))
            .ReturnsAsync((PriceAlert?)null);

        var result = await _controller.Update(alertId, request);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Update_SetsIsActive_UpdatesCorrectly()
    {
        var alertId = Guid.NewGuid();
        var alert = PriceAlert.Create(_testUserId, Guid.NewGuid(), "Test", 30000m, 25000m);
        var request = new UpdatePriceAlertRequest(IsActive: false);

        _repositoryMock.Setup(r => r.GetByIdAndUserAsync(alertId, _testUserId))
            .ReturnsAsync(alert);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<PriceAlert>()))
            .Returns(Task.CompletedTask);

        await _controller.Update(alertId, request);

        alert.IsActive.Should().BeFalse();
        alert.UpdatedAt.Should().NotBeNull();
    }

    // ========== Delete ==========

    [Fact]
    public async Task Delete_WithExistingAlert_ReturnsNoContent()
    {
        var alertId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.ExistsAsync(alertId, _testUserId)).ReturnsAsync(true);
        _repositoryMock.Setup(r => r.DeleteAsync(alertId)).Returns(Task.CompletedTask);

        var result = await _controller.Delete(alertId);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_WithNonExistingAlert_ReturnsNotFound()
    {
        var alertId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.ExistsAsync(alertId, _testUserId)).ReturnsAsync(false);

        var result = await _controller.Delete(alertId);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_Unauthenticated_ReturnsUnauthorized()
    {
        SetupUnauthenticatedUser();

        var result = await _controller.Delete(Guid.NewGuid());

        result.Should().BeOfType<UnauthorizedResult>();
    }

    // ========== Ownership checks ==========

    [Fact]
    public async Task GetById_OtherUsersAlert_ReturnsNotFound()
    {
        var alertId = Guid.NewGuid();
        // Repository returns null for mismatched user (ownership check in repo)
        _repositoryMock.Setup(r => r.GetByIdAndUserAsync(alertId, _testUserId))
            .ReturnsAsync((PriceAlert?)null);

        var result = await _controller.GetById(alertId);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
