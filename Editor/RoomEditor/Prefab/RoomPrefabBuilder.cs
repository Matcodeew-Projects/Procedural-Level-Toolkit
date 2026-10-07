using System;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class RoomPrefabBuilder
{
    public static GameObject Build(
        RoomDefinition room,
        RoomGenerationSettings settings,
        string prefabPath)
    {
        ValidateArguments(
            room,
            settings,
            prefabPath
        );

        /*
         * Integrity must run BEFORE validation.
         *
         * Some persistent structures can repair their own
         * serialized state (Ids, cell counts, null lists, etc.).
         * Validating first can therefore report an error that
         * EnsureIntegrity would immediately fix.
         */
        room.EnsureIntegrity();

        RoomValidationResult validation =
            RoomValidator.Validate(
                room,
                settings,
                true
            );

        LogValidationIssues(
            room,
            validation
        );

        if (validation.HasErrors)
        {
            throw new InvalidOperationException(
                BuildValidationErrorMessage(
                    validation
                )
            );
        }

        prefabPath =
            NormalizePrefabPath(
                prefabPath
            );

        GameObject existingPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                prefabPath
            );

        if (existingPrefab == null)
        {
            return BuildNewPrefab(
                room,
                settings,
                prefabPath
            );
        }

        return RebuildExistingPrefab(
            room,
            settings,
            prefabPath
        );
    }

    private static GameObject BuildNewPrefab(
        RoomDefinition room,
        RoomGenerationSettings settings,
        string prefabPath)
    {
        RoomGenerator generator =
            new RoomGenerator();

        RoomGenerationResult result =
            null;

        try
        {
            result =
                generator.Generate(
                    room,
                    settings
                );

            if (
                result == null ||
                result.Root == null)
            {
                throw new InvalidOperationException(
                    "RoomGenerator returned no root GameObject."
                );
            }

            result.Root.name =
                room.name;

            GameObject prefab =
                PrefabUtility.SaveAsPrefabAsset(
                    result.Root,
                    prefabPath
                );

            if (prefab == null)
            {
                throw new InvalidOperationException(
                    $"Unity failed to create prefab at '{prefabPath}'."
                );
            }

            LogGenerationWarnings(
                room,
                result
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[RoomPrefabBuilder] Created prefab: {prefabPath}",
                prefab
            );

            return prefab;
        }
        finally
        {
            if (result?.Root != null)
            {
                UnityEngine.Object.DestroyImmediate(
                    result.Root
                );
            }
        }
    }

    private static GameObject RebuildExistingPrefab(
        RoomDefinition room,
        RoomGenerationSettings settings,
        string prefabPath)
    {
        GameObject prefabContents =
            PrefabUtility.LoadPrefabContents(
                prefabPath
            );

        if (prefabContents == null)
        {
            throw new InvalidOperationException(
                $"Could not load prefab contents from '{prefabPath}'."
            );
        }

        RoomGenerationResult temporaryResult =
            null;

        try
        {
            Transform manualRoot =
                prefabContents.transform.Find(
                    "Manual"
                );

            if (manualRoot == null)
            {
                GameObject manualObject =
                    new GameObject(
                        "Manual"
                    );

                manualObject.transform.SetParent(
                    prefabContents.transform,
                    false
                );
            }

            Transform oldGenerated =
                prefabContents.transform.Find(
                    "Generated"
                );

            if (oldGenerated != null)
            {
                UnityEngine.Object.DestroyImmediate(
                    oldGenerated.gameObject
                );
            }

            RoomGenerator generator =
                new RoomGenerator();

            temporaryResult =
                generator.Generate(
                    room,
                    settings
                );

            if (
                temporaryResult == null ||
                temporaryResult.GeneratedRoot == null)
            {
                throw new InvalidOperationException(
                    "RoomGenerator failed to create Generated hierarchy."
                );
            }

            Transform newGenerated =
                temporaryResult.GeneratedRoot;

            newGenerated.SetParent(
                prefabContents.transform,
                false
            );

            newGenerated.name =
                "Generated";

            RoomModule module =
                prefabContents.GetComponent<
                    RoomModule
                >();

            if (module == null)
            {
                module =
                    prefabContents.AddComponent<
                        RoomModule
                    >();
            }

            module.Configure(
                room,
                settings
            );

            prefabContents.name =
                room.name;

            GameObject prefab =
                PrefabUtility.SaveAsPrefabAsset(
                    prefabContents,
                    prefabPath
                );

            if (prefab == null)
            {
                throw new InvalidOperationException(
                    $"Unity failed to update prefab at '{prefabPath}'."
                );
            }

            LogGenerationWarnings(
                room,
                temporaryResult
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[RoomPrefabBuilder] Rebuilt prefab: {prefabPath}",
                prefab
            );

            return prefab;
        }
        finally
        {
            if (temporaryResult?.Root != null)
            {
                UnityEngine.Object.DestroyImmediate(
                    temporaryResult.Root
                );
            }

            PrefabUtility.UnloadPrefabContents(
                prefabContents
            );
        }
    }

    private static string BuildValidationErrorMessage(
        RoomValidationResult validation)
    {
        StringBuilder builder =
            new StringBuilder();

        builder.Append(
            "Room validation failed: "
        );

        builder.Append(
            validation.GetSummary()
        );

        foreach (
            RoomValidationIssue issue
            in validation.Issues)
        {
            if (
                issue.Severity !=
                RoomValidationSeverity.Error)
            {
                continue;
            }

            builder.AppendLine();

            builder.Append(
                "• "
            );

            builder.Append(
                issue.Code
            );

            builder.Append(
                ": "
            );

            builder.Append(
                issue.Message
            );
        }

        return builder.ToString();
    }

    private static void LogValidationIssues(
        RoomDefinition room,
        RoomValidationResult validation)
    {
        if (validation == null)
            return;

        foreach (
            RoomValidationIssue issue
            in validation.Issues)
        {
            string message =
                $"[RoomValidator:{issue.Code}] {issue.Message}";

            switch (issue.Severity)
            {
                case RoomValidationSeverity.Error:

                    Debug.LogError(
                        message,
                        room
                    );

                    break;

                case RoomValidationSeverity.Warning:

                    Debug.LogWarning(
                        message,
                        room
                    );

                    break;

                default:

                    Debug.Log(
                        message,
                        room
                    );

                    break;
            }
        }
    }

    private static void ValidateArguments(
        RoomDefinition room,
        RoomGenerationSettings settings,
        string prefabPath)
    {
        if (room == null)
        {
            throw new ArgumentNullException(
                nameof(room),
                "A RoomDefinition is required."
            );
        }

        if (settings == null)
        {
            throw new ArgumentNullException(
                nameof(settings),
                "RoomGenerationSettings are required."
            );
        }

        if (
            string.IsNullOrWhiteSpace(
                prefabPath
            ))
        {
            throw new ArgumentException(
                "A prefab output path is required.",
                nameof(prefabPath)
            );
        }
    }

    private static string NormalizePrefabPath(
        string path)
    {
        path =
            path.Replace(
                "\\",
                "/"
            );

        if (
            !path.StartsWith(
                "Assets/",
                StringComparison.Ordinal
            ))
        {
            throw new ArgumentException(
                "Prefab must be saved somewhere inside the project's Assets folder."
            );
        }

        if (
            !path.EndsWith(
                ".prefab",
                StringComparison.OrdinalIgnoreCase
            ))
        {
            path +=
                ".prefab";
        }

        return path;
    }

    private static void LogGenerationWarnings(
        RoomDefinition room,
        RoomGenerationResult result)
    {
        if (
            result == null ||
            !result.HasWarnings)
        {
            return;
        }

        foreach (
            string warning
            in result.Warnings)
        {
            Debug.LogWarning(
                $"[RoomGenerator] {warning}",
                room
            );
        }
    }
}
