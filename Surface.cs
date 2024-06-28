using Godot;
using System;
using System.Collections.Generic;

public partial class Surface : MeshInstance3D
{
	private Godot.Collections.Array surfaceArray;


    private ICanSample _samplingDistribution;
    public ICanSample SamplingDistribution {
        get { 
            return _samplingDistribution; 
        }
        set {
            if (_samplingDistribution != null) {
                _samplingDistribution.DistributionChanged -= OnSamplingDistributionChanged;
                _samplingDistribution.OriginChanged -= OnSamplingOriginChanged;
            }

            _samplingDistribution = value;

            if (_samplingDistribution != null) {
                _samplingDistribution.DistributionChanged += OnSamplingDistributionChanged;
                _samplingDistribution.OriginChanged += OnSamplingOriginChanged;
            }

            OnSamplingDistributionChanged();
        }
    }

    private void OnSamplingOriginChanged()
    {
        // GD.Print("Origin changed");
        // Set origin in shader
        var origin = new double[2];
        for(int i=0; i<2; i++) {
            origin[i] = (_samplingDistribution.Origin[i] - _samplingDistribution.MinCoords[i])/
                (_samplingDistribution.MaxCoords[i] - _samplingDistribution.MinCoords[i]);
        }
        (MaterialOverride as ShaderMaterial).SetShaderParameter("highlightPosition", origin);
    }

    private IDistribution _targetDistribution;
    public IDistribution TargetDistribution {
        get { 
            return _targetDistribution; 
        }
        set {
            if (_targetDistribution != null) {
                _targetDistribution.DistributionChanged -= OnTargetDistributionChanged;
            }

            _targetDistribution = value;

            if (_targetDistribution != null) {
                _targetDistribution.DistributionChanged += OnTargetDistributionChanged;
            }

            OnTargetDistributionChanged();
        }
    }

    private Vector2 _samplingPosition = new Vector2(0.0f, 0.0f);
    [Export] public Vector2 SamplingPosition {
        get { return _samplingPosition; }
        set {
            _samplingPosition = value;
            if (_targetDistribution != null)
                UpdateShader();
        }
    }

    private double _samplingStd;
    [Export] public double SamplingStd {
        get { return _samplingStd; }
        set {
            _samplingStd = value;
            if (_targetDistribution != null)
               UpdateShader();
        }
    }
    
    private double _fade = 0.0;
    [Export] public double Fade {
        get { return _fade; }
        set {
            _fade = value;
            if (_targetDistribution != null)
                UpdateShader();
        }
    }

    private void UpdateShader() {
        var dims = new double[2]{
            _targetDistribution.MaxCoords[0] - _targetDistribution.MinCoords[0], 
            _targetDistribution.MaxCoords[1] - _targetDistribution.MinCoords[1]
        };

        // Compute sample position in UV coordinates
        var posUV = new Vector2(
            (float)((_samplingPosition[0] - _targetDistribution.MinCoords[0]) / dims[0]),
            (float)((_samplingPosition[1] - _targetDistribution.MinCoords[1]) / dims[1])
        );

        (MaterialOverride as ShaderMaterial).SetShaderParameter("samplePos", posUV);
        (MaterialOverride as ShaderMaterial).SetShaderParameter("fade", _fade);

        // Compute inverse covariance matrix and set shader attribute

        (MaterialOverride as ShaderMaterial).SetShaderParameter(
            "sampleInvCov", 
            new Vector2(
                (float)Mathf.Pow(dims[0] / _samplingStd, 2), (float)Mathf.Pow(dims[1] / _samplingStd, 2)
            )
        );
    }

    private double _yScale = 1.0f;
    [Export] public double YScale {
        get { return _yScale; }
        set {
            _yScale = value;
            if(_targetDistribution != null)
                OnTargetDistributionChanged();
        }
    }

    [Export] public double res = 0.01f;


    private bool _recomputeMesh = false;
    private bool _recomputeHighlight = false;

    private void OnTargetDistributionChanged() {
        _recomputeMesh = true;
    }

    private void OnSamplingDistributionChanged() {
        _recomputeHighlight = true;
    }

