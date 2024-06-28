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
    
    public double VMax => _inv_area;

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
        return VMax;
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
