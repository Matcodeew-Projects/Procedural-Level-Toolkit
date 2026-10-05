using UnityEditor;
using UnityEngine;

public static class LevelPrefabBuilder
{
    public static GameObject BuildWithSaveDialog(LevelDefinition level)
    {
        if (level == null)
        {
            Debug.LogError("LevelPrefabBuilder: LevelDefinition is null.");
            return null;
        }

        string defaultName = string.IsNullOrWhiteSpace(level.LevelName)
            ? "GeneratedLevel"
            : level.LevelName;

        string path = EditorUtility.SaveFilePanelInProject(
            "Build Level Prefab",
            defaultName,
            "prefab",
            "Choose where the generated level prefab should be saved.");

        if (string.IsNullOrWhiteSpace(path))
            return null;

        return Build(level, path);
    }

    public static GameObject Build(LevelDefinition level, string prefabPath)
    {
        GameObject temporaryRoot = null;

        try
        {
            LevelBuildResult result = LevelBuilder.Build(
                level,
                null,
                name => new GameObject(name),
                (prefab, parent) =>
                {
                    GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                    if (instance != null)
                        instance.transform.SetParent(parent, false);
                    return instance;
                });

            temporaryRoot = result.Root;

            if (!result.Success)
            {
                for (int i = 0; i < result.Errors.Count; i++)
                    Debug.LogError(result.Errors[i]);
                return null;
            }

            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(
                temporaryRoot,
                prefabPath,
                out bool success);

            if (!success || prefabAsset == null)
            {
                Debug.LogError("Level prefab could not be saved. Check the Unity Console for details.");
                return null;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = prefabAsset;
            EditorGUIUtility.PingObject(prefabAsset);
            return prefabAsset;
        }
        finally
        {
            if (temporaryRoot != null)
                Object.DestroyImmediate(temporaryRoot);
        }
    }
}
