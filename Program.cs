using BybitBot.Models;
using BybitBot.Services;
using BybitBot.Utils;

namespace BybitBot;

class Program
{
    private static IBybitClient? _client;
    private static PriceMonitorService? _priceMonitor;
    private static TradingStrategy? _strategy;
    private static BotState _state = new BotState();
    private static BotConfig _config = new BotConfig();
    private static bool _isShuttingDown = false;
    
    static async Task Main(string[] args)
    {
        ConsoleHelper.WriteHeader("BYBIT TRADING BOT v1.0");
        
        // Загрузка конфигурации из переменных окружения
        LoadConfiguration();
        
        // Получение API ключей из переменных окружения
        string apiKey = Environment.GetEnvironmentVariable("BYBIT_API_KEY") ?? "";
        string apiSecret = Environment.GetEnvironmentVariable("BYBIT_API_SECRET") ?? "";
        
        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
        {
            ConsoleHelper.WriteError("API keys are required!");
            ConsoleHelper.WriteInfo("Please set environment variables:");
            ConsoleHelper.WriteInfo("  BYBIT_API_KEY=your_testnet_api_key");
            ConsoleHelper.WriteInfo("  BYBIT_API_SECRET=your_testnet_api_secret");
            ConsoleHelper.WriteInfo("");
            ConsoleHelper.WriteInfo("Or run in PowerShell:");
            ConsoleHelper.WriteInfo("  $env:BYBIT_API_KEY='your_key'");
            ConsoleHelper.WriteInfo("  $env:BYBIT_API_SECRET='your_secret'");
            ConsoleHelper.WriteInfo("  dotnet run");
            return;
        }
        
        try
        {
            // Инициализация сервисов
            ConsoleHelper.WriteInfo("Initializing services...");
            _client = new BybitClient(apiKey, apiSecret);
            _strategy = new TradingStrategy(_client, _config, _state);
            _priceMonitor = new PriceMonitorService(_client, _config.Symbol);
            
            // Настройка обработчиков событий
            SetupEventHandlers();
            
            // Проверка подключения
            await TestConnection();
            
            // Запуск мониторинга
            ConsoleHelper.WriteSeparator();
            ConsoleHelper.WriteSuccess($"🚀 Bot started! Monitoring {_config.Symbol}");
            ConsoleHelper.WriteInfo($"📊 Strategy: Buy on -${_config.PriceChangeThreshold}, Sell on +${_config.PriceChangeThreshold}");
            ConsoleHelper.WriteInfo($"💰 Trade amount: {_config.TradeQuantity} {_config.Symbol.Replace("USDT", "")}");
            ConsoleHelper.WriteInfo($"⏱️  Update interval: {_config.UpdateIntervalMs}ms");
            ConsoleHelper.WriteSeparator();
            ConsoleHelper.WriteWarning("Press Ctrl+C to stop...");
            Console.WriteLine();
            
            await _priceMonitor.StartAsync();
            
            // Ожидание завершения
            await WaitForShutdown();
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"Fatal error: {ex.Message}");
        }
        finally
        {
            await CleanupAsync();
        }
    }
    
    /// <summary>
    /// Загрузка конфигурации из переменных окружения
    /// </summary>
    private static void LoadConfiguration()
    {
        // Чтение настроек из переменных окружения (с значениями по умолчанию)
        _config.Symbol = Environment.GetEnvironmentVariable("TRADE_SYMBOL") ?? "ETHUSDT";
        _config.TradeQuantity = decimal.Parse(Environment.GetEnvironmentVariable("TRADE_QUANTITY") ?? "0.01");
        _config.PriceChangeThreshold = int.Parse(Environment.GetEnvironmentVariable("PRICE_THRESHOLD") ?? "100");
        _config.UpdateIntervalMs = int.Parse(Environment.GetEnvironmentVariable("UPDATE_INTERVAL_MS") ?? "2000");
        
        bool useTestnet = bool.Parse(Environment.GetEnvironmentVariable("BYBIT_USE_TESTNET") ?? "true");
        if (useTestnet)
        {
            ConsoleHelper.WriteInfo("🔧 Using Bybit Testnet (paper trading)");
        }
        
        // Валидация конфигурации
        try
        {
            _config.Validate();
        }
        catch (ArgumentException ex)
        {
            ConsoleHelper.WriteError($"Invalid configuration: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// Настройка обработчиков событий
    /// </summary>
    private static void SetupEventHandlers()
    {
        if (_priceMonitor == null || _strategy == null) return;
        
        // Подписка на обновления цены
        _priceMonitor.OnPriceUpdate += async price => 
        {
            try
            {
                await _strategy.ProcessPriceAsync(price);
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Strategy error: {ex.Message}");
            }
        };
        
        // Подписка на ошибки мониторинга
        _priceMonitor.OnError += error => 
            ConsoleHelper.WriteError($"Price monitor error: {error}");
        
        // Подписка на события стратегии
        _strategy.OnLog += ConsoleHelper.WriteInfo;
        
        _strategy.OnPositionOpened += (price, type) => 
            ConsoleHelper.WriteTrade($"🎯 POSITION OPENED: {type} @ ${price:F2}");
        
        _strategy.OnPositionClosed += (price, profit) =>
        {
            string profitSymbol = profit >= 0 ? "+" : "";
            ConsoleHelper.WriteTrade($"💰 POSITION CLOSED @ ${price:F2} | P&L: {profitSymbol}${profit:F2}");
        };
    }
    
    /// <summary>
    /// Проверка подключения к бирже
    /// </summary>
    private static async Task TestConnection()
    {
        if (_client == null) return;
        
        ConsoleHelper.WriteInfo("🔌 Testing connection to Bybit...");
        
        try
        {
            var price = await _client.GetCurrentPriceAsync(_config.Symbol);
            
            if (price.HasValue)
            {
                ConsoleHelper.WriteSuccess($"✅ Connection successful! {_config.Symbol} price: ${price.Value:F2}");
            }
            else
            {
                ConsoleHelper.WriteError("❌ Connection failed! Please check your API keys and network.");
                ConsoleHelper.WriteWarning("Continuing anyway, but price updates may fail...");
            }
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"❌ Connection test failed: {ex.Message}");
            ConsoleHelper.WriteWarning("Continuing anyway...");
        }
    }
    
    /// <summary>
    /// Ожидание сигнала завершения (Ctrl+C)
    /// </summary>
    private static async Task WaitForShutdown()
    {
        var tcs = new TaskCompletionSource<bool>();
        
        Console.CancelKeyPress += (sender, e) =>
        {
            if (_isShuttingDown) return;
            
            e.Cancel = true;
            _isShuttingDown = true;
            ConsoleHelper.WriteWarning("\n🛑 Shutdown signal received...");
            tcs.SetResult(true);
        };
        
        await tcs.Task;
    }
    
    /// <summary>
    /// Очистка ресурсов при завершении
    /// </summary>
    private static async Task CleanupAsync()
    {
        ConsoleHelper.WriteWarning("Stopping bot...");
        
        // Остановка мониторинга
        if (_priceMonitor != null)
        {
            await _priceMonitor.StopAsync();
            _priceMonitor.Dispose();
        }
        
        // Закрытие клиентов
        _client?.Dispose();
        
        // Вывод финальной статистики
        ConsoleHelper.WriteHeader("FINAL STATISTICS");
        ConsoleHelper.WriteInfo($"📊 Total Trades: {_state.TotalTrades}");
        ConsoleHelper.WriteInfo($"💰 Total P&L: ${_state.TotalProfit:F2}");
        
        if (_state.TotalTrades > 0)
        {
            decimal avgProfit = _state.TotalProfit / _state.TotalTrades;
            string avgSymbol = avgProfit >= 0 ? "+" : "";
            ConsoleHelper.WriteInfo($"📈 Average P&L per trade: {avgSymbol}${avgProfit:F2}");
        }
        
        if (_state.HasOpenPosition && _state.AverageEntryPrice != null && _state.LastPrice > 0)
        {
            decimal unrealizedProfit = _state.CurrentPositionType == PositionType.Long
                ? _state.LastPrice - _state.AverageEntryPrice.Value
                : _state.AverageEntryPrice.Value - _state.LastPrice;
            
            ConsoleHelper.WriteWarning($"⚠️ Position still open! Unrealized P&L: ${unrealizedProfit:F2}");
            ConsoleHelper.WriteInfo("Manual intervention may be required to close the position.");
        }
        
        ConsoleHelper.WriteSuccess("✅ Bot stopped successfully!");
    }
}