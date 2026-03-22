using Microsoft.Extensions.Options;
using OpenCvSharp;
using PadInspector.Configs;
using PadInspector.Models;
using PadInspector.Services;

namespace PadInspector.Tests;

public class InspectionServiceTests
{
    private static InspectionService CreateService(double threshold = 128, double passScore = 5.0)
    {
        var settings = Options.Create(new InspectionSettings
        {
            ThresholdValue = threshold,
            MinAreaRatio = 0.01,
            MaxAreaRatio = 0.5,
            PassScoreThreshold = passScore
        });
        return new InspectionService(settings);
    }

    [Fact]
    public void Inspect_BlackImage_ReturnsFail()
    {
        var svc = CreateService();
        using var image = new Mat(100, 100, MatType.CV_8UC1, new Scalar(0));

        var (result, overlay) = svc.Inspect(image);
        overlay.Dispose();

        Assert.False(result.IsPass);
        Assert.Equal(0, result.PadCount);
    }

    [Fact]
    public void Inspect_WithWhiteBlobs_DetectsPads()
    {
        var svc = CreateService(threshold: 128, passScore: 1.0);
        using var image = new Mat(200, 200, MatType.CV_8UC1, new Scalar(0));

        // Draw white rectangles as "pads"
        Cv2.Rectangle(image, new Rect(30, 30, 40, 40), new Scalar(255), -1);
        Cv2.Rectangle(image, new Rect(100, 100, 40, 40), new Scalar(255), -1);

        var (result, overlay) = svc.Inspect(image);
        overlay.Dispose();

        Assert.True(result.PadCount >= 2);
        Assert.True(result.Score > 0);
    }

    [Fact]
    public void Inspect_ReturnsOverlay_AsBgrMat()
    {
        var svc = CreateService();
        using var image = new Mat(100, 100, MatType.CV_8UC1, new Scalar(0));

        var (_, overlay) = svc.Inspect(image);

        Assert.Equal(3, overlay.Channels());
        overlay.Dispose();
    }

    [Fact]
    public void Inspect_WithRoi_CropsCorrectly()
    {
        var svc = CreateService(threshold: 128, passScore: 0.1);
        using var image = new Mat(200, 200, MatType.CV_8UC1, new Scalar(0));
        // White blob only in top-left quadrant
        Cv2.Rectangle(image, new Rect(10, 10, 40, 40), new Scalar(255), -1);

        var roiTopLeft = new RoiRect { X = 0, Y = 0, Width = 0.5, Height = 0.5 };
        var roiBottomRight = new RoiRect { X = 0.5, Y = 0.5, Width = 0.5, Height = 0.5 };

        var (resultTL, overlayTL) = svc.Inspect(image, roiTopLeft);
        var (resultBR, overlayBR) = svc.Inspect(image, roiBottomRight);
        overlayTL.Dispose();
        overlayBR.Dispose();

        Assert.True(resultTL.PadCount > resultBR.PadCount);
    }

    [Fact]
    public void ApplyRecipe_UpdatesParameters()
    {
        var svc = CreateService();
        var recipe = new Recipe
        {
            ThresholdValue = 200,
            MinAreaRatio = 0.05,
            MaxAreaRatio = 0.8,
            PassScoreThreshold = 10.0
        };

        svc.ApplyRecipe(recipe);

        Assert.Equal(200, svc.ThresholdValue);
        Assert.Equal(0.05, svc.MinAreaRatio);
        Assert.Equal(0.8, svc.MaxAreaRatio);
        Assert.Equal(10.0, svc.PassScoreThreshold);
    }

    [Fact]
    public void Inspect_ColorImage_HandledCorrectly()
    {
        var svc = CreateService();
        using var image = new Mat(100, 100, MatType.CV_8UC3, new Scalar(0, 0, 0));

        var (result, overlay) = svc.Inspect(image);
        Assert.NotNull(result);
        Assert.Equal(3, overlay.Channels());
        overlay.Dispose();
    }
}
