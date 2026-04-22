namespace BybitBot.Models;

/// <summary>
/// Текущее состояние бота
/// </summary>
public class BotState
{
    public decimal? ReferencePrice { get; set; }     
    public bool HasOpenPosition { get; set; }         
    public decimal? AverageEntryPrice { get; set; }        
    public PositionType? CurrentPositionType { get; set; }
    
    // Статистика
    public int TotalTrades { get; set; }
    public decimal TotalProfit { get; set; }
    
    // Для отладки
    public DateTime LastUpdateTime { get; set; }
    public decimal LastPrice { get; set; }
    
    public void Reset()
    {
        ReferencePrice = null;
        HasOpenPosition = false;
        AverageEntryPrice = null;
        CurrentPositionType = null;
    }
    
    public void RecordTrade(PositionType type, decimal price)
    {
        HasOpenPosition = true;
        CurrentPositionType = type;
        AverageEntryPrice = price;
        ReferencePrice = price;
        TotalTrades++;
    }
    
    public void ClosePosition(decimal exitPrice, decimal profit)
    {
        HasOpenPosition = false;
        CurrentPositionType = null;
        ReferencePrice = exitPrice;
        TotalProfit += profit;
    }
}

/// <summary>
/// Тип позиции
/// </summary>
public enum PositionType
{
    Long,   
    Short  
}