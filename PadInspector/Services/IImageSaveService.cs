using OpenCvSharp;
using PadInspector.Models;

namespace PadInspector.Services;

public interface IImageSaveService
{
    string? Save(Mat image, InspectionResult result);
}
