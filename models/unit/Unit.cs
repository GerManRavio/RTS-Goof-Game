using Godot;

namespace RTSGoofGame.models.unit;

public partial class Unit : CharacterBody3D
{
    private float _movementSpeed = 5.0f;
    private bool IsSelected { get; set; }

    [Export] public NavigationAgent3D NavigationAgent { get; set; }

    [Export] private Node3D _selectionBox;

    [Export] public Node3D IsMovingNode;
    [Export] public Node3D IsFinishedNode;

    public void SetSpecificTarget(Vector3 target)
    {
        NavigationAgent.TargetPosition = target;
        IsFinishedNode.Visible = false;
        IsMovingNode.Visible = true;
    }

    public override void _Ready()
    {
        base._Ready();
        NavigationAgent.NavigationFinished += OnNavigationFinished;
        NavigationAgent.PathChanged += OnPathChanged;
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
        if (!IsOnFloor())
        {
            Velocity += GetGravity() * (float)delta * 10;
        }

        Vector3 desiredVelocity;

        if (!NavigationAgent.IsNavigationFinished())
        {
            var nextPosition = NavigationAgent.GetNextPathPosition();
            var direction = (nextPosition - GlobalPosition).Normalized();
            desiredVelocity = direction * _movementSpeed;

            NavigationAgent.Velocity = desiredVelocity;
            
            var lookDirection = direction;
            lookDirection.Y = 0;
            if (!lookDirection.IsZeroApprox() && lookDirection.LengthSquared() > 0.001f)
            {
                LookAt(GlobalPosition + lookDirection, Vector3.Up);
            }
        }
        else
        {
            desiredVelocity = Velocity;
            desiredVelocity.X = Mathf.MoveToward(Velocity.X, 0, _movementSpeed * (float)delta * 10);
            desiredVelocity.Z = Mathf.MoveToward(Velocity.Z, 0, _movementSpeed * (float)delta * 10);
        }

        Velocity = desiredVelocity;
        MoveAndSlide();
    }

    public void SetSelected(bool selected)
    {
        _selectionBox.Visible = selected;
    }
}