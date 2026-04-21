namespace BybitBot.Models;

/// <summary>
/// Конфигурация торгового бота
/// </summary>
public class BotConfig
{
    public string Symbol { get; set; } = "ETHUSDT";
    public int PriceChangeThreshold { get; set; } = 100;
    public decimal TradeQuantity { get; set; } = 0.01m;
    public int UpdateIntervalMs { get; set; } = 2000; 
    
    // Для стоп-лосса и тейк-профита (опционально)
    public decimal? StopLossPercent { get; set; } = null;
    public decimal? TakeProfitPercent { get; set; } = null;
    
    public static BotConfig Default => new BotConfig();
    
    public void Validate()
    {
        if (TradeQuantity <= 0)
            throw new ArgumentException("TradeQuantity must be positive");
        if (PriceChangeThreshold <= 0)
            throw new ArgumentException("PriceChangeThreshold must be positive");
        if (UpdateIntervalMs < 500)
            throw new ArgumentException("UpdateIntervalMs must be at least 500ms");
    }
}