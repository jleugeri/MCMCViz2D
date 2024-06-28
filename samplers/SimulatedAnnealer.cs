using Godot;
using System;

public partial class SimulatedAnnealer : MCMCSampler
{
    private double _temperature = 1.0;

    private Label _temperatureLabel;
    private HSlider _temperatureSlider;

    private double _coolingRate = 0.999;
    private Label _coolingRateLabel;
    private HSlider _coolingRateSlider;

    public override void InitControls(HBoxContainer container)
    {
        // Add a control for the temperature
        _temperatureLabel = new Label
        {
            Text = "Temperature: "
        };
        container.AddChild(_temperatureLabel);

        _temperatureSlider = new HSlider
        {
            MinValue = 0.0f,
            MaxValue = 1.0f,
            Step = 0.0f,
            Page = 0.0f,
            CustomMinimumSize = new Vector2(100, 20)
        };
        _temperatureSlider.SetValueNoSignal(_temperature);
        _temperatureSlider.Connect("value_changed", new Callable(this, nameof(OnTemperatureValueChanged)));
        container.AddChild(_temperatureSlider);

        // Add a control for the cooling rate
        _coolingRateLabel = new Label
        {
            Text = "Cooling Rate: "
        };
        container.AddChild(_coolingRateLabel);

        _coolingRateSlider = new HSlider
        {
            MinValue = 0.0001f,
            MaxValue = 1.0f,
            Step = 0.0f,
            Page = 0.0f,
            CustomMinimumSize = new Vector2(100, 20),
            ExpEdit = true
        };
        _coolingRateSlider.SetValueNoSignal(_coolingRate);
        _coolingRateSlider.Connect("value_changed", new Callable(this, nameof(OnCoolingRateValueChanged)));
        container.AddChild(_coolingRateSlider);

        // The other custom controls can come from the sampling distribution
        SamplingDistribution.InitControls(container);
    }

    public void OnCoolingRateValueChanged(double value)
    {
        _coolingRate = value;
    }

    public void OnTemperatureValueChanged(double value)
    {
        _temperature = value;
    }

    public override double P_accept(double E_new, double E_old, double P_old_to_new, double P_new_to_old)
    {
        return Math.Min(1.0, Math.Exp(-(E_new - E_old) / _temperature));
    }

    public override Sample Next()
    {
        // get next sample
        var sample =  base.Next();

        // update temperature
        _temperatureSlider.Value = _temperature*_coolingRate;

        return sample;
    }

    public override void Reset()
    {
        // call reset of base class
        base.Reset();

        // reset temperature
        _temperature = 1.0;
    }
}
