using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class CellGroupData
{
    [SerializeField] private string id;
    [SerializeField] private string label = "Group";
    [SerializeField] private string layerId;
    [SerializeField] private List<Vector2Int> cells = new();

    [SerializeField]
    private CellGroupGenerationMode generationMode =
        CellGroupGenerationMode.PerCell;

    [SerializeField] private bool enabled = true;
    [SerializeField] private GameObject prefab;
    [SerializeField] private bool fitPrefabToFootprint = true;
    [SerializeField] private Vector3 positionOffset = Vector3.zero;
    [SerializeField] private Vector3 rotation = Vector3.zero;
    [SerializeField] private Vector3 scaleMultiplier = Vector3.one;
    [SerializeField] private Vector2Int anchor;

    public string Id => id;
    public string Label => label;
    public string LayerId => layerId;
    public IReadOnlyList<Vector2Int> Cells => cells;
    public CellGroupGenerationMode GenerationMode => generationMode;
    public bool Enabled => enabled;
    public GameObject Prefab => prefab;
    public bool FitPrefabToFootprint => fitPrefabToFootprint;
    public Vector3 PositionOffset => positionOffset;
    public Vector3 Rotation => rotation;
    public Vector3 ScaleMultiplier => scaleMultiplier;
    public Vector2Int Anchor => anchor;

    public CellGroupData()
    {
        EnsureIntegrity();
    }

    public CellGroupData(
        IEnumerable<Vector2Int> cells,
        string label = "Group")
    {
        EnsureIntegrity();
        SetLabel(label);
        SetCells(cells);
    }

    public void EnsureIntegrity()
    {
        if (string.IsNullOrWhiteSpace(id))
            id = Guid.NewGuid().ToString("N");

        cells ??= new List<Vector2Int>();

        if (string.IsNullOrWhiteSpace(label))
            label = "Group";

        if (scaleMultiplier == Vector3.zero)
            scaleMultiplier = Vector3.one;
    }

    public void SetId(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            id = value.Trim();
    }

    public void SetLabel(string value)
    {
        label = string.IsNullOrWhiteSpace(value)
            ? "Group"
            : value.Trim();
    }

    public void SetLayerId(string value)
    {
        layerId = string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim();
    }

    public void SetCells(IEnumerable<Vector2Int> values)
    {
        cells.Clear();

        if (values == null)
            return;

        HashSet<Vector2Int> unique = new();

        foreach (Vector2Int cell in values)
        {
            if (unique.Add(cell))
                cells.Add(cell);
        }

        if (cells.Count > 0)
            anchor = cells[0];
    }

    public bool Contains(Vector2Int position)
    {
        return cells.Contains(position);
    }

    public void RemoveCellsOutside(int width, int height)
    {
        cells.RemoveAll(
            position =>
                position.x < 0 ||
                position.y < 0 ||
                position.x >= width ||
                position.y >= height
        );

        if (cells.Count > 0 && !cells.Contains(anchor))
            anchor = cells[0];
    }

    public void SetGenerationMode(CellGroupGenerationMode value)
    {
        generationMode = value;
    }

    public void SetEnabled(bool value)
    {
        enabled = value;
    }

    public void SetPrefab(GameObject value)
    {
        prefab = value;
    }

    public void SetFitPrefabToFootprint(bool value)
    {
        fitPrefabToFootprint = value;
    }

    public void SetPositionOffset(Vector3 value)
    {
        positionOffset = value;
    }

    public void SetRotation(Vector3 value)
    {
        rotation = value;
    }

    public void SetScaleMultiplier(Vector3 value)
    {
        scaleMultiplier = new Vector3(
            Mathf.Approximately(value.x, 0f) ? 1f : value.x,
            Mathf.Approximately(value.y, 0f) ? 1f : value.y,
            Mathf.Approximately(value.z, 0f) ? 1f : value.z
        );
    }

    public void SetAnchor(Vector2Int value)
    {
        anchor = value;
    }
}
