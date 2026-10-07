using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "RoomDefinition",
    menuName = "Procedural Level Toolkit/Room/Room Definition"
)]
public sealed class RoomDefinition
    : ScriptableObject
{
    [SerializeField, Min(1)]
    private int width = 20;

    [SerializeField, Min(1)]
    private int height = 20;

    [SerializeField]
    private List<RoomLayerData> layers =
        new();

    [SerializeField]
    private List<CellGroupData> groups =
        new();

    [SerializeField]
    private List<RoomSocketData> sockets =
        new();


    public int Width =>
        width;

    public int Height =>
        height;

    public IReadOnlyList<RoomLayerData> Layers =>
        layers;

    public IReadOnlyList<CellGroupData> Groups =>
        groups;

    public IReadOnlyList<RoomSocketData> Sockets =>
        sockets;


    public void Initialize(
        int initialWidth = 20,
        int initialHeight = 20)
    {
        width =
            Mathf.Max(
                1,
                initialWidth
            );


        height =
            Mathf.Max(
                1,
                initialHeight
            );


        EnsureIntegrity();
    }


    public void EnsureIntegrity()
    {
        width =
            Mathf.Max(
                1,
                width
            );


        height =
            Mathf.Max(
                1,
                height
            );


        layers ??=
            new List<RoomLayerData>();


        groups ??=
            new List<CellGroupData>();


        sockets ??=
            new List<RoomSocketData>();


        if (
            layers.Count ==
            0)
        {
            layers.Add(
                new RoomLayerData(
                    "Base",
                    width,
                    height
                )
            );
        }


        foreach (
            RoomLayerData layer
            in layers)
        {
            layer?.EnsureCellCount(
                width,
                height
            );
        }


        foreach (
            CellGroupData group
            in groups)
        {
            group?.EnsureIntegrity();
        }


        foreach (
            RoomSocketData socket
            in sockets)
        {
            socket?.EnsureIntegrity();
        }
    }


    // =========================================================
    // Resize
    // =========================================================

    public void Resize(
        int newWidth,
        int newHeight)
    {
        newWidth =
            Mathf.Max(
                1,
                newWidth
            );


        newHeight =
            Mathf.Max(
                1,
                newHeight
            );


        if (
            newWidth == width &&
            newHeight == height)
        {
            return;
        }


        int oldWidth =
            width;

        int oldHeight =
            height;


        foreach (
            RoomLayerData layer
            in layers)
        {
            layer?.Resize(
                oldWidth,
                oldHeight,
                newWidth,
                newHeight
            );
        }


        width =
            newWidth;

        height =
            newHeight;


        foreach (
            CellGroupData group
            in groups)
        {
            group?.RemoveCellsOutside(
                width,
                height
            );
        }


        groups.RemoveAll(
            group =>
                group == null ||
                group.Cells.Count == 0
        );


        sockets.RemoveAll(
            socket =>
                socket == null ||
                !IsInside(
                    socket.Position
                )
        );
    }


    // =========================================================
    // Layers
    // =========================================================

    public RoomLayerData AddLayer(
        string displayName)
    {
        RoomLayerData layer =
            new(
                displayName,
                width,
                height
            );


        layers.Add(
            layer
        );


        return layer;
    }


    public RoomLayerData DuplicateLayer(
        RoomLayerData source)
    {
        int index =
            layers.IndexOf(
                source
            );


        if (index < 0)
            return null;


        RoomLayerData clone =
            source.Clone(
                width,
                height,
                source.DisplayName +
                " Copy"
            );


        layers.Insert(
            index + 1,
            clone
        );


        return clone;
    }


    public bool RemoveLayer(
        RoomLayerData layer)
    {
        if (
            layers.Count <=
            1)
        {
            return false;
        }


        return
            layers.Remove(
                layer
            );
    }


    public bool MoveLayer(
        RoomLayerData layer,
        int direction)
    {
        int index =
            layers.IndexOf(
                layer
            );


        if (index < 0)
            return false;


        int target =
            Mathf.Clamp(
                index + direction,
                0,
                layers.Count - 1
            );


        if (target == index)
            return false;


        layers.RemoveAt(
            index
        );


        layers.Insert(
            target,
            layer
        );


        return true;
    }


    // =========================================================
    // Cells
    // =========================================================

    public CellData GetCell(
        RoomLayerData layer,
        Vector2Int position)
    {
        if (layer == null)
            return null;


        return
            layer.GetCell(
                width,
                height,
                position
            );
    }


    // =========================================================
    // Groups
    // =========================================================

    public CellGroupData AddGroup(
        IEnumerable<Vector2Int> cells,
        string label)
    {
        CellGroupData group =
            new(
                cells,
                label
            );


        groups.Add(
            group
        );


        return group;
    }


    public bool RemoveGroup(
        CellGroupData group)
    {
        return
            groups.Remove(
                group
            );
    }


    public CellGroupData GetGroupAtCell(
        Vector2Int position)
    {
        foreach (
            CellGroupData group
            in groups)
        {
            if (
                group != null &&
                group.Contains(
                    position
                ))
            {
                return group;
            }
        }


        return null;
    }


    public CellGroupData GetGroupById(
        string id)
    {
        if (
            string.IsNullOrWhiteSpace(
                id
            ))
        {
            return null;
        }


        foreach (
            CellGroupData group
            in groups)
        {
            if (
                group != null &&
                group.Id == id)
            {
                return group;
            }
        }


        return null;
    }


    // =========================================================
    // Sockets
    // =========================================================

    public RoomSocketData AddSocket(
        Vector2Int position)
    {
        if (!TryGetDefaultSocketDirection(
                position,
                out SocketDirection direction
            ))
        {
            return null;
        }


        return AddSocket(
            position,
            direction
        );
    }


    public RoomSocketData AddSocket(
        Vector2Int position,
        SocketDirection direction)
    {
        if (!IsInside(
                position
            ) ||
            !IsSocketDirectionValidForPosition(
                position,
                direction
            ))
        {
            return null;
        }


        RoomSocketData existing =
            GetSocketAt(
                position
            );


        if (existing != null)
        {
            return existing;
        }


        RoomSocketData socket =
            new(
                position
            );


        socket.SetDirection(
            direction
        );


        sockets.Add(
            socket
        );


        return socket;
    }


    public bool TryGetDefaultSocketDirection(
        Vector2Int position,
        out SocketDirection direction)
    {
        direction =
            SocketDirection.North;


        if (!IsInside(
                position
            ))
        {
            return false;
        }


        // UI grid convention:
        // Y = 0 is the top row, Y increases downward.
        // X = 0 is the left column, X increases to the right.

        if (position.y ==
            0)
        {
            direction =
                SocketDirection.North;


            return true;
        }


        if (position.x ==
            width - 1)
        {
            direction =
                SocketDirection.East;


            return true;
        }


        if (position.y ==
            height - 1)
        {
            direction =
                SocketDirection.South;


            return true;
        }


        if (position.x ==
            0)
        {
            direction =
                SocketDirection.West;


            return true;
        }


        return false;
    }


    public bool IsSocketDirectionValidForPosition(
        Vector2Int position,
        SocketDirection direction)
    {
        if (!IsInside(
                position
            ))
        {
            return false;
        }


        return direction
            switch
        {
            SocketDirection.North =>
                position.y ==
                0,

            SocketDirection.East =>
                position.x ==
                width - 1,

            SocketDirection.South =>
                position.y ==
                height - 1,

            SocketDirection.West =>
                position.x ==
                0,

            _ =>
                false
        };
    }


    public bool RemoveSocket(
        RoomSocketData socket)
    {
        return
            sockets.Remove(
                socket
            );
    }


    public RoomSocketData GetSocketAt(
        Vector2Int position)
    {
        foreach (
            RoomSocketData socket
            in sockets)
        {
            if (
                socket != null &&
                socket.Position ==
                position)
            {
                return socket;
            }
        }


        return null;
    }


    public RoomSocketData GetSocketById(
        string id)
    {
        if (
            string.IsNullOrWhiteSpace(
                id
            ))
        {
            return null;
        }


        foreach (
            RoomSocketData socket
            in sockets)
        {
            if (
                socket != null &&
                socket.Id == id)
            {
                return socket;
            }
        }


        return null;
    }


    // =========================================================
    // Bounds
    // =========================================================

    public bool IsInside(
        Vector2Int position)
    {
        return
            position.x >= 0 &&
            position.y >= 0 &&
            position.x < width &&
            position.y < height;
    }
}