using BybitBot.Models;
using Xunit;

namespace BybitBot.Tests;

public class BotConfigTests
{
    [Fact]
    public void Validate_WhenTradeQuantityIsZero_ShouldThrowException()
    {
        // Arrange
        var config = new BotConfig { TradeQuantity = 0 };
        
        // Act & Assert
        Assert.Throws<ArgumentException>(() => config.Validate());
    }
    
    [Fact]
    public void Validate_WhenTradeQuantityIsNegative_ShouldThrowException()
    {
        // Arrange
        var config = new BotConfig { TradeQuantity = -0.1m };
        
        // Act & Assert
        Assert.Throws<ArgumentException>(() => config.Validate());
    }
    
    [Fact]
    public void Validate_WhenPriceChangeThresholdIsZero_ShouldThrowException()
    {
        // Arrange
        var config = new BotConfig { PriceChangeThreshold = 0, TradeQuantity = 0.01m };
        
        // Act & Assert
        Assert.Throws<ArgumentException>(() => config.Validate());
    }
    
    [Fact]
    public void Validate_WhenUpdateIntervalLessThan500_ShouldThrowException()
    {
        // Arrange
        var config = new BotConfig { UpdateIntervalMs = 100, TradeQuantity = 0.01m, PriceChangeThreshold = 100 };
        
        // Act & Assert
        Assert.Throws<ArgumentException>(() => config.Validate());
    }
    
    [Fact]
    public void Validate_WhenAllValid_ShouldNotThrow()
    {
        // Arrange
        var config = new BotConfig 
        { 
            TradeQuantity = 0.01m, 
            PriceChangeThreshold = 100, 
            UpdateIntervalMs = 2000 
        };
        
        // Act
        var exception = Record.Exception(() => config.Validate());
        
        // Assert
        Assert.Null(exception);
    }
}