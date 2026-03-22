using OpenCvSharp;
using PadInspector.Models;

namespace PadInspector.Services;

public interface IInspectionService
{
    double ThresholdValue { get; set; }
    double MinAreaRatio { get; set; }
    double MaxAreaRatio { get; set; }
    double PassScoreThreshold { get; set; }
    InspectionResult Inspect(Mat image, RoiRect? roi = null);
    void ApplyRecipe(Recipe recipe);
}
