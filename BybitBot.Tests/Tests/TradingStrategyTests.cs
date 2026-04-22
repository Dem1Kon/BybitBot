using BybitBot.Models;
using BybitBot.Services;
using Bybit.Net.Enums;
using Moq;
using Xunit;

namespace BybitBot.Tests;

public class TradingStrategyTests
{
    private readonly Mock<IBybitClient> _mockClient;
    private readonly BotConfig _config;
    private readonly BotState _state;
    private readonly TradingStrategy _strategy;
    
    public TradingStrategyTests()
    {
        _mockClient = new Mock<IBybitClient>();
        _config = new BotConfig 
        { 
            PriceChangeThreshold = 100,
            TradeQuantity = 0.01m,
            Symbol = "ETHUSDT"
        };
        _state = new BotState();
        _strategy = new TradingStrategy(_mockClient.Object, _config, _state);
        
        // Настройка моков по умолчанию
        _mockClient.Setup(x => x.GetBalanceAsync(It.IsAny<string>()))
            .ReturnsAsync(10000m);
        _mockClient.Setup(x => x.PlaceMarketOrderAsync(It.IsAny<string>(), It.IsAny<OrderSide>(), It.IsAny<decimal>()))
            .ReturnsAsync(true);
    }
    
    [Fact]
    public async Task ProcessPriceAsync_FirstCall_SetsReferencePrice()
    {
        // Act
        await _strategy.ProcessPriceAsync(1000m);
        
        // Assert
        Assert.Equal(1000m, _state.ReferencePrice);
        Assert.Equal(1000m, _state.LastPrice);
        Assert.False(_state.HasOpenPosition);
    }
    
    [Fact]
    public async Task ProcessPriceAsync_WhenPriceDrops100Points_OpensLongPosition()
    {
        // Act
        await _strategy.ProcessPriceAsync(1000m);  // Reference price
        await _strategy.ProcessPriceAsync(899m);  // Drop 101 points
        
        // Assert
        Assert.True(_state.HasOpenPosition);
        Assert.Equal(PositionType.Long, _state.CurrentPositionType);
        Assert.Equal(899m, _state.AverageEntryPrice);
        
        // Verify order was placed
        _mockClient.Verify(x => x.PlaceMarketOrderAsync(
            "ETHUSDT", 
            OrderSide.Buy, 
            0.01m), 
            Times.Once);
    }
    
    [Fact]
    public async Task ProcessPriceAsync_WhenPriceRises100Points_OpensShortPosition()
    {
        // Act
        await _strategy.ProcessPriceAsync(1000m);  // Reference price
        await _strategy.ProcessPriceAsync(1101m);  // Rise 101 points
        
        // Assert
        Assert.True(_state.HasOpenPosition);
        Assert.Equal(PositionType.Short, _state.CurrentPositionType);
        Assert.Equal(1101m, _state.AverageEntryPrice);
        
        _mockClient.Verify(x => x.PlaceMarketOrderAsync(
            "ETHUSDT", 
            OrderSide.Sell, 
            0.01m), 
            Times.Once);
    }
    
    [Fact]
    public async Task ProcessPriceAsync_WhenPriceChangeLessThanThreshold_NoPositionOpened()
    {
        // Act
        await _strategy.ProcessPriceAsync(1000m);  // Reference price
        await _strategy.ProcessPriceAsync(950m);   // Drop 50 points (less than 100)
        
        // Assert
        Assert.False(_state.HasOpenPosition);
        _mockClient.Verify(x => x.PlaceMarketOrderAsync(
            It.IsAny<string>(), 
            It.IsAny<OrderSide>(), 
            It.IsAny<decimal>()), 
            Times.Never);
    }
    
