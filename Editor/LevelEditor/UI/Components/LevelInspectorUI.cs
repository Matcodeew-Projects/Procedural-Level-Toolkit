using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

public sealed class LevelInspectorUI : VisualElement
{
    private const string UnassignedChoice = "< Unassigned >";

    private readonly LevelEditorContext context;
    private readonly ScrollView content;

    public LevelInspectorUI(LevelEditorContext context)
    {
        this.context = context;
        AddToClassList("level-panel");

        Label header = new Label("INSPECTOR");
        header.AddToClassList("level-panel__header");
        Add(header);

        content = new ScrollView();
        content.AddToClassList("level-panel__content");
        Add(content);

        RegisterCallback<AttachToPanelEvent>(OnAttach);
        RegisterCallback<DetachFromPanelEvent>(OnDetach);

        Refresh();
    }

    private void OnAttach(AttachToPanelEvent evt)
    {
        context.OnSelectionChanged += Refresh;
        context.OnGraphChanged += Refresh;
        context.OnLevelChanged += Refresh;
        context.OnNodePositionChanged += OnNodePositionChanged;
    }

    private void OnDetach(DetachFromPanelEvent evt)
    {
        context.OnSelectionChanged -= Refresh;
        context.OnGraphChanged -= Refresh;
        context.OnLevelChanged -= Refresh;
        context.OnNodePositionChanged -= OnNodePositionChanged;
    }

    private void OnNodePositionChanged(LevelNodeData node)
    {
        if (node != null && context.SelectedNode == node)
            Refresh();
    }

    private void Refresh()
    {
        content.Clear();

        if (!context.HasLevel)
        {
            AddInfo("No LevelDefinition selected.");
            return;
        }

        if (context.SelectedNode != null)
        {
            DrawNodeInspector(context.SelectedNode);
            return;
        }

        if (context.SelectedConnection != null)
        {
            DrawConnectionInspector(context.SelectedConnection);
            return;
        }

        DrawLevelInspector();
    }

    private void DrawLevelInspector()
    {
        AddTitle(context.CurrentLevel.LevelName);
        AddInfo($"Nodes: {context.Graph.NodeCount}");
        AddInfo($"Connections: {context.Graph.ConnectionCount}");

        int legacy = 0;
        for (int i = 0; i < context.Graph.Nodes.Count; i++)
        {
            if (context.Graph.Nodes[i]?.IsLegacyRoomReference == true)
                legacy++;
        }

        if (legacy > 0)
        {
            AddWarning(
                $"{legacy} node(s) still use legacy RoomDefinition references. " +
                "Create matching RoomModuleDefinition assets, then reload the level.");
        }
    }

    private void DrawNodeInspector(LevelNodeData node)
    {
        AddTitle(GetNodeDisplayName(node));

        ObjectField moduleField = new ObjectField("Module")
        {
            objectType = typeof(RoomModuleDefinition),
            allowSceneObjects = false,
            value = node.Module
        };

        moduleField.RegisterValueChangedCallback(evt =>
        {
            RoomModuleDefinition module = evt.newValue as RoomModuleDefinition;
            if (module != null)
                context.SetNodeModule(node, module);
        });

        content.Add(moduleField);

        if (node.Module == null && node.Room != null)
        {
            AddWarning(
                $"Legacy Room: {node.Room.name}. Layout solving still works, " +
                "but 3D build requires a RoomModuleDefinition.");
        }

        if (node.Module != null)
        {
            AddInfo(node.Module.Prefab != null
                ? $"Prefab: {node.Module.Prefab.name}"
                : "Prefab: Missing");

            AddInfo($"World Units / Cell: {node.Module.CellWorldSize:0.###}");
        }

        Vector2Field graphPosition = new Vector2Field("Graph Position")
        {
            value = node.GraphPosition
        };

        graphPosition.RegisterValueChangedCallback(evt =>
            context.MoveNode(node, evt.newValue));

        content.Add(graphPosition);

        AddSpacing();
        AddTitle("Room Sockets");
        DrawRoomSocketList(node);

        AddSpacing();
        AddTitle("Connections");
        DrawNodeConnections(node);

        AddSpacing();

        Button remove = new Button(() => context.RemoveNode(node))
        {
            text = "Remove Node"
        };
        remove.AddToClassList("level-danger-button");
        content.Add(remove);
    }

