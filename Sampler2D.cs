using Godot;
using System;
using System.Collections.Generic;

public partial class Sampler2D : Node3D
{
    double _yrange = 1.0;
    private Tween TopViewTween;
    private Tween RotationTween;
    private Tween YScaleTween;
    private Tween SpotlightTween;

    private bool _mcmc = false;
    private double _rotationSpeed = 0.0;
    private bool _rotating = false;
    private bool _running = false;
    private bool _topDownView = false;
    private double _speed = 1.0f;

    private Camera3D _camera;

    private int _numAccepted = 0;


    private OptionButton _distributionSelector;
    private CheckButton _rotateButton;
    private CheckButton _MCMCButton;
    private Node3D _domain;
    private SamplesMesh _acceptedSamplesMesh;
    private SamplesMesh _rejectedSamplesMesh;
    private HSlider _yScaleSlider;
    private HSlider _radiusSlider;

    private Surface _surface;

    private List<Sample> _samples = new List<Sample>();

    private IDistribution2D _distribution;
    [Export] public Resource Distribution {
        get { 
            return _distribution as Resource; 
        }
        set {
            _distribution = value as IDistribution2D;
            if (_surface != null)
                _surface.Distribution = value;
            Reset();
        }
    }

    private Vector2 _samplingPosition = new Vector2(0.0f, 0.0f);
    [Export] public Vector2 SamplingPosition {
        get { return _samplingPosition; }
        set {
            _samplingPosition = value;
            if (_surface != null)
                _surface.SamplingPosition = value;
        }
    }

    private double _samplingStd;
    [Export] public double SamplingStd {
        get { return _samplingStd; }
        set {
            _samplingStd = value;
            if (_surface != null)
                _surface.SamplingStd = value;
        }
    }

    private double _yScale = 1.0f;
    [Export] public double YScale {
        get { return _yScale; }
        set {
            _yScale = value;
            if(_surface != null)
                _surface.YScale = value;
            if(_acceptedSamplesMesh != null)
                _acceptedSamplesMesh.YScale = value;
            if(_rejectedSamplesMesh != null)
                _rejectedSamplesMesh.YScale = value;
        }
    }

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        _domain = GetNode<Node3D>("%Domain");
        _surface = GetNode<Surface>("%Surface");
        _acceptedSamplesMesh = GetNode<SamplesMesh>("%AcceptedSamples");
        _rejectedSamplesMesh = GetNode<SamplesMesh>("%RejectedSamples");
        _camera = GetNode<Camera3D>("Camera");
        _yScaleSlider = GetNode<HSlider>("%YScale");
        _rotateButton = GetNode<CheckButton>("%Rotate");
        _distributionSelector = GetNode<OptionButton>("%Distribution");
        _MCMCButton = GetNode<CheckButton>("%MCMC");
        _radiusSlider = GetNode<HSlider>("%Radius");

        _surface.Distribution = Distribution;
        _surface.SamplingPosition = SamplingPosition;
        _surface.SamplingStd = SamplingStd;
        _surface.YScale = YScale;

        // Go through all .tres files in the res://distributions folder and add them to the dropdown
        _distributionSelector.Clear();
        using var dir = DirAccess.Open("res://distributions/examples");
        if (dir != null)
        {
            dir.ListDirBegin();
            string fileName = dir.GetNext();
            while (fileName != "")
            {
                if (dir.CurrentIsDir())
                {
                    GD.Print($"Ignoring directory: {fileName}");
                }
                else
                {
                    if (fileName.EndsWith(".tres"))
                    {
                        // splice off file extension after dot
                        var name = fileName[..fileName.LastIndexOf('.')];
                        
                        // Add entry to dropdown
                        _distributionSelector.AddItem(name);
                    }
                }
                fileName = dir.GetNext();
            }
        }
        else
        {
            GD.Print("An error occurred when trying to access the path.");
        }

        _acceptedSamplesMesh.YScale = YScale;
        _rejectedSamplesMesh.YScale = YScale;
        Reset();

