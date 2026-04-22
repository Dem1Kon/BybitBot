using BybitBot.Models;
using Xunit;

namespace BybitBot.Tests;

public class BotStateTests
{
    [Fact]
    public void RecordTrade_WhenLongPosition_ShouldUpdateStateCorrectly()
    {
        // Arrange
        var state = new BotState();
        
        // Act
        state.RecordTrade(PositionType.Long, 1000m);
        
        // Assert
        Assert.True(state.HasOpenPosition);
        Assert.Equal(PositionType.Long, state.CurrentPositionType);
        Assert.Equal(1000m, state.AverageEntryPrice);
        Assert.Equal(1000m, state.ReferencePrice);
        Assert.Equal(1, state.TotalTrades);
        Assert.Equal(0, state.TotalProfit);
    }
    
    [Fact]
    public void RecordTrade_WhenShortPosition_ShouldUpdateStateCorrectly()
    {
        // Arrange
        var state = new BotState();
        
        // Act
        state.RecordTrade(PositionType.Short, 2000m);
        
        // Assert
        Assert.True(state.HasOpenPosition);
        Assert.Equal(PositionType.Short, state.CurrentPositionType);
        Assert.Equal(2000m, state.AverageEntryPrice);
        Assert.Equal(2000m, state.ReferencePrice);
        Assert.Equal(1, state.TotalTrades);
    }
    
    [Fact]
    public void ClosePosition_WithProfit_ShouldUpdateTotalProfit()
    {
        // Arrange
        var state = new BotState();
        state.RecordTrade(PositionType.Long, 1000m);
        
        // Act
        state.ClosePosition(1100m, 100m);
        
        // Assert
        Assert.False(state.HasOpenPosition);
        Assert.Null(state.CurrentPositionType);
        Assert.Equal(1100m, state.ReferencePrice);
        Assert.Equal(100m, state.TotalProfit);
    }
    
    [Fact]
    public void ClosePosition_WithLoss_ShouldUpdateTotalProfit()
    {
        // Arrange
        var state = new BotState();
        state.RecordTrade(PositionType.Long, 1000m);
        
        // Act
        state.ClosePosition(900m, -100m);
        
        // Assert
        Assert.False(state.HasOpenPosition);
        Assert.Equal(-100m, state.TotalProfit);
    }
    
    [Fact]
    public void Reset_ShouldClearAllState()
    {
        // Arrange
        var state = new BotState();
        state.RecordTrade(PositionType.Long, 1000m);
        state.TotalProfit = 500m;
        
        // Act
        state.Reset();
        
        // Assert
        Assert.Null(state.ReferencePrice);
        Assert.False(state.HasOpenPosition);
        Assert.Null(state.AverageEntryPrice);
        Assert.Null(state.CurrentPositionType);
        Assert.Equal(0, state.TotalTrades);
        Assert.Equal(500m, state.TotalProfit); // Profit сохраняется при Reset
    }
}