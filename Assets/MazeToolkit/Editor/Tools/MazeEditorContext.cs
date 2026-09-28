using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class MazeEditorContext
{
    public int Width { get; private set; }
    public int Height { get; private set; }

    public float CellSize { get; private set; }

    public Cell[,] Cells { get; private set; }

    public CellType CurrentTypeSelected { get; private set; }
        = CellType.Wall;


    public event Action OnGridChanged;
    public event Action OnSettingsChanged;
    public event Action OnBrushChanged;


    public MazeEditorContext(
        int width = 20,
        int height = 20,
        float cellSize = 24f)
    {
        CreateGrid(
            width,
            height,
            cellSize
        );
    }


    // =========================================================
    // Grid
    // =========================================================

    private void CreateGrid(
        int width,
        int height,
        float cellSize)
    {
        Width = Mathf.Max(1, width);
        Height = Mathf.Max(1, height);
        CellSize = Mathf.Max(1f, cellSize);

        Cells = new Cell[Width, Height];

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                Cells[x, y] = new Cell();
            }
        }
    }


    public void Resize(
        int width,
        int height,
        float cellSize,
        bool preserveCells = true)
    {
        width = Mathf.Max(1, width);
        height = Mathf.Max(1, height);
        cellSize = Mathf.Max(1f, cellSize);

        Cell[,] previousCells = Cells;

        int previousWidth = Width;
        int previousHeight = Height;

        Cell[,] newCells =
            new Cell[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                CellType initialType =
                    CellType.Empty;

                if (
                    preserveCells &&
                    previousCells != null &&
                    x < previousWidth &&
                    y < previousHeight)
                {
                    initialType =
                        previousCells[x, y].Type;
                }

                newCells[x, y] =
                    new Cell(initialType);
            }
        }

        Width = width;
        Height = height;
        CellSize = cellSize;

        Cells = newCells;

        OnSettingsChanged?.Invoke();
        OnGridChanged?.Invoke();
    }


    // =========================================================
    // Cells
    // =========================================================

    public bool IsInside(Vector2Int position)
    {
        return
            position.x >= 0 &&
            position.y >= 0 &&
            position.x < Width &&
            position.y < Height;
    }


    public Cell GetCell(Vector2Int position)
    {
        if (!IsInside(position))
            return null;

        return Cells[
            position.x,
            position.y
        ];
    }


    public Cell GetCell(int x, int y)
    {
        return GetCell(
            new Vector2Int(x, y)
        );
    }


    public bool SetCellType(
        Vector2Int position,
        CellType type)
    {
        if (!IsInside(position))
            return false;

        Cell cell =
            Cells[
                position.x,
                position.y
            ];

        if (cell.Type == type)
            return false;

        cell.SetType(type);

        OnGridChanged?.Invoke();

        return true;
    }


    public void SetCellTypes(
        IEnumerable<Vector2Int> positions,
        CellType type)
    {
        bool changed = false;

        foreach (Vector2Int position in positions)
        {
            if (!IsInside(position))
                continue;

            Cell cell =
                Cells[
                    position.x,
                    position.y
                ];

            if (cell.Type == type)
                continue;

            cell.SetType(type);

            changed = true;
        }

        /*
         * Une seule notification,
         * même si 200 cellules sont modifiées.
         */
        if (changed)
            OnGridChanged?.Invoke();
    }


    public void Clear(
        CellType type = CellType.Empty)
    {
        bool changed = false;

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (Cells[x, y].Type == type)
                    continue;

                Cells[x, y].SetType(type);

                changed = true;
            }
        }

        if (changed)
            OnGridChanged?.Invoke();
    }


    // =========================================================
    // Brush
    // =========================================================

    public void SetCurrentType(
        CellType type)
    {
        if (CurrentTypeSelected == type)
            return;

        CurrentTypeSelected = type;

        OnBrushChanged?.Invoke();
    }
}