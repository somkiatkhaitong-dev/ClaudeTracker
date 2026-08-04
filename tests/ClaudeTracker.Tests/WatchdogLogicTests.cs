using ClaudeTracker.Utilities;
using Xunit;

namespace ClaudeTracker.Tests;

public class WatchdogLogicTests
{
    // ── Limit-hit / reset transition detection ──

    [Fact]
    public void IsLimitHitTransition_True_WhenCrossingIntoLimit()
    {
        Assert.True(WatchdogLogic.IsLimitHitTransition(80.0, 99.6));
    }

    [Fact]
    public void IsLimitHitTransition_False_WhenAlreadyAtLimit()
    {
        Assert.False(WatchdogLogic.IsLimitHitTransition(99.6, 100.0));
    }

    [Fact]
    public void IsLimitHitTransition_False_WhenStillBelowThreshold()
    {
        Assert.False(WatchdogLogic.IsLimitHitTransition(80.0, 95.0));
    }

    [Fact]
    public void IsLimitHitTransition_TreatsNullPreviousAsZero()
    {
        Assert.True(WatchdogLogic.IsLimitHitTransition(null, 99.5));
    }

    [Fact]
    public void IsResetTransition_True_WhenCrossingBelowLimit()
    {
        Assert.True(WatchdogLogic.IsResetTransition(100.0, 0.0));
    }

    [Fact]
    public void IsResetTransition_False_WhenStillAtLimit()
    {
        Assert.False(WatchdogLogic.IsResetTransition(100.0, 99.6));
    }

    // ── Permission-mode safety gate ──

    [Theory]
    [InlineData("bypassPermissions", true)]
    [InlineData("BYPASSPERMISSIONS", true)] // case-insensitive
    [InlineData("acceptEdits", false)]      // can still prompt for non-edit tools
    [InlineData("default", false)]
    [InlineData("dontAsk", false)]
    [InlineData("auto", false)]
    [InlineData("plan", false)]
    [InlineData("", false)]
    public void IsNonPromptingPermissionMode_OnlyAllowsVerifiedModes(string mode, bool expected)
    {
        Assert.Equal(expected, WatchdogLogic.IsNonPromptingPermissionMode(mode));
    }

    // ── Failure-phrase matching ──

    [Theory]
    [InlineData("You have hit your usage limit for this session", true)]
    [InlineData("Error: rate limit exceeded, try again later", true)]
    [InlineData("quota exceeded for this billing period", true)]
    [InlineData("Task completed successfully, all tests pass", false)]
    [InlineData("", false)]
    public void ContainsFailurePhrase_DetectsKnownErrorText(string text, bool expected)
    {
        Assert.Equal(expected, WatchdogLogic.ContainsFailurePhrase(text));
    }

    // ── Retry backoff ──

    [Fact]
    public void GetRetryBackoff_IncreasesWithAttemptCount()
    {
        var first = WatchdogLogic.GetRetryBackoff(1);
        var second = WatchdogLogic.GetRetryBackoff(2);
        var third = WatchdogLogic.GetRetryBackoff(3);

        Assert.True(first < second);
        Assert.True(second < third);
    }

    [Fact]
    public void GetRetryBackoff_ClampsBeyondConfiguredList()
    {
        var atLimit = WatchdogLogic.GetRetryBackoff(Constants.Watchdog.RetryBackoff.Length);
        var beyond = WatchdogLogic.GetRetryBackoff(Constants.Watchdog.RetryBackoff.Length + 5);

        Assert.Equal(atLimit, beyond);
    }

    // ── Staleness / readiness ──

    [Fact]
    public void IsStale_False_WithinMaxAge()
    {
        var now = DateTime.UtcNow;
        Assert.False(WatchdogLogic.IsStale(now.AddHours(-1), now));
    }

