using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using ClaudeTracker.Models;
using ClaudeTracker.Utilities;
using ClaudeTracker.ViewModels;

namespace ClaudeTracker.Views.Controls;

public partial class AgentPetControl : UserControl
{
    private const double WorkingOpacity = 1.0;
    private const double IdleOpacity = 0.82;
    private const double SleepingOpacity = 0.55;
    private const double BoxWidth = 96;
    private const double BoxHeight = 83;

    private readonly Storyboard _bob = new();
    private readonly Storyboard _babyBob = new();
    private readonly Storyboard _zzz = new();
    private readonly Storyboard _breathe = new();
    private Storyboard[] AllStoryboards => new[] { _bob, _babyBob, _zzz, _breathe };

    private AgentPetViewModel? _viewModel;
    private PetSkin _skin = PetSkins.All[0];
    private bool _storyboardsBuilt;

    // Frame animation: walk cycle while Working, periodic blink while Idle
    private const int FrameTickMs = 110;
    private readonly DispatcherTimer _frameTimer = new() { Interval = TimeSpan.FromMilliseconds(FrameTickMs) };
    private readonly Random _random = new();
    private const int SleepTicksPerFrame = 4; // ~440ms per frame — breathing pace
    private const int CelebrateTicksPerFrame = 2; // ~220ms per frame
    private const int CelebrateLoops = 2;
    private BitmapImage[] _walkFrames = Array.Empty<BitmapImage>();
    private BitmapImage[] _blinkFrames = Array.Empty<BitmapImage>();
    private BitmapImage[] _sleepFrames = Array.Empty<BitmapImage>();
    private BitmapImage[] _celebrateFrames = Array.Empty<BitmapImage>();
    private int _walkFrameIndex;
    private int _ticksUntilBlink;
    private int _blinkPos = -1; // -1 = not currently blinking
    private int _sleepFrameIndex;
    private int _sleepTickCounter;
    private int _celebratePos = -1; // -1 = not celebrating
    private int _celebrateTickCounter;
    private PetState _lastAppliedState = PetState.Idle;
    private Point _dragOffset;

    public AgentPetControl()
    {
        InitializeComponent();
        _frameTimer.Tick += (_, _) => FrameTick();
        DataContextChanged += OnDataContextChanged;
        Loaded += (_, _) =>
        {
            BuildStoryboards();
            if (_viewModel != null)
            {
                ResolveSkin();
                ApplyState(_viewModel.State);
                SetFacing(_viewModel.FacingRight);
                UpdateSubagentVisuals(_viewModel.SubagentCount);
            }
        };
        Unloaded += (_, _) => _frameTimer.Stop();

        PreviewMouseLeftButtonDown += OnDragStart;
        PreviewMouseMove += OnDragMove;
        PreviewMouseLeftButtonUp += OnDragEnd;
    }

    /// <summary>Per-pet free-drag — the pets window is a full-screen click-through overlay
    /// (see AgentPetsWindow's WM_NCHITTEST hook), so this is the only way to reposition a
    /// pet now that the whole-window drag strip is gone.</summary>
    private void OnDragStart(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel == null) return;
        var window = Window.GetWindow(this);
        if (window == null) return;

