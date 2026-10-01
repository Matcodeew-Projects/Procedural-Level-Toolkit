using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class RoomEditorInputHandler
{
    private readonly RoomEditorContext context;
    private GridCanvasUI grid;

    private bool pointerPressed;
    private Vector2Int? lastCell;

    public event Action<Vector2Int> SelectionRequested;
    public event Action<IReadOnlyList<Vector2Int>, CellTypeDefinition> PaintRequested;
    public event Action<IReadOnlyList<Vector2Int>> EraseRequested;
    public event Action<Vector2Int, CellTypeDefinition> FillRequested;
    public event Action<IReadOnlyList<Vector2Int>> GroupRequested;
    public event Action<Vector2Int> SocketRequested;

    public RoomEditorInputHandler(RoomEditorContext context)
    {
        this.context = context;
    }

    public void Bind(GridCanvasUI gridCanvas)
    {
        Unbind();

        grid = gridCanvas;
        grid.PointerDown += OnPointerDown;
        grid.PointerDrag += OnPointerDrag;
        grid.PointerUp += OnPointerUp;
    }

    public void Unbind()
    {
        if (grid == null)
            return;

        grid.PointerDown -= OnPointerDown;
        grid.PointerDrag -= OnPointerDrag;
        grid.PointerUp -= OnPointerUp;
        grid = null;
    }

    private void OnPointerDown(Vector2Int cell)
    {
        pointerPressed = true;
        lastCell = cell;
        ApplyTool(cell);
    }

    private void OnPointerDrag(Vector2Int cell)
    {
        if (!pointerPressed || !context.ContinuousPaint)
            return;

        if (context.CurrentTool is RoomEditorTool.Select or
            RoomEditorTool.Fill or
            RoomEditorTool.Socket)
        {
            return;
        }

        if (lastCell.HasValue)
        {
            foreach (Vector2Int lineCell in RasterizeLine(lastCell.Value, cell))
                ApplyTool(lineCell);
        }
        else
        {
            ApplyTool(cell);
        }

        lastCell = cell;
    }

    private void OnPointerUp(Vector2Int cell)
    {
        pointerPressed = false;
        lastCell = null;
    }

    private void ApplyTool(Vector2Int cell)
    {
        switch (context.CurrentTool)
        {
            case RoomEditorTool.Select:
                SelectionRequested?.Invoke(cell);
                break;

            case RoomEditorTool.Paint:
                PaintRequested?.Invoke(
                    GetBrushCells(cell),
                    context.CurrentCellType
                );
                break;

            case RoomEditorTool.Erase:
                EraseRequested?.Invoke(GetBrushCells(cell));
                break;

            case RoomEditorTool.Fill:
                FillRequested?.Invoke(cell, context.CurrentCellType);
                break;

            case RoomEditorTool.Group:
                GroupRequested?.Invoke(GetBrushCells(cell));
                break;

            case RoomEditorTool.Socket:
                SocketRequested?.Invoke(cell);
                break;
        }
    }

    private IReadOnlyList<Vector2Int> GetBrushCells(Vector2Int center)
    {
        List<Vector2Int> cells = new();

        int size = context.BrushSize;
        int offset = (size - 1) / 2;
        int startX = center.x - offset;
        int startY = center.y - offset;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Vector2Int position = new(
                    startX + x,
                    startY + y
                );

                if (context.IsInside(position))
                    cells.Add(position);
            }
        }

        return cells;
    }

    private IEnumerable<Vector2Int> RasterizeLine(
        Vector2Int start,
        Vector2Int end)
    {
        int x0 = start.x;
        int y0 = start.y;
        int x1 = end.x;
        int y1 = end.y;

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int error = dx - dy;

        while (true)
        {
            yield return new Vector2Int(x0, y0);

            if (x0 == x1 && y0 == y1)
                yield break;

            int doubleError = error * 2;

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
}
