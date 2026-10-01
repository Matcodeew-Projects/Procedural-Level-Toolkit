using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class CellGroupData
{
    [SerializeField]
    private string id;

    [SerializeField]
    private string label = "Group";

    [SerializeField]
    private List<Vector2Int> cells = new();

    [SerializeField]
    private CellGroupGenerationMode generationMode =
        CellGroupGenerationMode.PerCell;

    [SerializeField]
    private GameObject prefab;

    [SerializeField]
    private Vector2Int anchor;

    [SerializeField]
    private Vector3 rotation;


    public string Id => id;

    public string Label => label;

    public IReadOnlyList<Vector2Int> Cells =>
        cells;

    public CellGroupGenerationMode GenerationMode =>
        generationMode;

    public GameObject Prefab =>
        prefab;

    public Vector2Int Anchor =>
        anchor;

    public Vector3 Rotation =>
        rotation;


    public CellGroupData()
    {
        EnsureIntegrity();
    }


    public CellGroupData(
        IEnumerable<Vector2Int> cells,
        string label = "Group")
    {
        EnsureIntegrity();

        SetLabel(
            label
        );

        SetCells(
            cells
        );
    }


    public void EnsureIntegrity()
    {
        if (
            string.IsNullOrWhiteSpace(
                id
            ))
        {
            id =
                Guid.NewGuid()
                    .ToString("N");
        }


        cells ??=
            new List<Vector2Int>();


        if (
            string.IsNullOrWhiteSpace(
                label
            ))
        {
            label =
                "Group";
        }
    }


    public void SetId(
        string value)
    {
        if (
            string.IsNullOrWhiteSpace(
                value
            ))
        {
            return;
        }


        id =
            value.Trim();
    }


    public void SetLabel(
        string value)
    {
        label =
            string.IsNullOrWhiteSpace(
                value
            )
                ? "Group"
                : value.Trim();
    }


    public void SetCells(
        IEnumerable<Vector2Int> values)
    {
        cells.Clear();


        if (values == null)
            return;


        HashSet<Vector2Int> unique =
            new();


        foreach (
            Vector2Int cell
            in values)
        {
            if (
                unique.Add(
                    cell
                ))
            {
                cells.Add(
                    cell
                );
            }
        }


        if (
            cells.Count >
            0)
        {
            anchor =
                cells[0];
        }
    }


    public bool Contains(
        Vector2Int position)
    {
        return
            cells.Contains(
                position
            );
    }


    public void RemoveCellsOutside(
        int width,
        int height)
    {
        cells.RemoveAll(
            position =>
                position.x < 0 ||
                position.y < 0 ||
                position.x >= width ||
                position.y >= height
        );


        if (
            cells.Count >
            0 &&
            !cells.Contains(
                anchor
            ))
        {
            anchor =
                cells[0];
        }
    }


    public void SetGenerationMode(
        CellGroupGenerationMode value)
    {
        generationMode =
            value;
    }


    public void SetPrefab(
        GameObject value)
    {
        prefab =
            value;
    }


    public void SetAnchor(
        Vector2Int value)
    {
        anchor =
            value;
    }


    public void SetRotation(
        Vector3 value)
    {
        rotation =
            value;
    }
}