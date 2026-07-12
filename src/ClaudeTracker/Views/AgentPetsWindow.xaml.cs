using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
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
    private const int WM_NCHITTEST = 0x0084;
    private const int HTCLIENT = 1;
    private const int HTTRANSPARENT = -1;
    private const double HitTestPadding = 6; // easier to grab a pet without pixel-perfect aim

    private readonly AgentPetsViewModel _viewModel;
    private readonly DispatcherTimer _walkTimer;
    private readonly DispatcherTimer _stateTimer;

    public AgentPetsWindow()
    {
        InitializeComponent();

        _viewModel = App.Services.GetRequiredService<AgentPetsViewModel>();
        DataContext = _viewModel;
        PetsHost.ItemsSource = _viewModel.Pets;

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

        SourceInitialized += (_, _) =>
        {
            if (PresentationSource.FromVisual(this) is HwndSource source)
                source.AddHook(WndProc);
        };
    }

    /// <summary>Sizes/positions the overlay to cover the primary monitor's work area.
    /// Called once on show — the window itself is never user-moved anymore (individual
    /// pets are dragged instead), so there's nothing to persist here.</summary>
    public void RestorePosition()
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Left;
        Top = workArea.Top;
        Width = workArea.Width;
        Height = workArea.Height;
    }

    /// <summary>Click-through everywhere except directly over a pet: WM_NCHITTEST is sent
    /// on every mouse move over the window, so this must stay cheap (a handful of pets,
    /// simple rect checks). Returning HTTRANSPARENT lets the click fall through to
    /// whatever's beneath (desktop icons, other app windows); HTCLIENT over a pet lets
    /// normal WPF mouse events (including the drag handlers in AgentPetControl) fire.</summary>
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WM_NCHITTEST) return IntPtr.Zero;

        var screenPoint = new Point(GetX(lParam), GetY(lParam));
        var clientPoint = PointFromScreen(screenPoint);

        foreach (var pet in _viewModel.Pets)
        {
            var rect = new Rect(
                pet.X - HitTestPadding, pet.Y - HitTestPadding,
                Constants.Pets.PetWidth + 2 * HitTestPadding, Constants.Pets.PetHeight + 2 * HitTestPadding);
            if (rect.Contains(clientPoint))
            {
                handled = true;
                return new IntPtr(HTCLIENT);
            }
        }

        handled = true;
        return new IntPtr(HTTRANSPARENT);
    }

    private static int GetX(IntPtr lParam) => unchecked((short)((long)lParam & 0xFFFF));
    private static int GetY(IntPtr lParam) => unchecked((short)(((long)lParam >> 16) & 0xFFFF));

    private void WalkTick()
    {
        var maxX = Math.Max(0, ActualWidth - Constants.Pets.PetWidth);
        foreach (var pet in _viewModel.Pets)
        {
            if (pet.IsDragging || pet.State != PetState.Working) continue;

            var next = pet.X + (pet.FacingRight ? 1 : -1) * Constants.Pets.WorkingSpeed * pet.SpeedJitter * PetRuntimeSettings.SpeedMultiplier;

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
}
