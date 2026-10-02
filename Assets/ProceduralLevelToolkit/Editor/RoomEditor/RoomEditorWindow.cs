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

    private Button buildPrefabButton;

    [MenuItem("Procedural Level Toolkit/Room Editor")]
    public static void Open()
    {
        RoomEditorWindow window =
            GetWindow<RoomEditorWindow>();

        window.titleContent =
            new GUIContent(
                "Room Editor"
            );

        window.minSize =
            new Vector2(
                720f,
                460f
            );
    }

    public void CreateGUI()
    {
        DisposeEditor();

        rootVisualElement.Clear();

        VisualTreeAsset template =
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                RoomEditorUIPaths.RoomEditorUxml
            );

        if (template == null)
        {
            rootVisualElement.Add(
                new HelpBox(
                    $"RoomEditor.uxml introuvable :\n{RoomEditorUIPaths.RoomEditorUxml}",
                    HelpBoxMessageType.Error
                )
            );

            return;
        }

        template.CloneTree(
            rootVisualElement
        );

        context =
            new RoomEditorContext();

        ui =
            new RoomEditorUI(
                rootVisualElement,
                context
            );

        inputHandler =
            new RoomEditorInputHandler(
                context
            );

        controller =
            new RoomEditorController(
                context,
                ui
            );

        previewController =
            new RoomPreviewController(
                context,
                ui.Preview
            );

        inputHandler.Bind(
            ui.GridCanvas
        );

        controller.BindInput(
            inputHandler
        );

        buildPrefabButton =
            rootVisualElement.Q<Button>(
                "build-prefab-button"
            );

        if (buildPrefabButton != null)
        {
            buildPrefabButton.clicked +=
                OpenBuildPrefabWindow;
        }

        LoadCellTypeDefinitions();

        ui.SetStatus(
            "Ready"
        );
    }

    private void OpenBuildPrefabWindow()
    {
        if (
            context == null ||
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

    private void LoadCellTypeDefinitions()
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "t:CellTypeDefinition"
            );

        List<CellTypeDefinition> definitions =
            new();

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid
                );

            CellTypeDefinition definition =
                AssetDatabase.LoadAssetAtPath<CellTypeDefinition>(
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
            (a, b) =>
                string.Compare(
                    a.DisplayName,
                    b.DisplayName,
                    System.StringComparison.OrdinalIgnoreCase
                )
        );

        ui.ToolPalette.SetCellTypes(
            definitions
        );

        ui.Inspector.SetCellTypes(
            definitions
        );

        if (
            definitions.Count > 0 &&
            context.CurrentCellType == null)
        {
            context.SetCurrentCellType(
                definitions[0]
            );
        }
    }

    private void OnDisable()
    {
        DisposeEditor();
    }

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
    }
}
