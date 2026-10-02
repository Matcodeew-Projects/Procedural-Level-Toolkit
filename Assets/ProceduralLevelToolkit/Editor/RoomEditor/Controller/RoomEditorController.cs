using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class RoomEditorController
    : IDisposable
{
    private const float InactiveLayerOpacity = 0.20f;

    private readonly RoomEditorContext context;
    private readonly RoomEditorUI ui;

    private RoomEditorInputHandler inputHandler;

    private bool strokeActive;
    private bool strokeUndoRecorded;
    private int strokeUndoGroup = -1;

    public RoomEditorController(
        RoomEditorContext context,
        RoomEditorUI ui)
    {
        this.context =
            context
            ?? throw new ArgumentNullException(
                nameof(context)
            );

        this.ui =
            ui
            ?? throw new ArgumentNullException(
                nameof(ui)
            );

        ui.GridCanvas.CellColorProvider =
            GetCellColor;

        ui.GridCanvas.SocketProvider =
            GetSocketAt;

        ui.Inspector.SetSocketDirections(
            Enum.GetNames(
                typeof(SocketDirection)
            )
        );

        ui.Inspector.SetSocketRoles(
            Enum.GetNames(
                typeof(SocketRole)
            )
        );

        BindToolbar();
        BindLayers();
        BindInspector();

        context.RoomChanged +=
            RefreshAll;

        context.ActiveLayerChanged +=
            RefreshAfterLayerChanged;

        context.SelectionChanged +=
            RefreshInspector;

        Undo.undoRedoPerformed +=
            OnUndoRedoPerformed;

        RefreshAll();
    }

    public void BindInput(
        RoomEditorInputHandler input)
    {
        UnbindInput();

        inputHandler =
            input
            ?? throw new ArgumentNullException(
                nameof(input)
            );

        inputHandler.StrokeStarted +=
            OnStrokeStarted;

        inputHandler.StrokeEnded +=
            OnStrokeEnded;

        inputHandler.SelectionRequested +=
            OnSelectionRequested;

        inputHandler.PaintRequested +=
            OnPaintRequested;

        inputHandler.EraseRequested +=
            OnEraseRequested;

        inputHandler.FillRequested +=
            OnFillRequested;

        inputHandler.GroupRequested +=
            OnGroupRequested;

        inputHandler.SocketRequested +=
            OnSocketRequested;
    }

    private void BindToolbar()
    {
        ui.Toolbar.NewRequested +=
            CreateNewRoom;

        ui.Toolbar.LoadRequested +=
            LoadRoom;

        ui.Toolbar.SaveRequested +=
            SaveRoom;

        ui.Toolbar.UndoRequested +=
            PerformUndo;

        ui.Toolbar.RedoRequested +=
            PerformRedo;

        ui.Toolbar.ValidateRequested +=
            ValidateRoom;
    }

    private void BindLayers()
    {
        ui.Layers.AddRequested +=
            AddLayer;

        ui.Layers.DuplicateRequested +=
            DuplicateLayer;

        ui.Layers.DeleteRequested +=
            DeleteLayer;

        ui.Layers.MoveUpRequested +=
            MoveLayerUp;

        ui.Layers.MoveDownRequested +=
            MoveLayerDown;

        ui.Layers.RenameRequested +=
            RenameLayer;

        ui.Layers.VisibilityChanged +=
            ChangeLayerVisibility;

        ui.Layers.LockChanged +=
            ChangeLayerLock;
    }

    private void BindInspector()
    {
        ui.Inspector.RoomResizeRequested +=
            ResizeRoom;

        ui.Inspector.CellTypeChanged +=
            ChangeSelectedCellType;

        ui.Inspector.CellEnabledChanged +=
            ChangeSelectedCellEnabled;

        ui.Inspector.GroupIdChanged +=
            ChangeGroupId;

        ui.Inspector.GroupLabelChanged +=
            ChangeGroupLabel;

        ui.Inspector.ClearGroupRequested +=
            DeleteSelectedGroup;

        ui.Inspector.SocketIdChanged +=
            ChangeSocketId;

        ui.Inspector.SocketDirectionChanged +=
            ChangeSocketDirection;

        ui.Inspector.SocketRoleChanged +=
            ChangeSocketRole;

        ui.Inspector.SocketTypeChanged +=
            ChangeSocketType;

        ui.Inspector.SocketWidthChanged +=
            ChangeSocketWidth;

        ui.Inspector.RemoveSocketRequested +=
            DeleteSelectedSocket;
    }

    private void CreateNewRoom()
    {
        if (!ConfirmDiscardUnsavedChanges())
            return;

        string path =
            EditorUtility.SaveFilePanelInProject(
                "Create Room Definition",
                "NewRoom",
                "asset",
                "Choose where to save the RoomDefinition."
            );

        if (string.IsNullOrWhiteSpace(path))
            return;

        RoomDefinition room =
            ScriptableObject.CreateInstance<
                RoomDefinition
            >();

        room.Initialize(
            20,
            20
        );

        AssetDatabase.CreateAsset(
            room,
            path
        );

        AssetDatabase.SaveAssets();

        SetRoom(
            room
        );

        Selection.activeObject =
            room;

        ui.SetStatus(
            $"Created {room.name}"
        );
    }

    private void LoadRoom()
    {
        if (!ConfirmDiscardUnsavedChanges())
            return;

        string absolutePath =
            EditorUtility.OpenFilePanel(
                "Load Room Definition",
                Application.dataPath,
                "asset"
            );

        if (string.IsNullOrWhiteSpace(absolutePath))
            return;

        string assetPath =
            FileUtil.GetProjectRelativePath(
                absolutePath
            );

        if (string.IsNullOrWhiteSpace(assetPath))
            return;

        RoomDefinition room =
            AssetDatabase.LoadAssetAtPath<
                RoomDefinition
            >(
                assetPath
            );

        if (room == null)
        {
            EditorUtility.DisplayDialog(
                "Invalid Room",
                "The selected asset is not a RoomDefinition.",
                "OK"
            );

            return;
        }

        room.EnsureIntegrity();

        SetRoom(
            room
        );

        Selection.activeObject =
            room;

        ui.SetStatus(
            $"Loaded {room.name}"
        );
    }

    private void SaveRoom()
    {
        RoomDefinition room =
            context.CurrentRoom;

        if (room == null)
            return;

        EditorUtility.SetDirty(
            room
        );

        AssetDatabase.SaveAssets();

        ui.Toolbar.SetDirty(
            false
        );

        ui.SetStatus(
            $"Saved {room.name}"
        );
    }

    private void PerformUndo()
    {
        Undo.PerformUndo();
    }

    private void PerformRedo()
    {
        Undo.PerformRedo();
    }

    private void ValidateRoom()
    {
        RoomDefinition room =
            context.CurrentRoom;

        if (room == null)
        {
            ui.Toolbar.SetValidationState(
                "No room"
            );

            ui.SetStatus(
                "No Room loaded."
            );

            return;
        }

        RoomValidationResult result =
            RoomValidator.Validate(
                room,
                ui.Preview.GenerationSettings,
                false
            );

        if (result.HasErrors)
        {
            ui.Toolbar.SetValidationState(
                $"{result.ErrorCount} error(s)"
            );
        }
        else if (result.HasWarnings)
        {
            ui.Toolbar.SetValidationState(
                $"{result.WarningCount} warning(s)"
            );
        }
        else
        {
            ui.Toolbar.SetValidationState(
                "Valid"
            );
        }

        ui.SetStatus(
            result.GetSummary()
        );

        foreach (RoomValidationIssue issue in result.Issues)
        {
            switch (issue.Severity)
            {
                case RoomValidationSeverity.Error:
                    Debug.LogError(
                        $"[RoomValidator] {issue.Message}",
                        room
                    );
                    break;

                case RoomValidationSeverity.Warning:
                    Debug.LogWarning(
                        $"[RoomValidator] {issue.Message}",
                        room
                    );
                    break;

                default:
                    Debug.Log(
                        $"[RoomValidator] {issue.Message}",
                        room
                    );
                    break;
            }
        }
    }

    private void AddLayer()
    {
        RoomDefinition room =
            context.CurrentRoom;

        if (room == null)
            return;

        RecordImmediateUndo(
            "Add Layer"
        );

        RoomLayerData layer =
            room.AddLayer(
                $"Layer {room.Layers.Count + 1}"
            );

        context.SetActiveLayer(
            layer
        );

        MarkRoomDirty();
        RefreshAll();

        ui.SetStatus(
            $"Created {layer.DisplayName}"
        );
    }

    private void DuplicateLayer()
    {
        RoomDefinition room =
            context.CurrentRoom;

        RoomLayerData layer =
            context.ActiveLayer;

        if (
            room == null ||
            layer == null)
        {
            return;
        }

        RecordImmediateUndo(
            "Duplicate Layer"
        );

        RoomLayerData clone =
            room.DuplicateLayer(
                layer
            );

        if (clone == null)
            return;

        context.SetActiveLayer(
            clone
        );

        MarkRoomDirty();
        RefreshAll();

        ui.SetStatus(
            $"Duplicated {layer.DisplayName}"
        );
    }

    private void DeleteLayer()
    {
        RoomDefinition room =
            context.CurrentRoom;

        RoomLayerData layer =
            context.ActiveLayer;

        if (
            room == null ||
            layer == null)
        {
            return;
        }

        if (room.Layers.Count <= 1)
        {
            ui.SetStatus(
                "A Room must contain at least one Layer."
            );

            return;
        }

        foreach (CellGroupData group in room.Groups)
        {
            if (
                group != null &&
                group.LayerId == layer.Id)
            {
                ui.SetStatus(
                    $"Cannot delete '{layer.DisplayName}' while CellGroups still reference it."
                );

                return;
            }
        }

        bool confirm =
            EditorUtility.DisplayDialog(
                "Delete Layer",
                $"Delete '{layer.DisplayName}'?",
                "Delete",
                "Cancel"
            );

        if (!confirm)
            return;

        int index =
            IndexOfLayer(
                room,
                layer
            );

        RecordImmediateUndo(
            "Delete Layer"
        );

        if (!room.RemoveLayer(layer))
            return;

        index =
            Mathf.Clamp(
                index,
                0,
                room.Layers.Count - 1
            );

        context.SetActiveLayer(
            room.Layers[index]
        );

        MarkRoomDirty();
        RefreshAll();

        ui.SetStatus(
            "Layer deleted"
        );
    }

    private void MoveLayerUp()
    {
        MoveLayer(-1);
    }

    private void MoveLayerDown()
    {
        MoveLayer(1);
    }

    private void MoveLayer(
        int direction)
    {
        RoomDefinition room =
            context.CurrentRoom;

        RoomLayerData layer =
            context.ActiveLayer;

        if (
            room == null ||
            layer == null)
        {
            return;
        }

        RecordImmediateUndo(
            "Move Layer"
        );

        if (
            !room.MoveLayer(
                layer,
                direction
            ))
        {
            return;
        }

        MarkRoomDirty();
        RefreshAll();
    }

    private void RenameLayer(
        RoomLayerData layer,
        string newName)
    {
        if (
            layer == null ||
            string.IsNullOrWhiteSpace(newName))
        {
            return;
        }

        newName =
            newName.Trim();

        if (layer.DisplayName == newName)
            return;

        RecordImmediateUndo(
            "Rename Layer"
        );

        layer.SetDisplayName(
            newName
        );

        MarkRoomDirty();
        RefreshAll();

        ui.SetStatus(
            $"Layer renamed to {newName}"
        );
    }

    private void ChangeLayerVisibility(
        RoomLayerData layer,
        bool visible)
    {
        if (layer == null)
            return;

        if (layer.Visible == visible)
            return;

        RecordImmediateUndo(
            "Change Layer Visibility"
        );

        layer.SetVisible(
            visible
        );

        MarkRoomDirty(
            false
        );

        ui.Layers.SetLayers(
            context.CurrentRoom.Layers
        );

        ui.GridCanvas.Refresh();

        ui.SetStatus(
            visible
                ? $"{layer.DisplayName} is visible"
                : $"{layer.DisplayName} is hidden"
        );
    }

    private void ChangeLayerLock(
        RoomLayerData layer,
        bool locked)
    {
        if (layer == null)
            return;

        if (layer.Locked == locked)
            return;

        RecordImmediateUndo(
            "Change Layer Lock"
        );

        layer.SetLocked(
            locked
        );

        MarkRoomDirty(
            false
        );

        ui.Layers.SetLayers(
            context.CurrentRoom.Layers
        );

        ui.SetStatus(
            locked
                ? $"{layer.DisplayName} is locked"
                : $"{layer.DisplayName} is unlocked"
        );
    }

    private void OnSelectionRequested(
        IReadOnlyList<Vector2Int> cells)
    {
        if (context.CurrentRoom == null)
            return;

        if (
            cells == null ||
            cells.Count == 0)
        {
            context.ClearSelection();
            return;
        }

        if (cells.Count > 1)
        {
            context.SetSelectedCells(
                cells
            );

            return;
        }

        Vector2Int position =
            cells[0];

        RoomSocketData socket =
            context.CurrentRoom.GetSocketAt(
                position
            );

        if (socket != null)
        {
            context.SetSelectedSocket(
                socket
            );

            return;
        }

        CellGroupData group =
            GetGroupAtCellOnLayer(
                context.CurrentRoom,
                position,
                context.ActiveLayer?.Id
            )
            ??
            context.CurrentRoom.GetGroupAtCell(
                position
            );

        if (group != null)
        {
            context.SetSelectedGroup(
                group
            );

            return;
        }

        context.SetSelectedCell(
            position
        );
    }

    private void OnPaintRequested(
        IReadOnlyList<Vector2Int> positions,
        CellTypeDefinition type)
    {
        if (
            type == null ||
            !CanEditActiveLayer())
        {
            return;
        }

        EnsureStrokeUndo(
            "Paint Cells"
        );

        RoomDefinition room =
            context.CurrentRoom;

        foreach (Vector2Int position in positions)
        {
            CellData cell =
                room.GetCell(
                    context.ActiveLayer,
                    position
                );

            if (cell == null)
                continue;

            cell.SetType(
                type
            );

            cell.SetEnabled(
                true
            );
        }

        MarkRoomDirty();
        ui.GridCanvas.Refresh();
    }

    private void OnEraseRequested(
        IReadOnlyList<Vector2Int> positions)
    {
        if (!CanEditActiveLayer())
            return;

        EnsureStrokeUndo(
            "Erase Cells"
        );

        foreach (Vector2Int position in positions)
        {
            CellData cell =
                context.CurrentRoom.GetCell(
                    context.ActiveLayer,
                    position
                );

            cell?.SetType(
                null
            );
        }

        MarkRoomDirty();
        ui.GridCanvas.Refresh();
    }

    private void OnFillRequested(
        Vector2Int origin,
        CellTypeDefinition replacement)
    {
        if (
            replacement == null ||
            !CanEditActiveLayer())
        {
            return;
        }

        RoomDefinition room =
            context.CurrentRoom;

        RoomLayerData layer =
            context.ActiveLayer;

        CellData originCell =
            room.GetCell(
                layer,
                origin
            );

        if (originCell == null)
            return;

        CellTypeDefinition target =
            originCell.Type;

        if (
            ReferenceEquals(
                target,
                replacement
            ))
        {
            return;
        }

        EnsureStrokeUndo(
            "Fill Cells"
        );

        Queue<Vector2Int> queue =
            new();

        HashSet<Vector2Int> visited =
            new();

        queue.Enqueue(
            origin
        );

        while (queue.Count > 0)
        {
            Vector2Int current =
                queue.Dequeue();

            if (!visited.Add(current))
                continue;

            CellData cell =
                room.GetCell(
                    layer,
                    current
                );

            if (
                cell == null ||
                !ReferenceEquals(
                    cell.Type,
                    target
                ))
            {
                continue;
            }

            cell.SetType(
                replacement
            );

            TryQueue(current + Vector2Int.up);
            TryQueue(current + Vector2Int.down);
            TryQueue(current + Vector2Int.left);
            TryQueue(current + Vector2Int.right);
        }

        void TryQueue(
            Vector2Int position)
        {
            if (context.IsInside(position))
                queue.Enqueue(position);
        }

        MarkRoomDirty();
        ui.GridCanvas.Refresh();
    }

    private void OnGroupRequested(
        IReadOnlyList<Vector2Int> cells)
    {
        RoomDefinition room =
            context.CurrentRoom;

        RoomLayerData activeLayer =
            context.ActiveLayer;

        if (
            room == null ||
            activeLayer == null ||
            cells == null ||
            cells.Count == 0)
        {
            return;
        }

        if (cells.Count == 1)
        {
            CellGroupData existing =
                GetGroupAtCellOnLayer(
                    room,
                    cells[0],
                    activeLayer.Id
                );

            if (existing != null)
            {
                context.SetSelectedGroup(
                    existing
                );

                return;
            }
        }

        foreach (Vector2Int position in cells)
        {
            CellGroupData existing =
                GetGroupAtCellOnLayer(
                    room,
                    position,
                    activeLayer.Id
                );

            if (existing != null)
            {
                context.SetSelectedGroup(
                    existing
                );

                ui.SetStatus(
                    "One or more Cells already belong to a Group on this Layer."
                );

                return;
            }
        }

        EnsureStrokeUndo(
            "Create Cell Group"
        );

        CellGroupData newGroup =
            room.AddGroup(
                cells,
                $"Group {room.Groups.Count + 1}"
            );

        newGroup.SetLayerId(
            activeLayer.Id
        );

        context.SetSelectedGroup(
            newGroup
        );

        MarkRoomDirty();
        ui.GridCanvas.Refresh();
    }

    private void ChangeGroupId(
        string value)
    {
        CellGroupData group =
            context.SelectedGroup;

        if (
            group == null ||
            string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        RecordImmediateUndo(
            "Change Group ID"
        );

        group.SetId(
            value
        );

        MarkRoomDirty();
    }

    private void ChangeGroupLabel(
        string value)
    {
        CellGroupData group =
            context.SelectedGroup;

        if (group == null)
            return;

        RecordImmediateUndo(
            "Rename Group"
        );

        group.SetLabel(
            value
        );

        MarkRoomDirty();
        RefreshInspector();
    }

    private void DeleteSelectedGroup()
    {
        CellGroupData group =
            context.SelectedGroup;

        if (
            group == null ||
            context.CurrentRoom == null)
        {
            return;
        }

        RecordImmediateUndo(
            "Delete Group"
        );

        context.CurrentRoom.RemoveGroup(
            group
        );

        context.ClearSelection();

        MarkRoomDirty();
        ui.GridCanvas.Refresh();
    }

    private void OnSocketRequested(
        Vector2Int position)
    {
        RoomDefinition room =
            context.CurrentRoom;

        if (room == null)
            return;

        RoomSocketData socket =
            room.GetSocketAt(
                position
            );

        if (socket != null)
        {
            context.SetSelectedSocket(
                socket
            );

            return;
        }

        EnsureStrokeUndo(
            "Create Room Socket"
        );

        socket =
            room.AddSocket(
                position
            );

        context.SetSelectedSocket(
            socket
        );

        MarkRoomDirty();
        ui.GridCanvas.Refresh();
    }

    private void ChangeSocketId(
        string value)
    {
        if (context.SelectedSocket == null)
            return;

        RecordImmediateUndo(
            "Change Socket ID"
        );

        context.SelectedSocket.SetId(
            value
        );

        MarkRoomDirty();
    }

    private void ChangeSocketDirection(
        string value)
    {
        if (context.SelectedSocket == null)
            return;

        if (
            !Enum.TryParse(
                value,
                out SocketDirection direction
            ))
        {
            return;
        }

        RecordImmediateUndo(
            "Change Socket Direction"
        );

        context.SelectedSocket.SetDirection(
            direction
        );

        MarkRoomDirty();
        ui.GridCanvas.Refresh();
    }

    private void ChangeSocketRole(
        string value)
    {
        if (context.SelectedSocket == null)
            return;

        if (
            !Enum.TryParse(
                value,
                out SocketRole role
            ))
        {
            return;
        }

        RecordImmediateUndo(
            "Change Socket Role"
        );

        context.SelectedSocket.SetRole(
            role
        );

        MarkRoomDirty();
    }

    private void ChangeSocketType(
        string value)
    {
        if (context.SelectedSocket == null)
            return;

        RecordImmediateUndo(
            "Change Socket Type"
        );

        context.SelectedSocket.SetType(
            value
        );

        MarkRoomDirty();
    }

    private void ChangeSocketWidth(
        int value)
    {
        if (context.SelectedSocket == null)
            return;

        RecordImmediateUndo(
            "Change Socket Width"
        );

        context.SelectedSocket.SetWidth(
            value
        );

        MarkRoomDirty();
    }

    private void DeleteSelectedSocket()
    {
        RoomSocketData socket =
            context.SelectedSocket;

        if (
            socket == null ||
            context.CurrentRoom == null)
        {
            return;
        }

        RecordImmediateUndo(
            "Delete Socket"
        );

        context.CurrentRoom.RemoveSocket(
            socket
        );

        context.ClearSelection();

        MarkRoomDirty();
        ui.GridCanvas.Refresh();
    }

    private void ChangeSelectedCellType(
        CellTypeDefinition type)
    {
        CellData cell =
            GetSelectedCell();

        if (
            cell == null ||
            !CanEditActiveLayer())
        {
            return;
        }

        RecordImmediateUndo(
            "Change Cell Type"
        );

        cell.SetType(
            type
        );

        MarkRoomDirty();
        ui.GridCanvas.Refresh();
    }

    private void ChangeSelectedCellEnabled(
        bool enabled)
    {
        CellData cell =
            GetSelectedCell();

        if (
            cell == null ||
            !CanEditActiveLayer())
        {
            return;
        }

        RecordImmediateUndo(
            "Change Cell Enabled"
        );

        cell.SetEnabled(
            enabled
        );

        MarkRoomDirty();
        ui.GridCanvas.Refresh();
    }

    private CellData GetSelectedCell()
    {
        if (
            context.CurrentRoom == null ||
            context.ActiveLayer == null ||
            !context.SelectedCell.HasValue)
        {
            return null;
        }

        return
            context.CurrentRoom.GetCell(
                context.ActiveLayer,
                context.SelectedCell.Value
            );
    }

    private void ResizeRoom(
        int width,
        int height)
    {
        RoomDefinition room =
            context.CurrentRoom;

        if (room == null)
            return;

        RecordImmediateUndo(
            "Resize Room"
        );

        room.Resize(
            width,
            height
        );

        context.SetGridMetrics(
            room.Width,
            room.Height
        );

        context.ClearSelection();

        MarkRoomDirty();
        RefreshAll();
    }

    private Color? GetCellColor(
        Vector2Int position)
    {
        RoomDefinition room =
            context.CurrentRoom;

        if (room == null)
            return null;

        Color composite =
            EditorGUIUtility.isProSkin
                ? new Color(
                    0.12f,
                    0.13f,
                    0.15f,
                    1f
                )
                : new Color(
                    0.83f,
                    0.83f,
                    0.84f,
                    1f
                );

        bool hasContent =
            false;

        foreach (RoomLayerData layer in room.Layers)
        {
            if (
                layer == null ||
                !layer.Visible ||
                ReferenceEquals(
                    layer,
                    context.ActiveLayer
                ))
            {
                continue;
            }

            CellData cell =
                room.GetCell(
                    layer,
                    position
                );

            if (
                cell == null ||
                cell.Type == null)
            {
                continue;
            }

            Color color =
                cell.Type.EditorColor;

            float opacity =
                InactiveLayerOpacity;

            if (!cell.Enabled)
                opacity *= 0.35f;

            color.a =
                Mathf.Clamp01(
                    color.a *
                    opacity
                );

            composite =
                BlendColor(
                    composite,
                    color
                );

            hasContent =
                true;
        }

        RoomLayerData activeLayer =
            context.ActiveLayer;

        if (
            activeLayer != null &&
            activeLayer.Visible)
        {
            CellData activeCell =
                room.GetCell(
                    activeLayer,
                    position
                );

            if (
                activeCell != null &&
                activeCell.Type != null)
            {
                Color color =
                    activeCell.Type.EditorColor;

                float opacity =
                    activeCell.Enabled
                        ? 1f
                        : 0.35f;

                color.a =
                    Mathf.Clamp01(
                        color.a *
                        opacity
                    );

                composite =
                    BlendColor(
                        composite,
                        color
                    );

                hasContent =
                    true;
            }
        }

        return
            hasContent
                ? composite
                : null;
    }

    private static Color BlendColor(
        Color background,
        Color overlay)
    {
        float alpha =
            Mathf.Clamp01(
                overlay.a
            );

        return new Color(
            Mathf.Lerp(
                background.r,
                overlay.r,
                alpha
            ),
            Mathf.Lerp(
                background.g,
                overlay.g,
                alpha
            ),
            Mathf.Lerp(
                background.b,
                overlay.b,
                alpha
            ),
            1f
        );
    }

    private RoomSocketData GetSocketAt(
        Vector2Int position)
    {
        return
            context.CurrentRoom?.GetSocketAt(
                position
            );
    }

    private bool CanEditActiveLayer()
    {
        RoomLayerData layer =
            context.ActiveLayer;

        if (
            context.CurrentRoom == null ||
            layer == null)
        {
            return false;
        }

        if (layer.Locked)
        {
            ui.SetStatus(
                $"'{layer.DisplayName}' is locked."
            );

            return false;
        }

        return true;
    }

    private void OnStrokeStarted()
    {
        strokeActive =
            true;

        strokeUndoRecorded =
            false;

        strokeUndoGroup =
            -1;
    }

    private void EnsureStrokeUndo(
        string name)
    {
        if (context.CurrentRoom == null)
            return;

        if (!strokeActive)
        {
            RecordImmediateUndo(
                name
            );

            return;
        }

        if (strokeUndoRecorded)
            return;

        Undo.IncrementCurrentGroup();

        strokeUndoGroup =
            Undo.GetCurrentGroup();

        Undo.SetCurrentGroupName(
            name
        );

        Undo.RegisterCompleteObjectUndo(
            context.CurrentRoom,
            name
        );

        strokeUndoRecorded =
            true;
    }

    private void OnStrokeEnded()
    {
        if (
            strokeUndoRecorded &&
            strokeUndoGroup >= 0)
        {
            Undo.CollapseUndoOperations(
                strokeUndoGroup
            );
        }

        strokeActive =
            false;

        strokeUndoRecorded =
            false;

        strokeUndoGroup =
            -1;
    }

    private void RecordImmediateUndo(
        string name)
    {
        if (context.CurrentRoom == null)
            return;

        Undo.RegisterCompleteObjectUndo(
            context.CurrentRoom,
            name
        );
    }

    private void MarkRoomDirty(
        bool refreshPreview = true)
    {
        if (context.CurrentRoom == null)
            return;

        EditorUtility.SetDirty(
            context.CurrentRoom
        );

        ui.Toolbar.SetDirty(
            true
        );

        if (refreshPreview)
        {
            ui.Preview.NotifyRoomContentChanged();
        }
    }

    private void SetRoom(
        RoomDefinition room)
    {
        if (room != null)
            room.EnsureIntegrity();

        context.SetRoom(
            room
        );

        context.SetActiveLayer(
            room != null &&
            room.Layers.Count > 0
                ? room.Layers[0]
                : null
        );

        ui.Toolbar.SetDirty(
            false
        );

        ui.Toolbar.SetValidationState(
            "Not validated"
        );

        RefreshAll();

        ui.Preview.NotifyRoomContentChanged();
    }

    private void RefreshAll()
    {
        RoomDefinition room =
            context.CurrentRoom;

        if (room == null)
        {
            ui.Toolbar.SetRoomName(
                "Untitled Room"
            );

            ui.Layers.SetLayers(
                Array.Empty<
                    RoomLayerData
                >()
            );

            ui.Inspector.ShowNoRoom();

            ui.GridCanvas.Refresh();

            return;
        }

        room.EnsureIntegrity();

        context.SetGridMetrics(
            room.Width,
            room.Height
        );

        if (
            context.ActiveLayer == null ||
            IndexOfLayer(
                room,
                context.ActiveLayer
            ) < 0)
        {
            context.SetActiveLayer(
                room.Layers.Count > 0
                    ? room.Layers[0]
                    : null
            );
        }

        ui.Toolbar.SetRoomName(
            room.name
        );

        ui.Layers.SetLayers(
            room.Layers
        );

        ui.Layers.SelectLayer(
            context.ActiveLayer
        );

        ui.GridCanvas.Refresh();

        RefreshInspector();
    }

    private void RefreshAfterLayerChanged()
    {
        ui.Layers.SelectLayer(
            context.ActiveLayer
        );

        ui.GridCanvas.Refresh();

        RefreshInspector();
    }

    private void RefreshInspector()
    {
        RoomDefinition room =
            context.CurrentRoom;

        if (room == null)
        {
            ui.Inspector.ShowNoRoom();
            return;
        }

        if (context.SelectedSocket != null)
        {
            ui.Inspector.ShowSocket(
                context.SelectedSocket
            );

            return;
        }

        if (context.SelectedGroup != null)
        {
            ui.Inspector.ShowGroup(
                context.SelectedGroup
            );

            return;
        }

        if (context.SelectionCount > 1)
        {
            ui.Inspector.ShowMultipleCells(
                context.SelectionCount
            );

            return;
        }

        if (context.SelectedCell.HasValue)
        {
            CellData cell =
                GetSelectedCell();

            if (cell != null)
            {
                ui.Inspector.ShowCell(
                    context.SelectedCell.Value,
                    cell.Type,
                    cell.Enabled
                );

                return;
            }
        }

        ui.Inspector.ShowRoom(
            room.Width,
            room.Height
        );
    }

    private void OnUndoRedoPerformed()
    {
        context.CurrentRoom
            ?.EnsureIntegrity();

        RefreshAll();

        ui.Preview.NotifyRoomContentChanged();

        ui.SetStatus(
            "Undo / Redo applied"
        );
    }

    private bool ConfirmDiscardUnsavedChanges()
    {
        RoomDefinition room =
            context.CurrentRoom;

        if (
            room == null ||
            !EditorUtility.IsDirty(room))
        {
            return true;
        }

        int result =
            EditorUtility.DisplayDialogComplex(
                "Unsaved Room",
                $"'{room.name}' contains unsaved changes.",
                "Save",
                "Cancel",
                "Discard"
            );

        switch (result)
        {
            case 0:
                SaveRoom();
                return true;

            case 2:
                return true;

            default:
                return false;
        }
    }

    private static CellGroupData GetGroupAtCellOnLayer(
        RoomDefinition room,
        Vector2Int position,
        string layerId)
    {
        if (
            room == null ||
            string.IsNullOrWhiteSpace(layerId))
        {
            return null;
        }

        foreach (CellGroupData group in room.Groups)
        {
            if (
                group != null &&
                group.LayerId == layerId &&
                group.Contains(position))
            {
                return group;
            }
        }

        return null;
    }

    private static int IndexOfLayer(
        RoomDefinition room,
        RoomLayerData layer)
    {
        if (
            room == null ||
            layer == null)
        {
            return -1;
        }

        for (
            int i = 0;
            i < room.Layers.Count;
            i++)
        {
            if (
                ReferenceEquals(
                    room.Layers[i],
                    layer
                ))
            {
                return i;
            }
        }

        return -1;
    }

    private void UnbindInput()
    {
        if (inputHandler == null)
            return;

        inputHandler.StrokeStarted -=
            OnStrokeStarted;

        inputHandler.StrokeEnded -=
            OnStrokeEnded;

        inputHandler.SelectionRequested -=
            OnSelectionRequested;

        inputHandler.PaintRequested -=
            OnPaintRequested;

        inputHandler.EraseRequested -=
            OnEraseRequested;

        inputHandler.FillRequested -=
            OnFillRequested;

        inputHandler.GroupRequested -=
            OnGroupRequested;

        inputHandler.SocketRequested -=
            OnSocketRequested;

        inputHandler =
            null;
    }

    public void Dispose()
    {
        UnbindInput();

        ui.Toolbar.NewRequested -=
            CreateNewRoom;

        ui.Toolbar.LoadRequested -=
            LoadRoom;

        ui.Toolbar.SaveRequested -=
            SaveRoom;

        ui.Toolbar.UndoRequested -=
            PerformUndo;

        ui.Toolbar.RedoRequested -=
            PerformRedo;

        ui.Toolbar.ValidateRequested -=
            ValidateRoom;

        ui.Layers.AddRequested -=
            AddLayer;

        ui.Layers.DuplicateRequested -=
            DuplicateLayer;

        ui.Layers.DeleteRequested -=
            DeleteLayer;

        ui.Layers.MoveUpRequested -=
            MoveLayerUp;

        ui.Layers.MoveDownRequested -=
            MoveLayerDown;

        ui.Layers.RenameRequested -=
            RenameLayer;

        ui.Layers.VisibilityChanged -=
            ChangeLayerVisibility;

        ui.Layers.LockChanged -=
            ChangeLayerLock;

        ui.Inspector.RoomResizeRequested -=
            ResizeRoom;

        ui.Inspector.CellTypeChanged -=
            ChangeSelectedCellType;

        ui.Inspector.CellEnabledChanged -=
            ChangeSelectedCellEnabled;

        ui.Inspector.GroupIdChanged -=
            ChangeGroupId;

        ui.Inspector.GroupLabelChanged -=
            ChangeGroupLabel;

        ui.Inspector.ClearGroupRequested -=
            DeleteSelectedGroup;

        ui.Inspector.SocketIdChanged -=
            ChangeSocketId;

        ui.Inspector.SocketDirectionChanged -=
            ChangeSocketDirection;

        ui.Inspector.SocketRoleChanged -=
            ChangeSocketRole;

        ui.Inspector.SocketTypeChanged -=
            ChangeSocketType;

        ui.Inspector.SocketWidthChanged -=
            ChangeSocketWidth;

        ui.Inspector.RemoveSocketRequested -=
            DeleteSelectedSocket;

        context.RoomChanged -=
            RefreshAll;

        context.ActiveLayerChanged -=
            RefreshAfterLayerChanged;

        context.SelectionChanged -=
            RefreshInspector;

        Undo.undoRedoPerformed -=
            OnUndoRedoPerformed;
    }
}
