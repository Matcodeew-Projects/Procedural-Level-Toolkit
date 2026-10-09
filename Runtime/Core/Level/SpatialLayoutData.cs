using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class SpatialLayoutData
{
    [SerializeField] private bool isSolved;
    [SerializeField] private List<LevelModuleInstanceData> modules = new();
    [SerializeField] private List<LevelSocketConnectionData> socketConnections = new();

    public bool IsSolved => isSolved;
    public IReadOnlyList<LevelModuleInstanceData> Modules => modules;
    public IReadOnlyList<LevelSocketConnectionData> SocketConnections => socketConnections;
    public int ModuleCount => modules?.Count ?? 0;
    public int SocketConnectionCount => socketConnections?.Count ?? 0;

    public SpatialLayoutData() => EnsureIntegrity();

    public void EnsureIntegrity()
    {
        modules ??= new List<LevelModuleInstanceData>();
        socketConnections ??= new List<LevelSocketConnectionData>();
    }

    public void Clear()
    {
        EnsureIntegrity();
        modules.Clear();
        socketConnections.Clear();
        isSolved = false;
    }

    public void AddModule(LevelModuleInstanceData module)
    {
        if (module == null) return;
        EnsureIntegrity();
        modules.Add(module);
    }

    public void AddSocketConnection(LevelSocketConnectionData connection)
    {
        if (connection == null) return;
        EnsureIntegrity();
        socketConnections.Add(connection);
    }

    public LevelModuleInstanceData FindModule(string nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId)) return null;
        EnsureIntegrity();

        for (int i = 0; i < modules.Count; i++)
        {
            LevelModuleInstanceData module = modules[i];
            if (module != null && module.NodeId == nodeId) return module;
        }

        return null;
    }

    public void SetSolved(bool solved) => isSolved = solved;
}
