using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using ClaudeTracker.Models;
using ClaudeTracker.ViewModels;

namespace ClaudeTracker.Views.Settings;

public partial class WatchdogView : UserControl
{
    private readonly WatchdogViewModel _vm;

    public WatchdogView()
    {
        InitializeComponent();
        _vm = App.Services.GetRequiredService<WatchdogViewModel>();
        DataContext = _vm;
        _vm.Refresh();

        // Enable toggle
        ResumeEnabledToggle.IsChecked = _vm.ResumeCliSessionOnReset;
        UpdateModeVisibility(_vm.ResumeCliSessionOnReset);
        ResumeEnabledToggle.Checked += (_, _) => { _vm.ResumeCliSessionOnReset = true; UpdateModeVisibility(true); };
        ResumeEnabledToggle.Unchecked += (_, _) => { _vm.ResumeCliSessionOnReset = false; UpdateModeVisibility(false); };

        // Mode
        InteractiveModeRadio.IsChecked = !_vm.IsUnattendedMode;
        UnattendedModeRadio.IsChecked = _vm.IsUnattendedMode;
        InteractiveModeRadio.Checked += (_, _) => _vm.IsUnattendedMode = false;
        UnattendedModeRadio.Checked += (_, _) => _vm.IsUnattendedMode = true;

        // Allowed projects
        AllowedProjectInput.ItemsSource = _vm.KnownProjectPaths;
        AllowedProjectsList.ItemsSource = _vm.AllowedProjects;
        AddAllowedProjectButton.Click += (_, _) =>
        {
            var text = AllowedProjectInput.Text;
            _vm.AddAllowedProjectCommand.Execute(text);
            AllowedProjectInput.Text = string.Empty;
        };

        // Manually add to PENDING from an active session
        ActiveSessionCombo.ItemsSource = _vm.ActiveSessions;
        UpdateActiveSessionsVisibility();
        _vm.ActiveSessions.CollectionChanged += (_, _) => UpdateActiveSessionsVisibility();
        AddPendingResumeButton.Click += (_, _) =>
        {
            _vm.AddPendingResumeCommand.Execute(ActiveSessionCombo.SelectedItem as SessionState);
        };

        // Pending / history lists
        PendingList.ItemsSource = _vm.PendingResumes;
        UpdatePendingVisibility();
        _vm.PendingResumes.CollectionChanged += (_, _) => UpdatePendingVisibility();

        HistoryList.ItemsSource = _vm.EventLog;
        UpdateHistoryVisibility();
        _vm.EventLog.CollectionChanged += (_, _) => UpdateHistoryVisibility();

        // Save
        SaveButton.Click += (_, _) =>
        {
            _vm.SaveCommand.Execute(null);
            SaveButton.Visibility = Visibility.Collapsed;
        };
        _vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(_vm.HasUnsavedChanges))
                Dispatcher.Invoke(() => SaveButton.Visibility = _vm.HasUnsavedChanges
                    ? Visibility.Visible : Visibility.Collapsed);
        };
    }

    private void UpdateModeVisibility(bool enabled)
    {
        ModePanel.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateActiveSessionsVisibility()
    {
        var hasAny = _vm.ActiveSessions.Count > 0;
        ActiveSessionCombo.Visibility = hasAny ? Visibility.Visible : Visibility.Collapsed;
        AddPendingResumeButton.Visibility = hasAny ? Visibility.Visible : Visibility.Collapsed;
        NoActiveSessionsText.Visibility = hasAny ? Visibility.Collapsed : Visibility.Visible;
    }

    private void UpdatePendingVisibility()
    {
        var hasAny = _vm.PendingResumes.Count > 0;
        PendingList.Visibility = hasAny ? Visibility.Visible : Visibility.Collapsed;
        NoPendingText.Visibility = hasAny ? Visibility.Collapsed : Visibility.Visible;
    }

    private void UpdateHistoryVisibility()
    {
        var hasAny = _vm.EventLog.Count > 0;
        HistoryList.Visibility = hasAny ? Visibility.Visible : Visibility.Collapsed;
        NoHistoryText.Visibility = hasAny ? Visibility.Collapsed : Visibility.Visible;
    }
}
