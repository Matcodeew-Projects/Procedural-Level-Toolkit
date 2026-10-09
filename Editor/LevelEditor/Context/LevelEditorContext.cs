using System;
using UnityEditor;
using UnityEngine;

public sealed class LevelEditorContext
{
    private readonly LayoutSolver layoutSolver = new LayoutSolver();

    private LevelDefinition currentLevel;
    private LevelNodeData selectedNode;
    private LevelConnectionData selectedConnection;
    private LayoutSolveResult lastSolveResult;

    public LevelDefinition CurrentLevel => currentLevel;
    public LevelGraphData Graph => currentLevel != null ? currentLevel.Graph : null;
    public SpatialLayoutData SpatialLayout => currentLevel != null ? currentLevel.SpatialLayout : null;
    public LevelNodeData SelectedNode => selectedNode;
    public LevelConnectionData SelectedConnection => selectedConnection;
    public string LayoutRootNodeId => currentLevel != null ? currentLevel.LayoutRootNodeId : null;
    public LayoutSolveResult LastSolveResult => lastSolveResult;
    public bool HasLevel => currentLevel != null;

    public event Action OnLevelChanged;
    public event Action OnGraphChanged;
    public event Action OnSelectionChanged;
    public event Action<LevelNodeData> OnNodePositionChanged;
    public event Action OnLayoutChanged;
    public event Action OnLayoutRootChanged;

    public void SetLevel(LevelDefinition level)
    {
        if (currentLevel == level)
            return;

        currentLevel = level;
        selectedNode = null;
        selectedConnection = null;
        lastSolveResult = null;

        if (currentLevel != null)
        {
            currentLevel.EnsureIntegrity();
            RoomModuleEditorUtility.TryMigrateLegacyNodes(currentLevel);
        }

        NormalizeLayoutRoot();

        OnLevelChanged?.Invoke();
        OnGraphChanged?.Invoke();
        OnSelectionChanged?.Invoke();
        OnLayoutChanged?.Invoke();
        OnLayoutRootChanged?.Invoke();
    }

    public void ClearLevel() => SetLevel(null);

    public LevelNodeData AddModule(RoomModuleDefinition module, Vector2 graphPosition)
    {
        if (currentLevel == null || module == null || module.Room == null)
            return null;

        RecordUndo("Add Room Module To Level");
        LevelNodeData node = currentLevel.Graph.AddNode(module, graphPosition);
        InvalidateSpatialLayout();

        if (string.IsNullOrWhiteSpace(currentLevel.LayoutRootNodeId))
        {
            currentLevel.SetLayoutRootNodeId(node.Id);
            OnLayoutRootChanged?.Invoke();
        }

        MarkDirty();
        selectedNode = node;
        selectedConnection = null;

        OnGraphChanged?.Invoke();
        OnSelectionChanged?.Invoke();
        OnLayoutChanged?.Invoke();

        return node;
    }

    // Compatibility with older caller code.
    public LevelNodeData AddRoom(RoomDefinition room, Vector2 graphPosition)
    {
        RoomModuleDefinition module = RoomModuleEditorUtility.FindModuleForRoom(room);
        if (module != null)
            return AddModule(module, graphPosition);

        if (currentLevel == null || room == null)
            return null;

        RecordUndo("Add Legacy Room To Level");
        LevelNodeData node = currentLevel.Graph.AddNode(room, graphPosition);
        InvalidateSpatialLayout();
        MarkDirty();
        selectedNode = node;
        selectedConnection = null;

        OnGraphChanged?.Invoke();
        OnSelectionChanged?.Invoke();
        OnLayoutChanged?.Invoke();
        return node;
    }

    public bool RemoveNode(LevelNodeData node)
    {
        if (currentLevel == null || node == null)
            return false;

        RecordUndo("Remove Room From Level");
        bool removed = currentLevel.Graph.RemoveNode(node.Id);
        if (!removed)
            return false;

        InvalidateSpatialLayout();

        if (selectedNode == node)
            selectedNode = null;

        if (selectedConnection != null && currentLevel.Graph.FindConnection(selectedConnection.Id) == null)
            selectedConnection = null;

        NormalizeLayoutRoot();
        MarkDirty();

        OnGraphChanged?.Invoke();
        OnSelectionChanged?.Invoke();
        OnLayoutChanged?.Invoke();
        OnLayoutRootChanged?.Invoke();
        return true;
    }

    public void BeginNodeMove(LevelNodeData node)
    {
        if (currentLevel != null && node != null)
            RecordUndo("Move Level Node");
    }

    public void MoveNodeContinuous(LevelNodeData node, Vector2 position)
    {
        if (currentLevel == null || node == null)
            return;

        node.SetGraphPosition(position);
        MarkDirty();
        OnNodePositionChanged?.Invoke(node);
    }

