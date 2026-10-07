using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class RoomLibraryUI
    : VisualElement
{
    private readonly TextField searchField;

    private readonly ScrollView list;

    private readonly ObjectField createRoomField;

    private readonly ObjectField createPrefabField;

    private readonly FloatField createCellSizeField;


    public event Action<
        RoomModuleDefinition
    > AddModuleRequested;


    public RoomLibraryUI()
    {
        AddToClassList(
            "level-panel"
        );


        // =====================================================
        // Header
        // =====================================================

        VisualElement header =
            new VisualElement();


        header.AddToClassList(
            "level-library-header"
        );


        VisualElement titleBlock =
            new VisualElement();


        titleBlock.style.flexGrow =
            1;


        Label title =
            new Label(
                "ROOM MODULES"
            );


        title.AddToClassList(
            "level-library-header__title"
        );


        Label subtitle =
            new Label(
                "Reusable rooms available to this level"
            );


        subtitle.AddToClassList(
            "level-library-header__subtitle"
        );


        titleBlock.Add(
            title
        );


        titleBlock.Add(
            subtitle
        );


        Button refreshButton =
            new Button(
                Refresh
            )
            {
                text =
                    "↻"
            };


        refreshButton.tooltip =
            "Refresh RoomModuleDefinition assets";


        refreshButton.AddToClassList(
            "level-library-refresh"
        );


        header.Add(
            titleBlock
        );


        header.Add(
            refreshButton
        );


        Add(
            header
        );


        // =====================================================
        // Content
        // =====================================================

        VisualElement content =
            new VisualElement();


        content.AddToClassList(
            "level-library-content"
        );


        Add(
            content
        );


        // =====================================================
        // Filter
        // =====================================================

        searchField =
            new TextField(
                "Filter"
            );


        searchField.tooltip =
            "Filters the module list by module name or RoomDefinition name.";


        searchField.AddToClassList(
            "level-library-search"
        );


        searchField.RegisterValueChangedCallback(
            _ =>
                Refresh()
        );


        content.Add(
            searchField
        );


        // =====================================================
        // Create / Update Card
        // =====================================================

        Foldout createFoldout =
            new Foldout
            {
                text =
                    "Create / Update Module",

                value =
                    false
            };


        createFoldout.AddToClassList(
            "level-module-create"
        );


        Label help =
            new Label(
                "Create the Level Editor module that links a logical RoomDefinition to its final 3D prefab."
            );


        help.AddToClassList(
            "level-module-create__help"
        );


        createRoomField =
            new ObjectField(
                "Room Definition"
            )
            {
                objectType =
                    typeof(
                        RoomDefinition
                    ),

                allowSceneObjects =
                    false
            };


        createPrefabField =
            new ObjectField(
                "3D Prefab"
            )
            {
                objectType =
                    typeof(
                        GameObject
                    ),

                allowSceneObjects =
                    false
            };


        createCellSizeField =
            new FloatField(
                "World Units / Cell"
            )
            {
                value =
                    1f
            };


        createRoomField.AddToClassList(
            "level-module-create__field"
        );


        createPrefabField.AddToClassList(
            "level-module-create__field"
        );


        createCellSizeField.AddToClassList(
            "level-module-create__field"
        );


        Button createButton =
            new Button(
                CreateOrUpdateModule
            )
            {
                text =
                    "Create / Update Module"
            };


        createButton.AddToClassList(
            "level-module-create__button"
        );


        createFoldout.Add(
            help
        );


        createFoldout.Add(
            createRoomField
        );


        createFoldout.Add(
            createPrefabField
        );


        createFoldout.Add(
            createCellSizeField
        );


        createFoldout.Add(
            createButton
        );


        content.Add(
            createFoldout
        );


        // =====================================================
        // List
        // =====================================================

        Label availableTitle =
            new Label(
                "AVAILABLE MODULES"
            );


        availableTitle.AddToClassList(
            "level-library-section-title"
        );


        content.Add(
            availableTitle
        );


        list =
            new ScrollView();


        list.AddToClassList(
            "level-library-list"
        );


        content.Add(
            list
        );


        Refresh();
    }


    // =========================================================
    // Create / Update
    // =========================================================

    private void CreateOrUpdateModule()
    {
        RoomDefinition room =
            createRoomField.value
                as RoomDefinition;


        GameObject prefab =
            createPrefabField.value
                as GameObject;


        float cellWorldSize =
            Mathf.Max(
                0.0001f,
                createCellSizeField.value
            );


        if (room == null ||
            prefab == null)
        {
            Debug.LogWarning(
                "Room Module creation requires both a RoomDefinition and a 3D prefab."
            );


            return;
        }


        RoomModuleDefinition module =
            RoomModuleEditorUtility
                .CreateOrUpdateModule(
                    room,
                    prefab,
                    cellWorldSize
                );


        if (module != null)
        {
            Refresh();
        }
    }


    // =========================================================
    // Refresh
    // =========================================================

    public void Refresh()
    {
        list.Clear();


        string filter =
            searchField?.value?
                .Trim()
                .ToLowerInvariant()
            ??
            string.Empty;


        string[] guids =
            AssetDatabase.FindAssets(
                "t:RoomModuleDefinition"
            );


        List<
            RoomModuleDefinition
        > modules =
            new List<
                RoomModuleDefinition
            >();


        for (int i = 0;
             i < guids.Length;
             i++)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guids[i]
                );


            RoomModuleDefinition module =
                AssetDatabase
                    .LoadAssetAtPath<
                        RoomModuleDefinition
                    >(
                        path
                    );


            if (module == null)
            {
                continue;
            }


            string searchable =
                (
                    module.DisplayName
                    +
                    " "
                    +
                    module.name
                    +
                    " "
                    +
                    (
                        module.Room != null
                            ? module.Room.name
                            : string.Empty
                    )
                )
                .ToLowerInvariant();


            if (!string.IsNullOrWhiteSpace(
                    filter
                ) &&
                !searchable.Contains(
                    filter
                ))
            {
                continue;
            }


            modules.Add(
                module
            );
        }


        modules.Sort(
            (
                a,
                b
            ) =>
                string.Compare(
                    a.DisplayName,
                    b.DisplayName,
                    StringComparison.OrdinalIgnoreCase
                )
        );


        for (int i = 0;
             i < modules.Count;
             i++)
        {
            AddModuleRow(
                modules[i]
            );
        }


        if (modules.Count ==
            0)
        {
            Label empty =
                new Label(
                    string.IsNullOrWhiteSpace(
                        filter
                    )
                        ? "No RoomModuleDefinition found."
                        : "No module matches this filter."
                );


            empty.AddToClassList(
                "level-library-empty"
            );


            list.Add(
                empty
            );
        }
    }


    private void AddModuleRow(
        RoomModuleDefinition module
    )
    {
        VisualElement row =
            new VisualElement();


        row.AddToClassList(
            "level-library-row"
        );


        VisualElement identity =
            new VisualElement();


        identity.AddToClassList(
            "level-library-row__identity"
        );


        Label name =
            new Label(
                module.DisplayName
            );


        name.AddToClassList(
            "level-library-row__name"
        );


        Label meta =
            new Label(
                module.Prefab != null
                    ? module.Prefab.name
                    : "Missing prefab"
            );


        meta.AddToClassList(
            "level-library-row__meta"
        );


        identity.Add(
            name
        );


        identity.Add(
            meta
        );


        Button ping =
            new Button(
                () =>
                {
                    Selection.activeObject =
                        module;


                    EditorGUIUtility.PingObject(
                        module
                    );
                }
            )
            {
                text =
                    "Ping"
            };


        ping.AddToClassList(
            "level-library-row__ping"
        );


        Button add =
            new Button(
                () =>
                    AddModuleRequested?
                        .Invoke(
                            module
                        )
            )
            {
                text =
                    "+"
            };


        add.tooltip =
            "Add this module to the level graph";


        add.AddToClassList(
            "level-library-row__add"
        );


        row.Add(
            identity
        );


        row.Add(
            ping
        );


        row.Add(
            add
        );


        list.Add(
            row
        );
    }
}
