using System.Collections.Generic;
using UnityEngine;

public static class RoomValidator
{
    public static RoomValidationResult Validate(
        RoomDefinition room,
        RoomGenerationSettings settings = null,
        bool forBuild = false)
    {
        RoomValidationResult result = new();

        if (room == null)
        {
            result.Add(
                RoomValidationSeverity.Error,
                "ROOM_NULL",
                "RoomDefinition is missing."
            );

            return result;
        }

        ValidateDimensions(room, result);
        ValidateLayers(room, result);
        ValidateGroups(room, result, forBuild);
        ValidateSockets(room, result);

        if (settings != null)
        {
            ValidateGenerationMappings(
                room,
                settings,
                result,
                forBuild
            );
        }

        return result;
    }

    private static void ValidateDimensions(
        RoomDefinition room,
        RoomValidationResult result)
    {
        if (room.Width <= 0 || room.Height <= 0)
        {
            result.Add(
                RoomValidationSeverity.Error,
                "ROOM_DIMENSIONS",
                "Room dimensions must be greater than zero."
            );
        }
    }

    private static void ValidateLayers(
        RoomDefinition room,
        RoomValidationResult result)
    {
        if (room.Layers == null || room.Layers.Count == 0)
        {
            result.Add(
                RoomValidationSeverity.Error,
                "ROOM_NO_LAYERS",
                "Room contains no Layer."
            );

            return;
        }

        HashSet<string> ids = new();
        int expectedCellCount = Mathf.Max(0, room.Width * room.Height);

        for (int index = 0; index < room.Layers.Count; index++)
        {
            RoomLayerData layer = room.Layers[index];

            if (layer == null)
            {
                result.Add(
                    RoomValidationSeverity.Error,
                    "LAYER_NULL",
                    $"Layer index {index} is null."
                );

                continue;
            }

            if (string.IsNullOrWhiteSpace(layer.Id))
            {
                result.Add(
                    RoomValidationSeverity.Error,
                    "LAYER_ID_EMPTY",
                    $"Layer '{layer.DisplayName}' has no Id."
                );
            }
            else if (!ids.Add(layer.Id))
            {
                result.Add(
                    RoomValidationSeverity.Error,
                    "LAYER_ID_DUPLICATE",
                    $"Layer Id '{layer.Id}' is duplicated."
                );
            }

            if (
                layer.Cells == null ||
                layer.Cells.Count != expectedCellCount)
            {
                result.Add(
                    RoomValidationSeverity.Error,
                    "LAYER_CELL_COUNT",
                    $"Layer '{layer.DisplayName}' contains an invalid number of cells."
                );
            }
        }
    }

    private static void ValidateGroups(
        RoomDefinition room,
        RoomValidationResult result,
        bool forBuild)
    {
        if (room.Groups == null)
            return;

        HashSet<string> groupIds = new();
        HashSet<string> occupiedSinglePrefabCells = new();

        foreach (CellGroupData group in room.Groups)
        {
            if (group == null)
            {
                result.Add(
                    RoomValidationSeverity.Error,
                    "GROUP_NULL",
                    "Room contains a null CellGroup."
                );

                continue;
            }

            if (string.IsNullOrWhiteSpace(group.Id))
            {
                result.Add(
                    RoomValidationSeverity.Error,
                    "GROUP_ID_EMPTY",
                    $"Group '{group.Label}' has no Id."
                );
            }
            else if (!groupIds.Add(group.Id))
            {
                result.Add(
                    RoomValidationSeverity.Error,
                    "GROUP_ID_DUPLICATE",
                    $"Group Id '{group.Id}' is duplicated."
                );
            }

            if (string.IsNullOrWhiteSpace(group.LayerId))
            {
                result.Add(
                    RoomValidationSeverity.Warning,
                    "GROUP_LAYER_MISSING",
                    $"Group '{group.Label}' is not explicitly assigned to a Layer."
                );
            }
            else if (FindLayerById(room, group.LayerId) == null)
            {
                result.Add(
                    RoomValidationSeverity.Error,
                    "GROUP_LAYER_INVALID",
                    $"Group '{group.Label}' references missing Layer '{group.LayerId}'."
                );
            }

            if (group.Cells == null || group.Cells.Count == 0)
            {
                result.Add(
                    RoomValidationSeverity.Error,
                    "GROUP_EMPTY",
                    $"Group '{group.Label}' contains no cells."
                );

                continue;
            }

            foreach (Vector2Int cell in group.Cells)
            {
                if (!room.IsInside(cell))
                {
                    result.Add(
                        RoomValidationSeverity.Error,
                        "GROUP_CELL_OUTSIDE",
                        $"Group '{group.Label}' contains out-of-bounds Cell {cell}."
                    );
                }

                if (
                    group.GenerationMode ==
                    CellGroupGenerationMode.SinglePrefab &&
                    !string.IsNullOrWhiteSpace(group.LayerId))
                {
                    string key =
                        $"{group.LayerId}:{cell.x}:{cell.y}";

                    if (!occupiedSinglePrefabCells.Add(key))
                    {
                        result.Add(
                            RoomValidationSeverity.Error,
                            "GROUP_OVERLAP",
                            $"SinglePrefab groups overlap at Cell {cell} on Layer '{group.LayerId}'."
                        );
                    }
                }
            }

            if (
                group.Enabled &&
                group.GenerationMode ==
                CellGroupGenerationMode.SinglePrefab &&
                group.Prefab == null)
            {
                result.Add(
                    forBuild
                        ? RoomValidationSeverity.Error
                        : RoomValidationSeverity.Warning,
                    "GROUP_PREFAB_MISSING",
                    $"Group '{group.Label}' is SinglePrefab but has no prefab."
                );
            }
        }
    }

