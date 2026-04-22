using Bybit.Net.Enums;
using BybitBot.Models;

namespace BybitBot.Services;

/// <summary>
/// Торговая стратегия
/// </summary>
public class TradingStrategy(IBybitClient client, BotConfig config, BotState state)
{
    private readonly IBybitClient _client = client ?? throw new ArgumentNullException(nameof(client));
    private readonly BotConfig _config = config ?? throw new ArgumentNullException(nameof(config));
    private readonly BotState _state = state ?? throw new ArgumentNullException(nameof(state));
    
    /// <summary>
    /// Событие для логирования
    /// </summary>
    public event Action<string>? OnLog;
    
    /// <summary>
    /// Событие при открытии позиции
    /// </summary>
    public event Action<decimal, PositionType>? OnPositionOpened;
    
    /// <summary>
    /// Событие при закрытии позиции
    /// </summary>
    public event Action<decimal, decimal>? OnPositionClosed;

    /// <summary>
    /// Обработка новой цены
    /// </summary>
    public async Task ProcessPriceAsync(decimal currentPrice)
    {
        // Если референсная цена не установлена, устанавливаем её
        if (_state.ReferencePrice == null)
        {
            _state.ReferencePrice = currentPrice;
            _state.LastPrice = currentPrice;
            Log($"Reference price set to ${currentPrice:F2}");
            return;
        }
        
        _state.LastPrice = currentPrice;
        _state.LastUpdateTime = DateTime.Now;
        
        var priceChange = currentPrice - _state.ReferencePrice.Value;
        
        // Нет открытой позиции → проверяем сигналы на вход
        if (!_state.HasOpenPosition)
        {
            await CheckEntrySignals(currentPrice, priceChange);
        }
        // Есть открытая позиция → проверяем выход
        else
        {
            await CheckExitConditions(currentPrice);
        }
    }
    
    /// <summary>
    /// Проверка сигналов на вход в позицию
    /// </summary>
    private async Task CheckEntrySignals(decimal currentPrice, decimal priceChange)
    {
        // Падение на порог и более → покупка (Long)
        if (priceChange <= -_config.PriceChangeThreshold)
        {
            Log($"📈 BUY SIGNAL! Price dropped to ${currentPrice:F2} (change: {priceChange:F2}$)");
            await OpenPosition(PositionType.Long, currentPrice);
        }
        // Рост на порог и более → продажа (Short)
        else if (priceChange >= _config.PriceChangeThreshold)
        {
            Log($"📉 SELL SIGNAL! Price rose to ${currentPrice:F2} (change: +{priceChange:F2}$)");
            await OpenPosition(PositionType.Short, currentPrice);
        }
    }
    
    /// <summary>
    /// Открытие позиции
    /// </summary>
    private async Task OpenPosition(PositionType type, decimal price)
    {
        var side = type == PositionType.Long ? OrderSide.Buy : OrderSide.Sell;
        
        // Проверяем баланс перед покупкой (для Long позиции)
        if (side == OrderSide.Buy)
        {
            var usdtBalance = await _client.GetBalanceAsync("USDT");
            var requiredAmount = _config.TradeQuantity * price;
            
            if (usdtBalance == null || usdtBalance < requiredAmount)
            {
                Log($"❌ Insufficient USDT balance. Required: ${requiredAmount:F2}, Available: ${usdtBalance ?? 0:F2}");
                return;
            }
            
            Log($"✅ Balance check passed. USDT available: ${usdtBalance:F2}");
        }
        else
        {
            // Для Short позиции проверяем баланс ETH
            var ethBalance = await _client.GetBalanceAsync(_config.Symbol.Replace("USDT", ""));
            
            if (ethBalance == null || ethBalance < _config.TradeQuantity)
            {
                Log($"❌ Insufficient {_config.Symbol.Replace("USDT", "")} balance. Required: {_config.TradeQuantity}, Available: {ethBalance ?? 0}");
                return;
            }
            
            Log($"✅ Balance check passed. {_config.Symbol.Replace("USDT", "")} available: {ethBalance}");
        }
        
        bool success = await _client.PlaceMarketOrderAsync(_config.Symbol, side, _config.TradeQuantity);
        
        if (success)
        {
            _state.RecordTrade(type, price);
            OnPositionOpened?.Invoke(price, type);
            Log($"✅ POSITION OPENED ({type}) at ${price:F2}");
        }
        else
        {
            Log($"❌ Failed to open {type} position");
        }
    }
    
    /// <summary>
    /// Проверка условий для закрытия позиции
    /// </summary>
    private async Task CheckExitConditions(decimal currentPrice)
    {
        if (_state.AverageEntryPrice == null) return;
        
        decimal profit = CalculateProfit(currentPrice);
        
        // Логируем текущий P&L каждые ~5 секунд
        if (DateTime.Now.Second % 5 == 0)
        {
            Log($"💰 Current P&L: ${profit:F2}");
        }
        
        // Если профит/убыток достиг порога → закрываем позицию
        if (Math.Abs(profit) >= _config.PriceChangeThreshold)
        {
            await ClosePosition(currentPrice, profit);
        }
    }
    
    /// <summary>
    /// Расчет текущей прибыли
    /// </summary>
    private decimal CalculateProfit(decimal currentPrice)
    {
        if (_state.AverageEntryPrice == null) return 0;
        
        return _state.CurrentPositionType switch
        {
            PositionType.Long => currentPrice - _state.AverageEntryPrice.Value,
            PositionType.Short => _state.AverageEntryPrice.Value - currentPrice,
            _ => 0
        };
    }
    
    /// <summary>
    /// Закрытие позиции
    /// </summary>
    private async Task ClosePosition(decimal currentPrice, decimal profit)
    {
        var closeSide = _state.CurrentPositionType == PositionType.Long 
            ? OrderSide.Sell 
            : OrderSide.Buy;
        
        string result = profit >= 0 ? "PROFIT" : "LOSS";
        Log($"🏁 CLOSE SIGNAL! {result}: ${Math.Abs(profit):F2}");
        
        bool success = await _client.PlaceMarketOrderAsync(_config.Symbol, closeSide, _config.TradeQuantity);
        
        if (success)
        {
            _state.ClosePosition(currentPrice, profit);
            OnPositionClosed?.Invoke(currentPrice, profit);
            Log($"✅ POSITION CLOSED. Result: ${profit:F2}");
            Log($"📊 Stats: Trades: {_state.TotalTrades}, Total P&L: ${_state.TotalProfit:F2}");
        }
        else
        {
            Log($"❌ Failed to close position");
        }
    }
    
    private void Log(string message)
    {
        OnLog?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");
    }
}