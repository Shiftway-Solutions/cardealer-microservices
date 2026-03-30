using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using UserService.Api.Controllers;
using UserService.Application.DTOs;
using UserService.Domain.Entities;
using UserService.Domain.Interfaces;

namespace UserService.Tests.Controllers;

public class DealerSettingsIntegrationTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<IDealerRepository> _dealerRepositoryMock;
    private readonly IConfiguration _configuration;
    private readonly Guid _ownerUserId = Guid.NewGuid();

    public DealerSettingsIntegrationTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _dealerRepositoryMock = new Mock<IDealerRepository>();
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
    }

    [Fact]
    public async Task GetDealerSettings_WhenDealerBelongsToUser_ReturnsDefaults()
    {
        var dealerId = Guid.NewGuid();
        var dealer = CreateDealer(dealerId, _ownerUserId);
        _dealerRepositoryMock.Setup(r => r.GetByIdAsync(dealerId)).ReturnsAsync(dealer);

        var controller = CreateController(_ownerUserId);

        var result = await controller.GetDealerSettings(dealerId);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = okResult.Value.Should().BeOfType<DealerSettingsDto>().Subject;
        payload.DealerId.Should().Be(dealerId);
        payload.Notifications.EmailNewLead.Should().BeTrue();
        payload.Notifications.SmsNewLead.Should().BeFalse();
        payload.Security.SessionTimeoutMinutes.Should().Be(30);
        payload.Security.TwoFactorEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateDealerSettings_PersistsNotificationsAndSecurity()
    {
        var dealerId = Guid.NewGuid();
        var dealer = CreateDealer(dealerId, _ownerUserId);
        _dealerRepositoryMock.Setup(r => r.GetByIdAsync(dealerId)).ReturnsAsync(dealer);
        _dealerRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Dealer>())).Returns(Task.CompletedTask);

        var controller = CreateController(_ownerUserId);
        var notificationSettings = new DealerNotificationSettingsDto
        {
            EmailNewLead = false,
            EmailMessages = true,
            EmailAppointments = false,
            EmailWeeklyReport = true,
            SmsNewLead = true,
            SmsAppointments = false,
            PushMessages = false,
            PushLeads = true
        };

        var notificationResult = await controller.UpdateNotificationSettings(dealerId, notificationSettings);
        var securityResult = await controller.UpdateSecuritySettings(
            dealerId,
            new UpdateDealerSecuritySettingsRequest { SessionTimeoutMinutes = 120 });
        var getResult = await controller.GetDealerSettings(dealerId);

        notificationResult.Should().BeOfType<OkObjectResult>();
        securityResult.Should().BeOfType<OkObjectResult>();

        var okResult = getResult.Should().BeOfType<OkObjectResult>().Subject;
        var payload = okResult.Value.Should().BeOfType<DealerSettingsDto>().Subject;
        payload.Notifications.EmailNewLead.Should().BeFalse();
        payload.Notifications.EmailAppointments.Should().BeFalse();
        payload.Notifications.SmsNewLead.Should().BeTrue();
        payload.Notifications.PushMessages.Should().BeFalse();
        payload.Security.SessionTimeoutMinutes.Should().Be(120);
        _dealerRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Dealer>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GetDealerSettings_WhenDealerBelongsToAnotherUser_ReturnsForbidden()
    {
        var dealerId = Guid.NewGuid();
        var dealer = CreateDealer(dealerId, Guid.NewGuid());
        _dealerRepositoryMock.Setup(r => r.GetByIdAsync(dealerId)).ReturnsAsync(dealer);

        var controller = CreateController(_ownerUserId);

        var result = await controller.GetDealerSettings(dealerId);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task UpdateSecuritySettings_WhenTwoFactorIsRequested_ReturnsBadRequest()
    {
        var dealerId = Guid.NewGuid();
        var dealer = CreateDealer(dealerId, _ownerUserId);
        _dealerRepositoryMock.Setup(r => r.GetByIdAsync(dealerId)).ReturnsAsync(dealer);

        var controller = CreateController(_ownerUserId);

        var result = await controller.UpdateSecuritySettings(
            dealerId,
            new UpdateDealerSecuritySettingsRequest { TwoFactorEnabled = true });

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    private DealersController CreateController(Guid authenticatedUserId)
    {
        var controller = new DealersController(
            _mediatorMock.Object,
            _configuration,
            _dealerRepositoryMock.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, authenticatedUserId.ToString()),
                    new Claim("sub", authenticatedUserId.ToString())
                }, "TestAuth"))
            }
        };

        return controller;
    }

    private static Dealer CreateDealer(Guid dealerId, Guid ownerUserId)
    {
        return new Dealer
        {
            Id = dealerId,
            OwnerUserId = ownerUserId,
            BusinessName = $"Dealer {dealerId:N}",
            Email = $"{dealerId:N}@okla.com.do",
            Phone = "+18095550000",
            WhatsApp = "+18095550001",
            Address = "Av. Winston Churchill #1",
            City = "Santo Domingo",
            State = "Distrito Nacional",
            Country = "DO",
            Slug = $"dealer-{dealerId:N}",
            VerificationStatus = DealerVerificationStatus.Verified,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }
}