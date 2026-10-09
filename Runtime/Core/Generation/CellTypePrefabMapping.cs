using System;
using UnityEngine;

[Serializable]
public sealed class CellTypePrefabMapping
{
    [SerializeField]
    private CellTypeDefinition cellType;

    [SerializeField]
    private GameObject prefab;

    [SerializeField]
    private bool enabled = true;

    [SerializeField]
    private Vector3 positionOffset = Vector3.zero;

    [SerializeField]
    private Vector3 rotationOffset = Vector3.zero;

    [SerializeField]
    private Vector3 scaleMultiplier = Vector3.one;


    public CellTypeDefinition CellType =>
        cellType;

    public GameObject Prefab =>
        prefab;

    public bool Enabled =>
        enabled;

    public Vector3 PositionOffset =>
        positionOffset;

    public Vector3 RotationOffset =>
        rotationOffset;

    public Vector3 ScaleMultiplier =>
        scaleMultiplier;


    public bool Matches(
        CellTypeDefinition type)
    {
        return
            enabled &&
            type != null &&
            ReferenceEquals(
                cellType,
                type
            );
    }
}