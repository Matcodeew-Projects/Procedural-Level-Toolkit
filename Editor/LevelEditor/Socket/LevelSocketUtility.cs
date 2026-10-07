using System;
using System.Collections.Generic;

public enum LevelConnectionSocketState
{
    Unassigned,
    Partial,
    Valid,
    Invalid
}

public static class LevelSocketUtility
{
    // =========================================================
    // Find
    // =========================================================

    public static RoomSocketData FindSocket(
        RoomDefinition room,
        string socketId
    )
    {
        if (room == null ||
            string.IsNullOrWhiteSpace(socketId))
        {
            return null;
        }


        IReadOnlyList<RoomSocketData> sockets =
            room.Sockets;


        if (sockets == null)
        {
            return null;
        }


        for (int i = 0;
             i < sockets.Count;
             i++)
        {
            RoomSocketData socket =
                sockets[i];


            if (socket == null)
            {
                continue;
            }


            if (socket.Id == socketId)
            {
                return socket;
            }
        }


        return null;
    }


    public static RoomSocketData FindSocket(
        LevelNodeData node,
        string socketId
    )
    {
        if (node == null)
        {
            return null;
        }


        return FindSocket(
            node.Room,
            socketId
        );
    }


    // =========================================================
    // Usage
    // =========================================================

    public static bool IsSocketUsed(
        LevelGraphData graph,
        string nodeId,
        string socketId,
        string ignoredConnectionId = null
    )
    {
        if (graph == null ||
            string.IsNullOrWhiteSpace(nodeId) ||
            string.IsNullOrWhiteSpace(socketId))
        {
            return false;
        }


        IReadOnlyList<LevelConnectionData> connections =
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


            if (!string.IsNullOrWhiteSpace(
                    ignoredConnectionId
                ) &&
                connection.Id ==
                ignoredConnectionId)
            {
                continue;
            }


            bool usedAsFrom =
                connection.FromNodeId ==
                nodeId &&
                connection.FromSocketId ==
                socketId;


            bool usedAsTo =
                connection.ToNodeId ==
                nodeId &&
                connection.ToSocketId ==
                socketId;


            if (usedAsFrom ||
                usedAsTo)
            {
                return true;
            }
        }


