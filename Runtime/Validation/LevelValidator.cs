using System;
using System.Collections.Generic;
using UnityEngine;

public static class LevelValidator
{
    private const float CellSizeTolerance =
        0.0001f;

    private const float SocketPositionTolerance =
        0.001f;


    // =========================================================
    // Entry Point
    // =========================================================

    public static LevelValidationResult Validate(
        LevelDefinition level
    )
    {
        LevelValidationResult result =
            new LevelValidationResult();


        if (level == null)
        {
            result.AddError(
                "LEVEL_NULL",
                "LevelDefinition is null."
            );


            return result;
        }


        level.EnsureIntegrity();


        ValidateMetadata(
            level,
            result
        );


        ValidateGraph(
            level,
            result
        );


        ValidateLayout(
            level,
            result
        );


        return result;
    }


    // =========================================================
    // Metadata
    // =========================================================

    private static void ValidateMetadata(
        LevelDefinition level,
        LevelValidationResult result
    )
    {
        if (string.IsNullOrWhiteSpace(
                level.LevelName
            ))
        {
            result.AddWarning(
                "LEVEL_NAME_EMPTY",
                "Level name is empty."
            );
        }


        if (string.IsNullOrWhiteSpace(
                level.Description
            ))
        {
            result.AddWarning(
                "LEVEL_DESCRIPTION_EMPTY",
                "Level description is empty."
            );
        }
    }


    // =========================================================
    // Graph
    // =========================================================

    private static void ValidateGraph(
        LevelDefinition level,
        LevelValidationResult result
    )
    {
        LevelGraphData graph =
            level.Graph;


        if (graph == null)
        {
            result.AddError(
                "GRAPH_NULL",
                "Level graph is missing."
            );


            return;
        }


        if (graph.NodeCount ==
            0)
        {
            result.AddError(
                "GRAPH_EMPTY",
                "The level graph contains no nodes."
            );


            return;
        }


        ValidateNodes(
            graph,
            result
        );


        ValidateConnections(
            graph,
            result
        );


        ValidateConnectivity(
            graph,
            result
        );
    }


    private static void ValidateNodes(
        LevelGraphData graph,
        LevelValidationResult result
    )
    {
        HashSet<string> nodeIds =
            new HashSet<string>();


        float commonCellSize =
            -1f;


        for (int i = 0;
             i < graph.Nodes.Count;
             i++)
        {
            LevelNodeData node =
                graph.Nodes[i];


            if (node == null)
            {
                result.AddError(
                    "NODE_NULL",
                    $"Graph node #{i + 1} is null."
                );


                continue;
            }


            if (string.IsNullOrWhiteSpace(
                    node.Id
                ))
            {
                result.AddError(
                    "NODE_ID_EMPTY",
                    $"Graph node #{i + 1} has no ID."
                );
            }
            else if (!nodeIds.Add(
                         node.Id
                     ))
            {
                result.AddError(
                    "NODE_ID_DUPLICATE",
                    $"Node ID '{node.Id}' is duplicated."
                );
            }


            if (node.Module == null)
            {
                result.AddError(
                    "NODE_MODULE_MISSING",
                    $"Node '{GetNodeName(node)}' has no RoomModuleDefinition."
                );


                continue;
            }


            RoomModuleDefinition module =
                node.Module;


            if (module.Room == null)
            {
                result.AddError(
                    "MODULE_ROOM_MISSING",
                    $"Room module '{module.DisplayName}' has no RoomDefinition."
                );
            }


            if (module.Prefab == null)
            {
                result.AddError(
                    "MODULE_PREFAB_MISSING",
                    $"Room module '{module.DisplayName}' has no 3D prefab."
                );
            }


            if (module.CellWorldSize <=
                0f)
            {
                result.AddError(
                    "MODULE_CELL_SIZE_INVALID",
                    $"Room module '{module.DisplayName}' has an invalid World Units / Cell value."
                );
            }
            else if (commonCellSize <
                     0f)
            {
                commonCellSize =
                    module.CellWorldSize;
            }
            else if (Mathf.Abs(
                         commonCellSize -
                         module.CellWorldSize
                     ) >
                     CellSizeTolerance)
            {
                result.AddError(
                    "MODULE_CELL_SIZE_MISMATCH",
                    $"Room module '{module.DisplayName}' uses World Units / Cell " +
                    $"{module.CellWorldSize:0.###}, while the level uses {commonCellSize:0.###}."
                );
            }


            RoomDefinition room =
                node.Room;


            if (room != null)
            {
                RoomValidationResult roomValidation =
                    RoomValidator.Validate(
                        room
                    );


                for (int issueIndex = 0;
                     issueIndex < roomValidation.Issues.Count;
                     issueIndex++)
                {
                    RoomValidationIssue issue =
                        roomValidation.Issues[issueIndex];


                    string code =
                        "ROOM_" +
                        issue.Code;


                    string message =
                        $"Room '{room.name}': {issue.Message}";


                    if (issue.Severity ==
                        RoomValidationSeverity.Error)
                    {
                        result.AddError(
                            code,
                            message
                        );
                    }
                    else
                    {
                        result.AddWarning(
                            code,
                            message
                        );
                    }
                }
            }
        }
    }


