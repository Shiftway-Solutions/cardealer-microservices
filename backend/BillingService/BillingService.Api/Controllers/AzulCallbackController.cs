using BillingService.Application.DTOs.Azul;
using BillingService.Application.Configuration;
using BillingService.Application.Interfaces;
using BillingService.Domain.Entities;
using BillingService.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BillingService.Api.Controllers;

[ApiController]
[Route("api/payment/azul/callback")]
public class AzulCallbackController : ControllerBase
{
    private readonly AzulConfiguration _config;
    private readonly IAzulHashGenerator _hashGenerator;
    private readonly IAzulTransactionRepository _transactionRepository;
    private readonly ILogger<AzulCallbackController> _logger;

    public AzulCallbackController(
        IOptions<AzulConfiguration> config,
        IAzulHashGenerator hashGenerator,
        IAzulTransactionRepository transactionRepository,
        ILogger<AzulCallbackController> logger)
    {
        _config = config.Value;
        _hashGenerator = hashGenerator;
        _transactionRepository = transactionRepository;
        _logger = logger;
    }

    [HttpGet("approved")]
    [HttpPost("approved")]
    public async Task<IActionResult> Approved([FromQuery] AzulCallbackQuery query)
    {
        _logger.LogInformation("AZUL Payment Approved - OrderNumber: {OrderNumber}, AzulOrderId: {AzulOrderId}",
            query.OrderNumber, query.AzulOrderId);

        var response = MapToResponse(query);

        // Validar hash de respuesta
        var isValidHash = _hashGenerator.ValidateResponseHash(
            response.OrderNumber,
            response.Amount,
            response.AuthorizationCode,
            response.DateTime,
            response.ResponseCode,
            response.IsoCode,
            response.ResponseMessage,
            response.RRN,
            response.AzulOrderId,
            response.AuthHash,
            _config.AuthKey);

        if (!isValidHash)
        {
            _logger.LogWarning("Hash inválido en respuesta AZUL aprobada - OrderNumber: {OrderNumber}", query.OrderNumber);
            return BadRequest(new { Error = "Hash de respuesta inválido" });
        }

        if (!response.IsApproved)
        {
            _logger.LogWarning("Transacción AZUL no aprobada - OrderNumber: {OrderNumber}, IsoCode: {IsoCode}",
                query.OrderNumber, query.IsoCode);
        }

        // Idempotency: check if this transaction was already processed
        var existingTransaction = await _transactionRepository.GetByOrderNumberAsync(query.OrderNumber ?? string.Empty);
        if (existingTransaction != null && existingTransaction.Status == "Approved")
        {
            _logger.LogInformation("AZUL callback duplicado ignorado - OrderNumber: {OrderNumber} ya procesada como Approved",
                query.OrderNumber);
            var encodedExistingOrder = Uri.EscapeDataString(query.OrderNumber ?? string.Empty);
            return Redirect($"{Request.Scheme}://{Request.Host}/payment/success?orderId={encodedExistingOrder}");
        }

        // Guardar transacción en base de datos
        try
        {
            var transaction = await CreateTransactionFromResponse(response, "Approved");
            await _transactionRepository.CreateAsync(transaction);

            _logger.LogInformation("Transacción AZUL guardada exitosamente - Id: {TransactionId}", transaction.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error guardando transacción AZUL - OrderNumber: {OrderNumber}", query.OrderNumber);
        }

        // TODO: Actualizar estado del pedido
        // TODO: Enviar notificación al usuario

        // Redirigir al frontend con resultado
        // Security: URL-encode external values to prevent XSS/header injection (CWE-79, CWE-113)
        var encodedOrderNumber = Uri.EscapeDataString(query.OrderNumber ?? string.Empty);
        var frontendUrl = $"{Request.Scheme}://{Request.Host}/payment/success?orderId={encodedOrderNumber}";
        return Redirect(frontendUrl);
    }

    [HttpGet("declined")]
    [HttpPost("declined")]
    public async Task<IActionResult> Declined([FromQuery] AzulCallbackQuery query)
    {
        _logger.LogWarning("AZUL Payment Declined - OrderNumber: {OrderNumber}, ResponseMessage: {ResponseMessage}",
            query.OrderNumber, query.ResponseMessage);

        var response = MapToResponse(query);

        // Idempotency: check if this decline was already processed
        var existingDecline = await _transactionRepository.GetByOrderNumberAsync(query.OrderNumber ?? string.Empty);
        if (existingDecline != null && existingDecline.Status == "Declined")
        {
            _logger.LogInformation("AZUL decline callback duplicado ignorado - OrderNumber: {OrderNumber}",
                query.OrderNumber);
            var encodedExistingDecline = Uri.EscapeDataString(query.OrderNumber ?? string.Empty);
            var encodedExistingReason = Uri.EscapeDataString(response.GetDeclineReasonSpanish());
            return Redirect($"{Request.Scheme}://{Request.Host}/payment/declined?orderId={encodedExistingDecline}&reason={encodedExistingReason}");
        }

        // Guardar transacción declinada con razón localizada
        try
        {
            var transaction = await CreateTransactionFromResponse(response, "Declined");
            transaction.DeclineReasonLocalized = response.GetDeclineReasonSpanish();
            await _transactionRepository.CreateAsync(transaction);

            _logger.LogInformation("Transacción AZUL declinada guardada - Id: {TransactionId}, IsoCode: {IsoCode}, Razón: {Reason}",
                transaction.Id, response.IsoCode, transaction.DeclineReasonLocalized);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error guardando transacción declinada - OrderNumber: {OrderNumber}", query.OrderNumber);
        }

        // TODO: Notificar al usuario

        // Security: URL-encode external values to prevent XSS/header injection (CWE-79, CWE-113)
        // Use localized decline reason instead of raw Azul message for better UX
        var encodedOrderNum = Uri.EscapeDataString(query.OrderNumber ?? string.Empty);
        var encodedReason = Uri.EscapeDataString(response.GetDeclineReasonSpanish());
        var frontendUrl = $"{Request.Scheme}://{Request.Host}/payment/declined?orderId={encodedOrderNum}&reason={encodedReason}";
        return Redirect(frontendUrl);
    }

    [HttpGet("cancelled")]
    [HttpPost("cancelled")]
    public async Task<IActionResult> Cancelled([FromQuery] AzulCallbackQuery query)
    {
        _logger.LogInformation("AZUL Payment Cancelled - OrderNumber: {OrderNumber}", query.OrderNumber);

        // Idempotency: check if this cancellation was already processed
        var existingCancel = await _transactionRepository.GetByOrderNumberAsync(query.OrderNumber ?? string.Empty);
        if (existingCancel != null && existingCancel.Status == "Cancelled")
        {
            _logger.LogInformation("AZUL cancel callback duplicado ignorado - OrderNumber: {OrderNumber}",
                query.OrderNumber);
            var encodedExistingCancel = Uri.EscapeDataString(query.OrderNumber ?? string.Empty);
            return Redirect($"{Request.Scheme}://{Request.Host}/payment/cancelled?orderId={encodedExistingCancel}");
        }

        // Guardar transacción cancelada
        try
        {
            var transaction = new AzulTransaction
            {
                OrderNumber = query.OrderNumber ?? string.Empty,
                AzulOrderId = query.AzulOrderId ?? string.Empty,
                Status = "Cancelled",
                Currency = "DOP",
                TransactionDateTime = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers["User-Agent"].ToString()
            };

            await _transactionRepository.CreateAsync(transaction);

            _logger.LogInformation("Transacción AZUL cancelada guardada - Id: {TransactionId}", transaction.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error guardando transacción cancelada - OrderNumber: {OrderNumber}", query.OrderNumber);
        }

        // TODO: Notificar al usuario

        // Security: URL-encode external values to prevent XSS/header injection (CWE-79)
        var encodedCancelledOrderNumber = Uri.EscapeDataString(query.OrderNumber ?? string.Empty);
        var frontendUrl = $"{Request.Scheme}://{Request.Host}/payment/cancelled?orderId={encodedCancelledOrderNumber}";
        return Redirect(frontendUrl);
    }

    private static AzulPaymentResponse MapToResponse(AzulCallbackQuery query)
    {
        return new AzulPaymentResponse
        {
            OrderNumber = query.OrderNumber ?? string.Empty,
            Amount = query.Amount ?? string.Empty,
            ITBIS = query.ITBIS ?? string.Empty,
            AuthorizationCode = query.AuthorizationCode ?? string.Empty,
            DateTime = query.DateTime ?? string.Empty,
            ResponseCode = query.ResponseCode ?? string.Empty,
            IsoCode = query.IsoCode ?? string.Empty,
            ResponseMessage = query.ResponseMessage ?? string.Empty,
            ErrorDescription = query.ErrorDescription ?? string.Empty,
            RRN = query.RRN ?? string.Empty,
            AzulOrderId = query.AzulOrderId ?? string.Empty,
            DataVaultToken = query.DataVaultToken ?? string.Empty,
            DataVaultExpiration = query.DataVaultExpiration ?? string.Empty,
            DataVaultBrand = query.DataVaultBrand ?? string.Empty,
            AuthHash = query.AuthHash ?? string.Empty
        };
    }

    private Task<AzulTransaction> CreateTransactionFromResponse(AzulPaymentResponse response, string status)
    {
        // Parsear DateTime de AZUL (formato: YYYYMMDDHHMMSS)
        DateTime transactionDateTime = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(response.DateTime) && response.DateTime.Length == 14)
        {
            try
            {
                var year = int.Parse(response.DateTime.Substring(0, 4));
                var month = int.Parse(response.DateTime.Substring(4, 2));
                var day = int.Parse(response.DateTime.Substring(6, 2));
                var hour = int.Parse(response.DateTime.Substring(8, 2));
                var minute = int.Parse(response.DateTime.Substring(10, 2));
                var second = int.Parse(response.DateTime.Substring(12, 2));
                transactionDateTime = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
            }
            catch
            {
                _logger.LogWarning("Error parsing AZUL DateTime: {DateTime}", response.DateTime);
            }
        }

        // Parsear montos (vienen sin decimales, ej: "100000" = $1,000.00)
        decimal amount = 0;
        decimal itbis = 0;
        if (decimal.TryParse(response.Amount, out var amountParsed))
        {
            amount = amountParsed / 100;
        }
        if (decimal.TryParse(response.ITBIS, out var itbisParsed))
        {
            itbis = itbisParsed / 100;
        }

        var transaction = new AzulTransaction
        {
            OrderNumber = response.OrderNumber,
            AzulOrderId = response.AzulOrderId,
            Amount = amount,
            ITBIS = itbis,
            AuthorizationCode = response.AuthorizationCode,
            ResponseCode = response.ResponseCode,
            IsoCode = response.IsoCode,
            ResponseMessage = response.ResponseMessage,
            ErrorDescription = response.ErrorDescription,
            RRN = response.RRN,
            TransactionDateTime = transactionDateTime,
            Status = status,
            Currency = "DOP", // Azul always processes in DOP (ISO 4217 code 214)
            DataVaultToken = response.DataVaultToken,
            DataVaultExpiration = response.DataVaultExpiration,
            DataVaultBrand = response.DataVaultBrand,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers["User-Agent"].ToString()
        };

        return Task.FromResult(transaction);
    }
}

public record AzulCallbackQuery
{
    public string? OrderNumber { get; init; }
    public string? Amount { get; init; }
    public string? ITBIS { get; init; }
    public string? AuthorizationCode { get; init; }
    public string? DateTime { get; init; }
    public string? ResponseCode { get; init; }
    public string? IsoCode { get; init; }
    public string? ResponseMessage { get; init; }
    public string? ErrorDescription { get; init; }
    public string? RRN { get; init; }
    public string? AzulOrderId { get; init; }
    public string? DataVaultToken { get; init; }
    public string? DataVaultExpiration { get; init; }
    public string? DataVaultBrand { get; init; }
    public string? AuthHash { get; init; }
}
