using System.IO;
using UnityEditor;
using UnityEngine;

public static class RoomModuleEditorUtility
{
    public static RoomModuleDefinition CreateOrUpdateModule(
        RoomDefinition room,
        GameObject prefab,
        float cellWorldSize)
    {
        if (room == null)
        {
            Debug.LogError("RoomModuleEditorUtility: RoomDefinition is null.");
            return null;
        }

        if (prefab == null)
        {
            Debug.LogError("RoomModuleEditorUtility: Prefab is null.");
            return null;
        }

        RoomModuleDefinition module = FindModuleForRoom(room);

        if (module == null)
        {
            module = ScriptableObject.CreateInstance<RoomModuleDefinition>();
            module.Configure(room, prefab, cellWorldSize);

            string roomPath = AssetDatabase.GetAssetPath(room);
            string parentFolder = Path.GetDirectoryName(roomPath)?.Replace("\\", "/");
            if (string.IsNullOrWhiteSpace(parentFolder))
                parentFolder = "Assets";

            string modulesFolder = parentFolder + "/Modules";
            EnsureFolder(modulesFolder);

            string assetPath = AssetDatabase.GenerateUniqueAssetPath(
                modulesFolder + "/" + room.name + "_Module.asset");

            AssetDatabase.CreateAsset(module, assetPath);
        }
        else
        {
            Undo.RecordObject(module, "Update Room Module");
            module.Configure(room, prefab, cellWorldSize);
            EditorUtility.SetDirty(module);
        }

        AssetDatabase.SaveAssets();
        EnsurePrefabMarker(module);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = module;
        EditorGUIUtility.PingObject(module);

        return module;
    }

    public static RoomModuleDefinition FindModuleForRoom(RoomDefinition room)
    {
        if (room == null)
            return null;

        string[] guids = AssetDatabase.FindAssets("t:RoomModuleDefinition");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            RoomModuleDefinition module = AssetDatabase.LoadAssetAtPath<RoomModuleDefinition>(path);

            if (module != null && module.Room == room)
                return module;
        }

        return null;
    }

    public static int TryMigrateLegacyNodes(LevelDefinition level)
    {
        if (level == null || level.Graph == null)
            return 0;

        int migrated = 0;

        for (int i = 0; i < level.Graph.Nodes.Count; i++)
        {
            LevelNodeData node = level.Graph.Nodes[i];
            if (node == null || node.Module != null || node.Room == null)
                continue;

            RoomModuleDefinition module = FindModuleForRoom(node.Room);
            if (module == null)
                continue;

            if (migrated == 0)
                Undo.RecordObject(level, "Migrate Level Nodes To Room Modules");

            node.SetModule(module);
            migrated++;
        }

        if (migrated > 0)
        {
            level.EnsureIntegrity();
            EditorUtility.SetDirty(level);
            AssetDatabase.SaveAssets();
        }

        return migrated;
    }

    private static void EnsurePrefabMarker(RoomModuleDefinition module)
    {
        if (module == null || module.Prefab == null)
            return;

        string prefabPath = AssetDatabase.GetAssetPath(module.Prefab);
        if (string.IsNullOrWhiteSpace(prefabPath) || !prefabPath.EndsWith(".prefab"))
            return;

        GameObject contents = null;

        try
        {
            contents = PrefabUtility.LoadPrefabContents(prefabPath);
            RoomModule marker = contents.GetComponent<RoomModule>();

            if (marker == null)
                marker = contents.AddComponent<RoomModule>();

            marker.Configure(module);
            PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
        }
        finally
        {
            if (contents != null)
                PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        string parent = Path.GetDirectoryName(folderPath)?.Replace("\\", "/");
        string name = Path.GetFileName(folderPath);

        if (!string.IsNullOrWhiteSpace(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, name);
    }
}