    private static void ValidateConnections(
        LevelGraphData graph,
        LevelValidationResult result
    )
    {
        HashSet<string> connectionIds =
            new HashSet<string>();


        HashSet<string> usedSockets =
            new HashSet<string>();


        for (int i = 0;
             i < graph.Connections.Count;
             i++)
        {
            LevelConnectionData connection =
                graph.Connections[i];


            if (connection == null)
            {
                result.AddError(
                    "CONNECTION_NULL",
                    $"Graph connection #{i + 1} is null."
                );


                continue;
            }


            if (string.IsNullOrWhiteSpace(
                    connection.Id
                ))
            {
                result.AddError(
                    "CONNECTION_ID_EMPTY",
                    $"Graph connection #{i + 1} has no ID."
                );
            }
            else if (!connectionIds.Add(
                         connection.Id
                     ))
            {
                result.AddError(
                    "CONNECTION_ID_DUPLICATE",
                    $"Connection ID '{connection.Id}' is duplicated."
                );
            }


            LevelNodeData from =
                graph.FindNode(
                    connection.FromNodeId
                );


            LevelNodeData to =
                graph.FindNode(
                    connection.ToNodeId
                );


            if (from == null ||
                to == null)
            {
                result.AddError(
                    "CONNECTION_NODE_MISSING",
                    $"Connection '{connection.Id}' references a missing node."
                );


                continue;
            }


            if (from.Id ==
                to.Id)
            {
                result.AddError(
                    "CONNECTION_SELF",
                    $"Node '{GetNodeName(from)}' is connected to itself."
                );
            }


            if (string.IsNullOrWhiteSpace(
                    connection.FromSocketId
                ) ||
                string.IsNullOrWhiteSpace(
                    connection.ToSocketId
                ))
            {
                result.AddError(
                    "CONNECTION_SOCKET_UNASSIGNED",
                    $"Connection '{GetNodeName(from)} → {GetNodeName(to)}' does not have both sockets assigned."
                );


                continue;
            }


            RoomSocketData fromSocket =
                FindSocket(
                    from.Room,
                    connection.FromSocketId
                );


            RoomSocketData toSocket =
                FindSocket(
                    to.Room,
                    connection.ToSocketId
                );


            if (fromSocket == null)
            {
                result.AddError(
                    "CONNECTION_FROM_SOCKET_MISSING",
                    $"Connection '{GetNodeName(from)} → {GetNodeName(to)}' references a missing socket on '{GetNodeName(from)}'."
                );
            }


            if (toSocket == null)
            {
                result.AddError(
                    "CONNECTION_TO_SOCKET_MISSING",
                    $"Connection '{GetNodeName(from)} → {GetNodeName(to)}' references a missing socket on '{GetNodeName(to)}'."
                );
            }


            if (fromSocket == null ||
                toSocket == null)
            {
                continue;
            }


            string fromUsage =
                from.Id +
                "|" +
                fromSocket.Id;


            string toUsage =
                to.Id +
                "|" +
                toSocket.Id;


            if (!usedSockets.Add(
                    fromUsage
                ))
            {
                result.AddError(
                    "SOCKET_REUSED",
                    $"Socket '{GetSocketName(fromSocket)}' on '{GetNodeName(from)}' is used by more than one connection."
                );
            }


            if (!usedSockets.Add(
                    toUsage
                ))
            {
                result.AddError(
                    "SOCKET_REUSED",
                    $"Socket '{GetSocketName(toSocket)}' on '{GetNodeName(to)}' is used by more than one connection."
                );
            }


            if (!AreSocketsCompatible(
                    fromSocket,
                    toSocket,
                    out string reason
                ))
            {
                result.AddError(
                    "SOCKET_INCOMPATIBLE",
                    $"Connection '{GetNodeName(from)} → {GetNodeName(to)}' is invalid: {reason}"
                );
            }
        }
    }


