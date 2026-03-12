using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BillingService.Domain.Entities;
using BillingService.Infrastructure.Persistence;

namespace BillingService.Api.Controllers;

/// <summary>
/// Controlador para el sistema de créditos OKLA Coins.
/// Permite comprar paquetes, consultar balance, y ver historial de transacciones.
/// AUDIT FIX: Rewritten to use real database persistence instead of hardcoded mock data.
/// </summary>
[ApiController]
[Route("api/okla-coins")]
[Authorize]
public class OklaCoinsController : BillingBaseController
{
    private readonly BillingDbContext _context;
    private readonly ILogger<OklaCoinsController> _logger;

    public OklaCoinsController(BillingDbContext context, ILogger<OklaCoinsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ========================================
    // PACKAGES (Public)
    // ========================================

    /// <summary>
    /// Obtiene todos los paquetes de OKLA Coins disponibles
    /// </summary>
    [HttpGet("packages")]
    [AllowAnonymous]
    public ActionResult<CoinsPackagesResponse> GetPackages()
    {
        var packages = OklaCoinPackageCatalog.GetAll()
            .Where(p => p.IsActive)
            .OrderBy(p => p.DisplayOrder)
            .Select(p => new CoinPackageDto(
                Id: p.Id.ToString(),
                Slug: p.Slug,
                Name: p.Name,
                BaseCredits: p.BaseCredits,
                BonusCredits: p.BonusCredits,
                TotalCredits: p.TotalCredits,
                BonusPercentage: p.BonusPercentage,
                PriceUsd: p.PriceUsd,
                CostPerCoin: Math.Round(p.CostPerCoin, 5),
                BadgeText: p.BadgeText
            ))
            .ToList();

        return Ok(new CoinsPackagesResponse(
            Packages: packages,
            ConversionRate: "1 OKLA Coin = $0.01 USD",
            TotalPackages: packages.Count
        ));
    }

    // ========================================
    // WALLET
    // ========================================

    /// <summary>
    /// Obtiene el balance de OKLA Coins del dealer
    /// </summary>
    [HttpGet("wallet")]
    public async Task<ActionResult<WalletDto>> GetWallet()
    {
        var dealerId = GetDealerIdFromJwt();
        _logger.LogInformation("Getting OKLA Coins wallet for dealer {DealerId}", dealerId);

        var wallet = await _context.OklaCoinsWallets
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.DealerId == dealerId);

        if (wallet == null)
        {
            // Auto-create wallet for first-time access
            wallet = new OklaCoinsWallet
            {
                Id = Guid.NewGuid(),
                DealerId = dealerId,
                Balance = 0,
                TotalPurchased = 0,
                TotalBonus = 0,
                TotalFromPlan = 0,
                TotalSpent = 0,
                Currency = "USD",
                CreatedAt = DateTime.UtcNow
            };
            _context.OklaCoinsWallets.Add(wallet);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Created new OKLA Coins wallet for dealer {DealerId}", dealerId);
        }

        return Ok(new WalletDto(
            DealerId: wallet.DealerId.ToString(),
            Balance: wallet.Balance,
            TotalPurchased: wallet.TotalPurchased,
            TotalBonus: wallet.TotalBonus,
            TotalFromPlan: wallet.TotalFromPlan,
            TotalSpent: wallet.TotalSpent,
            Currency: wallet.Currency,
            BalanceUsd: wallet.Balance * 0.01m, // 1 coin = $0.01 USD
            LastPlanCreditDate: wallet.LastPlanCreditDate?.ToString("o")
        ));
    }

