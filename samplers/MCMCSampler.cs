using Godot;
using System;

public partial class MCMCSampler : Resource, ISampler
{
    private Sample _lastSample;

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
    

    public ICanSample SamplingDistribution { get => _samplingDistributionResource; }

    public IDistribution TargetDistribution { get; set; }

    public virtual void InitControls(HBoxContainer container)
    {
        // The only custom controls can come from the sampling distribution
        _samplingDistributionResource.InitControls(container);
    }

    public virtual double P_accept(double P_new, double P_old, double P_old_to_new, double P_new_to_old)
    {
        return Mathf.Min(1.0, P_new_to_old/P_old_to_new * P_new/P_old);
    }

    public virtual Sample Next()
    {
        // alias variables
        var P_S_given_X = _samplingDistributionResource;    // P_{S|X}
        var P_X = TargetDistribution;                       // P_X
        var x = _lastSample?.Value;                         // x

        // draw new sample
        var sample = P_S_given_X.Sample();                  // s ~ P_{S|X}
        var s = sample.Value;                               // s
        
        if (_lastSample == null) {
            // always accept the first sample
            sample.Accepted = true;
            sample.Probability = P_X.PDF(s);
        } else {
            // compute probabilities
            var P_new = P_X.PDF(s);                         // P_X(s)
            var P_old = P_X.PDF(x);                         // P_X(x)
            var P_old_to_new = P_S_given_X.PDF(s);          // P_{S|X}(x -> s)
            P_S_given_X.Origin = s;
            var P_new_to_old = P_S_given_X.PDF(x);          // P_{S|X}(s -> x)
            
            // accept sample with probability P_{S|X}(s -> x)/P_{S|X}(x -> s) * P_X(s)/P_X(x)
            sample.Accepted = GD.Randf() < P_accept(P_new, P_old, P_old_to_new, P_new_to_old);
            sample.Probability = P_new;
        } 
        
        if (sample.Accepted) {
            // update last sample if the sample was accepted
            _lastSample = sample;
        } else {
            // revert distribution if the sample was not accepted
            P_S_given_X.Origin = x;
        }

        return sample;
    }

    public virtual void Reset()
    {
        // reset last sample
        _lastSample = null;

        // draw new random origin from the target distribution's domain
        var unif = new Uniform2D
        {
            MinCoords = TargetDistribution.MinCoords,
            MaxCoords = TargetDistribution.MaxCoords
        };
        var samp = unif.Sample();
        _samplingDistributionResource.Origin = samp.Value;
    }
}
