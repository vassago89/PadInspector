using Microsoft.Extensions.Options;
using OpenCvSharp;
using PadInspector.Configs;
using PadInspector.Models;

namespace PadInspector.Services;

/// <summary>
/// 패드 검사 서비스 - OpenCV 기반 영상 검사 + 오버레이 생성
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

    public (InspectionResult Result, Mat Overlay) Inspect(Mat image, RoiRect? roi = null)
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
            var validContours = new List<Point[]>();
            var invalidContours = new List<Point[]>();

            foreach (var c in contours)
            {
                double area = Cv2.ContourArea(c);
                double ratio = area / imageArea;
                if (ratio >= MinAreaRatio && ratio <= MaxAreaRatio)
                    validContours.Add(c);
                else
                    invalidContours.Add(c);
            }

            // 패드 영역 비율로 양/불량 판정
            double totalPadArea = validContours.Sum(c => Cv2.ContourArea(c));
            double padRatio = totalPadArea / imageArea;
            double score = Math.Round(padRatio * 100, 2);
            bool isPass = validContours.Count > 0 && score > PassScoreThreshold;

            var result = new InspectionResult
            {
                Id = _inspectionCount,
                Timestamp = DateTime.Now,
                IsPass = isPass,
                Score = score,
                PadCount = validContours.Count,
                Description = isPass
                    ? $"OK - 패드 {validContours.Count}개 ({score}%)"
                    : $"NG - ({score}%)"
            };

            var overlay = DrawOverlay(image, roi, validContours, invalidContours, isPass, score);
            return (result, overlay);
        }
        catch (Exception ex)
        {
            var result = new InspectionResult
            {
                Id = _inspectionCount,
                Timestamp = DateTime.Now,
                IsPass = false,
                Score = 0,
                Description = $"검사 오류: {ex.Message}"
            };

            var overlay = image.Channels() == 1
                ? image.CvtColor(ColorConversionCodes.GRAY2BGR)
                : image.Clone();
            return (result, overlay);
        }
    }

    private Mat DrawOverlay(Mat image, RoiRect? roi,
        List<Point[]> validContours, List<Point[]> invalidContours,
        bool isPass, double score)
    {
        var overlay = image.Channels() == 1
            ? image.CvtColor(ColorConversionCodes.GRAY2BGR)
            : image.Clone();

        // ROI offset
        int offsetX = 0, offsetY = 0;
        if (roi != null && !roi.IsFullImage)
        {
            offsetX = Math.Max(0, (int)(roi.X * image.Width));
            offsetY = Math.Max(0, (int)(roi.Y * image.Height));
            int rw = Math.Min(image.Width - offsetX, (int)(roi.Width * image.Width));
            int rh = Math.Min(image.Height - offsetY, (int)(roi.Height * image.Height));
            Cv2.Rectangle(overlay, new Rect(offsetX, offsetY, rw, rh), new Scalar(0, 200, 200), 1);
        }

        // Valid contours (green)
        foreach (var c in validContours)
        {
            var shifted = c.Select(p => new Point(p.X + offsetX, p.Y + offsetY)).ToArray();
            Cv2.DrawContours(overlay, [shifted], 0, new Scalar(0, 220, 0), 2);
        }

        // Invalid contours (dim red)
        foreach (var c in invalidContours)
        {
            var shifted = c.Select(p => new Point(p.X + offsetX, p.Y + offsetY)).ToArray();
            Cv2.DrawContours(overlay, [shifted], 0, new Scalar(0, 0, 150), 1);
        }

        // Result text (outline + fill for readability)
        var text = $"{(isPass ? "PASS" : "FAIL")} {score:F1}%  Pads:{validContours.Count}";
        var textColor = isPass ? new Scalar(0, 220, 0) : new Scalar(0, 0, 255);
        Cv2.PutText(overlay, text, new Point(8, 24), HersheyFonts.HersheySimplex, 0.6, new Scalar(0, 0, 0), 3);
        Cv2.PutText(overlay, text, new Point(8, 24), HersheyFonts.HersheySimplex, 0.6, textColor, 1);

        return overlay;
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
