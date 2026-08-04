using System.Text.Json.Serialization;

namespace ClaudeTracker.Models;

/// <summary>How the Watchdog resumes an interrupted CLI session once quota resets.</summary>
public enum WatchdogMode
{
    /// <summary>Reopen a visible terminal, resume the session, and send the continuation prompt. Stays open for the user.</summary>
    Interactive,

    /// <summary>Resume headlessly via `claude --resume &lt;id&gt; -p`, no visible window. Gated by permission mode — see PermissionModeGate.</summary>
    Unattended
}

/// <summary>
/// A Claude Code session that was cut off by hitting the usage limit, captured at the exact
/// moment the limit was hit (see IWatchdogService), and waiting to be resumed once
/// SessionResetAtUtc passes.
/// </summary>
public class PendingResume
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("cwd")]
    public string Cwd { get; set; } = string.Empty;

    /// <summary>The permission mode the original session was running under (e.g. "default",
    /// "acceptEdits", "bypassPermissions") — carried forward so a resume never runs with a
    /// broader trust level than the user already chose for this project.</summary>
    [JsonPropertyName("permissionMode")]
    public string PermissionMode { get; set; } = string.Empty;

    [JsonPropertyName("limitHitAtUtc")]
    public DateTime LimitHitAtUtc { get; set; }

    [JsonPropertyName("sessionResetAtUtc")]
    public DateTime SessionResetAtUtc { get; set; }

    [JsonPropertyName("attemptCount")]
    public int AttemptCount { get; set; }

    [JsonPropertyName("lastAttemptUtc")]
    public DateTime? LastAttemptUtc { get; set; }

    /// <summary>"Success" | "Failed" | "Retrying" | "SkippedRequiresApproval" | null (not yet attempted)</summary>
    [JsonPropertyName("lastOutcome")]
    public string? LastOutcome { get; set; }

    /// <summary>Manual per-item override — flip off to keep this specific captured session out
    /// of auto-resume without touching the project allowlist or the master toggle.</summary>
    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; } = true;

    [JsonIgnore]
    public string ProjectName => string.IsNullOrEmpty(Cwd)
        ? "Unknown"
        : System.IO.Path.GetFileName(Cwd.TrimEnd('\\', '/')) is { Length: > 0 } name ? name : Cwd;

    /// <summary>Scratch field backing the reset-time editor in the Watchdog dashboard — never
    /// persisted, refreshed from SessionResetAtUtc whenever the PENDING list is redrawn.</summary>
    [JsonIgnore]
    public string ResetTimeInputText { get; set; } = string.Empty;

    /// <summary>Set when the last edit to ResetTimeInputText failed to parse; null when clean.</summary>
    [JsonIgnore]
    public string? ResetTimeError { get; set; }
}

public enum WatchdogEventKind
{
    ResumeLaunched,
    ResumeSucceeded,
    ResumeFailed,
    RetryScheduled,
    RetryExhausted,
    SkippedRequiresApproval
}

/// <summary>A single entry in the Watchdog's persisted activity history, shown in the dashboard tab.</summary>
public class WatchdogEvent
{
    [JsonPropertyName("timestampUtc")]
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("cwd")]
    public string Cwd { get; set; } = string.Empty;

    [JsonPropertyName("kind")]
    public WatchdogEventKind Kind { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonIgnore]
    public string ProjectName => string.IsNullOrEmpty(Cwd)
        ? "Unknown"
        : System.IO.Path.GetFileName(Cwd.TrimEnd('\\', '/')) is { Length: > 0 } name ? name : Cwd;
}
