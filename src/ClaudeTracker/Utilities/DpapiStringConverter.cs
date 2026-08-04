using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClaudeTracker.Services;

namespace ClaudeTracker.Utilities;

/// <summary>
/// Encrypts a string property at rest using DPAPI (current-user scope) when it's serialized to JSON,
/// and transparently decrypts it back on read. Only for use on ClaudeTracker's own settings file
/// (%APPDATA%\ClaudeTracker\settings.json) — never on files shared with the Claude Code CLI, such as
/// ~/.claude/.credentials.json, which must stay in the CLI's own plaintext format.
/// Values from settings.json written before this converter existed are plain, unprefixed strings;
/// they're read back as-is and get encrypted the next time settings are saved.
/// </summary>
public class DpapiStringConverter : JsonConverter<string?>
{
    private const string Prefix = "dpapi:";

    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value) || !value.StartsWith(Prefix, StringComparison.Ordinal))
            return value;

        try
        {
            var encryptedBytes = Convert.FromBase64String(value[Prefix.Length..]);
            var plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (CryptographicException ex)
        {
            LoggingService.Instance.LogError("Failed to decrypt a protected settings value (different user/machine?)", ex);
            return null;
        }
        catch (FormatException ex)
        {
            LoggingService.Instance.LogError("Failed to decode a protected settings value", ex);
            return null;
        }
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (string.IsNullOrEmpty(value))
        {
            writer.WriteStringValue(value);
            return;
        }

        var plainBytes = Encoding.UTF8.GetBytes(value);
        var encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
        writer.WriteStringValue(Prefix + Convert.ToBase64String(encryptedBytes));
    }
}