    private static void ValidateConnectivity(
        LevelGraphData graph,
        LevelValidationResult result
    )
    {
        if (graph.NodeCount <=
            1)
        {
            return;
        }


        LevelNodeData root =
            graph.Nodes[0];


        if (root == null)
        {
            return;
        }


        HashSet<string> visited =
            new HashSet<string>();


        Queue<string> queue =
            new Queue<string>();


        visited.Add(
            root.Id
        );


        queue.Enqueue(
            root.Id
        );


        while (queue.Count >
               0)
        {
            string current =
                queue.Dequeue();


            for (int i = 0;
                 i < graph.Connections.Count;
                 i++)
            {
                LevelConnectionData connection =
                    graph.Connections[i];


                if (connection == null ||
                    !connection.TouchesNode(
                        current
                    ))
                {
                    continue;
                }


                string next =
                    connection.FromNodeId ==
                    current
                        ? connection.ToNodeId
                        : connection.FromNodeId;


                if (visited.Add(
                        next
                    ))
                {
                    queue.Enqueue(
                        next
                    );
                }
            }
        }


        if (visited.Count ==
            graph.NodeCount)
        {
            return;
        }


        List<string> disconnected =
            new List<string>();


        for (int i = 0;
             i < graph.Nodes.Count;
             i++)
        {
            LevelNodeData node =
                graph.Nodes[i];


            if (node != null &&
                !visited.Contains(
                    node.Id
                ))
            {
                disconnected.Add(
                    GetNodeName(
                        node
                    )
                );
            }
        }


        result.AddError(
            "GRAPH_DISCONNECTED",
            $"The graph is disconnected. Unreachable node(s): {string.Join(", ", disconnected)}."
        );
    }


    // =========================================================
    // Layout
    // =========================================================

    private static void ValidateLayout(
        LevelDefinition level,
        LevelValidationResult result
    )
    {
        LevelGraphData graph =
            level.Graph;


        SpatialLayoutData layout =
            level.SpatialLayout;


        if (layout == null)
        {
            result.AddError(
                "LAYOUT_NULL",
                "SpatialLayoutData is missing."
            );


            return;
        }


        if (!layout.IsSolved)
        {
            result.AddError(
                "LAYOUT_NOT_SOLVED",
                "The spatial layout has not been solved successfully."
            );
        }


        Dictionary<
            string,
            LevelModuleInstanceData
        > placements =
            ValidatePlacements(
                graph,
                layout,
                result
            );


        ValidateOverlaps(
            layout,
            result
        );


        ValidateResolvedConnections(
            graph,
            layout,
            placements,
            result
        );
    }


    private static Dictionary<
        string,
        LevelModuleInstanceData
    > ValidatePlacements(
        LevelGraphData graph,
        SpatialLayoutData layout,
        LevelValidationResult result
    )
    {
        Dictionary<
            string,
            LevelModuleInstanceData
        > placements =
            new Dictionary<
                string,
                LevelModuleInstanceData
            >();


        for (int i = 0;
             i < layout.Modules.Count;
             i++)
        {
            LevelModuleInstanceData module =
                layout.Modules[i];


            if (module == null)
            {
                result.AddError(
                    "LAYOUT_MODULE_NULL",
                    $"Layout module #{i + 1} is null."
                );


                continue;
            }


            if (graph.FindNode(
                    module.NodeId
                ) ==
                null)
            {
                result.AddError(
                    "LAYOUT_UNKNOWN_NODE",
                    $"Spatial placement '{module.NodeId}' does not belong to the graph."
                );


                continue;
            }


            if (placements.ContainsKey(
                    module.NodeId
                ))
            {
                result.AddError(
                    "LAYOUT_DUPLICATE_NODE",
                    $"Node '{module.NodeId}' has more than one spatial placement."
                );


                continue;
            }


            placements.Add(
                module.NodeId,
                module
            );
        }


        for (int i = 0;
             i < graph.Nodes.Count;
             i++)
        {
            LevelNodeData node =
                graph.Nodes[i];


            if (node == null)
            {
                continue;
            }


            if (!placements.ContainsKey(
                    node.Id
                ))
            {
                result.AddError(
                    "LAYOUT_NODE_MISSING",
                    $"Node '{GetNodeName(node)}' has no spatial placement."
                );
            }
        }


        return placements;
    }


