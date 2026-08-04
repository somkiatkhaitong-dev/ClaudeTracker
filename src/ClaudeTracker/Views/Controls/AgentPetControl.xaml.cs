using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using ClaudeTracker.Models;
using ClaudeTracker.Services.Interfaces;
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
    private const int HeroPowerIdleTicksPerFrame = 2; // ~220ms per frame — 8-frame cape-flow loop
    private const int CelebrateTicksPerFrame = 2; // ~220ms per frame
    private const int CelebrateLoops = 2;
    private const int HeroTransformTicksPerFrame = 2; // ~220ms per frame, matches celebrate's cadence
    private const int SitTransformTicksPerFrame = 3; // ~330ms per frame — slower, gentle sit/stand motion
    private const int SitLoopTicksPerFrame = 4; // ~440ms per frame, matches sleep's breathing pace
    private const int HeroSitLoopTicksPerFrame = 1; // ~110ms per frame — closest tick multiple to the
                                                      // 8-frame set's authored 8 FPS (125ms)
    private const double ClickMoveThresholdPx = 4;
    private BitmapImage[] _walkFrames = Array.Empty<BitmapImage>();
    private BitmapImage[] _blinkFrames = Array.Empty<BitmapImage>();
    private BitmapImage[] _sleepFrames = Array.Empty<BitmapImage>();
    private BitmapImage[] _celebrateFrames = Array.Empty<BitmapImage>();
    private BitmapImage[] _heroBlinkFrames = Array.Empty<BitmapImage>();
    private BitmapImage[] _heroWalkFrames = Array.Empty<BitmapImage>();
    private BitmapImage[] _heroPowerIdleFrames = Array.Empty<BitmapImage>();
    private BitmapImage[] _heroTransformFrames = Array.Empty<BitmapImage>();
    private BitmapImage[] _heroTransformBackFrames = Array.Empty<BitmapImage>();
    private BitmapImage[] _sitEnterFrames = Array.Empty<BitmapImage>();
    private BitmapImage[] _sitLoopFrames = Array.Empty<BitmapImage>();
    private BitmapImage[] _sitExitFrames = Array.Empty<BitmapImage>();
    private BitmapImage[] _waveFrames = Array.Empty<BitmapImage>();
    private BitmapImage[] _heroSitEnterFrames = Array.Empty<BitmapImage>();
    private BitmapImage[] _heroSitLoopFrames = Array.Empty<BitmapImage>();
    private BitmapImage[] _heroSitExitFrames = Array.Empty<BitmapImage>();
    private BitmapImage[] _heroSleepFrames = Array.Empty<BitmapImage>();
    private BitmapImage[] _heroWaveFrames = Array.Empty<BitmapImage>();
    private int _walkFrameIndex;
    private int _ticksUntilBlink;
    private int _blinkPos = -1; // -1 = not currently blinking
    private int _sleepFrameIndex;
    private int _sleepTickCounter;
    private int _celebratePos = -1; // -1 = not celebrating
    private int _celebrateTickCounter;
    private int _heroPowerIdleFrameIndex;
    private int _heroPowerIdleTickCounter;
    private int _heroTransformPos = -1; // -1 = not transforming
    private int _heroTransformTickCounter;
    private bool _heroTransformEntering;
    private int _sitTransformPos = -1; // -1 = not playing the sit-down/stand-up transition
    private int _sitTransformTickCounter;
    private bool _sitEntering;
    private bool _sitTransformHero; // which frame set (hero vs normal) this transition is playing
    private int _sitLoopFrameIndex;
    private int _sitLoopTickCounter;
    private int _wavePos = -1; // -1 = not currently waving
    private int _ticksUntilWave;
    private PetState _lastAppliedState = PetState.Idle;
    private Point _dragOffset;
    private Point _dragStartPos;
    private bool _dragMoved;

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
                UpdateHeroHintVisuals(_viewModel.ShowHeroHint);
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
        _dragStartPos = pos;
        _dragMoved = false;
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
        if ((pos - _dragStartPos).Length > ClickMoveThresholdPx) _dragMoved = true;
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

        if (_dragMoved)
        {
            App.Services.GetRequiredService<AgentPetsViewModel>().SavePetPosition(_viewModel);
        }
        else if (_skin.HasHeroMode && _heroTransformPos < 0 && _celebratePos < 0)
        {
            // a plain click, not a drag — toggle Hero mode instead of saving position
            _viewModel.IsHeroMode = !_viewModel.IsHeroMode;
            if (_viewModel.IsHeroMode)
                MarkHeroModeDiscovered();
        }
        e.Handled = true;
    }

    /// <summary>First time this skin family's hero mode is triggered, records it so the
    /// discoverability hint (badge + tooltip) stops showing — for this pet immediately via
    /// the property setter below, and for every other pet sharing the family via the
    /// settings-changed broadcast in AgentPetsViewModel.RefreshHeroHints.</summary>
    private void MarkHeroModeDiscovered()
    {
        var settings = App.Services.GetRequiredService<ISettingsService>();
        if (settings.Settings.SeenHeroModeSkins.Contains(_skin.EffectiveFamilyId)) return;
        settings.Settings.SeenHeroModeSkins.Add(_skin.EffectiveFamilyId);
        settings.Save();
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
        _heroBlinkFrames = LoadFrames(_skin.HeroBlinkFramePaths);
        _heroWalkFrames = LoadFrames(_skin.HeroWalkFramePaths);
        _heroPowerIdleFrames = LoadFrames(_skin.HeroPowerIdleFramePaths);
        _heroTransformFrames = LoadFrames(_skin.HeroTransformFramePaths);
        _heroTransformBackFrames = LoadFrames(_skin.HeroTransformBackFramePaths);
        _sitEnterFrames = LoadFrames(_skin.SitEnterFramePaths);
        _sitLoopFrames = LoadFrames(_skin.SitFramePaths);
        _sitExitFrames = LoadFrames(_skin.SitExitFramePaths);
        _waveFrames = LoadFrames(_skin.WaveFramePaths);
        _heroSitEnterFrames = LoadFrames(_skin.HeroSitEnterFramePaths);
        _heroSitLoopFrames = LoadFrames(_skin.HeroSitFramePaths);
        _heroSitExitFrames = LoadFrames(_skin.HeroSitExitFramePaths);
        _heroSleepFrames = LoadFrames(_skin.HeroSleepFramePaths);
        _heroWaveFrames = LoadFrames(_skin.HeroWaveFramePaths);

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

    /// <summary>Plays the one-shot normal↔hero transform animation, then re-applies the
    /// current state (which now reads the already-flipped IsHeroMode). Falls straight
    /// through to ApplyDedicatedPoseState if the skin has no transform art for this
    /// direction, so a skin can ship Hero mode incrementally.</summary>
    private void StartHeroTransform(bool enteringHero)
    {
        if (_viewModel == null) return;
        var frames = enteringHero ? _heroTransformFrames : _heroTransformBackFrames;
        if (frames.Length == 0)
        {
            ApplyDedicatedPoseState(_viewModel.State);
            return;
        }

        EyesClosed.Visibility = Visibility.Collapsed;
        _zzz.Stop();
        ZzzText.Opacity = 0;
        PetImage.BeginAnimation(UIElement.OpacityProperty, null);
        PetImage.Opacity = 1.0;
        _bob.Stop();
        _breathe.Stop();
        _heroTransformEntering = enteringHero;
        _heroTransformPos = 0;
        _heroTransformTickCounter = 0;
        PetImage.Source = frames[0];
        _frameTimer.Start();
    }

    /// <summary>Plays the one-shot sit-down (stand→crouch→sit) or stand-up (crouch→stand)
    /// transition, then re-applies the current state — mirrors StartHeroTransform. Falls
    /// straight through to ApplyDedicatedPoseState if the skin has no transition art, so a
    /// skin can ship Sitting art incrementally (loop frames only, no dedicated transition).</summary>
    private void StartSitTransition(bool entering)
    {
        if (_viewModel == null) return;
        bool isHeroMode = _viewModel.IsHeroMode;
        var enterFrames = isHeroMode ? _heroSitEnterFrames : _sitEnterFrames;
        var exitFrames = isHeroMode ? _heroSitExitFrames : _sitExitFrames;
        var frames = entering ? enterFrames : exitFrames;
        if (frames.Length == 0)
        {
            ApplyDedicatedPoseState(_viewModel.State);
            return;
        }

        EyesClosed.Visibility = Visibility.Collapsed;
        _zzz.Stop();
        ZzzText.Opacity = 0;
        PetImage.BeginAnimation(UIElement.OpacityProperty, null);
        PetImage.Opacity = 1.0;
        _bob.Stop();
        _babyBob.Stop();
        _breathe.Stop();
        _sitEntering = entering;
        _sitTransformHero = isHeroMode;
        _sitTransformPos = 0;
        _sitTransformTickCounter = 0;
        PetImage.Source = frames[0];
        _frameTimer.Start();
    }

    private void FrameTick()
    {
        if (_viewModel == null) return;

        if (_heroTransformPos >= 0)
        {
            var frames = _heroTransformEntering ? _heroTransformFrames : _heroTransformBackFrames;
            if (++_heroTransformTickCounter >= HeroTransformTicksPerFrame)
            {
                _heroTransformTickCounter = 0;
                _heroTransformPos++;
                if (_heroTransformPos >= frames.Length)
                {
                    _heroTransformPos = -1;
                    ApplyDedicatedPoseState(_viewModel.State);
                    return;
                }
                PetImage.Source = frames[_heroTransformPos];
            }
            return;
        }

        if (_sitTransformPos >= 0)
        {
            var enterFrames = _sitTransformHero ? _heroSitEnterFrames : _sitEnterFrames;
            var exitFrames = _sitTransformHero ? _heroSitExitFrames : _sitExitFrames;
            var sitFrames = _sitEntering ? enterFrames : exitFrames;
            if (++_sitTransformTickCounter >= SitTransformTicksPerFrame)
            {
                _sitTransformTickCounter = 0;
                _sitTransformPos++;
                if (_sitTransformPos >= sitFrames.Length)
                {
                    _sitTransformPos = -1;
                    ApplyDedicatedPoseState(_viewModel.State);
                    return;
                }
                PetImage.Source = sitFrames[_sitTransformPos];
            }
            return;
        }

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

        if (_viewModel.State == PetState.Working)
        {
            var walkFrames = _viewModel.IsHeroMode && _heroWalkFrames.Length > 0 ? _heroWalkFrames : _walkFrames;
            if (walkFrames.Length > 0)
            {
                _walkFrameIndex = (_walkFrameIndex + 1) % walkFrames.Length;
                PetImage.Source = walkFrames[_walkFrameIndex];
                return;
            }
        }

        if (_viewModel.State == PetState.Sleeping)
        {
            var sleepFrames = _viewModel.IsHeroMode ? _heroSleepFrames : _sleepFrames;
            if (sleepFrames.Length > 0)
            {
                if (++_sleepTickCounter >= SleepTicksPerFrame)
                {
                    _sleepTickCounter = 0;
                    _sleepFrameIndex = (_sleepFrameIndex + 1) % sleepFrames.Length;
                    PetImage.Source = sleepFrames[_sleepFrameIndex];
                }
                return;
            }
        }

        if (_viewModel.State == PetState.Sitting)
        {
            var sitLoopFrames = _viewModel.IsHeroMode ? _heroSitLoopFrames : _sitLoopFrames;
            if (sitLoopFrames.Length > 0)
            {
                var sitTicksPerFrame = _viewModel.IsHeroMode ? HeroSitLoopTicksPerFrame : SitLoopTicksPerFrame;
                if (++_sitLoopTickCounter >= sitTicksPerFrame)
                {
                    _sitLoopTickCounter = 0;
                    _sitLoopFrameIndex = (_sitLoopFrameIndex + 1) % sitLoopFrames.Length;
                    PetImage.Source = sitLoopFrames[_sitLoopFrameIndex];
                }
                return;
            }
        }

        if (_viewModel.State == PetState.Idle)
        {
            bool isHeroMode = _viewModel.IsHeroMode;
            bool heroPowerIdle = isHeroMode && _heroPowerIdleFrames.Length > 0;
            var waveFrames = isHeroMode ? _heroWaveFrames : _waveFrames;

            // one-shot wave takes priority over power-idle/blink — they never play together
            if (_wavePos >= 0)
            {
                _wavePos++;
                if (_wavePos >= waveFrames.Length)
                {
                    _wavePos = -1;
                    if (heroPowerIdle)
                    {
                        _heroPowerIdleFrameIndex = 0;
                        _heroPowerIdleTickCounter = 0;
                        PetImage.Source = _heroPowerIdleFrames[0];
                    }
                    else
                    {
                        var idlePath = isHeroMode && _skin.HeroIdleImagePath != null
                            ? _skin.HeroIdleImagePath
                            : _skin.IdleImagePath;
                        PetImage.Source = LoadImage(idlePath);
                        ScheduleNextBlink();
                    }
                    if (waveFrames.Length > 0) ScheduleNextWave();
                }
                else
                {
                    PetImage.Source = waveFrames[_wavePos];
                }
                return;
            }

            if (heroPowerIdle)
            {
                if (++_heroPowerIdleTickCounter >= HeroPowerIdleTicksPerFrame)
                {
                    _heroPowerIdleTickCounter = 0;
                    _heroPowerIdleFrameIndex = (_heroPowerIdleFrameIndex + 1) % _heroPowerIdleFrames.Length;
                    PetImage.Source = _heroPowerIdleFrames[_heroPowerIdleFrameIndex];
                }
                if (waveFrames.Length > 0 && --_ticksUntilWave <= 0)
                {
                    _wavePos = 0;
                    PetImage.Source = waveFrames[0];
                }
                return;
            }

            var blinkFrames = isHeroMode && _heroBlinkFrames.Length > 0 ? _heroBlinkFrames : _blinkFrames;
            if (_blinkPos >= 0)
            {
                _blinkPos++;
                if (_blinkPos >= blinkFrames.Length)
                {
                    _blinkPos = -1;
                    var idlePath = isHeroMode && _skin.HeroIdleImagePath != null
                        ? _skin.HeroIdleImagePath
                        : _skin.IdleImagePath;
                    PetImage.Source = LoadImage(idlePath);
                    ScheduleNextBlink();
                }
                else
                {
                    PetImage.Source = blinkFrames[_blinkPos];
                }
            }
            else if (waveFrames.Length > 0 && --_ticksUntilWave <= 0)
            {
                _wavePos = 0;
                PetImage.Source = waveFrames[0];
            }
            else if (blinkFrames.Length > 0 && --_ticksUntilBlink <= 0)
            {
                _blinkPos = 0;
                PetImage.Source = blinkFrames[0];
            }
        }
    }

    private void ScheduleNextBlink() =>
        _ticksUntilBlink = _random.Next(3000 / FrameTickMs, 8000 / FrameTickMs);

    /// <summary>Occasional idle wave — much rarer than a blink, so it reads as a deliberate
    /// greeting rather than a tic.</summary>
    private void ScheduleNextWave() =>
        _ticksUntilWave = _random.Next(30_000 / FrameTickMs, 90_000 / FrameTickMs);

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
            case nameof(AgentPetViewModel.IsHeroMode):
                if (_celebratePos < 0)
                    StartHeroTransform(_viewModel.IsHeroMode);
                break;
            case nameof(AgentPetViewModel.ShowHeroHint):
                UpdateHeroHintVisuals(_viewModel.ShowHeroHint);
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
            // a hero transform or sit transition is mid-flight — let it finish; FrameTick
            // calls ApplyDedicatedPoseState(_viewModel.State) itself on completion, which
            // will pick up whatever state is current by then
            if (_heroTransformPos >= 0 || _sitTransformPos >= 0) return;

            // finished working → one-shot celebrate jump before settling into idle
            if (previous == PetState.Working && state == PetState.Idle && _celebrateFrames.Length > 0)
            {
                StartCelebrate();
                return;
            }
            _celebratePos = -1;

            // idle too long → sit down; resumed activity or fell fully asleep → stand back up.
            // Uses the hero sit-enter/exit art in hero mode, normal-mode art otherwise — never
            // crosses the two, since normal-mode sit art has no cape/crown. If the active mode
            // has no transition art (e.g. hero mode ships only the seated loop, no stand-crouch-
            // sit frames), this falls through to ApplyDedicatedPoseState, which cuts straight to
            // the seated pose.
            bool isHeroModeForSit = _viewModel?.IsHeroMode ?? false;
            var sitEnterFrames = isHeroModeForSit ? _heroSitEnterFrames : _sitEnterFrames;
            var sitExitFrames = isHeroModeForSit ? _heroSitExitFrames : _sitExitFrames;
            if (previous != PetState.Sitting && state == PetState.Sitting && sitEnterFrames.Length > 0)
            {
                StartSitTransition(entering: true);
                return;
            }
            if (previous == PetState.Sitting && state != PetState.Sitting && sitExitFrames.Length > 0)
            {
                StartSitTransition(entering: false);
                return;
            }

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
        _wavePos = -1;

        bool isHeroMode = _viewModel?.IsHeroMode ?? false;

        switch (state)
        {
            case PetState.Working:
                var walkFrames = isHeroMode && _heroWalkFrames.Length > 0 ? _heroWalkFrames : _walkFrames;
                if (walkFrames.Length > 0)
                {
                    // walk frames carry the motion — no bob on top
                    _walkFrameIndex = 0;
                    PetImage.Source = walkFrames[0];
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
                var idleWaveFrames = isHeroMode ? _heroWaveFrames : _waveFrames;
                if (isHeroMode && _heroPowerIdleFrames.Length > 0)
                {
                    // continuous power-idle loop, rendered like sleep frames — no static+blink split.
                    // The frames already carry the head-bob + cape motion, so no synthetic _bob on
                    // top: a sub-pixel translate every screen frame re-samples the crown's thin rods
                    // (~1.7px on screen) into a shimmer that reads as flicker. Stop it (matches the
                    // walk-frame path) and let the sprite swap be the only motion.
                    _heroPowerIdleFrameIndex = 0;
                    _heroPowerIdleTickCounter = 0;
                    PetImage.Source = _heroPowerIdleFrames[0];
                    _bob.Stop();
                    _babyBob.Begin();
                    _babyBob.SetSpeedRatio(0.5);
                    if (idleWaveFrames.Length > 0) ScheduleNextWave();
                    _frameTimer.Start();
                    break;
                }

                var idlePath = isHeroMode && _skin.HeroIdleImagePath != null ? _skin.HeroIdleImagePath : _skin.IdleImagePath;
                PetImage.Source = LoadImage(idlePath);
                _bob.Begin();
                _bob.SetSpeedRatio(0.45);
                _babyBob.Begin();
                _babyBob.SetSpeedRatio(0.5);
                var idleBlinkFrames = isHeroMode && _heroBlinkFrames.Length > 0 ? _heroBlinkFrames : _blinkFrames;
                if (idleBlinkFrames.Length > 0) ScheduleNextBlink();
                if (idleWaveFrames.Length > 0) ScheduleNextWave();
                if (idleBlinkFrames.Length > 0 || idleWaveFrames.Length > 0)
                {
                    _frameTimer.Start();
                }
                else
                {
                    _frameTimer.Stop();
                }
                break;

            case PetState.Sitting:
                _bob.Stop();
                _babyBob.Stop();
                var sitLoopFrames = isHeroMode ? _heroSitLoopFrames : _sitLoopFrames;
                if (sitLoopFrames.Length > 0)
                {
                    // sit frames carry the breathing — no synthetic scale on top
                    _sitLoopFrameIndex = 0;
                    _sitLoopTickCounter = 0;
                    PetImage.Source = sitLoopFrames[0];
                    _frameTimer.Start();
                }
                else
                {
                    // no dedicated sit art for this mode yet — hold idle art, no motion. In hero
                    // mode this deliberately does not fall back to normal-mode sit art, which has
                    // no cape/crown.
                    var sitFallbackIdlePath = isHeroMode && _skin.HeroIdleImagePath != null ? _skin.HeroIdleImagePath : _skin.IdleImagePath;
                    PetImage.Source = LoadImage(sitFallbackIdlePath);
                    _frameTimer.Stop();
                }
                break;

            case PetState.Sleeping:
                _bob.Stop();
                _babyBob.Stop();
                var sleepFrames = isHeroMode ? _heroSleepFrames : _sleepFrames;
                if (sleepFrames.Length > 0)
                {
                    // sleep frames carry the breathing — no synthetic scale on top
                    _sleepFrameIndex = 0;
                    _sleepTickCounter = 0;
                    PetImage.Source = sleepFrames[0];
                    _frameTimer.Start();
                }
                else if (isHeroMode)
                {
                    // no dedicated hero sleep art — hold hero idle art rather than falling back to
                    // normal-mode sleeping art, which has no cape/crown
                    var sleepFallbackIdlePath = _skin.HeroIdleImagePath ?? _skin.IdleImagePath;
                    PetImage.Source = LoadImage(sleepFallbackIdlePath);
                    _frameTimer.Stop();
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

    private void UpdateHeroHintVisuals(bool show) =>
        HeroHintBadge.Visibility = show ? Visibility.Visible : Visibility.Collapsed;

    public void PauseAll()
    {
        _frameTimer.Stop();
        if (!_storyboardsBuilt) return;
        foreach (var sb in AllStoryboards)
            try { sb.Pause(); } catch (InvalidOperationException) { }
    }

    public void ResumeAll()
    {
        bool isHeroMode = _viewModel?.IsHeroMode ?? false;
        // Walk/blink fall back to normal-mode art when hero art is missing (small overlays or
        // already-matching poses); sit/sleep/wave/power-idle never fall back (full-body poses —
        // see PetSkin.HasHeroMode) so they only count in the mode that actually has art for them.
        bool hasFramesForState = _celebratePos >= 0 || _viewModel?.State switch
        {
            PetState.Working => (isHeroMode && _heroWalkFrames.Length > 0) || _walkFrames.Length > 0,
            PetState.Idle => (isHeroMode && _heroBlinkFrames.Length > 0) || _blinkFrames.Length > 0
                || (isHeroMode ? _heroWaveFrames.Length > 0 : _waveFrames.Length > 0)
                || (isHeroMode && _heroPowerIdleFrames.Length > 0),
            PetState.Sitting => (isHeroMode ? _heroSitLoopFrames : _sitLoopFrames).Length > 0,
            PetState.Sleeping => (isHeroMode ? _heroSleepFrames : _sleepFrames).Length > 0,
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
