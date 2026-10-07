using UnityEngine;

public static class LayoutGeometryUtility
{
    private const float Epsilon =
        0.0001f;


    // =========================================================
    // Spatial Convention
    // =========================================================
    //
    // Room Editor:
    //
    // (0,0) --------> +X
    //   |
    //   |
    //   v
    //  +Y
    //
    // North = y == 0
    // South = y == Height - 1
    //
    // Layout / Unity:
    //
    // logical X -> Unity X
    // logical North -> Unity +Z
    //
    // Therefore Room Editor Y must be inverted when converted
    // to the spatial Z axis.
    //
    // A LevelModuleInstanceData.Position represents the CENTER
    // pivot of the Room, not its bottom-left corner.
    //
    // Room local bounds:
    //
    // X = [-Width / 2, +Width / 2]
    // Z = [-Height / 2, +Height / 2]
    //
    // Socket anchors are projected onto those real boundaries.
    // =========================================================


    // =========================================================
    // Socket
    // =========================================================

    public static Vector2 GetSocketLocalAnchor(
        RoomDefinition room,
        RoomSocketData socket
    )
    {
        if (room == null ||
            socket == null)
        {
            return Vector2.zero;
        }


        float halfWidth =
            room.Width *
            0.5f;


        float halfHeight =
            room.Height *
            0.5f;


        Vector2Int cell =
            socket.Position;


        /*
         * X follows the Room Editor directly:
         *
         * x = 0 -> West
         * x = Width - 1 -> East
         */

        float localCellX =
            cell.x +
            0.5f -
            halfWidth;


        /*
         * Room Editor Y increases downward.
         *
         * Spatial Z increases toward North.
         *
         * Therefore:
         *
         * y = 0
         *     -> positive local spatial Y / Unity +Z
         *
         * y = Height - 1
         *     -> negative local spatial Y / Unity -Z
         */

        float localCellY =
            halfHeight -
            cell.y -
            0.5f;


        switch (socket.Direction)
        {
            case SocketDirection.North:
                return new Vector2(
                    Mathf.Clamp(
                        localCellX,
                        -halfWidth,
                        halfWidth
                    ),
                    halfHeight
                );


            case SocketDirection.East:
                return new Vector2(
                    halfWidth,
                    Mathf.Clamp(
                        localCellY,
                        -halfHeight,
                        halfHeight
                    )
                );


            case SocketDirection.South:
                return new Vector2(
                    Mathf.Clamp(
                        localCellX,
                        -halfWidth,
                        halfWidth
                    ),
                    -halfHeight
                );


            case SocketDirection.West:
                return new Vector2(
                    -halfWidth,
                    Mathf.Clamp(
                        localCellY,
                        -halfHeight,
                        halfHeight
                    )
                );


            default:
                return new Vector2(
                    localCellX,
                    localCellY
                );
        }
    }


    public static Vector2 GetModulePosition2D(
        LevelModuleInstanceData module
    )
    {
        if (module == null)
        {
            return Vector2.zero;
        }


        return new Vector2(
            module.Position.x,
            module.Position.z
        );
    }


    public static Vector2 GetWorldSocketAnchor2D(
        LevelModuleInstanceData module,
        RoomSocketData socket
    )
    {
        if (module == null ||
            module.Room == null ||
            socket == null)
        {
            return Vector2.zero;
        }


        Vector2 local =
            GetSocketLocalAnchor(
                module.Room,
                socket
            );


        Vector2 rotated =
            RotatePointClockwise(
                local,
                module.QuarterTurns
            );


        return
            GetModulePosition2D(
                module
            )
            +
            rotated;
    }


    public static Vector3 GetWorldSocketAnchor3D(
        LevelModuleInstanceData module,
        RoomSocketData socket
    )
    {
        Vector2 point =
            GetWorldSocketAnchor2D(
                module,
                socket
            );


        return new Vector3(
            point.x,
            0f,
            point.y
        );
    }


    // =========================================================
    // Room Footprint
    // =========================================================

