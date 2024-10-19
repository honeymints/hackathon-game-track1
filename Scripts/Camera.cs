using Godot;
using System;

public partial class Camera : Camera3D
{
	[Export] private NodePath leadCarPath; // Path to the lead car node
	[Export]private Car leadCar;

	[Export] private float followSpeed = 5f; // Controls how quickly the camera follows
	[Export] private Vector3 cameraOffset = new Vector3(0, 5, -10); // Offset above and behind the car

	public override void _Ready()
	{
		// Get the reference to the lead car based on the provided node path
		if (leadCarPath != null)
			leadCar = GetNode<Car>(leadCarPath);
	}

	public override void _Process(double delta)
	{
		if (leadCar != null)
			FollowLeadCar((float)delta);
	}

	private void FollowLeadCar(float delta)
	{
		Transform3D carTransform = leadCar.GlobalTransform;

		Vector3 targetPosition = carTransform.Origin
								 + carTransform.Basis.Y * cameraOffset.Y // Height offset
								 + carTransform.Basis.Z * cameraOffset.Z; // Backward offset
		
		Vector3 newPosition = GlobalTransform.Origin.Lerp(targetPosition, followSpeed * delta);
		
		LookAt(carTransform.Origin, Vector3.Up);
		
		GlobalTransform = new Transform3D(GlobalTransform.Basis, newPosition);
		
	}
}
