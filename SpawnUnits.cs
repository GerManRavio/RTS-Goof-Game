using Godot;

namespace RTSGoofGame;

public partial class SpawnUnits : Timer
{
    [Export] public PackedScene UnitScene;
    [Export] public Node3D SpawnPositionNode;

    private int _unitCount;
    [Export] public int MaxUnitCount = 1000;
    [Export] public int GridWidth = 100;
    [Export] public float Spacing = 2.0f;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Timeout += OnTimeout;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }

    private void OnTimeout()
    {
        if (UnitScene == null) return;
        if (_unitCount >= MaxUnitCount) return;

        var unit = UnitScene.Instantiate<Node3D>();
        GetParent().AddChild(unit);

        var basePos = SpawnPositionNode?.GlobalPosition ?? Vector3.Zero;

        var row = _unitCount / GridWidth;
        var col = _unitCount % GridWidth;

        var offset = new Vector3(col * Spacing, 0, row * Spacing);
        unit.GlobalPosition = basePos + offset;

        _unitCount++;
    }
}