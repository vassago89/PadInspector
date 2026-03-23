using Microsoft.Extensions.Options;
using PadInspector.Configs;

namespace PadInspector.Services;

public class IOOutputService : IIOOutputService
{
    private readonly IIOService _ioService;
    private readonly ILogService _logService;
    private readonly IOSettings _ioSettings;

    public IOOutputService(IIOService ioService, ILogService logService, IOptions<IOSettings> ioOptions)
    {
        _ioService = ioService;
        _logService = logService;
        _ioSettings = ioOptions.Value;
    }

    public void OutputResult(int cameraIndex, bool isPass)
    {
        var (passChannel, failChannel) = cameraIndex == 0
            ? (_ioSettings.Camera1PassChannel, _ioSettings.Camera1FailChannel)
            : (_ioSettings.Camera2PassChannel, _ioSettings.Camera2FailChannel);

        var activeChannel = isPass ? passChannel : failChannel;
        _ioService.SetOutput(activeChannel, true);
        _ = ResetOutputAfterDelayAsync(activeChannel, _ioSettings.OutputPulseMs);
    }

    private async Task ResetOutputAfterDelayAsync(int channel, int delayMs)
    {
        try
        {
            await Task.Delay(delayMs);
            _ioService.SetOutput(channel, false);
        }
        catch (Exception ex)
        {
            _logService.Log("ERR", $"IO output reset failed: {ex.Message}");
        }
    }
}
