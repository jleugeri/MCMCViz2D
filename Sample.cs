using Godot;

public class Sample
{
    public double[] Value;
    public double Probability;
    public bool Accepted;

    public Sample(double[] value, double probability, bool accepted) {
        Value = value;
        Probability = probability;
        Accepted = accepted;
    }
}
