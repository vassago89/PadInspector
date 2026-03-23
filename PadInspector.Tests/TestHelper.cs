using Microsoft.Extensions.Options;
using PadInspector.Configs;
using PadInspector.Services;

namespace PadInspector.Tests;

internal static class TestHelper
{
    internal static LogService CreateLogService() =>
        new(Options.Create(new LogSettings { EnableFileLog = false, MaxLogLines = 100 }));

    internal static AlarmService CreateAlarmService(int threshold = 3, bool enabled = true) =>
        new(Options.Create(new AlarmSettings
        {
            Enabled = enabled,
            ConsecutiveNgThreshold = threshold
        }), CreateLogService());

    internal static StatisticsService CreateStatisticsService(int maxHistory = 100) =>
        new(Options.Create(new InspectionSettings { MaxResultHistory = maxHistory }));
}