    [Fact]
    public async Task ProcessPriceAsync_WhenInsufficientBalance_DoesNotOpenPosition()
    {
        // Arrange
        _mockClient.Setup(x => x.GetBalanceAsync("USDT"))
            .ReturnsAsync(5m);  // Insufficient (needs ~9 USDT for 0.01 ETH)
        
        // Act
        await _strategy.ProcessPriceAsync(1000m);  // Reference price
        await _strategy.ProcessPriceAsync(899m);   // Signal to buy
        
        // Assert
        Assert.False(_state.HasOpenPosition);
        _mockClient.Verify(x => x.PlaceMarketOrderAsync(
            It.IsAny<string>(), 
            It.IsAny<OrderSide>(), 
            It.IsAny<decimal>()), 
            Times.Never);
    }
    
    [Fact]
    public async Task ProcessPriceAsync_WhenPositionOpenAndProfitReachesThreshold_ClosesPosition()
    {
        // Arrange
        await _strategy.ProcessPriceAsync(1000m);  // Reference price
        await _strategy.ProcessPriceAsync(899m);   // Open Long at 899
        
        // Act
        await _strategy.ProcessPriceAsync(999m);   // Profit 100 points
        
        // Assert
        Assert.False(_state.HasOpenPosition);
        Assert.Equal(100m, _state.TotalProfit);
        
        // Verify close order was placed
        _mockClient.Verify(x => x.PlaceMarketOrderAsync(
            "ETHUSDT", 
            OrderSide.Sell, 
            0.01m), 
            Times.Once);
    }
    
    [Fact]
    public async Task ProcessPriceAsync_WhenPositionOpenAndLossReachesThreshold_ClosesPosition()
    {
        // Arrange
        await _strategy.ProcessPriceAsync(1000m);  // Reference price
        await _strategy.ProcessPriceAsync(1101m);  // Open Short at 1101
        
        // Act
        await _strategy.ProcessPriceAsync(1001m);  // Loss 100 points (price went up)
        
        // Assert
        Assert.False(_state.HasOpenPosition);
        Assert.Equal(100m, _state.TotalProfit);
    }
    
    [Fact]
    public async Task ProcessPriceAsync_WhenMultipleSignals_OnlyOpensOnePosition()
    {
        // Arrange
        await _strategy.ProcessPriceAsync(1000m);  // Reference price
        
        // Act
        await _strategy.ProcessPriceAsync(899m);   // First signal - opens position
        await _strategy.ProcessPriceAsync(800m);   // Second signal - should be ignored
        
        // Assert
        Assert.True(_state.HasOpenPosition);
        Assert.Equal(899m, _state.AverageEntryPrice);  // First price, not second
        
        // Only one order placed
        _mockClient.Verify(x => x.PlaceMarketOrderAsync(
            It.IsAny<string>(), 
            It.IsAny<OrderSide>(), 
            It.IsAny<decimal>()), 
            Times.Once);
    }
    
    [Fact]
    public async Task ProcessPriceAsync_AfterPositionClosed_CanOpenNewPosition()
    {
        // Arrange - open and close position
        await _strategy.ProcessPriceAsync(1000m);  // Reference
        await _strategy.ProcessPriceAsync(899m);   // Open Long
        await _strategy.ProcessPriceAsync(999m);   // Close with profit
        
        // Act - new signal
        await _strategy.ProcessPriceAsync(1101m);  // Should open Short
        
        // Assert
        Assert.True(_state.HasOpenPosition);
        Assert.Equal(PositionType.Short, _state.CurrentPositionType);
        Assert.Equal(1101m, _state.AverageEntryPrice);
        Assert.Equal(2, _state.TotalTrades);
    }
    
    [Fact]
    public async Task ProcessPriceAsync_WhenOrderFails_DoesNotUpdateState()
    {
        // Arrange
        _mockClient.Setup(x => x.PlaceMarketOrderAsync(It.IsAny<string>(), It.IsAny<OrderSide>(), It.IsAny<decimal>()))
            .ReturnsAsync(false);  // Order fails
        
        // Act
        await _strategy.ProcessPriceAsync(1000m);  // Reference
        await _strategy.ProcessPriceAsync(899m);   // Signal
        
        // Assert
        Assert.False(_state.HasOpenPosition);
        Assert.Equal(0, _state.TotalTrades);
    }
}