    public void MoveNode(LevelNodeData node, Vector2 position)
    {
        if (currentLevel == null || node == null)
            return;

        BeginNodeMove(node);
        MoveNodeContinuous(node, position);
    }

    public void SetNodeModule(LevelNodeData node, RoomModuleDefinition module)
    {
        if (currentLevel == null || node == null || module == null || module.Room == null)
            return;

        if (node.Module == module)
            return;

        RecordUndo("Change Level Node Module");
        node.SetModule(module);
        InvalidateSpatialLayout();
        MarkDirty();

        OnGraphChanged?.Invoke();
        OnSelectionChanged?.Invoke();
        OnLayoutChanged?.Invoke();
    }

    public void SetNodeRoom(LevelNodeData node, RoomDefinition room)
    {
        if (node == null || room == null)
            return;

        RoomModuleDefinition module = RoomModuleEditorUtility.FindModuleForRoom(room);
        if (module != null)
        {
            SetNodeModule(node, module);
            return;
        }

        if (currentLevel == null)
            return;

        RecordUndo("Change Legacy Level Node Room");
        node.SetRoom(room);
        InvalidateSpatialLayout();
        MarkDirty();

        OnGraphChanged?.Invoke();
        OnSelectionChanged?.Invoke();
        OnLayoutChanged?.Invoke();
    }

    public LevelConnectionData ConnectNodes(LevelNodeData from, LevelNodeData to)
    {
        return ConnectNodes(from, to, string.Empty, string.Empty);
    }

    public LevelConnectionData ConnectNodes(
        LevelNodeData from,
        LevelNodeData to,
        string fromSocketId,
        string toSocketId)
    {
        if (currentLevel == null || from == null || to == null)
            return null;

        RecordUndo("Connect Level Nodes");

        bool created = currentLevel.Graph.TryAddConnection(
            from.Id,
            to.Id,
            fromSocketId,
            toSocketId,
            out LevelConnectionData connection);

        if (!created)
            return null;

        InvalidateSpatialLayout();
        MarkDirty();
        selectedNode = null;
        selectedConnection = connection;

        OnGraphChanged?.Invoke();
        OnSelectionChanged?.Invoke();
        OnLayoutChanged?.Invoke();
        return connection;
    }

    public bool RemoveConnection(LevelConnectionData connection)
    {
        if (currentLevel == null || connection == null)
            return false;

        RecordUndo("Remove Level Connection");
        bool removed = currentLevel.Graph.RemoveConnection(connection.Id);
        if (!removed)
            return false;

        InvalidateSpatialLayout();

        if (selectedConnection == connection)
            selectedConnection = null;

        MarkDirty();
        OnGraphChanged?.Invoke();
        OnSelectionChanged?.Invoke();
        OnLayoutChanged?.Invoke();
        return true;
    }

