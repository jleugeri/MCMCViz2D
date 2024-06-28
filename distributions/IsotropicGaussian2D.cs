using Godot;
using System;

public partial class IsotropicGaussian2D : Gaussian2D
{
    private HSlider _radiusSlider;
    private Label _radiusLabel;

    private double _std;

    [Export]
    public double StandardDeviation {
        get { return _std; }
        set {
            _std = value;

            if(Origin == null)
            {
                Origin = new double[2]{0.0, 0.0};
            }
            
            Transform = new Transform2D(
                (float)Mathf.Pow(_std, 2), 0.0f, 
                0.0f, (float)Mathf.Pow(_std, 2), 
                (float)Origin[0], (float)Origin[1]
            );
        }
    }

    public override void InitControls(HBoxContainer container)
    {   
        // create label node
        _radiusLabel = new Label
        {
            Text = "Standard Deviation: "
        };
        container.AddChild(_radiusLabel);

        // create slider node
        _radiusSlider = new HSlider
        {
            Name = "RadiusSlider",
            Value = 0.1,
            MinValue = 0.01,
            MaxValue = 1.0,
            Step = 0.0,
            Page = 0.0,
            CustomMinimumSize = new Vector2(100, 20)
        };
        container.AddChild(_radiusSlider);

        // connect slider signal
        _radiusSlider.Connect("value_changed", new Callable(this, nameof(OnRadiusValueChanged)));

        OnRadiusValueChanged(_radiusSlider.Value);
    }

    private void OnRadiusValueChanged(double value)
    {
        StandardDeviation = value;
    }
}
