using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class RoomLibraryUI : VisualElement
{
    private readonly TextField searchField;
    private readonly ScrollView list;
    private readonly ObjectField createRoomField;
    private readonly ObjectField createPrefabField;
    private readonly FloatField createCellSizeField;

    public event Action<RoomModuleDefinition> AddModuleRequested;

    public RoomLibraryUI()
    {
        AddToClassList("level-panel");

        Label header = new Label("ROOM MODULES");
        header.AddToClassList("level-panel__header");
        Add(header);

        VisualElement content = new VisualElement();
        content.AddToClassList("level-panel__content");
        Add(content);

        searchField = new TextField { label = "Search" };
        searchField.AddToClassList("level-library-search");
        searchField.RegisterValueChangedCallback(_ => Refresh());

        Button refreshButton = new Button(Refresh) { text = "Refresh Modules" };

        content.Add(searchField);
        content.Add(refreshButton);

        Foldout createFoldout = new Foldout
        {
            text = "Create / Update Module",
            value = false
        };

        createRoomField = new ObjectField("Room")
        {
            objectType = typeof(RoomDefinition),
            allowSceneObjects = false
        };

        createPrefabField = new ObjectField("Prefab")
        {
            objectType = typeof(GameObject),
            allowSceneObjects = false
        };

        createCellSizeField = new FloatField("World Units / Cell")
        {
            value = 1f
        };

        Button createButton = new Button(CreateOrUpdateModule)
        {
            text = "Create / Update Module"
        };

        createFoldout.Add(createRoomField);
        createFoldout.Add(createPrefabField);
        createFoldout.Add(createCellSizeField);
        createFoldout.Add(createButton);
        content.Add(createFoldout);

        list = new ScrollView();
        list.style.flexGrow = 1;
        content.Add(list);

        Refresh();
    }

    private void CreateOrUpdateModule()
    {
        RoomDefinition room = createRoomField.value as RoomDefinition;
        GameObject prefab = createPrefabField.value as GameObject;
        float cellWorldSize = Mathf.Max(0.0001f, createCellSizeField.value);

        RoomModuleDefinition module = RoomModuleEditorUtility.CreateOrUpdateModule(
            room,
            prefab,
            cellWorldSize);

        if (module != null)
            Refresh();
    }

    public void Refresh()
    {
        list.Clear();

        string filter = searchField?.value?.Trim().ToLowerInvariant() ?? string.Empty;
        string[] guids = AssetDatabase.FindAssets("t:RoomModuleDefinition");
        List<RoomModuleDefinition> modules = new List<RoomModuleDefinition>();

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            RoomModuleDefinition module = AssetDatabase.LoadAssetAtPath<RoomModuleDefinition>(path);
            if (module == null)
                continue;

            string searchable = (
                module.DisplayName + " " +
                module.name + " " +
                (module.Room != null ? module.Room.name : string.Empty))
                .ToLowerInvariant();

            if (!string.IsNullOrWhiteSpace(filter) && !searchable.Contains(filter))
                continue;

            modules.Add(module);
        }

        modules.Sort((a, b) => string.Compare(
            a.DisplayName,
            b.DisplayName,
            StringComparison.OrdinalIgnoreCase));

        for (int i = 0; i < modules.Count; i++)
            AddModuleRow(modules[i]);

        if (modules.Count == 0)
        {
            Label empty = new Label(
                "No RoomModuleDefinition found.\n" +
                "Create one above from a RoomDefinition + prefab.");
            empty.style.whiteSpace = WhiteSpace.Normal;
            empty.style.opacity = 0.65f;
            list.Add(empty);
        }
    }

    private void AddModuleRow(RoomModuleDefinition module)
    {
        VisualElement row = new VisualElement();
        row.AddToClassList("level-library-row");

        Label name = new Label(module.DisplayName);
        name.AddToClassList("level-library-row__name");
        name.tooltip = module.Prefab != null ? module.Prefab.name : "Missing prefab";

        Button ping = new Button(() =>
        {
            Selection.activeObject = module;
            EditorGUIUtility.PingObject(module);
        })
        {
            text = "◉"
        };

        Button add = new Button(() => AddModuleRequested?.Invoke(module))
        {
            text = "+"
        };
        add.AddToClassList("level-library-row__add");

        row.Add(name);
        row.Add(ping);
        row.Add(add);
        list.Add(row);
    }
}
