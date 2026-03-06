using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using BillingService.Domain.Interfaces;
using BillingService.Infrastructure.Services;
using BillingService.Infrastructure.External;
using BillingService.Infrastructure.Messaging;
using CarDealer.Contracts.Events.Billing;
using Stripe;
using DomainEntities = BillingService.Domain.Entities;

namespace BillingService.Api.Controllers;

/// <summary>
/// Controlador para manejar webhooks de Stripe
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous] // Los webhooks no requieren autenticación, se verifican con firma
public class StripeWebhooksController : ControllerBase
{
    private readonly ILogger<StripeWebhooksController> _logger;
    private readonly IStripeService _stripeService;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IStripeCustomerRepository _customerRepository;
    private readonly IUserServiceClient _userServiceClient;
    private readonly IEventPublisher _eventPublisher;
    private readonly StripeSettings _stripeSettings;

    public StripeWebhooksController(
        ILogger<StripeWebhooksController> logger,
        IStripeService stripeService,
        ISubscriptionRepository subscriptionRepository,
        IInvoiceRepository invoiceRepository,
        IPaymentRepository paymentRepository,
        IStripeCustomerRepository customerRepository,
        IUserServiceClient userServiceClient,
        IEventPublisher eventPublisher,
        IOptions<StripeSettings> stripeSettings)
    {
        _logger = logger;
        _stripeService = stripeService;
        _subscriptionRepository = subscriptionRepository;
        _invoiceRepository = invoiceRepository;
        _paymentRepository = paymentRepository;
        _customerRepository = customerRepository;
        _userServiceClient = userServiceClient;
        _eventPublisher = eventPublisher;
        _stripeSettings = stripeSettings.Value;
    }

    /// <summary>
    /// Endpoint para recibir webhooks de Stripe
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> HandleWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        try
        {
            var stripeSignature = Request.Headers["Stripe-Signature"].ToString();

            var stripeEvent = EventUtility.ConstructEvent(
                json,
                stripeSignature,
                _stripeSettings.WebhookSecret
            );

            _logger.LogInformation(
                "Received Stripe webhook: {EventType} ({EventId})",
                stripeEvent.Type, stripeEvent.Id);

            // Procesar el evento según su tipo
            await ProcessEventAsync(stripeEvent);

            return Ok(new { received = true });
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error processing Stripe webhook");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing Stripe webhook");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    private async Task ProcessEventAsync(Event stripeEvent)
    {
        switch (stripeEvent.Type)
        {
            // ========================================
            // CUSTOMER EVENTS
            // ========================================
            case "customer.created":
                await HandleCustomerCreated(stripeEvent);
                break;

            case "customer.updated":
                await HandleCustomerUpdated(stripeEvent);
                break;

            case "customer.deleted":
                await HandleCustomerDeleted(stripeEvent);
                break;

            // ========================================
            // SUBSCRIPTION EVENTS
            // ========================================
            case "customer.subscription.created":
                await HandleSubscriptionCreated(stripeEvent);
                break;

            case "customer.subscription.updated":
                await HandleSubscriptionUpdated(stripeEvent);
                break;

            case "customer.subscription.deleted":
                await HandleSubscriptionDeleted(stripeEvent);
                break;

            case "customer.subscription.trial_will_end":
                await HandleTrialWillEnd(stripeEvent);
                break;

            // ========================================
            // INVOICE EVENTS
            // ========================================
            case "invoice.created":
                await HandleInvoiceCreated(stripeEvent);
                break;

            case "invoice.paid":
                await HandleInvoicePaid(stripeEvent);
                break;

            case "invoice.payment_failed":
                await HandleInvoicePaymentFailed(stripeEvent);
                break;

            case "invoice.upcoming":
                await HandleInvoiceUpcoming(stripeEvent);
                break;

            // ========================================
            // PAYMENT INTENT EVENTS
            // ========================================
            case "payment_intent.succeeded":
                await HandlePaymentIntentSucceeded(stripeEvent);
                break;

            case "payment_intent.payment_failed":
                await HandlePaymentIntentFailed(stripeEvent);
                break;

            // ========================================
            // CHECKOUT SESSION EVENTS
            // ========================================
            case "checkout.session.completed":
                await HandleCheckoutSessionCompleted(stripeEvent);
                break;

            default:
                _logger.LogInformation("Unhandled event type: {EventType}", stripeEvent.Type);
                break;
        }
    }

