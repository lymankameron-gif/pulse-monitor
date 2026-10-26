namespace PulseMonitor.Models;

public sealed class ProcessRow
{
    public required string Name { get; init; }
    public required int Pid { get; init; }
    public double? CpuPercent { get; init; }
    public double MemoryMb { get; init; }

    public string CpuText { get; private set; } = "—";
    public string MemoryText { get; private set; } = "0";

    public void ApplyFormat(int cpuDecimals, MemoryUnit memoryUnit)
    {
        if (CpuPercent is null)
        {
            CpuText = "—";
        }
        else if (cpuDecimals <= 0)
        {
            CpuText = $"{CpuPercent.Value:0}%";
        }
        else
        {
            CpuText = $"{CpuPercent.Value:0.0}%";
        }

        MemoryText = memoryUnit == MemoryUnit.Gb
            ? $"{MemoryMb / 1024.0:0.00}"
            : $"{MemoryMb:0.0}";
    }
}
