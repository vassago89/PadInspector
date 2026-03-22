namespace PadInspector.Models;

/// <summary>
/// 검사 레시피 - 촬상 대상별 파라미터 세트
/// </summary>
public class Recipe
{
    public string Name { get; set; } = "Default";
    public string Description { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime ModifiedAt { get; set; } = DateTime.Now;

    // 카메라 파라미터
    public string PixelFormat { get; set; } = "Mono8";
    public string TriggerMode { get; set; } = "On";
    public string TriggerSource { get; set; } = "Line0";
    public int ExposureTimeUs { get; set; } = 500;
    public int GainDb { get; set; } = 0;

    // 검사 파라미터 (공통)
    public double ThresholdValue { get; set; } = 128;
    public double MinAreaRatio { get; set; } = 0.01;
    public double MaxAreaRatio { get; set; } = 0.5;
    public double PassScoreThreshold { get; set; } = 5.0;

    // 카메라별 ROI (비율 기반 0.0~1.0)
    public RoiRect Camera1Roi { get; set; } = new();
    public RoiRect Camera2Roi { get; set; } = new();

    // IO 파라미터
    public int TriggerChannel { get; set; } = 0;
    public int TriggerIntervalMs { get; set; } = 2000;
}

/// <summary>
/// ROI 영역 (비율 기반, 0.0~1.0)
/// </summary>
public class RoiRect
{
    public double X { get; set; } = 0;
    public double Y { get; set; } = 0;
    public double Width { get; set; } = 1;
    public double Height { get; set; } = 1;

    public bool IsFullImage => X <= 0 && Y <= 0 && Width >= 1 && Height >= 1;

    /// <summary>
    /// 모든 값을 0~1 범위로 제한
    /// </summary>
    public RoiRect Clamp() => new()
    {
        X = Math.Clamp(X, 0, 1),
        Y = Math.Clamp(Y, 0, 1),
        Width = Math.Clamp(Width, 0, 1),
        Height = Math.Clamp(Height, 0, 1)
    };
}
