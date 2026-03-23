using OpenCvSharp;

namespace PadInspector.Services;

public class TestImageService : ITestImageService
{
    public Mat Generate()
    {
        var random = Random.Shared;
        var mat = new Mat(480, 640, MatType.CV_8UC1, Scalar.All(30));

        int padCount = random.Next(1, 6);
        for (int i = 0; i < padCount; i++)
        {
            int x = random.Next(50, 550);
            int y = random.Next(50, 400);
            int w = random.Next(30, 80);
            int h = random.Next(30, 80);
            Cv2.Rectangle(mat, new Rect(x, y, w, h), Scalar.All(200), -1);
        }

        using var noise = new Mat(mat.Size(), MatType.CV_8UC1);
        Cv2.Randn(noise, Scalar.All(0), Scalar.All(15));
        Cv2.Add(mat, noise, mat);

        return mat;
    }
}
