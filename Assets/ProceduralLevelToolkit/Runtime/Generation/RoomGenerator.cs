using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class RoomGenerator
{
    private readonly struct CellGenerationKey
        : IEquatable<CellGenerationKey>
    {
        public readonly int LayerIndex;
        public readonly Vector2Int Cell;

        public CellGenerationKey(
            int layerIndex,
            Vector2Int cell)
        {
            LayerIndex = layerIndex;
            Cell = cell;
        }

        public bool Equals(
            CellGenerationKey other)
        {
            return
                LayerIndex == other.LayerIndex &&
                Cell == other.Cell;
        }

        public override bool Equals(
            object obj)
        {
            return
                obj is CellGenerationKey other &&
                Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return
                    (LayerIndex * 397) ^
                    Cell.GetHashCode();
            }
        }
    }

    public RoomGenerationResult Generate(
        RoomDefinition room,
        RoomGenerationSettings settings,
        Transform parent = null)
    {
        if (room == null)
            throw new ArgumentNullException(nameof(room));

        if (settings == null)
            throw new ArgumentNullException(nameof(settings));

        room.EnsureIntegrity();

        GameObject root =
            new GameObject(
                $"Room_{room.name}"
            );

        root.transform.SetParent(
            parent,
            false
        );

        RoomModule roomModule =
            root.AddComponent<RoomModule>();

        roomModule.Configure(
            room,
            settings
        );

        Transform generatedRoot =
            CreateChild(
                root.transform,
                "Generated"
            );

        Transform manualRoot =
            CreateChild(
                root.transform,
                "Manual"
            );

        RoomGenerationResult result =
            new(
                root,
                generatedRoot,
                manualRoot
            );

        HashSet<CellGenerationKey>
            consumedCells =
                new();

        GenerateSinglePrefabGroups(
            room,
            settings,
            generatedRoot,
            consumedCells,
            result
        );

        GenerateLayers(
            room,
            settings,
            generatedRoot,
            consumedCells,
            result
        );

        if (settings.GenerateSockets)
        {
            GenerateSockets(
                room,
                settings,
                generatedRoot,
                result
            );
        }

        return result;
    }

    private void GenerateSinglePrefabGroups(
        RoomDefinition room,
        RoomGenerationSettings settings,
        Transform generatedRoot,
        HashSet<CellGenerationKey> consumedCells,
        RoomGenerationResult result)
    {
        if (
            room.Groups == null ||
            room.Groups.Count == 0)
        {
            return;
        }

        Transform groupsRoot =
            CreateChild(
                generatedRoot,
                "Groups"
            );

        int groupIndex = 0;

        foreach (CellGroupData group in room.Groups)
        {
            if (group == null)
                continue;

            groupIndex++;

            if (!group.Enabled)
                continue;

            if (
                group.GenerationMode !=
                CellGroupGenerationMode.SinglePrefab)
            {
                continue;
            }

            if (group.Prefab == null)
            {
                result.AddWarning(
                    $"Group '{group.Label}' requires a prefab."
                );

                continue;
            }

            if (
                !TryGetGroupBounds(
                    group,
                    out Vector2Int min,
                    out Vector2Int max
                ))
            {
                result.AddWarning(
                    $"Group '{group.Label}' contains no cells."
                );

                continue;
            }

            int layerIndex =
                ResolveGroupLayerIndex(
                    room,
                    group,
                    result
                );

            int footprintWidth =
                max.x - min.x + 1;

            int footprintHeight =
                max.y - min.y + 1;

            Vector2 center =
                new Vector2(
                    (min.x + max.x) * 0.5f,
                    (min.y + max.y) * 0.5f
                );

            Vector3 position =
                settings.GridToLocalPosition(
                    room,
                    center,
                    layerIndex
                );

            position +=
                group.PositionOffset;

            GameObject instance =
                InstantiatePrefab(
                    group.Prefab,
                    groupsRoot
                );

            instance.name =
                string.IsNullOrWhiteSpace(
                    group.Label
                )
                    ? $"Group_{groupIndex}"
                    : group.Label;

            instance.transform.localPosition =
                position;

            instance.transform.localRotation =
                Quaternion.Euler(
                    group.Rotation
                );

            Vector3 scale =
                instance.transform.localScale;

            if (
                settings.ScaleSinglePrefabGroupsToFootprint &&
                group.FitPrefabToFootprint)
            {
                Vector3 footprintScale =
                    GetGroupFootprintScale(
                        group,
                        footprintWidth,
                        footprintHeight
                    );

                scale =
                    Vector3.Scale(
                        scale,
                        footprintScale
                    );
            }

            scale =
                Vector3.Scale(
                    scale,
                    group.ScaleMultiplier
                );

            instance.transform.localScale =
                scale;

            result.AddGeneratedObject(
                instance
            );

            foreach (Vector2Int cell in group.Cells)
            {
                consumedCells.Add(
                    new CellGenerationKey(
                        layerIndex,
                        cell
                    )
                );
            }
        }
    }

    private int ResolveGroupLayerIndex(
        RoomDefinition room,
        CellGroupData group,
        RoomGenerationResult result)
    {
        if (
            !string.IsNullOrWhiteSpace(
                group.LayerId
            ))
        {
            for (
                int index = 0;
                index < room.Layers.Count;
                index++)
            {
                RoomLayerData layer =
                    room.Layers[index];

                if (
                    layer != null &&
                    layer.Id == group.LayerId)
                {
                    return index;
                }
            }

            result.AddWarning(
                $"Group '{group.Label}' references missing Layer " +
                $"'{group.LayerId}'. Falling back to a compatible Layer."
            );
        }
        else
        {
            result.AddWarning(
                $"Group '{group.Label}' has no LayerId. " +
                $"Falling back to a compatible Layer."
            );
        }

        return
            FindLegacyGroupLayerIndex(
                room,
                group
            );
    }

    private int FindLegacyGroupLayerIndex(
        RoomDefinition room,
        CellGroupData group)
    {
        for (
            int layerIndex = 0;
            layerIndex < room.Layers.Count;
            layerIndex++)
        {
            RoomLayerData layer =
                room.Layers[layerIndex];

            if (layer == null)
                continue;

            foreach (Vector2Int cell in group.Cells)
            {
                CellData cellData =
                    room.GetCell(
                        layer,
                        cell
                    );

                if (
                    cellData != null &&
                    cellData.Type != null)
                {
                    return layerIndex;
                }
            }
        }

        return 0;
    }

    private static bool TryGetGroupBounds(
        CellGroupData group,
        out Vector2Int min,
        out Vector2Int max)
    {
        min = default;
        max = default;

        if (
            group == null ||
            group.Cells == null ||
            group.Cells.Count == 0)
        {
            return false;
        }

        min = group.Cells[0];
        max = group.Cells[0];

        for (
            int i = 1;
            i < group.Cells.Count;
            i++)
        {
            Vector2Int cell =
                group.Cells[i];

            min.x =
                Mathf.Min(
                    min.x,
                    cell.x
                );

            min.y =
                Mathf.Min(
                    min.y,
                    cell.y
                );

            max.x =
                Mathf.Max(
                    max.x,
                    cell.x
                );

            max.y =
                Mathf.Max(
                    max.y,
                    cell.y
                );
        }

        return true;
    }

    private static Vector3 GetGroupFootprintScale(
        CellGroupData group,
        int width,
        int height)
    {
        float rotationY =
            Mathf.Repeat(
                group.Rotation.y,
                360f
            );

        bool quarterTurn =
            Mathf.Approximately(
                rotationY,
                90f
            )
            ||
            Mathf.Approximately(
                rotationY,
                270f
            );

        float x =
            quarterTurn
                ? height
                : width;

        float z =
            quarterTurn
                ? width
                : height;

        return new Vector3(
            Mathf.Max(1f, x),
            1f,
            Mathf.Max(1f, z)
        );
    }

    private void GenerateLayers(
        RoomDefinition room,
        RoomGenerationSettings settings,
        Transform generatedRoot,
        HashSet<CellGenerationKey> consumedCells,
        RoomGenerationResult result)
    {
        Transform layersRoot =
            CreateChild(
                generatedRoot,
                "Layers"
            );

        for (
            int layerIndex = 0;
            layerIndex < room.Layers.Count;
            layerIndex++)
        {
            RoomLayerData layer =
                room.Layers[layerIndex];

            if (layer == null)
                continue;

            Transform layerRoot =
                CreateChild(
                    layersRoot,
                    GetLayerObjectName(
                        layer,
                        layerIndex
                    )
                );

            GenerateLayerCells(
                room,
                layer,
                layerIndex,
                settings,
                layerRoot,
                consumedCells,
                result
            );
        }
    }

    private void GenerateLayerCells(
        RoomDefinition room,
        RoomLayerData layer,
        int layerIndex,
        RoomGenerationSettings settings,
        Transform layerRoot,
        HashSet<CellGenerationKey> consumedCells,
        RoomGenerationResult result)
    {
        for (
            int y = 0;
            y < room.Height;
            y++)
        {
            for (
                int x = 0;
                x < room.Width;
                x++)
            {
                Vector2Int position =
                    new(
                        x,
                        y
                    );

                CellGenerationKey key =
                    new(
                        layerIndex,
                        position
                    );

                if (consumedCells.Contains(key))
                    continue;

                CellData cell =
                    room.GetCell(
                        layer,
                        position
                    );

                if (
                    cell == null ||
                    !cell.Enabled ||
                    cell.Type == null)
                {
                    continue;
                }

                if (
                    !settings.TryGetMapping(
                        cell.Type,
                        out CellTypePrefabMapping mapping
                    ))
                {
                    result.AddWarning(
                        $"No mapping for CellType '{cell.Type.DisplayName}'."
                    );

                    continue;
                }

                if (mapping.Prefab == null)
                {
                    result.AddWarning(
                        $"CellType '{cell.Type.DisplayName}' has no prefab."
                    );

                    continue;
                }

                GameObject instance =
                    InstantiatePrefab(
                        mapping.Prefab,
                        layerRoot
                    );

                instance.name =
                    $"{cell.Type.DisplayName}_{x}_{y}";

                Vector3 localPosition =
                    settings.CellToLocalPosition(
                        room,
                        position,
                        layerIndex
                    );

                localPosition +=
                    mapping.PositionOffset;

                instance.transform.localPosition =
                    localPosition;

                instance.transform.localRotation =
                    Quaternion.Euler(
                        mapping.RotationOffset
                    );

                instance.transform.localScale =
                    Vector3.Scale(
                        instance.transform.localScale,
                        mapping.ScaleMultiplier
                    );

                result.AddGeneratedObject(
                    instance
                );
            }
        }
    }

    private void GenerateSockets(
        RoomDefinition room,
        RoomGenerationSettings settings,
        Transform generatedRoot,
        RoomGenerationResult result)
    {
        if (
            room.Sockets == null ||
            room.Sockets.Count == 0)
        {
            return;
        }

        Transform socketsRoot =
            CreateChild(
                generatedRoot,
                "Sockets"
            );

        int index = 0;

        foreach (RoomSocketData socket in room.Sockets)
        {
            if (socket == null)
                continue;

            index++;

            GameObject socketObject =
                new GameObject(
                    GetSocketObjectName(
                        socket,
                        index
                    )
                );

            socketObject.transform.SetParent(
                socketsRoot,
                false
            );

            socketObject.transform.localPosition =
                settings.SocketToLocalPosition(
                    room,
                    socket
                );

            socketObject.transform.localRotation =
                settings.SocketToLocalRotation(
                    socket
                );

            RoomSocketComponent component =
                socketObject.AddComponent<
                    RoomSocketComponent
                >();

            component.Configure(
                socket
            );

            result.AddGeneratedObject(
                socketObject
            );
        }
    }

    private static Transform CreateChild(
        Transform parent,
        string name)
    {
        GameObject child =
            new GameObject(
                name
            );

        child.transform.SetParent(
            parent,
            false
        );

        return child.transform;
    }

    private static GameObject InstantiatePrefab(
        GameObject prefab,
        Transform parent)
    {
        GameObject instance =
            UnityEngine.Object.Instantiate(
                prefab,
                parent
            );

        instance.transform.localPosition =
            Vector3.zero;

        return instance;
    }

    private static string GetLayerObjectName(
        RoomLayerData layer,
        int index)
    {
        string layerName =
            string.IsNullOrWhiteSpace(
                layer.DisplayName
            )
                ? "Layer"
                : layer.DisplayName;

        return
            $"{index:00}_{layerName}";
    }

    private static string GetSocketObjectName(
        RoomSocketData socket,
        int index)
    {
        string id =
            string.IsNullOrWhiteSpace(
                socket.Id
            )
                ? index.ToString()
                : socket.Id;

        return
            $"Socket_{id}";
    }
}
