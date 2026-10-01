using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class InspectorPanelUI
    : IDisposable
{
    private readonly Label selectionKindLabel;


    private readonly VisualElement emptyInspector;
    private readonly VisualElement roomInspector;
    private readonly VisualElement cellInspector;
    private readonly VisualElement multiCellInspector;
    private readonly VisualElement groupInspector;
    private readonly VisualElement socketInspector;


    private readonly IntegerField roomWidthField;
    private readonly IntegerField roomHeightField;
    private readonly Button resizeRoomButton;


    private readonly Label cellPositionLabel;
    private readonly DropdownField cellTypeField;
    private readonly Toggle cellEnabledToggle;


    private readonly Label multiCellCountLabel;


    private readonly TextField groupIdField;
    private readonly TextField groupLabelField;
    private readonly Label groupCellCountLabel;
    private readonly Button clearGroupButton;


    private readonly Label socketPositionLabel;
    private readonly TextField socketIdField;
    private readonly DropdownField socketDirectionField;
    private readonly DropdownField socketRoleField;
    private readonly TextField socketTypeField;
    private readonly IntegerField socketWidthField;
    private readonly Button removeSocketButton;


    private readonly List<CellTypeDefinition>
        cellTypes =
            new();


    public event Action<int, int>
        RoomResizeRequested;


    public event Action<CellTypeDefinition>
        CellTypeChanged;


    public event Action<bool>
        CellEnabledChanged;


    public event Action<string>
        GroupIdChanged;


    public event Action<string>
        GroupLabelChanged;


    public event Action
        ClearGroupRequested;


    public event Action<string>
        SocketIdChanged;


    public event Action<string>
        SocketDirectionChanged;


    public event Action<string>
        SocketRoleChanged;


    public event Action<string>
        SocketTypeChanged;


    public event Action<int>
        SocketWidthChanged;


    public event Action
        RemoveSocketRequested;


    public InspectorPanelUI(
        VisualElement root)
    {
        selectionKindLabel =
            Require<Label>(
                root,
                "selection-kind-label"
            );


        emptyInspector =
            Require<VisualElement>(
                root,
                "empty-selection-inspector"
            );


        roomInspector =
            Require<VisualElement>(
                root,
                "room-inspector"
            );


        cellInspector =
            Require<VisualElement>(
                root,
                "cell-inspector"
            );


        multiCellInspector =
            Require<VisualElement>(
                root,
                "multi-cell-inspector"
            );


        groupInspector =
            Require<VisualElement>(
                root,
                "group-inspector"
            );


        socketInspector =
            Require<VisualElement>(
                root,
                "socket-inspector"
            );


        roomWidthField =
            Require<IntegerField>(
                root,
                "room-width-field"
            );


        roomHeightField =
            Require<IntegerField>(
                root,
                "room-height-field"
            );


        resizeRoomButton =
            Require<Button>(
                root,
                "resize-room-button"
            );


        cellPositionLabel =
            Require<Label>(
                root,
                "cell-position-label"
            );


        cellTypeField =
            Require<DropdownField>(
                root,
                "cell-type-field"
            );


        cellEnabledToggle =
            Require<Toggle>(
                root,
                "cell-enabled-toggle"
            );


        multiCellCountLabel =
            Require<Label>(
                root,
                "multi-cell-count-label"
            );


        groupIdField =
            Require<TextField>(
                root,
                "group-id-field"
            );


        groupLabelField =
            Require<TextField>(
                root,
                "group-label-field"
            );


        groupCellCountLabel =
            Require<Label>(
                root,
                "group-cell-count-label"
            );


        clearGroupButton =
            Require<Button>(
                root,
                "clear-group-button"
            );


        socketPositionLabel =
            Require<Label>(
                root,
                "socket-position-label"
            );


        socketIdField =
            Require<TextField>(
                root,
                "socket-id-field"
            );


        socketDirectionField =
            Require<DropdownField>(
                root,
                "socket-direction-field"
            );


        socketRoleField =
            Require<DropdownField>(
                root,
                "socket-role-field"
            );


        socketTypeField =
            Require<TextField>(
                root,
                "socket-type-field"
            );


        socketWidthField =
            Require<IntegerField>(
                root,
                "socket-width-field"
            );


        removeSocketButton =
            Require<Button>(
                root,
                "remove-socket-button"
            );


        roomWidthField.isDelayed =
            true;


        roomHeightField.isDelayed =
            true;


        groupIdField.isDelayed =
            true;


        groupLabelField.isDelayed =
            true;


        socketIdField.isDelayed =
            true;


        socketTypeField.isDelayed =
            true;


        socketWidthField.isDelayed =
            true;


        resizeRoomButton.clicked +=
            OnResizeRoomClicked;


        cellTypeField
            .RegisterValueChangedCallback(
                OnCellTypeChanged
            );


        cellEnabledToggle
            .RegisterValueChangedCallback(
                OnCellEnabledChanged
            );


        groupIdField
            .RegisterValueChangedCallback(
                OnGroupIdChanged
            );


        groupLabelField
            .RegisterValueChangedCallback(
                OnGroupLabelChanged
            );


        clearGroupButton.clicked +=
            OnClearGroupClicked;


        socketIdField
            .RegisterValueChangedCallback(
                OnSocketIdChanged
            );


        socketDirectionField
            .RegisterValueChangedCallback(
                OnSocketDirectionChanged
            );


        socketRoleField
            .RegisterValueChangedCallback(
                OnSocketRoleChanged
            );


        socketTypeField
            .RegisterValueChangedCallback(
                OnSocketTypeChanged
            );


        socketWidthField
            .RegisterValueChangedCallback(
                OnSocketWidthChanged
            );


        removeSocketButton.clicked +=
            OnRemoveSocketClicked;


        ShowNoRoom();
    }


    private static T Require<T>(
        VisualElement root,
        string name)
        where T : VisualElement
    {
        T element =
            root.Q<T>(
                name
            );


        if (element != null)
            return element;


        throw new InvalidOperationException(
            $"[RoomEditor] Missing '{name}' " +
            $"({typeof(T).Name})."
        );
    }


    // =========================================================
    // Configuration
    // =========================================================

    public void SetCellTypes(
        IEnumerable<CellTypeDefinition> definitions)
    {
        cellTypes.Clear();


        if (definitions != null)
        {
            cellTypes.AddRange(
                definitions.Where(
                    value =>
                        value != null
                )
            );
        }


        List<string> choices =
            new()
            {
                "Empty"
            };


        choices.AddRange(
            cellTypes.Select(
                type =>
                    type.DisplayName
            )
        );


        cellTypeField.choices =
            choices;
    }


    public void SetSocketDirections(
        IEnumerable<string> values)
    {
        socketDirectionField.choices =
            values?.ToList()
            ??
            new List<string>();
    }


    public void SetSocketRoles(
        IEnumerable<string> values)
    {
        socketRoleField.choices =
            values?.ToList()
            ??
            new List<string>();
    }


    // =========================================================
    // States
    // =========================================================

    public void ShowNoRoom()
    {
        ShowState(
            emptyInspector
        );


        selectionKindLabel.text =
            "No Room";
    }


    public void ShowRoom(
        int width,
        int height)
    {
        ShowState(
            roomInspector
        );


        selectionKindLabel.text =
            "Room";


        roomWidthField
            .SetValueWithoutNotify(
                width
            );


        roomHeightField
            .SetValueWithoutNotify(
                height
            );
    }


    public void ShowCell(
        Vector2Int position,
        CellTypeDefinition type,
        bool enabled)
    {
        ShowState(
            cellInspector
        );


        selectionKindLabel.text =
            "Cell";


        cellPositionLabel.text =
            $"Position: {position.x}, {position.y}";


        cellTypeField
            .SetValueWithoutNotify(
                type != null
                    ? type.DisplayName
                    : "Empty"
            );


        cellEnabledToggle
            .SetValueWithoutNotify(
                enabled
            );
    }


    public void ShowMultipleCells(
        int count)
    {
        ShowState(
            multiCellInspector
        );


        selectionKindLabel.text =
            "Multiple Cells";


        multiCellCountLabel.text =
            $"Selected: {count}";
    }


    public void ShowGroup(
        CellGroupData group)
    {
        ShowState(
            groupInspector
        );


        selectionKindLabel.text =
            "Cell Group";


        groupIdField
            .SetValueWithoutNotify(
                group.Id
            );


        groupLabelField
            .SetValueWithoutNotify(
                group.Label
            );


        groupCellCountLabel.text =
            $"Cells: {group.Cells.Count}";
    }


    public void ShowSocket(
        RoomSocketData socket)
    {
        ShowState(
            socketInspector
        );


        selectionKindLabel.text =
            "Room Socket";


        socketPositionLabel.text =
            $"Position: {socket.Position.x}, {socket.Position.y}";


        socketIdField
            .SetValueWithoutNotify(
                socket.Id
            );


        socketDirectionField
            .SetValueWithoutNotify(
                socket.Direction
                    .ToString()
            );


        socketRoleField
            .SetValueWithoutNotify(
                socket.Role
                    .ToString()
            );


        socketTypeField
            .SetValueWithoutNotify(
                socket.Type
            );


        socketWidthField
            .SetValueWithoutNotify(
                socket.Width
            );
    }


    private void ShowState(
        VisualElement state)
    {
        emptyInspector.EnableInClassList(
            "hidden",
            state != emptyInspector
        );


        roomInspector.EnableInClassList(
            "hidden",
            state != roomInspector
        );


        cellInspector.EnableInClassList(
            "hidden",
            state != cellInspector
        );


        multiCellInspector.EnableInClassList(
            "hidden",
            state != multiCellInspector
        );


        groupInspector.EnableInClassList(
            "hidden",
            state != groupInspector
        );


        socketInspector.EnableInClassList(
            "hidden",
            state != socketInspector
        );
    }


    // =========================================================
    // Callbacks
    // =========================================================

    private void OnResizeRoomClicked()
    {
        RoomResizeRequested?.Invoke(
            Mathf.Max(
                1,
                roomWidthField.value
            ),
            Mathf.Max(
                1,
                roomHeightField.value
            )
        );
    }


    private void OnCellTypeChanged(
        ChangeEvent<string> evt)
    {
        if (
            evt.newValue ==
            "Empty")
        {
            CellTypeChanged?.Invoke(
                null
            );


            return;
        }


        CellTypeDefinition type =
            cellTypes.FirstOrDefault(
                value =>
                    value.DisplayName ==
                    evt.newValue
            );


        CellTypeChanged?.Invoke(
            type
        );
    }


    private void OnCellEnabledChanged(
        ChangeEvent<bool> evt)
    {
        CellEnabledChanged?.Invoke(
            evt.newValue
        );
    }


    private void OnGroupIdChanged(
        ChangeEvent<string> evt)
    {
        GroupIdChanged?.Invoke(
            evt.newValue
        );
    }


    private void OnGroupLabelChanged(
        ChangeEvent<string> evt)
    {
        GroupLabelChanged?.Invoke(
            evt.newValue
        );
    }


    private void OnClearGroupClicked()
    {
        ClearGroupRequested?.Invoke();
    }


    private void OnSocketIdChanged(
        ChangeEvent<string> evt)
    {
        SocketIdChanged?.Invoke(
            evt.newValue
        );
    }


    private void OnSocketDirectionChanged(
        ChangeEvent<string> evt)
    {
        SocketDirectionChanged?.Invoke(
            evt.newValue
        );
    }


    private void OnSocketRoleChanged(
        ChangeEvent<string> evt)
    {
        SocketRoleChanged?.Invoke(
            evt.newValue
        );
    }


    private void OnSocketTypeChanged(
        ChangeEvent<string> evt)
    {
        SocketTypeChanged?.Invoke(
            evt.newValue
        );
    }


    private void OnSocketWidthChanged(
        ChangeEvent<int> evt)
    {
        SocketWidthChanged?.Invoke(
            Mathf.Max(
                1,
                evt.newValue
            )
        );
    }


    private void OnRemoveSocketClicked()
    {
        RemoveSocketRequested?.Invoke();
    }


    public void Dispose()
    {
    }
}