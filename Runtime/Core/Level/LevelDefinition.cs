using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "LevelDefinition",
    menuName = "Procedural Level Toolkit/Level Definition"
)]
public sealed class LevelDefinition
    : ScriptableObject
{
    // =========================================================
    // Identity
    // =========================================================

    [SerializeField]
    private string id;

    [SerializeField]
    private string levelName =
        "New Level";

    [SerializeField]
    [TextArea(2, 6)]
    private string description;


    // =========================================================
    // Persistent Level Data
    // =========================================================

    [SerializeField]
    private LevelGraphData graph =
        new LevelGraphData();

    [SerializeField]
    private SpatialLayoutData spatialLayout =
        new SpatialLayoutData();

    [SerializeField]
    private string layoutRootNodeId;


    // =========================================================
    // Build Output
    // =========================================================

    [SerializeField]
    private GameObject generatedPrefab;

    [SerializeField]
    private string lastBuildUtc;


    // =========================================================
    // Properties
    // =========================================================

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

    public string LayoutRootNodeId =>
        layoutRootNodeId;

    public GameObject GeneratedPrefab =>
        generatedPrefab;

    public string LastBuildUtc =>
        lastBuildUtc;

    public bool HasBuildOutput =>
        generatedPrefab != null;


    // =========================================================
    // Unity
    // =========================================================

    private void OnEnable()
    {
        EnsureIntegrity();
    }


    private void OnValidate()
    {
        EnsureIntegrity();
    }


    // =========================================================
    // Integrity
    // =========================================================

    public void EnsureIntegrity()
    {
        if (string.IsNullOrWhiteSpace(
                id
            ))
        {
            id =
                Guid.NewGuid()
                    .ToString(
                        "N"
                    );
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

        EnsureLayoutRootIntegrity();
    }


    private void EnsureLayoutRootIntegrity()
    {
        if (graph == null ||
            graph.NodeCount == 0)
        {
            layoutRootNodeId =
                string.Empty;

            return;
        }


        if (!string.IsNullOrWhiteSpace(
                layoutRootNodeId
            ) &&
            graph.FindNode(
                layoutRootNodeId
            ) != null)
        {
            return;
        }


        LevelNodeData firstNode =
            graph.Nodes[0];


        layoutRootNodeId =
            firstNode != null
                ? firstNode.Id
                : string.Empty;
    }


    // =========================================================
    // Metadata
    // =========================================================

    public void SetLevelName(
        string value
    )
    {
        levelName =
            value?.Trim()
            ?? string.Empty;
    }


    public void SetDescription(
        string value
    )
    {
        description =
            value
            ?? string.Empty;
    }


    // =========================================================
    // Layout
    // =========================================================

    public bool SetLayoutRootNodeId(
        string nodeId
    )
    {
        EnsureIntegrity();


        if (graph.NodeCount == 0)
        {
            layoutRootNodeId =
                string.Empty;

            return string.IsNullOrWhiteSpace(
                nodeId
            );
        }


        if (string.IsNullOrWhiteSpace(
                nodeId
            ))
        {
            return false;
        }


        if (graph.FindNode(
                nodeId
            ) == null)
        {
            return false;
        }


        layoutRootNodeId =
            nodeId;


        return true;
    }


    public void InvalidateSpatialLayout()
    {
        EnsureIntegrity();


        spatialLayout.Clear();
    }


    // =========================================================
    // Build Output
    // =========================================================

    public void SetBuildOutput(
        GameObject prefab,
        string buildUtc
    )
    {
        generatedPrefab =
            prefab;


        lastBuildUtc =
            buildUtc
            ?? string.Empty;
    }


    public void ClearBuildOutput()
    {
        generatedPrefab =
            null;


        lastBuildUtc =
            string.Empty;
    }
}
