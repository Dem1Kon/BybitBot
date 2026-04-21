using Bybit.Net;
using Bybit.Net.Clients;
using CryptoExchange.Net.Authentication;

namespace BybitBot.Services;

using Bybit.Net.Enums;

/// <summary>
/// Интерфейс для работы с API Bybit
/// </summary>
public interface IBybitClient : IDisposable
{
    /// <summary>
    /// Получение текущей цены символа
    /// </summary>
    Task<decimal?> GetCurrentPriceAsync(string symbol);
    
    /// <summary>
    /// Размещение рыночного ордера
    /// </summary>
    Task<bool> PlaceMarketOrderAsync(string symbol, OrderSide side, decimal quantity);
    
    /// <summary>
    /// Получение баланса по монете
    /// </summary>
    Task<decimal?> GetBalanceAsync(string asset);
    
    /// <summary>
    /// Подписка на обновления цены через WebSocket
    /// </summary>
    Task SubscribeToPriceUpdatesAsync(string symbol, Action<decimal> onPriceUpdate);
}

public class BybitClient : IBybitClient
{
    private readonly BybitRestClient _restClient;
    private readonly BybitSocketClient _socketClient;
    private bool _disposed = false;
    
    public BybitClient(string apiKey, string apiSecret)
    {
        // Создаем объект с учетными данными
        var credentials = new BybitCredentials(apiKey, apiSecret);
        
        // Инициализируем REST клиент
        _restClient = new BybitRestClient(options =>
        {
            options.ApiCredentials = credentials;
            options.Environment = BybitEnvironment.Testnet;  // Важно для Testnet!
            options.AutoTimestamp = true;                    // Авто-синхронизация времени
        });
        
        // Инициализируем WebSocket клиент
        _socketClient = new BybitSocketClient(options =>
        {
            options.ApiCredentials = credentials;
            options.Environment = BybitEnvironment.Testnet;
            options.AutoTimestamp = true;
        });
    }
    
    public async Task<decimal?> GetCurrentPriceAsync(string symbol)
    {
        try
        {
            var tickerResult = await _restClient.V5Api.ExchangeData.GetSpotTickersAsync(symbol);
            
            if (tickerResult.Success && tickerResult.Data.List.Any())
            {
                return tickerResult.Data.List.First().LastPrice;
            }
            
            Console.WriteLine($"[ERROR] Get price failed: {tickerResult.Error?.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Exception getting price: {ex.Message}");
            return null;
        }
    }
    
    public async Task<bool> PlaceMarketOrderAsync(string symbol, OrderSide side, decimal quantity)
    {
        try
        {
            var orderResult = await _restClient.V5Api.Trading.PlaceOrderAsync(
                category: Category.Spot,
                symbol: symbol,
                side: side,
                type: NewOrderType.Market,
                quantity: quantity
            );
            
            if (orderResult.Success)
            {
                Console.WriteLine($"   ✓ {side} order executed: {quantity} {symbol}, ID: {orderResult.Data.OrderId}");
                return true;
            }
            
            Console.WriteLine($"   ✗ Order failed: {orderResult.Error?.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ✗ Exception placing order: {ex.Message}");
            return false;
        }
    }
    
    public async Task<decimal?> GetBalanceAsync(string asset)
    {
        try
        {
            var balanceResult = await _restClient.V5Api.Account.GetBalancesAsync(
                accountType: AccountType.Unified
            );
            
            if (balanceResult.Success && balanceResult.Data.List != null)
            {
                // Ищем баланс нужного актива
                var assetBalance = balanceResult.Data.List
                    .SelectMany(x => x.Assets)
                    .FirstOrDefault(x => x.Asset == asset);
                    
                return assetBalance?.WalletBalance ?? 0m;
            }
            
            Console.WriteLine($"[ERROR] Get balance failed: {balanceResult.Error?.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Exception getting balance: {ex.Message}");
            return null;
        }
    }
    
    public async Task SubscribeToPriceUpdatesAsync(string symbol, Action<decimal> onPriceUpdate)
    {
        try
        {
            var subscriptionResult = await _socketClient.V5SpotApi.SubscribeToTickerUpdatesAsync(
                symbol,
                update =>
                {
                    if (update.Data != null)
                    {
                        onPriceUpdate?.Invoke(update.Data.LastPrice);
                    }
                }
            );
            
            if (!subscriptionResult.Success)
            {
                Console.WriteLine($"[ERROR] WebSocket subscription failed: {subscriptionResult.Error?.Message}");
            }
            else
            {
                Console.WriteLine($"[INFO] Subscribed to price updates for {symbol}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Exception in WebSocket subscription: {ex.Message}");
        }
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            _restClient?.Dispose();
            _socketClient?.Dispose();
            _disposed = true;
        }
    }
}