    private static void ValidateOverlaps(
        SpatialLayoutData layout,
        LevelValidationResult result
    )
    {
        for (int i = 0;
             i < layout.Modules.Count;
             i++)
        {
            LevelModuleInstanceData a =
                layout.Modules[i];


            if (a == null ||
                a.Room == null)
            {
                continue;
            }


            Rect boundsA =
                LayoutGeometryUtility
                    .GetWorldBounds2D(
                        a
                    );


            for (int j =
                     i + 1;
                 j < layout.Modules.Count;
                 j++)
            {
                LevelModuleInstanceData b =
                    layout.Modules[j];


                if (b == null ||
                    b.Room == null)
                {
                    continue;
                }


                Rect boundsB =
                    LayoutGeometryUtility
                        .GetWorldBounds2D(
                            b
                        );


                if (!LayoutGeometryUtility
                        .OverlapsArea(
                            boundsA,
                            boundsB
                        ))
                {
                    continue;
                }


                result.AddError(
                    "LAYOUT_OVERLAP",
                    $"Room '{GetModuleName(a)}' overlaps '{GetModuleName(b)}'."
                );
            }
        }
    }


    private static void ValidateResolvedConnections(
        LevelGraphData graph,
        SpatialLayoutData layout,
        Dictionary<
            string,
            LevelModuleInstanceData
        > placements,
        LevelValidationResult result
    )
    {
        Dictionary<
            string,
            LevelSocketConnectionData
        > resolved =
            new Dictionary<
                string,
                LevelSocketConnectionData
            >();


        for (int i = 0;
             i < layout.SocketConnections.Count;
             i++)
        {
            LevelSocketConnectionData connection =
                layout.SocketConnections[i];


            if (connection == null)
            {
                result.AddError(
                    "LAYOUT_SOCKET_CONNECTION_NULL",
                    $"Resolved socket connection #{i + 1} is null."
                );


                continue;
            }


            if (string.IsNullOrWhiteSpace(
                    connection.GraphConnectionId
                ))
            {
                result.AddError(
                    "LAYOUT_SOCKET_CONNECTION_ID_EMPTY",
                    "A resolved socket connection has no graph connection ID."
                );


                continue;
            }


            if (resolved.ContainsKey(
                    connection.GraphConnectionId
                ))
            {
                result.AddError(
                    "LAYOUT_SOCKET_CONNECTION_DUPLICATE",
                    $"Graph connection '{connection.GraphConnectionId}' has more than one spatial solution."
                );


                continue;
            }


            resolved.Add(
                connection.GraphConnectionId,
                connection
            );


            if (graph.FindConnection(
                    connection.GraphConnectionId
                ) ==
                null)
            {
                result.AddError(
                    "LAYOUT_ORPHAN_CONNECTION",
                    $"Spatial connection '{connection.GraphConnectionId}' has no matching logical connection."
                );
            }
        }


        for (int i = 0;
             i < graph.Connections.Count;
             i++)
        {
            LevelConnectionData logical =
                graph.Connections[i];


            if (logical == null)
            {
                continue;
            }


            if (!resolved.ContainsKey(
                    logical.Id
                ))
            {
                result.AddError(
                    "LAYOUT_CONNECTION_UNRESOLVED",
                    $"Logical connection '{logical.Id}' has no spatial socket solution."
                );
            }


            ValidateConnectionTransform(
                graph,
                logical,
                placements,
                result
            );
        }
    }


