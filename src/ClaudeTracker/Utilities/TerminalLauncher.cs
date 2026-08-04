using System.ComponentModel;
using System.Diagnostics;
using ClaudeTracker.Services;

namespace ClaudeTracker.Utilities;

/// <summary>
/// Launches `claude --resume &lt;sessionId&gt;` to continue an interrupted CLI session, either in a
/// visible terminal (Interactive mode) or headlessly with a captured exit code (Unattended mode).
/// Verified flags against the installed CLI's `claude --help`: -r/--resume &lt;id&gt;, -p/--print,
/// --permission-mode &lt;mode&gt;, and a trailing positional prompt.
/// </summary>
public static class TerminalLauncher
{
    public class UnattendedResult
    {
        public bool Success { get; init; }
        public int ExitCode { get; init; }
        public string Output { get; init; } = string.Empty;
    }

    /// <summary>Opens a visible terminal (Windows Terminal, falling back to cmd.exe) that resumes
    /// the session and sends the continuation prompt. `/c` means the shell — and, for a WT tab
    /// under its default "graceful" behavior, the tab itself — closes on a clean exit; a failed
    /// run leaves the window open so the user can see what happened.</summary>
    public static bool LaunchInteractive(string cwd, string sessionId, string permissionMode, string prompt)
    {
        var claudeArgs = BuildClaudeArgs(sessionId, permissionMode, prompt, print: false);

        try
        {
            var psi = new ProcessStartInfo { FileName = "wt.exe", UseShellExecute = true };
            psi.ArgumentList.Add("-d");
            psi.ArgumentList.Add(cwd);
            psi.ArgumentList.Add("cmd.exe");
            psi.ArgumentList.Add("/c");
            psi.ArgumentList.Add("claude");
            foreach (var a in claudeArgs) psi.ArgumentList.Add(a);

            Process.Start(psi);
            return true;
        }
        catch (Win32Exception ex)
        {
            LoggingService.Instance.LogWarning($"wt.exe unavailable ({ex.Message}) — falling back to cmd.exe for resume");
            return LaunchViaCmdFallback(cwd, claudeArgs);
        }
        catch (Exception ex)
        {
            LoggingService.Instance.LogError("Failed to launch wt.exe for resume", ex);
            return LaunchViaCmdFallback(cwd, claudeArgs);
        }
    }

    private static bool LaunchViaCmdFallback(string cwd, List<string> claudeArgs)
    {
        try
        {
            var psi = new ProcessStartInfo { FileName = "cmd.exe", UseShellExecute = true, WorkingDirectory = cwd };
            psi.ArgumentList.Add("/c");
            psi.ArgumentList.Add("claude");
            foreach (var a in claudeArgs) psi.ArgumentList.Add(a);
            Process.Start(psi);
            return true;
        }
        catch (Exception ex)
        {
            LoggingService.Instance.LogError("Failed to launch fallback terminal for resume", ex);
            return false;
        }
    }

    /// <summary>Runs the resume headlessly (`--print`), no visible window. Success is decided by
    /// the process exit code, corroborated by scanning output for known limit/error phrases —
    /// a deterministic signal, not a Stop-hook timing guess.</summary>
    public static async Task<UnattendedResult> RunUnattendedAsync(string cwd, string sessionId, string permissionMode, string prompt)
    {
        var claudeArgs = BuildClaudeArgs(sessionId, permissionMode, prompt, print: true);

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = cwd
            };
            psi.ArgumentList.Add("/c");
            psi.ArgumentList.Add("claude");
            foreach (var a in claudeArgs) psi.ArgumentList.Add(a);

            using var process = Process.Start(psi);
            if (process == null)
                return new UnattendedResult { Success = false, ExitCode = -1, Output = "Failed to start process" };

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            var combined = $"{await stdoutTask}\n{await stderrTask}";

            var hasFailurePhrase = WatchdogLogic.ContainsFailurePhrase(combined);
            var hasOutput = WatchdogLogic.HasResumeOutput(combined);

            return new UnattendedResult
            {
                Success = process.ExitCode == 0 && !hasFailurePhrase && hasOutput,
                ExitCode = process.ExitCode,
                Output = combined.Length > 2000 ? combined[..2000] : combined
            };
        }
        catch (Exception ex)
        {
            LoggingService.Instance.LogError("Unattended resume failed to launch", ex);
            return new UnattendedResult { Success = false, ExitCode = -1, Output = ex.Message };
        }
    }

    private static List<string> BuildClaudeArgs(string sessionId, string permissionMode, string prompt, bool print)
    {
        var args = new List<string> { "--resume", sessionId };
        if (!string.IsNullOrEmpty(permissionMode))
        {
            args.Add("--permission-mode");
            args.Add(permissionMode);
        }
        if (print) args.Add("--print");
        args.Add(prompt);
        return args;
    }
}