    public bool TrySetConnectionSockets(
        LevelConnectionData connection,
        string fromSocketId,
        string toSocketId,
        out string error)
    {
        error = string.Empty;

        if (currentLevel == null || connection == null)
        {
            error = "Missing LevelDefinition or connection.";
            return false;
        }

        LevelNodeData fromNode = currentLevel.Graph.FindNode(connection.FromNodeId);
        LevelNodeData toNode = currentLevel.Graph.FindNode(connection.ToNodeId);

        if (fromNode == null || toNode == null)
        {
            error = "Connection references a missing node.";
            return false;
        }

        RoomSocketData fromSocket = null;
        RoomSocketData toSocket = null;

        if (!string.IsNullOrWhiteSpace(fromSocketId))
        {
            fromSocket = LevelSocketUtility.FindSocket(fromNode, fromSocketId);
            if (fromSocket == null)
            {
                error = $"Socket '{fromSocketId}' does not exist on {GetRoomName(fromNode)}.";
                return false;
            }

            if (LevelSocketUtility.IsSocketUsed(currentLevel.Graph, fromNode.Id, fromSocket.Id, connection.Id))
            {
                error = $"Socket '{fromSocket.Id}' is already used by another connection.";
                return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(toSocketId))
        {
            toSocket = LevelSocketUtility.FindSocket(toNode, toSocketId);
            if (toSocket == null)
            {
                error = $"Socket '{toSocketId}' does not exist on {GetRoomName(toNode)}.";
                return false;
            }

            if (LevelSocketUtility.IsSocketUsed(currentLevel.Graph, toNode.Id, toSocket.Id, connection.Id))
            {
                error = $"Socket '{toSocket.Id}' is already used by another connection.";
                return false;
            }
        }

        if (fromSocket != null && toSocket != null &&
            !LevelSocketUtility.AreCompatible(fromSocket, toSocket, out error))
            return false;

        string safeFromId = fromSocketId ?? string.Empty;
        string safeToId = toSocketId ?? string.Empty;

        if (connection.FromSocketId == safeFromId && connection.ToSocketId == safeToId)
            return true;

        RecordUndo("Change Connection Sockets");
        connection.SetSockets(safeFromId, safeToId);
        InvalidateSpatialLayout();
        MarkDirty();

        OnGraphChanged?.Invoke();
        OnSelectionChanged?.Invoke();
        OnLayoutChanged?.Invoke();
        return true;
    }

    public bool TryAutoAssignSockets(LevelConnectionData connection, out string error)
    {
        error = string.Empty;

        if (currentLevel == null || connection == null)
        {
            error = "Missing LevelDefinition or connection.";
            return false;
        }

        bool found = LevelSocketUtility.TryFindCompatiblePair(
            currentLevel.Graph,
            connection,
            out RoomSocketData fromSocket,
            out RoomSocketData toSocket,
            out error);

        if (!found)
            return false;

        return TrySetConnectionSockets(connection, fromSocket.Id, toSocket.Id, out error);
    }

    public void ClearConnectionSockets(LevelConnectionData connection)
    {
        if (connection != null)
            TrySetConnectionSockets(connection, string.Empty, string.Empty, out _);
    }

    public void SetLayoutRoot(string nodeId)
    {
        if (currentLevel == null)
        {
            OnLayoutRootChanged?.Invoke();
            return;
        }

        if (string.IsNullOrWhiteSpace(nodeId) || currentLevel.Graph.FindNode(nodeId) == null)
            return;

        if (currentLevel.LayoutRootNodeId == nodeId)
            return;

        RecordUndo("Change Layout Root");

        if (!currentLevel.SetLayoutRootNodeId(nodeId))
            return;

        InvalidateSpatialLayout();
        MarkDirty();

        OnLayoutRootChanged?.Invoke();
        OnLayoutChanged?.Invoke();
    }

    public LayoutSolveResult SolveLayout()
    {
        if (currentLevel == null)
        {
            lastSolveResult = new LayoutSolveResult();
            lastSolveResult.AddError("No LevelDefinition selected.");
            OnLayoutChanged?.Invoke();
            return lastSolveResult;
        }

        NormalizeLayoutRoot();
        RecordUndo("Solve Level Layout");
        lastSolveResult = layoutSolver.Solve(currentLevel, currentLevel.LayoutRootNodeId);
        MarkDirty();
        OnLayoutChanged?.Invoke();
        return lastSolveResult;
    }

    public void ClearLayout()
    {
        if (currentLevel == null)
            return;

        RecordUndo("Clear Level Layout");
        currentLevel.SpatialLayout.Clear();
        lastSolveResult = null;
        MarkDirty();
        OnLayoutChanged?.Invoke();
    }

    public bool SaveCurrentLevel()
    {
        if (currentLevel == null)
            return false;

        currentLevel.EnsureIntegrity();
        EditorUtility.SetDirty(currentLevel);
        AssetDatabase.SaveAssetIfDirty(currentLevel);

        return true;
    }

    public void SelectNode(LevelNodeData node)
    {
        if (selectedNode == node && selectedConnection == null)
            return;

        selectedNode = node;
        selectedConnection = null;
        OnSelectionChanged?.Invoke();
    }

    public void SelectConnection(LevelConnectionData connection)
    {
        if (selectedConnection == connection && selectedNode == null)
            return;

        selectedNode = null;
        selectedConnection = connection;
        OnSelectionChanged?.Invoke();
    }

    public void ClearSelection()
    {
        if (selectedNode == null && selectedConnection == null)
            return;

        selectedNode = null;
        selectedConnection = null;
        OnSelectionChanged?.Invoke();
    }

    private void NormalizeLayoutRoot()
    {
        if (currentLevel == null)
            return;

        string previousRoot =
            currentLevel.LayoutRootNodeId;

        currentLevel.EnsureIntegrity();

        if (previousRoot != currentLevel.LayoutRootNodeId)
            EditorUtility.SetDirty(currentLevel);
    }

    private void InvalidateSpatialLayout()
    {
        currentLevel?.InvalidateSpatialLayout();
        lastSolveResult = null;
    }

    private void RecordUndo(string operationName)
    {
        if (currentLevel != null)
            Undo.RecordObject(currentLevel, operationName);
    }

    private void MarkDirty()
    {
        if (currentLevel == null)
            return;

        currentLevel.EnsureIntegrity();
        EditorUtility.SetDirty(currentLevel);
    }

    private static string GetRoomName(LevelNodeData node)
    {
        if (node?.Module != null)
            return node.Module.DisplayName;

        if (node?.Room != null)
            return node.Room.name;

        return "Missing Room";
    }
}
