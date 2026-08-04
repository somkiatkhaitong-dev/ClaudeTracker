using System.Text.Json;
using ClaudeTracker.Models;
using ClaudeTracker.Utilities;
using Xunit;

namespace ClaudeTracker.Tests;

public class DpapiStringConverterTests
{
    [Fact]
    public void Write_EncryptsValue_NotStoredAsPlaintext()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new DpapiStringConverter());

        var json = JsonSerializer.Serialize("sk-ant-super-secret-token", options);

        Assert.DoesNotContain("sk-ant-super-secret-token", json);
        Assert.Contains("dpapi:", json);
    }

    [Fact]
    public void RoundTrip_EncryptThenDecrypt_ReturnsOriginalValue()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new DpapiStringConverter());

        var json = JsonSerializer.Serialize("sk-ant-super-secret-token", options);
        var result = JsonSerializer.Deserialize<string>(json, options);

        Assert.Equal("sk-ant-super-secret-token", result);
    }

    [Fact]
    public void Read_PlaintextFromOlderSettingsFile_ReturnsAsIs()
    {
        // settings.json written before this converter existed has unencrypted values;
        // they must still load without crashing or losing data.
        var options = new JsonSerializerOptions();
        options.Converters.Add(new DpapiStringConverter());

        var json = "\"sk-ant-legacy-plaintext-token\"";
        var result = JsonSerializer.Deserialize<string>(json, options);

        Assert.Equal("sk-ant-legacy-plaintext-token", result);
    }

    [Fact]
    public void Read_NullOrEmpty_ReturnsAsIs()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new DpapiStringConverter());

        Assert.Null(JsonSerializer.Deserialize<string?>("null", options));
        Assert.Equal("", JsonSerializer.Deserialize<string>("\"\"", options));
    }

    [Fact]
    public void Profile_SerializeDeserialize_CredentialFieldsRoundTripAndAreEncryptedAtRest()
    {
        var profile = new Profile
        {
            ClaudeSessionKey = "sk-ant-session-key-value",
            ApiSessionKey = "sk-ant-api03-value",
            CliCredentialsJSON = "{\"accessToken\":\"secret\"}",
            OrganizationId = "org-not-secret" // unaffected field, should stay plaintext
        };

        var json = JsonSerializer.Serialize(profile);

        Assert.DoesNotContain("sk-ant-session-key-value", json);
        Assert.DoesNotContain("sk-ant-api03-value", json);
        Assert.DoesNotContain("\"accessToken\":\"secret\"", json);
        Assert.Contains("org-not-secret", json); // non-sensitive field left plaintext

        var restored = JsonSerializer.Deserialize<Profile>(json);

        Assert.NotNull(restored);
        Assert.Equal(profile.ClaudeSessionKey, restored!.ClaudeSessionKey);
        Assert.Equal(profile.ApiSessionKey, restored.ApiSessionKey);
        Assert.Equal(profile.CliCredentialsJSON, restored.CliCredentialsJSON);
        Assert.Equal(profile.OrganizationId, restored.OrganizationId);
    }
}
