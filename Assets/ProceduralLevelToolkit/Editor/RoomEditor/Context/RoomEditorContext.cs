using System;
using UnityEngine;

public sealed class RoomEditorContext
{
    public RoomDefinition CurrentRoom { get; private set; }
    public RoomLayerData ActiveLayer { get; private set; }
    public CellTypeDefinition CurrentCellType { get; private set; }

    public RoomEditorTool CurrentTool { get; private set; }
        = RoomEditorTool.Select;

    public int BrushSize { get; private set; } = 1;
    public bool ContinuousPaint { get; private set; } = true;

    // Valeurs d'affichage provisoires tant que l'adapter RoomDefinition
    // n'est pas branché. Elles ne dupliquent pas les CellData.
    public int Width { get; private set; } = 20;
    public int Height { get; private set; } = 20;
    public float CellSize { get; private set; } = 24f;

    public float Zoom { get; private set; } = 1f;

    public event Action RoomChanged;
    public event Action ActiveLayerChanged;
    public event Action ToolChanged;
    public event Action BrushChanged;
    public event Action CellTypeChanged;
    public event Action GridMetricsChanged;
    public event Action ZoomChanged;

    public void SetRoom(RoomDefinition room)
    {
        if (ReferenceEquals(CurrentRoom, room))
            return;

        CurrentRoom = room;
        RoomChanged?.Invoke();
    }

    public void SetActiveLayer(RoomLayerData layer)
    {
        if (ReferenceEquals(ActiveLayer, layer))
            return;

        ActiveLayer = layer;
        ActiveLayerChanged?.Invoke();
    }

    public void SetTool(RoomEditorTool tool)
    {
        if (CurrentTool == tool)
            return;

        CurrentTool = tool;
        ToolChanged?.Invoke();
    }

    public void SetCurrentCellType(CellTypeDefinition type)
    {
        if (ReferenceEquals(CurrentCellType, type))
            return;

        CurrentCellType = type;
        CellTypeChanged?.Invoke();
    }

    public void SetBrushSize(int value)
    {
        value = Mathf.Clamp(value, 1, 16);

        if (BrushSize == value)
            return;

        BrushSize = value;
        BrushChanged?.Invoke();
    }

    public void SetContinuousPaint(bool enabled)
    {
        if (ContinuousPaint == enabled)
            return;

        ContinuousPaint = enabled;
        BrushChanged?.Invoke();
    }

    public void SetGridMetrics(int width, int height, float cellSize)
    {
        width = Mathf.Max(1, width);
        height = Mathf.Max(1, height);
        cellSize = Mathf.Max(1f, cellSize);

        if (Width == width && Height == height &&
            Mathf.Approximately(CellSize, cellSize))
        {
            return;
        }

        Width = width;
        Height = height;
        CellSize = cellSize;
        GridMetricsChanged?.Invoke();
    }

    public void SetZoom(float value)
    {
        value = Mathf.Clamp(value, 0.25f, 4f);

        if (Mathf.Approximately(Zoom, value))
            return;

        Zoom = value;
        ZoomChanged?.Invoke();
    }

    public bool IsInside(Vector2Int cell)
    {
        return cell.x >= 0 &&
               cell.y >= 0 &&
               cell.x < Width &&
               cell.y < Height;
    }
}
