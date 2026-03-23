using Microsoft.Extensions.Options;
using PadInspector.Configs;
using PadInspector.Services;

namespace PadInspector.Tests;

internal static class TestHelper
{
    internal static LogService CreateLogService() =>
        new(Options.Create(new LogSettings { EnableFileLog = false, MaxLogLines = 100 }));
}
