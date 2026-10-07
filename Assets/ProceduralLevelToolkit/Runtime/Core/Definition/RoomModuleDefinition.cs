using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "RoomModuleDefinition",
    menuName = "Procedural Level Toolkit/Room Module Definition"
)]
public sealed class RoomModuleDefinition : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField] private RoomDefinition room;
    [SerializeField] private GameObject prefab;
    [SerializeField, Min(0.0001f)] private float cellWorldSize = 1f;
    [SerializeField] private List<string> tags = new List<string>();

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public RoomDefinition Room => room;
    public GameObject Prefab => prefab;
    public float CellWorldSize => Mathf.Max(0.0001f, cellWorldSize);
    public IReadOnlyList<string> Tags => tags;
    public bool IsBuildable => room != null && prefab != null && cellWorldSize > 0f;

    public void Configure(RoomDefinition room, GameObject prefab, float cellWorldSize)
    {
        this.room = room;
        this.prefab = prefab;
        this.cellWorldSize = Mathf.Max(0.0001f, cellWorldSize);

        if (string.IsNullOrWhiteSpace(displayName) && room != null)
            displayName = room.name;
    }

    public void SetDisplayName(string value) => displayName = value?.Trim() ?? string.Empty;
    public void SetRoom(RoomDefinition value) => room = value;
    public void SetPrefab(GameObject value) => prefab = value;
    public void SetCellWorldSize(float value) => cellWorldSize = Mathf.Max(0.0001f, value);
}
