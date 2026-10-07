using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class RoomEditorContext
{
    public const float BaseCellPixelSize =
        24f;


    private readonly HashSet<Vector2Int>
        selectedCells =
            new();


    public RoomDefinition CurrentRoom
    {
        get;
        private set;
    }


    public RoomLayerData ActiveLayer
    {
        get;
        private set;
    }


    public CellTypeDefinition CurrentCellType
    {
        get;
        private set;
    }


    public RoomEditorTool CurrentTool
    {
        get;
        private set;
    } = RoomEditorTool.Select;


    public int BrushSize
    {
        get;
        private set;
    } = 1;


    public bool ContinuousPaint
    {
        get;
        private set;
    } = true;


    public int Width
    {
        get;
        private set;
    } = 20;


    public int Height
    {
        get;
        private set;
    } = 20;


    public float CellSize =>
        BaseCellPixelSize;


    public float Zoom
    {
        get;
        private set;
    } = 1f;


    public IReadOnlyCollection<Vector2Int>
        SelectedCells =>
            selectedCells;


    public int SelectionCount =>
        selectedCells.Count;


    public Vector2Int? SelectedCell
    {
        get
        {
            if (
                selectedCells.Count !=
                1)
            {
                return null;
            }


            foreach (
                Vector2Int cell
                in selectedCells)
            {
                return cell;
            }


            return null;
        }
    }


    public CellGroupData SelectedGroup
    {
        get;
        private set;
    }


    public RoomSocketData SelectedSocket
    {
        get;
        private set;
    }


    public event Action RoomChanged;
    public event Action ActiveLayerChanged;
    public event Action ToolChanged;
    public event Action BrushChanged;
    public event Action CellTypeChanged;
    public event Action GridMetricsChanged;
    public event Action ZoomChanged;
    public event Action SelectionChanged;


    // =========================================================
    // Room
    // =========================================================

    public void SetRoom(
        RoomDefinition room)
    {
        CurrentRoom =
            room;


        ClearSelectionInternal();


        if (room != null)
        {
            SetGridMetrics(
                room.Width,
                room.Height
            );
        }


        RoomChanged?.Invoke();

        SelectionChanged?.Invoke();
    }


    // =========================================================
    // Layer
    // =========================================================

    public void SetActiveLayer(
        RoomLayerData layer)
    {
        if (
            ReferenceEquals(
                ActiveLayer,
                layer
            ))
        {
            return;
        }


        ActiveLayer =
            layer;


        ClearSelectionInternal();


        ActiveLayerChanged?.Invoke();

        SelectionChanged?.Invoke();
    }


    // =========================================================
    // Tool
    // =========================================================

    public void SetTool(
        RoomEditorTool tool)
    {
        if (
            CurrentTool ==
            tool)
        {
            return;
        }


        CurrentTool =
            tool;


        ToolChanged?.Invoke();
    }


    // =========================================================
    // Cell Type
    // =========================================================

    public void SetCurrentCellType(
        CellTypeDefinition type)
    {
        if (
            ReferenceEquals(
                CurrentCellType,
                type
            ))
        {
            return;
        }


        CurrentCellType =
            type;


        CellTypeChanged?.Invoke();
    }


    // =========================================================
    // Brush
    // =========================================================

    public void SetBrushSize(
        int value)
    {
        value =
            Mathf.Clamp(
                value,
                1,
                16
            );


        if (
            BrushSize ==
            value)
        {
            return;
        }


        BrushSize =
            value;


        BrushChanged?.Invoke();
    }


    public void SetContinuousPaint(
        bool value)
    {
        if (
            ContinuousPaint ==
            value)
        {
            return;
        }


        ContinuousPaint =
            value;


        BrushChanged?.Invoke();
    }


    // =========================================================
    // Grid
    // =========================================================

    public void SetGridMetrics(
        int width,
        int height)
    {
        width =
            Mathf.Max(
                1,
                width
            );


        height =
            Mathf.Max(
                1,
                height
            );


        bool changed =
            Width != width ||
            Height != height;


        Width =
            width;

        Height =
            height;


        selectedCells.RemoveWhere(
            cell =>
                !IsInside(
                    cell
                )
        );


        if (changed)
        {
            GridMetricsChanged?.Invoke();

            SelectionChanged?.Invoke();
        }
    }


    // =========================================================
    // Zoom
    // =========================================================

    public void SetZoom(
        float value)
    {
        value =
            Mathf.Clamp(
                value,
                0.25f,
                4f
            );


        if (
            Mathf.Approximately(
                Zoom,
                value
            ))
        {
            return;
        }


        Zoom =
            value;


        ZoomChanged?.Invoke();
    }


    // =========================================================
    // Selection
    // =========================================================

    public void SetSelectedCell(
        Vector2Int? cell)
    {
        ClearSelectionInternal();


        if (
            cell.HasValue &&
            IsInside(
                cell.Value
            ))
        {
            selectedCells.Add(
                cell.Value
            );
        }


        SelectionChanged?.Invoke();
    }


    public void SetSelectedCells(
        IEnumerable<Vector2Int> cells)
    {
        ClearSelectionInternal();


        if (cells != null)
        {
            foreach (
                Vector2Int cell
                in cells)
            {
                if (
                    IsInside(
                        cell
                    ))
                {
                    selectedCells.Add(
                        cell
                    );
                }
            }
        }


        SelectionChanged?.Invoke();
    }


    public void SetSelectedGroup(
        CellGroupData group)
    {
        ClearSelectionInternal();


        SelectedGroup =
            group;


        if (group != null)
        {
            foreach (
                Vector2Int cell
                in group.Cells)
            {
                if (
                    IsInside(
                        cell
                    ))
                {
                    selectedCells.Add(
                        cell
                    );
                }
            }
        }


        SelectionChanged?.Invoke();
    }


    public void SetSelectedSocket(
        RoomSocketData socket)
    {
        ClearSelectionInternal();


        SelectedSocket =
            socket;


        if (
            socket != null &&
            IsInside(
                socket.Position
            ))
        {
            selectedCells.Add(
                socket.Position
            );
        }


        SelectionChanged?.Invoke();
    }


    public void ClearSelection()
    {
        ClearSelectionInternal();

        SelectionChanged?.Invoke();
    }


    private void ClearSelectionInternal()
    {
        selectedCells.Clear();

        SelectedGroup =
            null;

        SelectedSocket =
            null;
    }


    // =========================================================
    // Bounds
    // =========================================================

    public bool IsInside(
        Vector2Int position)
    {
        return
            position.x >= 0 &&
            position.y >= 0 &&
            position.x < Width &&
            position.y < Height;
    }
}