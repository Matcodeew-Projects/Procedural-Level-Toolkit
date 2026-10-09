using System;
using UnityEditor;
using UnityEngine;

public static class LevelPrefabBuilder
{
    // =========================================================
    // Public API
    // =========================================================

    public static GameObject BuildOrRebuild(
        LevelDefinition level
    )
    {
        if (level == null)
        {
            Debug.LogError(
                "LevelPrefabBuilder: LevelDefinition is null."
            );


            return null;
        }


        if (level.GeneratedPrefab != null)
        {
            string existingPath =
                AssetDatabase.GetAssetPath(
                    level.GeneratedPrefab
                );


            if (!string.IsNullOrWhiteSpace(
                    existingPath
                ) &&
                existingPath.EndsWith(
                    ".prefab",
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                return Build(
                    level,
                    existingPath
                );
            }
        }


        return BuildWithSaveDialog(
            level
        );
    }


    public static GameObject BuildWithSaveDialog(
        LevelDefinition level
    )
    {
        if (level == null)
        {
            Debug.LogError(
                "LevelPrefabBuilder: LevelDefinition is null."
            );


            return null;
        }


        string defaultName =
            string.IsNullOrWhiteSpace(
                level.LevelName
            )
                ? "GeneratedLevel"
                : level.LevelName;


        string path =
            EditorUtility
                .SaveFilePanelInProject(
                    "Build Level Prefab",
                    defaultName,
                    "prefab",
                    "Choose where the generated level prefab should be saved."
                );


        if (string.IsNullOrWhiteSpace(
                path
            ))
        {
            return null;
        }


        return Build(
            level,
            path
        );
    }


    public static GameObject Build(
        LevelDefinition level,
        string prefabPath
    )
    {
        if (level == null)
        {
            Debug.LogError(
                "LevelPrefabBuilder: LevelDefinition is null."
            );


            return null;
        }


        LevelValidationResult validation =
            LevelValidator.Validate(
                level
            );


        LogValidation(
            validation
        );


        if (!validation.IsValid)
        {
            Debug.LogError(
                $"Level '{level.LevelName}' was not built because validation failed with " +
                $"{validation.ErrorCount} error(s)."
            );


            return null;
        }


        if (string.IsNullOrWhiteSpace(
                prefabPath
            ))
        {
            Debug.LogError(
                "LevelPrefabBuilder: output prefab path is empty."
            );


            return null;
        }


        GameObject temporaryRoot =
            null;


        try
        {
            LevelBuildResult buildResult =
                LevelBuilder.Build(
                    level,
                    null,
                    name =>
                        new GameObject(
                            name
                        ),
                    (
                        prefab,
                        parent
                    ) =>
                    {
                        GameObject instance =
                            PrefabUtility
                                .InstantiatePrefab(
                                    prefab
                                )
                                as GameObject;


                        if (instance != null)
                        {
                            instance.transform
                                .SetParent(
                                    parent,
                                    false
                                );
                        }


                        return instance;
                    }
                );


            temporaryRoot =
                buildResult.Root;


            if (!buildResult.Success)
            {
                for (int i = 0;
                     i < buildResult.Errors.Count;
                     i++)
                {
                    Debug.LogError(
                        buildResult.Errors[i]
                    );
                }


                return null;
            }


            GameObject prefabAsset =
                PrefabUtility
                    .SaveAsPrefabAsset(
                        temporaryRoot,
                        prefabPath,
                        out bool success
                    );


            if (!success ||
                prefabAsset == null)
            {
                Debug.LogError(
                    "Level prefab could not be saved."
                );


                return null;
            }


            Undo.RecordObject(
                level,
                "Update Level Build Output"
            );


            level.SetBuildOutput(
                prefabAsset,
                DateTime.UtcNow
                    .ToString(
                        "O"
                    )
            );


            EditorUtility.SetDirty(
                level
            );


            AssetDatabase.SaveAssets();

            AssetDatabase.Refresh();


            Selection.activeObject =
                prefabAsset;


            EditorGUIUtility.PingObject(
                prefabAsset
            );


            Debug.Log(
                $"Level '{level.LevelName}' built successfully at '{prefabPath}'."
            );


            return prefabAsset;
        }
        finally
        {
            if (temporaryRoot != null)
            {
                UnityEngine.Object
                    .DestroyImmediate(
                        temporaryRoot
                    );
            }
        }
    }


    // =========================================================
    // Validation Logging
    // =========================================================

    private static void LogValidation(
        LevelValidationResult validation
    )
    {
        if (validation == null)
        {
            return;
        }


        for (int i = 0;
             i < validation.Issues.Count;
             i++)
        {
            LevelValidationIssue issue =
                validation.Issues[i];


            string message =
                $"[{issue.Code}] {issue.Message}";


            if (issue.Severity ==
                LevelValidationSeverity.Error)
            {
                Debug.LogError(
                    message
                );
            }
            else
            {
                Debug.LogWarning(
                    message
                );
            }
        }
    }
}
