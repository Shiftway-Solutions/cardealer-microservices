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

public class SavedSearchesControllerTests
{
    private readonly Mock<ISavedSearchRepository> _repositoryMock;
    private readonly Mock<ILogger<SavedSearchesController>> _loggerMock;
    private readonly SavedSearchesController _controller;
    private readonly Guid _testUserId = Guid.NewGuid();

    public SavedSearchesControllerTests()
    {
        _repositoryMock = new Mock<ISavedSearchRepository>();
        _loggerMock = new Mock<ILogger<SavedSearchesController>>();
        _controller = new SavedSearchesController(_loggerMock.Object, _repositoryMock.Object);
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

    private static SavedSearchCriteria CreateTestCriteria() => new(
        Make: "Toyota",
        Model: "Camry",
        MinYear: 2020,
        MaxYear: 2024,
        MinPrice: 20000m,
        MaxPrice: 40000m,
        Location: "Santo Domingo");

    // ========== GetAll ==========

    [Fact]
    public async Task GetAll_WithAuthenticatedUser_ReturnsOkWithPaginatedResults()
    {
        var searches = new List<SavedSearch>
        {
            SavedSearch.Create(_testUserId, "My Toyota Search", CreateTestCriteria()),
            SavedSearch.Create(_testUserId, "Honda Budget Search", CreateTestCriteria())
        };

        _repositoryMock.Setup(r => r.GetByUserIdAsync(_testUserId, 1, 20))
            .ReturnsAsync(searches);
        _repositoryMock.Setup(r => r.GetCountByUserIdAsync(_testUserId))
            .ReturnsAsync(2);

        var result = await _controller.GetAll();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetAll_WithPagination_PassesParametersToRepository()
    {
        _repositoryMock.Setup(r => r.GetByUserIdAsync(_testUserId, 3, 10))
            .ReturnsAsync(new List<SavedSearch>());
        _repositoryMock.Setup(r => r.GetCountByUserIdAsync(_testUserId))
            .ReturnsAsync(0);

        await _controller.GetAll(page: 3, pageSize: 10);

        _repositoryMock.Verify(r => r.GetByUserIdAsync(_testUserId, 3, 10), Times.Once);
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
    public async Task GetById_WithExistingSearch_ReturnsOk()
    {
        var searchId = Guid.NewGuid();
        var search = SavedSearch.Create(_testUserId, "My Search", CreateTestCriteria());

        _repositoryMock.Setup(r => r.GetByIdAndUserAsync(searchId, _testUserId))
            .ReturnsAsync(search);

        var result = await _controller.GetById(searchId);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<SavedSearchResponse>().Subject;
        response.Name.Should().Be("My Search");
        response.UserId.Should().Be(_testUserId);
    }

    [Fact]
    public async Task GetById_WithNonExistingSearch_ReturnsNotFound()
    {
        var searchId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAndUserAsync(searchId, _testUserId))
            .ReturnsAsync((SavedSearch?)null);

        var result = await _controller.GetById(searchId);

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
        var request = new CreateSavedSearchRequest(
            Name: "Toyota SUV Search",
            Criteria: CreateTestCriteria(),
            NotifyOnNewResults: true,
            NotifyByEmail: true,
            NotifyByPush: true,
            NotificationFrequency: "daily");

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<SavedSearch>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.Create(request);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        createdResult.ActionName.Should().Be(nameof(SavedSearchesController.GetById));
    }

    [Fact]
    public async Task Create_SetsUserIdFromClaims()
    {
        var request = new CreateSavedSearchRequest(
            Name: "Test Search",
            Criteria: CreateTestCriteria());

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<SavedSearch>()))
            .Returns(Task.CompletedTask);

        await _controller.Create(request);

        _repositoryMock.Verify(r => r.AddAsync(It.Is<SavedSearch>(s =>
            s.UserId == _testUserId &&
            s.Name == "Test Search"
        )), Times.Once);
    }

