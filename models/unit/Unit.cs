using Godot;

namespace RTSGoofGame.models.unit;

public partial class Unit : CharacterBody3D
{
	private float _movementSpeed = 5.0f;
	private Vector3 _movementTargetPosition;
	public bool IsSelected { get; private set; }

	[Export] public NavigationAgent3D NavigationAgent { get; set;}

	[Export] private Node3D _selectionBox;

	public Vector3 MovementTarget
	{
		get => NavigationAgent.TargetPosition;
		set => NavigationAgent.TargetPosition = value;
	}

	public override void _Ready()
	{
		base._Ready();
	}

	public override void _PhysicsProcess(double delta)
	{
		var velocity = Velocity;

		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta * 10;
		}

		if (!NavigationAgent.IsNavigationFinished())
		{
			var nextPosition = NavigationAgent.GetNextPathPosition();
			var direction = (nextPosition - GlobalPosition).Normalized();

			velocity.X = direction.X * _movementSpeed;
			velocity.Z = direction.Z * _movementSpeed;

			var lookDirection = direction;
			lookDirection.Y = 0;
			if (!lookDirection.IsZeroApprox())
			{
				LookAt(GlobalPosition + lookDirection, Vector3.Up);
			}
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, _movementSpeed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, _movementSpeed);
		}

		Velocity = velocity;
		MoveAndSlide();
	}
	
	public void SetSelected(bool selected)
	{
		_selectionBox.Visible = selected;
	}
}
