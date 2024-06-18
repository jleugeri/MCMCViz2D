using Godot;
using System;

public partial class Gaussian : Resource, IDistribution2D
{
    private double _yScale = 1.0f;
    [Export] public double YScale {
        get { return _yScale; }
        set {
            _yScale = value;
            DistributionChanged?.Invoke();
        }
    }

    private Transform2D _transform = new(10.0f, 0.0f, 0.0f, 10.0f, 0.0f, 0.0f);
    [Export] public Transform2D Transform {
        get { return _transform; }
        set {
            _transform = value;
            DistributionChanged?.Invoke();
        }
    }

    private Vector2 _minCoords = new Vector2(-1, -1);
    [Export] public Vector2 MinCoords {
        get { return _minCoords; }
        set {
            _minCoords = value;
            DistributionChanged?.Invoke();
        }
    }

    private Vector2 _maxCoords = new Vector2(1, 1);
    [Export] public Vector2 MaxCoords {
        get { return _maxCoords; }
        set {
            _maxCoords = value;
            DistributionChanged?.Invoke();
        }
    }

    public double VMax { get => PDF(Transform.Origin.X, Transform.Origin.Y); }
    public double VMin { get => 0.0f; }

    public event DistributionChangedEventHandler DistributionChanged;

    public double PDF(double x, double y)
    {
        var xy = new Vector2((float)x, (float)y)-Transform.Origin;
        return Mathf.Exp(-(xy).Dot(Transform.BasisXform(xy)))*_yScale/Mathf.Sqrt(2*Mathf.Pi*(Transform[0,0]*Transform[1,1]-Transform[0,1]*Transform[1,0]));
    }
}
