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
    
    [Export] private Node3D _selectionBox;

    [Export] public Node3D IsMovingNode;
    [Export] public Node3D IsFinishedNode;

    public void SetSpecificTarget(Vector3 target)
    {
        Velocity = Vector3.Zero;
        IsFinishedNode.Visible = false;
        IsMovingNode.Visible = true;
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

    }
}