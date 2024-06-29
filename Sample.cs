using Godot;

public class Sample
{
    public double[] Value;
    
    private double _probability = 0.0;
    private double _energy=-Mathf.Inf;
    public double Probability {
        get => _probability;
        set {
            _probability = value;
            _energy = -Mathf.Log((float)value);
        }
    }

    public double Energy {
        get => _energy;
        set {
            _energy = value;
            _probability = Mathf.Exp((float)-value);
        }
    }
    public bool Accepted;

    public Sample(double[] value, double probability, bool accepted) {
        Value = value;
        Probability = probability;
        Accepted = accepted;
    }
}
