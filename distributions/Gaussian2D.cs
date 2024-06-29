using Godot;
using System;

public partial class Gaussian2D : Resource, ICanSample
{
    public int DIM => 2;

    private double[,] _cov;
    private double[,] _chol;
    private double[,] _inv_chol;
    private double _log_inv_partition;
    private double[] _origin;

    private Transform2D _transform;
    [Export] public Transform2D Transform {
        get { return _transform; }
        set {
            _transform = value;

            // initialize if necessary
            _cov ??= new double[2,2];
            _origin ??= new double[2];
            _chol ??= new double[2,2];
            _inv_chol ??= new double[2,2];

            // copy covarianve matrix
            _cov[0,0] = _transform[0,0];
            _cov[0,1] = _transform[0,1];
            _cov[1,0] = _transform[1,0];
            _cov[1,1] = _transform[1,1];

            // copy origin
            _origin[0] = _transform.Origin.X;
            _origin[1] = _transform.Origin.Y;

            // Compute cholesky decomposition of the covariance matrix
            _chol[0,0] = Mathf.Sqrt(_cov[0,0]);
            _chol[0,1] = 0.0f;
            _chol[1,0] = _cov[0,1]/_chol[0,0];
            _chol[1,1] = Mathf.Sqrt(_cov[1,1]-_chol[1,0]*_chol[1,0]);

            // compute inverse cholesky decomposition (C=LL^T -> C^{-1} = (L^T)^{-1}(L^{-1})) -> inv_chol = (L^T)^{-1}
            _inv_chol[0,0] = 1.0/_chol[0,0];
            _inv_chol[1,1] = 1.0/_chol[1,1];
            _inv_chol[0,1] = 0.0;
            _inv_chol[1,0] = -_chol[1,0]/_chol[1,1]*_inv_chol[0,0];

            // compute inverse determinant of covariance matrix
            var det = _cov[0,0]*_cov[1,1]-_cov[0,1]*_cov[1,0];
            _log_inv_partition = -Mathf.Log(2*Mathf.Pi*det)*0.5;

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

    public double[] Origin { 
        get => _origin;
        set {
            _transform = new(Transform[0,0], Transform[0,1], Transform[1,0], Transform[1,1], (float)value[0], (float)value[1]);
            _origin = value;
            OriginChanged?.Invoke();
        }
    }

    public double PMax => Mathf.Exp(-EMin);

    public double PMin => Mathf.Exp(-EMax);

    public double EMax {
        get {
            double e_best = -Mathf.Inf;
            // go through all corners of the range simplex and compute the energy there
            for(int i=0; i<(1<<DIM); i++) {
                // construct the corner
                var corner = new double[DIM];
                for(int j=0; j<DIM; j++) {
                    corner[j] = ((i>>j) & 1) == 1 ? MinCoords[j] : MaxCoords[j];
                }

                // compute the energy
                var e = Energy(corner);
                if(e > e_best) {
                    e_best = e;
                }
            }
            return e_best;
        }
    }

    public double EMin => Energy(_origin);

    public event DistributionChangedEventHandler DistributionChanged;
    public event OriginChangedEventHandler OriginChanged;

    public double PDF(double[] x)
    {
        return Mathf.Exp(-Energy(x));
    }

    public double Energy(double[] x)
    {
        // compute transformed x
        var xy = new double[2]{x[0] - _origin[0], x[1] - _origin[1]};

        // apply cholesky
        multiply_lower_trianglular_inplace(_inv_chol, xy);

        // compute trace
        return xy[0]*xy[0]+xy[1]*xy[1]-_log_inv_partition;
    }

    private void multiply_lower_trianglular_inplace(double[,] mat, double[] vec)
    {
        vec[1] = vec[0]*mat[1,0]+vec[1]*mat[1,1];
        vec[0] = vec[0]*mat[0,0];
    }

    public Sample Sample()
    {

        var x = new double[2];
        bool inside = false;
        while(!inside) {
            // generate isotropic gaussian
            x[0] = GD.Randfn(0.0, 1.0);
            x[1] = GD.Randfn(0.0, 1.0);

            // apply cholesky decomposition
            multiply_lower_trianglular_inplace(_chol, x);

            // apply translation
            x[0] += _origin[0];
            x[1] += _origin[1];

            // stop if sample is inside bounds, otherwise draw new sample
            inside = true;
            for (int i = 0; i < 2; i++) {
                if (x[i] < _minCoords[i] || x[i] > _maxCoords[i]) {
                    inside = false;
                    break;
                }
            }
        }

        return new Sample(x, PDF(x), false);
    }

    public virtual void InitControls(HBoxContainer container)
    {
        // No controls
    }
}
