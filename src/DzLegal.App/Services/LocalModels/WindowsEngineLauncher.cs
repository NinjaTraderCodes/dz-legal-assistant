using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace DzLegal.App.Services.LocalModels;

public sealed class WindowsEngineLauncher(string executable, string root) : IEngineLauncher
{
    public IEngineProcess Start(int port)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        if (!File.Exists(executable)) throw new FileNotFoundException("The bundled local engine is missing.");
        EngineBundle.Verify(Path.GetDirectoryName(Path.GetFullPath(executable))!);
        return WindowsEngineProcess.Start(executable, root, port);
    }

    internal static ProcessStartInfo StartInfo(string executable, string root, int port)
    {
        var info = new ProcessStartInfo(Path.GetFullPath(executable))
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(executable))!,
        };
        info.ArgumentList.Add("serve");
        info.Environment.Clear();
        foreach (var name in new[] { "SystemRoot", "WINDIR", "COMSPEC", "PROCESSOR_ARCHITECTURE", "NUMBER_OF_PROCESSORS" })
            if (Environment.GetEnvironmentVariable(name) is { } value) info.Environment[name] = value;
        info.Environment["PATH"] = Environment.SystemDirectory;
        info.Environment["USERPROFILE"] = Path.Combine(root, "engine-home");
        info.Environment["LOCALAPPDATA"] = Path.Combine(root, "engine-home", "local");
        info.Environment["APPDATA"] = Path.Combine(root, "engine-home", "roaming");
        info.Environment["TEMP"] = info.Environment["TMP"] = Path.Combine(root, "engine-temp");
        info.Environment["OLLAMA_MODELS"] = Path.Combine(root, "models");
        info.Environment["OLLAMA_HOST"] = $"127.0.0.1:{port}";
        info.Environment["OLLAMA_NO_CLOUD"] = "1";
        info.Environment["OLLAMA_NOHISTORY"] = "1";
        info.Environment["OLLAMA_DEBUG"] = "ERROR";
        info.Environment["OLLAMA_DEBUG_LOG_REQUESTS"] = "false";
        info.Environment["OLLAMA_NUM_PARALLEL"] = "1";
        info.Environment["OLLAMA_MAX_LOADED_MODELS"] = "1";
        info.Environment["OLLAMA_CONTEXT_LENGTH"] = "4096";
        return info;
    }
}

[SupportedOSPlatform("windows")]
internal sealed partial class WindowsEngineProcess : IEngineProcess
{
    private readonly Process process;
    private readonly SafeFileHandle job;
    private bool disposed;

    private WindowsEngineProcess(Process process, SafeFileHandle job) { this.process = process; this.job = job; }

    public static WindowsEngineProcess Start(string executable, string root, int port)
    {
        Directory.CreateDirectory(root);
        foreach (var directory in new[] { "engine-home", "engine-home/local", "engine-home/roaming", "engine-temp", "models" })
            Directory.CreateDirectory(Path.Combine(root, directory));
        var job = CreateJobObjectW(IntPtr.Zero, IntPtr.Zero);
        if (job.IsInvalid) { job.Dispose(); throw new Win32Exception(Marshal.GetLastPInvokeError()); }
        try
        {
            var limits = new JobLimits { Basic = new BasicLimits { Flags = 0x2000 } }; // kill owned children when our job closes
            unsafe
            {
                if (!SetInformationJobObject(job, 9, &limits, (uint)sizeof(JobLimits)))
                    throw new Win32Exception(Marshal.GetLastPInvokeError());
            }
            var process = Process.Start(WindowsEngineLauncher.StartInfo(executable, root, port))
                ?? throw new IOException("The local engine could not start.");
            try
            {
                if (!AssignProcessToJobObject(job, process.Handle)) throw new Win32Exception(Marshal.GetLastPInvokeError());
                // Never persist the engine's raw output: it can contain identifiers or request text.
                process.OutputDataReceived += (_, _) => { };
                process.ErrorDataReceived += (_, _) => { };
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                return new WindowsEngineProcess(process, job);
            }
            catch
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
                process.Dispose();
                throw;
            }
        }
        catch { job.Dispose(); throw; }
    }

    public bool HasExited => disposed || process.HasExited;

    public bool OwnsListener(int port)
    {
        if (HasExited) return false;
        uint size = 0;
        _ = GetExtendedTcpTable(IntPtr.Zero, ref size, false, 2, 3, 0);
        if (size < 4 || size > 16 * 1024 * 1024) return false;
        var table = Marshal.AllocHGlobal((int)size);
        try
        {
            if (GetExtendedTcpTable(table, ref size, false, 2, 3, 0) != 0) return false;
            var count = Marshal.ReadInt32(table);
            for (var i = 0; i < count && 4L + (i + 1L) * 24 <= size; i++)
            {
                var row = IntPtr.Add(table, 4 + i * 24);
                var address = new IPAddress(BitConverter.GetBytes(Marshal.ReadInt32(row, 4)));
                var localPort = (ushort)IPAddress.NetworkToHostOrder((short)Marshal.ReadInt32(row, 8));
                if (address.Equals(IPAddress.Loopback) && localPort == port && Marshal.ReadInt32(row, 20) == process.Id)
                    return true;
            }
            return false;
        }
        finally { Marshal.FreeHGlobal(table); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        job.Dispose();
        process.Dispose();
    }

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial SafeFileHandle CreateJobObjectW(IntPtr attributes, IntPtr name);
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static unsafe partial bool SetInformationJobObject(SafeFileHandle job, int infoClass, void* information, uint length);
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AssignProcessToJobObject(SafeFileHandle job, IntPtr process);
    [LibraryImport("iphlpapi.dll")]
    private static partial uint GetExtendedTcpTable(IntPtr table, ref uint size, [MarshalAs(UnmanagedType.Bool)] bool order, uint family, uint tableClass, uint reserved);

    [StructLayout(LayoutKind.Sequential)]
    private struct BasicLimits
    {
        public long ProcessTime, JobTime;
        public uint Flags;
        public nuint MinWorkingSet, MaxWorkingSet;
        public uint ActiveProcesses;
        public nuint Affinity;
        public uint Priority, Scheduling;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct IoCounters { public ulong ReadOperations, WriteOperations, OtherOperations, ReadBytes, WriteBytes, OtherBytes; }
    [StructLayout(LayoutKind.Sequential)]
    private struct JobLimits
    {
        public BasicLimits Basic;
        public IoCounters Io;
        public nuint ProcessMemory, JobMemory, PeakProcessMemory, PeakJobMemory;
    }
}