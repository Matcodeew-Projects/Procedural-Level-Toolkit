using System;
using UnityEngine;

[Serializable]
public sealed class LevelSocketConnectionData
{
    [SerializeField] private string graphConnectionId;
    [SerializeField] private string fromNodeId;
    [SerializeField] private string fromSocketId;
    [SerializeField] private string toNodeId;
    [SerializeField] private string toSocketId;
    [SerializeField] private Vector3 worldPosition;

    public string GraphConnectionId => graphConnectionId;
    public string FromNodeId => fromNodeId;
    public string FromSocketId => fromSocketId;
    public string ToNodeId => toNodeId;
    public string ToSocketId => toSocketId;
    public Vector3 WorldPosition => worldPosition;

    public LevelSocketConnectionData() { }

    public LevelSocketConnectionData(
        string graphConnectionId,
        string fromNodeId,
        string fromSocketId,
        string toNodeId,
        string toSocketId,
        Vector3 worldPosition)
    {
        this.graphConnectionId = graphConnectionId;
        this.fromNodeId = fromNodeId;
        this.fromSocketId = fromSocketId;
        this.toNodeId = toNodeId;
        this.toSocketId = toSocketId;
        this.worldPosition = worldPosition;
    }
}
