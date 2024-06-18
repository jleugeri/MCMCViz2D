using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Mixture : Resource, IDistribution2D
{
    private List<IDistribution2D> _distributions = new List<IDistribution2D>();
    [Export] public Godot.Collections.Array<Resource> Distributions {
        get { return new Godot.Collections.Array<Resource>(_distributions.Cast<Resource>()); }
        set {
            _distributions = new List<IDistribution2D>();
            foreach (Resource res in value)
            {
                _distributions.Add(res as IDistribution2D);
            }
            Init();
        }
    }

    private List<double> _weights;

    private double _yScale = 1.0f;
    [Export] public double YScale {
        get { return _yScale; }
        set {
            _yScale = value;

            // update all distributions
            int i=0;
            foreach (IDistribution2D dist in Distributions.Cast<IDistribution2D>())
            {
                dist.YScale = _weights[i]*value;
                i++;
            }

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

    public double VMax { get => _distributions.Max(d => d.VMax); }
    public double VMin { get => _distributions.Min(d => d.VMin); }

    public event DistributionChangedEventHandler DistributionChanged;

    public void Init()
    {
        _weights = new List<double>();
        double weights_sum = 0.0;
        
        // collect weights
        foreach (var dist in _distributions)
        {
            _weights.Add(dist.YScale);
            weights_sum += dist.YScale;
        }

        // normalize weights
        for (int i = 0; i < _weights.Count; i++)
        {
            _weights[i] /= weights_sum;
        }

        // Force update of all distributions
        YScale = _yScale;
    }

    public double PDF(double x, double y)
    {
        double result = 0.0f;
        foreach (var dist in _distributions)
        {
            result += dist.PDF(x, y);
        }
        return result;
    }
}
