using System.Collections.Generic;
using System.Linq;
using Godot;
using RTSGoofGame.models.unit;

namespace RTSGoofGame.models.camera;

public partial class Camera : Camera3D
{
    [Export] public float MovementSpeed = 10.0f;
    [Export] public float SpeedMultiplier = 2.5f;

    [Export] public float ZoomSpeed = 5.0f;
    [Export] public float MinZoom = 5.0f;
    [Export] public float MaxZoom = 50.0f;
    [Export] public float ZoomDuration = 0.2f;

    [Export] public Control SelectionBox;
    [Export] public Label FpsLabel;
    [Export] public Label UnitCountLabel;
    [Export] public Label UnitSelectionCountLabel;

    private Vector2 _dragStart;
    private bool _isDragging;
    private readonly Queue<float> _rotationCameraQueue = new();
    private bool _isRotating;
    private readonly HashSet<Unit> _selectedUnits = [];

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        base._Ready();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        UpdateUi();
        var inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");

        if (inputDir != Vector2.Zero)
        {
            var currentSpeed = MovementSpeed;

            if (Input.IsActionPressed("ui_shift"))
            {
                currentSpeed *= SpeedMultiplier;
            }

            var direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

            var velocity = new Vector3(direction.X, 0, direction.Z) * currentSpeed * (float)delta;
            GlobalPosition += velocity;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } leftClickEvent:
                _dragStart = leftClickEvent.Position;
                _isDragging = true;
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } leftClickEvent:
            {
                if (_isDragging)
                {
                    _isDragging = false;
                    SelectionBox.Visible = false;

                    var isStepping = Input.IsActionPressed("ui_shift") || Input.IsKeyPressed(Key.Shift);

                    if (_dragStart.DistanceTo(leftClickEvent.Position) < 5)
                    {
                        SelectUnitAtMousePosition(leftClickEvent.Position, isStepping);
                    }
                    else
                    {
                        SelectUnitsInBox(_dragStart, leftClickEvent.Position, isStepping);
                    }
                }

                break;
            }
            case InputEventMouseMotion mouseMotionEvent when _isDragging:
                UpdateSelectionBox(mouseMotionEvent.Position);
                break;
            case InputEventKey { Keycode: Key.F, Pressed: true }:
                ToggleUnitDebug();
                break;
        }

        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Right } rightClickEvent)
        {
            OrderMovementToSelectedUnits(rightClickEvent.Position);
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (_rotationCameraQueue.Count < 3)
        {
            if (@event.IsActionPressed("rotate_left"))
            {
                _rotationCameraQueue.Enqueue(Mathf.Pi / 2.0f);
                ProcessRotateCameraQueue();
            }
            else if (@event.IsActionPressed("rotate_right"))
            {
                _rotationCameraQueue.Enqueue(-Mathf.Pi / 2.0f);
                ProcessRotateCameraQueue();
            }
        }

        if (@event.IsActionPressed("zoom_in")) ZoomCamera(-ZoomSpeed);
        if (@event.IsActionPressed("zoom_out")) ZoomCamera(ZoomSpeed);

        base._Input(@event);
    }

    #region UI

    private void UpdateUi()
    {
        if (FpsLabel != null)
        {
            FpsLabel.Text = $"FPS: {Engine.GetFramesPerSecond()}";
        }

        if (UnitCountLabel != null)
        {
            var unitCount = GetTree().GetNodesInGroup("unit").Count;
            UnitCountLabel.Text = $"Units: {unitCount}";
        }

        _selectedUnits.RemoveWhere(u => !IsInstanceValid(u));
        if (UnitSelectionCountLabel != null)
        {
            UnitSelectionCountLabel.Text = $"Selected Units: {_selectedUnits.Count}";
        }
    }
    
    private void ToggleUnitDebug()
    {
        var allUnits = GetTree().GetNodesInGroup("unit").OfType<Unit>();
        var allUnitsArray = allUnits as Unit[] ?? allUnits.ToArray();
        var firstUnit = allUnitsArray.FirstOrDefault();
        if (firstUnit == null) return;

        var newDebugState = !firstUnit.NavigationAgent.DebugEnabled;

        foreach (var unit in allUnitsArray)
        {
            if (IsInstanceValid(unit) && unit.NavigationAgent != null)
            {
                unit.NavigationAgent.DebugEnabled = newDebugState;
            }
        }
    }

    #endregion

    #region Camera Rotation

    private void ZoomCamera(float amount)
    {
        var zoomVector = Transform.Basis.Z * amount;
        var targetPos = GlobalPosition + zoomVector;

        if (Mathf.Abs(zoomVector.Y) > 0.001f)
        {
            if (targetPos.Y < MinZoom || targetPos.Y > MaxZoom)
            {
                var clampedY = Mathf.Clamp(targetPos.Y, MinZoom, MaxZoom);
                var ratio = (clampedY - GlobalPosition.Y) / zoomVector.Y;
                targetPos = GlobalPosition + (zoomVector * ratio);
            }
        }

        var tween = GetTree().CreateTween();
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.SetEase(Tween.EaseType.Out);

        tween.TweenProperty(this, "global_position", targetPos, ZoomDuration);
    }

    private void ProcessRotateCameraQueue()
    {
        if (_isRotating || _rotationCameraQueue.Count == 0)
        {
            return;
        }

        _isRotating = true;
        var angleAmount = _rotationCameraQueue.Dequeue();

        var tween = GetTree().CreateTween();
        var targetRotationY = Rotation.Y + angleAmount;

        tween.TweenProperty(this, "rotation:y", targetRotationY, 0.15f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);

        tween.Finished += () =>
        {
            _isRotating = false;
            ProcessRotateCameraQueue();
        };
    }

    #endregion

    #region Unit Actions

    private void UpdateSelectionBox(Vector2 mousePos)
    {
        if (SelectionBox == null) return;

        if (!SelectionBox.Visible) SelectionBox.Visible = true;

        var size = (mousePos - _dragStart).Abs();
        var pos = new Vector2(
            Mathf.Min(_dragStart.X, mousePos.X),
            Mathf.Min(_dragStart.Y, mousePos.Y)
        );

        SelectionBox.Position = pos;
        SelectionBox.Size = size;
    }

    private void SelectUnitsInBox(Vector2 start, Vector2 end, bool append)
    {
        if (!append) DeselectAll();

        var selectionRect = new Rect2(start, Vector2.Zero).Expand(end);

        var allUnits = GetTree().GetNodesInGroup("unit").OfType<Unit>();

        foreach (var unit in allUnits)
        {
            if (IsPositionBehind(unit.GlobalPosition)) continue;

            var screenPos = UnprojectPosition(unit.GlobalPosition);

            if (selectionRect.HasPoint(screenPos))
            {
                unit.IsSelected = true;
                _selectedUnits.Add(unit);
            }
        }
    }

    private void SelectUnitAtMousePosition(Vector2 mousePos, bool append)
    {
        var spaceState = GetWorld3D().DirectSpaceState;
        var from = ProjectRayOrigin(mousePos);
        var to = from + ProjectRayNormal(mousePos) * 1000f;

        var query = PhysicsRayQueryParameters3D.Create(from, to);
        var result = spaceState.IntersectRay(query);

        if (!append)
            DeselectAll();

        if (result.Count <= 0) return;

        var clickedObject = result["collider"].As<Node3D>();

        if (clickedObject is not Unit unit) return;

        if (_selectedUnits.Contains(unit))
        {
            unit.IsSelected = false;
            _selectedUnits.Remove(unit);
        }
        else
        {
            unit.IsSelected = true;
            _selectedUnits.Add(unit);
        }
    }

    private void OrderMovementToSelectedUnits(Vector2 mousePos)
    {
        var spaceState = GetWorld3D().DirectSpaceState;
        var from = ProjectRayOrigin(mousePos);
        var to = from + ProjectRayNormal(mousePos) * 1000f;

        var query = PhysicsRayQueryParameters3D.Create(from, to);
        var result = spaceState.IntersectRay(query);

        if (result.Count > 0)
        {
            var targetPosition = (Vector3)result["position"];
            var selectedList = _selectedUnits.Where(IsInstanceValid).ToList();
            var count = selectedList.Count;

            for (var i = 0; i < count; i++)
            {
                var spacing = selectedList[i].NavigationAgent.Radius * 2f;
                var phi = i * Mathf.Pi * (3.0f - Mathf.Sqrt(5.0f));
                var radius = spacing * Mathf.Sqrt(i);

                var offset = new Vector3(
                    Mathf.Cos(phi) * radius,
                    0,
                    Mathf.Sin(phi) * radius
                );
                selectedList[i].SetSpecificTarget(targetPosition + offset);
            }
        }
    }

    private void DeselectAll()
    {
        foreach (var unit in _selectedUnits.Where(IsInstanceValid))
        {
            unit.IsSelected = false;
        }

        _selectedUnits.Clear();
    }

    #endregion
}