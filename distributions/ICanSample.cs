using Godot;
using System;

// Event handler for notifying renderer that the distribution has changed
public delegate void OriginChangedEventHandler();

public interface ICanSample: IDistribution
{
    public Sample Sample();

    public double[] Origin { get; set; }

    // Event to notify renderer that the origin has changed
    public event OriginChangedEventHandler OriginChanged;
}
