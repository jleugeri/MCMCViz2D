using Godot;
using System;

public partial class Banana : Resource, IDistribution2D
{
    private double _var_x = 0.2f;
    [Export] public double VarX {
        get { return _var_x; }
        set {
            _var_x = value;
            DistributionChanged?.Invoke();
        }
    }

    private double _var_y = 0.025f;
    [Export] public double VarY {
        get { return _var_y; }
        set {
            _var_y = value;
            DistributionChanged?.Invoke();
        }
    }

    private double _bend = 0.5f;
    [Export] public double Bend {
        get { return _bend; }
        set {
            _bend = value;
            DistributionChanged?.Invoke();
        }
    }



    private double _yScale = 1.0f;
    [Export] public double YScale {
        get { return _yScale; }
        set {
            _yScale = value;
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

    public double VMax { get => PDF(0.0f, -0.25f); }
    public double VMin { get => 0.0f; }

    public event DistributionChangedEventHandler DistributionChanged;

    public double PDF(double x, double y)
    {
        var p_x = Mathf.Exp(-Mathf.Pow(x,2)/VarX);
        var p_y_x = Mathf.Exp(-Mathf.Pow(y-Bend*Mathf.Pow(x,2)+0.25,2)/VarY);
        return p_x*p_y_x * _yScale / Mathf.Sqrt(2*Mathf.Pi*VarX*VarY);
    }
}
