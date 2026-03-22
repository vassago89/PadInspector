using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using OpenCvSharp;
using PadInspector.Configs;
using PadInspector.Models;
using PadInspector.Services;

namespace PadInspector.ViewModels;

public partial class CameraViewModel : ObservableObject, IDisposable
{
    private readonly ICameraService _cameraService;

    [ObservableProperty] private BitmapSource? _image;
    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private InspectionResult? _lastResult;

    public string Name { get; }

    /// <summary>
    /// sender = this CameraViewModel
    /// </summary>
    public event EventHandler<Mat>? ImageAcquired;

    public CameraViewModel(CameraConfig config, ICameraServiceFactory cameraFactory)
    {
        Name = config.Name;
        _cameraService = cameraFactory.Create(config);
        _cameraService.ImageGrabbed += OnImageGrabbed;
    }

    private void OnImageGrabbed(object? sender, Mat image)
    {
        ImageAcquired?.Invoke(this, image);
    }

    public async Task<bool> ConnectAsync()
    {
        IsConnected = await _cameraService.ConnectAsync();
        return IsConnected;
    }

    public void Disconnect()
    {
        _cameraService.Disconnect();
        IsConnected = false;
    }

    public Task StartGrabAsync() => _cameraService.StartGrabAsync();

    public void StopGrab() => _cameraService.StopGrab();

    public void ApplyExposure(double exposureUs) => _cameraService.SetExposure(exposureUs);
    public void ApplyGain(double gainDb) => _cameraService.SetGain(gainDb);

    public void UpdateDisplay(BitmapSource bitmapSource, InspectionResult result)
    {
        Image = bitmapSource;
        LastResult = result;
    }

    public void Dispose()
    {
        _cameraService.ImageGrabbed -= OnImageGrabbed;
        _cameraService.Dispose();
    }
}
