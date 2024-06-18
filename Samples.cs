using Godot;

public class Sample
{
    public Vector2 Value;
    public double Probability;
    public bool Accepted;

    public Sample(Vector2 value, double probability, bool accepted) {
        Value = value;
        Probability = probability;
        Accepted = accepted;
    }
}
