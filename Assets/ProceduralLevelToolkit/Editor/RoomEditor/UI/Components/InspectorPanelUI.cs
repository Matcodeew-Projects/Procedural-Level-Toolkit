using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class InspectorPanelUI : IDisposable
{
    private readonly Label selectionKindLabel;

    private readonly VisualElement emptyInspector;
    private readonly VisualElement cellInspector;
    private readonly VisualElement groupInspector;
    private readonly VisualElement socketInspector;

    private readonly Label cellPositionLabel;
    private readonly DropdownField cellTypeField;
    private readonly Toggle cellEnabledToggle;

    private readonly TextField groupIdField;
    private readonly TextField groupLabelField;
    private readonly Label groupCellCountLabel;
    private readonly Button clearGroupButton;

    private readonly TextField socketIdField;
    private readonly DropdownField socketDirectionField;
    private readonly DropdownField socketTypeField;
    private readonly Toggle socketRequiredToggle;
    private readonly Button removeSocketButton;

    private readonly List<CellTypeDefinition> cellTypes = new();

    public event Action<CellTypeDefinition> CellTypeChanged;
    public event Action<bool> CellEnabledChanged;
    public event Action<string> GroupIdChanged;
    public event Action<string> GroupLabelChanged;
    public event Action ClearGroupRequested;
    public event Action<string> SocketIdChanged;
    public event Action<string> SocketDirectionChanged;
    public event Action<string> SocketTypeChanged;
    public event Action<bool> SocketRequiredChanged;
    public event Action RemoveSocketRequested;

    public InspectorPanelUI(VisualElement root)
    {
        selectionKindLabel = root.Q<Label>("selection-kind-label");

        emptyInspector = root.Q<VisualElement>("empty-selection-inspector");
        cellInspector = root.Q<VisualElement>("cell-inspector");
        groupInspector = root.Q<VisualElement>("group-inspector");
        socketInspector = root.Q<VisualElement>("socket-inspector");

        cellPositionLabel = root.Q<Label>("cell-position-label");
        cellTypeField = root.Q<DropdownField>("cell-type-field");
        cellEnabledToggle = root.Q<Toggle>("cell-enabled-toggle");

        groupIdField = root.Q<TextField>("group-id-field");
        groupLabelField = root.Q<TextField>("group-label-field");
        groupCellCountLabel = root.Q<Label>("group-cell-count-label");
        clearGroupButton = root.Q<Button>("clear-group-button");

        socketIdField = root.Q<TextField>("socket-id-field");
        socketDirectionField = root.Q<DropdownField>("socket-direction-field");
        socketTypeField = root.Q<DropdownField>("socket-type-field");
        socketRequiredToggle = root.Q<Toggle>("socket-required-toggle");
        removeSocketButton = root.Q<Button>("remove-socket-button");

        cellTypeField.RegisterValueChangedCallback(OnCellTypeChanged);
        cellEnabledToggle.RegisterValueChangedCallback(evt =>
            CellEnabledChanged?.Invoke(evt.newValue));

        groupIdField.RegisterValueChangedCallback(evt =>
            GroupIdChanged?.Invoke(evt.newValue));
        groupLabelField.RegisterValueChangedCallback(evt =>
            GroupLabelChanged?.Invoke(evt.newValue));
        clearGroupButton.clicked += () => ClearGroupRequested?.Invoke();

        socketIdField.RegisterValueChangedCallback(evt =>
            SocketIdChanged?.Invoke(evt.newValue));
        socketDirectionField.RegisterValueChangedCallback(evt =>
            SocketDirectionChanged?.Invoke(evt.newValue));
        socketTypeField.RegisterValueChangedCallback(evt =>
            SocketTypeChanged?.Invoke(evt.newValue));
        socketRequiredToggle.RegisterValueChangedCallback(evt =>
            SocketRequiredChanged?.Invoke(evt.newValue));
        removeSocketButton.clicked += () => RemoveSocketRequested?.Invoke();

        ShowNone();
    }

    public void SetCellTypes(IEnumerable<CellTypeDefinition> definitions)
    {
        cellTypes.Clear();

        if (definitions != null)
            cellTypes.AddRange(definitions.Where(type => type != null));

        cellTypeField.choices = cellTypes
            .Select(type => type.name)
            .ToList();
    }

    public void SetSocketDirections(IEnumerable<string> directions)
    {
        socketDirectionField.choices = directions?.ToList() ?? new List<string>();
    }

    public void SetSocketTypes(IEnumerable<string> socketTypes)
    {
        socketTypeField.choices = socketTypes?.ToList() ?? new List<string>();
    }

    public void ShowNone()
    {
        SetState(emptyInspector);
        selectionKindLabel.text = "Nothing selected";
    }

    public void ShowCell(
        Vector2Int position,
        CellTypeDefinition type,
        bool enabled)
    {
        SetState(cellInspector);
        selectionKindLabel.text = "Cell";

        cellPositionLabel.text = $"Position: {position.x}, {position.y}";
        cellTypeField.SetValueWithoutNotify(type != null ? type.name : string.Empty);
        cellEnabledToggle.SetValueWithoutNotify(enabled);
    }

    public void ShowGroup(
        string id,
        string label,
        int cellCount)
    {
        SetState(groupInspector);
        selectionKindLabel.text = "Cell Group";

        groupIdField.SetValueWithoutNotify(id ?? string.Empty);
        groupLabelField.SetValueWithoutNotify(label ?? string.Empty);
        groupCellCountLabel.text = $"Cells: {cellCount}";
    }

    public void ShowSocket(
        string id,
        string direction,
        string socketType,
        bool required)
    {
        SetState(socketInspector);
        selectionKindLabel.text = "Room Socket";

        socketIdField.SetValueWithoutNotify(id ?? string.Empty);
        socketDirectionField.SetValueWithoutNotify(direction ?? string.Empty);
        socketTypeField.SetValueWithoutNotify(socketType ?? string.Empty);
        socketRequiredToggle.SetValueWithoutNotify(required);
    }

    private void SetState(VisualElement visibleState)
    {
        emptyInspector.EnableInClassList("hidden", visibleState != emptyInspector);
        cellInspector.EnableInClassList("hidden", visibleState != cellInspector);
        groupInspector.EnableInClassList("hidden", visibleState != groupInspector);
        socketInspector.EnableInClassList("hidden", visibleState != socketInspector);
    }

    private void OnCellTypeChanged(ChangeEvent<string> evt)
    {
        CellTypeDefinition selected = cellTypes.FirstOrDefault(
            type => type.name == evt.newValue
        );

        CellTypeChanged?.Invoke(selected);
    }

    public void Dispose()
    {
        // Les callbacks UI Toolkit vivent avec l'arbre de la fenêtre.
        // Aucun abonnement externe persistant ici.
    }
}
