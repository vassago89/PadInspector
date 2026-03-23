using System.IO;

namespace PadInspector.Models;

public class RecipeValidationResult
{
    public bool IsValid { get; set; } = true;
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];

    public static RecipeValidationResult Validate(Recipe recipe)
    {
        var result = new RecipeValidationResult();

        // Name
        if (string.IsNullOrWhiteSpace(recipe.Name))
            result.Errors.Add("레시피 이름이 비어 있습니다.");
        else if (recipe.Name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            result.Errors.Add("레시피 이름에 사용할 수 없는 문자가 포함되어 있습니다.");

        // Threshold
        if (recipe.ThresholdValue < 0 || recipe.ThresholdValue > 255)
            result.Errors.Add($"Threshold 값이 범위를 벗어났습니다: {recipe.ThresholdValue} (0~255)");

        // Area ratios
        if (recipe.MinAreaRatio < 0 || recipe.MinAreaRatio > 1)
            result.Errors.Add($"Min Area Ratio 범위 오류: {recipe.MinAreaRatio} (0~1)");
        if (recipe.MaxAreaRatio < 0 || recipe.MaxAreaRatio > 1)
            result.Errors.Add($"Max Area Ratio 범위 오류: {recipe.MaxAreaRatio} (0~1)");
        if (recipe.MinAreaRatio >= recipe.MaxAreaRatio)
            result.Warnings.Add("Min Area가 Max Area보다 크거나 같습니다.");

        // Pass score
        if (recipe.PassScoreThreshold < 0 || recipe.PassScoreThreshold > 100)
            result.Errors.Add($"Pass Score 범위 오류: {recipe.PassScoreThreshold} (0~100)");

        // Exposure
        if (recipe.ExposureTimeUs < 1)
            result.Errors.Add($"Exposure 값이 너무 작습니다: {recipe.ExposureTimeUs}us");
        if (recipe.ExposureTimeUs > 1000000)
            result.Warnings.Add($"Exposure 값이 매우 큽니다: {recipe.ExposureTimeUs}us (1초 이상)");

        // Gain
        if (recipe.GainDb < 0)
            result.Errors.Add($"Gain 값이 음수입니다: {recipe.GainDb}dB");

        // ROI
        ValidateRoi(recipe.Camera1Roi, "CAM1", result);
        ValidateRoi(recipe.Camera2Roi, "CAM2", result);

        // Trigger interval
        if (recipe.TriggerIntervalMs < 100)
            result.Warnings.Add($"트리거 간격이 매우 짧습니다: {recipe.TriggerIntervalMs}ms");

        result.IsValid = result.Errors.Count == 0;
        return result;
    }

    private static void ValidateRoi(RoiRect roi, string camName, RecipeValidationResult result)
    {
        if (roi.X < 0 || roi.X >= 1) result.Errors.Add($"[{camName}] ROI X 범위 오류: {roi.X}");
        if (roi.Y < 0 || roi.Y >= 1) result.Errors.Add($"[{camName}] ROI Y 범위 오류: {roi.Y}");
        if (roi.Width <= 0 || roi.Width > 1) result.Errors.Add($"[{camName}] ROI Width 범위 오류: {roi.Width}");
        if (roi.Height <= 0 || roi.Height > 1) result.Errors.Add($"[{camName}] ROI Height 범위 오류: {roi.Height}");
        if (roi.X + roi.Width > 1.001) result.Warnings.Add($"[{camName}] ROI가 이미지 경계를 초과합니다.");
        if (roi.Y + roi.Height > 1.001) result.Warnings.Add($"[{camName}] ROI가 이미지 경계를 초과합니다.");
    }
}