    [Fact]
    public void IsStale_True_BeyondMaxAge()
    {
        var now = DateTime.UtcNow;
        Assert.True(WatchdogLogic.IsStale(now.AddHours(-(Constants.Watchdog.PendingResumeMaxAgeHours + 1)), now));
    }

    [Fact]
    public void IsReadyToResume_False_BeforeResetTime()
    {
        var now = DateTime.UtcNow;
        Assert.False(WatchdogLogic.IsReadyToResume(now.AddMinutes(5), now));
    }

    [Fact]
    public void IsReadyToResume_True_AtOrAfterResetTime()
    {
        var now = DateTime.UtcNow;
        Assert.True(WatchdogLogic.IsReadyToResume(now.AddSeconds(-1), now));
    }

    // ── Project allowlist ──

    [Fact]
    public void IsProjectAllowed_True_WhenListIsEmpty()
    {
        Assert.True(WatchdogLogic.IsProjectAllowed(@"C:\Projects\Anything", Array.Empty<string>()));
    }

    [Fact]
    public void IsProjectAllowed_True_ForExactMatch()
    {
        var allowed = new[] { @"C:\Projects\Foo" };
        Assert.True(WatchdogLogic.IsProjectAllowed(@"C:\Projects\Foo", allowed));
    }

    [Fact]
    public void IsProjectAllowed_True_ForExactMatch_CaseInsensitive()
    {
        var allowed = new[] { @"C:\Projects\Foo" };
        Assert.True(WatchdogLogic.IsProjectAllowed(@"c:\projects\foo", allowed));
    }

    [Fact]
    public void IsProjectAllowed_True_ForSubdirectory()
    {
        var allowed = new[] { @"C:\Projects\Foo" };
        Assert.True(WatchdogLogic.IsProjectAllowed(@"C:\Projects\Foo\src\sub", allowed));
    }

    [Fact]
    public void IsProjectAllowed_False_ForUnrelatedDirectory()
    {
        var allowed = new[] { @"C:\Projects\Foo" };
        Assert.False(WatchdogLogic.IsProjectAllowed(@"C:\Projects\Bar", allowed));
    }

    [Fact]
    public void IsProjectAllowed_False_ForSiblingWithSharedPrefix()
    {
        // "Foo2" starts with the string "Foo" but is not a subdirectory of "Foo" —
        // must not match on a bare string prefix.
        var allowed = new[] { @"C:\Projects\Foo" };
        Assert.False(WatchdogLogic.IsProjectAllowed(@"C:\Projects\Foo2", allowed));
    }

    [Fact]
    public void IsProjectAllowed_True_WithTrailingSlashOnAllowedEntry()
    {
        var allowed = new[] { @"C:\Projects\Foo\" };
        Assert.True(WatchdogLogic.IsProjectAllowed(@"C:\Projects\Foo\src", allowed));
    }

    [Fact]
    public void IsProjectAllowed_True_WhenSeparatorsUseDifferentStyles()
    {
        var allowed = new[] { "C:/Projects/Foo" };
        Assert.True(WatchdogLogic.IsProjectAllowed(@"C:\Projects\Foo\src", allowed));
    }

    [Fact]
    public void IsProjectAllowed_ResolvesDotSegmentsBeforeComparing()
    {
        var allowed = new[] { @"C:\Projects\Foo" };
        Assert.True(WatchdogLogic.IsProjectAllowed(@"C:\Projects\Foo\src\..", allowed));
    }

    [Theory]
    [InlineData("authentication failed")]
    [InlineData("session not found")]
    [InlineData("permission denied")]
    public void ContainsFailurePhrase_CatchesCliFailures(string output)
    {
        Assert.True(WatchdogLogic.ContainsFailurePhrase(output));
    }

    [Fact]
    public void HasResumeOutput_RejectsSilentProcess()
    {
        Assert.False(WatchdogLogic.HasResumeOutput(" \r\n "));
        Assert.True(WatchdogLogic.HasResumeOutput("continued"));
    }
}