    public static Rect GetWorldBounds2D(
        LevelModuleInstanceData module
    )
    {
        if (module == null ||
            module.Room == null)
        {
            return new Rect();
        }


        float halfWidth =
            module.Room.Width *
            0.5f;


        float halfHeight =
            module.Room.Height *
            0.5f;


        Vector2 p0 =
            TransformLocalPoint(
                module,
                new Vector2(
                    -halfWidth,
                    -halfHeight
                )
            );


        Vector2 p1 =
            TransformLocalPoint(
                module,
                new Vector2(
                    halfWidth,
                    -halfHeight
                )
            );


        Vector2 p2 =
            TransformLocalPoint(
                module,
                new Vector2(
                    halfWidth,
                    halfHeight
                )
            );


        Vector2 p3 =
            TransformLocalPoint(
                module,
                new Vector2(
                    -halfWidth,
                    halfHeight
                )
            );


        float minX =
            Mathf.Min(
                p0.x,
                p1.x,
                p2.x,
                p3.x
            );


        float maxX =
            Mathf.Max(
                p0.x,
                p1.x,
                p2.x,
                p3.x
            );


        float minY =
            Mathf.Min(
                p0.y,
                p1.y,
                p2.y,
                p3.y
            );


        float maxY =
            Mathf.Max(
                p0.y,
                p1.y,
                p2.y,
                p3.y
            );


        return Rect.MinMaxRect(
            minX,
            minY,
            maxX,
            maxY
        );
    }


    public static bool OverlapsArea(
        Rect a,
        Rect b
    )
    {
        float overlapX =
            Mathf.Min(
                a.xMax,
                b.xMax
            )
            -
            Mathf.Max(
                a.xMin,
                b.xMin
            );


        float overlapY =
            Mathf.Min(
                a.yMax,
                b.yMax
            )
            -
            Mathf.Max(
                a.yMin,
                b.yMin
            );


        /*
         * Sharing a wall or socket boundary is valid.
         * Only positive-area intersection is an overlap.
         */

        return
            overlapX >
            Epsilon
            &&
            overlapY >
            Epsilon;
    }


    public static Vector2 TransformLocalPoint(
        LevelModuleInstanceData module,
        Vector2 localPoint
    )
    {
        return
            GetModulePosition2D(
                module
            )
            +
            RotatePointClockwise(
                localPoint,
                module.QuarterTurns
            );
    }


    // =========================================================
    // Rotation
    // =========================================================
    //
    // QuarterTurns follow the same convention as Unity positive
    // Y rotation:
    //
    // 0 -> 0°
    // 1 -> +90°
    // 2 -> +180°
    // 3 -> +270°
    //
    // In X/Z top-down coordinates this is clockwise:
    //
    // (x, z) -> (z, -x)
    // =========================================================

    public static Vector2 RotatePointClockwise(
        Vector2 point,
        int quarterTurns
    )
    {
        quarterTurns =
            NormalizeQuarterTurns(
                quarterTurns
            );


        switch (quarterTurns)
        {
            case 0:
                return point;


            case 1:
                return new Vector2(
                    point.y,
                    -point.x
                );


            case 2:
                return new Vector2(
                    -point.x,
                    -point.y
                );


            case 3:
                return new Vector2(
                    -point.y,
                    point.x
                );


            default:
                return point;
        }
    }


    public static int GetDirectionIndex(
        SocketDirection direction
    )
    {
        switch (direction)
        {
            case SocketDirection.North:
                return 0;


            case SocketDirection.East:
                return 1;


            case SocketDirection.South:
                return 2;


            case SocketDirection.West:
                return 3;


            default:
                return 0;
        }
    }


    public static SocketDirection RotateDirection(
        SocketDirection direction,
        int quarterTurns
    )
    {
        int index =
            GetDirectionIndex(
                direction
            );


        index =
            NormalizeQuarterTurns(
                index +
                quarterTurns
            );


        switch (index)
        {
            case 0:
                return SocketDirection.North;


            case 1:
                return SocketDirection.East;


            case 2:
                return SocketDirection.South;


            case 3:
                return SocketDirection.West;


            default:
                return SocketDirection.North;
        }
    }


    public static SocketDirection Opposite(
        SocketDirection direction
    )
    {
        return RotateDirection(
            direction,
            2
        );
    }


    public static int FindRotationToFace(
        SocketDirection localDirection,
        SocketDirection desiredWorldDirection
    )
    {
        int local =
            GetDirectionIndex(
                localDirection
            );


        int desired =
            GetDirectionIndex(
                desiredWorldDirection
            );


        return NormalizeQuarterTurns(
            desired -
            local
        );
    }


    public static int NormalizeQuarterTurns(
        int value
    )
    {
        value %=
            4;


        if (value <
            0)
        {
            value +=
                4;
        }


        return value;
    }
}