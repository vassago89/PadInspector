using Microsoft.Extensions.Options;
using OpenCvSharp;
using PadInspector.Configs;
using PadInspector.Models;

namespace PadInspector.Services;

/// <summary>
/// 패드 검사 서비스 - OpenCV 기반 영상 검사
/// </summary>
public class InspectionService : IInspectionService
{
    private int _inspectionCount;

    public double ThresholdValue { get; set; }
    public double MinAreaRatio { get; set; }
    public double MaxAreaRatio { get; set; }
    public double PassScoreThreshold { get; set; }

    public InspectionService(IOptions<InspectionSettings> options)
    {
        var s = options.Value;
        ThresholdValue = s.ThresholdValue;
        MinAreaRatio = s.MinAreaRatio;
        MaxAreaRatio = s.MaxAreaRatio;
        PassScoreThreshold = s.PassScoreThreshold;
    }

    public void ApplyRecipe(Recipe recipe)
    {
        ThresholdValue = recipe.ThresholdValue;
        MinAreaRatio = recipe.MinAreaRatio;
        MaxAreaRatio = recipe.MaxAreaRatio;
        PassScoreThreshold = recipe.PassScoreThreshold;
    }

    public InspectionResult Inspect(Mat image)
    {
        _inspectionCount++;

        try
        {
            // Grayscale 변환
            using var gray = image.Channels() > 1
                ? image.CvtColor(ColorConversionCodes.BGR2GRAY)
                : image.Clone();

            // 이진화
            using var binary = gray.Threshold(ThresholdValue, 255, ThresholdTypes.Binary);

            // 윤곽선 검출
            binary.FindContours(out var contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            double imageArea = image.Width * image.Height;
            var validContours = contours
                .Where(c =>
                {
                    double area = Cv2.ContourArea(c);
                    double ratio = area / imageArea;
                    return ratio >= MinAreaRatio && ratio <= MaxAreaRatio;
                })
                .ToArray();

            // 패드 영역 비율로 양/불량 판정
            double totalPadArea = validContours.Sum(c => Cv2.ContourArea(c));
            double padRatio = totalPadArea / imageArea;
            double score = Math.Round(padRatio * 100, 2);
            bool isPass = validContours.Length > 0 && score > PassScoreThreshold;

            return new InspectionResult
            {
                Id = _inspectionCount,
                Timestamp = DateTime.Now,
                IsPass = isPass,
                Score = score,
                Description = isPass
                    ? $"OK - 패드 {validContours.Length}개 검출 ({score}%)"
                    : $"NG - 패드 미검출 또는 비정상 ({score}%)"
            };
        }
        catch (Exception ex)
        {
            return new InspectionResult
            {
                Id = _inspectionCount,
                Timestamp = DateTime.Now,
                IsPass = false,
                Score = 0,
                Description = $"검사 오류: {ex.Message}"
            };
        }
    }
}