        return false;
    }


    // =========================================================
    // Compatibility
    // =========================================================

    public static bool AreCompatible(
        RoomSocketData from,
        RoomSocketData to,
        out string reason
    )
    {
        reason =
            string.Empty;


        if (from == null ||
            to == null)
        {
            reason =
                "One of the sockets does not exist.";

            return false;
        }


        // -----------------------------------------------------
        // Type
        // -----------------------------------------------------
        //
        // Empty Type acts as a generic / wildcard socket.
        // Otherwise both types must match.
        // -----------------------------------------------------

        bool fromHasType =
            !string.IsNullOrWhiteSpace(
                from.Type
            );


        bool toHasType =
            !string.IsNullOrWhiteSpace(
                to.Type
            );


        if (fromHasType &&
            toHasType &&
            !string.Equals(
                from.Type,
                to.Type,
                StringComparison.OrdinalIgnoreCase
            ))
        {
            reason =
                $"Socket types are incompatible: " +
                $"{from.Type} ↔ {to.Type}.";

            return false;
        }


        // -----------------------------------------------------
        // Width
        // -----------------------------------------------------

        if (from.Width !=
            to.Width)
        {
            reason =
                $"Socket widths are incompatible: " +
                $"{from.Width} ↔ {to.Width}.";

            return false;
        }


        // -----------------------------------------------------
        // Role
        // -----------------------------------------------------

        if (!AreRolesCompatible(
                from.Role,
                to.Role
            ))
        {
            reason =
                $"Socket roles are incompatible: " +
                $"{from.Role} ↔ {to.Role}.";

            return false;
        }


        /*
         * Direction is deliberately NOT checked here.
         *
         * North / East / South / West are local Room directions.
         * The LayoutSolver will be allowed to rotate the destination
         * Room so both sockets face each other physically.
         */


        reason =
            "Compatible.";

        return true;
    }


    private static bool AreRolesCompatible(
        SocketRole from,
        SocketRole to
    )
    {
        if (from == SocketRole.Any ||
            to == SocketRole.Any)
        {
            return true;
        }


        if (from == SocketRole.Exit &&
            to == SocketRole.Entry)
        {
            return true;
        }


        if (from == SocketRole.Entry &&
            to == SocketRole.Exit)
        {
            return true;
        }


        return false;
    }


    // =========================================================
    // Connection State
    // =========================================================

    public static LevelConnectionSocketState
        GetConnectionState(
            LevelGraphData graph,
            LevelConnectionData connection,
            out string reason
        )
    {
        reason =
            string.Empty;


        if (graph == null ||
            connection == null)
        {
            reason =
                "Missing graph or connection.";

            return LevelConnectionSocketState
                .Invalid;
        }


        bool hasFrom =
            !string.IsNullOrWhiteSpace(
                connection.FromSocketId
            );


        bool hasTo =
            !string.IsNullOrWhiteSpace(
                connection.ToSocketId
            );


        if (!hasFrom &&
            !hasTo)
        {
            reason =
                "No sockets assigned.";

            return LevelConnectionSocketState
                .Unassigned;
        }


        if (!hasFrom ||
            !hasTo)
        {
            reason =
                "Only one side has a socket assigned.";

            return LevelConnectionSocketState
                .Partial;
        }


        LevelNodeData fromNode =
            graph.FindNode(
                connection.FromNodeId
            );


        LevelNodeData toNode =
            graph.FindNode(
                connection.ToNodeId
            );


        if (fromNode == null ||
            toNode == null)
        {
            reason =
                "Connection references a missing node.";

            return LevelConnectionSocketState
                .Invalid;
        }


        RoomSocketData fromSocket =
            FindSocket(
                fromNode,
                connection.FromSocketId
            );


        RoomSocketData toSocket =
            FindSocket(
                toNode,
                connection.ToSocketId
            );


        if (fromSocket == null ||
            toSocket == null)
        {
            reason =
                "Connection references a missing socket.";

            return LevelConnectionSocketState
                .Invalid;
        }


        if (IsSocketUsed(
                graph,
                fromNode.Id,
                fromSocket.Id,
                connection.Id
            ))
        {
            reason =
                $"Socket {fromSocket.Id} is already used by another connection.";

            return LevelConnectionSocketState
                .Invalid;
        }


        if (IsSocketUsed(
                graph,
                toNode.Id,
                toSocket.Id,
                connection.Id
            ))
        {
            reason =
                $"Socket {toSocket.Id} is already used by another connection.";

            return LevelConnectionSocketState
                .Invalid;
        }


        if (!AreCompatible(
                fromSocket,
                toSocket,
                out reason
            ))
        {
            return LevelConnectionSocketState
                .Invalid;
        }


        reason =
            "Socket pair ready for layout.";

        return LevelConnectionSocketState
            .Valid;
    }


    // =========================================================
    // Auto Assign
    // =========================================================

    public static bool TryFindCompatiblePair(
        LevelGraphData graph,
        LevelConnectionData connection,
        out RoomSocketData fromSocket,
        out RoomSocketData toSocket,
        out string error
    )
    {
        fromSocket =
            null;

        toSocket =
            null;

        error =
            string.Empty;


        if (graph == null ||
            connection == null)
        {
            error =
                "Missing graph or connection.";

            return false;
        }


        LevelNodeData fromNode =
            graph.FindNode(
                connection.FromNodeId
            );


        LevelNodeData toNode =
            graph.FindNode(
                connection.ToNodeId
            );


        if (fromNode == null ||
            toNode == null ||
            fromNode.Room == null ||
            toNode.Room == null)
        {
            error =
                "Connection nodes or rooms are missing.";

            return false;
        }


        IReadOnlyList<RoomSocketData> fromSockets =
            fromNode.Room.Sockets;


        IReadOnlyList<RoomSocketData> toSockets =
            toNode.Room.Sockets;


        if (fromSockets == null ||
            fromSockets.Count == 0)
        {
            error =
                $"{fromNode.Room.name} has no sockets.";

            return false;
        }


        if (toSockets == null ||
            toSockets.Count == 0)
        {
            error =
                $"{toNode.Room.name} has no sockets.";

            return false;
        }


        for (int i = 0;
             i < fromSockets.Count;
             i++)
        {
            RoomSocketData candidateFrom =
                fromSockets[i];


            if (candidateFrom == null)
            {
                continue;
            }


            if (IsSocketUsed(
                    graph,
                    fromNode.Id,
                    candidateFrom.Id,
                    connection.Id
                ))
            {
                continue;
            }


            for (int j = 0;
                 j < toSockets.Count;
                 j++)
            {
                RoomSocketData candidateTo =
                    toSockets[j];


                if (candidateTo == null)
                {
                    continue;
                }


                if (IsSocketUsed(
                        graph,
                        toNode.Id,
                        candidateTo.Id,
                        connection.Id
                    ))
                {
                    continue;
                }


                if (!AreCompatible(
                        candidateFrom,
                        candidateTo,
                        out _
                    ))
                {
                    continue;
                }


                fromSocket =
                    candidateFrom;

                toSocket =
                    candidateTo;

                return true;
            }
        }


        error =
            "No free compatible socket pair was found.";

        return false;
    }


    // =========================================================
    // Presentation
    // =========================================================

    public static string GetSocketSummary(
        RoomSocketData socket
    )
    {
        if (socket == null)
        {
            return "Unassigned";
        }


        string type =
            string.IsNullOrWhiteSpace(
                socket.Type
            )
                ? "Any Type"
                : socket.Type;


        return
            $"{GetSocketDisplayName(socket)} | " +
            $"{socket.Direction} | " +
            $"{socket.Role} | " +
            $"{type} | " +
            $"Width {socket.Width}";
    }


    public static string GetSocketDisplayName(
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
