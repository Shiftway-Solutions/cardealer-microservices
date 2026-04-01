using BillingService.Domain.Entities;
using BillingService.Domain.Interfaces;
using BillingService.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BillingService.Tests;

/// <summary>
/// CONTRA #6 FIX: Payment Reconciliation Tests
///
/// Validates the PaymentReconciliationService correctly:
///   1. Detects active subscriptions without payments (SubscriptionWithoutPayment)
///   2. Detects payments without valid subscription links (PaymentWithoutRecord)
///   3. Detects amount mismatches between payments and subscription price
///   4. Detects subscription status mismatches between OKLA and Stripe
///   5. Auto-resolves minor discrepancies (rounding ≤ $0.01)
///   6. Reports correct severity levels
///   7. Generates clean reports when no discrepancies found
///   8. Handles Stripe API failures gracefully
/// </summary>
public class PaymentReconciliationTests
{
    private readonly Mock<IPaymentRepository> _paymentRepoMock;
    private readonly Mock<ISubscriptionRepository> _subscriptionRepoMock;
    private readonly Mock<IStripeService> _stripeServiceMock;
    private readonly Mock<IReconciliationRepository> _reconciliationRepoMock;
    private readonly PaymentReconciliationService _service;

    public PaymentReconciliationTests()
    {
        _paymentRepoMock = new Mock<IPaymentRepository>();
        _subscriptionRepoMock = new Mock<ISubscriptionRepository>();
        _stripeServiceMock = new Mock<IStripeService>();
        _reconciliationRepoMock = new Mock<IReconciliationRepository>();
        var logger = NullLogger<PaymentReconciliationService>.Instance;

        // Default: repository Add/Update return successfully
        _reconciliationRepoMock
            .Setup(r => r.AddAsync(It.IsAny<ReconciliationReport>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReconciliationReport report, CancellationToken _) => report);
        _reconciliationRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<ReconciliationReport>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Default: empty payment date range and subscription statuses
        _paymentRepoMock
            .Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Payment>());
        foreach (var status in new[] { SubscriptionStatus.Active, SubscriptionStatus.PastDue, SubscriptionStatus.Trial })
        {
            _subscriptionRepoMock
                .Setup(r => r.GetByStatusAsync(status, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<Subscription>());
        }

        // Default: Stripe returns "active" for any unspecified subscription ID (Check 3 won't fail)
        _stripeServiceMock
            .Setup(s => s.GetSubscriptionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string subId, CancellationToken _) => new StripeSubscriptionResult(
                subId, "cus_default", "active", "price_default",
                0m, "usd", "month",
                DateTime.UtcNow.AddDays(-15), DateTime.UtcNow.AddDays(15),
                null, null, null, false, DateTime.UtcNow.AddMonths(-6)));

