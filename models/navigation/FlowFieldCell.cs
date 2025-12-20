using Godot;

namespace RTSGoofGame.models.navigation;

public struct FlowFieldCell(Vector3 worldPos, Vector2I gridPos)
{
    public Vector2I GridPosition = gridPos;
    public Vector3 WorldPosition = worldPos;
    public byte Cost = 1;
    public ushort BestCost = ushort.MaxValue;
    public Vector2 Direction = Vector2.Zero;
}