    private static void ValidateSockets(
        RoomDefinition room,
        RoomValidationResult result)
    {
        if (room.Sockets == null)
            return;

        HashSet<string> ids = new();

        foreach (RoomSocketData socket in room.Sockets)
        {
            if (socket == null)
            {
                result.Add(
                    RoomValidationSeverity.Error,
                    "SOCKET_NULL",
                    "Room contains a null Socket."
                );

                continue;
            }

            if (string.IsNullOrWhiteSpace(socket.Id))
            {
                result.Add(
                    RoomValidationSeverity.Error,
                    "SOCKET_ID_EMPTY",
                    "A Socket has no Id."
                );
            }
            else if (!ids.Add(socket.Id))
            {
                result.Add(
                    RoomValidationSeverity.Error,
                    "SOCKET_ID_DUPLICATE",
                    $"Socket Id '{socket.Id}' is duplicated."
                );
            }

            if (!room.IsInside(socket.Position))
            {
                result.Add(
                    RoomValidationSeverity.Error,
                    "SOCKET_OUTSIDE",
                    $"Socket '{socket.Id}' is outside the Room at {socket.Position}."
                );
            }
            else if (!room.IsSocketDirectionValidForPosition(
                         socket.Position,
                         socket.Direction
                     ))
            {
                result.Add(
                    RoomValidationSeverity.Error,
                    "SOCKET_BOUNDARY",
                    $"Socket '{socket.Id}' at {socket.Position} does not match its {socket.Direction} boundary. " +
                    "North sockets must be on the top row, South on the bottom row, East on the right column and West on the left column."
                );
            }

            if (socket.Width <= 0)
            {
                result.Add(
                    RoomValidationSeverity.Error,
                    "SOCKET_WIDTH",
                    $"Socket '{socket.Id}' has an invalid width."
                );
            }
        }
    }

    private static void ValidateGenerationMappings(
        RoomDefinition room,
        RoomGenerationSettings settings,
        RoomValidationResult result,
        bool forBuild)
    {
        HashSet<CellTypeDefinition> usedTypes = new();

        foreach (RoomLayerData layer in room.Layers)
        {
            if (layer == null || layer.Cells == null)
                continue;

            foreach (CellData cell in layer.Cells)
            {
                if (
                    cell == null ||
                    !cell.Enabled ||
                    cell.Type == null)
                {
                    continue;
                }

                usedTypes.Add(cell.Type);
            }
        }

        foreach (CellTypeDefinition type in usedTypes)
        {
            if (
                !settings.TryGetMapping(
                    type,
                    out CellTypePrefabMapping mapping
                ))
            {
                result.Add(
                    forBuild
                        ? RoomValidationSeverity.Error
                        : RoomValidationSeverity.Warning,
                    "CELL_MAPPING_MISSING",
                    $"CellType '{type.DisplayName}' has no enabled prefab mapping."
                );

                continue;
            }

            if (mapping.Prefab == null)
            {
                result.Add(
                    forBuild
                        ? RoomValidationSeverity.Error
                        : RoomValidationSeverity.Warning,
                    "CELL_PREFAB_MISSING",
                    $"CellType '{type.DisplayName}' has a mapping but no prefab."
                );
            }
        }
    }

    private static RoomLayerData FindLayerById(
        RoomDefinition room,
        string layerId)
    {
        if (
            room == null ||
            string.IsNullOrWhiteSpace(layerId))
        {
            return null;
        }

        foreach (RoomLayerData layer in room.Layers)
        {
            if (layer != null && layer.Id == layerId)
                return layer;
        }

        return null;
    }
}
