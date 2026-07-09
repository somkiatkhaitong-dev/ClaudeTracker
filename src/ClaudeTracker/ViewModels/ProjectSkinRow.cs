using CommunityToolkit.Mvvm.ComponentModel;
using ClaudeTracker.Models;

namespace ClaudeTracker.ViewModels;

/// <summary>One selectable option in a project's skin ComboBox — "" id means Auto (cycling).</summary>
public record SkinOption(string Id, string DisplayName);

/// <summary>One row in the Project Overrides list in Settings — pins a character to a
/// project folder instead of leaving it to round-robin assignment.</summary>
public partial class ProjectSkinRow : ObservableObject
{
    public string ProjectPath { get; }
    public string ProjectName { get; }
    public List<SkinOption> SkinOptions { get; }

    [ObservableProperty] private string _selectedSkinId = "";

    public ProjectSkinRow(string projectPath, string projectName, IEnumerable<PetSkin> skins)
    {
        ProjectPath = projectPath;
        ProjectName = projectName;
        SkinOptions = new List<SkinOption> { new("", "Auto") };
        SkinOptions.AddRange(skins.Select(s => new SkinOption(s.Id, FormatSkinName(s.Id))));
    }

    private static string FormatSkinName(string id) =>
        id.Length == 0 ? id : char.ToUpperInvariant(id[0]) + id[1..];
}
