using FluentAssertions;
using NotificationService.Domain.Entities;
using Xunit;

namespace NotificationService.Tests.Unit.Entities;

public class OnboardingReportScheduleTests
{
    [Fact]
    public void Create_SetsAllProperties()
    {
        // Arrange
        var dealerId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var approvedAt = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);
        var delay = TimeSpan.FromDays(7);

        // Act
        var schedule = OnboardingReportSchedule.Create(dealerId, ownerUserId, approvedAt, delay);

        // Assert
        schedule.DealerId.Should().Be(dealerId);
        schedule.OwnerUserId.Should().Be(ownerUserId);
        schedule.ApprovedAt.Should().Be(approvedAt);
        schedule.DueAt.Should().Be(approvedAt.AddDays(7));
        schedule.Status.Should().Be("Scheduled");
        schedule.RetryCount.Should().Be(0);
        schedule.MaxRetries.Should().Be(3);
        schedule.IdempotencyKey.Should().Be($"onboarding_report:{dealerId}");
        schedule.SentAt.Should().BeNull();
        schedule.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Create_WithNullOwnerUserId_SetsNull()
    {
        var schedule = OnboardingReportSchedule.Create(
            Guid.NewGuid(), null, DateTime.UtcNow, TimeSpan.FromDays(7));

        schedule.OwnerUserId.Should().BeNull();
    }

    [Fact]
    public void MarkAsProcessing_SetsStatusAndUpdatedAt()
    {
        var schedule = OnboardingReportSchedule.Create(
            Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, TimeSpan.FromDays(7));

        schedule.MarkAsProcessing();

        schedule.Status.Should().Be("Processing");
        schedule.UpdatedAt.Should().NotBeNull();
        schedule.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void MarkAsSent_SetsStatusSentAtAndUpdatedAt()
    {
        var schedule = OnboardingReportSchedule.Create(
            Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, TimeSpan.FromDays(7));

        schedule.MarkAsSent();

        schedule.Status.Should().Be("Sent");
        schedule.SentAt.Should().NotBeNull();
        schedule.SentAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        schedule.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsFailed_FirstRetry_ReschedulesWithBackoff()
    {
        var schedule = OnboardingReportSchedule.Create(
            Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, TimeSpan.FromDays(7));

        schedule.MarkAsFailed("Connection timeout");

        schedule.RetryCount.Should().Be(1);
        schedule.ErrorMessage.Should().Be("Connection timeout");
        schedule.Status.Should().Be("Scheduled"); // Still scheduled for retry
        // After first failure, DueAt is rescheduled to ~15min from now (not 7 days)
        schedule.DueAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(15), TimeSpan.FromSeconds(5));
        schedule.CanRetry().Should().BeTrue();
    }

    [Fact]
    public void MarkAsFailed_ExhaustsRetries_SetsFailed()
    {
        var schedule = OnboardingReportSchedule.Create(
            Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, TimeSpan.FromDays(7));

        // Exhaust all 3 retries
        schedule.MarkAsFailed("Error 1");
        schedule.MarkAsFailed("Error 2");
        schedule.MarkAsFailed("Error 3");

        schedule.RetryCount.Should().Be(3);
        schedule.Status.Should().Be("Failed");
        schedule.CanRetry().Should().BeFalse();
        schedule.ErrorMessage.Should().Be("Error 3");
    }

    [Fact]
    public void IsDue_WhenScheduledAndPastDue_ReturnsTrue()
    {
        var schedule = OnboardingReportSchedule.Create(
            Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(-8), TimeSpan.FromDays(7));

        schedule.IsDue().Should().BeTrue();
    }

    [Fact]
    public void IsDue_WhenScheduledAndNotYetDue_ReturnsFalse()
    {
        var schedule = OnboardingReportSchedule.Create(
            Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, TimeSpan.FromDays(7));

        schedule.IsDue().Should().BeFalse();
    }

    [Fact]
    public void IsDue_WhenAlreadySent_ReturnsFalse()
    {
        var schedule = OnboardingReportSchedule.Create(
            Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(-8), TimeSpan.FromDays(7));
        schedule.MarkAsSent();

        schedule.IsDue().Should().BeFalse();
    }

    [Fact]
    public void IsDue_WhenFailed_ReturnsFalse()
    {
        var schedule = OnboardingReportSchedule.Create(
            Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(-8), TimeSpan.FromDays(7));
        schedule.MarkAsFailed("err1");
        schedule.MarkAsFailed("err2");
        schedule.MarkAsFailed("err3"); // Now Failed

        schedule.IsDue().Should().BeFalse();
    }

    [Fact]
    public void ExponentialBackoff_IncreasesBetweenRetries()
    {
        var schedule = OnboardingReportSchedule.Create(
            Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(-8), TimeSpan.FromDays(7));

        // First retry: ~15 min backoff
        schedule.MarkAsFailed("Error 1");
        var firstDueAt = schedule.DueAt;

        // Second retry: ~60 min backoff (15 * 4^1)
        schedule.MarkAsFailed("Error 2");
        var secondDueAt = schedule.DueAt;

        // Second retry should be further in the future
        secondDueAt.Should().BeAfter(firstDueAt);
    }

    [Fact]
    public void IdempotencyKey_ContainsDealerId()
    {
        var dealerId = Guid.NewGuid();
        var schedule = OnboardingReportSchedule.Create(
            dealerId, Guid.NewGuid(), DateTime.UtcNow, TimeSpan.FromDays(7));

        schedule.IdempotencyKey.Should().Contain(dealerId.ToString());
        schedule.IdempotencyKey.Should().StartWith("onboarding_report:");
    }
}
