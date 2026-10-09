using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class RoomEditorInputHandler
{
    private readonly RoomEditorContext
        context;


    private GridCanvasUI grid;


    private bool pointerPressed;


    private Vector2Int?
        lastCell;


    private Vector2Int?
        selectionAnchor;


    private readonly HashSet<Vector2Int>
        groupCells =
            new();


    public event Action StrokeStarted;
    public event Action StrokeEnded;


    public event Action<
        IReadOnlyList<Vector2Int>
    >
        SelectionRequested;


    public event Action<
        IReadOnlyList<Vector2Int>,
        CellTypeDefinition
    >
        PaintRequested;


    public event Action<
        IReadOnlyList<Vector2Int>
    >
        EraseRequested;


    public event Action<
        Vector2Int,
        CellTypeDefinition
    >
        FillRequested;


    public event Action<
        IReadOnlyList<Vector2Int>
    >
        GroupRequested;


    public event Action<Vector2Int>
        SocketRequested;


    public RoomEditorInputHandler(
        RoomEditorContext context)
    {
        this.context =
            context
            ??
            throw new ArgumentNullException(
                nameof(context)
            );
    }


    // =========================================================
    // Bind
    // =========================================================

    public void Bind(
        GridCanvasUI gridCanvas)
    {
        Unbind();


        grid =
            gridCanvas
            ??
            throw new ArgumentNullException(
                nameof(gridCanvas)
            );


        grid.PointerDown +=
            OnPointerDown;

        grid.PointerDrag +=
            OnPointerDrag;

        grid.PointerUp +=
            OnPointerUp;
    }


    public void Unbind()
    {
        if (grid != null)
        {
            grid.PointerDown -=
                OnPointerDown;

            grid.PointerDrag -=
                OnPointerDrag;

            grid.PointerUp -=
                OnPointerUp;
        }


        grid =
            null;


        ResetPointerState();
    }


    // =========================================================
    // Down
    // =========================================================

    private void OnPointerDown(
        Vector2Int cell)
    {
        if (
            context.CurrentRoom ==
            null)
        {
            return;
        }


        pointerPressed =
            true;


        lastCell =
            cell;


        StrokeStarted?.Invoke();


        switch (
            context.CurrentTool)
        {
            case RoomEditorTool.Select:

                selectionAnchor =
                    cell;


                SelectionRequested?.Invoke(
                    GetRectangleCells(
                        cell,
                        cell
                    )
                );

                break;


            case RoomEditorTool.Group:

                groupCells.Clear();


                AddBrushCellsToGroup(
                    cell
                );


                SelectionRequested?.Invoke(
                    new List<Vector2Int>(
                        groupCells
                    )
                );

                break;


            default:

                ApplyTool(
                    cell
                );

                break;
        }
    }


    // =========================================================
    // Drag
    // =========================================================

    private void OnPointerDrag(
        Vector2Int cell)
    {
        if (!pointerPressed)
            return;


        switch (
            context.CurrentTool)
        {
            case RoomEditorTool.Select:

                HandleSelectionDrag(
                    cell
                );

                return;


            case RoomEditorTool.Group:

                HandleGroupDrag(
                    cell
                );

                return;
        }


        if (
            !context.ContinuousPaint)
        {
            return;
        }


        if (
            context.CurrentTool ==
            RoomEditorTool.Fill ||
            context.CurrentTool ==
            RoomEditorTool.Socket)
        {
            return;
        }


        if (!lastCell.HasValue)
        {
            ApplyTool(
                cell
            );


            lastCell =
                cell;


            return;
        }


        foreach (
            Vector2Int position
            in RasterizeLine(
                lastCell.Value,
                cell
            ))
        {
            ApplyTool(
                position
            );
        }


        lastCell =
            cell;
    }


    // =========================================================
    // Up
    // =========================================================

    private void OnPointerUp(
        Vector2Int cell)
    {
        if (!pointerPressed)
            return;


        if (
            context.CurrentTool ==
            RoomEditorTool.Select)
        {
            if (
                selectionAnchor
                    .HasValue)
            {
                SelectionRequested?.Invoke(
                    GetRectangleCells(
                        selectionAnchor.Value,
                        cell
                    )
                );
            }
        }


        if (
            context.CurrentTool ==
            RoomEditorTool.Group)
        {
            if (
                groupCells.Count >
                0)
            {
                GroupRequested?.Invoke(
                    new List<Vector2Int>(
                        groupCells
                    )
                );
            }
        }


        StrokeEnded?.Invoke();


        ResetPointerState();
    }


    // =========================================================
    // Select
    // =========================================================

    private void HandleSelectionDrag(
        Vector2Int current)
    {
        if (
            !selectionAnchor
                .HasValue)
        {
            return;
        }


        SelectionRequested?.Invoke(
            GetRectangleCells(
                selectionAnchor.Value,
                current
            )
        );
    }


    private IReadOnlyList<Vector2Int>
        GetRectangleCells(
            Vector2Int start,
            Vector2Int end)
    {
        int minX =
            Mathf.Min(
                start.x,
                end.x
            );


        int maxX =
            Mathf.Max(
                start.x,
                end.x
            );


        int minY =
            Mathf.Min(
                start.y,
                end.y
            );


        int maxY =
            Mathf.Max(
                start.y,
                end.y
            );


        List<Vector2Int> cells =
            new();


        for (
            int y = minY;
            y <= maxY;
            y++)
        {
            for (
                int x = minX;
                x <= maxX;
                x++)
            {
                Vector2Int cell =
                    new(
                        x,
                        y
                    );


                if (
                    context.IsInside(
                        cell
                    ))
                {
                    cells.Add(
                        cell
                    );
                }
            }
        }


        return cells;
    }


    // =========================================================
    // Group
    // =========================================================

    private void HandleGroupDrag(
        Vector2Int current)
    {
        if (
            lastCell.HasValue)
        {
            foreach (
                Vector2Int lineCell
                in RasterizeLine(
                    lastCell.Value,
                    current
                ))
            {
                AddBrushCellsToGroup(
                    lineCell
                );
            }
        }
        else
        {
            AddBrushCellsToGroup(
                current
            );
        }


        lastCell =
            current;


        SelectionRequested?.Invoke(
            new List<Vector2Int>(
                groupCells
            )
        );
    }


    private void AddBrushCellsToGroup(
        Vector2Int center)
    {
        foreach (
            Vector2Int cell
            in GetBrushCells(
                center
            ))
        {
            groupCells.Add(
                cell
            );
        }
    }


    // =========================================================
    // Tools
    // =========================================================

    private void ApplyTool(
        Vector2Int cell)
    {
        if (
            !context.IsInside(
                cell
            ))
        {
            return;
        }


        switch (
            context.CurrentTool)
        {
            case RoomEditorTool.Paint:

                PaintRequested?.Invoke(
                    GetBrushCells(
                        cell
                    ),
                    context.CurrentCellType
                );

                break;


            case RoomEditorTool.Erase:

                EraseRequested?.Invoke(
                    GetBrushCells(
                        cell
                    )
                );

                break;


            case RoomEditorTool.Fill:

                FillRequested?.Invoke(
                    cell,
                    context.CurrentCellType
                );

                break;


            case RoomEditorTool.Socket:

                SocketRequested?.Invoke(
                    cell
                );

                break;
        }
    }


    // =========================================================
    // Brush
    // =========================================================

    private IReadOnlyList<Vector2Int>
        GetBrushCells(
            Vector2Int center)
    {
        List<Vector2Int> cells =
            new();


        int size =
            Mathf.Clamp(
                context.BrushSize,
                1,
                16
            );


        int half =
            size / 2;


        int startX =
            center.x -
            half;


        int startY =
            center.y -
            half;


        if (
            size % 2 ==
            0)
        {
            startX++;

            startY++;
        }


        for (
            int y = 0;
            y < size;
            y++)
        {
            for (
                int x = 0;
                x < size;
                x++)
            {
                Vector2Int position =
                    new(
                        startX + x,
                        startY + y
                    );


                if (
                    context.IsInside(
                        position
                    ))
                {
                    cells.Add(
                        position
                    );
                }
            }
        }


        return cells;
    }


    // =========================================================
    // Bresenham
    // =========================================================

    private static IEnumerable<Vector2Int>
        RasterizeLine(
            Vector2Int start,
            Vector2Int end)
    {
        int x0 =
            start.x;

        int y0 =
            start.y;

        int x1 =
            end.x;

        int y1 =
            end.y;


        int dx =
            Mathf.Abs(
                x1 - x0
            );


        int dy =
            Mathf.Abs(
                y1 - y0
            );


        int sx =
            x0 < x1
                ? 1
                : -1;


        int sy =
            y0 < y1
                ? 1
                : -1;


        int error =
            dx - dy;


        while (true)
        {
            yield return
                new Vector2Int(
                    x0,
                    y0
                );


            if (
                x0 == x1 &&
                y0 == y1)
            {
                yield break;
            }


            int doubleError =
                error * 2;


            if (
                doubleError >
                -dy)
            {
                error -= dy;

                x0 += sx;
            }


            if (
                doubleError <
                dx)
            {
                error += dx;

                y0 += sy;
            }
        }
    }


    // =========================================================
    // Reset
    // =========================================================

    private void ResetPointerState()
    {
        pointerPressed =
            false;


        lastCell =
            null;


        selectionAnchor =
            null;


        groupCells.Clear();
    }
}