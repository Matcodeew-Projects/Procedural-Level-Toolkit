using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class RoomEditorWindow : EditorWindow
{
    private RoomEditorContext context;
    private RoomEditorUI ui;
    private RoomEditorInputHandler inputHandler;

    [MenuItem("Procedural Level Toolkit/Room Editor")]
    public static void Open()
    {
        RoomEditorWindow window = GetWindow<RoomEditorWindow>();
        window.titleContent = new GUIContent("Room Editor");
        window.minSize = new Vector2(720f, 460f);
    }

    public void CreateGUI()
    {
        rootVisualElement.Clear();

        VisualTreeAsset template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
            RoomEditorUIPaths.RoomEditorUxml
        );

        if (template == null)
        {
            rootVisualElement.Add(new HelpBox(
                $"RoomEditor.uxml introuvable :\n{RoomEditorUIPaths.RoomEditorUxml}",
                HelpBoxMessageType.Error
            ));
            return;
        }

        template.CloneTree(rootVisualElement);

        context = new RoomEditorContext();
        ui = new RoomEditorUI(rootVisualElement, context);
        inputHandler = new RoomEditorInputHandler(context);

        inputHandler.Bind(ui.GridCanvas);

        LoadCellTypeDefinitions();
        WireEditorEvents();

        ui.GridCanvas.Refresh();
        ui.SetStatus("Ready");
    }

    private void LoadCellTypeDefinitions()
    {
        string[] guids = AssetDatabase.FindAssets("t:CellTypeDefinition");
        List<CellTypeDefinition> definitions = new();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CellTypeDefinition definition =
                AssetDatabase.LoadAssetAtPath<CellTypeDefinition>(path);

            if (definition != null)
                definitions.Add(definition);
        }

        definitions.Sort((a, b) =>
            string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));

        ui.ToolPalette.SetCellTypes(definitions);
        ui.Inspector.SetCellTypes(definitions);

        if (definitions.Count > 0 && context.CurrentCellType == null)
            context.SetCurrentCellType(definitions[0]);
    }

    private void WireEditorEvents()
    {
        // Toolbar : les vraies opérations Asset/Undo seront branchées
        // lorsque RoomDefinition aura son contrôleur d'édition dédié.
        ui.Toolbar.NewRequested += () => ui.SetStatus("New room requested");
        ui.Toolbar.LoadRequested += () => ui.SetStatus("Load room requested");
        ui.Toolbar.SaveRequested += () => ui.SetStatus("Save room requested");
        ui.Toolbar.UndoRequested += () => ui.SetStatus("Undo requested");
        ui.Toolbar.RedoRequested += () => ui.SetStatus("Redo requested");
        ui.Toolbar.ValidateRequested += () => ui.SetStatus("Validation requested");

        // Layers : mêmes principes, le panneau ne modifie pas directement Runtime.
        ui.Layers.AddRequested += () => ui.SetStatus("Add layer requested");
        ui.Layers.DuplicateRequested += () => ui.SetStatus("Duplicate layer requested");
        ui.Layers.DeleteRequested += () => ui.SetStatus("Delete layer requested");
        ui.Layers.MoveUpRequested += () => ui.SetStatus("Move layer up requested");
        ui.Layers.MoveDownRequested += () => ui.SetStatus("Move layer down requested");

        // Input : à raccorder ensuite à un contrôleur qui modifie RoomDefinition.
        inputHandler.SelectionRequested += cell =>
            ui.SetStatus($"Selected cell {cell.x}, {cell.y}");

        inputHandler.PaintRequested += (cells, type) =>
        {
            string typeName = type != null ? type.name : "None";
            ui.SetStatus($"Paint {cells.Count} cell(s) with {typeName}");
        };

        inputHandler.EraseRequested += cells =>
            ui.SetStatus($"Erase {cells.Count} cell(s)");

        inputHandler.FillRequested += (cell, type) =>
        {
            string typeName = type != null ? type.name : "None";
            ui.SetStatus($"Fill from {cell.x}, {cell.y} with {typeName}");
        };

        inputHandler.GroupRequested += cells =>
            ui.SetStatus($"Group action on {cells.Count} cell(s)");

        inputHandler.SocketRequested += cell =>
            ui.SetStatus($"Socket action at {cell.x}, {cell.y}");
    }

    private void OnDisable()
    {
        inputHandler?.Unbind();
        ui?.Dispose();
    }
}