        var pos = e.GetPosition(window);
        _dragOffset = new Point(pos.X - _viewModel.X, pos.Y - _viewModel.Y);
        _viewModel.IsDragging = true;
        CaptureMouse();
        e.Handled = true;
    }

    private void OnDragMove(object sender, MouseEventArgs e)
    {
        if (_viewModel == null || !_viewModel.IsDragging || !IsMouseCaptured) return;
        var window = Window.GetWindow(this);
        if (window == null) return;

        var pos = e.GetPosition(window);
        var maxX = Math.Max(0, window.ActualWidth - Constants.Pets.PetWidth);
        var maxY = Math.Max(0, window.ActualHeight - Constants.Pets.PetHeight);
        _viewModel.X = Math.Clamp(pos.X - _dragOffset.X, 0, maxX);
        _viewModel.Y = Math.Clamp(pos.Y - _dragOffset.Y, 0, maxY);
        e.Handled = true;
    }

    private void OnDragEnd(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel == null || !_viewModel.IsDragging) return;
        _viewModel.IsDragging = false;
        ReleaseMouseCapture();
        App.Services.GetRequiredService<AgentPetsViewModel>().SavePetPosition(_viewModel);
        e.Handled = true;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_viewModel != null)
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

        _viewModel = e.NewValue as AgentPetViewModel;
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            ResolveSkin();
        }
    }

    private void ResolveSkin()
    {
        if (_viewModel == null) return;
        _skin = PetSkins.ResolveStage(_viewModel.SkinId, _viewModel.EvolutionStage);

        _walkFrames = LoadFrames(_skin.WalkFramePaths);
        _blinkFrames = LoadFrames(_skin.BlinkFramePaths);
        _sleepFrames = LoadFrames(_skin.SleepFramePaths);
        _celebrateFrames = LoadFrames(_skin.CelebrateFramePaths);

        BabyImage.Source = LoadImage(_skin.IdleImagePath);

        if (!_skin.HasDedicatedPoses && _skin.EyeLeftXFrac.HasValue)
        {
            Canvas.SetLeft(EyeClosedLeft, _skin.EyeLeftXFrac.Value * BoxWidth);
            Canvas.SetTop(EyeClosedLeft, _skin.EyeLeftYFrac!.Value * BoxHeight);
            Canvas.SetLeft(EyeClosedRight, _skin.EyeRightXFrac!.Value * BoxWidth);
            Canvas.SetTop(EyeClosedRight, _skin.EyeRightYFrac!.Value * BoxHeight);
        }
    }

    private static BitmapImage LoadImage(string path) =>
        new(new Uri("pack://application:,,," + path));

    private static BitmapImage[] LoadFrames(string[]? paths)
    {
        if (paths == null || paths.Length == 0) return Array.Empty<BitmapImage>();
        var frames = new BitmapImage[paths.Length];
        for (int i = 0; i < paths.Length; i++)
        {
            frames[i] = LoadImage(paths[i]);
            frames[i].Freeze();
        }
        return frames;
    }

    /// <summary>One tick of the frame animator. Working: advance the walk cycle.
    /// Idle: count down to a blink, then play the blink frames once and return to idle art.</summary>
    private void StartCelebrate()
    {
        EyesClosed.Visibility = Visibility.Collapsed;
        _zzz.Stop();
        ZzzText.Opacity = 0;
        PetImage.BeginAnimation(UIElement.OpacityProperty, null);
        PetImage.Opacity = 1.0;
        _bob.Stop();
        _breathe.Stop();
        _celebratePos = 0;
        _celebrateTickCounter = 0;
        PetImage.Source = _celebrateFrames[0];
        _frameTimer.Start();
    }

    private void FrameTick()
    {
        if (_viewModel == null) return;

        if (_celebratePos >= 0 && _celebrateFrames.Length > 0)
        {
            if (++_celebrateTickCounter >= CelebrateTicksPerFrame)
            {
                _celebrateTickCounter = 0;
                _celebratePos++;
                if (_celebratePos >= _celebrateFrames.Length * CelebrateLoops)
                {
                    _celebratePos = -1;
                    ApplyDedicatedPoseState(_viewModel.State);
                    return;
                }
                PetImage.Source = _celebrateFrames[_celebratePos % _celebrateFrames.Length];
            }
            return;
        }

        if (_viewModel.State == PetState.Working && _walkFrames.Length > 0)
        {
            _walkFrameIndex = (_walkFrameIndex + 1) % _walkFrames.Length;
            PetImage.Source = _walkFrames[_walkFrameIndex];
            return;
        }

        if (_viewModel.State == PetState.Sleeping && _sleepFrames.Length > 0)
        {
            if (++_sleepTickCounter >= SleepTicksPerFrame)
            {
                _sleepTickCounter = 0;
                _sleepFrameIndex = (_sleepFrameIndex + 1) % _sleepFrames.Length;
                PetImage.Source = _sleepFrames[_sleepFrameIndex];
            }
            return;
        }

        if (_viewModel.State == PetState.Idle && _blinkFrames.Length > 0)
        {
            if (_blinkPos >= 0)
            {
                _blinkPos++;
                if (_blinkPos >= _blinkFrames.Length)
                {
                    _blinkPos = -1;
                    PetImage.Source = LoadImage(_skin.IdleImagePath);
                    ScheduleNextBlink();
                }
                else
                {
                    PetImage.Source = _blinkFrames[_blinkPos];
                }
            }
            else if (--_ticksUntilBlink <= 0)
            {
                _blinkPos = 0;
                PetImage.Source = _blinkFrames[0];
            }
        }
    }

    private void ScheduleNextBlink() =>
        _ticksUntilBlink = _random.Next(3000 / FrameTickMs, 8000 / FrameTickMs);

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_viewModel == null) return;
        switch (e.PropertyName)
        {
            case nameof(AgentPetViewModel.State):
                ApplyState(_viewModel.State);
                break;
            case nameof(AgentPetViewModel.FacingRight):
                SetFacing(_viewModel.FacingRight);
                break;
            case nameof(AgentPetViewModel.SubagentCount):
                UpdateSubagentVisuals(_viewModel.SubagentCount);
                break;
            case nameof(AgentPetViewModel.EvolutionStage):
                ApplyEvolutionStage();
                break;
        }
    }

    /// <summary>Re-resolve the skin for the pet's new evolution stage and re-apply the
    /// current pose against it — mirrors how state transitions already fully re-derive
    /// visuals from _skin. A cross-fade can be layered on later once stage art is visually
    /// distinct enough to make the swap worth softening.</summary>
    private void ApplyEvolutionStage()
    {
        if (_viewModel == null || !_storyboardsBuilt) return;
        ResolveSkin();
        ApplyState(_viewModel.State);
    }

    private void BuildStoryboards()
    {
        if (_storyboardsBuilt) return;
        _storyboardsBuilt = true;

        AddAnimation(_bob, Bob, "Y", 0, -3, TimeSpan.FromSeconds(0.45), autoReverse: true);
        AddAnimation(_babyBob, BabyBob, "Y", 0, -2, TimeSpan.FromSeconds(0.5), autoReverse: true);

        // slow breathing while asleep: body swells up ~3% and settles back
        AddBreatheAnimation(Breathe, "ScaleY", 1.0, 1.03);
        AddBreatheAnimation(Breathe, "ScaleX", 1.0, 1.012);

        var zzzOpacity = new DoubleAnimationUsingKeyFrames
        {
            Duration = TimeSpan.FromSeconds(2.5),
            RepeatBehavior = RepeatBehavior.Forever
        };
        zzzOpacity.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        zzzOpacity.KeyFrames.Add(new LinearDoubleKeyFrame(0.9, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.0))));
        zzzOpacity.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2.5))));
        Timeline.SetDesiredFrameRate(zzzOpacity, 30);
        Storyboard.SetTarget(zzzOpacity, ZzzText);
        Storyboard.SetTargetProperty(zzzOpacity, new PropertyPath("Opacity"));
        _zzz.Children.Add(zzzOpacity);
        AddAnimation(_zzz, ZzzDrift, "Y", 0, -8, TimeSpan.FromSeconds(2.5));
    }

    private void AddBreatheAnimation(DependencyObject target, string property, double from, double to)
    {
        var anim = new DoubleAnimation(from, to, TimeSpan.FromSeconds(1.7))
        {
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };
        Timeline.SetDesiredFrameRate(anim, 30);
        Storyboard.SetTarget(anim, target);
        Storyboard.SetTargetProperty(anim, new PropertyPath(property));
        _breathe.Children.Add(anim);
    }

    private static void AddAnimation(Storyboard storyboard, DependencyObject target, string property,
        double from, double to, TimeSpan duration, bool autoReverse = false)
    {
        var anim = new DoubleAnimation(from, to, duration)
        {
            AutoReverse = autoReverse,
            RepeatBehavior = RepeatBehavior.Forever
        };
        Timeline.SetDesiredFrameRate(anim, 30);
        Storyboard.SetTarget(anim, target);
        Storyboard.SetTargetProperty(anim, new PropertyPath(property));
        storyboard.Children.Add(anim);
    }

    public void ApplyState(PetState state)
    {
        if (!_storyboardsBuilt) return;
        var previous = _lastAppliedState;
        _lastAppliedState = state;

        if (_skin.HasDedicatedPoses)
        {
            // finished working → one-shot celebrate jump before settling into idle
            if (previous == PetState.Working && state == PetState.Idle && _celebrateFrames.Length > 0)
            {
                StartCelebrate();
                return;
            }
            _celebratePos = -1;
            ApplyDedicatedPoseState(state);
            return;
        }

        switch (state)
        {
            case PetState.Working:
                EyesClosed.Visibility = Visibility.Collapsed;
                PetImage.Source = LoadImage(_skin.WorkingImagePath);
                PetImage.BeginAnimation(UIElement.OpacityProperty,
                    new DoubleAnimation(WorkingOpacity, TimeSpan.FromSeconds(0.4)));
                _zzz.Stop();
                ZzzText.Opacity = 0;
                _bob.Begin();
                _bob.SetSpeedRatio(1.0);
                _babyBob.Begin();
                break;

            case PetState.Idle:
                EyesClosed.Visibility = Visibility.Collapsed;
                PetImage.Source = LoadImage(_skin.IdleImagePath);
                PetImage.BeginAnimation(UIElement.OpacityProperty,
                    new DoubleAnimation(IdleOpacity, TimeSpan.FromSeconds(0.4)));
                _zzz.Stop();
                ZzzText.Opacity = 0;
                _bob.Begin();
                _bob.SetSpeedRatio(0.45);
                _babyBob.Begin();
                _babyBob.SetSpeedRatio(0.5);
                break;

            case PetState.Sleeping:
                EyesClosed.Visibility = Visibility.Visible;
                PetImage.Source = LoadImage(_skin.SleepingImagePath);
                PetImage.BeginAnimation(UIElement.OpacityProperty,
                    new DoubleAnimation(SleepingOpacity, TimeSpan.FromSeconds(0.4)));
                _bob.Stop();
                _babyBob.Stop();
                _zzz.Begin();
                _breathe.Begin();
                break;
        }

        if (state != PetState.Sleeping)
        {
            _breathe.Stop();
        }
    }

    /// <summary>Skins with real per-state artwork: just swap the image, no synthetic
    /// opacity dimming, eye overlay, or zzz text — the art already carries the pose.
    /// With walk/blink frames, the frame timer animates instead of (or on top of) the bob.</summary>
    private void ApplyDedicatedPoseState(PetState state)
    {
        EyesClosed.Visibility = Visibility.Collapsed;
        _zzz.Stop();
        ZzzText.Opacity = 0;
        PetImage.BeginAnimation(UIElement.OpacityProperty, null);
        PetImage.Opacity = 1.0;
        _blinkPos = -1;

        switch (state)
        {
            case PetState.Working:
                if (_walkFrames.Length > 0)
                {
                    // walk frames carry the motion — no bob on top
                    _walkFrameIndex = 0;
                    PetImage.Source = _walkFrames[0];
                    _bob.Stop();
                    _frameTimer.Start();
                }
                else
                {
                    PetImage.Source = LoadImage(_skin.WorkingImagePath);
                    _bob.Begin();
                    _bob.SetSpeedRatio(1.0);
                    _frameTimer.Stop();
                }
                _babyBob.Begin();
                break;

            case PetState.Idle:
                PetImage.Source = LoadImage(_skin.IdleImagePath);
                _bob.Begin();
                _bob.SetSpeedRatio(0.45);
                _babyBob.Begin();
                _babyBob.SetSpeedRatio(0.5);
                if (_blinkFrames.Length > 0)
                {
                    ScheduleNextBlink();
                    _frameTimer.Start();
                }
                else
                {
                    _frameTimer.Stop();
                }
                break;

            case PetState.Sleeping:
                _bob.Stop();
                _babyBob.Stop();
                if (_sleepFrames.Length > 0)
                {
                    // sleep frames carry the breathing — no synthetic scale on top
                    _sleepFrameIndex = 0;
                    _sleepTickCounter = 0;
                    PetImage.Source = _sleepFrames[0];
                    _frameTimer.Start();
                }
                else
                {
                    PetImage.Source = LoadImage(_skin.SleepingImagePath);
                    _frameTimer.Stop();
                    _breathe.Begin();
                }
                break;
        }

        if (state != PetState.Sleeping)
        {
            _breathe.Stop();
        }
    }

    public void SetFacing(bool right) => Flip.ScaleX = right ? 1 : -1;

    private void UpdateSubagentVisuals(int count)
    {
        var visible = count > 0 ? Visibility.Visible : Visibility.Collapsed;
        SubagentBadge.Visibility = visible;
        BabyBlob.Visibility = visible;
    }

    public void PauseAll()
    {
        _frameTimer.Stop();
        if (!_storyboardsBuilt) return;
        foreach (var sb in AllStoryboards)
            try { sb.Pause(); } catch (InvalidOperationException) { }
    }

    public void ResumeAll()
    {
        bool hasFramesForState = _celebratePos >= 0 || _viewModel?.State switch
        {
            PetState.Working => _walkFrames.Length > 0,
            PetState.Idle => _blinkFrames.Length > 0,
            PetState.Sleeping => _sleepFrames.Length > 0,
            _ => false
        };
        if (hasFramesForState)
        {
            _frameTimer.Start();
        }
        if (!_storyboardsBuilt) return;
        foreach (var sb in AllStoryboards)
            try { sb.Resume(); } catch (InvalidOperationException) { }
    }
}
