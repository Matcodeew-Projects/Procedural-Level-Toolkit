using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class RoomLayerData
{
    [SerializeField]
    private string id;

    [SerializeField]
    private string displayName = "Layer";

    [SerializeField]
    private RoomLayerDefinition definition;

    [SerializeField]
    private bool visible = true;

    [SerializeField]
    private bool locked;

    [SerializeField]
    private List<CellData> cells = new();

    public string Id => id;
    public string DisplayName => displayName;
    public RoomLayerDefinition Definition => definition;
    public bool Visible => visible;
    public bool Locked => locked;
    public IReadOnlyList<CellData> Cells => cells;

    public RoomLayerData()
    {
        EnsureId();
    }

    public RoomLayerData(
        string displayName,
        int width,
        int height)
    {
        EnsureId();

        this.displayName = string.IsNullOrWhiteSpace(displayName)
            ? "Layer"
            : displayName;

        InitializeCells(width, height);
    }

    public void SetDisplayName(string value)
    {
        displayName = string.IsNullOrWhiteSpace(value)
            ? "Layer"
            : value;
    }

    public void SetDefinition(RoomLayerDefinition value)
    {
        definition = value;
    }

    public void SetVisible(bool value)
    {
        visible = value;
    }

    public void SetLocked(bool value)
    {
        locked = value;
    }

    public CellData GetCell(
        int roomWidth,
        int roomHeight,
        Vector2Int position)
    {
        if (!IsInside(roomWidth, roomHeight, position))
            return null;

        int index = ToIndex(
            roomWidth,
            position.x,
            position.y
        );

        if (index < 0 || index >= cells.Count)
            return null;

        return cells[index];
    }

    public void EnsureCellCount(
        int width,
        int height)
    {
        width = Mathf.Max(1, width);
        height = Mathf.Max(1, height);

        int targetCount = width * height;

        while (cells.Count < targetCount)
            cells.Add(new CellData());

        if (cells.Count > targetCount)
        {
            cells.RemoveRange(
                targetCount,
                cells.Count - targetCount
            );
        }
    }

    public void Resize(
        int oldWidth,
        int oldHeight,
        int newWidth,
        int newHeight)
    {
        oldWidth = Mathf.Max(1, oldWidth);
        oldHeight = Mathf.Max(1, oldHeight);
        newWidth = Mathf.Max(1, newWidth);
        newHeight = Mathf.Max(1, newHeight);

        List<CellData> oldCells = cells;
        List<CellData> resized = new(newWidth * newHeight);

        for (int y = 0; y < newHeight; y++)
        {
            for (int x = 0; x < newWidth; x++)
            {
                CellData cell = null;

                if (x < oldWidth && y < oldHeight)
                {
                    int oldIndex = ToIndex(
                        oldWidth,
                        x,
                        y
                    );

                    if (oldIndex >= 0 && oldIndex < oldCells.Count)
                        cell = oldCells[oldIndex];
                }

                resized.Add(cell ?? new CellData());
            }
        }

        cells = resized;
    }

    public RoomLayerData Clone(
        int width,
        int height,
        string newDisplayName)
    {
        RoomLayerData clone = new(
            newDisplayName,
            width,
            height
        )
        {
            definition = definition,
            visible = visible,
            locked = locked
        };

        int count = Mathf.Min(
            cells.Count,
            clone.cells.Count
        );

        for (int i = 0; i < count; i++)
            clone.cells[i] = cells[i]?.Clone() ?? new CellData();

        return clone;
    }

    private void InitializeCells(
        int width,
        int height)
    {
        cells.Clear();

        int count =
            Mathf.Max(1, width) *
            Mathf.Max(1, height);

        for (int i = 0; i < count; i++)
            cells.Add(new CellData());
    }

    private void EnsureId()
    {
        if (string.IsNullOrWhiteSpace(id))
            id = Guid.NewGuid().ToString("N");
    }

    private static int ToIndex(
        int width,
        int x,
        int y)
    {
        return y * width + x;
    }

    private static bool IsInside(
        int width,
        int height,
        Vector2Int position)
    {
        return
            position.x >= 0 &&
            position.y >= 0 &&
            position.x < width &&
            position.y < height;
    }
}
