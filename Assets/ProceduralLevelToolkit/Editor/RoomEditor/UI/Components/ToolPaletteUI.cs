using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

public sealed class ToolPaletteUI : IDisposable
{
    private readonly RoomEditorContext context;

    private readonly Label activeToolLabel;
    private readonly ScrollView cellTypeList;
    private readonly Label cellTypePlaceholder;
    private readonly IntegerField brushSizeField;
    private readonly Toggle continuousPaintToggle;

    private readonly Dictionary<RoomEditorTool, Button> toolButtons = new();
    private readonly List<Button> cellTypeButtons = new();

    public ToolPaletteUI(
        VisualElement root,
        RoomEditorContext context)
    {
        this.context = context;

        activeToolLabel = root.Q<Label>("active-tool-label");
        cellTypeList = root.Q<ScrollView>("cell-type-list");
        cellTypePlaceholder = root.Q<Label>("cell-type-list-placeholder");
        brushSizeField = root.Q<IntegerField>("brush-size-field");
        continuousPaintToggle = root.Q<Toggle>("continuous-paint-toggle");

        RegisterTool(root, "select-tool-button", RoomEditorTool.Select);
        RegisterTool(root, "paint-tool-button", RoomEditorTool.Paint);
        RegisterTool(root, "erase-tool-button", RoomEditorTool.Erase);
        RegisterTool(root, "fill-tool-button", RoomEditorTool.Fill);
        RegisterTool(root, "group-tool-button", RoomEditorTool.Group);
        RegisterTool(root, "socket-tool-button", RoomEditorTool.Socket);

        brushSizeField.SetValueWithoutNotify(context.BrushSize);
        continuousPaintToggle.SetValueWithoutNotify(context.ContinuousPaint);

        brushSizeField.RegisterValueChangedCallback(OnBrushSizeChanged);
        continuousPaintToggle.RegisterValueChangedCallback(OnContinuousPaintChanged);

        context.ToolChanged += RefreshToolState;
        context.BrushChanged += RefreshBrushState;
        context.CellTypeChanged += RefreshCellTypeState;

        RefreshToolState();
    }

    private void RegisterTool(
        VisualElement root,
        string buttonName,
        RoomEditorTool tool)
    {
        Button button = root.Q<Button>(buttonName);
        toolButtons.Add(tool, button);
        button.clicked += () => context.SetTool(tool);
    }

    public void SetCellTypes(IEnumerable<CellTypeDefinition> definitions)
    {
        foreach (Button button in cellTypeButtons)
            button.RemoveFromHierarchy();

        cellTypeButtons.Clear();

        bool any = false;

        foreach (CellTypeDefinition definition in definitions)
        {
            if (definition == null)
                continue;

            any = true;

            Button button = new Button
            {
                text = definition.name
            };

            button.AddToClassList("button");
            button.AddToClassList("button--compact");
            button.userData = definition;

            button.clicked += () =>
                context.SetCurrentCellType(definition);

            cellTypeList.Add(button);
            cellTypeButtons.Add(button);
        }

        if (cellTypePlaceholder != null)
            cellTypePlaceholder.EnableInClassList("hidden", any);

        RefreshCellTypeState();
    }

    private void OnBrushSizeChanged(ChangeEvent<int> evt)
    {
        context.SetBrushSize(evt.newValue);
    }

    private void OnContinuousPaintChanged(ChangeEvent<bool> evt)
    {
        context.SetContinuousPaint(evt.newValue);
    }

    private void RefreshToolState()
    {
        activeToolLabel.text = context.CurrentTool.ToString();

        foreach (KeyValuePair<RoomEditorTool, Button> pair in toolButtons)
        {
            pair.Value.EnableInClassList(
                "tool-button--active",
                context.CurrentTool == pair.Key
            );
        }
    }

    private void RefreshBrushState()
    {
        brushSizeField.SetValueWithoutNotify(context.BrushSize);
        continuousPaintToggle.SetValueWithoutNotify(context.ContinuousPaint);
    }

    private void RefreshCellTypeState()
    {
        foreach (Button button in cellTypeButtons)
        {
            bool selected = ReferenceEquals(
                button.userData,
                context.CurrentCellType
            );

            button.EnableInClassList(
                "tool-button--active",
                selected
            );
        }
    }

    public void Dispose()
    {
        context.ToolChanged -= RefreshToolState;
        context.BrushChanged -= RefreshBrushState;
        context.CellTypeChanged -= RefreshCellTypeState;
    }
}
