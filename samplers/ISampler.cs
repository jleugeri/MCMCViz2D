using Godot;
using System;

public interface ISampler
{
    public Sample Next();

    public void Reset();

    public ICanSample SamplingDistribution { get; }

    public IDistribution TargetDistribution { get; set; }

    public void InitControls(HBoxContainer container);
}
