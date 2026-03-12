using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Registry;
using Stripe;
using BillingService.Domain.Interfaces;

namespace BillingService.Infrastructure.Services;

/// <summary>
/// Implementación del servicio de Stripe con circuit breaker.
/// All Stripe SDK calls are wrapped in a ResiliencePipeline that provides
/// retry (2 attempts) + circuit breaker (opens at 50% failure rate in 60s).
/// Degraded behavior: throws BrokenCircuitException → caller returns 503.
/// </summary>
public class StripeService : IStripeService
{
    private readonly ILogger<StripeService> _logger;
    private readonly StripeSettings _settings;
    private readonly ResiliencePipeline _resiliencePipeline;

    public StripeService(
        ILogger<StripeService> logger,
        IOptions<StripeSettings> settings,
        ResiliencePipelineProvider<string> resiliencePipelineProvider)
    {
        _logger = logger;
        _settings = settings.Value;
        _resiliencePipeline = resiliencePipelineProvider.GetPipeline("stripe-circuit-breaker");

        // Configurar Stripe con la API key
        StripeConfiguration.ApiKey = _settings.SecretKey;
    }

    // ========================================
    // RESILIENCE WRAPPER
    // ========================================

    /// <summary>
    /// Wraps a Stripe SDK call in the circuit breaker pipeline.
    /// When the circuit is open, throws BrokenCircuitException immediately
    /// instead of sending requests to a degraded Stripe API.
    /// </summary>
    private async Task<T> ExecuteWithResilienceAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
    {
        return await _resiliencePipeline.ExecuteAsync(async ct => await action(ct), cancellationToken);
    }

    // ========================================
    // CUSTOMER OPERATIONS
    // ========================================

    public async Task<StripeCustomerResult> CreateCustomerAsync(
        string email,
        string name,
        string? phone = null,
        string? dealerId = null,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Merge dealerId into metadata
            var allMetadata = metadata ?? new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(dealerId))
            {
                allMetadata["dealer_id"] = dealerId;
            }

            var options = new CustomerCreateOptions
            {
                Email = email,
                Name = name,
                Phone = phone,
                Metadata = allMetadata
            };

            var service = new CustomerService();
            var customer = await ExecuteWithResilienceAsync(
                async ct => await service.CreateAsync(options, cancellationToken: ct),
                cancellationToken);

            _logger.LogInformation("Created Stripe customer {CustomerId} for {Email}", customer.Id, email);