        // Initialize to values selected in UI
        OnTopDownViewToggled(GetNode<CheckButton>("%TopDownView").ButtonPressed);
        OnRotateToggled(_rotateButton.ButtonPressed);
        OnSpotlightToggled(GetNode<CheckButton>("%Spotlight").ButtonPressed);
        OnSpeedValueChanged(GetNode<HSlider>("%Speed").Value);
        OnRunToggled(GetNode<Button>("%Run").ButtonPressed);
        OnDistributionSelected(_distributionSelector.Selected);
        OnYScaleValueChanged(_yScaleSlider.Value);
        OnRadiusValueChanged(_radiusSlider.Value);
        OnMCMCToggled(_MCMCButton.ButtonPressed);
        OnShowRejectedToggled(GetNode<CheckButton>("%ShowRejected").ButtonPressed);
	}

    private double _spawn = 0;
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
        if (_rotating)
            _domain.Rotation = new Vector3(_domain.Rotation.X, Mathf.PosMod(_domain.Rotation.Y + (float)(delta*_rotationSpeed), Mathf.Pi*2), _domain.Rotation.Z);

        if (_running) {
            _spawn += delta*_speed;
            var new_spawn = (int)_spawn;
            for (int i = 0; i < new_spawn; i++) {
                Next();
            }
            _spawn -= new_spawn;
        }

        // Update stats to display number of accepted samples
        GetNode<RichTextLabel>("%Stats").Text =
            _samples.Count == 0 ? "No samples" : $"Accepted: {_numAccepted}/{_samples.Count} ({_numAccepted/(float)_samples.Count*100:0.00}%)";
	}

    public void OnRadiusValueChanged(double value) {
        SamplingStd = value;
    }

    public void OnMCMCToggled(bool active) {
        _mcmc = active;
        _radiusSlider.Editable = active;
        _radiusSlider.Visible = active;
        GetNode<Label>("%RadiusLabel").Visible = active;
        GetNode<CheckButton>("%Spotlight").Disabled = !active;
        GetNode<CheckButton>("%Spotlight").Visible = active;

        Reset();
    }

    private void Next() {
        // Draw random point from sampling distribution (isotropic Gaussian) inside bounds
        Vector2 sample_pos;
        while(true) {
            if (_mcmc) {
                sample_pos = new Vector2(
                    (float)GD.Randfn(SamplingPosition.X, SamplingStd), 
                    (float)GD.Randfn(SamplingPosition.Y, SamplingStd)
                );
            } else {
                sample_pos = new Vector2(
                    (float)GD.RandRange(_distribution.MinCoords.X, _distribution.MaxCoords.X),
                    (float)GD.RandRange(_distribution.MinCoords.Y, _distribution.MaxCoords.Y)
                );
            }

            if (sample_pos.X >= _distribution.MinCoords.X && sample_pos.X <= _distribution.MaxCoords.X &&
                sample_pos.Y >= _distribution.MinCoords.Y && sample_pos.Y <= _distribution.MaxCoords.Y) {
                break;
            }
        }

        // Compute probability of sample
        var p = _distribution.PDF(sample_pos.X, sample_pos.Y);

        // Create new sample
        var sample = new Sample(sample_pos, p, false);
        _samples.Add(sample);

        if(_mcmc) {
            // always accept first sample, otherwise accept with probability P_new/P_old
            if (_samples.Count == 1 || GD.Randf() < p/_samples[^2].Probability) {
                sample.Accepted = true;
                _numAccepted++;
                
                // Update sampling position for next sample
                SamplingPosition = sample_pos;
            }
        } else {
            // always accept first sample, otherwise accept with probability P_new/(c*P_sample)
            if (_samples.Count == 1 || GD.Randf() < p / _distribution.VMax) {
                sample.Accepted = true;
                _numAccepted++;
            }
        }

        // Update samples mesh
        if (sample.Accepted)
            _acceptedSamplesMesh.AddSample(sample);
        else
            _rejectedSamplesMesh.AddSample(sample);
    }

    private void Reset() {

        if (!_mcmc) {
            // Sample from the center
            SamplingPosition = 0.5f * (_distribution.MaxCoords + _distribution.MinCoords);
        } else {
            // Draw random point in range of distribution
            var x = (double)GD.RandRange(_distribution.MinCoords.X, _distribution.MaxCoords.X);
            var y = (double)GD.RandRange(_distribution.MinCoords.Y, _distribution.MaxCoords.Y);
            
            SamplingPosition = new Vector2((float)x, (float)y);
        }

        // reset the range of possible values
        _yrange = 0.5/Mathf.Max(Mathf.Abs(_distribution.VMin), Mathf.Abs(_distribution.VMax));
        YScale = _yScale;

        _numAccepted = 0;

        // delete all samples
        if (_samples.Count > 0)
            _samples.Clear();
        
        // clear the meshes
        _acceptedSamplesMesh?.Reset();
        _rejectedSamplesMesh?.Reset();
    }

    public void OnDistributionSelected(int index) {
        var name = _distributionSelector.GetItemText(index);
        var path = $"res://distributions/examples/{name}.tres";
        var res = GD.Load(path);

        // Blend between the two distributions
        YScaleTween?.Kill();
        YScaleTween = GetTree().CreateTween();
        YScaleTween.SetEase(Tween.EaseType.InOut);
        YScaleTween.TweenProperty(this, "YScale", 0, 0.2f);
        YScaleTween.TweenCallback(Callable.From(() => {
            Distribution = res;
        }));

        var dist = res as IDistribution2D;
        var new_yrange = 0.5/Mathf.Max(Mathf.Abs(dist.VMin), Mathf.Abs(dist.VMax));
        YScaleTween.TweenProperty(this, "YScale", _yScaleSlider.Value*new_yrange, 0.2f);
    }

    private void OnShowRejectedToggled(bool active) {
        GD.Print("Show rejected", active);
        _rejectedSamplesMesh.Visible = active;
    }

    private void OnYScaleValueChanged(double value) {
        YScaleTween?.Kill();

        YScaleTween = GetTree().CreateTween();
        YScaleTween.SetEase(Tween.EaseType.InOut);

        YScaleTween.TweenProperty(this, "YScale", value*_yrange, 1.0f);
    }

    public void OnSpotlightToggled(bool active) {
        SpotlightTween?.Kill();

        SpotlightTween = GetTree().CreateTween();
        SpotlightTween.SetEase(Tween.EaseType.InOut);

        SpotlightTween.TweenProperty(_surface, "Fade", active ? 1.0 : 0.0, 1.0f);
    }
	
    public void OnRotateToggled(bool active) {
        RotationTween?.Kill();

        RotationTween = GetTree().CreateTween();
        RotationTween.SetEase(Tween.EaseType.InOut);

        _rotating = active;
        RotationTween.TweenProperty(this, "_rotationSpeed", active ? 0.1 : 0.0, 1.0f);
    }

    public void OnSpeedValueChanged(double value) {
        _speed = value;
    }

    public void OnTopDownViewToggled(bool active) {
        // if active, tween to top-down view at 0 rotation
        // if inactive, tween to 45 degree view
        _topDownView = active;

        TopViewTween?.Kill();
        TopViewTween = GetTree().CreateTween();
        TopViewTween.SetEase(Tween.EaseType.InOut);

        TopViewTween.SetParallel();
        if(active) {
            TopViewTween.TweenProperty(_domain, "rotation", new Vector3(0, _domain.Rotation.Y + Mathf.Wrap(- _domain.Rotation.Y, -Mathf.Pi, Mathf.Pi), 0), 1.0f);
            TopViewTween.TweenProperty(this, "YScale", 0.0f, 1.0f);
            TopViewTween.TweenProperty(_camera, "rotation_degrees", new Vector3(-90, 0, 0), 1.0f);
            TopViewTween.TweenProperty(_camera, "position", new Vector3(0, 2, 0), 1.0f);

            // Stop rotation
            RotationTween?.Kill();
            _rotating = false;

            // Disable rotation button
            _rotateButton.Disabled = true;
            TopViewTween.SetParallel(false);
            
        } else {
            TopViewTween.TweenProperty(this, "YScale", _yScaleSlider.Value*_yrange, 1.0f);
            TopViewTween.TweenProperty(_camera, "rotation_degrees", new Vector3(-45, 0, 0), 1.0f);
            TopViewTween.TweenProperty(_camera, "position", new Vector3(0, 1.41f, 1.41f), 1.0f);

            // Maybe start rotation
            _rotating = _rotateButton.ButtonPressed;
            _rotateButton.Disabled = false;
        }
    }

    public void OnRunToggled(bool active) {
        _running = active;
    }

}
