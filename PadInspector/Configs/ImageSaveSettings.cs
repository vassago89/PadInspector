namespace PadInspector.Configs;

public class ImageSaveSettings
{
    public bool Enabled { get; set; } = true;
    public bool SaveOk { get; set; } = false;
    public bool SaveNg { get; set; } = true;
    public string BasePath { get; set; } = "Images";
    public string Format { get; set; } = "bmp";
    public int MaxDaysToKeep { get; set; } = 30;
}
