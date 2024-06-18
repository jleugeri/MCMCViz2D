using Godot;

// Event handler for notifying renderer that the distribution has changed
public delegate void DistributionChangedEventHandler();

public interface IDistribution2D
{
    // Event to notify renderer that the distribution has changed
    public event DistributionChangedEventHandler DistributionChanged;

    public double YScale {get; set;}

    public double VMax {get; }
    public double VMin {get; }

    public Vector2 MinCoords {get;}
    public Vector2 MaxCoords {get;}
    public double PDF(double x, double y);
}