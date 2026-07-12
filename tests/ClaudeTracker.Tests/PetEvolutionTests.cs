using ClaudeTracker.Utilities;
using Xunit;

namespace ClaudeTracker.Tests;

public class PetEvolutionTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(25, 1)]
    [InlineData(26, 2)]
    [InlineData(50, 2)]
    [InlineData(51, 3)]
    [InlineData(75, 3)]
    [InlineData(76, 4)]
    [InlineData(100, 4)]
    public void StageForPercentage_ReturnsExpectedStage(double percentage, int expectedStage)
    {
        Assert.Equal(expectedStage, PetEvolution.StageForPercentage(percentage));
    }

    [Fact]
    public void StageForPercentage_AbovePercentage_ClampsToMaxStage()
    {
        Assert.Equal(PetEvolution.MaxStage, PetEvolution.StageForPercentage(150));
    }
}