    // ========================================
    // CUSTOMER EVENT HANDLERS
    // ========================================

    private async Task HandleCustomerCreated(Event stripeEvent)
    {
        var customer = stripeEvent.Data.Object as Customer;
        if (customer == null) return;

        _logger.LogInformation(
            "Customer created in Stripe: {CustomerId} - {Email}",
            customer.Id, customer.Email);

        // Verificar si ya existe en nuestra base de datos
        var existingCustomer = await _customerRepository.GetByStripeCustomerIdAsync(customer.Id);
        if (existingCustomer != null)
        {
            _logger.LogInformation("Customer {CustomerId} already exists in database", customer.Id);
            return;
        }

        // Si tiene dealerId en metadata, lo creamos
        if (customer.Metadata?.TryGetValue("dealer_id", out var dealerIdStr) == true
            && Guid.TryParse(dealerIdStr, out var dealerId))
        {
            var newCustomer = new DomainEntities.StripeCustomer(
                dealerId,
                customer.Id,
                customer.Email,
                customer.Name ?? "",
                customer.Phone,
                !_stripeSettings.SecretKey.StartsWith("sk_live")
            );

            await _customerRepository.AddAsync(newCustomer);
            _logger.LogInformation("Created customer record for dealer {DealerId}", dealerId);
        }
    }

    private async Task HandleCustomerUpdated(Event stripeEvent)
    {
        var customer = stripeEvent.Data.Object as Customer;
        if (customer == null) return;

        var existingCustomer = await _customerRepository.GetByStripeCustomerIdAsync(customer.Id);
        if (existingCustomer == null)
        {
            _logger.LogWarning("Customer {CustomerId} not found in database", customer.Id);
            return;
        }

        existingCustomer.UpdateFromStripe(
            customer.Email,
            customer.Name ?? "",
            customer.Phone,
            customer.Balance
        );

        if (!string.IsNullOrEmpty(customer.InvoiceSettings?.DefaultPaymentMethodId))
        {
            existingCustomer.SetDefaultPaymentMethod(customer.InvoiceSettings.DefaultPaymentMethodId);
        }

        await _customerRepository.UpdateAsync(existingCustomer);
        _logger.LogInformation("Updated customer {CustomerId}", customer.Id);
    }

    private async Task HandleCustomerDeleted(Event stripeEvent)
    {
        var customer = stripeEvent.Data.Object as Customer;
        if (customer == null) return;

        var existingCustomer = await _customerRepository.GetByStripeCustomerIdAsync(customer.Id);
        if (existingCustomer == null) return;

        existingCustomer.Deactivate();
        await _customerRepository.UpdateAsync(existingCustomer);
        _logger.LogInformation("Deactivated customer {CustomerId}", customer.Id);
    }

    // ========================================
    // SUBSCRIPTION EVENT HANDLERS
    // ========================================