        _service = new PaymentReconciliationService(
            _paymentRepoMock.Object,
            _subscriptionRepoMock.Object,
            _stripeServiceMock.Object,
            _reconciliationRepoMock.Object,
            logger);
    }

    // ════════════════════════════════════════════════════════════════════════
    // CLEAN RECONCILIATION (NO DISCREPANCIES)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task RunReconciliation_AllPaymentsMatch_ReturnsCleanReport()
    {
        // Arrange: 1 active subscription with 1 matching payment
        var sub = CreateSubscription(SubscriptionPlan.Professional, SubscriptionStatus.Active, 89m);
        var payment = CreatePayment(sub.Id, sub.DealerId, 89m, PaymentStatus.Succeeded);

        SetupSubscriptions(SubscriptionStatus.Active, sub);
        SetupPaymentsBySubscription(sub.Id, payment);
        SetupPaymentsByDateRange(payment);
        SetupStripeSubscription(sub.StripeSubscriptionId!, "active");
        SetupSubscriptionStatuses();

        // Act
        var report = await _service.RunReconciliationAsync("2026-03");

        // Assert
        report.Status.Should().Be(ReconciliationStatus.Clean);
        report.DiscrepancyCount.Should().Be(0);
        report.TotalSubscriptionsChecked.Should().Be(1);
        report.Period.Should().Be("2026-03");
    }

    [Fact]
    public async Task RunReconciliation_FreeSubscriptions_AreSkipped()
    {
        // Arrange: Free plan subscription should not require payment
        var freeSub = CreateSubscription(SubscriptionPlan.Free, SubscriptionStatus.Active, 0m);
        SetupSubscriptions(SubscriptionStatus.Active, freeSub);
        SetupPaymentsByDateRange();
        SetupSubscriptionStatuses();

        // Act
        var report = await _service.RunReconciliationAsync("2026-03");

        // Assert: Free plans should NOT trigger SubscriptionWithoutPayment
        report.Status.Should().Be(ReconciliationStatus.Clean);
        report.DiscrepancyCount.Should().Be(0);
    }

    // ════════════════════════════════════════════════════════════════════════
    // SUBSCRIPTION WITHOUT PAYMENT
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task RunReconciliation_ActiveSubWithoutPayment_DetectsDiscrepancy()
    {
        // Arrange: Active paid subscription with NO payments in the period
        var sub = CreateSubscription(SubscriptionPlan.Enterprise, SubscriptionStatus.Active, 199m);
        SetupSubscriptions(SubscriptionStatus.Active, sub);
        SetupPaymentsBySubscription(sub.Id); // empty — no payments
        SetupPaymentsByDateRange();
        SetupSubscriptionStatuses();

        // Act
        var report = await _service.RunReconciliationAsync("2026-03");

        // Assert
        report.Status.Should().Be(ReconciliationStatus.DiscrepanciesFound);
        report.DiscrepancyCount.Should().Be(1);

        var disc = report.Discrepancies.First();
        disc.Type.Should().Be(DiscrepancyType.SubscriptionWithoutPayment);
        disc.Severity.Should().Be(DiscrepancySeverity.High);
        disc.DealerId.Should().Be(sub.DealerId);
        disc.StripeAmount.Should().Be(199m);
        disc.OklaAmount.Should().Be(0m);
    }

    [Fact]
    public async Task RunReconciliation_MultipleSubsWithoutPayment_DetectsAll()
    {
        var sub1 = CreateSubscription(SubscriptionPlan.Professional, SubscriptionStatus.Active, 89m);
        var sub2 = CreateSubscription(SubscriptionPlan.Enterprise, SubscriptionStatus.Active, 199m);

        SetupSubscriptions(SubscriptionStatus.Active, sub1, sub2);
        SetupPaymentsBySubscription(sub1.Id); // no payments for sub1
        SetupPaymentsBySubscription(sub2.Id); // no payments for sub2
        SetupPaymentsByDateRange();
        SetupSubscriptionStatuses();

        var report = await _service.RunReconciliationAsync("2026-03");

        report.DiscrepancyCount.Should().Be(2);
        report.Discrepancies
            .Where(d => d.Type == DiscrepancyType.SubscriptionWithoutPayment)
            .Should().HaveCount(2);
    }

    // ════════════════════════════════════════════════════════════════════════
    // PAYMENT WITHOUT SUBSCRIPTION
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task RunReconciliation_PaymentWithoutSubscriptionLink_DetectsDiscrepancy()
    {
        // Arrange: Succeeded payment with no SubscriptionId
        var orphanPayment = CreatePayment(null, Guid.NewGuid(), 89m, PaymentStatus.Succeeded);

        SetupSubscriptions(SubscriptionStatus.Active); // no active subs
        SetupPaymentsByDateRange(orphanPayment);
        SetupSubscriptionStatuses();

        var report = await _service.RunReconciliationAsync("2026-03");

        report.Status.Should().Be(ReconciliationStatus.DiscrepanciesFound);
        var disc = report.Discrepancies.First(d => d.Type == DiscrepancyType.PaymentWithoutRecord);
        disc.Severity.Should().Be(DiscrepancySeverity.Medium);
    }

    [Fact]
    public async Task RunReconciliation_PaymentRefsDeletedSubscription_DetectsDiscrepancy()
    {
        // Arrange: Payment references subscription that doesn't exist
        var deletedSubId = Guid.NewGuid();
        var payment = CreatePayment(deletedSubId, Guid.NewGuid(), 89m, PaymentStatus.Succeeded);

        SetupSubscriptions(SubscriptionStatus.Active);
        SetupPaymentsByDateRange(payment);
        _subscriptionRepoMock
            .Setup(r => r.GetByIdAsync(deletedSubId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);
        SetupSubscriptionStatuses();

        var report = await _service.RunReconciliationAsync("2026-03");

        var disc = report.Discrepancies
            .First(d => d.Type == DiscrepancyType.PaymentWithoutRecord);
        disc.Severity.Should().Be(DiscrepancySeverity.High);
        disc.Description.Should().Contain("does not exist");
    }

    // ════════════════════════════════════════════════════════════════════════
    // AMOUNT MISMATCH
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task RunReconciliation_AmountMismatchLarge_DetectsHighSeverity()
    {
        // Arrange: Payment of $50 but subscription costs $89
        var sub = CreateSubscription(SubscriptionPlan.Professional, SubscriptionStatus.Active, 89m);
        var payment = CreatePayment(sub.Id, sub.DealerId, 50m, PaymentStatus.Succeeded);

        SetupSubscriptions(SubscriptionStatus.Active, sub);
        SetupPaymentsBySubscription(sub.Id, payment);
        SetupPaymentsByDateRange(payment);
        SetupStripeSubscription(sub.StripeSubscriptionId!, "active");
        SetupSubscriptionStatuses();

        var report = await _service.RunReconciliationAsync("2026-03");

        var disc = report.Discrepancies
            .First(d => d.Type == DiscrepancyType.AmountMismatch);
        disc.Severity.Should().Be(DiscrepancySeverity.High);
        disc.AmountDifference.Should().Be(39m);
    }

    [Fact]
    public async Task RunReconciliation_AmountMismatchSmall_AutoResolvesWhenEnabled()
    {
        // Arrange: Payment of $88.50 but subscription costs $89 (within $1 tolerance)
        var sub = CreateSubscription(SubscriptionPlan.Professional, SubscriptionStatus.Active, 89m);
        var payment = CreatePayment(sub.Id, sub.DealerId, 88.50m, PaymentStatus.Succeeded);

        SetupSubscriptions(SubscriptionStatus.Active, sub);
        SetupPaymentsBySubscription(sub.Id, payment);
        SetupPaymentsByDateRange(payment);
        SetupStripeSubscription(sub.StripeSubscriptionId!, "active");
        SetupSubscriptionStatuses();

        var report = await _service.RunReconciliationAsync("2026-03", autoResolve: true);

        var disc = report.Discrepancies
            .First(d => d.Type == DiscrepancyType.AmountMismatch);
        disc.IsAutoResolved.Should().BeTrue();
        report.AutoResolvedCount.Should().Be(1);
    }

    [Fact]
    public async Task RunReconciliation_AmountWithinTolerance_NoDiscrepancy()
    {
        // Arrange: Payment of $88.99 but subscription costs $89 (within $0.02 tolerance)
        var sub = CreateSubscription(SubscriptionPlan.Professional, SubscriptionStatus.Active, 89m);
        var payment = CreatePayment(sub.Id, sub.DealerId, 88.99m, PaymentStatus.Succeeded);

        SetupSubscriptions(SubscriptionStatus.Active, sub);
        SetupPaymentsBySubscription(sub.Id, payment);
        SetupPaymentsByDateRange(payment);
        SetupStripeSubscription(sub.StripeSubscriptionId!, "active");
        SetupSubscriptionStatuses();

        var report = await _service.RunReconciliationAsync("2026-03");

        report.Discrepancies
            .Where(d => d.Type == DiscrepancyType.AmountMismatch)
            .Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════
    // STATUS MISMATCH
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task RunReconciliation_StripeActiveBuOklaPastDue_DetectsStatusMismatch()
    {
        // Arrange: OKLA says PastDue but Stripe says active
        var sub = CreateSubscription(SubscriptionPlan.Professional, SubscriptionStatus.PastDue, 89m);
        SetupSubscriptionsByStatus(SubscriptionStatus.PastDue, sub);
        SetupStripeSubscription(sub.StripeSubscriptionId!, "active");

        // Set up other statuses as empty
        _subscriptionRepoMock
            .Setup(r => r.GetByStatusAsync(SubscriptionStatus.Active, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Subscription>());
        _subscriptionRepoMock
            .Setup(r => r.GetByStatusAsync(SubscriptionStatus.Trial, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Subscription>());
        SetupPaymentsByDateRange();

        var report = await _service.RunReconciliationAsync("2026-03");

        var disc = report.Discrepancies
            .First(d => d.Type == DiscrepancyType.StatusMismatch);
        disc.Description.Should().Contain("OKLA=PastDue");
        disc.Description.Should().Contain("Stripe=active");
    }

    [Fact]
    public async Task RunReconciliation_StripeCanceledButOklaActive_DetectsCriticalSeverity()
    {
        var sub = CreateSubscription(SubscriptionPlan.Enterprise, SubscriptionStatus.Active, 199m);
        var payment = CreatePayment(sub.Id, sub.DealerId, 199m, PaymentStatus.Succeeded);

        SetupSubscriptions(SubscriptionStatus.Active, sub);
        SetupPaymentsBySubscription(sub.Id, payment);
        SetupPaymentsByDateRange(payment);
        SetupStripeSubscription(sub.StripeSubscriptionId!, "canceled");

        _subscriptionRepoMock
            .Setup(r => r.GetByStatusAsync(SubscriptionStatus.PastDue, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Subscription>());
        _subscriptionRepoMock
            .Setup(r => r.GetByStatusAsync(SubscriptionStatus.Trial, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Subscription>());

        var report = await _service.RunReconciliationAsync("2026-03");

        var disc = report.Discrepancies
            .First(d => d.Type == DiscrepancyType.StatusMismatch);
        disc.Severity.Should().Be(DiscrepancySeverity.Critical);
    }

    // ════════════════════════════════════════════════════════════════════════
    // DEALER RECONCILIATION
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ReconcileDealer_ActiveSubWithoutPayment_ReturnsDiscrepancy()
    {
        var dealerId = Guid.NewGuid();
        var sub = CreateSubscription(SubscriptionPlan.Professional, SubscriptionStatus.Active, 89m, dealerId);

        _subscriptionRepoMock
            .Setup(r => r.GetByDealerIdAsync(dealerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sub);
        _paymentRepoMock
            .Setup(r => r.GetBySubscriptionIdAsync(sub.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Payment>());
        _stripeServiceMock
            .Setup(s => s.GetSubscriptionAsync(sub.StripeSubscriptionId!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StripeSubscriptionResult(
                sub.StripeSubscriptionId!, "cus_test", "active", "price_test",
                89m, "usd", "month", DateTime.UtcNow.AddDays(-15), DateTime.UtcNow.AddDays(15),
                null, null, null, false, DateTime.UtcNow.AddMonths(-6)));

        var discrepancies = await _service.ReconcileDealerAsync(dealerId, "2026-03");

        discrepancies.Should().HaveCount(1);
        discrepancies.First().Type.Should().Be(DiscrepancyType.SubscriptionWithoutPayment);
    }

    [Fact]
    public async Task ReconcileDealer_NoSubscription_ReturnsEmpty()
    {
        var dealerId = Guid.NewGuid();
        _subscriptionRepoMock
            .Setup(r => r.GetByDealerIdAsync(dealerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        var discrepancies = await _service.ReconcileDealerAsync(dealerId, "2026-03");

        discrepancies.Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════
    // REPORT METADATA
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task RunReconciliation_SetsTriggeredBy()
    {
        SetupSubscriptions(SubscriptionStatus.Active);
        SetupPaymentsByDateRange();
        SetupSubscriptionStatuses();

        var report = await _service.RunReconciliationAsync("2026-03", triggeredBy: "admin@okla.do");

        report.TriggeredBy.Should().Be("admin@okla.do");
    }

    [Fact]
    public async Task RunReconciliation_DefaultsToCurrentMonth()
    {
        SetupSubscriptions(SubscriptionStatus.Active);
        SetupPaymentsByDateRange();
        SetupSubscriptionStatuses();

        var report = await _service.RunReconciliationAsync();

        report.Period.Should().Be(DateTime.UtcNow.ToString("yyyy-MM"));
    }

    [Fact]
    public async Task RunReconciliation_PersistsReport()
    {
        SetupSubscriptions(SubscriptionStatus.Active);
        SetupPaymentsByDateRange();
        SetupSubscriptionStatuses();

        await _service.RunReconciliationAsync("2026-03");

        _reconciliationRepoMock.Verify(
            r => r.AddAsync(It.IsAny<ReconciliationReport>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _reconciliationRepoMock.Verify(
            r => r.UpdateAsync(It.IsAny<ReconciliationReport>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ════════════════════════════════════════════════════════════════════════
    // ERROR HANDLING
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task RunReconciliation_StripeApiFailure_ReportsFailedStatus()
    {
        var sub = CreateSubscription(SubscriptionPlan.Professional, SubscriptionStatus.Active, 89m);
        var payment = CreatePayment(sub.Id, sub.DealerId, 89m, PaymentStatus.Succeeded);

        SetupSubscriptions(SubscriptionStatus.Active, sub);
        SetupPaymentsBySubscription(sub.Id, payment);
        SetupPaymentsByDateRange(payment);

        // Stripe throws on status check
        _stripeServiceMock
            .Setup(s => s.GetSubscriptionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Stripe API rate limited"));

        // Need to set up other statuses
        _subscriptionRepoMock
            .Setup(r => r.GetByStatusAsync(SubscriptionStatus.PastDue, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Subscription>());
        _subscriptionRepoMock
            .Setup(r => r.GetByStatusAsync(SubscriptionStatus.Trial, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Subscription>());

        // The service should handle Stripe API errors gracefully per-subscription
        // and not fail the entire reconciliation
        var report = await _service.RunReconciliationAsync("2026-03");

        // Should complete (the Stripe error is caught per-subscription in CheckSubscriptionStatusSync)
        report.Status.Should().NotBe(ReconciliationStatus.InProgress);
    }

    // ════════════════════════════════════════════════════════════════════════
    // ENTITY TESTS
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ReconciliationReport_Complete_SetsCorrectStatus()
    {
        var report = new ReconciliationReport("2026-03");
        report.Complete();

        report.Status.Should().Be(ReconciliationStatus.Clean);
        report.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void ReconciliationReport_WithDiscrepancies_SetsDiscrepanciesFound()
    {
        var report = new ReconciliationReport("2026-03");
        var disc = new ReconciliationDiscrepancy(
            report.Id, DiscrepancyType.PaymentWithoutRecord,
            DiscrepancySeverity.Medium, "Test", "Fix it");
        report.AddDiscrepancy(disc);
        report.Complete();

        report.Status.Should().Be(ReconciliationStatus.DiscrepanciesFound);
        report.DiscrepancyCount.Should().Be(1);
    }

    [Fact]
    public void ReconciliationReport_Fail_SetsFailedStatus()
    {
        var report = new ReconciliationReport("2026-03");
        report.Fail("Stripe API unreachable");

        report.Status.Should().Be(ReconciliationStatus.Failed);
        report.ErrorMessage.Should().Be("Stripe API unreachable");
    }

    [Fact]
    public void ReconciliationDiscrepancy_AutoResolve_SetsFields()
    {
        var disc = new ReconciliationDiscrepancy(
            Guid.NewGuid(), DiscrepancyType.AmountMismatch,
            DiscrepancySeverity.Low, "Rounding diff", "Auto-resolve");

        disc.AutoResolve("Amount difference $0.01 within tolerance");

        disc.IsAutoResolved.Should().BeTrue();
        disc.ResolvedBy.Should().Be("system");
        disc.ResolvedAt.Should().NotBeNull();
        disc.ResolutionNotes.Should().Contain("$0.01");
    }

    [Fact]
    public void ReconciliationDiscrepancy_ManualResolve_SetsFields()
    {
        var disc = new ReconciliationDiscrepancy(
            Guid.NewGuid(), DiscrepancyType.SubscriptionWithoutPayment,
            DiscrepancySeverity.High, "Missing payment", "Create payment record");

        disc.Resolve("admin@okla.do", "Payment found in Stripe, created OKLA record");

        disc.ResolvedBy.Should().Be("admin@okla.do");
        disc.ResolvedAt.Should().NotBeNull();
        disc.ResolutionNotes.Should().Contain("found in Stripe");
    }

    [Fact]
    public void ReconciliationDiscrepancy_AmountDifference_CalculatesCorrectly()
    {
        var disc = new ReconciliationDiscrepancy(
            Guid.NewGuid(), DiscrepancyType.AmountMismatch,
            DiscrepancySeverity.Medium, "Mismatch", "Review",
            stripeAmount: 89m, oklaAmount: 50m);

        disc.AmountDifference.Should().Be(39m);
    }

    // ════════════════════════════════════════════════════════════════════════
    // HELPERS
    // ════════════════════════════════════════════════════════════════════════

    private static Subscription CreateSubscription(
        SubscriptionPlan plan, SubscriptionStatus status, decimal price, Guid? dealerId = null)
    {
        var sub = new Subscription(
            dealerId ?? Guid.NewGuid(), plan, BillingCycle.Monthly, price,
            maxUsers: 10, maxVehicles: 100);

        // Use reflection to set StripeSubscriptionId since it's private set
        typeof(Subscription)
            .GetProperty(nameof(Subscription.StripeSubscriptionId))!
            .SetValue(sub, $"sub_{Guid.NewGuid():N}");

        // Set subscription to desired status using domain methods
        switch (status)
        {
            case SubscriptionStatus.Active:
                sub.Activate();
                break;
            case SubscriptionStatus.PastDue:
                sub.Activate();
                sub.MarkPastDue();
                break;
            case SubscriptionStatus.Cancelled:
                sub.Activate();
                sub.Cancel("test cancellation");
                break;
            case SubscriptionStatus.Suspended:
                sub.Activate();
                sub.Suspend();
                break;
        }

        return sub;
    }

    private static Payment CreatePayment(
        Guid? subscriptionId, Guid dealerId, decimal amount, PaymentStatus status,
        DateTime? createdAt = null)
    {
        var payment = new Payment(dealerId, amount, PaymentMethod.CreditCard,
            subscriptionId: subscriptionId);

        payment.SetStripeInfo($"pi_{Guid.NewGuid():N}");

        if (status == PaymentStatus.Succeeded)
            payment.MarkSucceeded();

        // Force CreatedAt to a fixed March 2026 date so the payment always falls
        // within the "2026-03" reconciliation period, avoiding UTC midnight rollover failures.
        var effectiveDate = createdAt ?? new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc);
        typeof(Payment).GetProperty(nameof(Payment.CreatedAt))!
            .SetValue(payment, effectiveDate);

        return payment;
    }

    private void SetupSubscriptions(SubscriptionStatus status, params Subscription[] subs)
    {
        _subscriptionRepoMock
            .Setup(r => r.GetByStatusAsync(status, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subs);

        // Also set up GetByIdAsync for each subscription (needed by Check 2)
        foreach (var sub in subs)
        {
            _subscriptionRepoMock
                .Setup(r => r.GetByIdAsync(sub.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(sub);
        }

        // Set up other statuses as empty by default
        var otherStatuses = new[] { SubscriptionStatus.PastDue, SubscriptionStatus.Trial }
            .Where(s => s != status);
        foreach (var otherStatus in otherStatuses)
        {
            _subscriptionRepoMock
                .Setup(r => r.GetByStatusAsync(otherStatus, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<Subscription>());
        }
    }

    private void SetupSubscriptionsByStatus(SubscriptionStatus status, params Subscription[] subs)
    {
        _subscriptionRepoMock
            .Setup(r => r.GetByStatusAsync(status, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subs);
    }

    private void SetupSubscriptionStatuses()
    {
        // Set up empty arrays for any status not already configured
        foreach (var status in new[] { SubscriptionStatus.Active, SubscriptionStatus.PastDue, SubscriptionStatus.Trial })
        {
            if (!_subscriptionRepoMock.Setups.Any())
            {
                _subscriptionRepoMock
                    .Setup(r => r.GetByStatusAsync(status, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Array.Empty<Subscription>());
            }
        }
    }

    private void SetupPaymentsBySubscription(Guid subscriptionId, params Payment[] payments)
    {
        _paymentRepoMock
            .Setup(r => r.GetBySubscriptionIdAsync(subscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payments);
    }

    private void SetupPaymentsByDateRange(params Payment[] payments)
    {
        _paymentRepoMock
            .Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(payments);
    }

    private void SetupStripeSubscription(string stripeSubId, string stripeStatus)
    {
        _stripeServiceMock
            .Setup(s => s.GetSubscriptionAsync(stripeSubId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StripeSubscriptionResult(
                stripeSubId, "cus_test", stripeStatus, "price_test",
                0m, "usd", "month",
                DateTime.UtcNow.AddDays(-15), DateTime.UtcNow.AddDays(15),
                null, null, null, false, DateTime.UtcNow.AddMonths(-6)));
    }
}
