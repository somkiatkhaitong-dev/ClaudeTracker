using CommunityToolkit.Mvvm.ComponentModel;

namespace ClaudeTracker.ViewModels;

/// <summary>One row in the Desktop Pets skin picker in Settings.</summary>
public partial class PetSkinToggle : ObservableObject
{
    public string Id { get; }
    public string DisplayName { get; }

    [ObservableProperty] private bool _isEnabled;

    public PetSkinToggle(string id, string displayName, bool isEnabled)
    {
        Id = id;
        DisplayName = displayName;
        IsEnabled = isEnabled;
    }
}
