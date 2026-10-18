using System.Runtime.InteropServices;
using DzLegal.Presentation.Services;

namespace DzLegal.App.Services;

/// <summary>
/// Reads the Windows power state (GetSystemPowerStatus): on battery, or with the energy saver on. Elsewhere, or if the
/// call fails, it never warns.
/// </summary>
public sealed partial class WindowsPowerStatus : IPowerStatus
{
    private const byte OnBattery = 0;
    private const byte EnergySaverOn = 1;

    public bool IsSavingPower()
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        try
        {
            return GetSystemPowerStatus(out var status) && (status.AcLineStatus == OnBattery || status.SystemStatusFlag == EnergySaverOn);
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            return false;
        }
    }

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetSystemPowerStatus(out SystemPowerStatus status);

    [StructLayout(LayoutKind.Sequential)]
    private struct SystemPowerStatus
    {
        public byte AcLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public int BatteryLifeTime;
        public int BatteryFullLifeTime;
    }
}
