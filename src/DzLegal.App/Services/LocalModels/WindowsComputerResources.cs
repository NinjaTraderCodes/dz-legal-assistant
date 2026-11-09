using System.Runtime.InteropServices;

namespace DzLegal.App.Services.LocalModels;

public interface IComputerResources
{
    ulong InstalledMemoryBytes { get; }
    ulong AvailableMemoryBytes { get; }
    long FreeDiskBytes(string path);
    long AllocatedFileBytes(string path) => File.Exists(path) ? new FileInfo(path).Length : 0;
}

public sealed partial class WindowsComputerResources : IComputerResources
{
    public ulong InstalledMemoryBytes => OperatingSystem.IsWindows() && GetPhysicallyInstalledSystemMemory(out var kb) ? kb * 1024 : 0;
    public ulong AvailableMemoryBytes
    {
        get
        {
            var status = new MemoryStatus { Length = (uint)Marshal.SizeOf<MemoryStatus>() };
            return OperatingSystem.IsWindows() && GlobalMemoryStatusEx(ref status) ? status.AvailablePhysical : 0;
        }
    }
    public long FreeDiskBytes(string path) => new DriveInfo(Path.GetPathRoot(Path.GetFullPath(path))!).AvailableFreeSpace;
    public long AllocatedFileBytes(string path)
    {
        if (!File.Exists(path)) return 0;
        if (!OperatingSystem.IsWindows()) return new FileInfo(path).Length;
        var low = GetCompressedFileSizeW(path, out var high);
        return low == uint.MaxValue && Marshal.GetLastPInvokeError() != 0 ? 0 : (long)(((ulong)high << 32) | low);
    }
    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    private static partial uint GetCompressedFileSizeW(string file, out uint high);

    public static string RecommendedModel(ulong installedMemoryBytes) => installedMemoryBytes >= 16UL * 1024 * 1024 * 1024 ? "gemma4:e4b" : "gemma3:4b";

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetPhysicallyInstalledSystemMemory(out ulong memoryInKilobytes);
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GlobalMemoryStatusEx(ref MemoryStatus status);
    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatus
    {
        public uint Length, Load;
        public ulong TotalPhysical, AvailablePhysical, TotalPageFile, AvailablePageFile, TotalVirtual, AvailableVirtual, Extended;
    }
}