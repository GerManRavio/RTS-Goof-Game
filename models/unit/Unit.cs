using Godot;

namespace RTSGoofGame.models.unit;

public partial class Unit : CharacterBody3D
{
    private float _movementSpeed = 5.0f;

    public bool IsSelected
    {
        get => _selectionBox.Visible;
        set => _selectionBox.Visible = value;
    }

    [Export] public NavigationAgent3D NavigationAgent { get; set; }

    [Export] private Node3D _selectionBox;

    [Export] public Node3D IsMovingNode;
    [Export] public Node3D IsFinishedNode;

    public void SetSpecificTarget(Vector3 target)
    {
        Velocity = Vector3.Zero;
        NavigationAgent.TargetPosition = target;
        IsFinishedNode.Visible = false;
        IsMovingNode.Visible = true;
    }

    public override void _Ready()
    {
        base._Ready();
        NavigationAgent.NavigationFinished += OnNavigationFinished;
        NavigationAgent.PathChanged += OnPathChanged;
        NavigationAgent.VelocityComputed += OnVelocityComputed;
    }

    public override void _ExitTree()
    {
        NavigationAgent.NavigationFinished -= OnNavigationFinished;
        NavigationAgent.PathChanged -= OnPathChanged;
        NavigationAgent.VelocityComputed -= OnVelocityComputed;
    }

    private void OnVelocityComputed(Vector3 safeVelocity)
    {
        var velocityWithGravity = safeVelocity;

        if (!IsOnFloor())
        {
            velocityWithGravity.Y = Velocity.Y;
        }
        else
        {
            velocityWithGravity.Y = 0;
        }

        Velocity = velocityWithGravity;

        var horizontalVelocity = new Vector3(Velocity.X, 0, Velocity.Z);

        if (horizontalVelocity.LengthSquared() > 0.1f)
        {
            var targetRotation = Basis.LookingAt(horizontalVelocity.Normalized()).GetRotationQuaternion();
            Quaternion = Quaternion.Slerp(targetRotation, 0.15f);
        }

        MoveAndSlide();
    }

    private void OnNavigationFinished()
    {
        IsFinishedNode.Visible = true;
        IsMovingNode.Visible = false;
    }

    private void OnPathChanged()
    {
        IsFinishedNode.Visible = false;
        IsMovingNode.Visible = true;
    }

    public override void _PhysicsProcess(double delta)
    {
        var currentVelocity = Velocity;
        if (!IsOnFloor())
        {
            currentVelocity += GetGravity() * (float)delta * 10.0f;
        }

        Velocity = currentVelocity;

        if (!NavigationAgent.IsNavigationFinished() && NavigationAgent.IsTargetReachable())
        {
            var nextPosition = NavigationAgent.GetNextPathPosition();
            var direction = (nextPosition - GlobalPosition).Normalized();

            NavigationAgent.Velocity = direction * _movementSpeed;
        }
        else
        {
            NavigationAgent.Velocity = Vector3.Zero;

            if (Velocity.Length() > 0.1f)
            {
                Velocity = Velocity.MoveToward(Vector3.Zero, (float)delta * _movementSpeed);
                MoveAndSlide();
            }
        }
    }
}