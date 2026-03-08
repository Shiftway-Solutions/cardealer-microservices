using ErrorService.Application.DTOs;
using ErrorService.Application.UseCases.GetError;
using ErrorService.Application.UseCases.GetErrors;
using ErrorService.Application.UseCases.GetErrorStats;
using ErrorService.Application.UseCases.GetServiceNames;
using ErrorService.Domain.Entities;
using ErrorService.Domain.Interfaces;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ErrorService.Tests.Application.UseCases;

public class GetErrorQueryHandlerTests
{
    private readonly Mock<IErrorLogRepository> _repoMock;
    private readonly GetErrorQueryHandler _handler;

    public GetErrorQueryHandlerTests()
    {
        _repoMock = new Mock<IErrorLogRepository>();
        _handler = new GetErrorQueryHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingError_ReturnsErrorResponse()
    {
        // Arrange
        var errorId = Guid.NewGuid();
        var errorLog = new ErrorLog
        {
            Id = errorId,
            ServiceName = "AuthService",
            ExceptionType = "NullReferenceException",
            Message = "Object reference not set to an instance of an object",
            StackTrace = "at AuthService.Login()",
            OccurredAt = DateTime.UtcNow.AddMinutes(-10),
            Endpoint = "/api/auth/login",
            HttpMethod = "POST",
            StatusCode = 500,
            UserId = "user-123",
            Metadata = new Dictionary<string, object> { { "correlationId", "corr-456" } }
        };

        _repoMock.Setup(x => x.GetByIdAsync(errorId)).ReturnsAsync(errorLog);

        var query = new GetErrorQuery(new GetErrorRequest(errorId));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(errorId, result!.Id);
        Assert.Equal("AuthService", result.ServiceName);
        Assert.Equal("NullReferenceException", result.ExceptionType);
        Assert.Equal("POST", result.HttpMethod);
        Assert.Equal(500, result.StatusCode);
        Assert.Equal("user-123", result.UserId);
    }

    [Fact]
    public async Task Handle_NonExistentError_ReturnsNull()
    {
        // Arrange
        var errorId = Guid.NewGuid();
        _repoMock.Setup(x => x.GetByIdAsync(errorId)).ReturnsAsync((ErrorLog?)null);

        var query = new GetErrorQuery(new GetErrorRequest(errorId));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_ErrorWithNullOptionalFields_MapsCorrectly()
    {
        // Arrange
        var errorId = Guid.NewGuid();
        var errorLog = new ErrorLog
        {
            Id = errorId,
            ServiceName = "MediaService",
            ExceptionType = "TimeoutException",
            Message = "Operation timed out",
            StackTrace = null,
            OccurredAt = DateTime.UtcNow,
            Endpoint = null,
            HttpMethod = null,
            StatusCode = null,
            UserId = null,
            Metadata = new Dictionary<string, object>()
        };

        _repoMock.Setup(x => x.GetByIdAsync(errorId)).ReturnsAsync(errorLog);

        var query = new GetErrorQuery(new GetErrorRequest(errorId));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result!.StackTrace);
        Assert.Null(result.Endpoint);
        Assert.Null(result.HttpMethod);
        Assert.Null(result.StatusCode);
        Assert.Null(result.UserId);
    }
}

public class GetErrorsQueryHandlerTests
{
    private readonly Mock<IErrorLogRepository> _repoMock;
    private readonly GetErrorsQueryHandler _handler;

    public GetErrorsQueryHandlerTests()
    {
        _repoMock = new Mock<IErrorLogRepository>();
        _handler = new GetErrorsQueryHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsFilteredErrors()
    {
        // Arrange
        var errors = new List<ErrorLog>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ServiceName = "VehiclesSaleService",
                ExceptionType = "DbUpdateException",
                Message = "Could not save entity",
                OccurredAt = DateTime.UtcNow,
                StatusCode = 500,
                Metadata = new Dictionary<string, object>()
            },
            new()
            {
                Id = Guid.NewGuid(),
                ServiceName = "VehiclesSaleService",
                ExceptionType = "ValidationException",
                Message = "Invalid vehicle data",
                OccurredAt = DateTime.UtcNow,
                StatusCode = 400,
                Metadata = new Dictionary<string, object>()
            }
        };

        _repoMock.Setup(x => x.GetAsync(It.IsAny<ErrorQuery>())).ReturnsAsync(errors);

        var request = new GetErrorsRequest("VehiclesSaleService", null, null, 1, 10);
        var query = new GetErrorsQuery(request);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(2, result.Errors.Count);
        Assert.All(result.Errors, e => Assert.Equal("VehiclesSaleService", e.ServiceName));
    }

