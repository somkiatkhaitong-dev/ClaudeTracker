using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using ClaudeTracker.ViewModels;

namespace ClaudeTracker.Views.Controls;

public partial class AgentPetControl : UserControl
{
    private static readonly Random Random = new();

    private readonly Storyboard _bob = new();
    private readonly Storyboard _blink = new();
    private readonly Storyboard _gear = new();
    private readonly Storyboard _feet = new();
    private readonly Storyboard _zzz = new();
    private readonly Storyboard _babyBob = new();
    private Storyboard[] AllStoryboards => new[] { _bob, _blink, _gear, _feet, _zzz, _babyBob };

    private AgentPetViewModel? _viewModel;
    private bool _storyboardsBuilt;

    public AgentPetControl()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += (_, _) =>
        {
            BuildStoryboards();
            if (_viewModel != null)
            {
                ApplyState(_viewModel.State);
                SetFacing(_viewModel.FacingRight);
                UpdateSubagentVisuals(_viewModel.SubagentCount);
            }
        };
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_viewModel != null)
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

        _viewModel = e.NewValue as AgentPetViewModel;
        if (_viewModel != null)
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

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
        }
    }

    private void BuildStoryboards()
    {
        if (_storyboardsBuilt) return;
        _storyboardsBuilt = true;

        AddAnimation(_bob, Bob, "Y", 0, -3, TimeSpan.FromSeconds(0.45), autoReverse: true);
        AddAnimation(_babyBob, BabyBob, "Y", 0, -2, TimeSpan.FromSeconds(0.5), autoReverse: true);
        AddAnimation(_gear, GearSpin, "Angle", 0, 360, TimeSpan.FromSeconds(1.2));
        AddAnimation(_feet, FootLT, "X", 0, 2, TimeSpan.FromSeconds(0.3), autoReverse: true);
        AddAnimation(_feet, FootRT, "X", 0, -2, TimeSpan.FromSeconds(0.3), autoReverse: true);

        // Blink: quick close-open once every ~4s, staggered so pets don't blink in sync
        foreach (var target in new object[] { BlinkL, BlinkR })
        {
            var anim = new DoubleAnimationUsingKeyFrames
            {
                Duration = TimeSpan.FromSeconds(4),
                RepeatBehavior = RepeatBehavior.Forever,
                BeginTime = TimeSpan.FromSeconds(Random.NextDouble() * 2)
            };
            anim.KeyFrames.Add(new DiscreteDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            anim.KeyFrames.Add(new LinearDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(3.6))));
            anim.KeyFrames.Add(new LinearDoubleKeyFrame(0.1, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(3.72))));
            anim.KeyFrames.Add(new LinearDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(3.85))));
            Timeline.SetDesiredFrameRate(anim, 30);
            Storyboard.SetTarget(anim, (DependencyObject)target);
            Storyboard.SetTargetProperty(anim, new PropertyPath("ScaleY"));
            _blink.Children.Add(anim);
        }

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

        _blink.Begin();
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

        switch (state)
        {
            case PetState.Working:
                EyesOpen.Visibility = Visibility.Visible;
                EyesClosed.Visibility = Visibility.Collapsed;
                Smile.Visibility = Visibility.Visible;
                Gear.Visibility = Visibility.Visible;
                Sparkle.Visibility = Visibility.Visible;
                BodyTilt.Angle = -4;
                _zzz.Stop();
                ZzzText.Opacity = 0;
                _gear.Begin();
                _bob.Begin();
                _bob.SetSpeedRatio(1.0);
                _feet.Begin();
                _feet.SetSpeedRatio(1.0);
                _babyBob.Begin();
                break;

            case PetState.Idle:
                EyesOpen.Visibility = Visibility.Visible;
                EyesClosed.Visibility = Visibility.Collapsed;
                Smile.Visibility = Visibility.Visible;
                Gear.Visibility = Visibility.Collapsed;
                Sparkle.Visibility = Visibility.Collapsed;
                BodyTilt.Angle = 0;
                _zzz.Stop();
                ZzzText.Opacity = 0;
                _gear.Stop();
                _bob.Begin();
                _bob.SetSpeedRatio(0.45);
                _feet.Begin();
                _feet.SetSpeedRatio(0.5);
                _babyBob.Begin();
                _babyBob.SetSpeedRatio(0.5);
                break;

            case PetState.Sleeping:
                EyesOpen.Visibility = Visibility.Collapsed;
                EyesClosed.Visibility = Visibility.Visible;
                Smile.Visibility = Visibility.Collapsed;
                Gear.Visibility = Visibility.Collapsed;
                Sparkle.Visibility = Visibility.Collapsed;
                BodyTilt.Angle = 0;
                _gear.Stop();
                _bob.Stop();
                _feet.Stop();
                _babyBob.Stop();
                _zzz.Begin();
                break;
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
        if (!_storyboardsBuilt) return;
        foreach (var sb in AllStoryboards)
            try { sb.Pause(); } catch (InvalidOperationException) { }
    }

    public void ResumeAll()
    {
        if (!_storyboardsBuilt) return;
        foreach (var sb in AllStoryboards)
            try { sb.Resume(); } catch (InvalidOperationException) { }
    }
}
