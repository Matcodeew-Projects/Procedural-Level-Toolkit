using System.Collections.Generic;
using UnityEngine;

public enum MazeEditorTool
{
    Brush,
    Eraser,
    Fill
}

public sealed class InputHandler
{
    private readonly MazeEditorContext context;

    private GridPreviewUI preview;


    public MazeEditorTool CurrentTool { get; private set; }
        = MazeEditorTool.Brush;

    public int BrushSize { get; private set; }
        = 1;

    public bool PaintOnDrag { get; private set; }
        = true;


    private bool pointerPressed;

    private Vector2Int? lastCell;


    public InputHandler(
        MazeEditorContext context)
    {
        this.context = context;
    }


    // =========================================================
    // Bind
    // =========================================================

    public void Bind(
        GridPreviewUI preview)
    {
        Unbind();

        this.preview = preview;

        preview.PointerDown += OnPointerDown;
        preview.PointerDrag += OnPointerDrag;
        preview.PointerUp += OnPointerUp;
    }


    public void Unbind()
    {
        if (preview == null)
            return;

        preview.PointerDown -= OnPointerDown;
        preview.PointerDrag -= OnPointerDrag;
        preview.PointerUp -= OnPointerUp;

        preview = null;
    }


    // =========================================================
    // Settings
    // =========================================================

    public void SetTool(
        MazeEditorTool tool)
    {
        CurrentTool = tool;
    }


    public void SetBrushSize(
        int size)
    {
        BrushSize =
            Mathf.Clamp(
                size,
                1,
                16
            );
    }


    public void SetPaintOnDrag(
        bool enabled)
    {
        PaintOnDrag = enabled;
    }


    // =========================================================
    // Pointer
    // =========================================================

    private void OnPointerDown(
        Vector2 mousePosition)
    {
        if (!TryMouseToCell(
            mousePosition,
            out Vector2Int cell))
        {
            return;
        }

        pointerPressed = true;
        lastCell = cell;

        ApplyTool(cell);
    }


    private void OnPointerDrag(
        Vector2 mousePosition)
    {
        if (!pointerPressed)
            return;

        if (!PaintOnDrag)
            return;

        if (CurrentTool == MazeEditorTool.Fill)
            return;

        if (!TryMouseToCell(
            mousePosition,
            out Vector2Int cell))
        {
            return;
        }

        if (lastCell.HasValue)
        {
            PaintLine(
                lastCell.Value,
                cell
            );
        }
        else
        {
            ApplyTool(cell);
        }

        lastCell = cell;
    }


    private void OnPointerUp(
        Vector2 mousePosition)
    {
        pointerPressed = false;

        lastCell = null;
    }


    // =========================================================
    // Conversion
    // =========================================================

    private bool TryMouseToCell(
        Vector2 mousePosition,
        out Vector2Int cellPosition)
    {
        cellPosition =
            new Vector2Int(
                Mathf.FloorToInt(
                    mousePosition.x /
                    context.CellSize
                ),
                Mathf.FloorToInt(
                    mousePosition.y /
                    context.CellSize
                )
            );

        return context.IsInside(
            cellPosition
        );
    }


    // =========================================================
    // Tool
    // =========================================================

    private void ApplyTool(
        Vector2Int cellPosition)
    {
        switch (CurrentTool)
        {
            case MazeEditorTool.Brush:

                PaintBrush(
                    cellPosition,
                    context.CurrentTypeSelected
                );

                break;


            case MazeEditorTool.Eraser:

                PaintBrush(
                    cellPosition,
                    CellType.Empty
                );

                break;


            case MazeEditorTool.Fill:

                FloodFill(
                    cellPosition,
                    context.CurrentTypeSelected
                );

                break;
        }
    }


    // =========================================================
    // Brush
    // =========================================================

    private void PaintBrush(
        Vector2Int center,
        CellType type)
    {
        List<Vector2Int> positions =
            new();

        int offset =
            (BrushSize - 1) / 2;

        int startX =
            center.x - offset;

        int startY =
            center.y - offset;

        for (
            int x = 0;
            x < BrushSize;
            x++)
        {
            for (
                int y = 0;
                y < BrushSize;
                y++)
            {
                Vector2Int position =
                    new Vector2Int(
                        startX + x,
                        startY + y
                    );

                if (!context.IsInside(position))
                    continue;

                positions.Add(position);
            }
        }

        context.SetCellTypes(
            positions,
            type
        );
    }


    // =========================================================
    // Drag interpolation
    // =========================================================

    private void PaintLine(
        Vector2Int from,
        Vector2Int to)
    {
        foreach (
            Vector2Int position
            in RasterizeLine(from, to))
        {
            ApplyTool(position);
        }
    }


    private IEnumerable<Vector2Int>
        RasterizeLine(
            Vector2Int start,
            Vector2Int end)
    {
        int x0 = start.x;
        int y0 = start.y;

        int x1 = end.x;
        int y1 = end.y;

        int dx =
            Mathf.Abs(x1 - x0);

        int dy =
            Mathf.Abs(y1 - y0);

        int sx =
            x0 < x1 ? 1 : -1;

        int sy =
            y0 < y1 ? 1 : -1;

        int error =
            dx - dy;

        while (true)
        {
            yield return
                new Vector2Int(x0, y0);

            if (
                x0 == x1 &&
                y0 == y1)
            {
                break;
            }

            int doubleError =
                error * 2;

            if (doubleError > -dy)
            {
                error -= dy;
                x0 += sx;
            }

            if (doubleError < dx)
            {
                error += dx;
                y0 += sy;
            }
        }
    }


    // =========================================================
    // Fill
    // =========================================================

    private void FloodFill(
        Vector2Int origin,
        CellType replacement)
    {
        Cell originCell =
            context.GetCell(origin);

        if (originCell == null)
            return;

        CellType target =
            originCell.Type;

        if (target == replacement)
            return;


        bool[,] visited =
            new bool[
                context.Width,
                context.Height
            ];

        Queue<Vector2Int> queue =
            new();

        List<Vector2Int> cells =
            new();


        queue.Enqueue(origin);

        visited[
            origin.x,
            origin.y
        ] = true;


        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };


        while (queue.Count > 0)
        {
            Vector2Int current =
                queue.Dequeue();

            Cell cell =
                context.GetCell(current);

            if (
                cell == null ||
                cell.Type != target)
            {
                continue;
            }

            cells.Add(current);


            foreach (
                Vector2Int direction
                in directions)
            {
                Vector2Int neighbour =
                    current + direction;

                if (!context.IsInside(neighbour))
                    continue;

                if (
                    visited[
                        neighbour.x,
                        neighbour.y
                    ])
                {
                    continue;
                }

                visited[
                    neighbour.x,
                    neighbour.y
                ] = true;

                if (
                    context
                        .GetCell(neighbour)
                        .Type == target)
                {
                    queue.Enqueue(neighbour);
                }
            }
        }

        context.SetCellTypes(
            cells,
            replacement
        );
    }
}