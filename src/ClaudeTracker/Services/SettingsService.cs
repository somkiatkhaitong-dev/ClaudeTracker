using System.IO;
using System.Text.Json;
using ClaudeTracker.Models;
using ClaudeTracker.Services.Interfaces;
using ClaudeTracker.Utilities;

namespace ClaudeTracker.Services;

public class SettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _settingsPath;
    private readonly object _lock = new();

    public AppSettings Settings { get; private set; } = new();

    public event EventHandler? SettingsChanged;

    public SettingsService()
    {
        _settingsPath = Constants.SettingsFilePath;
        EnsureDirectoryExists();
        Load();
    }

    public void Save()
    {
        lock (_lock)
        {
            try
            {
                var json = JsonSerializer.Serialize(Settings, _jsonOptions);
                // Write to a temp file first so a crash mid-write can't truncate settings.json
                var tempPath = _settingsPath + ".tmp";
                File.WriteAllText(tempPath, json);
                File.Move(tempPath, _settingsPath, overwrite: true);
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Failed to save settings: {ex.Message}");
            }
        }
    }

    public void Load()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    Settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions) ?? new AppSettings();
                }
                else
                {
                    Settings = new AppSettings();
                    Save();
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Failed to load settings: {ex.Message}");
                BackupCorruptSettings();
                Settings = new AppSettings();
            }
        }
    }

    private void BackupCorruptSettings()
    {
        try
        {
            if (!File.Exists(_settingsPath)) return;
            var backupPath = $"{_settingsPath}.corrupt-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
            File.Copy(_settingsPath, backupPath, overwrite: true);
            LoggingService.Instance.LogWarning($"Unreadable settings backed up to {backupPath} before reset");
        }
        catch (Exception ex)
        {
            LoggingService.Instance.LogError($"Failed to back up corrupt settings: {ex.Message}");
        }
    }

    private void EnsureDirectoryExists()
    {
        var dir = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }
}
