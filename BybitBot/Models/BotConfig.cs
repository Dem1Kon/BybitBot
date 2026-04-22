namespace BybitBot.Models;

/// <summary>
/// Конфигурация торгового бота
/// </summary>
public class BotConfig
{
    public string Symbol { get; set; }
    public int PriceChangeThreshold { get; set; }
    public decimal TradeQuantity { get; set; }
    public int UpdateIntervalMs { get; set; }
    
    
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