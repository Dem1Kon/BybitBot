using System.Timers;
using BybitBot.Models;
using Timer = System.Timers.Timer;

namespace BybitBot.Services;




public class PriceMonitorService(IBybitClient client, BotConfig config) : IDisposable
{
    private readonly IBybitClient _client = client ?? throw new ArgumentNullException(nameof(client));
    private readonly BotConfig _config = config ?? throw new ArgumentNullException(nameof(config));
    private Timer? _timer;
    private bool _isRunning;
    private bool _disposed = false;
    
    /// <summary>
    /// Событие при обновлении цены
    /// </summary>
    public event Action<decimal>? OnPriceUpdate;
    
    /// <summary>
    /// Событие при ошибке
    /// </summary>
    public event Action<string>? OnError;
    
    // Правильный конструктор с телом

    /// <summary>
    /// Запуск мониторинга цены
    /// </summary>
    public async Task StartAsync()
    {
        if (_isRunning)
        {
            Console.WriteLine("[WARNING] PriceMonitor is already running");
            return;
        }
        
        Console.WriteLine($"[INFO] Starting PriceMonitor for {_config.Symbol} with interval {_config.UpdateIntervalMs}ms");
        
        _timer = new Timer(_config.UpdateIntervalMs);
        _timer.Elapsed += async (sender, e) => await FetchPrice();
        _timer.AutoReset = true;
        _timer.Start();
        _isRunning = true;
        
        // Сразу получаем первую цену
        await FetchPrice();
    }
    
    /// <summary>
    /// Остановка мониторинга цены
    /// </summary>
    public async Task StopAsync()
    {
        if (!_isRunning)
        {
            return;
        }
        
        Console.WriteLine($"[INFO] Stopping PriceMonitor for {_config.Symbol}");
        
        if (_timer != null)
        {
            _timer.Stop();
            _timer.Dispose();
            _timer = null;
        }
        
        _isRunning = false;
        await Task.CompletedTask;
    }
    
    /// <summary>
    /// Получение текущей цены
    /// </summary>
    private async Task FetchPrice()
    {
        try
        {
            var price = await _client.GetCurrentPriceAsync(_config.Symbol);
            if (price.HasValue)
            {
                OnPriceUpdate?.Invoke(price.Value);
            }
        }
        catch (Exception ex)
        {
            OnError?.Invoke($"Price fetch error: {ex.Message}");
        }
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            _timer?.Dispose();
            _disposed = true;
        }
    }
}