    private void RecomputeHighlight(ICanSample dist) {
        _recomputeHighlight = false;

        GD.Print("Recomputing highlight");

        // compute number of points
        double x_min = dist.MinCoords[0];
        double x_max = dist.MaxCoords[0];
        double y_min = dist.MinCoords[1];
        double y_max = dist.MaxCoords[1];

        int num_x_points = (int)Mathf.Ceil((x_max - x_min) / res);
        int num_y_points = (int)Mathf.Ceil((y_max - y_min) / res);

        var highlight = new float[num_x_points,num_y_points];
		for(int i=0; i<num_x_points; i++) {
			for(int j=0; j<num_y_points; j++) {
				double x = i * res + x_min;
				double y = j * res + y_min;

                var z = dist.PDF(new double[2]{x + dist.Origin[0], y + dist.Origin[1]});
                highlight[j,i] = (float)(z / dist.PMax);
            }
        }

        var byteArray = new byte[highlight.Length * sizeof(float)];
        Buffer.BlockCopy(highlight, 0, byteArray, 0, byteArray.Length);
        
        Image img = Image.CreateFromData(num_x_points, num_y_points, false, Image.Format.Rf, byteArray);
        ImageTexture img_texture = ImageTexture.CreateFromImage(img);
        
        (MaterialOverride as ShaderMaterial).SetShaderParameter("highlightMap", img_texture);
    }

    private void RecomputeMesh(IDistribution dist) {
        _recomputeMesh = false;

        GD.Print("Recomputing mesh");
		surfaceArray = new Godot.Collections.Array();
		surfaceArray.Resize((int)Mesh.ArrayType.Max);

		var verts = new List<Vector3>();
		var uvs = new List<Vector2>();
		var normals = new List<Vector3>();
		var indices = new List<int>();
        var colors = new List<Color>();

        double x_min = dist.MinCoords[0];
        double x_max = dist.MaxCoords[0];
        double y_min = dist.MinCoords[1];
        double y_max = dist.MaxCoords[1];

        int num_x_points = (int)Mathf.Ceil((x_max - x_min) / res);
        int num_y_points = (int)Mathf.Ceil((y_max - y_min) / res);

		var zs = new double[num_x_points, num_y_points];
		// compute mesh
		for(int i=0; i<num_x_points; i++) {
			for(int j=0; j<num_y_points; j++) {
				double x = i * res + x_min;
				double y = j * res + y_min;
				
				zs[i,j] = dist.PDF(new double[2]{x, y});
			}
		}

		// create mesh 
		for(int i=0; i<num_x_points; i++) {
			for(int j=0; j<num_y_points; j++) {
				double x = i * res + x_min;
				double y = j * res + y_min;
				
				double z = zs[i,j];
				
				var vertex = new Vector3((float)x, (float)(z*YScale), (float)y);
				verts.Add(vertex);
				
                // Compute slope in x and y direction
                double zx0, zx1, zy0, zy1, _dx, _dy;
                _dx = 0.0f;
                _dy = 0.0f;

				if(i>0) {
					zx0 = zs[i-1,j]*YScale;
                    _dx += res;
				} else {
					zx0 = z*YScale;
				}

                if(i<num_x_points-1) {
                    zx1 = zs[i+1,j]*YScale;
                    _dx += res;
                } else {
                    zx1 = z*YScale;
                }

                if(j>0) {
                    zy0 = zs[i,j-1]*YScale;
                    _dy += res;
                } else {
                    zy0 = z*YScale;
                }

                if(j<num_y_points-1) {
                    zy1 = zs[i,j+1]*YScale;
                    _dy += res;
                } else {
                    zy1 = z*YScale;
                }

                // Compute normal
                var normal = new Vector3((float)((zx0-zx1)/_dx), 1.0f, (float)((zy0-zy1)/_dy)).Normalized();
				normals.Add(normal);
				
				// Compute UVs
				uvs.Add(new Vector2((float)(i/(double)num_x_points), (float)(j/(double)num_y_points)));

                // Compute color
                colors.Add(new Color((float)(z / dist.PMax), 0, 0, 1.0f));
				
				if(i>0 && j>0) {
					
					indices.Add(num_y_points*i + j);
					indices.Add(num_y_points*(i-1) + j);
					indices.Add(num_y_points*(i-1) + j - 1);
					
					indices.Add(num_y_points*i + j);
					indices.Add(num_y_points*(i-1) + j - 1);
					indices.Add(num_y_points*i + j - 1);
				}
			}
		}


		// Convert Lists to arrays and assign to surface array
		surfaceArray[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
		surfaceArray[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
		surfaceArray[(int)Mesh.ArrayType.Normal] = normals.ToArray();
		surfaceArray[(int)Mesh.ArrayType.Index] = indices.ToArray();
        surfaceArray[(int)Mesh.ArrayType.Color] = colors.ToArray();

		var arrMesh = Mesh as ArrayMesh;
		if (arrMesh != null)
		{
			GD.Print("DONE");
			// Create mesh surface from mesh array
			// No blendshapes, lods, or compression used.
            arrMesh.ClearSurfaces();
			arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);
		}
    }

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
        if(_recomputeMesh) {
            var dist = _targetDistribution;
            RecomputeMesh(dist);
        }

        if(_recomputeHighlight) {
            var dist = _samplingDistribution;
            RecomputeHighlight(dist);
        }
	}
}
