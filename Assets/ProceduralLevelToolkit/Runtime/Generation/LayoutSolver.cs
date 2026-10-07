using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class LayoutSolver
{
    private const float PositionTolerance =
        0.001f;


    private readonly Dictionary<
        string,
        LevelModuleInstanceData
    > placed =
        new Dictionary<
            string,
            LevelModuleInstanceData
        >();


    private readonly Queue<string> queue =
        new Queue<string>();


    // =========================================================
    // Solve
    // =========================================================

    public LayoutSolveResult Solve(
        LevelDefinition level,
        string rootNodeId
    )
    {
        LayoutSolveResult result =
            new LayoutSolveResult();


        if (level == null)
        {
            result.AddError(
                "LevelDefinition is null."
            );

            return result;
        }


        level.EnsureIntegrity();


        LevelGraphData graph =
            level.Graph;


        SpatialLayoutData layout =
            level.SpatialLayout;


        layout.Clear();

        placed.Clear();

        queue.Clear();


        result.TotalModuleCount =
            graph.NodeCount;


        result.TotalConnectionCount =
            graph.ConnectionCount;


        if (graph.NodeCount == 0)
        {
            result.AddError(
                "The level graph contains no nodes."
            );


            return FinalizeResult(
                layout,
                result
            );
        }


        LevelNodeData root =
            ResolveRoot(
                graph,
                rootNodeId
            );


        if (root == null ||
            root.Room == null)
        {
            result.AddError(
                "The selected root node is missing or has no RoomDefinition."
            );


            return FinalizeResult(
                layout,
                result
            );
        }


        ValidateGraphSockets(
            graph,
            result
        );


        if (!result.Success)
        {
            return FinalizeResult(
                layout,
                result
            );
        }


        LevelModuleInstanceData rootModule =
            new LevelModuleInstanceData(
                root.Id,
                root.Room,
                Vector3.zero,
                0
            );


        placed.Add(
            root.Id,
            rootModule
        );


        layout.AddModule(
            rootModule
        );


        queue.Enqueue(
            root.Id
        );


        // =====================================================
        // BFS placement
        // =====================================================

        while (queue.Count > 0)
        {
            string currentNodeId =
                queue.Dequeue();


            LevelNodeData currentNode =
                graph.FindNode(
                    currentNodeId
                );


            if (currentNode == null)
            {
                continue;
            }


            LevelModuleInstanceData currentModule =
                placed[
                    currentNodeId
                ];


            IReadOnlyList<
                LevelConnectionData
            > connections =
                graph.Connections;


            for (int i = 0;
                 i < connections.Count;
                 i++)
            {
                LevelConnectionData connection =
                    connections[i];


                if (connection == null ||
                    !connection.TouchesNode(
                        currentNodeId
                    ))
                {
                    continue;
                }


                ProcessConnection(
                    graph,
                    layout,
                    result,
                    currentNode,
                    currentModule,
                    connection
                );
            }
        }


        // =====================================================
        // Unresolved nodes
        // =====================================================

        IReadOnlyList<
            LevelNodeData
        > nodes =
            graph.Nodes;


        for (int i = 0;
             i < nodes.Count;
             i++)
        {
            LevelNodeData node =
                nodes[i];


            if (node == null)
            {
                continue;
            }


            if (!placed.ContainsKey(
                    node.Id
                ))
            {
                result.AddError(
                    $"Node '{GetNodeName(node)}' could not be placed. " +
                    "The graph may be disconnected or an earlier placement failed."
                );
            }
        }


        // =====================================================
        // Physical socket connections
        // =====================================================

        BuildResolvedSocketConnections(
            graph,
            layout,
            result
        );


        return FinalizeResult(
            layout,
            result
        );
    }


    // =========================================================
    // Process Connection
    // =========================================================

    private void ProcessConnection(
        LevelGraphData graph,
        SpatialLayoutData layout,
        LayoutSolveResult result,
        LevelNodeData currentNode,
        LevelModuleInstanceData currentModule,
        LevelConnectionData connection
    )
    {
        bool currentIsFrom =
            connection.FromNodeId ==
            currentNode.Id;


        string neighborNodeId =
            currentIsFrom
                ? connection.ToNodeId
                : connection.FromNodeId;


        string currentSocketId =
            currentIsFrom
                ? connection.FromSocketId
                : connection.ToSocketId;


        string neighborSocketId =
            currentIsFrom
                ? connection.ToSocketId
                : connection.FromSocketId;


        LevelNodeData neighborNode =
            graph.FindNode(
                neighborNodeId
            );


        if (neighborNode == null ||
            neighborNode.Room == null)
        {
            result.AddError(
                $"Connection references missing node '{neighborNodeId}'."
            );


            return;
        }


        RoomSocketData currentSocket =
            FindSocket(
                currentNode.Room,
                currentSocketId
            );


        RoomSocketData neighborSocket =
            FindSocket(
                neighborNode.Room,
                neighborSocketId
            );


        if (currentSocket == null ||
            neighborSocket == null)
        {
            result.AddError(
                $"Connection '{GetNodeName(currentNode)} → {GetNodeName(neighborNode)}' " +
                "references a missing socket."
            );


            return;
        }


        // -----------------------------------------------------
        // Already placed -> validate cycle / secondary edge
        // -----------------------------------------------------

        if (placed.TryGetValue(
                neighborNode.Id,
                out LevelModuleInstanceData existingModule
            ))
        {
            ValidateExistingConnection(
                result,
                currentNode,
                currentModule,
                currentSocket,
                neighborNode,
                existingModule,
                neighborSocket
            );


            return;
        }


        // -----------------------------------------------------
        // Compute placement
        // -----------------------------------------------------

        LevelModuleInstanceData candidate =
            ComputeNeighborPlacement(
                currentModule,
                currentSocket,
                neighborNode,
                neighborSocket
            );


        // -----------------------------------------------------
        // Collision
        // -----------------------------------------------------

        if (TryFindOverlap(
                candidate,
                out LevelModuleInstanceData overlapping
            ))
        {
            Rect candidateBounds =
                LayoutGeometryUtility
                    .GetWorldBounds2D(
                        candidate
                    );


            Rect overlapBounds =
                LayoutGeometryUtility
                    .GetWorldBounds2D(
                        overlapping
                    );


            result.AddError(
                $"Cannot place '{GetNodeName(neighborNode)}': " +
                $"it overlaps '{GetModuleName(overlapping)}'. " +
                $"Candidate bounds={FormatRect(candidateBounds)}, " +
                $"existing bounds={FormatRect(overlapBounds)}."
            );


            return;
        }


        placed.Add(
            neighborNode.Id,
            candidate
        );


        layout.AddModule(
            candidate
        );


        queue.Enqueue(
            neighborNode.Id
        );
    }


    // =========================================================
    // Placement Math
    // =========================================================

    private LevelModuleInstanceData ComputeNeighborPlacement(
        LevelModuleInstanceData currentModule,
        RoomSocketData currentSocket,
        LevelNodeData neighborNode,
        RoomSocketData neighborSocket
    )
    {
        SocketDirection currentWorldDirection =
            LayoutGeometryUtility
                .RotateDirection(
                    currentSocket.Direction,
                    currentModule.QuarterTurns
                );


        SocketDirection desiredNeighborDirection =
            LayoutGeometryUtility
                .Opposite(
                    currentWorldDirection
                );


        int neighborQuarterTurns =
            LayoutGeometryUtility
                .FindRotationToFace(
                    neighborSocket.Direction,
                    desiredNeighborDirection
                );


        Vector2 currentWorldAnchor =
            LayoutGeometryUtility
                .GetWorldSocketAnchor2D(
                    currentModule,
                    currentSocket
                );


        Vector2 neighborLocalAnchor =
            LayoutGeometryUtility
                .GetSocketLocalAnchor(
                    neighborNode.Room,
                    neighborSocket
                );


        Vector2 rotatedNeighborAnchor =
            LayoutGeometryUtility
                .RotatePointClockwise(
                    neighborLocalAnchor,
                    neighborQuarterTurns
                );


        Vector2 neighborPosition =
            currentWorldAnchor -
            rotatedNeighborAnchor;


        return new LevelModuleInstanceData(
            neighborNode.Id,
            neighborNode.Room,
            new Vector3(
                neighborPosition.x,
                0f,
                neighborPosition.y
            ),
            neighborQuarterTurns
        );
    }


    // =========================================================
    // Existing placement validation
    // =========================================================

    private void ValidateExistingConnection(
        LayoutSolveResult result,
        LevelNodeData currentNode,
        LevelModuleInstanceData currentModule,
        RoomSocketData currentSocket,
        LevelNodeData neighborNode,
        LevelModuleInstanceData neighborModule,
        RoomSocketData neighborSocket
    )
    {
        Vector2 currentAnchor =
            LayoutGeometryUtility
                .GetWorldSocketAnchor2D(
                    currentModule,
                    currentSocket
                );


        Vector2 neighborAnchor =
            LayoutGeometryUtility
                .GetWorldSocketAnchor2D(
                    neighborModule,
                    neighborSocket
                );


        if (Vector2.Distance(
                currentAnchor,
                neighborAnchor
            ) >
            PositionTolerance)
        {
            result.AddError(
                $"Cycle constraint failed between " +
                $"'{GetNodeName(currentNode)}' and " +
                $"'{GetNodeName(neighborNode)}': " +
                $"socket anchors differ. " +
                $"A={currentAnchor}, B={neighborAnchor}."
            );


            return;
        }


        SocketDirection currentDirection =
            LayoutGeometryUtility
                .RotateDirection(
                    currentSocket.Direction,
                    currentModule.QuarterTurns
                );


        SocketDirection neighborDirection =
            LayoutGeometryUtility
                .RotateDirection(
                    neighborSocket.Direction,
                    neighborModule.QuarterTurns
                );


        if (LayoutGeometryUtility
                .Opposite(
                    currentDirection
                )
            !=
            neighborDirection)
        {
            result.AddError(
                $"Cycle constraint failed between " +
                $"'{GetNodeName(currentNode)}' and " +
                $"'{GetNodeName(neighborNode)}': " +
                "socket directions are not facing each other."
            );
        }
    }


    // =========================================================
    // Collision
    // =========================================================

    private bool TryFindOverlap(
        LevelModuleInstanceData candidate,
        out LevelModuleInstanceData overlapping
    )
    {
        overlapping =
            null;


        Rect candidateBounds =
            LayoutGeometryUtility
                .GetWorldBounds2D(
                    candidate
                );


        foreach (
            KeyValuePair<
                string,
                LevelModuleInstanceData
            > pair
            in placed
        )
        {
            LevelModuleInstanceData existing =
                pair.Value;


            Rect existingBounds =
                LayoutGeometryUtility
                    .GetWorldBounds2D(
                        existing
                    );


            if (LayoutGeometryUtility
                    .OverlapsArea(
                        candidateBounds,
                        existingBounds
                    ))
            {
                overlapping =
                    existing;


                return true;
            }
        }


        return false;
    }


    // =========================================================
    // Physical Connections
    // =========================================================

    private void BuildResolvedSocketConnections(
        LevelGraphData graph,
        SpatialLayoutData layout,
        LayoutSolveResult result
    )
    {
        IReadOnlyList<
            LevelConnectionData
        > connections =
            graph.Connections;


        for (int i = 0;
             i < connections.Count;
             i++)
        {
            LevelConnectionData connection =
                connections[i];


            if (connection == null)
            {
                continue;
            }


            if (!placed.TryGetValue(
                    connection.FromNodeId,
                    out LevelModuleInstanceData fromModule
                ) ||
                !placed.TryGetValue(
                    connection.ToNodeId,
                    out LevelModuleInstanceData toModule
                ))
            {
                continue;
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
                continue;
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
                PositionTolerance)
            {
                continue;
            }


            Vector3 worldPosition =
                new Vector3(
                    fromAnchor.x,
                    0f,
                    fromAnchor.y
                );


            layout.AddSocketConnection(
                new LevelSocketConnectionData(
                    connection.Id,
                    connection.FromNodeId,
                    connection.FromSocketId,
                    connection.ToNodeId,
                    connection.ToSocketId,
                    worldPosition
                )
            );


            result.ResolvedConnectionCount++;
        }
    }


    // =========================================================
    // Pre-validation
    // =========================================================

    private void ValidateGraphSockets(
        LevelGraphData graph,
        LayoutSolveResult result
    )
    {
        HashSet<string> usedSockets =
            new HashSet<string>();


        IReadOnlyList<
            LevelConnectionData
        > connections =
            graph.Connections;


        for (int i = 0;
             i < connections.Count;
             i++)
        {
            LevelConnectionData connection =
                connections[i];


            if (connection == null)
            {
                continue;
            }


            LevelNodeData fromNode =
                graph.FindNode(
                    connection.FromNodeId
                );


            LevelNodeData toNode =
                graph.FindNode(
                    connection.ToNodeId
                );


            if (fromNode?.Room == null ||
                toNode?.Room == null)
            {
                result.AddError(
                    $"Connection #{i + 1} references a missing Room."
                );


                continue;
            }


            if (string.IsNullOrWhiteSpace(
                    connection.FromSocketId
                ) ||
                string.IsNullOrWhiteSpace(
                    connection.ToSocketId
                ))
            {
                result.AddError(
                    $"Connection '{GetNodeName(fromNode)} → {GetNodeName(toNode)}' " +
                    "does not have both sockets assigned."
                );


                continue;
            }


            RoomSocketData fromSocket =
                FindSocket(
                    fromNode.Room,
                    connection.FromSocketId
                );


            RoomSocketData toSocket =
                FindSocket(
                    toNode.Room,
                    connection.ToSocketId
                );


            if (fromSocket == null ||
                toSocket == null)
            {
                result.AddError(
                    $"Connection '{GetNodeName(fromNode)} → {GetNodeName(toNode)}' " +
                    "references a socket that no longer exists."
                );


                continue;
            }


            string fromUsage =
                fromNode.Id +
                "|" +
                fromSocket.Id;


            string toUsage =
                toNode.Id +
                "|" +
                toSocket.Id;


            if (!usedSockets.Add(
                    fromUsage
                ))
            {
                result.AddError(
                    $"Socket '{GetSocketName(fromSocket)}' on " +
                    $"'{GetNodeName(fromNode)}' is used more than once."
                );
            }


            if (!usedSockets.Add(
                    toUsage
                ))
            {
                result.AddError(
                    $"Socket '{GetSocketName(toSocket)}' on " +
                    $"'{GetNodeName(toNode)}' is used more than once."
                );
            }


            if (!AreSocketsCompatible(
                    fromSocket,
                    toSocket,
                    out string reason
                ))
            {
                result.AddError(
                    $"Connection '{GetNodeName(fromNode)} → {GetNodeName(toNode)}' " +
                    $"is incompatible: {reason}"
                );
            }
        }
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
                $"width {from.Width} does not match width {to.Width}.";


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
        if (a == SocketRole.Any ||
            b == SocketRole.Any)
        {
            return true;
        }


        return
            (
                a == SocketRole.Entry &&
                b == SocketRole.Exit
            )
            ||
            (
                a == SocketRole.Exit &&
                b == SocketRole.Entry
            );
    }


    // =========================================================
    // Helpers
    // =========================================================

    private static LevelNodeData ResolveRoot(
        LevelGraphData graph,
        string rootNodeId
    )
    {
        if (!string.IsNullOrWhiteSpace(
                rootNodeId
            ))
        {
            LevelNodeData requested =
                graph.FindNode(
                    rootNodeId
                );


            if (requested != null)
            {
                return requested;
            }
        }


        return graph.NodeCount > 0
            ? graph.Nodes[0]
            : null;
    }


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


        IReadOnlyList<
            RoomSocketData
        > sockets =
            room.Sockets;


        for (int i = 0;
             i < sockets.Count;
             i++)
        {
            RoomSocketData socket =
                sockets[i];


            if (socket != null &&
                socket.Id ==
                socketId)
            {
                return socket;
            }
        }


        return null;
    }


    private static LayoutSolveResult FinalizeResult(
        SpatialLayoutData layout,
        LayoutSolveResult result
    )
    {
        result.PlacedModuleCount =
            layout.ModuleCount;


        layout.SetSolved(
            result.Success &&
            result.PlacedModuleCount ==
            result.TotalModuleCount &&
            result.ResolvedConnectionCount ==
            result.TotalConnectionCount
        );


        return result;
    }


    private static string GetNodeName(
        LevelNodeData node
    )
    {
        if (node?.Room == null)
        {
            return "Missing Room";
        }


        return node.Room.name;
    }


    private static string GetModuleName(
        LevelModuleInstanceData module
    )
    {
        if (module?.Room == null)
        {
            return "Missing Room";
        }


        return module.Room.name;
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


    private static string FormatRect(
        Rect rect
    )
    {
        return
            $"[{rect.xMin:0.###}, {rect.yMin:0.###}] " +
            $"→ [{rect.xMax:0.###}, {rect.yMax:0.###}]";
    }
}
