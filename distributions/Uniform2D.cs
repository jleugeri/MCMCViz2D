using Godot;
using System;

public partial class Uniform2D : Resource, ICanSample
{
    public int DIM => 2;

    private double _inv_area = 4.0;
    private void ComputeInvArea()
    {
        _inv_area = 1.0 / ((_maxCoords[0] - _minCoords[0]) * (_maxCoords[1] - _minCoords[1]));
    }

    private double[] _origin = new double[2]{0.0f, 0.0f};
    public double[] Origin { 
        get => _origin;
        set {
            _origin = value;
            OriginChanged?.Invoke();
        }
    }
    
    public double PMax => _inv_area;
    public double PMin => _inv_area;

    public double EMax => -Mathf.Log(PMin);
    public double EMin => -Mathf.Log(PMax);

    private double[] _minCoords = new double[2]{-1, -1};
    [Export] public double[] MinCoords {
        get { return _minCoords; }
        set {
            _minCoords = value;
            ComputeInvArea();
            DistributionChanged?.Invoke();
        }
    }

    private double[] _maxCoords = new double[2]{1, 1};
    [Export] public double[] MaxCoords {
        get { return _maxCoords; }
        set {
            _maxCoords = value;
            ComputeInvArea();
            DistributionChanged?.Invoke();
        }
    }

    public event DistributionChangedEventHandler DistributionChanged;
    public event OriginChangedEventHandler OriginChanged;

    public double PDF(double[] x)
    {
        return PMax;
    }

    public double Energy(double[] x)
    {
        return -Mathf.Log(PDF(x));
    }

    public Sample Sample()
    {
        var x = new double[2];
        x[0] = GD.RandRange(_minCoords[0], _maxCoords[0]);
        x[1] = GD.RandRange(_minCoords[1], _maxCoords[1]);
        return new Sample(x, PDF(x), false);
    }

    public void InitControls(HBoxContainer container)
    {
        // No controls
    }
}
