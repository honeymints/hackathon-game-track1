using Godot;
using System;
using System.Collections.Generic;

public partial class Car : CharacterBody3D
{
	[Export] private Node casts;
	[Export] private PathFollow3D pathFollow;
	[Export] private float _speed = 20f;
	[Export] private float _rotationSpeed = 5f; 
	[Export] private float _avoidanceRadius = 5f; // Distance to start avoiding nearby cars
	[Export] private float _avoidanceForce = 0.5f;
	[Export] private float _repulsionStrength = 1.5f;
	[Export] private float rotationThreshold = 0.01f;
	
	private float targetYRotation; // Target Y rotation based on input
	private float originalYRotation; // Store original Y rotation
	
	private float _gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity");
	private Vector3 _gravityVelocity;
	private int _rayNum;
	private float _initalYAxis;
	private float[] _interest;
	private float[] _danger;
	private Vector3 _chosenDir = Vector3.Zero;
	private List<Car> _allCars;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print("Here is initial Basis" + GlobalBasis);
		originalYRotation = GlobalTransform.Basis.GetEuler().Y;
		_rayNum = casts.GetChildCount();
		_interest = new float[_rayNum];
		_interest = new float[_rayNum];
		_allCars = GetAllCars(); 
	}

	private List<Car> GetAllCars()
	{
		Node parent = GetParent();
		var cars = new List<Car>();

		foreach (Node node in parent.GetChildren())
		{
			if (node is Car car)
			{
				cars.Add(car);
				GD.Print("Car found: " + car);
			}
		}

		return cars;
	}
	
	public override void _Process(double delta)
	{
		if (pathFollow != null)
		{
			pathFollow.Progress += _speed * (float)delta;
	
			Vector3 targetPos = pathFollow.GlobalTransform.Origin;
			Vector3 dir = CalculatePath(targetPos);
			
			dir = AvoidOtherCars(dir, (float)delta);
			
			Velocity = dir * _speed;
			/*GD.Print(dir);*/
			UpdateTargetRotation(dir, delta);

		}
	}
	
	

	private Vector3 AvoidOtherCars(Vector3 currentDirection, float delta)
	{
		Vector3 avoidanceDir = Vector3.Zero;
		int numCloseCars = 0;

		foreach (Car otherCar in _allCars)
		{
		
			if (otherCar == this)
				continue;
			
			float distance = GlobalTransform.Origin.DistanceTo(otherCar.GlobalTransform.Origin);


			if (distance < _avoidanceRadius)
			{
				Vector3 repulsionDir = GlobalTransform.Origin.DirectionTo(otherCar.GlobalTransform.Origin).Normalized();
				avoidanceDir -= repulsionDir * _repulsionStrength; 
				numCloseCars++;
				
				if (distance < _avoidanceRadius / 2) 
				{
					MoveAndCollide(-repulsionDir * _speed * delta); 
					otherCar.MoveAndCollide(repulsionDir * _speed * delta);
				}
			}
			
		}
		
		if (numCloseCars > 0)
		{
			avoidanceDir = avoidanceDir.Normalized() * _avoidanceForce; 
			currentDirection = (currentDirection + avoidanceDir).Normalized(); 
		}

		return currentDirection;
	}
	
	private void UpdateTargetRotation(Vector3 targetDirection, double delta)
	{
		if (targetDirection != Vector3.Zero)
		{
			targetYRotation = Mathf.Atan2(targetDirection.X, targetDirection.Z);

			float currentYRotation = GlobalTransform.Basis.GetEuler().Y;
			float angleDifference = Mathf.Wrap(targetYRotation - currentYRotation, -Mathf.Pi, Mathf.Pi);

			if (Mathf.Abs(angleDifference) > rotationThreshold)
			{
				SmoothRotate((float)angleDifference);
			}
			else
			{
				ResetToOriginalRotation((float) delta);
			}
		}
	}
	
	private void SmoothRotate(float angleDifference)
	{
		float currentYRotation = GlobalTransform.Basis.GetEuler().Y;
		double rotationAmount = Mathf.Min(_rotationSpeed * GetProcessDeltaTime(), Mathf.Abs(angleDifference));

		// Apply the rotation
		if (angleDifference > 0)
		{
			RotateY((float)rotationAmount);
		}
		else
		{
			RotateY(-(float)rotationAmount);
		}
	}
	private void ResetToOriginalRotation(float delta)
	{
		float currentYRotation = GlobalTransform.Basis.GetEuler().Y;
		float angleDifference = Mathf.Wrap(originalYRotation - currentYRotation, -Mathf.Pi, Mathf.Pi);

		if (Mathf.Abs(angleDifference) > rotationThreshold)
		{
			// Smoothly rotate back to the original rotation
			float rotationAmount = Mathf.Min(_rotationSpeed * delta, Mathf.Abs(angleDifference));
			RotateY(angleDifference > 0 ? rotationAmount : -rotationAmount);
		}
	}
	
	public override void _PhysicsProcess(double delta)
	{
		
		Vector3 gravityForce = Gravity((float)delta);

		Velocity += gravityForce;
		MoveAndSlide();

		// Smoothly rotate towards the target Y rotation
		SmoothRotate((float)delta);

	}

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
		dir.Y = 0f;
		dir = dir.Normalized();
		return dir;
	}
	
	private Vector3 Gravity(float delta)
	{
		if (IsOnFloor())
		{
			_gravityVelocity = Vector3.Zero;
			/*Velocity = new Vector3(Velocity.X, 0, Velocity.Z);*/
		}
		else
		{
			_gravityVelocity = _gravityVelocity.MoveToward(
				new Vector3(0, Velocity.Y - _gravity, 0),
				_gravity * delta
			);
		}

		return _gravityVelocity;
	}
}
