using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class RoomEditorController
    : IDisposable
{
    private readonly RoomEditorContext
        context;


    private readonly RoomEditorUI
        ui;


    private RoomEditorInputHandler
        inputHandler;


    private bool strokeActive;


    private bool strokeUndoRecorded;


    private int strokeUndoGroup =
        -1;


    public RoomEditorController(
        RoomEditorContext context,
        RoomEditorUI ui)
    {
        this.context =
            context;


        this.ui =
            ui;


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


    // =========================================================
    // Input
    // =========================================================

    public void BindInput(
        RoomEditorInputHandler input)
    {
        UnbindInput();


        inputHandler =
            input;


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


    // =========================================================
    // Bind
    // =========================================================

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


    // =========================================================
    // Room
    // =========================================================

    private void CreateNewRoom()
    {
        string path =
            EditorUtility
                .SaveFilePanelInProject(
                    "Create Room Definition",
                    "NewRoom",
                    "asset",
                    "Choose where to save the RoomDefinition."
                );


        if (
            string.IsNullOrWhiteSpace(
                path
            ))
        {
            return;
        }


        RoomDefinition room =
            ScriptableObject
                .CreateInstance<RoomDefinition>();


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
    }


    private void LoadRoom()
    {
        string absolutePath =
            EditorUtility.OpenFilePanel(
                "Load Room Definition",
                Application.dataPath,
                "asset"
            );


        if (
            string.IsNullOrWhiteSpace(
                absolutePath
            ))
        {
            return;
        }


        string assetPath =
            FileUtil.GetProjectRelativePath(
                absolutePath
            );


        RoomDefinition room =
            AssetDatabase
                .LoadAssetAtPath<RoomDefinition>(
                    assetPath
                );


        if (room == null)
            return;


        room.EnsureIntegrity();


        SetRoom(
            room
        );
    }


    private void SaveRoom()
    {
        if (
            context.CurrentRoom ==
            null)
        {
            return;
        }


        EditorUtility.SetDirty(
            context.CurrentRoom
        );


        AssetDatabase.SaveAssets();


        ui.Toolbar.SetDirty(
            false
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
        ui.Toolbar.SetValidationState(
            context.CurrentRoom !=
            null
                ? "Valid"
                : "No room"
        );
    }


    // =========================================================
    // Layer
    // =========================================================

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
    }


    private void DuplicateLayer()
    {
        RoomDefinition room =
            context.CurrentRoom;


        if (
            room == null ||
            context.ActiveLayer ==
            null)
        {
            return;
        }


        RecordImmediateUndo(
            "Duplicate Layer"
        );


        RoomLayerData clone =
            room.DuplicateLayer(
                context.ActiveLayer
            );


        context.SetActiveLayer(
            clone
        );


        MarkRoomDirty();

        RefreshAll();
    }


    private void DeleteLayer()
    {
        RoomDefinition room =
            context.CurrentRoom;


        RoomLayerData layer =
            context.ActiveLayer;


        if (
            room == null ||
            layer == null ||
            room.Layers.Count <=
            1)
        {
            return;
        }


        RecordImmediateUndo(
            "Delete Layer"
        );


        room.RemoveLayer(
            layer
        );


        context.SetActiveLayer(
            room.Layers[0]
        );


        MarkRoomDirty();

        RefreshAll();
    }


    private void MoveLayerUp()
    {
        MoveLayer(
            -1
        );
    }


    private void MoveLayerDown()
    {
        MoveLayer(
            1
        );
    }


    private void MoveLayer(
        int direction)
    {
        if (
            context.CurrentRoom ==
            null ||
            context.ActiveLayer ==
            null)
        {
            return;
        }


        RecordImmediateUndo(
            "Move Layer"
        );


        context.CurrentRoom.MoveLayer(
            context.ActiveLayer,
            direction
        );


        MarkRoomDirty();

        RefreshAll();
    }


    private void RenameLayer(
        RoomLayerData layer,
        string newName)
    {
        if (
            layer == null ||
            string.IsNullOrWhiteSpace(
                newName
            ))
        {
            RefreshAll();

            return;
        }


        if (
            layer.DisplayName ==
            newName)
        {
            return;
        }


        RecordImmediateUndo(
            "Rename Layer"
        );


        layer.SetDisplayName(
            newName
        );


        MarkRoomDirty();

        RefreshAll();
    }


    private void ChangeLayerVisibility(
        RoomLayerData layer,
        bool visible)
    {
        if (layer == null)
            return;


        RecordImmediateUndo(
            "Change Layer Visibility"
        );


        layer.SetVisible(
            visible
        );


        MarkRoomDirty();

        ui.GridCanvas.Refresh();
    }


    private void ChangeLayerLock(
        RoomLayerData layer,
        bool locked)
    {
        if (layer == null)
            return;


        RecordImmediateUndo(
            "Change Layer Lock"
        );


        layer.SetLocked(
            locked
        );


        MarkRoomDirty();

        RefreshAll();
    }


    // =========================================================
    // Select
    // =========================================================

    private void OnSelectionRequested(
        IReadOnlyList<Vector2Int> cells)
    {
        if (
            cells == null ||
            cells.Count ==
            0)
        {
            context.ClearSelection();

            return;
        }


        if (
            cells.Count >
            1)
        {
            context.SetSelectedCells(
                cells
            );


            return;
        }


        Vector2Int cell =
            cells[0];


        RoomSocketData socket =
            context.CurrentRoom
                ?.GetSocketAt(
                    cell
                );


        if (socket != null)
        {
            context.SetSelectedSocket(
                socket
            );


            return;
        }


        CellGroupData group =
            context.CurrentRoom
                ?.GetGroupAtCell(
                    cell
                );


        if (group != null)
        {
            context.SetSelectedGroup(
                group
            );


            return;
        }


        context.SetSelectedCell(
            cell
        );
    }


    // =========================================================
    // Paint
    // =========================================================

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


        foreach (
            Vector2Int position
            in positions)
        {
            CellData cell =
                context.CurrentRoom
                    .GetCell(
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


    // =========================================================
    // Erase
    // =========================================================

    private void OnEraseRequested(
        IReadOnlyList<Vector2Int> positions)
    {
        if (!CanEditActiveLayer())
            return;


        EnsureStrokeUndo(
            "Erase Cells"
        );


        foreach (
            Vector2Int position
            in positions)
        {
            CellData cell =
                context.CurrentRoom
                    .GetCell(
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


    // =========================================================
    // Fill
    // =========================================================

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


        while (
            queue.Count >
            0)
        {
            Vector2Int current =
                queue.Dequeue();


            if (
                !visited.Add(
                    current
                ))
            {
                continue;
            }


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


            TryEnqueue(
                current +
                Vector2Int.up
            );


            TryEnqueue(
                current +
                Vector2Int.down
            );


            TryEnqueue(
                current +
                Vector2Int.left
            );


            TryEnqueue(
                current +
                Vector2Int.right
            );
        }


        void TryEnqueue(
            Vector2Int position)
        {
            if (
                context.IsInside(
                    position
                ))
            {
                queue.Enqueue(
                    position
                );
            }
        }


        MarkRoomDirty();

        ui.GridCanvas.Refresh();
    }


    // =========================================================
    // Group
    // =========================================================

    private void OnGroupRequested(
        IReadOnlyList<Vector2Int> cells)
    {
        RoomDefinition room =
            context.CurrentRoom;


        if (
            room == null ||
            cells == null ||
            cells.Count ==
            0)
        {
            return;
        }


        if (
            cells.Count ==
            1)
        {
            CellGroupData existing =
                room.GetGroupAtCell(
                    cells[0]
                );


            if (existing != null)
            {
                context.SetSelectedGroup(
                    existing
                );


                return;
            }
        }


        foreach (
            Vector2Int cell
            in cells)
        {
            CellGroupData existing =
                room.GetGroupAtCell(
                    cell
                );


            if (existing != null)
            {
                context.SetSelectedGroup(
                    existing
                );


                ui.SetStatus(
                    "One or more cells already belong to a group."
                );


                return;
            }
        }


        EnsureStrokeUndo(
            "Create Cell Group"
        );


        CellGroupData group =
            room.AddGroup(
                cells,
                $"Group {room.Groups.Count + 1}"
            );


        context.SetSelectedGroup(
            group
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
            string.IsNullOrWhiteSpace(
                value
            ))
        {
            return;
        }


        CellGroupData other =
            context.CurrentRoom
                .GetGroupById(
                    value
                );


        if (
            other != null &&
            !ReferenceEquals(
                other,
                group
            ))
        {
            ui.SetStatus(
                "Another group already uses this ID."
            );


            RefreshInspector();

            return;
        }


        RecordImmediateUndo(
            "Change Group ID"
        );


        group.SetId(
            value
        );


        MarkRoomDirty();

        RefreshInspector();
    }


    private void ChangeGroupLabel(
        string value)
    {
        if (
            context.SelectedGroup ==
            null)
        {
            return;
        }


        RecordImmediateUndo(
            "Rename Group"
        );


        context.SelectedGroup
            .SetLabel(
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
            context.CurrentRoom ==
            null)
        {
            return;
        }


        RecordImmediateUndo(
            "Delete Group"
        );


        context.CurrentRoom
            .RemoveGroup(
                group
            );


        context.ClearSelection();


        MarkRoomDirty();

        ui.GridCanvas.Refresh();
    }


    // =========================================================
    // Socket
    // =========================================================

    private void OnSocketRequested(
        Vector2Int cell)
    {
        RoomDefinition room =
            context.CurrentRoom;


        if (room == null)
            return;


        RoomSocketData socket =
            room.GetSocketAt(
                cell
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
                cell
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
        RoomSocketData socket =
            context.SelectedSocket;


        if (
            socket == null ||
            string.IsNullOrWhiteSpace(
                value
            ))
        {
            return;
        }


        RoomSocketData other =
            context.CurrentRoom
                .GetSocketById(
                    value
                );


        if (
            other != null &&
            !ReferenceEquals(
                other,
                socket
            ))
        {
            RefreshInspector();

            return;
        }


        RecordImmediateUndo(
            "Change Socket ID"
        );


        socket.SetId(
            value
        );


        MarkRoomDirty();
    }


    private void ChangeSocketDirection(
        string value)
    {
        if (
            context.SelectedSocket ==
            null)
        {
            return;
        }


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


        context.SelectedSocket
            .SetDirection(
                direction
            );


        MarkRoomDirty();
    }


    private void ChangeSocketRole(
        string value)
    {
        if (
            context.SelectedSocket ==
            null)
        {
            return;
        }


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


        context.SelectedSocket
            .SetRole(
                role
            );


        MarkRoomDirty();
    }


    private void ChangeSocketType(
        string value)
    {
        if (
            context.SelectedSocket ==
            null)
        {
            return;
        }


        RecordImmediateUndo(
            "Change Socket Type"
        );


        context.SelectedSocket
            .SetType(
                value
            );


        MarkRoomDirty();
    }


    private void ChangeSocketWidth(
        int value)
    {
        if (
            context.SelectedSocket ==
            null)
        {
            return;
        }


        RecordImmediateUndo(
            "Change Socket Width"
        );


        context.SelectedSocket
            .SetWidth(
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
            context.CurrentRoom ==
            null)
        {
            return;
        }


        RecordImmediateUndo(
            "Delete Socket"
        );


        context.CurrentRoom
            .RemoveSocket(
                socket
            );


        context.ClearSelection();


        MarkRoomDirty();

        ui.GridCanvas.Refresh();
    }


    // =========================================================
    // Inspector Cell
    // =========================================================

    private void ChangeSelectedCellType(
        CellTypeDefinition type)
    {
        CellData cell =
            GetSelectedCell();


        if (cell == null)
            return;


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


        if (cell == null)
            return;


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
            !context.SelectedCell.HasValue ||
            context.CurrentRoom ==
            null ||
            context.ActiveLayer ==
            null)
        {
            return null;
        }


        return
            context.CurrentRoom
                .GetCell(
                    context.ActiveLayer,
                    context.SelectedCell.Value
                );
    }


    // =========================================================
    // Resize
    // =========================================================

    private void ResizeRoom(
        int width,
        int height)
    {
        if (
            context.CurrentRoom ==
            null)
        {
            return;
        }


        RecordImmediateUndo(
            "Resize Room"
        );


        context.CurrentRoom.Resize(
            width,
            height
        );


        context.SetGridMetrics(
            width,
            height
        );


        context.ClearSelection();


        MarkRoomDirty();

        RefreshAll();
    }


    // =========================================================
    // Rendering
    // =========================================================

    private Color? GetCellColor(
        Vector2Int position)
    {
        if (
            context.CurrentRoom ==
            null ||
            context.ActiveLayer ==
            null ||
            !context.ActiveLayer.Visible)
        {
            return null;
        }


        CellData cell =
            context.CurrentRoom
                .GetCell(
                    context.ActiveLayer,
                    position
                );


        if (
            cell?.Type ==
            null)
        {
            return null;
        }


        Color color =
            cell.Type.EditorColor;


        if (!cell.Enabled)
        {
            color.a *=
                0.35f;
        }


        return color;
    }


    private RoomSocketData GetSocketAt(
        Vector2Int position)
    {
        return
            context.CurrentRoom
                ?.GetSocketAt(
                    position
                );
    }


    // =========================================================
    // Validation
    // =========================================================

    private bool CanEditActiveLayer()
    {
        if (
            context.CurrentRoom ==
            null ||
            context.ActiveLayer ==
            null)
        {
            return false;
        }


        if (
            context.ActiveLayer.Locked)
        {
            ui.SetStatus(
                "Active layer is locked."
            );


            return false;
        }


        return true;
    }


    // =========================================================
    // Undo
    // =========================================================

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
        if (
            context.CurrentRoom ==
            null)
        {
            return;
        }


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
            strokeUndoGroup >=
            0)
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
        if (
            context.CurrentRoom ==
            null)
        {
            return;
        }


        Undo.RegisterCompleteObjectUndo(
            context.CurrentRoom,
            name
        );
    }


    private void MarkRoomDirty()
    {
        if (
            context.CurrentRoom ==
            null)
        {
            return;
        }


        EditorUtility.SetDirty(
            context.CurrentRoom
        );


        ui.Toolbar.SetDirty(
            true
        );
    }


    // =========================================================
    // Refresh
    // =========================================================

    private void SetRoom(
        RoomDefinition room)
    {
        context.SetRoom(
            room
        );


        context.SetActiveLayer(
            room != null &&
            room.Layers.Count >
            0
                ? room.Layers[0]
                : null
        );


        RefreshAll();
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
                Array.Empty<RoomLayerData>()
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
            context.ActiveLayer ==
            null)
        {
            context.SetActiveLayer(
                room.Layers[0]
            );
        }


        ui.Toolbar.SetRoomName(
            room.name
        );


        ui.Layers.SetLayers(
            room.Layers
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
        if (
            context.CurrentRoom ==
            null)
        {
            ui.Inspector.ShowNoRoom();

            return;
        }


        if (
            context.SelectedSocket !=
            null)
        {
            ui.Inspector.ShowSocket(
                context.SelectedSocket
            );


            return;
        }


        if (
            context.SelectedGroup !=
            null)
        {
            ui.Inspector.ShowGroup(
                context.SelectedGroup
            );


            return;
        }


        if (
            context.SelectionCount >
            1)
        {
            ui.Inspector.ShowMultipleCells(
                context.SelectionCount
            );


            return;
        }


        if (
            context.SelectedCell
                .HasValue)
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
            context.CurrentRoom.Width,
            context.CurrentRoom.Height
        );
    }


    private void OnUndoRedoPerformed()
    {
        context.CurrentRoom
            ?.EnsureIntegrity();


        RefreshAll();
    }


    // =========================================================
    // Unbind / Dispose
    // =========================================================

    private void UnbindInput()
    {
        if (
            inputHandler ==
            null)
        {
            return;
        }


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