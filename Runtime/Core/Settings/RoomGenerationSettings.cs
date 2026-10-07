using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "RoomGenerationSettings",
    menuName = "Procedural Level Toolkit/Generation/Room Generation Settings"
)]
public sealed class RoomGenerationSettings
    : ScriptableObject
{
    // =========================================================
    // Grid
    // =========================================================

    [Header("Grid")]

    [SerializeField, Min(0.01f)]
    private float cellSize = 1f;


    [SerializeField]
    private float layerHeight = 0f;


    [SerializeField]
    private bool centerRoomAtOrigin = true;


    // =========================================================
    // Groups
    // =========================================================

    [Header("Cell Groups")]

    [SerializeField]
    private bool scaleSinglePrefabGroupsToFootprint = true;


    // =========================================================
    // Sockets
    // =========================================================

    [Header("Sockets")]

    [SerializeField]
    private bool generateSockets = true;


    [SerializeField]
    private float socketHeight = 0.5f;


    // =========================================================
    // Mappings
    // =========================================================

    [Header("Cell Type Mappings")]

    [SerializeField]
    private List<CellTypePrefabMapping>
        cellMappings = new();


    // =========================================================
    // Properties
    // =========================================================

    public float CellSize =>
        Mathf.Max(
            0.01f,
            cellSize
        );


    public float LayerHeight =>
        layerHeight;


    public bool CenterRoomAtOrigin =>
        centerRoomAtOrigin;


    public bool ScaleSinglePrefabGroupsToFootprint =>
        scaleSinglePrefabGroupsToFootprint;


    public bool GenerateSockets =>
        generateSockets;


    public float SocketHeight =>
        socketHeight;


    public IReadOnlyList<CellTypePrefabMapping>
        CellMappings =>
            cellMappings;


    // =========================================================
    // Mapping
    // =========================================================

    public bool TryGetMapping(
        CellTypeDefinition cellType,
        out CellTypePrefabMapping mapping)
    {
        mapping =
            null;


        if (cellType == null)
            return false;


        foreach (
            CellTypePrefabMapping candidate
            in cellMappings)
        {
            if (
                candidate == null ||
                !candidate.Matches(
                    cellType
                ))
            {
                continue;
            }


            mapping =
                candidate;


            return true;
        }


        return false;
    }


    // =========================================================
    // Integer Grid Position
    // =========================================================

    public Vector3 CellToLocalPosition(
        RoomDefinition room,
        Vector2Int cell,
        int layerIndex)
    {
        return GridToLocalPosition(
            room,
            new Vector2(
                cell.x,
                cell.y
            ),
            layerIndex
        );
    }


    // =========================================================
    // Floating Grid Position
    // =========================================================

    public Vector3 GridToLocalPosition(
        RoomDefinition room,
        Vector2 gridPosition,
        int layerIndex)
    {
        float x =
            gridPosition.x *
            CellSize;


        // =====================================================
        // Coordinate convention
        // =====================================================
        //
        // Room Editor:
        //
        // (0,0) --------> +X
        //   |
        //   |
        //   v
        //  +Y
        //
        // Therefore:
        //
        // y = 0            -> North
        // y = Height - 1   -> South
        //
        // Unity / Layout convention:
        //
        // North -> +Z
        // South -> -Z
        //
        // Grid Y must therefore be inverted when converted
        // into Unity Z.
        // =====================================================

        float z =
            -gridPosition.y *
            CellSize;


        if (
            CenterRoomAtOrigin &&
            room != null)
        {
            x -=
                (
                    room.Width -
                    1
                )
                *
                CellSize
                *
                0.5f;


            z +=
                (
                    room.Height -
                    1
                )
                *
                CellSize
                *
                0.5f;
        }


        float y =
            layerIndex *
            LayerHeight;


        return new Vector3(
            x,
            y,
            z
        );
    }


    // =========================================================
    // Socket Position
    // =========================================================

    public Vector3 SocketToLocalPosition(
        RoomDefinition room,
        RoomSocketData socket)
    {
        if (socket == null)
            return Vector3.zero;


        Vector3 position =
            CellToLocalPosition(
                room,
                socket.Position,
                0
            );


        position.y +=
            SocketHeight;


        Vector3 directionOffset =
            socket.Direction
            switch
            {
                SocketDirection.North =>
                    Vector3.forward,

                SocketDirection.East =>
                    Vector3.right,

                SocketDirection.South =>
                    Vector3.back,

                SocketDirection.West =>
                    Vector3.left,

                _ =>
                    Vector3.zero
            };


        position +=
            directionOffset *
            CellSize *
            0.5f;


        return position;
    }


    // =========================================================
    // Socket Rotation
    // =========================================================

    public Quaternion SocketToLocalRotation(
        RoomSocketData socket)
    {
        if (socket == null)
            return Quaternion.identity;


        float rotationY =
            socket.Direction
            switch
            {
                SocketDirection.North =>
                    0f,

                SocketDirection.East =>
                    90f,

                SocketDirection.South =>
                    180f,

                SocketDirection.West =>
                    270f,

                _ =>
                    0f
            };


        return Quaternion.Euler(
            0f,
            rotationY,
            0f
        );
    }
}