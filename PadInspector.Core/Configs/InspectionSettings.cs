namespace PadInspector.Configs;

public class InspectionSettings
{
    public double ThresholdValue { get; set; } = 128;
    public double MinAreaRatio { get; set; } = 0.01;
    public double MaxAreaRatio { get; set; } = 0.5;
    public double PassScoreThreshold { get; set; } = 5.0;
    public int MaxResultHistory { get; set; } = 100;
}
