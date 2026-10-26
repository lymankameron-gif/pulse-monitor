namespace PulseMonitor.Models;

public sealed class ProcessRow
{
    public required string Name { get; init; }
    public required int Pid { get; init; }
    public double? CpuPercent { get; init; }
    public double MemoryMb { get; init; }

    public string CpuText => CpuPercent is null ? "—" : $"{CpuPercent.Value:0.0}%";
    public string MemoryMbText => $"{MemoryMb:0.0}";
}
