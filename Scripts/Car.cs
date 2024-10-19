using Godot;
using System;

public partial class Car : CharacterBody3D
{
	[Export] private Node casts;
	private int _rayNum;
	private float[] _interest;
	private float[] _danger;
	private Vector3 _chosenDir = Vector3.Zero;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_rayNum = casts.GetChildCount();
		_interest = new float[_rayNum];
		_interest = new float[_rayNum];
	}
	// Called every frame. 'delta' is the elapsed time since the previous frame.	public override void _Process(double delta)

	public Vector3 GetDir(int num)
	{
		RayCast3D cast = (RayCast3D)casts.GetChild(num);
		return -cast.GlobalTransform.Basis.Z;
	}

	public Vector3 CalculatePath(Vector3 pos)
	{
		var dir = GlobalTransform.Origin.DirectionTo(pos);
		_interest = new float[_rayNum];
		_danger = new float[_rayNum];
		SetInterest(dir);
		SetDanger();
		var newDir = DecidePath();
		return newDir;
	}

	public void SetInterest(Vector3 preferredDirection)
	{
		for (int i = 0; i < _rayNum; i++)
		{
			var d = GetDir(i).Dot(preferredDirection);
			_interest[i] = Math.Max(0, d);
		}
	}

	public void SetDanger()
	{
		for (int i = 0; i < _rayNum; i++)
		{
			var cast = (RayCast3D)casts.GetChild(i);
			var result = cast.IsColliding();
			_danger[i] = result ? 1.0f : 0.0f;
		}
	}

	public Vector3 DecidePath()
	{
		for (int i = 0; i < _rayNum; i++)
		{
			if (_danger[i] > 0.0f)
			{
				_interest[i] = 0;
			}
		}

		var dir = Vector3.Zero;
		for (int i = 0; i < _rayNum; i++)
		{
			dir += GetDir(i) * _interest[i];
		}

		dir = dir.Normalized();
		return dir;
	}
}
