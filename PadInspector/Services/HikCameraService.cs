using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;
using MvCameraControl;
using OpenCvSharp;
using PadInspector.Configs;

namespace PadInspector.Services;

/// <summary>
/// HIK 라인스캔 카메라 서비스 (MVS SDK - MvCameraControl.Net)
/// </summary>
public class HikCameraService : ICameraService
{
    public event EventHandler<Mat>? ImageGrabbed;

    private readonly CameraSettings _settings;
    private IDevice? _device;
    private bool _isGrabbing;

    public bool IsConnected => _device != null;
    public bool IsGrabbing => _isGrabbing;

    public HikCameraService(IOptions<CameraSettings> options)
    {
        _settings = options.Value;
    }

    public async Task<bool> ConnectAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                // 디바이스 열거
                int ret = DeviceEnumerator.EnumDevices(
                    DeviceTLayerType.MvGigEDevice | DeviceTLayerType.MvUsbDevice,
                    out var deviceInfoList);

                if (ret != 0 || deviceInfoList == null || deviceInfoList.Count == 0)
                    return false;

                // 첫 번째 카메라에 연결
                _device = DeviceFactory.CreateDevice(deviceInfoList[0]);
                _device.Open();

                // 라인스캔 카메라 설정
                ConfigureLineScan();

                return true;
            }
            catch
            {
                _device = null;
                return false;
            }
        });
    }

    private void ConfigureLineScan()
    {
        if (_device == null) return;

        try
        {
            _device.Parameters.SetEnumValueByString("TriggerMode", _settings.TriggerMode);
            _device.Parameters.SetEnumValueByString("TriggerSource", _settings.TriggerSource);
            _device.Parameters.SetEnumValueByString("PixelFormat", _settings.PixelFormat);
        }
        catch
        {
            // 카메라에 따라 지원하지 않는 파라미터가 있을 수 있음
        }
    }

    public async Task StartGrabAsync()
    {
        if (_device == null || _isGrabbing) return;

        await Task.Run(() =>
        {
            _device.StreamGrabber.StartGrabbing();
            _isGrabbing = true;
        });

        _ = Task.Run(GrabLoop);
    }

    private void GrabLoop()
    {
        while (_isGrabbing && _device != null)
        {
            try
            {
                int ret = _device.StreamGrabber.GetImageBuffer((uint)_settings.GrabTimeoutMs, out var frameOut);
                if (ret == 0 && frameOut != null)
                {
                    var mat = ConvertToMat(frameOut);
                    if (mat != null)
                    {
                        ImageGrabbed?.Invoke(this, mat);
                    }
                    _device.StreamGrabber.FreeImageBuffer(frameOut);
                }
            }
            catch
            {
                // Timeout or grab error - continue
            }
        }
    }

    private static Mat? ConvertToMat(IFrameOut frameOut)
    {
        try
        {
            var image = frameOut.Image;
            int width = (int)image.Width;
            int height = (int)image.Height;

            // PixelData returns managed byte[] (copy from unmanaged)
            byte[] pixelData = image.PixelData;
            var mat = new Mat(height, width, MatType.CV_8UC1);
            Marshal.Copy(pixelData, 0, mat.Data, pixelData.Length);
            return mat;
        }
        catch
        {
            return null;
        }
    }

    public async Task<Mat?> GrabSingleAsync()
    {
        if (_device == null) return null;

        return await Task.Run(() =>
        {
            try
            {
                // 소프트웨어 트리거로 전환
                _device.Parameters.SetEnumValueByString("TriggerSource", "Software");
                _device.StreamGrabber.StartGrabbing();
                _device.Parameters.SetCommandValue("TriggerSoftware");

                int ret = _device.StreamGrabber.GetImageBuffer((uint)_settings.SingleGrabTimeoutMs, out var frameOut);
                Mat? mat = null;
                if (ret == 0 && frameOut != null)
                {
                    mat = ConvertToMat(frameOut);
                    _device.StreamGrabber.FreeImageBuffer(frameOut);
                }

                _device.StreamGrabber.StopGrabbing();
                // 외부 트리거로 복원
                _device.Parameters.SetEnumValueByString("TriggerSource", _settings.TriggerSource);

                return mat;
            }
            catch
            {
                return null;
            }
        });
    }

    public void StopGrab()
    {
        _isGrabbing = false;
        try
        {
            _device?.StreamGrabber.StopGrabbing();
        }
        catch { }
    }

    public void Disconnect()
    {
        StopGrab();
        try
        {
            _device?.Close();
        }
        catch { }
        _device = null;
    }

    public void Dispose()
    {
        Disconnect();
    }
}