    [Fact]
    public async Task Create_WithDefaultNotificationSettings_UsesDefaults()
    {
        var request = new CreateSavedSearchRequest(
            Name: "Minimal Search",
            Criteria: CreateTestCriteria());

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<SavedSearch>()))
            .Returns(Task.CompletedTask);

        await _controller.Create(request);

        _repositoryMock.Verify(r => r.AddAsync(It.Is<SavedSearch>(s =>
            s.NotifyOnNewResults == true &&
            s.NotifyByEmail == true &&
            s.NotifyByPush == true &&
            s.NotificationFrequency == "daily"
        )), Times.Once);
    }

    [Fact]
    public async Task Create_Unauthenticated_ReturnsUnauthorized()
    {
        SetupUnauthenticatedUser();
        var request = new CreateSavedSearchRequest("Test", CreateTestCriteria());

        var result = await _controller.Create(request);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    // ========== Update ==========

    [Fact]
    public async Task Update_WithExistingSearch_ReturnsOk()
    {
        var searchId = Guid.NewGuid();
        var search = SavedSearch.Create(_testUserId, "Old Name", CreateTestCriteria());
        var request = new UpdateSavedSearchRequest(Name: "New Name");

        _repositoryMock.Setup(r => r.GetByIdAndUserAsync(searchId, _testUserId))
            .ReturnsAsync(search);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<SavedSearch>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.Update(searchId, request);

        result.Should().BeOfType<OkObjectResult>();
        search.Name.Should().Be("New Name");
    }

    [Fact]
    public async Task Update_WithNonExistingSearch_ReturnsNotFound()
    {
        var searchId = Guid.NewGuid();
        var request = new UpdateSavedSearchRequest(Name: "New Name");

        _repositoryMock.Setup(r => r.GetByIdAndUserAsync(searchId, _testUserId))
            .ReturnsAsync((SavedSearch?)null);

        var result = await _controller.Update(searchId, request);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Update_SetsNotificationFrequency_UpdatesCorrectly()
    {
        var searchId = Guid.NewGuid();
        var search = SavedSearch.Create(_testUserId, "Test", CreateTestCriteria());
        var request = new UpdateSavedSearchRequest(
            NotifyByEmail: false,
            NotificationFrequency: "weekly");

        _repositoryMock.Setup(r => r.GetByIdAndUserAsync(searchId, _testUserId))
            .ReturnsAsync(search);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<SavedSearch>()))
            .Returns(Task.CompletedTask);

        await _controller.Update(searchId, request);

        search.NotifyByEmail.Should().BeFalse();
        search.NotificationFrequency.Should().Be("weekly");
        search.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Update_Unauthenticated_ReturnsUnauthorized()
    {
        SetupUnauthenticatedUser();
        var request = new UpdateSavedSearchRequest(Name: "Test");

        var result = await _controller.Update(Guid.NewGuid(), request);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    // ========== Delete ==========

    [Fact]
    public async Task Delete_WithExistingSearch_ReturnsNoContent()
    {
        var searchId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.ExistsAsync(searchId, _testUserId)).ReturnsAsync(true);
        _repositoryMock.Setup(r => r.DeleteAsync(searchId)).Returns(Task.CompletedTask);

        var result = await _controller.Delete(searchId);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_WithNonExistingSearch_ReturnsNotFound()
    {
        var searchId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.ExistsAsync(searchId, _testUserId)).ReturnsAsync(false);

        var result = await _controller.Delete(searchId);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_Unauthenticated_ReturnsUnauthorized()
    {
        SetupUnauthenticatedUser();

        var result = await _controller.Delete(Guid.NewGuid());

        result.Should().BeOfType<UnauthorizedResult>();
    }

    // ========== Ownership Isolation ==========

    [Fact]
    public async Task GetById_OtherUsersSearch_ReturnsNotFound()
    {
        var searchId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAndUserAsync(searchId, _testUserId))
            .ReturnsAsync((SavedSearch?)null);

        var result = await _controller.GetById(searchId);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_OtherUsersSearch_ReturnsNotFound()
    {
        var searchId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.ExistsAsync(searchId, _testUserId)).ReturnsAsync(false);

        var result = await _controller.Delete(searchId);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ========== MapToResponse / JSON Handling ==========

    [Fact]
    public async Task GetById_WithValidCriteriaJson_DeserializesCriteria()
    {
        var searchId = Guid.NewGuid();
        var search = SavedSearch.Create(_testUserId, "Test", CreateTestCriteria());

        _repositoryMock.Setup(r => r.GetByIdAndUserAsync(searchId, _testUserId))
            .ReturnsAsync(search);

        var result = await _controller.GetById(searchId);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<SavedSearchResponse>().Subject;
        response.Criteria.Should().NotBeNull();
    }
}
