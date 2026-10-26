using System.Diagnostics;
using System.Runtime.InteropServices;
using PulseMonitor.Models;

namespace PulseMonitor.Services;

public sealed class ProcessSampler
{
    private readonly Dictionary<int, TimeSpan> _previousCpu = new();
    private DateTime _previousSampleUtc = DateTime.MinValue;
    private readonly int _processorCount = Environment.ProcessorCount;
    private ulong _previousIdle;
    private ulong _previousKernel;
    private ulong _previousUser;
    private bool _hasPreviousSystemTimes;

    public ProcessSnapshot TakeSnapshot()
    {
        var now = DateTime.UtcNow;
        var elapsed = _previousSampleUtc == DateTime.MinValue
            ? TimeSpan.Zero
            : now - _previousSampleUtc;

        var nextCpu = new Dictionary<int, TimeSpan>();
        var rows = new List<ProcessRow>();

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                var name = string.IsNullOrWhiteSpace(process.ProcessName)
                    ? $"pid-{process.Id}"
                    : process.ProcessName;
                var pid = process.Id;
                double memoryMb;

                try
                {
                    memoryMb = process.WorkingSet64 / (1024.0 * 1024.0);
                }
                catch
                {
                    memoryMb = 0;
                }

                double? cpuPercent = null;
                try
                {
                    var total = process.TotalProcessorTime;
                    nextCpu[pid] = total;

                    if (elapsed > TimeSpan.Zero &&
                        _previousCpu.TryGetValue(pid, out var previous) &&
                        _processorCount > 0)
                    {
                        var delta = (total - previous).TotalMilliseconds;
                        var wall = elapsed.TotalMilliseconds * _processorCount;
                        if (wall > 0)
                        {
                            cpuPercent = Math.Clamp(100.0 * delta / wall, 0, 100);
                        }
                    }
                }
                catch
                {
                    // Protected processes may refuse CPU time; leave blank.
                }

                rows.Add(new ProcessRow
                {
                    Name = name,
                    Pid = pid,
                    CpuPercent = cpuPercent,
                    MemoryMb = memoryMb,
                });
            }
            catch
            {
                // Process may have exited while enumerating.
            }
            finally
            {
                process.Dispose();
            }
        }

        _previousCpu.Clear();
        foreach (var pair in nextCpu)
        {
            _previousCpu[pair.Key] = pair.Value;
        }

        _previousSampleUtc = now;

        GetMemoryStatus(out var usedMb, out var totalMb);
        var overallCpu = ReadOverallCpuPercent();

        return new ProcessSnapshot(rows, overallCpu, usedMb, totalMb);
    }

    private double ReadOverallCpuPercent()
    {
        if (!GetSystemTimes(out var idleTime, out var kernelTime, out var userTime))
        {
            return 0;
        }

        var idle = FileTimeToUInt64(idleTime);
        var kernel = FileTimeToUInt64(kernelTime);
        var user = FileTimeToUInt64(userTime);

        double percent = 0;
        if (_hasPreviousSystemTimes)
        {
            var idleDelta = idle - _previousIdle;
            var kernelDelta = kernel - _previousKernel;
            var userDelta = user - _previousUser;
            var totalDelta = kernelDelta + userDelta;
            if (totalDelta > 0)
            {
                // Kernel time includes idle time on Windows.
                var busy = totalDelta - idleDelta;
                percent = Math.Clamp(100.0 * busy / totalDelta, 0, 100);
            }
        }

        _previousIdle = idle;
        _previousKernel = kernel;
        _previousUser = user;
        _hasPreviousSystemTimes = true;
        return percent;
    }

    private static ulong FileTimeToUInt64(FILETIME time) =>
        ((ulong)time.dwHighDateTime << 32) | time.dwLowDateTime;

    private static void GetMemoryStatus(out double usedMb, out double totalMb)
    {
        var status = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        if (GlobalMemoryStatusEx(ref status))
        {
            totalMb = status.ullTotalPhys / (1024.0 * 1024.0);
            var availableMb = status.ullAvailPhys / (1024.0 * 1024.0);
            usedMb = Math.Max(0, totalMb - availableMb);
            return;
        }

        usedMb = 0;
        totalMb = 0;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(out FILETIME idleTime, out FILETIME kernelTime, out FILETIME userTime);

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FILETIME
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;
    }
}

public sealed record ProcessSnapshot(
    IReadOnlyList<ProcessRow> Rows,
    double OverallCpuPercent,
    double MemoryUsedMb,
    double MemoryTotalMb);
