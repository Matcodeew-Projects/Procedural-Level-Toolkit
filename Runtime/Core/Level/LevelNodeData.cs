using System;
using UnityEngine;

[Serializable]
public sealed class LevelNodeData
{
    [SerializeField] private string id;
    [SerializeField] private RoomModuleDefinition module;

    // Legacy compatibility with existing LevelDefinition assets.
    [SerializeField] private RoomDefinition room;

    [SerializeField] private Vector2 graphPosition;

    public string Id => id;
    public RoomModuleDefinition Module => module;
    public RoomDefinition Room => module != null && module.Room != null ? module.Room : room;
    public Vector2 GraphPosition => graphPosition;
    public bool IsLegacyRoomReference => module == null && room != null;

    public LevelNodeData()
    {
        EnsureIntegrity();
    }

    public LevelNodeData(RoomModuleDefinition module, Vector2 graphPosition)
    {
        id = CreateId();
        SetModule(module);
        this.graphPosition = graphPosition;
    }

    public LevelNodeData(RoomDefinition room, Vector2 graphPosition)
    {
        id = CreateId();
        this.room = room;
        this.graphPosition = graphPosition;
    }

    public void EnsureIntegrity()
    {
        if (string.IsNullOrWhiteSpace(id))
            id = CreateId();

        if (module != null && module.Room != null)
            room = module.Room;
    }

    public void SetModule(RoomModuleDefinition value)
    {
        module = value;
        if (module != null)
            room = module.Room;
    }

    public void SetRoom(RoomDefinition value)
    {
        room = value;
        if (module != null && module.Room != value)
            module = null;
    }

    public void SetGraphPosition(Vector2 position)
    {
        graphPosition = position;
    }

    private static string CreateId() => Guid.NewGuid().ToString("N");
}
