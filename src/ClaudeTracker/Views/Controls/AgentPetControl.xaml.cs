using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using ClaudeTracker.ViewModels;

namespace ClaudeTracker.Views.Controls;

public partial class AgentPetControl : UserControl
{
    private const double WorkingOpacity = 1.0;
    private const double IdleOpacity = 0.82;
    private const double SleepingOpacity = 0.55;

    private readonly Storyboard _bob = new();
    private readonly Storyboard _babyBob = new();
    private readonly Storyboard _zzz = new();
    private Storyboard[] AllStoryboards => new[] { _bob, _babyBob, _zzz };

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
                EyesClosed.Visibility = Visibility.Collapsed;
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
                PetImage.BeginAnimation(UIElement.OpacityProperty,
                    new DoubleAnimation(SleepingOpacity, TimeSpan.FromSeconds(0.4)));
                _bob.Stop();
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
