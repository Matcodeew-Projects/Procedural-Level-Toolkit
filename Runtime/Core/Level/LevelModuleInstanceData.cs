using System;
using UnityEngine;

[Serializable]
public sealed class LevelModuleInstanceData
{
    [SerializeField] private string nodeId;
    [SerializeField] private RoomDefinition room;
    [SerializeField] private Vector3 position;
    [SerializeField] private Quaternion rotation = Quaternion.identity;
    [SerializeField, Range(0, 3)] private int quarterTurns;

    public string NodeId => nodeId;
    public RoomDefinition Room => room;
    public Vector3 Position => position;
    public Quaternion Rotation => rotation;
    public int QuarterTurns => quarterTurns;

    public LevelModuleInstanceData() { }

    public LevelModuleInstanceData(string nodeId, RoomDefinition room, Vector3 position, int quarterTurns)
    {
        this.nodeId = nodeId;
        this.room = room;
        SetTransform(position, quarterTurns);
    }

    public LevelModuleInstanceData(string nodeId, RoomDefinition room, Vector3 position, Quaternion rotation)
    {
        this.nodeId = nodeId;
        this.room = room;
        SetTransform(position, rotation);
    }

    public void SetTransform(Vector3 position, int quarterTurns)
    {
        this.position = position;
        this.quarterTurns = NormalizeQuarterTurns(quarterTurns);
        rotation = Quaternion.Euler(0f, this.quarterTurns * 90f, 0f);
    }

    public void SetTransform(Vector3 position, Quaternion rotation)
    {
        this.position = position;
        this.rotation = rotation;
        quarterTurns = NormalizeQuarterTurns(Mathf.RoundToInt(rotation.eulerAngles.y / 90f));
    }

    private static int NormalizeQuarterTurns(int value)
    {
        value %= 4;
        if (value < 0) value += 4;
        return value;
    }
}
