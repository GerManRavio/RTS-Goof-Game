using System.Collections.Generic;
using Godot;

namespace RTSGoofGame.models.navigation;

public class FlowField
{
    private readonly FlowFieldCell[,] _grid;
    private readonly int _gridSizeX;
    private readonly int _gridSizeY;
    private readonly float _cellSize;

    public FlowField(int width, int height, float cellSize, Vector3 origin)
    {
        _gridSizeX = width;
        _gridSizeY = height;
        _cellSize = cellSize;
        _grid = new FlowFieldCell[width, height];

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                var worldPos = origin + new Vector3(x * cellSize, 0, y * cellSize);
                _grid[x, y] = new FlowFieldCell(worldPos, new Vector2I(x, y));
            }
        }
    }

    public void Generate(Vector3 targetWorldPos, Node labelContainer = null)
    {
        var targetGridPos = WorldToGrid(targetWorldPos);

        ResetBestCosts();

        CalculateIntegrationField(targetGridPos);

        CalculateFlowDirections(labelContainer);
    }

    private void CalculateIntegrationField(Vector2I target)
    {
        if (!IsValidIndex(target.X, target.Y)) return;

        _grid[target.X, target.Y].BestCost = 0;
        var queue = new Queue<Vector2I>();
        queue.Enqueue(target);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var curCell = _grid[current.X, current.Y];

            foreach (var neighborPos in GetNeighbors(current))
            {
                var neighbor = _grid[neighborPos.X, neighborPos.Y];
                if (neighbor.Cost == byte.MaxValue) continue;

                var newCost = curCell.BestCost + neighbor.Cost;
                if (newCost < neighbor.BestCost)
                {
                    _grid[neighborPos.X, neighborPos.Y].BestCost = (ushort)newCost;
                    queue.Enqueue(neighborPos);
                }
            }
        }
    }

    private void CalculateFlowDirections(Node labelContainer = null)
    {
        if (labelContainer != null)
        {
            foreach (var child in labelContainer.GetChildren())
            {
                child.QueueFree();
            }
        }

        for (var x = 0; x < _gridSizeX; x++)
        {
            for (var y = 0; y < _gridSizeY; y++)
            {
                var cell = _grid[x, y];
                int bestCost = cell.BestCost;
                var bestNeighbor = cell.GridPosition;

                foreach (var neighborPos in GetNeighbors(cell.GridPosition, true))
                {
                    if (_grid[neighborPos.X, neighborPos.Y].BestCost < bestCost)
                    {
                        bestCost = _grid[neighborPos.X, neighborPos.Y].BestCost;
                        bestNeighbor = neighborPos;
                    }
                }

                _grid[x, y].Direction = new Vector2(bestNeighbor.X - x, bestNeighbor.Y - y).Normalized();

                // 3D Labels erzeugen
                if (labelContainer != null && cell.BestCost != ushort.MaxValue)
                {
                    var label = new Label3D();

                    labelContainer.AddChild(label);

                    label.Text = cell.BestCost.ToString();
                    label.FontSize = 128;
                    label.GlobalPosition = cell.WorldPosition + new Vector3(0, 0.2f, 0f);
                    label.RotationDegrees = new Vector3(-90, 0, 0);
                }
            }
        }
    }

    #region Helper

    public Vector2 GetDirectionAtWorldPos(Vector3 worldPos)
    {
        var gridPos = WorldToGrid(worldPos);
        return IsValidIndex(gridPos.X, gridPos.Y) ? _grid[gridPos.X, gridPos.Y].Direction : Vector2.Zero;
    }

    private Vector2I WorldToGrid(Vector3 worldPos) => new((int)(worldPos.X / _cellSize), (int)(worldPos.Z / _cellSize));

    private bool IsValidIndex(int x, int y) => x >= 0 && x < _gridSizeX && y >= 0 && y < _gridSizeY;

    private IEnumerable<Vector2I> GetNeighbors(Vector2I pos, bool includeDiagonal = false)
    {
        for (var x = -1; x <= 1; x++)
        {
            for (var y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;
                if (!includeDiagonal && Mathf.Abs(x) + Mathf.Abs(y) > 1) continue;

                int nx = pos.X + x, ny = pos.Y + y;
                if (IsValidIndex(nx, ny)) yield return new Vector2I(nx, ny);
            }
        }
    }

    private void ResetBestCosts()
    {
        for (var x = 0; x < _gridSizeX; x++)
        for (var y = 0; y < _gridSizeY; y++)
            _grid[x, y].BestCost = ushort.MaxValue;
    }

    #endregion
}