    private static void ValidateConnectionTransform(
        LevelGraphData graph,
        LevelConnectionData connection,
        Dictionary<
            string,
            LevelModuleInstanceData
        > placements,
        LevelValidationResult result
    )
    {
        if (!placements.TryGetValue(
                connection.FromNodeId,
                out LevelModuleInstanceData fromModule
            ) ||
            !placements.TryGetValue(
                connection.ToNodeId,
                out LevelModuleInstanceData toModule
            ))
        {
            return;
        }


        LevelNodeData fromNode =
            graph.FindNode(
                connection.FromNodeId
            );


        LevelNodeData toNode =
            graph.FindNode(
                connection.ToNodeId
            );


        RoomSocketData fromSocket =
            FindSocket(
                fromNode?.Room,
                connection.FromSocketId
            );


        RoomSocketData toSocket =
            FindSocket(
                toNode?.Room,
                connection.ToSocketId
            );


        if (fromSocket == null ||
            toSocket == null)
        {
            return;
        }


        Vector2 fromAnchor =
            LayoutGeometryUtility
                .GetWorldSocketAnchor2D(
                    fromModule,
                    fromSocket
                );


        Vector2 toAnchor =
            LayoutGeometryUtility
                .GetWorldSocketAnchor2D(
                    toModule,
                    toSocket
                );


        if (Vector2.Distance(
                fromAnchor,
                toAnchor
            ) >
            SocketPositionTolerance)
        {
            result.AddError(
                "LAYOUT_SOCKET_POSITION_MISMATCH",
                $"Sockets for '{GetNodeName(fromNode)} → {GetNodeName(toNode)}' do not meet at the same position."
            );
        }


        SocketDirection fromDirection =
            LayoutGeometryUtility
                .RotateDirection(
                    fromSocket.Direction,
                    fromModule.QuarterTurns
                );


        SocketDirection toDirection =
            LayoutGeometryUtility
                .RotateDirection(
                    toSocket.Direction,
                    toModule.QuarterTurns
                );


        if (LayoutGeometryUtility
                .Opposite(
                    fromDirection
                )
            !=
            toDirection)
        {
            result.AddError(
                "LAYOUT_SOCKET_DIRECTION_MISMATCH",
                $"Sockets for '{GetNodeName(fromNode)} → {GetNodeName(toNode)}' are not facing each other."
            );
        }
    }


    // =========================================================
    // Socket Helpers
    // =========================================================

    private static RoomSocketData FindSocket(
        RoomDefinition room,
        string socketId
    )
    {
        if (room == null ||
            room.Sockets == null ||
            string.IsNullOrWhiteSpace(
                socketId
            ))
        {
            return null;
        }


        for (int i = 0;
             i < room.Sockets.Count;
             i++)
        {
            RoomSocketData socket =
                room.Sockets[i];


            if (socket != null &&
                socket.Id ==
                socketId)
            {
                return socket;
            }
        }


        return null;
    }


    private static bool AreSocketsCompatible(
        RoomSocketData from,
        RoomSocketData to,
        out string reason
    )
    {
        reason =
            string.Empty;


        if (from.Width !=
            to.Width)
        {
            reason =
                $"width {from.Width} does not match {to.Width}.";


            return false;
        }


        bool fromTyped =
            !string.IsNullOrWhiteSpace(
                from.Type
            );


        bool toTyped =
            !string.IsNullOrWhiteSpace(
                to.Type
            );


        if (fromTyped &&
            toTyped &&
            !string.Equals(
                from.Type,
                to.Type,
                StringComparison.OrdinalIgnoreCase
            ))
        {
            reason =
                $"type '{from.Type}' does not match '{to.Type}'.";


            return false;
        }


        if (!AreRolesCompatible(
                from.Role,
                to.Role
            ))
        {
            reason =
                $"role {from.Role} cannot connect to {to.Role}.";


            return false;
        }


        return true;
    }


    private static bool AreRolesCompatible(
        SocketRole a,
        SocketRole b
    )
    {
        if (a ==
                SocketRole.Any ||
            b ==
                SocketRole.Any)
        {
            return true;
        }


        return
            (
                a ==
                SocketRole.Entry
                &&
                b ==
                SocketRole.Exit
            )
            ||
            (
                a ==
                SocketRole.Exit
                &&
                b ==
                SocketRole.Entry
            );
    }


    // =========================================================
    // Display Helpers
    // =========================================================

    private static string GetNodeName(
        LevelNodeData node
    )
    {
        if (node == null)
        {
            return "Missing Node";
        }


        if (node.Module != null)
        {
            return node.Module.DisplayName;
        }


        if (node.Room != null)
        {
            return node.Room.name;
        }


        return "Missing Room";
    }


    private static string GetModuleName(
        LevelModuleInstanceData module
    )
    {
        if (module?.Room != null)
        {
            return module.Room.name;
        }


        return module?.NodeId
            ?? "Missing Module";
    }


    private static string GetSocketName(
        RoomSocketData socket
    )
    {
        if (socket == null)
        {
            return "Missing Socket";
        }


        if (!string.IsNullOrWhiteSpace(
                socket.DisplayName
            ))
        {
            return socket.DisplayName;
        }


        return socket.Direction.ToString();
    }
}
