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

    public InspectionResult Inspect(Mat image, RoiRect? roi = null)
    {
        _inspectionCount++;

        try
        {
            // ROI 크롭
            using var cropped = CropRoi(image, roi);
            var target = cropped ?? image;

            // Grayscale 변환
            using var gray = target.Channels() > 1
                ? target.CvtColor(ColorConversionCodes.BGR2GRAY)
                : target.Clone();

            // 이진화
            using var binary = gray.Threshold(ThresholdValue, 255, ThresholdTypes.Binary);

            // 윤곽선 검출
            binary.FindContours(out var contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            double imageArea = target.Width * target.Height;
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
                    ? $"OK - 패드 {validContours.Length}개 ({score}%)"
                    : $"NG - ({score}%)"
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

    private static Mat? CropRoi(Mat image, RoiRect? roi)
    {
        if (roi == null || roi.IsFullImage) return null;

        int x = Math.Max(0, (int)(roi.X * image.Width));
        int y = Math.Max(0, (int)(roi.Y * image.Height));
        int w = Math.Min(image.Width - x, (int)(roi.Width * image.Width));
        int h = Math.Min(image.Height - y, (int)(roi.Height * image.Height));

        if (w <= 0 || h <= 0) return null;

        return new Mat(image, new Rect(x, y, w, h));
    }
}
