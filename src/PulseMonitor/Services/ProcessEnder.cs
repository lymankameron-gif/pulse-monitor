using System.Diagnostics;

namespace PulseMonitor.Services;

public sealed class ProcessEnder
{
    public EndResult End(int pid, string expectedName)
    {
        if (pid == Environment.ProcessId)
        {
            return EndResult.Rejected("Pulse Monitor cannot end its own process.");
        }

        Process? process;
        try
        {
            process = Process.GetProcessById(pid);
        }
        catch (ArgumentException)
        {
            return EndResult.Gone();
        }

        using (process)
        {
            string currentName;
            try
            {
                currentName = process.ProcessName;
            }
            catch
            {
                return EndResult.Gone();
            }

            if (!string.Equals(currentName, expectedName, StringComparison.OrdinalIgnoreCase))
            {
                return EndResult.Gone();
            }

            try
            {
                process.Kill(entireProcessTree: false);
                process.WaitForExit(3000);
                return EndResult.Ok();
            }
            catch (Exception ex)
            {
                return EndResult.Rejected(ex.Message);
            }
        }
    }
}

public sealed class EndResult
{
    private EndResult(EndOutcome outcome, string? message)
    {
        Outcome = outcome;
        Message = message;
    }

    public EndOutcome Outcome { get; }
    public string? Message { get; }

    public static EndResult Ok() => new(EndOutcome.Ok, null);
    public static EndResult Gone() => new(EndOutcome.Gone, "That process is no longer running.");
    public static EndResult Rejected(string message) => new(EndOutcome.Rejected, message);
}

public enum EndOutcome
{
    Ok,
    Gone,
    Rejected,
}
