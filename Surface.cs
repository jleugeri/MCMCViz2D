using Godot;
using System;
using System.Collections.Generic;

public partial class Surface : MeshInstance3D
{
	private Godot.Collections.Array surfaceArray;

    private IDistribution2D _distribution;
    [Export] public Resource Distribution {
        get { 
            return _distribution as Resource; 
        }
        set {
            if (_distribution != null) {
                _distribution.DistributionChanged -= OnDistributionChanged;
            }

            _distribution = value as IDistribution2D;

            if (_distribution != null) {
                _distribution.DistributionChanged += OnDistributionChanged;
            }

            RecomputeMesh();
        }
    }

    private Vector2 _samplingPosition = new Vector2(0.0f, 0.0f);
    [Export] public Vector2 SamplingPosition {
        get { return _samplingPosition; }
        set {
            _samplingPosition = value;
            if (_distribution != null)
                UpdateShader();
        }
    }

    private double _samplingStd;
    [Export] public double SamplingStd {
        get { return _samplingStd; }
        set {
            _samplingStd = value;
            if (_distribution != null)
               UpdateShader();
        }
    }
    
    private double _fade = 0.0;
    [Export] public double Fade {
        get { return _fade; }
        set {
            _fade = value;
            if (_distribution != null)
                UpdateShader();
        }
    }

    private void UpdateShader() {
        var dims = _distribution.MaxCoords - _distribution.MinCoords;

        // Compute sample position in UV coordinates
        var posUV = new Vector2(
            (_samplingPosition.X - _distribution.MinCoords.X) / dims.X,
            (_samplingPosition.Y - _distribution.MinCoords.Y) / dims.Y
        );

        (MaterialOverride as ShaderMaterial).SetShaderParameter("samplePos", posUV);
        (MaterialOverride as ShaderMaterial).SetShaderParameter("fade", _fade);

        // Compute inverse covariance matrix and set shader attribute

        (MaterialOverride as ShaderMaterial).SetShaderParameter(
            "sampleInvCov", 
            new Vector2(
                (float)Mathf.Pow(dims.X / _samplingStd, 2), (float)Mathf.Pow(dims.Y / _samplingStd, 2)
            )
        );
    }

    private double _yScale = 1.0f;
    [Export] public double YScale {
        get { return _yScale; }
        set {
            _yScale = value;
            if(_distribution != null)
                RecomputeMesh();
        }
    }

    [Export] public double res = 0.01f;

    private void OnDistributionChanged() {
        RecomputeMesh();
    }

    private void RecomputeMesh() {
        GD.Print("Recomputing mesh");
		surfaceArray = new Godot.Collections.Array();
		surfaceArray.Resize((int)Mesh.ArrayType.Max);

		var verts = new List<Vector3>();
		var uvs = new List<Vector2>();
		var normals = new List<Vector3>();
		var indices = new List<int>();
        var colors = new List<Color>();

        double x_min = _distribution.MinCoords.X;
        double x_max = _distribution.MaxCoords.X;
        double y_min = _distribution.MinCoords.Y;
        double y_max = _distribution.MaxCoords.Y;

        int num_x_points = (int)Mathf.Ceil((x_max - x_min) / res);
        int num_y_points = (int)Mathf.Ceil((y_max - y_min) / res);

		var zs = new double[num_x_points, num_y_points];
		// compute mesh
		for(int i=0; i<num_x_points; i++) {
			for(int j=0; j<num_y_points; j++) {
				double x = i * res + x_min;
				double y = j * res + y_min;
				
				zs[i,j] = _distribution.PDF(x, y);
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
					zx0 = zs[i-1,j];
                    _dx += res;
				} else {
					zx0 = z;
				}

                if(i<num_x_points-1) {
                    zx1 = zs[i+1,j];
                    _dx += res;
                } else {
                    zx1 = z;
                }

                if(j>0) {
                    zy0 = zs[i,j-1];
                    _dy += res;
                } else {
                    zy0 = z;
                }

                if(j<num_y_points-1) {
                    zy1 = zs[i,j+1];
                    _dy += res;
                } else {
                    zy1 = z;
                }

                // Compute normal
                var normal = new Vector3((float)((zx0-zx1)*YScale/_dx), 1.0f, (float)((zy0-zy1)*YScale/_dy)).Normalized();
				normals.Add(normal);
				
				// Compute UVs
				uvs.Add(new Vector2((float)(i/(double)num_x_points), (float)(j/(double)num_y_points)));

                // Compute color
                colors.Add(new Color((float)((z - _distribution.VMin) / (_distribution.VMax-_distribution.VMin)), 0, 0, 1.0f));
				
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
	}
}