    /// <summary>
    /// Compra un paquete de OKLA Coins
    /// </summary>
    [HttpPost("purchase")]
    public async Task<ActionResult<CoinPurchaseResult>> PurchasePackage(
        [FromBody] PurchasePackageRequest request)
    {
        var dealerId = GetDealerIdFromJwt();
        var package = OklaCoinPackageCatalog.GetBySlug(request.PackageSlug);
        if (package == null)
            return NotFound(new { error = $"Package '{request.PackageSlug}' not found" });

        _logger.LogInformation(
            "Dealer {DealerId} purchasing OKLA Coins package {PackageSlug} ({TotalCredits} coins for ${Price})",
            dealerId, package.Slug, package.TotalCredits, package.PriceUsd);

        // Get or create wallet
        var wallet = await _context.OklaCoinsWallets
            .FirstOrDefaultAsync(w => w.DealerId == dealerId);

        if (wallet == null)
        {
            wallet = new OklaCoinsWallet
            {
                Id = Guid.NewGuid(),
                DealerId = dealerId,
                Balance = 0,
                CreatedAt = DateTime.UtcNow
            };
            _context.OklaCoinsWallets.Add(wallet);
        }

        // Credit base coins
        wallet.AddCoins(package.BaseCredits, CoinTransactionType.Purchase, $"Compra {package.Name}");

        // Record base transaction
        var baseTx = new OklaCoinsTransaction
        {
            Id = Guid.NewGuid(),
            WalletId = wallet.Id,
            DealerId = dealerId,
            Type = CoinTransactionType.Purchase,
            Amount = package.BaseCredits,
            BalanceAfter = wallet.Balance,
            Description = $"Compra {package.Name}",
            PackageSlug = package.Slug,
            AmountUsd = package.PriceUsd,
            CreatedAt = DateTime.UtcNow
        };
        _context.OklaCoinsTransactions.Add(baseTx);

        // Credit bonus coins if any
        if (package.BonusCredits > 0)
        {
            wallet.AddCoins(package.BonusCredits, CoinTransactionType.Bonus, $"Bonus {package.BonusPercentage}% - {package.Name}");

            var bonusTx = new OklaCoinsTransaction
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                DealerId = dealerId,
                Type = CoinTransactionType.Bonus,
                Amount = package.BonusCredits,
                BalanceAfter = wallet.Balance,
                Description = $"Bonus {package.BonusPercentage}% - {package.Name}",
                PackageSlug = package.Slug,
                CreatedAt = DateTime.UtcNow
            };
            _context.OklaCoinsTransactions.Add(bonusTx);
        }

        await _context.SaveChangesAsync();

        // TODO: Create payment via Stripe/Azul and publish billing.credits.purchased event

        return Ok(new CoinPurchaseResult(
            TransactionId: baseTx.Id.ToString(),
            PackageSlug: package.Slug,
            PackageName: package.Name,
            BaseCredits: package.BaseCredits,
            BonusCredits: package.BonusCredits,
            TotalCredits: package.TotalCredits,
            AmountCharged: package.PriceUsd,
            Currency: "USD",
            NewBalance: wallet.Balance,
            Status: "completed",
            Message: $"¡{package.TotalCredits:N0} OKLA Coins acreditados exitosamente!"
        ));
    }

    /// <summary>
    /// Gasta OKLA Coins en un producto publicitario
    /// </summary>
    [HttpPost("spend")]
    public async Task<ActionResult<CoinSpendResult>> SpendCoins(
        [FromBody] SpendCoinsRequest request)
    {
        var dealerId = GetDealerIdFromJwt();
        _logger.LogInformation(
            "Dealer {DealerId} spending {Amount} OKLA Coins on product {ProductSlug}",
            dealerId, request.Amount, request.ProductSlug);

        if (request.Amount <= 0)
            return BadRequest(new { error = "Amount must be positive" });

        var wallet = await _context.OklaCoinsWallets
            .FirstOrDefaultAsync(w => w.DealerId == dealerId);

        if (wallet == null)
            return BadRequest(new { error = "No tienes una billetera de OKLA Coins. Compra un paquete primero." });

        if (!wallet.HasSufficientBalance(request.Amount))
            return BadRequest(new
            {
                error = $"Balance insuficiente. Tienes {wallet.Balance} coins pero necesitas {request.Amount}.",
                currentBalance = wallet.Balance,
                required = request.Amount
            });

        // Debit wallet
        var success = wallet.SpendCoins(request.Amount, $"Gasto en {request.ProductSlug}");
        if (!success)
            return BadRequest(new { error = "No se pudo procesar el gasto. Balance insuficiente." });

        // Record transaction
        var tx = new OklaCoinsTransaction
        {
            Id = Guid.NewGuid(),
            WalletId = wallet.Id,
            DealerId = dealerId,
            Type = CoinTransactionType.Spend,
            Amount = -request.Amount,
            BalanceAfter = wallet.Balance,
            Description = $"Gasto en {request.ProductSlug}",
            AdvertisingProductId = request.VehicleId, // Reference to the vehicle being promoted
            CreatedAt = DateTime.UtcNow
        };
        _context.OklaCoinsTransactions.Add(tx);

        await _context.SaveChangesAsync();

        // TODO: Create ad campaign or activate product via AdvertisingService event

        return Ok(new CoinSpendResult(
            TransactionId: tx.Id.ToString(),
            AmountSpent: request.Amount,
            ProductSlug: request.ProductSlug,
            NewBalance: wallet.Balance,
            Status: "completed",
            Message: $"{request.Amount:N0} OKLA Coins utilizados exitosamente"
        ));
    }

    // ========================================
    // TRANSACTIONS
    // ========================================

    /// <summary>
    /// Obtiene el historial de transacciones de OKLA Coins
    /// </summary>
    [HttpGet("transactions")]
    public async Task<ActionResult<CoinsTransactionsResponse>> GetTransactions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var dealerId = GetDealerIdFromJwt();
        _logger.LogInformation("Getting OKLA Coins transactions for dealer {DealerId}", dealerId);

        var query = _context.OklaCoinsTransactions
            .AsNoTracking()
            .Where(t => t.DealerId == dealerId)
            .OrderByDescending(t => t.CreatedAt);

        var totalCount = await query.CountAsync();

        var transactions = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new CoinTransactionDto(
                t.Id.ToString(),
                t.Type.ToString().ToLower(),
                t.Amount,
                t.BalanceAfter,
                t.Description,
                t.PackageSlug,
                t.AmountUsd,
                t.CreatedAt.ToString("o")
            ))
            .ToListAsync();

        return Ok(new CoinsTransactionsResponse(
            Transactions: transactions,
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize
        ));
    }

    /// <summary>
    /// Admin: Acredita OKLA Coins mensuales del plan a un dealer
    /// </summary>
    [HttpPost("admin/plan-credit")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<CoinPurchaseResult>> CreditPlanCoins(
        [FromBody] PlanCreditRequest request)
    {
        _logger.LogInformation(
            "Admin crediting {Amount} OKLA Coins to dealer {DealerId} from plan",
            request.CoinsAmount, request.DealerId);

        var wallet = await _context.OklaCoinsWallets
            .FirstOrDefaultAsync(w => w.DealerId == request.DealerId);

        if (wallet == null)
        {
            wallet = new OklaCoinsWallet
            {
                Id = Guid.NewGuid(),
                DealerId = request.DealerId,
                Balance = 0,
                CreatedAt = DateTime.UtcNow
            };
            _context.OklaCoinsWallets.Add(wallet);
        }

        wallet.AddCoins(request.CoinsAmount, CoinTransactionType.PlanCredit, "Créditos mensuales del plan");

        var tx = new OklaCoinsTransaction
        {
            Id = Guid.NewGuid(),
            WalletId = wallet.Id,
            DealerId = request.DealerId,
            Type = CoinTransactionType.PlanCredit,
            Amount = request.CoinsAmount,
            BalanceAfter = wallet.Balance,
            Description = "Créditos mensuales del plan",
            CreatedAt = DateTime.UtcNow
        };
        _context.OklaCoinsTransactions.Add(tx);

        await _context.SaveChangesAsync();

        return Ok(new CoinPurchaseResult(
            TransactionId: tx.Id.ToString(),
            PackageSlug: "plan-credit",
            PackageName: "Créditos mensuales del plan",
            BaseCredits: request.CoinsAmount,
            BonusCredits: 0,
            TotalCredits: request.CoinsAmount,
            AmountCharged: 0m,
            Currency: "USD",
            NewBalance: wallet.Balance,
            Status: "completed",
            Message: $"¡{request.CoinsAmount:N0} OKLA Coins del plan acreditados!"
        ));
    }
}