    private async Task HandleSubscriptionCreated(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (subscription == null) return;

        _logger.LogInformation(
            "Subscription created: {SubscriptionId} for customer {CustomerId}",
            subscription.Id, subscription.CustomerId);

        // Buscar el customer para obtener el dealerId
        var customer = await _customerRepository.GetByStripeCustomerIdAsync(subscription.CustomerId);
        if (customer == null)
        {
            _logger.LogWarning("Customer {CustomerId} not found for subscription", subscription.CustomerId);
            return;
        }

        // Mapear plan de Stripe a nuestro plan
        var plan = MapStripePriceToSubscriptionPlan(subscription.Items.Data.FirstOrDefault()?.Price?.Id);
        var cycle = MapStripeIntervalToBillingCycle(subscription.Items.Data.FirstOrDefault()?.Price?.Recurring?.Interval);
        var price = (subscription.Items.Data.FirstOrDefault()?.Price?.UnitAmountDecimal ?? 0) / 100m;

        var trialDays = 0;
        if (subscription.TrialEnd.HasValue)
        {
            trialDays = (int)(subscription.TrialEnd.Value - DateTime.UtcNow).TotalDays;
        }

        // Crear suscripción en nuestra base de datos
        var newSubscription = new Domain.Entities.Subscription(
            customer.DealerId,
            plan,
            cycle,
            price,
            GetMaxUsersForPlan(plan),
            GetMaxVehiclesForPlan(plan),
            trialDays
        );

        newSubscription.SetStripeInfo(subscription.CustomerId, subscription.Id);

        await _subscriptionRepository.AddAsync(newSubscription);
        _logger.LogInformation(
            "Created subscription {SubscriptionId} for dealer {DealerId}",
            newSubscription.Id, customer.DealerId);

        // Sincronizar con UserService
        await _userServiceClient.UpdateStripeSubscriptionAsync(
            customer.DealerId,
            subscription.Id,
            plan.ToString(),
            subscription.Status,
            subscription.TrialEnd,
            subscription.CurrentPeriodEnd);
    }

    private async Task HandleSubscriptionUpdated(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (subscription == null) return;

        var existingSubscription = await _subscriptionRepository.GetByStripeSubscriptionIdAsync(subscription.Id);
        if (existingSubscription == null)
        {
            _logger.LogWarning("Subscription {SubscriptionId} not found", subscription.Id);
            return;
        }

        // Actualizar estado según Stripe
        switch (subscription.Status)
        {
            case "active":
                existingSubscription.Activate();
                break;
            case "past_due":
                existingSubscription.MarkPastDue();
                break;
            case "canceled":
            case "unpaid":
                existingSubscription.Cancel("Subscription cancelled in Stripe");
                break;
            case "trialing":
                // El trial sigue activo, no hacer nada
                break;
        }

        // Si cambió el plan (upgrade/downgrade)
        var currentPriceId = subscription.Items.Data.FirstOrDefault()?.Price?.Id;
        if (!string.IsNullOrEmpty(currentPriceId))
        {
            var newPlan = MapStripePriceToSubscriptionPlan(currentPriceId);
            var newPrice = (subscription.Items.Data.FirstOrDefault()?.Price?.UnitAmountDecimal ?? 0) / 100m;

            existingSubscription.Upgrade(
                newPlan,
                newPrice,
                GetMaxUsersForPlan(newPlan),
                GetMaxVehiclesForPlan(newPlan)
            );
        }

        await _subscriptionRepository.UpdateAsync(existingSubscription);
        _logger.LogInformation("Updated subscription {SubscriptionId}", subscription.Id);

        // Sincronizar con UserService
        var plan = MapStripePriceToSubscriptionPlan(subscription.Items.Data.FirstOrDefault()?.Price?.Id);
        await _userServiceClient.UpdateStripeSubscriptionAsync(
            existingSubscription.DealerId,
            subscription.Id,
            plan.ToString(),
            subscription.Status,
            subscription.TrialEnd,
            subscription.CurrentPeriodEnd);
    }

    private async Task HandleSubscriptionDeleted(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (subscription == null) return;

        var existingSubscription = await _subscriptionRepository.GetByStripeSubscriptionIdAsync(subscription.Id);
        if (existingSubscription == null) return;

        existingSubscription.Cancel("Subscription deleted in Stripe");
        await _subscriptionRepository.UpdateAsync(existingSubscription);

        _logger.LogInformation("Cancelled subscription {SubscriptionId}", subscription.Id);

        // Sincronizar cancelación con UserService
        await _userServiceClient.UpdateStripeSubscriptionAsync(
            existingSubscription.DealerId,
            subscription.Id,
            "free",  // Revertir a plan free
            "canceled",
            null,
            null);
    }

