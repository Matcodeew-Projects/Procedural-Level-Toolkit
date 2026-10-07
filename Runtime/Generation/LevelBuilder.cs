using System;
using System.Collections.Generic;
using UnityEngine;

public static class LevelBuilder
{
    private const float CellSizeTolerance =
        0.0001f;


    // =========================================================
    // Build
    // =========================================================
    //
    // Spatial convention:
    //
    // SpatialLayout.Position.x -> Unity X
    // SpatialLayout.Position.z -> Unity Z
    //
    // SpatialLayout.Position is the CENTER pivot of the Room.
    //
    // QuarterTurns are applied around Unity Y:
    //
    // 0 -> 0°
    // 1 -> 90°
    // 2 -> 180°
    // 3 -> 270°
    //
    // This matches LayoutGeometryUtility and the centered
    // RoomGenerator prefab convention.
    // =========================================================

    public static LevelBuildResult Build(
        LevelDefinition level,
        Transform parent = null,
        Func<string, GameObject> rootFactory = null,
        Func<GameObject, Transform, GameObject> prefabFactory = null
    )
    {
        LevelBuildResult result =
            new LevelBuildResult();


        if (level == null)
        {
            result.AddError(
                "LevelDefinition is null."
            );


            return result;
        }


        level.EnsureIntegrity();


        if (level.SpatialLayout == null ||
            !level.SpatialLayout.IsSolved)
        {
            result.AddError(
                "SpatialLayout is not solved. Run Solve Layout again."
            );


            return result;
        }


        IReadOnlyList<
            LevelModuleInstanceData
        > placements =
            level.SpatialLayout.Modules;


        if (placements == null ||
            placements.Count ==
            0)
        {
            result.AddError(
                "SpatialLayout contains no modules."
            );


            return result;
        }


        // =====================================================
        // Validate first
        // =====================================================

        float cellWorldSize =
            -1f;


        for (int i = 0;
             i < placements.Count;
             i++)
        {
            LevelModuleInstanceData placement =
                placements[i];


            if (placement == null)
            {
                result.AddError(
                    $"Layout placement #{i + 1} is null."
                );


                continue;
            }


            LevelNodeData node =
                level.Graph.FindNode(
                    placement.NodeId
                );


            if (node == null)
            {
                result.AddError(
                    $"Layout placement '{placement.NodeId}' has no graph node."
                );


                continue;
            }


            RoomModuleDefinition module =
                node.Module;


            if (module == null)
            {
                result.AddError(
                    $"Node '{GetNodeName(node)}' has no RoomModuleDefinition."
                );


                continue;
            }


            if (module.Room == null)
            {
                result.AddError(
                    $"Module '{module.name}' has no RoomDefinition."
                );
            }


            if (module.Prefab == null)
            {
                result.AddError(
                    $"Module '{module.DisplayName}' has no prefab."
                );
            }


            if (cellWorldSize <
                0f)
            {
                cellWorldSize =
                    module.CellWorldSize;
            }
            else if (Mathf.Abs(
                         cellWorldSize -
                         module.CellWorldSize
                     ) >
                     CellSizeTolerance)
            {
                result.AddError(
                    $"Module '{module.DisplayName}' uses Cell World Size " +
                    $"{module.CellWorldSize:0.###}, while the level uses " +
                    $"{cellWorldSize:0.###}. All modules currently need the same scale."
                );
            }
        }


        if (result.Errors.Count >
            0)
        {
            return result;
        }


        // =====================================================
        // Factories
        // =====================================================

        if (rootFactory == null)
        {
            rootFactory =
                name =>
                    new GameObject(
                        name
                    );
        }


        if (prefabFactory == null)
        {
            prefabFactory =
                (
                    prefab,
                    instanceParent
                ) =>
                {
                    GameObject instance =
                        UnityEngine.Object
                            .Instantiate(
                                prefab,
                                instanceParent
                            );


                    return instance;
                };
        }


        // =====================================================
        // Root
        // =====================================================

        string rootName =
            string.IsNullOrWhiteSpace(
                level.LevelName
            )
                ? "Generated Level"
                : level.LevelName;


        GameObject root =
            rootFactory(
                rootName
            );


        if (root == null)
        {
            result.AddError(
                "Root factory returned null."
            );


            return result;
        }


        result.Root =
            root;


        if (parent != null)
        {
            root.transform.SetParent(
                parent,
                false
            );
        }


        // =====================================================
        // Modules
        // =====================================================

        for (int i = 0;
             i < placements.Count;
             i++)
        {
            LevelModuleInstanceData placement =
                placements[i];


            LevelNodeData node =
                level.Graph.FindNode(
                    placement.NodeId
                );


            RoomModuleDefinition module =
                node.Module;


            GameObject instance =
                prefabFactory(
                    module.Prefab,
                    root.transform
                );


            if (instance == null)
            {
                result.AddError(
                    $"Could not instantiate prefab for module '{module.DisplayName}'."
                );


                continue;
            }


            instance.name =
                $"{i:00}_{module.DisplayName}";


            /*
             * Logical layout coordinates are expressed in cells.
             * Convert exactly once here.
             */
            Vector3 logicalPosition =
                placement.Position;


            instance.transform.localPosition =
                new Vector3(
                    logicalPosition.x *
                    cellWorldSize,

                    logicalPosition.y *
                    cellWorldSize,

                    logicalPosition.z *
                    cellWorldSize
                );


            /*
             * Rebuild the yaw from QuarterTurns instead of using
             * a potentially stale serialized Quaternion from an
             * older solved layout.
             */
            instance.transform.localRotation =
                Quaternion.Euler(
                    0f,
                    LayoutGeometryUtility
                        .NormalizeQuarterTurns(
                            placement.QuarterTurns
                        )
                    *
                    90f,
                    0f
                );


            instance.transform.localScale =
                Vector3.one;


            RoomModule marker =
                instance.GetComponent<
                    RoomModule
                >();


            if (marker != null)
            {
                marker.Configure(
                    module
                );
            }


            result.AddInstance(
                instance
            );
        }


        return result;
    }


    // =========================================================
    // Helpers
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
}
