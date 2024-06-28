using Godot;
using System;

public partial class RejectionSampler : Resource, ISampler
{
    [Export] public double c = 1.0f;

    private ICanSample _samplingDistributionResource;
    [Export] public Resource SamplingDistributionResource {
        get { return _samplingDistributionResource as Resource; }
        set {
            if (value is ICanSample val)
            {
                _samplingDistributionResource = val;
            }
            else
            {
                GD.PushError("SamplingDistributionResource must implement ICanSample");
            }
        }
    }

    public void InitControls(HBoxContainer container)
    {
        // no controls
    }

    // Always uses the same uniform distribution to sample from
    public ICanSample SamplingDistribution { get => _samplingDistributionResource; }

    public IDistribution TargetDistribution { get; set; }

    public Sample Next()
    {
        // Draw a sample from the sampling distribution
        var sample = _samplingDistributionResource.Sample();
        var P_sample = sample.Probability;
        var P_new = TargetDistribution.PDF(sample.Value);

        // accept sample with probability P_new/(c*P_sample)
        if (GD.Randf() < P_new / (c * P_sample)) {
            sample.Accepted = true;
        }

        sample.Probability = P_new;
        return sample;
    }

    public void Reset()
    {
        // Nothing to reset
    }
}
