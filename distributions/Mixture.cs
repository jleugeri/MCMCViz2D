using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Mixture : Resource, IDistribution
{
    private List<IDistribution> _distributions = new List<IDistribution>();
    [Export] public Godot.Collections.Array<Resource> Distributions {
        get { return new Godot.Collections.Array<Resource>(_distributions.Cast<Resource>()); }
        set {
            _distributions = new List<IDistribution>();
            foreach (Resource res in value)
            {
                _distributions.Add(res as IDistribution);
            }
            
            foreach (var dist in _distributions)
            {
                // assert that all distributions have the same dimension
                if (dist.DIM != DIM)
                {
                    throw new Exception("All distributions must have the same dimension");
                }
            }
        }
    }

    public int DIM => _distributions[0].DIM;

    private Godot.Collections.Array<double> _weights = new Godot.Collections.Array<double>();
    [Export] public Godot.Collections.Array<double> Weights {
        get { return _weights; }
        set {
            _weights = value;
            
            double weights_sum = 0.0;
            foreach (var weight in _weights)
            {
                weights_sum += weight;
            }

            // normalize weights
            for (int i = 0; i < _weights.Count; i++)
            {
                _weights[i] /= weights_sum;
            }

            DistributionChanged?.Invoke();
        }
    }

    public double[] MinCoords {
        get { 
            var minCoords = new double[DIM];

            // update min coords
            foreach (var dist in _distributions)
            {
                for (int i = 0; i < DIM; i++)
                {
                    minCoords[i] = Math.Min(minCoords[i], dist.MinCoords[i]);
                }
            }

            return minCoords;
         }
    }

    public double[] MaxCoords {
        get { 
            var maxCoords = new double[DIM];

            // update max coords
            foreach (var dist in _distributions)
            {
                for (int i = 0; i < DIM; i++)
                {
                    maxCoords[i] = Math.Max(maxCoords[i], dist.MaxCoords[i]);
                }
            }

            return maxCoords;
         }
    }


    public event DistributionChangedEventHandler DistributionChanged;

    private double[] _origin = new double[2]{0.0f, 0.0f};
    public double[] Origin { 
        get => _origin;
        set {
            _origin = value;
            DistributionChanged?.Invoke();
        }
    }

    // lazy option: just take the highest peaks of all the individual mixed distributions (not necessarily correct!)
    public double PMax => _distributions.Zip(_weights).Max(dw => dw.First.PMax*dw.Second);
    // lazy option: just take the lowest value of all the individual mixed distributions (not necessarily correct!)
    public double PMin => _distributions.Zip(_weights).Min(dw => dw.First.PMin*dw.Second);

    public double EMin => -Math.Log(PMax);
    public double EMax => -Math.Log(PMin);
    public double PDF(double[] x)
    {
        double result = 0.0f;

        // move x to origin
        var x_origin = new double[DIM];
        for (int i = 0; i < DIM; i++)
        {
            x_origin[i] = x[i] - Origin[i];
        }

        // Assert that we have the same number of distributions and weights
        if (_distributions.Count != _weights.Count)
        {
            throw new Exception("Number of distributions and weights must be the same but got " + _distributions.Count + " distributions and " + _weights.Count + " weights.");
        }

        for (int i=0; i<_distributions.Count; i++)
        {
            result += _weights[i]*_distributions[i].PDF(x_origin);
        }

        return result;
    }

    public double Energy(double[] x)
    {
        return -Math.Log(PDF(x));
    }

    public void InitControls(HBoxContainer container)
    {
        // No controls
    }
}
