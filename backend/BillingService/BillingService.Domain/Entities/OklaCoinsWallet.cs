namespace BillingService.Domain.Entities;

/// <summary>
/// Billetera de OKLA Coins para un dealer.
/// Los créditos se usan para comprar productos publicitarios.
/// Los planes incluyen créditos mensuales automáticos.
/// </summary>
public class OklaCoinsWallet
{
    public Guid Id { get; set; }

    /// <summary>
    /// ID del dealer propietario
    /// </summary>
    public Guid DealerId { get; set; }

    /// <summary>
    /// Balance actual de OKLA Coins
    /// </summary>
    public int Balance { get; set; }

    /// <summary>
    /// Total de coins comprados (histórico)
    /// </summary>
    public int TotalPurchased { get; set; }

    /// <summary>
    /// Total de coins recibidos como bonus
    /// </summary>
    public int TotalBonus { get; set; }

    /// <summary>
    /// Total de coins recibidos del plan mensual
    /// </summary>
    public int TotalFromPlan { get; set; }

    /// <summary>
    /// Total de coins gastados (histórico)
    /// </summary>
    public int TotalSpent { get; set; }

    /// <summary>
    /// Moneda de referencia para conversión (1 coin = $0.01 USD)
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Última fecha en que se acreditaron coins del plan mensual
    /// </summary>
    public DateTime? LastPlanCreditDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // ── Domain Methods ──

    public void AddCoins(int amount, CoinTransactionType type, string description)
    {
        if (amount <= 0) throw new ArgumentException("Amount must be positive", nameof(amount));

        Balance += amount;
        UpdatedAt = DateTime.UtcNow;

        switch (type)
        {
            case CoinTransactionType.Purchase:
                TotalPurchased += amount;
                break;
            case CoinTransactionType.Bonus:
                TotalBonus += amount;
                break;
            case CoinTransactionType.PlanCredit:
                TotalFromPlan += amount;
                LastPlanCreditDate = DateTime.UtcNow;
                break;
        }
    }

    public bool SpendCoins(int amount, string description)
    {
        if (amount <= 0) throw new ArgumentException("Amount must be positive", nameof(amount));
        if (Balance < amount) return false;

        Balance -= amount;
        TotalSpent += amount;
        UpdatedAt = DateTime.UtcNow;
        return true;
    }

    public bool HasSufficientBalance(int amount) => Balance >= amount;
}

/// <summary>
/// Tipo de transacción de OKLA Coins
/// </summary>
public enum CoinTransactionType
{
    Purchase = 0,       // Compra directa de paquete
    Bonus = 1,          // Bonus por volumen de compra
    PlanCredit = 2,     // Créditos mensuales del plan
    Spend = 3,          // Gasto en producto publicitario
    Refund = 4,         // Devolución por cancelación
    Adjustment = 5,     // Ajuste manual por admin
    Expiration = 6      // Expiración de coins no usados
}
