using System.Runtime.InteropServices;
using System.Text.Json;
using OpenDriverUpdater.Infrastructure.Hardware;
using OpenDriverUpdater.Core.Interfaces;

namespace OpenDriverUpdater.Bridge;

public static class NativeExports
{
    private static readonly ILoggerService Logger = new NullLoggerService();

    [UnmanagedCallersOnly(EntryPoint = "odu_cs_scan_hardware_json")]
    public static unsafe char* ScanHardwareJson()
    {
        try
        {
            var scanner = new WmiHardwareScanner(Logger);
            var devices = scanner.ScanAsync().GetAwaiter().GetResult();
            var json = JsonSerializer.Serialize(devices);
            return (char*)Marshal.StringToCoTaskMemUTF8(json);
        }
        catch (Exception ex)
        {
            var errJson = JsonSerializer.Serialize(new { error = ex.Message });
            return (char*)Marshal.StringToCoTaskMemUTF8(errJson);
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "odu_cs_free_string")]
    public static unsafe void FreeString(char* ptr)
    {
        if (ptr != null)
        {
            Marshal.FreeCoTaskMem((IntPtr)ptr);
        }
    }
}

internal sealed class NullLoggerService : ILoggerService
{
    public void Debug(string message, params object[] args) { }
    public void Info(string message, params object[] args) { }
    public void Warn(string message, params object[] args) { }
    public void Error(string message, Exception? exception = null, params object[] args) { }
}