// ── DTOs ──

public record CoinsPackagesResponse(
    List<CoinPackageDto> Packages,
    string ConversionRate,
    int TotalPackages
);

public record CoinPackageDto(
    string Id,
    string Slug,
    string Name,
    int BaseCredits,
    int BonusCredits,
    int TotalCredits,
    int BonusPercentage,
    decimal PriceUsd,
    decimal CostPerCoin,
    string? BadgeText
);

public record WalletDto(
    string DealerId,
    int Balance,
    int TotalPurchased,
    int TotalBonus,
    int TotalFromPlan,
    int TotalSpent,
    string Currency,
    decimal BalanceUsd,
    string? LastPlanCreditDate
);

public record PurchasePackageRequest(
    string PackageSlug,
    string? PaymentMethodId = null
);

public record CoinPurchaseResult(
    string TransactionId,
    string PackageSlug,
    string PackageName,
    int BaseCredits,
    int BonusCredits,
    int TotalCredits,
    decimal AmountCharged,
    string Currency,
    int NewBalance,
    string Status,
    string Message
);

public record SpendCoinsRequest(
    string ProductSlug,
    int Amount,
    Guid? VehicleId = null,
    string? Duration = "month"
);

public record CoinSpendResult(
    string TransactionId,
    int AmountSpent,
    string ProductSlug,
    int NewBalance,
    string Status,
    string Message
);

public record CoinsTransactionsResponse(
    List<CoinTransactionDto> Transactions,
    int TotalCount,
    int Page,
    int PageSize
);

public record CoinTransactionDto(
    string Id,
    string Type,
    int Amount,
    int BalanceAfter,
    string Description,
    string? PackageSlug,
    decimal? AmountUsd,
    string CreatedAt
);

public record PlanCreditRequest(
    Guid DealerId,
    int CoinsAmount
);
