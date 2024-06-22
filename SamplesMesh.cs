using Godot;
using System;
using System.Collections.Generic;

public partial class SamplesMesh : MultiMeshInstance3D
{

    private List<Sample> _samples = new List<Sample>();

    private double _yScale = 1.0f;
    public double YScale {
        get { return _yScale; }
        set {
            _yScale = value;

            // move all samples to new height
            for (int i = 0; i < _samples.Count; i++) {
                var t = Multimesh.GetInstanceTransform(i);
                t.Origin.Y = (float)(_yScale*_samples[i].Probability);
                // Update sample indicator
                Multimesh.SetInstanceTransform(
                    i, 
                    t
                );
            }
        }
    }

    private bool _visible = true;
    new public bool Visible {
        get { return _visible; }
        set {
            _visible = value;
            Multimesh.VisibleInstanceCount = _visible ? _samples.Count : 0;
        }
    }

    public void Reset()
    {
        _samples.Clear();
        Multimesh.VisibleInstanceCount = 0;
    }

    public void AddSample(Sample sample)
    {
        if(_samples.Count < Multimesh.InstanceCount) {
            // Update new sample indicator
            Multimesh.SetInstanceTransform(
                _samples.Count, 
                new Transform3D(Basis.Identity, new Vector3(sample.Value.X, (float)(YScale*sample.Probability), sample.Value.Y))
            );

            if (sample.Accepted) {
                Multimesh.SetInstanceColor(_samples.Count, new Color(0, 1, 0, 1));
            } else {
                Multimesh.SetInstanceColor(_samples.Count, new Color(1, 0, 0, 1));
            }

            _samples.Add(sample);
            if(_visible)
                Multimesh.VisibleInstanceCount = _samples.Count;
        }
    }

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        Reset();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