    [Fact]
    public async Task Handle_NoErrors_ReturnsEmptyList()
    {
        // Arrange
        _repoMock.Setup(x => x.GetAsync(It.IsAny<ErrorQuery>()))
            .ReturnsAsync(Enumerable.Empty<ErrorLog>());

        var request = new GetErrorsRequest("NonExistentService", null, null, 1, 10);
        var query = new GetErrorsQuery(request);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Errors);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task Handle_PassesCorrectQueryToRepository()
    {
        // Arrange
        ErrorQuery? capturedQuery = null;
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;

        _repoMock.Setup(x => x.GetAsync(It.IsAny<ErrorQuery>()))
            .Callback<ErrorQuery>(q => capturedQuery = q)
            .ReturnsAsync(Enumerable.Empty<ErrorLog>());

        var request = new GetErrorsRequest("AuthService", from, to, 2, 25);
        var query = new GetErrorsQuery(request);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedQuery);
        Assert.Equal("AuthService", capturedQuery!.ServiceName);
        Assert.Equal(from, capturedQuery.From);
        Assert.Equal(to, capturedQuery.To);
        Assert.Equal(2, capturedQuery.Page);
        Assert.Equal(25, capturedQuery.PageSize);
    }
}

public class GetErrorStatsQueryHandlerTests
{
    private readonly Mock<IErrorLogRepository> _repoMock;
    private readonly GetErrorStatsQueryHandler _handler;

    public GetErrorStatsQueryHandlerTests()
    {
        _repoMock = new Mock<IErrorLogRepository>();
        _handler = new GetErrorStatsQueryHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsStatsFromRepository()
    {
        // Arrange
        var stats = new ErrorStats
        {
            TotalErrors = 1500,
            ErrorsLast24Hours = 42,
            ErrorsLast7Days = 280,
            ErrorsByService = new Dictionary<string, int>
            {
                { "AuthService", 500 },
                { "VehiclesSaleService", 800 },
                { "MediaService", 200 }
            },
            ErrorsByStatusCode = new Dictionary<int, int>
            {
                { 500, 900 },
                { 400, 400 },
                { 503, 200 }
            }
        };

        _repoMock.Setup(x => x.GetStatsAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(stats);

        var query = new GetErrorStatsQuery(new GetErrorStatsRequest(null, null));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1500, result.TotalErrors);
        Assert.Equal(42, result.ErrorsLast24Hours);
        Assert.Equal(280, result.ErrorsLast7Days);
        Assert.Equal(3, result.ErrorsByService.Count);
        Assert.Equal(3, result.ErrorsByStatusCode.Count);
        Assert.Equal(800, result.ErrorsByService["VehiclesSaleService"]);
    }

    [Fact]
    public async Task Handle_WithDateRange_PassesDatesToRepository()
    {
        // Arrange
        var from = DateTime.UtcNow.AddDays(-30);
        var to = DateTime.UtcNow;

        _repoMock.Setup(x => x.GetStatsAsync(from, to))
            .ReturnsAsync(new ErrorStats());

        var query = new GetErrorStatsQuery(new GetErrorStatsRequest(from, to));

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _repoMock.Verify(x => x.GetStatsAsync(from, to), Times.Once);
    }
}

public class GetServiceNamesQueryHandlerTests
{
    private readonly Mock<IErrorLogRepository> _repoMock;
    private readonly GetServiceNamesQueryHandler _handler;

    public GetServiceNamesQueryHandlerTests()
    {
        _repoMock = new Mock<IErrorLogRepository>();
        _handler = new GetServiceNamesQueryHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsServiceNames()
    {
        // Arrange
        var serviceNames = new List<string>
        {
            "AuthService", "VehiclesSaleService", "MediaService",
            "NotificationService", "ContactService"
        };

        _repoMock.Setup(x => x.GetServiceNamesAsync())
            .ReturnsAsync(serviceNames);

        var query = new GetServiceNamesQuery(new GetServiceNamesRequest());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.ServiceNames.Count);
        Assert.Contains("AuthService", result.ServiceNames);
        Assert.Contains("VehiclesSaleService", result.ServiceNames);
    }

    [Fact]
    public async Task Handle_NoServices_ReturnsEmptyList()
    {
        // Arrange
        _repoMock.Setup(x => x.GetServiceNamesAsync())
            .ReturnsAsync(Enumerable.Empty<string>());

        var query = new GetServiceNamesQuery(new GetServiceNamesRequest());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.ServiceNames);
    }
}