            return MapToCustomerResult(customer);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to create Stripe customer for {Email}", email);
            throw new InvalidOperationException($"Failed to create Stripe customer: {ex.Message}", ex);
        }
    }

    public async Task<StripeCustomerResult?> GetCustomerAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var service = new CustomerService();
            var customer = await ExecuteWithResilienceAsync(
                async ct => await service.GetAsync(customerId, cancellationToken: ct),
                cancellationToken);

            return customer.Deleted == true ? null : MapToCustomerResult(customer);
        }
        catch (StripeException ex) when (ex.StripeError?.Code == "resource_missing")
        {
            return null;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to get Stripe customer {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<StripeCustomerResult> UpdateCustomerAsync(
        string customerId,
        string? email = null,
        string? name = null,
        string? phone = null,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new CustomerUpdateOptions();

            if (email != null) options.Email = email;
            if (name != null) options.Name = name;
            if (phone != null) options.Phone = phone;
            if (metadata != null) options.Metadata = metadata;

            var service = new CustomerService();
            var customer = await ExecuteWithResilienceAsync(
                async ct => await service.UpdateAsync(customerId, options, cancellationToken: ct),
                cancellationToken);

            _logger.LogInformation("Updated Stripe customer {CustomerId}", customerId);

            return MapToCustomerResult(customer);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to update Stripe customer {CustomerId}", customerId);
            throw new InvalidOperationException($"Failed to update Stripe customer: {ex.Message}", ex);
        }
    }

    public async Task<bool> DeleteCustomerAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var service = new CustomerService();
            var customer = await ExecuteWithResilienceAsync(
                async ct => await service.DeleteAsync(customerId, cancellationToken: ct),
                cancellationToken);

            _logger.LogInformation("Deleted Stripe customer {CustomerId}", customerId);

            return customer.Deleted == true;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to delete Stripe customer {CustomerId}", customerId);
            return false;
        }
    }

    // ========================================
    // SUBSCRIPTION OPERATIONS
    // ========================================

    public async Task<StripeSubscriptionResult> CreateSubscriptionAsync(
        string customerId,
        string priceId,
        int trialDays = 0,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new SubscriptionCreateOptions
            {
                Customer = customerId,
                Items = new List<SubscriptionItemOptions>
                {
                    new() { Price = priceId }
                },
                Metadata = metadata,
                PaymentBehavior = "default_incomplete",
                PaymentSettings = new SubscriptionPaymentSettingsOptions
                {
                    SaveDefaultPaymentMethod = "on_subscription"
                },
                Expand = new List<string> { "latest_invoice.payment_intent" }
            };

            if (trialDays > 0)
            {
                options.TrialPeriodDays = trialDays;
            }

            var service = new SubscriptionService();
            var subscription = await ExecuteWithResilienceAsync(
                async ct => await service.CreateAsync(options, cancellationToken: ct),
                cancellationToken);

            _logger.LogInformation(
                "Created Stripe subscription {SubscriptionId} for customer {CustomerId} with {TrialDays} trial days",
                subscription.Id, customerId, trialDays);

            return MapToSubscriptionResult(subscription);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to create Stripe subscription for customer {CustomerId}", customerId);
            throw new InvalidOperationException($"Failed to create subscription: {ex.Message}", ex);
        }
    }

    public async Task<StripeSubscriptionResult?> GetSubscriptionAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var service = new SubscriptionService();
            var subscription = await ExecuteWithResilienceAsync(
                async ct => await service.GetAsync(subscriptionId, cancellationToken: ct),
                cancellationToken);

            return MapToSubscriptionResult(subscription);
        }
        catch (StripeException ex) when (ex.StripeError?.Code == "resource_missing")
        {
            return null;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to get Stripe subscription {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    public async Task<StripeSubscriptionResult> UpdateSubscriptionAsync(
        string subscriptionId,
        string newPriceId,
        bool prorationBehavior = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Primero obtenemos la suscripción actual para obtener el item ID
            var getService = new SubscriptionService();
            var currentSubscription = await ExecuteWithResilienceAsync(
                async ct => await getService.GetAsync(subscriptionId, cancellationToken: ct),
                cancellationToken);
            var currentItemId = currentSubscription.Items.Data.First().Id;

            var options = new SubscriptionUpdateOptions
            {
                Items = new List<SubscriptionItemOptions>
                {
                    new()
                    {
                        Id = currentItemId,
                        Price = newPriceId
                    }
                },
                ProrationBehavior = prorationBehavior ? "create_prorations" : "none"
            };

            var subscription = await ExecuteWithResilienceAsync(
                async ct => await getService.UpdateAsync(subscriptionId, options, cancellationToken: ct),
                cancellationToken);

            _logger.LogInformation(
                "Updated Stripe subscription {SubscriptionId} to price {PriceId}",
                subscriptionId, newPriceId);

            return MapToSubscriptionResult(subscription);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to update Stripe subscription {SubscriptionId}", subscriptionId);
            throw new InvalidOperationException($"Failed to update subscription: {ex.Message}", ex);
        }
    }

    public async Task<StripeSubscriptionResult> CancelSubscriptionAsync(
        string subscriptionId,
        bool cancelAtPeriodEnd = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var service = new SubscriptionService();
            Stripe.Subscription subscription;

            if (cancelAtPeriodEnd)
            {
                var options = new SubscriptionUpdateOptions
                {
                    CancelAtPeriodEnd = true
                };
                subscription = await ExecuteWithResilienceAsync(
                    async ct => await service.UpdateAsync(subscriptionId, options, cancellationToken: ct),
                    cancellationToken);
            }
            else
            {
                subscription = await ExecuteWithResilienceAsync(
                    async ct => await service.CancelAsync(subscriptionId, cancellationToken: ct),
                    cancellationToken);
            }

            _logger.LogInformation(
                "Cancelled Stripe subscription {SubscriptionId}, at period end: {AtPeriodEnd}",
                subscriptionId, cancelAtPeriodEnd);

            return MapToSubscriptionResult(subscription);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to cancel Stripe subscription {SubscriptionId}", subscriptionId);
            throw new InvalidOperationException($"Failed to cancel subscription: {ex.Message}", ex);
        }
    }

    public async Task<StripeSubscriptionResult> ReactivateSubscriptionAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new SubscriptionUpdateOptions
            {
                CancelAtPeriodEnd = false
            };

            var service = new SubscriptionService();
            var subscription = await ExecuteWithResilienceAsync(
                async ct => await service.UpdateAsync(subscriptionId, options, cancellationToken: ct),
                cancellationToken);

            _logger.LogInformation("Reactivated Stripe subscription {SubscriptionId}", subscriptionId);

            return MapToSubscriptionResult(subscription);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to reactivate Stripe subscription {SubscriptionId}", subscriptionId);
            throw new InvalidOperationException($"Failed to reactivate subscription: {ex.Message}", ex);
        }
    }

    // ========================================
    // PAYMENT METHOD OPERATIONS
    // ========================================

    public async Task<StripePaymentMethodResult> AttachPaymentMethodAsync(
        string paymentMethodId,
        string customerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new PaymentMethodAttachOptions
            {
                Customer = customerId
            };

            var service = new PaymentMethodService();
            var paymentMethod = await ExecuteWithResilienceAsync(
                async ct => await service.AttachAsync(paymentMethodId, options, cancellationToken: ct),
                cancellationToken);

            _logger.LogInformation(
                "Attached payment method {PaymentMethodId} to customer {CustomerId}",
                paymentMethodId, customerId);

            return MapToPaymentMethodResult(paymentMethod, false);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to attach payment method {PaymentMethodId}", paymentMethodId);
            throw new InvalidOperationException($"Failed to attach payment method: {ex.Message}", ex);
        }
    }

    public async Task<bool> SetDefaultPaymentMethodAsync(
        string customerId,
        string paymentMethodId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new CustomerUpdateOptions
            {
                InvoiceSettings = new CustomerInvoiceSettingsOptions
                {
                    DefaultPaymentMethod = paymentMethodId
                }
            };

            var service = new CustomerService();
            await ExecuteWithResilienceAsync(
                async ct => await service.UpdateAsync(customerId, options, cancellationToken: ct),
                cancellationToken);

            _logger.LogInformation(
                "Set default payment method {PaymentMethodId} for customer {CustomerId}",
                paymentMethodId, customerId);

            return true;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to set default payment method for customer {CustomerId}", customerId);
            return false;
        }
    }

    public async Task<IEnumerable<StripePaymentMethodResult>> GetPaymentMethodsAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new PaymentMethodListOptions
            {
                Customer = customerId,
                Type = "card"
            };

            var service = new PaymentMethodService();
            var paymentMethods = await ExecuteWithResilienceAsync(
                async ct => await service.ListAsync(options, cancellationToken: ct),
                cancellationToken);

            // Obtener el método de pago por defecto del customer
            var customerService = new CustomerService();
            var customer = await ExecuteWithResilienceAsync(
                async ct => await customerService.GetAsync(customerId, cancellationToken: ct),
                cancellationToken);
            var defaultPaymentMethodId = customer.InvoiceSettings?.DefaultPaymentMethodId;

            return paymentMethods.Data.Select(pm =>
                MapToPaymentMethodResult(pm, pm.Id == defaultPaymentMethodId));
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to get payment methods for customer {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<bool> DetachPaymentMethodAsync(
        string paymentMethodId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var service = new PaymentMethodService();
            await ExecuteWithResilienceAsync(
                async ct => await service.DetachAsync(paymentMethodId, cancellationToken: ct),
                cancellationToken);

            _logger.LogInformation("Detached payment method {PaymentMethodId}", paymentMethodId);

            return true;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to detach payment method {PaymentMethodId}", paymentMethodId);
            return false;
        }
    }

    // ========================================
    // INVOICE OPERATIONS
    // ========================================

    public async Task<StripeInvoiceResult?> GetInvoiceAsync(
        string invoiceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var service = new InvoiceService();
            var invoice = await ExecuteWithResilienceAsync(
                async ct => await service.GetAsync(invoiceId, cancellationToken: ct),
                cancellationToken);

            return MapToInvoiceResult(invoice);
        }
        catch (StripeException ex) when (ex.StripeError?.Code == "resource_missing")
        {
            return null;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to get Stripe invoice {InvoiceId}", invoiceId);
            throw;
        }
    }

    public async Task<IEnumerable<StripeInvoiceResult>> GetInvoicesAsync(
        string customerId,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new InvoiceListOptions
            {
                Customer = customerId,
                Limit = limit
            };

            var service = new InvoiceService();
            var invoices = await ExecuteWithResilienceAsync(
                async ct => await service.ListAsync(options, cancellationToken: ct),
                cancellationToken);

            return invoices.Data.Select(MapToInvoiceResult);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to get invoices for customer {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<StripeInvoiceResult> PayInvoiceAsync(
        string invoiceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var service = new InvoiceService();
            var invoice = await ExecuteWithResilienceAsync(
                async ct => await service.PayAsync(invoiceId, cancellationToken: ct),
                cancellationToken);

            _logger.LogInformation("Paid invoice {InvoiceId}", invoiceId);

            return MapToInvoiceResult(invoice);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to pay invoice {InvoiceId}", invoiceId);
            throw new InvalidOperationException($"Failed to pay invoice: {ex.Message}", ex);
        }
    }

    // ========================================
    // CHECKOUT & BILLING PORTAL
    // ========================================

    public async Task<StripeCheckoutResult> CreateCheckoutSessionAsync(
        string customerId,
        string priceId,
        string successUrl,
        string cancelUrl,
        int trialDays = 0,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new Stripe.Checkout.SessionCreateOptions
            {
                Customer = customerId,
                Mode = "subscription",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                LineItems = new List<Stripe.Checkout.SessionLineItemOptions>
                {
                    new()
                    {
                        Price = priceId,
                        Quantity = 1
                    }
                },
                Metadata = metadata
            };

            if (trialDays > 0)
            {
                options.SubscriptionData = new Stripe.Checkout.SessionSubscriptionDataOptions
                {
                    TrialPeriodDays = trialDays
                };
            }

            var service = new Stripe.Checkout.SessionService();
            var session = await ExecuteWithResilienceAsync(
                async ct => await service.CreateAsync(options, cancellationToken: ct),
                cancellationToken);

            _logger.LogInformation(
                "Created checkout session {SessionId} for customer {CustomerId}",
                session.Id, customerId);

            return new StripeCheckoutResult(session.Id, session.Url);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to create checkout session for customer {CustomerId}", customerId);
            throw new InvalidOperationException($"Failed to create checkout session: {ex.Message}", ex);
        }
    }

    public async Task<StripeBillingPortalResult> CreateBillingPortalSessionAsync(
        string customerId,
        string returnUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = customerId,
                ReturnUrl = returnUrl
            };

            var service = new Stripe.BillingPortal.SessionService();
            var session = await ExecuteWithResilienceAsync(
                async ct => await service.CreateAsync(options, cancellationToken: ct),
                cancellationToken);

            _logger.LogInformation(
                "Created billing portal session for customer {CustomerId}",
                customerId);

            return new StripeBillingPortalResult(session.Url);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to create billing portal session for customer {CustomerId}", customerId);
            throw new InvalidOperationException($"Failed to create billing portal session: {ex.Message}", ex);
        }
    }

    // ========================================
    // WEBHOOK HANDLING
    // ========================================

    public StripeWebhookEvent? ConstructWebhookEvent(
        string payload,
        string signature,
        string webhookSecret)
    {
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                payload,
                signature,
                webhookSecret
            );

            return new StripeWebhookEvent(
                stripeEvent.Id,
                stripeEvent.Type,
                stripeEvent.Data.Object,
                stripeEvent.Created
            );
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to construct webhook event");
            return null;
        }
    }

    // ========================================
    // MAPPING HELPERS
    // ========================================

    private static StripeCustomerResult MapToCustomerResult(Customer customer)
    {
        return new StripeCustomerResult(
            customer.Id,
            customer.Email,
            customer.Name ?? "",
            customer.Phone,
            customer.InvoiceSettings?.DefaultPaymentMethodId,
            customer.Balance,
            customer.Currency ?? "usd",
            customer.Deleted == true,
            customer.Created
        );
    }

    private static StripeSubscriptionResult MapToSubscriptionResult(Stripe.Subscription subscription)
    {
        var item = subscription.Items.Data.FirstOrDefault();

        return new StripeSubscriptionResult(
            subscription.Id,
            subscription.CustomerId,
            subscription.Status,
            item?.Price?.Id ?? "",
            (item?.Price?.UnitAmountDecimal ?? 0) / 100m,
            subscription.Currency ?? "usd",
            item?.Price?.Recurring?.Interval ?? "month",
            subscription.CurrentPeriodStart,
            subscription.CurrentPeriodEnd,
            subscription.TrialStart,
            subscription.TrialEnd,
            subscription.CancelAt,
            subscription.CancelAtPeriodEnd,
            subscription.Created
        );
    }

    private static StripePaymentMethodResult MapToPaymentMethodResult(PaymentMethod pm, bool isDefault)
    {
        return new StripePaymentMethodResult(
            pm.Id,
            pm.Type,
            pm.Card?.Brand,
            pm.Card?.Last4,
            (int?)pm.Card?.ExpMonth,
            (int?)pm.Card?.ExpYear,
            isDefault
        );
    }

    private static StripeInvoiceResult MapToInvoiceResult(Invoice invoice)
    {
        return new StripeInvoiceResult(
            invoice.Id,
            invoice.CustomerId,
            invoice.SubscriptionId,
            invoice.Status,
            (invoice.AmountDue) / 100m,
            (invoice.AmountPaid) / 100m,
            invoice.Currency ?? "usd",
            invoice.InvoicePdf,
            invoice.HostedInvoiceUrl,
            invoice.Created,
            invoice.DueDate,
            invoice.StatusTransitions?.PaidAt
        );
    }
}

/// <summary>
/// Configuración de Stripe
/// </summary>
public class StripeSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string PublishableKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;

    // Price IDs para cada plan (configurados en Stripe Dashboard)
    public Dictionary<string, string> PriceIds { get; set; } = new()
    {
        { "basic_monthly", "" },
        { "basic_yearly", "" },
        { "professional_monthly", "" },
        { "professional_yearly", "" },
        { "enterprise_monthly", "" },
        { "enterprise_yearly", "" }
    };

    public int DefaultTrialDays { get; set; } = 14;
}
