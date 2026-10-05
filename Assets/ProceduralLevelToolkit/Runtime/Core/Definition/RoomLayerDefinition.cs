using UnityEngine;

[CreateAssetMenu(
    fileName = "RoomLayerDefinition",
    menuName = "Procedural Level Toolkit/Room Layer Definition"
)]
public sealed class RoomLayerDefinition
    : ScriptableObject
{
    // =========================================================
    // Identity
    // =========================================================

    [SerializeField]
    private string displayName =
        "New Layer";


    [SerializeField]
    [TextArea(2, 5)]
    private string description;


    // =========================================================
    // Data
    // =========================================================

    [SerializeField]
    private LayerDataType dataType;


    // =========================================================
    // Default Editor State
    // =========================================================

    [SerializeField]
    private bool visibleByDefault =
        true;


    [SerializeField]
    private bool lockedByDefault =
        false;


    // =========================================================
    // Properties
    // =========================================================

    public string DisplayName
    {
        get
        {
            if (string.IsNullOrWhiteSpace(
                    displayName
                ))
            {
                return name;
            }


            return displayName;
        }
    }


    public string Description =>
        description;


    public LayerDataType DataType =>
        dataType;


    public bool VisibleByDefault =>
        visibleByDefault;


    public bool LockedByDefault =>
        lockedByDefault;


    // =========================================================
    // Setters
    // =========================================================

    public void SetDisplayName(
        string value
    )
    {
        displayName =
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


    public void SetDataType(
        LayerDataType value
    )
    {
        dataType =
            value;
    }


    public void SetVisibleByDefault(
        bool value
    )
    {
        visibleByDefault =
            value;
    }


    public void SetLockedByDefault(
        bool value
    )
    {
        lockedByDefault =
            value;
    }
}