    private void DrawRoomSocketList(LevelNodeData node)
    {
        if (node.Room == null || node.Room.Sockets == null || node.Room.Sockets.Count == 0)
        {
            AddWarning("This RoomDefinition has no sockets.");
            return;
        }

        IReadOnlyList<RoomSocketData> sockets = node.Room.Sockets;
        for (int i = 0; i < sockets.Count; i++)
        {
            RoomSocketData socket = sockets[i];
            if (socket == null)
                continue;

            bool used = LevelSocketUtility.IsSocketUsed(
                context.Graph,
                node.Id,
                socket.Id);

            AddInfo((used ? "● " : "○ ") + LevelSocketUtility.GetSocketSummary(socket));
        }

        AddHint("● used   ○ available");
    }

    private void DrawNodeConnections(LevelNodeData node)
    {
        bool found = false;

        for (int i = 0; i < context.Graph.Connections.Count; i++)
        {
            LevelConnectionData connection = context.Graph.Connections[i];
            if (connection == null || !connection.TouchesNode(node.Id))
                continue;

            found = true;

            bool outgoing = connection.FromNodeId == node.Id;
            string otherId = outgoing ? connection.ToNodeId : connection.FromNodeId;
            LevelNodeData other = context.Graph.FindNode(otherId);

            VisualElement row = new VisualElement();
            row.AddToClassList("level-inspector-connection");

            Label label = new Label(
                outgoing
                    ? $"→ {GetNodeDisplayName(other)}"
                    : $"← {GetNodeDisplayName(other)}");

            Button select = new Button(() => context.SelectConnection(connection))
            {
                text = "Select"
            };

            row.Add(label);
            row.Add(select);
            content.Add(row);
        }

        if (!found)
            AddInfo("No connections.");
    }

    private void DrawConnectionInspector(LevelConnectionData connection)
    {
        AddTitle("Connection");

        LevelNodeData from = context.Graph.FindNode(connection.FromNodeId);
        LevelNodeData to = context.Graph.FindNode(connection.ToNodeId);

        AddInfo(GetNodeDisplayName(from));
        AddInfo("↓");
        AddInfo(GetNodeDisplayName(to));

        AddSpacing();
        AddTitle("Socket Assignment");

        if (from == null || to == null || from.Room == null || to.Room == null)
        {
            AddWarning("Connection references missing nodes or rooms.");
            DrawRemoveConnectionButton(connection);
            return;
        }

        DropdownField fromField = CreateSocketDropdown(
            "From Socket",
            from,
            connection.FromSocketId);

        DropdownField toField = CreateSocketDropdown(
            "To Socket",
            to,
            connection.ToSocketId);

        fromField.RegisterValueChangedCallback(evt =>
        {
            string id = ChoiceToSocketId(fromField, evt.newValue);
            if (!context.TrySetConnectionSockets(
                    connection,
                    id,
                    connection.ToSocketId,
                    out string error))
            {
                UnityEngine.Debug.LogWarning(error);
                Refresh();
            }
        });

        toField.RegisterValueChangedCallback(evt =>
        {
            string id = ChoiceToSocketId(toField, evt.newValue);
            if (!context.TrySetConnectionSockets(
                    connection,
                    connection.FromSocketId,
                    id,
                    out string error))
            {
                UnityEngine.Debug.LogWarning(error);
                Refresh();
            }
        });

        content.Add(fromField);
        DrawSocketDetails(from, connection.FromSocketId);
        AddSpacing();
        content.Add(toField);
        DrawSocketDetails(to, connection.ToSocketId);

        AddSpacing();
        DrawConnectionState(connection);
        AddSpacing();

        VisualElement actions = new VisualElement();
        actions.style.flexDirection = FlexDirection.Row;

        Button autoAssign = new Button(() =>
        {
            if (!context.TryAutoAssignSockets(connection, out string error))
                UnityEngine.Debug.LogWarning(error);

            Refresh();
        })
        {
            text = "Auto Assign"
        };

        Button clear = new Button(() =>
        {
            context.ClearConnectionSockets(connection);
            Refresh();
        })
        {
            text = "Clear"
        };

        actions.Add(autoAssign);
        actions.Add(clear);
        content.Add(actions);

        AddSpacing();
        DrawRemoveConnectionButton(connection);
    }

