using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using ClaudeTracker.Services.Interfaces;
using ClaudeTracker.Utilities;
using ClaudeTracker.ViewModels;
using ClaudeTracker.Views.Controls;

namespace ClaudeTracker.Views;

public partial class AgentPetsWindow : Window
{
    private readonly AgentPetsViewModel _viewModel;
    private readonly ISettingsService _settingsService;
    private readonly DispatcherTimer _saveTimer;
    private readonly DispatcherTimer _walkTimer;
    private readonly DispatcherTimer _stateTimer;

    public AgentPetsWindow()
    {
        InitializeComponent();

        _viewModel = App.Services.GetRequiredService<AgentPetsViewModel>();
        _settingsService = App.Services.GetRequiredService<ISettingsService>();
        DataContext = _viewModel;
        PetsHost.ItemsSource = _viewModel.Pets;

        CloseButton.Click += (_, _) => CloseRequested?.Invoke(this, EventArgs.Empty);

        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _saveTimer.Tick += (_, _) => { _saveTimer.Stop(); SavePosition(); };
        LocationChanged += (_, _) => { _saveTimer.Stop(); _saveTimer.Start(); };

        // One timer walks every pet; storyboards only handle per-pet micro-animation
        _walkTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(Constants.Pets.FrameIntervalMs) };
        _walkTimer.Tick += (_, _) => WalkTick();

        _stateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _stateTimer.Tick += (_, _) => _viewModel.RefreshStates();

        // CPU guard — the app renders in software, so everything stops when not visible
        IsVisibleChanged += (_, e) =>
        {
            if ((bool)e.NewValue) StartAnimations();
            else StopAnimations();
        };
        _viewModel.Pets.CollectionChanged += (_, _) =>
        {
            if (IsVisible && _viewModel.Pets.Count > 0 && !_walkTimer.IsEnabled) StartAnimations();
            else if (_viewModel.Pets.Count == 0) StopAnimations();
        };
    }

    public event EventHandler? CloseRequested;

    public void RestorePosition()
    {
        var settings = _settingsService.Settings;
        var workArea = SystemParameters.WorkArea;

        if (settings.AgentPetsWindowLeft.HasValue && settings.AgentPetsWindowTop.HasValue)
        {
            Left = Math.Clamp(settings.AgentPetsWindowLeft.Value, workArea.Left, workArea.Right - Width);
            Top = Math.Clamp(settings.AgentPetsWindowTop.Value, workArea.Top, workArea.Bottom - Height);
        }
        else
        {
            // Default to the left of the floating usage widget's spot
            Left = workArea.Right - Width - 340;
            Top = workArea.Bottom - Height - 20;
        }
    }

    private void WalkTick()
    {
        var maxX = Math.Max(0, RootGrid.ActualWidth - Constants.Pets.PetWidth);
        foreach (var pet in _viewModel.Pets)
        {
            if (pet.State == PetState.Sleeping) continue;

            var speed = pet.State == PetState.Working ? Constants.Pets.WorkingSpeed : Constants.Pets.IdleSpeed;
            var next = pet.X + (pet.FacingRight ? 1 : -1) * speed * pet.SpeedJitter;

            if (next <= 0) { next = 0; pet.FacingRight = true; }
            else if (next >= maxX) { next = maxX; pet.FacingRight = false; }

            pet.X = next;
        }
    }

    private void StartAnimations()
    {
        if (_viewModel.Pets.Count > 0) _walkTimer.Start();
        _stateTimer.Start();
        ForEachPetControl(c => c.ResumeAll());
    }

    private void StopAnimations()
    {
        _walkTimer.Stop();
        _stateTimer.Stop();
        ForEachPetControl(c => c.PauseAll());
    }

    private void ForEachPetControl(Action<AgentPetControl> action)
    {
        foreach (var pet in _viewModel.Pets)
        {
            if (PetsHost.ItemContainerGenerator.ContainerFromItem(pet) is DependencyObject container
                && FindPetControl(container) is { } control)
            {
                action(control);
            }
        }
    }

    private static AgentPetControl? FindPetControl(DependencyObject parent)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is AgentPetControl control) return control;
            if (FindPetControl(child) is { } nested) return nested;
        }
        return null;
    }

    private void GroundStrip_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    private void SavePosition()
    {
        _settingsService.Settings.AgentPetsWindowLeft = Left;
        _settingsService.Settings.AgentPetsWindowTop = Top;
        _settingsService.Save();
    }
}
