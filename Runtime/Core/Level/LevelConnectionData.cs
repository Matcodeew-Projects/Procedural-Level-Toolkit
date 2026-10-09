using System;
using UnityEngine;

[Serializable]
public sealed class LevelConnectionData
{
    [SerializeField]
    private string id;

    [SerializeField]
    private string fromNodeId;

    [SerializeField]
    private string toNodeId;

    [SerializeField]
    private string fromSocketId;

    [SerializeField]
    private string toSocketId;


    public string Id => id;

    public string FromNodeId => fromNodeId;

    public string ToNodeId => toNodeId;

    public string FromSocketId => fromSocketId;

    public string ToSocketId => toSocketId;


    public LevelConnectionData()
    {
        EnsureIntegrity();
    }


    public LevelConnectionData(
        string fromNodeId,
        string toNodeId,
        string fromSocketId = "",
        string toSocketId = ""
    )
    {
        id = CreateId();

        this.fromNodeId = fromNodeId;
        this.toNodeId = toNodeId;

        this.fromSocketId =
            fromSocketId ?? string.Empty;

        this.toSocketId =
            toSocketId ?? string.Empty;
    }


    public void EnsureIntegrity()
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            id = CreateId();
        }

        if (fromNodeId == null)
        {
            fromNodeId = string.Empty;
        }

        if (toNodeId == null)
        {
            toNodeId = string.Empty;
        }

        if (fromSocketId == null)
        {
            fromSocketId = string.Empty;
        }

        if (toSocketId == null)
        {
            toSocketId = string.Empty;
        }
    }


    public void SetSockets(
        string fromSocketId,
        string toSocketId
    )
    {
        this.fromSocketId =
            fromSocketId ?? string.Empty;

        this.toSocketId =
            toSocketId ?? string.Empty;
    }


    public bool TouchesNode(string nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            return false;
        }

        return
            fromNodeId == nodeId ||
            toNodeId == nodeId;
    }


    public bool Matches(
        string fromNodeId,
        string toNodeId,
        string fromSocketId,
        string toSocketId
    )
    {
        return
            this.fromNodeId == fromNodeId &&
            this.toNodeId == toNodeId &&
            this.fromSocketId ==
            (fromSocketId ?? string.Empty) &&
            this.toSocketId ==
            (toSocketId ?? string.Empty);
    }


    private static string CreateId()
    {
        return Guid.NewGuid()
            .ToString("N");
    }
}