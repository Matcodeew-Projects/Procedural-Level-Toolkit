using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "LevelDefinition",
    menuName =
        "Procedural Level Toolkit/Level Definition"
)]
public sealed class LevelDefinition
    : ScriptableObject
{
    [SerializeField]
    private string id;

    [SerializeField]
    private string levelName =
        "New Level";

    [SerializeField]
    [TextArea(2, 6)]
    private string description;

    [SerializeField]
    private LevelGraphData graph =
        new LevelGraphData();

    [SerializeField]
    private SpatialLayoutData spatialLayout =
        new SpatialLayoutData();


    public string Id =>
        id;

    public string LevelName =>
        levelName;

    public string Description =>
        description;

    public LevelGraphData Graph =>
        graph;

    public SpatialLayoutData SpatialLayout =>
        spatialLayout;


    private void OnEnable()
    {
        EnsureIntegrity();
    }


    private void OnValidate()
    {
        EnsureIntegrity();
    }


    public void EnsureIntegrity()
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            id =
                Guid.NewGuid()
                    .ToString("N");
        }


        if (graph == null)
        {
            graph =
                new LevelGraphData();
        }


        if (spatialLayout == null)
        {
            spatialLayout =
                new SpatialLayoutData();
        }


        graph.EnsureIntegrity();
        spatialLayout.EnsureIntegrity();
    }


    public void SetLevelName(
        string value
    )
    {
        levelName =
            string.IsNullOrWhiteSpace(value)
                ? "New Level"
                : value;
    }


    public void SetDescription(
        string value
    )
    {
        description =
            value ?? string.Empty;
    }


    public void InvalidateSpatialLayout()
    {
        spatialLayout.Clear();
    }
}