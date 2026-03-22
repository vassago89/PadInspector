using Microsoft.Extensions.Options;
using PadInspector.Configs;
using PadInspector.Services;

namespace PadInspector.Tests;

public class IOOutputServiceTests
{
    private static (IOOutputService svc, VirtualIOService io) CreateService()
    {
        var ioSettings = Options.Create(new IOSettings
        {
            ChannelCount = 8,
            Camera1PassChannel = 0,
            Camera1FailChannel = 1,
            Camera2PassChannel = 2,
            Camera2FailChannel = 3,
            OutputPulseMs = 50
        });

        var io = new VirtualIOService(ioSettings);
        var svc = new IOOutputService(io, new FakeLogService(), ioSettings);
        return (svc, io);
    }

    [Fact]
    public void OutputResult_Camera1Pass_SetsCorrectChannel()
    {
        var (svc, io) = CreateService();

        svc.OutputResult(0, isPass: true);

        Assert.True(io.GetOutput(0)); // Camera1PassChannel
        Assert.False(io.GetOutput(1)); // Camera1FailChannel stays off
    }

    [Fact]
    public void OutputResult_Camera1Fail_SetsCorrectChannel()
    {
        var (svc, io) = CreateService();

        svc.OutputResult(0, isPass: false);

        Assert.False(io.GetOutput(0)); // Camera1PassChannel stays off
        Assert.True(io.GetOutput(1)); // Camera1FailChannel
    }

    [Fact]
    public void OutputResult_Camera2Pass_SetsCorrectChannel()
    {
        var (svc, io) = CreateService();

        svc.OutputResult(1, isPass: true);

        Assert.True(io.GetOutput(2)); // Camera2PassChannel
    }

    [Fact]
    public void OutputResult_Camera2Fail_SetsCorrectChannel()
    {
        var (svc, io) = CreateService();

        svc.OutputResult(1, isPass: false);

        Assert.True(io.GetOutput(3)); // Camera2FailChannel
    }

    [Fact]
    public async Task OutputResult_ResetsAfterPulse()
    {
        var (svc, io) = CreateService();

        svc.OutputResult(0, isPass: true);
        Assert.True(io.GetOutput(0));

        // 펄스 후 리셋 확인 (50ms + 여유)
        await Task.Delay(150);
        Assert.False(io.GetOutput(0));
    }
}
