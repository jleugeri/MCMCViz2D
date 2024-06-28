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

    private double _rotationSpeed = 0.0;
    private bool _rotating = false;
    private bool _running = false;
    private bool _topDownView = false;
    private double _speed = 1.0f;

    private Camera3D _camera;

    private int _numAccepted = 0;


    private OptionButton _distributionSelector;
    private CheckButton _rotateButton;
    private OptionButton _samplerSelector;
    private Node3D _domain;
    private SamplesMesh _acceptedSamplesMesh;
    private SamplesMesh _rejectedSamplesMesh;
    private HSlider _yScaleSlider;

    private Surface _surface;

    private List<Sample> _samples = new List<Sample>();

    private ISampler _sampler;
    [Export] public Resource Sampler {
        get { 
            return _sampler as Resource; 
        }
        set {
            _sampler = value as ISampler;
            _sampler.TargetDistribution = _distribution;

            if (_sampler != null)
                Reset();
        }
    }

    private IDistribution _distribution;
    [Export] public Resource Distribution {
        get { 
            return _distribution as Resource; 
        }
        set {
            if (_distribution != null)
                _distribution.DistributionChanged -= Reset;
            
            _distribution = value as IDistribution;

            // assert that the distribution is 2D
            if (_distribution?.DIM != 2) {
                throw new Exception("Distribution must be 2D");
            }

            if (_surface != null)
                _surface.TargetDistribution = _distribution;

            if (_sampler != null)
                _sampler.TargetDistribution = _distribution;

            if (_distribution != null)
            {
                _distribution.DistributionChanged += Reset;
                Reset();
            }
        }
    }

    private double _yScale = 1.0f;
    [Export] public double YScale {
        get { return _yScale; }
        set {
            _yScale = value;
            if(_surface != null)
                _surface.YScale = value / _distribution.VMax;
            if(_acceptedSamplesMesh != null)
                _acceptedSamplesMesh.YScale = value / _distribution.VMax;
            if(_rejectedSamplesMesh != null)
                _rejectedSamplesMesh.YScale = value / _distribution.VMax;
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
        _samplerSelector = GetNode<OptionButton>("%Sampler");

        _surface.TargetDistribution = _distribution;

        // Go through all .tres files in the res://distributions/Examples folder and add them to the dropdown
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

        // Go through all .tres files in the res://samplers/Examples folder and add them to the dropdown
        _samplerSelector.Clear();
        using var dir2 = DirAccess.Open("res://samplers/examples");
        if (dir2 != null)
        {
            dir2.ListDirBegin();
            string fileName = dir2.GetNext();
            while (fileName != "")
            {
                if (dir2.CurrentIsDir())
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
                        _samplerSelector.AddItem(name);
                    }
                }
                fileName = dir2.GetNext();
            }
        }
        else
        {
            GD.Print("An error occurred when trying to access the path.");
        }

        Reset();

        // Initialize to values selected in UI
        OnTopDownViewToggled(GetNode<CheckButton>("%TopDownView").ButtonPressed);
        OnRotateToggled(_rotateButton.ButtonPressed);
        OnSpotlightToggled(GetNode<CheckButton>("%Spotlight").ButtonPressed);
        OnSpeedValueChanged(GetNode<HSlider>("%Speed").Value);
        OnRunToggled(GetNode<Button>("%Run").ButtonPressed);
        OnYScaleValueChanged(_yScaleSlider.Value);
        OnShowRejectedToggled(GetNode<CheckButton>("%ShowRejected").ButtonPressed);
        OnShowAcceptedToggled(GetNode<CheckButton>("%ShowAccepted").ButtonPressed);
        OnDistributionSelected(_distributionSelector.Selected);
        OnSamplerItemSelected(_samplerSelector.Selected);
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

    private void Next() {
        // Create new sample
        var sample = _sampler.Next();

        // Add sample to list
        _samples.Add(sample);

        // Update samples mesh
        if (sample.Accepted) {
            _numAccepted++;
            _acceptedSamplesMesh.AddSample(sample);
        } else {
            _rejectedSamplesMesh.AddSample(sample);
        }
    }

    private void Reset() {
        // reset sampler
        _sampler.Reset();

        // set the sampling distribution
        if (_surface != null)
            _surface.SamplingDistribution = _sampler.SamplingDistribution;

        // // reset the range of possible values
        // if(_distribution != null)
        //     _yrange = 0.5/_distribution.VMax;

        _numAccepted = 0;

        // delete all samples
        if (_samples.Count > 0)
            _samples.Clear();

        YScale = _yScale;
        
        // clear the meshes
        _acceptedSamplesMesh?.Reset();
        _rejectedSamplesMesh?.Reset();
    }

    public void OnSamplerItemSelected(int selected) {
        var ctrl = GetNode<HBoxContainer>("%CustomSamplerControls");

        // free up all old controls
        foreach (var child in ctrl.GetChildren()) {
            ctrl.RemoveChild(child);
            child.QueueFree();
        }

        var name = _samplerSelector.GetItemText(selected);
        var path = $"res://samplers/examples/{name}.tres";
        var res = GD.Load(path);

        Sampler = res;

        // initialize custom controls
        _sampler.InitControls(ctrl);

        Reset();
    }

    public void OnDistributionSelected(int index) {
        var ctrl = GetNode<HBoxContainer>("%CustomDistributionControls");

        // free up all old controls
        foreach (var child in ctrl.GetChildren()) {
            ctrl.RemoveChild(child);
            child.QueueFree();
        }

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

        var dist = res as IDistribution;
        YScaleTween.TweenProperty(this, "YScale", _yScaleSlider.Value*_yrange, 0.2f);

        // initialize custom controls
        dist.InitControls(ctrl);
    }

    private void OnShowRejectedToggled(bool active) {
        GD.Print("Show rejected", active);
        _rejectedSamplesMesh.Visible = active;
    }

    private void OnShowAcceptedToggled(bool active) {
        GD.Print("Show accepted", active);
        _acceptedSamplesMesh.Visible = active;
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
