using Godot;
using System;

public partial class Banana2D : Resource, IDistribution
{
    private HSlider _xStdSlider;
    private Label _xStdLabel;
    private HSlider _yStdSlider;
    private Label _yStdLabel;
    private HSlider _bendSlider;
    private Label _bendLabel;

    public int DIM => 2;

    private double _var_x = 0.2f;
    [Export] public double VarX {
        get { return _var_x; }
        set {
            _var_x = value;

            // update slider value
            if(_xStdSlider != null)
                _xStdSlider.SetValueNoSignal(Mathf.Sqrt(_var_x));
            DistributionChanged?.Invoke();
        }
    }

    private double _var_y = 0.05f;
    [Export] public double VarY {
        get { return _var_y; }
        set {
            _var_y = value;

            // update slider value
            if(_yStdSlider != null)
                _yStdSlider.SetValueNoSignal(Mathf.Sqrt(_var_y));
            DistributionChanged?.Invoke();
        }
    }

    private double _bend = 0.5f;
    [Export] public double Bend {
        get { return _bend; }
        set {
            _bend = value;

            // update slider value
            if(_bendSlider != null)
                _bendSlider.SetValueNoSignal(_bend);
            DistributionChanged?.Invoke();
        }
    }

    private double[] _minCoords = new double[2]{-1, -1};
    [Export] public double[] MinCoords {
        get { return _minCoords; }
        set {
            _minCoords = value;
            DistributionChanged?.Invoke();
        }
    }

    private double[] _maxCoords = new double[2]{1, 1};
    [Export] public double[] MaxCoords {
        get { return _maxCoords; }
        set {
            _maxCoords = value;
            DistributionChanged?.Invoke();
        }
    }

    private double[] _origin = new double[2]{0.0f, 0.0f};
    public double[] Origin { 
        get => _origin;
        set {
            _origin = value;
            DistributionChanged?.Invoke();
        }
    }

    public double VMax { get => PDF(new double[2]{0.0f, -0.25f}); }

    public event DistributionChangedEventHandler DistributionChanged;

    public double PDF(double[] x)
    {
        var p_x = Mathf.Exp(-Mathf.Pow(x[0]-_origin[0],2)/VarX);
        var p_y_x = Mathf.Exp(-Mathf.Pow(x[1]-_origin[1]-Bend*Mathf.Pow(x[0]-_origin[0],2)+0.25,2)/VarY);
        return p_x*p_y_x / Mathf.Sqrt(2*Mathf.Pi*VarX*VarY);
    }

    public void InitControls(HBoxContainer container)
    {   
        // create label node
        _xStdLabel = new Label
        {
            Text = "X-Std.: "
        };
        container.AddChild(_xStdLabel);


        // create slider node
        _xStdSlider = new HSlider
        {
            Name = "XStdSlider",
            MinValue = 0.01,
            MaxValue = 1.0,
            Step = 0.0,
            Page = 0.0,
            CustomMinimumSize = new Vector2(100, 20)
        };
        container.AddChild(_xStdSlider);

        // connect slider signal
        _xStdSlider.Connect("value_changed", new Callable(this, nameof(OnXStdValueChanged)));
        _xStdSlider.Value = Mathf.Sqrt(_var_x);

        // create label node
        _yStdLabel = new Label
        {
            Text = "Y-Std.: "
        };
        container.AddChild(_yStdLabel);

        // create slider node
        _yStdSlider = new HSlider
        {
            Name = "YStdSlider",
            MinValue = 0.01,
            MaxValue = 1.0,
            Step = 0.0,
            Page = 0.0,
            CustomMinimumSize = new Vector2(100, 20)
        };
        container.AddChild(_yStdSlider);

        // connect slider signal
        _yStdSlider.Connect("value_changed", new Callable(this, nameof(OnYStdValueChanged)));
        _yStdSlider.Value = Mathf.Sqrt(_var_y);

        // create label node
        _bendLabel = new Label
        {
            Text = "Bend: "
        };
        container.AddChild(_bendLabel);

        // create slider node
        _bendSlider = new HSlider
        {
            Name = "BendSlider",
            MinValue = 0.0,
            MaxValue = 5.0,
            Step = 0.0,
            Page = 0.0,
            CustomMinimumSize = new Vector2(100, 20)
        };
        container.AddChild(_bendSlider);

        // connect slider signal
        _bendSlider.Connect("value_changed", new Callable(this, nameof(OnBendValueChanged)));
        _bendSlider.Value = _bend;
    }

    private void OnXStdValueChanged(double value)
    {
        VarX = Mathf.Pow(value, 2);
    }

    private void OnYStdValueChanged(double value)
    {
        VarY = Mathf.Pow(value, 2);
    }

    private void OnBendValueChanged(double value)
    {
        Bend = value;
    }
}