    private DropdownField CreateSocketDropdown(
        string label,
        LevelNodeData node,
        string currentSocketId)
    {
        List<string> choices = new List<string> { UnassignedChoice };
        Dictionary<string, string> displayToId = new Dictionary<string, string>();
        Dictionary<string, int> counts = new Dictionary<string, int>();
        string currentChoice = UnassignedChoice;

        if (node?.Room?.Sockets != null)
        {
            for (int i = 0; i < node.Room.Sockets.Count; i++)
            {
                RoomSocketData socket = node.Room.Sockets[i];
                if (socket == null)
                    continue;

                bool usedByOther = LevelSocketUtility.IsSocketUsed(
                    context.Graph,
                    node.Id,
                    socket.Id,
                    context.SelectedConnection?.Id);

                if (usedByOther)
                    continue;

                string baseName = LevelSocketUtility.GetSocketDisplayName(socket);
                counts.TryGetValue(baseName, out int count);
                count++;
                counts[baseName] = count;

                string display = count == 1 ? baseName : $"{baseName} #{count}";
                choices.Add(display);
                displayToId[display] = socket.Id;

                if (socket.Id == currentSocketId)
                    currentChoice = display;
            }
        }

        if (!string.IsNullOrWhiteSpace(currentSocketId) && currentChoice == UnassignedChoice)
        {
            currentChoice = "Missing Socket";
            choices.Add(currentChoice);
            displayToId[currentChoice] = currentSocketId;
        }

        DropdownField field = new DropdownField(label, choices, currentChoice);
        field.userData = displayToId;
        return field;
    }

    private static string ChoiceToSocketId(DropdownField field, string choice)
    {
        if (choice == UnassignedChoice)
            return string.Empty;

        Dictionary<string, string> map = field.userData as Dictionary<string, string>;
        if (map == null || !map.TryGetValue(choice, out string id))
            return string.Empty;

        return id;
    }

    private void DrawSocketDetails(LevelNodeData node, string socketId)
    {
        if (string.IsNullOrWhiteSpace(socketId))
        {
            AddHint("No socket assigned.");
            return;
        }

        RoomSocketData socket = LevelSocketUtility.FindSocket(node, socketId);
        if (socket == null)
        {
            AddWarning($"Socket '{socketId}' no longer exists on this room.");
            return;
        }

        AddHint(LevelSocketUtility.GetSocketSummary(socket));
    }

    private void DrawConnectionState(LevelConnectionData connection)
    {
        LevelConnectionSocketState state = LevelSocketUtility.GetConnectionState(
            context.Graph,
            connection,
            out string reason);

        switch (state)
        {
            case LevelConnectionSocketState.Valid:
                AddSuccess(reason);
                break;
            case LevelConnectionSocketState.Unassigned:
            case LevelConnectionSocketState.Partial:
                AddWarning(reason);
                break;
            case LevelConnectionSocketState.Invalid:
                AddError(reason);
                break;
        }
    }

    private void DrawRemoveConnectionButton(LevelConnectionData connection)
    {
        Button remove = new Button(() => context.RemoveConnection(connection))
        {
            text = "Remove Connection"
        };
        remove.AddToClassList("level-danger-button");
        content.Add(remove);
    }

    private static string GetNodeDisplayName(LevelNodeData node)
    {
        if (node == null)
            return "Missing Node";
        if (node.Module != null)
            return node.Module.DisplayName;
        if (node.Room != null)
            return node.Room.name;
        return "Missing Room";
    }

    private void AddTitle(string text)
    {
        Label label = new Label(text);
        label.AddToClassList("level-inspector-title");
        content.Add(label);
    }

    private void AddInfo(string text)
    {
        Label label = new Label(text);
        label.style.whiteSpace = WhiteSpace.Normal;
        label.style.opacity = 0.80f;
        content.Add(label);
    }

    private void AddHint(string text)
    {
        Label label = new Label(text);
        label.style.whiteSpace = WhiteSpace.Normal;
        label.style.opacity = 0.55f;
        content.Add(label);
    }

    private void AddSuccess(string text)
    {
        Label label = new Label("✓ " + text);
        label.style.whiteSpace = WhiteSpace.Normal;
        content.Add(label);
    }

    private void AddWarning(string text)
    {
        Label label = new Label("⚠ " + text);
        label.style.whiteSpace = WhiteSpace.Normal;
        content.Add(label);
    }

    private void AddError(string text)
    {
        Label label = new Label("✕ " + text);
        label.style.whiteSpace = WhiteSpace.Normal;
        content.Add(label);
    }

    private void AddSpacing()
    {
        VisualElement spacer = new VisualElement();
        spacer.style.height = 10f;
        content.Add(spacer);
    }
}
