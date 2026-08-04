namespace ClaudeTracker.Utilities;

/// <summary>
/// Pure decision logic for the Watchdog feature (limit-hit/reset detection, the permission-mode
/// safety gate, retry backoff, and failure-phrase matching), factored out of WatchdogService and
/// TerminalLauncher so it can be unit-tested without the live timer/profile/session services.
/// </summary>
public static class WatchdogLogic
{
    /// <summary>True when usage crossed from below the limit threshold to at/above it — the
    /// exact instant a session was cut off.</summary>
    public static bool IsLimitHitTransition(double? previousPercentage, double newPercentage) =>
        (previousPercentage ?? 0) < Constants.Watchdog.LimitReachedThreshold
        && newPercentage >= Constants.Watchdog.LimitReachedThreshold;

    /// <summary>True when usage crossed from at/above the limit threshold back below it — a
    /// confirmed quota reset.</summary>
    public static bool IsResetTransition(double? previousPercentage, double newPercentage) =>
        (previousPercentage ?? 0) >= Constants.Watchdog.LimitReachedThreshold
        && newPercentage < Constants.Watchdog.LimitReachedThreshold;

    /// <summary>Whether a permission mode is verified to never block on an interactive prompt —
    /// the only modes Unattended auto-resume is allowed to run under. Never widen this to make
    /// Unattended "just work"; it must only reuse trust the user already granted.</summary>
    public static bool IsNonPromptingPermissionMode(string permissionMode) =>
        Constants.Watchdog.NonPromptingPermissionModes.Contains(permissionMode);

    /// <summary>Whether resume output indicates a genuine failure (limit/rate/quota error) rather
    /// than a successful completion — corroborates the process exit code.</summary>
    public static bool ContainsFailurePhrase(string text) =>
        Constants.Watchdog.FailurePhrases.Any(p => text.Contains(p, StringComparison.OrdinalIgnoreCase));

    /// <summary>Requires a non-empty CLI response in addition to a zero exit code. A silent
    /// process is not enough evidence that a headless resume actually ran.</summary>
    public static bool HasResumeOutput(string text) => !string.IsNullOrWhiteSpace(text);

    /// <summary>Backoff delay before the next retry, given the attempt count that just failed
    /// (1-based). Clamps to the last configured backoff beyond the configured list length.</summary>
    public static TimeSpan GetRetryBackoff(int failedAttemptCount)
    {
        var backoffs = Constants.Watchdog.RetryBackoff;
        var index = Math.Clamp(failedAttemptCount - 1, 0, backoffs.Length - 1);
        return backoffs[index];
    }

    /// <summary>Whether a captured PendingResume is too old to still be actionable — the user has
    /// likely moved on.</summary>
    public static bool IsStale(DateTime limitHitAtUtc, DateTime nowUtc) =>
        (nowUtc - limitHitAtUtc).TotalHours > Constants.Watchdog.PendingResumeMaxAgeHours;

    /// <summary>Whether a PendingResume's reset time has arrived.</summary>
    public static bool IsReadyToResume(DateTime sessionResetAtUtc, DateTime nowUtc) =>
        nowUtc >= sessionResetAtUtc;

    /// <summary>Whether a project directory is eligible for auto-resume under the user's
    /// allowlist. An empty allowlist means unrestricted (every project is eligible) — this
    /// keeps existing behavior unchanged for users who never touch the setting. A non-empty
    /// allowlist matches exactly or by subdirectory, case-insensitively (Windows paths).</summary>
    public static bool IsProjectAllowed(string cwd, IReadOnlyList<string> allowedProjects)
    {
        if (allowedProjects.Count == 0) return true;

        var normalizedCwd = NormalizeProjectPath(cwd);
        if (normalizedCwd.Length == 0) return false;
        return allowedProjects.Any(p =>
        {
            var normalized = NormalizeProjectPath(p);
            if (normalized.Length == 0) return false;
            return normalizedCwd.Equals(normalized, StringComparison.OrdinalIgnoreCase)
                || normalizedCwd.StartsWith(normalized + "\\", StringComparison.OrdinalIgnoreCase);
        });
    }

    private static string NormalizeProjectPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return string.Empty;

        var value = path.Trim().Replace('/', '\\');
        try
        {
            var full = System.IO.Path.GetFullPath(value);
            return full.TrimEnd('\\', '/');
        }
        catch (ArgumentException)
        {
            return value.TrimEnd('\\', '/');
        }
    }
}
