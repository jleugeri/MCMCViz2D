using System.Collections.Generic;
using Godot;

// Event handler for notifying renderer that the distribution has changed
public delegate void DistributionChangedEventHandler();

public interface IDistribution
{
    // Event to notify renderer that the distribution has changed
    public event DistributionChangedEventHandler DistributionChanged;

    public int DIM {get;}

    public double PMax { get; }
    public double PMin { get; }

    public double EMax { get; }
    public double EMin { get; }

    public double[] MinCoords {get;}
    public double[] MaxCoords {get;}
    public double PDF(double[] x);

    public double Energy(double[] x);

    // Initialize controls for the distribution
    public void InitControls(HBoxContainer container);
}