    private async Task HandleTrialWillEnd(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (subscription == null) return;

        // Notificar al dealer que su trial está por terminar
        _logger.LogInformation(
            "Trial will end for subscription {SubscriptionId} on {TrialEnd}",
            subscription.Id, subscription.TrialEnd);

        // TODO: Enviar notificación al dealer
        // await _notificationService.SendTrialEndingNotificationAsync(dealerId);
        await Task.CompletedTask;
    }

    // ========================================
    // INVOICE EVENT HANDLERS
    // ========================================

    private async Task HandleInvoiceCreated(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Stripe.Invoice;
        if (invoice == null) return;

        var customer = await _customerRepository.GetByStripeCustomerIdAsync(invoice.CustomerId);
        if (customer == null) return;

        var newInvoice = new Domain.Entities.Invoice(
            customer.DealerId,
            invoice.Number ?? $"INV-{invoice.Id[..8].ToUpper()}",
            (invoice.Subtotal) / 100m,
            (invoice.Tax ?? 0) / 100m,
            invoice.DueDate ?? DateTime.UtcNow.AddDays(30)
        );

        newInvoice.SetStripeInfo(invoice.Id, invoice.InvoicePdf);
        newInvoice.Issue();

        await _invoiceRepository.AddAsync(newInvoice);
        _logger.LogInformation("Created invoice {InvoiceId} for dealer {DealerId}", newInvoice.Id, customer.DealerId);

        // Publicar evento de factura generada para que NotificationService envíe el email
        try
        {
            await _eventPublisher.PublishAsync(new InvoiceGeneratedEvent
            {
                InvoiceId = newInvoice.Id,
                InvoiceNumber = newInvoice.InvoiceNumber,
                PaymentTransactionId = Guid.Empty, // Se asociará cuando se pague
                UserId = customer.DealerId,
                DealerId = customer.DealerId,
                BuyerName = customer.Name ?? "Cliente",
                BuyerEmail = customer.Email ?? string.Empty,
                TotalAmount = newInvoice.TotalAmount,
                Currency = newInvoice.Currency,
                Description = $"Factura {newInvoice.InvoiceNumber}",
                PdfUrl = invoice.InvoicePdf,
                IssuedAt = DateTime.UtcNow
            });

            _logger.LogInformation(
                "InvoiceGeneratedEvent published for InvoiceId: {InvoiceId}, DealerId: {DealerId}",
                newInvoice.Id, customer.DealerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish InvoiceGeneratedEvent for InvoiceId: {InvoiceId}", newInvoice.Id);
            // No fallar el webhook si la publicación del evento falla
        }
    }

    private async Task HandleInvoicePaid(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Stripe.Invoice;
        if (invoice == null) return;

        var existingInvoice = await _invoiceRepository.GetByStripeInvoiceIdAsync(invoice.Id);
        if (existingInvoice != null)
        {
            existingInvoice.RecordPayment((invoice.AmountPaid) / 100m);
            await _invoiceRepository.UpdateAsync(existingInvoice);

            // Si la factura se creó directamente como pagada (sin pasar por invoice.created),
            // publicar el evento de factura para que el usuario reciba el email
            var customer = await _customerRepository.GetByStripeCustomerIdAsync(invoice.CustomerId);
            if (customer != null)
            {
                try
                {
                    await _eventPublisher.PublishAsync(new InvoiceGeneratedEvent
                    {
                        InvoiceId = existingInvoice.Id,
                        InvoiceNumber = existingInvoice.InvoiceNumber,
                        PaymentTransactionId = Guid.Empty,
                        UserId = customer.DealerId,
                        DealerId = customer.DealerId,
                        BuyerName = customer.Name ?? "Cliente",
                        BuyerEmail = customer.Email ?? string.Empty,
                        TotalAmount = existingInvoice.TotalAmount,
                        Currency = existingInvoice.Currency,
                        Description = $"Factura {existingInvoice.InvoiceNumber} - Pagada",
                        PdfUrl = invoice.InvoicePdf,
                        IssuedAt = existingInvoice.IssueDate
                    });

                    _logger.LogInformation(
                        "InvoiceGeneratedEvent (paid) published for InvoiceId: {InvoiceId}",
                        existingInvoice.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to publish InvoiceGeneratedEvent for paid invoice: {InvoiceId}", existingInvoice.Id);
                }
            }
        }

        // Activar/renovar suscripción si aplica
        if (!string.IsNullOrEmpty(invoice.SubscriptionId))
        {
            var subscription = await _subscriptionRepository.GetByStripeSubscriptionIdAsync(invoice.SubscriptionId);
            if (subscription != null)
            {
                subscription.Activate();
                subscription.RenewBilling();
                await _subscriptionRepository.UpdateAsync(subscription);
            }
        }

        _logger.LogInformation("Invoice {InvoiceId} paid", invoice.Id);
    }

    private async Task HandleInvoicePaymentFailed(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Stripe.Invoice;
        if (invoice == null) return;

        _logger.LogWarning(
            "Payment failed for invoice {InvoiceId}, customer {CustomerId}",
            invoice.Id, invoice.CustomerId);

        // Marcar suscripción como past_due
        if (!string.IsNullOrEmpty(invoice.SubscriptionId))
        {
            var subscription = await _subscriptionRepository.GetByStripeSubscriptionIdAsync(invoice.SubscriptionId);
            if (subscription != null)
            {
                subscription.MarkPastDue();
                await _subscriptionRepository.UpdateAsync(subscription);
            }
        }

        // TODO: Enviar notificación al dealer
    }

    private async Task HandleInvoiceUpcoming(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Stripe.Invoice;
        if (invoice == null) return;

        _logger.LogInformation(
            "Upcoming invoice for customer {CustomerId}: {Amount}",
            invoice.CustomerId, invoice.AmountDue / 100m);

        // TODO: Enviar notificación de factura próxima
        await Task.CompletedTask;
    }

    // ========================================
    // PAYMENT INTENT EVENT HANDLERS
    // ========================================

    private async Task HandlePaymentIntentSucceeded(Event stripeEvent)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        if (paymentIntent == null) return;

        _logger.LogInformation(
            "Payment succeeded: {PaymentIntentId} - {Amount} {Currency}",
            paymentIntent.Id, paymentIntent.Amount / 100m, paymentIntent.Currency);

        var customer = await _customerRepository.GetByStripeCustomerIdAsync(paymentIntent.CustomerId);
        if (customer == null) return;

        // Registrar el pago
        var payment = new Domain.Entities.Payment(
            customer.DealerId,
            paymentIntent.Amount / 100m,
            DomainEntities.PaymentMethod.CreditCard,
            paymentIntent.Description
        );

        payment.SetStripeInfo(
            paymentIntent.Id,
            paymentIntent.LatestChargeId,
            paymentIntent.LatestCharge?.ReceiptUrl
        );
        payment.MarkSucceeded(paymentIntent.LatestCharge?.ReceiptUrl);

        await _paymentRepository.AddAsync(payment);

        // Publicar evento de pago completado
        try
        {
            // Obtener información del usuario (en producción esto vendría de UserService)
            var userEmail = customer.Email ?? "unknown@example.com";
            var userName = customer.Name ?? "Unknown User";

            await _eventPublisher.PublishAsync(new PaymentCompletedEvent
            {
                PaymentId = payment.Id,
                UserId = customer.DealerId,
                UserEmail = userEmail,
                UserName = userName,
                Amount = payment.Amount,
                Currency = paymentIntent.Currency.ToUpper(),
                StripePaymentIntentId = paymentIntent.Id,
                Description = payment.Description ?? string.Empty,
                SubscriptionPlan = null, // TODO: Agregar plan de suscripción si aplica
                PaidAt = DateTime.UtcNow
            });

            _logger.LogInformation(
                "PaymentCompletedEvent published for PaymentId: {PaymentId}, StripePaymentIntentId: {StripeId}",
                payment.Id, paymentIntent.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish PaymentCompletedEvent for PaymentIntentId: {PaymentIntentId}", paymentIntent.Id);
            // No fallar el webhook si la publicación del evento falla
        }
    }

    private async Task HandlePaymentIntentFailed(Event stripeEvent)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        if (paymentIntent == null) return;

        _logger.LogWarning(
            "Payment failed: {PaymentIntentId} - {Error}",
            paymentIntent.Id, paymentIntent.LastPaymentError?.Message);

        var customer = await _customerRepository.GetByStripeCustomerIdAsync(paymentIntent.CustomerId);
        if (customer == null) return;

        var payment = new Domain.Entities.Payment(
            customer.DealerId,
            paymentIntent.Amount / 100m,
            DomainEntities.PaymentMethod.CreditCard,
            paymentIntent.Description
        );

        payment.SetStripeInfo(paymentIntent.Id);
        payment.MarkFailed(paymentIntent.LastPaymentError?.Message ?? "Payment failed");

        await _paymentRepository.AddAsync(payment);
    }

    // ========================================
    // CHECKOUT SESSION EVENT HANDLERS
    // ========================================

    private async Task HandleCheckoutSessionCompleted(Event stripeEvent)
    {
        var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
        if (session == null) return;

        _logger.LogInformation(
            "Checkout session completed: {SessionId} for customer {CustomerId}",
            session.Id, session.CustomerId);

        // La suscripción ya se crea automáticamente vía customer.subscription.created
        // Aquí podemos hacer acciones adicionales post-checkout

        // TODO: Enviar email de bienvenida
        // TODO: Activar módulos según el plan comprado
        await Task.CompletedTask;
    }

    // ========================================
    // HELPER METHODS
    // ========================================

    private DomainEntities.SubscriptionPlan MapStripePriceToSubscriptionPlan(string? priceId)
    {
        if (string.IsNullOrEmpty(priceId)) return DomainEntities.SubscriptionPlan.Free;

        // Mapear según los Price IDs configurados
        if (priceId.Contains("basic")) return DomainEntities.SubscriptionPlan.Basic;
        if (priceId.Contains("professional") || priceId.Contains("pro")) return DomainEntities.SubscriptionPlan.Professional;
        if (priceId.Contains("enterprise")) return DomainEntities.SubscriptionPlan.Enterprise;

        return DomainEntities.SubscriptionPlan.Free;
    }

    private DomainEntities.BillingCycle MapStripeIntervalToBillingCycle(string? interval)
    {
        return interval switch
        {
            "month" => DomainEntities.BillingCycle.Monthly,
            "year" => DomainEntities.BillingCycle.Yearly,
            _ => DomainEntities.BillingCycle.Monthly
        };
    }

    private int GetMaxUsersForPlan(DomainEntities.SubscriptionPlan plan)
    {
        return plan switch
        {
            DomainEntities.SubscriptionPlan.Free => 1,
            DomainEntities.SubscriptionPlan.Basic => 5,
            DomainEntities.SubscriptionPlan.Professional => 20,
            DomainEntities.SubscriptionPlan.Enterprise => -1, // Ilimitado
            _ => 1
        };
    }

    private int GetMaxVehiclesForPlan(DomainEntities.SubscriptionPlan plan)
    {
        return plan switch
        {
            DomainEntities.SubscriptionPlan.Free => 3,
            DomainEntities.SubscriptionPlan.Basic => 50,
            DomainEntities.SubscriptionPlan.Professional => 500,
            DomainEntities.SubscriptionPlan.Enterprise => -1, // Ilimitado
            _ => 3
        };
    }
}
