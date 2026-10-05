using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class RoomEditorWindow
    : EditorWindow
{
    private RoomEditorContext context;

    private RoomEditorUI ui;

    private RoomEditorInputHandler inputHandler;

    private RoomEditorController controller;

    private RoomPreviewController previewController;


    private VisualElement roomEditorRoot;

    private Button buildPrefabButton;


    // =========================================================
    // Open
    // =========================================================

    [MenuItem(
        "Procedural Level Toolkit/Open Toolkit"
    )]
    public static void Open()
    {
        RoomEditorWindow window =
            GetWindow<RoomEditorWindow>();


        window.titleContent =
            new GUIContent(
                "Procedural Level Toolkit"
            );


        window.minSize =
            new Vector2(
                720f,
                460f
            );
    }


    // =========================================================
    // GUI
    // =========================================================

    public void CreateGUI()
    {
        DisposeEditor();


        rootVisualElement.Clear();


        // =====================================================
        // Load Room Editor UXML
        // =====================================================

        VisualTreeAsset template =
            AssetDatabase
                .LoadAssetAtPath<VisualTreeAsset>(
                    RoomEditorUIPaths
                        .RoomEditorUxml
                );


        if (template == null)
        {
            rootVisualElement.Add(
                new HelpBox(
                    $"RoomEditor.uxml introuvable :\n" +
                    RoomEditorUIPaths.RoomEditorUxml,
                    HelpBoxMessageType.Error
                )
            );


            return;
        }


        // =====================================================
        // Room Editor Page Root
        // =====================================================
        //
        // IMPORTANT :
        //
        // rootVisualElement
        //      = racine de toute la fenêtre
        //
        // roomEditorRoot
        //      = contenu de la page Room Editor
        //
        // Ils ne doivent surtout pas être le même élément.
        // =====================================================

        roomEditorRoot =
            new VisualElement
            {
                name =
                    "room-editor-page-root"
            };


        roomEditorRoot.style.flexGrow =
            1f;


        template.CloneTree(
            roomEditorRoot
        );


        // =====================================================
        // Context
        // =====================================================

        context =
            new RoomEditorContext();


        // =====================================================
        // UI
        // =====================================================

        ui =
            new RoomEditorUI(
                roomEditorRoot,
                context
            );


        // =====================================================
        // Input
        // =====================================================

        inputHandler =
            new RoomEditorInputHandler(
                context
            );


        // =====================================================
        // Controller
        // =====================================================

        controller =
            new RoomEditorController(
                context,
                ui
            );


        // =====================================================
        // Preview
        // =====================================================

        previewController =
            new RoomPreviewController(
                context,
                ui.Preview
            );


        // =====================================================
        // Input Binding
        // =====================================================

        inputHandler.Bind(
            ui.GridCanvas
        );


        controller.BindInput(
            inputHandler
        );


        // =====================================================
        // Build Prefab Button
        // =====================================================

        buildPrefabButton =
            roomEditorRoot.Q<Button>(
                "build-prefab-button"
            );


        if (buildPrefabButton != null)
        {
            buildPrefabButton.clicked +=
                OpenBuildPrefabWindow;
        }


        // =====================================================
        // Definitions
        // =====================================================

        LoadCellTypeDefinitions();


        // =====================================================
        // Status
        // =====================================================

        ui.SetStatus(
            "Ready"
        );


        // =====================================================
        // Toolkit Shell
        // =====================================================
        //
        // rootVisualElement :
        //     fenêtre complète
        //
        // roomEditorRoot :
        //     page Room Editor uniquement
        //
        // LevelEditorBootstrap va ajouter :
        //
        // ToolkitShell
        // ├── Room Editor
        // └── Level Editor
        //
        // =====================================================

        LevelEditorBootstrap.Attach(
            rootVisualElement,
            roomEditorRoot
        );
    }


    // =========================================================
    // Build Prefab
    // =========================================================

    private void OpenBuildPrefabWindow()
    {
        if (context == null ||
            context.CurrentRoom == null)
        {
            ui?.SetStatus(
                "Create or load a Room before building a prefab."
            );


            return;
        }


        RoomPrefabBuildWindow.Open(
            context.CurrentRoom
        );
    }


    // =========================================================
    // Cell Types
    // =========================================================

    private void LoadCellTypeDefinitions()
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "t:CellTypeDefinition"
            );


        List<CellTypeDefinition> definitions =
            new List<CellTypeDefinition>();


        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid
                );


            CellTypeDefinition definition =
                AssetDatabase
                    .LoadAssetAtPath<
                        CellTypeDefinition
                    >(
                        path
                    );


            if (definition != null)
            {
                definitions.Add(
                    definition
                );
            }
        }


        definitions.Sort(
            (
                a,
                b
            ) =>
                string.Compare(
                    a.DisplayName,
                    b.DisplayName,
                    System.StringComparison
                        .OrdinalIgnoreCase
                )
        );


        ui.ToolPalette.SetCellTypes(
            definitions
        );


        ui.Inspector.SetCellTypes(
            definitions
        );


        if (definitions.Count > 0 &&
            context.CurrentCellType == null)
        {
            context.SetCurrentCellType(
                definitions[0]
            );
        }
    }


    // =========================================================
    // Unity Lifecycle
    // =========================================================

    private void OnDisable()
    {
        DisposeEditor();
    }


    // =========================================================
    // Dispose
    // =========================================================

    private void DisposeEditor()
    {
        if (buildPrefabButton != null)
        {
            buildPrefabButton.clicked -=
                OpenBuildPrefabWindow;


            buildPrefabButton =
                null;
        }


        previewController?.Dispose();

        previewController =
            null;


        controller?.Dispose();

        controller =
            null;


        inputHandler?.Unbind();

        inputHandler =
            null;


        ui?.Dispose();

        ui =
            null;


        context =
            null;


        roomEditorRoot =
            null;
    }
}