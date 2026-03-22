using System.Runtime.InteropServices;
using MvCameraControl;
using OpenCvSharp;
using PadInspector.Configs;

namespace PadInspector.Services;

/// <summary>
/// HIK 라인스캔 카메라 서비스 (MVS SDK - MvCameraControl.Net)
/// IO 신호로 촬영 시작/완료를 제어하는 하드웨어 트리거 방식
/// </summary>
public class HikCameraService : ICameraService
{
    public event EventHandler<Mat>? ImageGrabbed;

    private readonly CameraConfig _config;
    private readonly ILogService _logService;
    private IDevice? _device;
    private CancellationTokenSource? _grabCts;
    private volatile bool _isGrabbing;

    public string Name => _config.Name;
    public bool IsConnected => _device != null;
    public bool IsGrabbing => _isGrabbing;

    public HikCameraService(CameraConfig config, ILogService logService)
    {
        _config = config;
        _logService = logService;
    }

    public async Task<bool> ConnectAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                int ret = DeviceEnumerator.EnumDevices(
                    DeviceTLayerType.MvGigEDevice | DeviceTLayerType.MvUsbDevice,
                    out var deviceInfoList);

                if (ret != 0 || deviceInfoList == null || deviceInfoList.Count == 0)
                    return false;

                if (string.IsNullOrEmpty(_config.SerialNumber))
                    return false;

                IDeviceInfo? targetDevice = null;
                foreach (var devInfo in deviceInfoList)
                {
                    if (devInfo.SerialNumber == _config.SerialNumber)
                    {
                        targetDevice = devInfo;
                        break;
                    }
                }
                if (targetDevice == null) return false;

                _device = DeviceFactory.CreateDevice(targetDevice);
                _device.Open();
                ConfigureLineScan();

                return true;
            }
            catch (Exception ex)
            {
                _logService.Log("ERR", $"[{_config.Name}] 카메라 연결 실패: {ex.Message}");
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
            _device.Parameters.SetEnumValueByString("TriggerMode", _config.TriggerMode);
            _device.Parameters.SetEnumValueByString("TriggerSource", _config.TriggerSource);
            _device.Parameters.SetEnumValueByString("TriggerActivation", _config.TriggerActivation);
            _device.Parameters.SetEnumValueByString("PixelFormat", _config.PixelFormat);
        }
        catch (Exception ex)
        {
            _logService.Log("ERR", $"[{_config.Name}] 파라미터 설정 실패: {ex.Message}");
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

        _grabCts = new CancellationTokenSource();
        var token = _grabCts.Token;
        _ = Task.Run(() => GrabLoop(token), token);
    }

    private void GrabLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _device != null)
        {
            try
            {
                int ret = _device.StreamGrabber.GetImageBuffer((uint)_config.GrabTimeoutMs, out var frameOut);
                if (ret == 0 && frameOut != null)
                {
                    var mat = ConvertToMat(frameOut);
                    if (mat != null)
                        ImageGrabbed?.Invoke(this, mat);
                    _device.StreamGrabber.FreeImageBuffer(frameOut);
                }
            }
            catch when (ct.IsCancellationRequested)
            {
                break;
            }
            catch
            {
                // Grab timeout - normal when waiting for hardware trigger
            }
        }
    }

    private Mat? ConvertToMat(IFrameOut frameOut)
    {
        try
        {
            var image = frameOut.Image;
            int width = (int)image.Width;
            int height = (int)image.Height;

            byte[] pixelData = image.PixelData;
            int expectedSize = width * height;
            if (pixelData.Length < expectedSize)
            {
                _logService.Log("ERR", $"[{_config.Name}] 픽셀 데이터 크기 불일치: expected={expectedSize}, actual={pixelData.Length}");
                return null;
            }

            var mat = new Mat(height, width, MatType.CV_8UC1);
            Marshal.Copy(pixelData, 0, mat.Data, expectedSize);
            return mat;
        }
        catch (Exception ex)
        {
            _logService.Log("ERR", $"[{_config.Name}] Mat 변환 실패: {ex.Message}");
            return null;
        }
    }

    public void StopGrab()
    {
        _isGrabbing = false;
        _grabCts?.Cancel();
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

    public void SetExposure(double exposureUs)
    {
        try { _device?.Parameters.SetFloatValue("ExposureTime", (float)exposureUs); }
        catch (Exception ex) { _logService.Log("ERR", $"[{_config.Name}] Exposure 설정 실패: {ex.Message}"); }
    }

    public void SetGain(double gainDb)
    {
        try { _device?.Parameters.SetFloatValue("Gain", (float)gainDb); }
        catch (Exception ex) { _logService.Log("ERR", $"[{_config.Name}] Gain 설정 실패: {ex.Message}"); }
    }

    public void Dispose()
    {
        Disconnect();
        _grabCts?.Dispose();
    }
}
