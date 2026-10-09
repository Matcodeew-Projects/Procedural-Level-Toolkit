using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class LevelGraphData
{
    [SerializeField] private List<LevelNodeData> nodes = new List<LevelNodeData>();
    [SerializeField] private List<LevelConnectionData> connections = new List<LevelConnectionData>();

    public IReadOnlyList<LevelNodeData> Nodes => nodes;
    public IReadOnlyList<LevelConnectionData> Connections => connections;
    public int NodeCount => nodes?.Count ?? 0;
    public int ConnectionCount => connections?.Count ?? 0;

    public LevelGraphData()
    {
        EnsureIntegrity();
    }

    public void EnsureIntegrity()
    {
        nodes ??= new List<LevelNodeData>();
        connections ??= new List<LevelConnectionData>();

        for (int i = 0; i < nodes.Count; i++)
            nodes[i]?.EnsureIntegrity();

        for (int i = 0; i < connections.Count; i++)
            connections[i]?.EnsureIntegrity();
    }

    public LevelNodeData AddNode(RoomModuleDefinition module, Vector2 graphPosition)
    {
        if (module == null)
            return null;

        EnsureIntegrity();
        LevelNodeData node = new LevelNodeData(module, graphPosition);
        nodes.Add(node);
        return node;
    }

    // Legacy overload retained so earlier code still compiles.
    public LevelNodeData AddNode(RoomDefinition room, Vector2 graphPosition)
    {
        if (room == null)
            return null;

        EnsureIntegrity();
        LevelNodeData node = new LevelNodeData(room, graphPosition);
        nodes.Add(node);
        return node;
    }

    public bool RemoveNode(string nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
            return false;

        EnsureIntegrity();
        LevelNodeData node = FindNode(nodeId);
        if (node == null)
            return false;

        for (int i = connections.Count - 1; i >= 0; i--)
        {
            if (connections[i] != null && connections[i].TouchesNode(nodeId))
                connections.RemoveAt(i);
        }

        return nodes.Remove(node);
    }

    public LevelNodeData FindNode(string nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
            return null;

        EnsureIntegrity();
        for (int i = 0; i < nodes.Count; i++)
        {
            LevelNodeData node = nodes[i];
            if (node != null && node.Id == nodeId)
                return node;
        }

        return null;
    }

    public bool ContainsNode(string nodeId) => FindNode(nodeId) != null;

    public bool TryAddConnection(
        string fromNodeId,
        string toNodeId,
        out LevelConnectionData connection)
    {
        return TryAddConnection(
            fromNodeId,
            toNodeId,
            string.Empty,
            string.Empty,
            out connection);
    }

    public bool TryAddConnection(
        string fromNodeId,
        string toNodeId,
        string fromSocketId,
        string toSocketId,
        out LevelConnectionData connection)
    {
        connection = null;
        EnsureIntegrity();

        if (string.IsNullOrWhiteSpace(fromNodeId) ||
            string.IsNullOrWhiteSpace(toNodeId) ||
            fromNodeId == toNodeId)
            return false;

        if (!ContainsNode(fromNodeId) || !ContainsNode(toNodeId))
            return false;

        if (ContainsExactConnection(fromNodeId, toNodeId))
            return false;

        connection = new LevelConnectionData(
            fromNodeId,
            toNodeId,
            fromSocketId,
            toSocketId);

        connections.Add(connection);
        return true;
    }

    public bool RemoveConnection(string connectionId)
    {
        LevelConnectionData connection = FindConnection(connectionId);
        if (connection == null)
            return false;

        return connections.Remove(connection);
    }

    public LevelConnectionData FindConnection(string connectionId)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
            return null;

        EnsureIntegrity();
        for (int i = 0; i < connections.Count; i++)
        {
            LevelConnectionData connection = connections[i];
            if (connection != null && connection.Id == connectionId)
                return connection;
        }

        return null;
    }

    public bool ContainsExactConnection(string nodeAId, string nodeBId)
    {
        EnsureIntegrity();
        for (int i = 0; i < connections.Count; i++)
        {
            LevelConnectionData connection = connections[i];
            if (connection == null)
                continue;

            bool direct = connection.FromNodeId == nodeAId && connection.ToNodeId == nodeBId;
            bool reverse = connection.FromNodeId == nodeBId && connection.ToNodeId == nodeAId;

            if (direct || reverse)
                return true;
        }

        return false;
    }

    public void Clear()
    {
        EnsureIntegrity();
        nodes.Clear();
        connections.Clear();
    }
}
