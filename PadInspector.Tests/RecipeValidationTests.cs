using PadInspector.Models;

namespace PadInspector.Tests;

public class RecipeValidationTests
{
    [Fact]
    public void ValidRecipe_ReturnsNoErrors()
    {
        var recipe = new Recipe
        {
            Name = "Test",
            ThresholdValue = 128,
            MinAreaRatio = 0.01,
            MaxAreaRatio = 0.5,
            PassScoreThreshold = 5.0,
            ExposureTimeUs = 500,
            GainDb = 0
        };

        var result = RecipeValidationResult.Validate(recipe);
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void EmptyName_ReturnsError()
    {
        var recipe = new Recipe { Name = "" };
        var result = RecipeValidationResult.Validate(recipe);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("이름"));
    }

    [Fact]
    public void InvalidThreshold_ReturnsError()
    {
        var recipe = new Recipe { Name = "Test", ThresholdValue = 300 };
        var result = RecipeValidationResult.Validate(recipe);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Threshold"));
    }

    [Fact]
    public void MinAreaGreaterThanMax_ReturnsWarning()
    {
        var recipe = new Recipe { Name = "Test", MinAreaRatio = 0.5, MaxAreaRatio = 0.3 };
        var result = RecipeValidationResult.Validate(recipe);
        Assert.NotEmpty(result.Warnings);
    }

    [Fact]
    public void NegativeGain_ReturnsError()
    {
        var recipe = new Recipe { Name = "Test", GainDb = -5 };
        var result = RecipeValidationResult.Validate(recipe);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Gain"));
    }

    [Fact]
    public void InvalidRoi_ReturnsError()
    {
        var recipe = new Recipe
        {
            Name = "Test",
            Camera1Roi = new RoiRect { X = -0.5, Y = 0, Width = 1, Height = 1 }
        };
        var result = RecipeValidationResult.Validate(recipe);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("CAM1"));
    }
}
