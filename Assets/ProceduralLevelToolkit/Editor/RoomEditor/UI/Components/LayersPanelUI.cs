using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class LayersPanelUI
    : IDisposable
{
    private readonly RoomEditorContext context;

    private readonly ListView layerList;
    private readonly Label layerCountLabel;
    private readonly Label activeLayerLabel;

    private readonly List<RoomLayerData> layers =
        new();


    // =========================================================
    // Events
    // =========================================================

    public event Action AddRequested;
    public event Action DuplicateRequested;
    public event Action DeleteRequested;
    public event Action MoveUpRequested;
    public event Action MoveDownRequested;

    public event Action<RoomLayerData>
        SelectionChanged;

    public event Action<RoomLayerData, string>
        RenameRequested;

    public event Action<RoomLayerData, bool>
        VisibilityChanged;

    public event Action<RoomLayerData, bool>
        LockChanged;


    // =========================================================
    // Constructor
    // =========================================================

    public LayersPanelUI(
        VisualElement root,
        RoomEditorContext context)
    {
        this.context =
            context
            ?? throw new ArgumentNullException(
                nameof(context)
            );


        layerList =
            Require<ListView>(
                root,
                "layer-list"
            );

        layerCountLabel =
            Require<Label>(
                root,
                "layer-count-label"
            );

        activeLayerLabel =
            Require<Label>(
                root,
                "active-layer-label"
            );


        // =====================================================
        // List
        // =====================================================

        layerList.selectionType =
            SelectionType.Single;

        layerList.fixedItemHeight =
            32f;

        layerList.makeItem =
            CreateLayerRow;

        layerList.bindItem =
            BindLayerRow;

        layerList.selectionChanged +=
            OnSelectionChanged;


        // =====================================================
        // Buttons
        // =====================================================

        Require<Button>(
            root,
            "add-layer-button"
        ).clicked +=
            OnAddClicked;


        Require<Button>(
            root,
            "duplicate-layer-button"
        ).clicked +=
            OnDuplicateClicked;


        Require<Button>(
            root,
            "delete-layer-button"
        ).clicked +=
            OnDeleteClicked;


        Require<Button>(
            root,
            "move-layer-up-button"
        ).clicked +=
            OnMoveUpClicked;


        Require<Button>(
            root,
            "move-layer-down-button"
        ).clicked +=
            OnMoveDownClicked;


        // =====================================================
        // Context
        // =====================================================

        context.ActiveLayerChanged +=
            OnActiveLayerChanged;


        SetLayers(
            Array.Empty<RoomLayerData>()
        );
    }


    // =========================================================
    // Create Row
    // =========================================================

    private VisualElement CreateLayerRow()
    {
        VisualElement row =
            new VisualElement();

        row.name =
            "layer-row";

        row.AddToClassList(
            "layer-row"
        );


        // =====================================================
        // Active accent
        // =====================================================

        VisualElement accent =
            new VisualElement();

        accent.name =
            "layer-row-accent";

        accent.AddToClassList(
            "layer-row__accent"
        );


        // =====================================================
        // Visibility
        // =====================================================

        Button visibleButton =
            new Button();

        visibleButton.name =
            "layer-row-visible";

        visibleButton.AddToClassList(
            "layer-row__state-button"
        );

        visibleButton.tooltip =
            "Toggle layer visibility";


        // =====================================================
        // Lock
        // =====================================================

        Button lockButton =
            new Button();

        lockButton.name =
            "layer-row-locked";

        lockButton.AddToClassList(
            "layer-row__state-button"
        );

        lockButton.tooltip =
            "Toggle layer lock";


        // =====================================================
        // Layer name
        // =====================================================

        Button nameButton =
            new Button();

        nameButton.name =
            "layer-row-select";

        nameButton.AddToClassList(
            "layer-row__name-button"
        );

        nameButton.tooltip =
            "Click to select. Double-click to rename.";


        // =====================================================
        // Rename field
        // =====================================================

        TextField renameField =
            new TextField();

        renameField.name =
            "layer-row-rename";

        renameField.isDelayed =
            false;

        renameField.AddToClassList(
            "layer-row__rename-field"
        );

        renameField.AddToClassList(
            "hidden"
        );


        // =====================================================
        // Hierarchy
        // =====================================================

        row.Add(
            accent
        );

        row.Add(
            visibleButton
        );

        row.Add(
            lockButton
        );

        row.Add(
            nameButton
        );

        row.Add(
            renameField
        );


        // =====================================================
        // Name interaction
        // =====================================================

        nameButton.RegisterCallback<
            PointerDownEvent
        >(
            evt =>
                OnNamePointerDown(
                    evt,
                    nameButton,
                    renameField
                ),
            TrickleDown.TrickleDown
        );


        // =====================================================
        // Rename interaction
        // =====================================================

        renameField.RegisterCallback<
            KeyDownEvent
        >(
            evt =>
                OnRenameKeyDown(
                    evt,
                    renameField,
                    nameButton
                )
        );


        renameField.RegisterCallback<
            FocusOutEvent
        >(
            evt =>
                CommitRename(
                    renameField,
                    nameButton
                )
        );


        // =====================================================
        // Visibility
        // =====================================================

        visibleButton.clicked +=
            () =>
                ToggleVisibility(
                    visibleButton
                );


        // =====================================================
        // Lock
        // =====================================================

        lockButton.clicked +=
            () =>
                ToggleLock(
                    lockButton
                );


        return row;
    }


    // =========================================================
    // Bind Row
    // =========================================================

    private void BindLayerRow(
        VisualElement row,
        int index)
    {
        if (
            index < 0 ||
            index >= layers.Count)
        {
            return;
        }


        RoomLayerData layer =
            layers[index];


        row.userData =
            layer;


        VisualElement accent =
            row.Q<VisualElement>(
                "layer-row-accent"
            );


        Button visibleButton =
            row.Q<Button>(
                "layer-row-visible"
            );


        Button lockButton =
            row.Q<Button>(
                "layer-row-locked"
            );


        Button nameButton =
            row.Q<Button>(
                "layer-row-select"
            );


        TextField renameField =
            row.Q<TextField>(
                "layer-row-rename"
            );


        accent.userData =
            layer;

        visibleButton.userData =
            layer;

        lockButton.userData =
            layer;

        nameButton.userData =
            layer;

        renameField.userData =
            layer;


        bool active =
            ReferenceEquals(
                layer,
                context.ActiveLayer
            );


        // =====================================================
        // Active state
        // =====================================================

        row.EnableInClassList(
            "layer-row--active",
            active
        );


        accent.EnableInClassList(
            "layer-row__accent--active",
            active
        );


        nameButton.EnableInClassList(
            "layer-row__name-button--active",
            active
        );


        // =====================================================
        // Name
        // =====================================================

        nameButton.text =
            layer.DisplayName;


        renameField
            .SetValueWithoutNotify(
                layer.DisplayName
            );


        renameField.AddToClassList(
            "hidden"
        );


        nameButton.RemoveFromClassList(
            "hidden"
        );


        // =====================================================
        // Visibility state
        // =====================================================

        visibleButton.text =
            layer.Visible
                ? "V"
                : "—";


        visibleButton.tooltip =
            layer.Visible
                ? "Layer visible"
                : "Layer hidden";


        visibleButton.EnableInClassList(
            "layer-row__state-button--on",
            layer.Visible
        );


        visibleButton.EnableInClassList(
            "layer-row__state-button--off",
            !layer.Visible
        );


        // =====================================================
        // Lock state
        // =====================================================

        lockButton.text =
            layer.Locked
                ? "L"
                : "—";


        lockButton.tooltip =
            layer.Locked
                ? "Layer locked"
                : "Layer unlocked";


        lockButton.EnableInClassList(
            "layer-row__state-button--on",
            layer.Locked
        );


        lockButton.EnableInClassList(
            "layer-row__state-button--off",
            !layer.Locked
        );
    }


    // =========================================================
    // Name Click
    // =========================================================

    private void OnNamePointerDown(
        PointerDownEvent evt,
        Button nameButton,
        TextField renameField)
    {
        if (
            evt.button !=
            0)
        {
            return;
        }


        RoomLayerData layer =
            nameButton.userData
            as RoomLayerData;


        if (layer == null)
            return;


        // =====================================================
        // Always select the layer
        // =====================================================

        SelectLayerInternal(
            layer
        );


        // =====================================================
        // Double-click → Rename
        // =====================================================

        if (
            evt.clickCount >=
            2)
        {
            /*
             * On attend la fin du traitement de l'événement.
             *
             * Cela évite que le refresh lié à la sélection
             * ferme immédiatement le TextField.
             */
            nameButton.schedule.Execute(
                () =>
                {
                    BeginRename(
                        nameButton,
                        renameField
                    );
                }
            );
        }


        evt.StopImmediatePropagation();
    }


    // =========================================================
    // Selection
    // =========================================================

    private void SelectLayerInternal(
        RoomLayerData layer)
    {
        int index =
            layers.IndexOf(
                layer
            );


        if (index < 0)
            return;


        if (
            ReferenceEquals(
                context.ActiveLayer,
                layer
            ))
        {
            return;
        }


        layerList.SetSelection(
            index
        );
    }


    private void OnSelectionChanged(
        IEnumerable<object> selection)
    {
        RoomLayerData layer =
            selection
                .OfType<RoomLayerData>()
                .FirstOrDefault();


        if (layer == null)
            return;


        context.SetActiveLayer(
            layer
        );


        SelectionChanged?.Invoke(
            layer
        );
    }


    // =========================================================
    // Begin Rename
    // =========================================================

    private static void BeginRename(
        Button nameButton,
        TextField renameField)
    {
        RoomLayerData layer =
            nameButton.userData
            as RoomLayerData;


        if (layer == null)
            return;


        renameField
            .SetValueWithoutNotify(
                layer.DisplayName
            );


        nameButton.AddToClassList(
            "hidden"
        );


        renameField.RemoveFromClassList(
            "hidden"
        );


        renameField.Focus();
    }


    // =========================================================
    // Rename Keyboard
    // =========================================================

    private void OnRenameKeyDown(
        KeyDownEvent evt,
        TextField renameField,
        Button nameButton)
    {
        if (
            evt.keyCode ==
            KeyCode.Return ||
            evt.keyCode ==
            KeyCode.KeypadEnter)
        {
            CommitRename(
                renameField,
                nameButton
            );


            evt.StopImmediatePropagation();

            return;
        }


        if (
            evt.keyCode ==
            KeyCode.Escape)
        {
            CancelRename(
                renameField,
                nameButton
            );


            evt.StopImmediatePropagation();
        }
    }


    // =========================================================
    // Commit Rename
    // =========================================================

    private void CommitRename(
        TextField renameField,
        Button nameButton)
    {
        if (
            renameField.ClassListContains(
                "hidden"
            ))
        {
            return;
        }


        RoomLayerData layer =
            renameField.userData
            as RoomLayerData;


        if (layer == null)
        {
            EndRename(
                renameField,
                nameButton
            );


            return;
        }


        string newName =
            renameField.value
                ?.Trim();


        if (
            string.IsNullOrWhiteSpace(
                newName
            ))
        {
            renameField
                .SetValueWithoutNotify(
                    layer.DisplayName
                );
        }
        else if (
            newName !=
            layer.DisplayName)
        {
            RenameRequested?.Invoke(
                layer,
                newName
            );
        }


        EndRename(
            renameField,
            nameButton
        );
    }


    // =========================================================
    // Cancel Rename
    // =========================================================

    private static void CancelRename(
        TextField renameField,
        Button nameButton)
    {
        RoomLayerData layer =
            renameField.userData
            as RoomLayerData;


        if (layer != null)
        {
            renameField
                .SetValueWithoutNotify(
                    layer.DisplayName
                );
        }


        EndRename(
            renameField,
            nameButton
        );
    }


    // =========================================================
    // End Rename
    // =========================================================

    private static void EndRename(
        TextField renameField,
        Button nameButton)
    {
        renameField.AddToClassList(
            "hidden"
        );


        nameButton.RemoveFromClassList(
            "hidden"
        );
    }


    // =========================================================
    // Visibility
    // =========================================================

    private void ToggleVisibility(
        Button button)
    {
        RoomLayerData layer =
            button.userData
            as RoomLayerData;


        if (layer == null)
            return;


        VisibilityChanged?.Invoke(
            layer,
            !layer.Visible
        );
    }


    // =========================================================
    // Lock
    // =========================================================

    private void ToggleLock(
        Button button)
    {
        RoomLayerData layer =
            button.userData
            as RoomLayerData;


        if (layer == null)
            return;


        LockChanged?.Invoke(
            layer,
            !layer.Locked
        );
    }


    // =========================================================
    // Set Layers
    // =========================================================

    public void SetLayers(
        IEnumerable<RoomLayerData> source)
    {
        layers.Clear();


        if (source != null)
        {
            layers.AddRange(
                source.Where(
                    layer =>
                        layer != null
                )
            );
        }


        layerList.itemsSource =
            layers;


        layerList.Rebuild();


        layerCountLabel.text =
            layers.Count
                .ToString();


        SelectLayer(
            context.ActiveLayer
        );


        RefreshActiveLayerLabel();
    }


    // =========================================================
    // Select Layer
    // =========================================================

    public void SelectLayer(
        RoomLayerData layer)
    {
        int index =
            layers.IndexOf(
                layer
            );


        if (index < 0)
        {
            layerList.ClearSelection();

            return;
        }


        /*
         * Important :
         * on évite SetSelection si le layer
         * est déjà actif.
         *
         * Ça évite des refresh inutiles
         * et facilite le double-clic.
         */
        if (
            ReferenceEquals(
                context.ActiveLayer,
                layer
            ))
        {
            layerList.RefreshItems();

            return;
        }


        layerList.SetSelection(
            index
        );


        layerList.RefreshItems();
    }


    // =========================================================
    // Context Active Layer
    // =========================================================

    private void OnActiveLayerChanged()
    {
        RefreshActiveLayerLabel();


        layerList.RefreshItems();
    }


    // =========================================================
    // Active Layer Label
    // =========================================================

    private void RefreshActiveLayerLabel()
    {
        RoomLayerData layer =
            context.ActiveLayer;


        if (layer == null)
        {
            activeLayerLabel.text =
                "Layer: —";

            return;
        }


        string suffix =
            string.Empty;


        if (!layer.Visible)
        {
            suffix +=
                " • Hidden";
        }


        if (layer.Locked)
        {
            suffix +=
                " • Locked";
        }


        activeLayerLabel.text =
            $"Layer: {layer.DisplayName}{suffix}";
    }


    // =========================================================
    // Toolbar Buttons
    // =========================================================

    private void OnAddClicked()
    {
        AddRequested?.Invoke();
    }


    private void OnDuplicateClicked()
    {
        DuplicateRequested?.Invoke();
    }


    private void OnDeleteClicked()
    {
        DeleteRequested?.Invoke();
    }


    private void OnMoveUpClicked()
    {
        MoveUpRequested?.Invoke();
    }


    private void OnMoveDownClicked()
    {
        MoveDownRequested?.Invoke();
    }


    // =========================================================
    // Query
    // =========================================================

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
            $"[RoomEditor] Missing UI element '{name}' " +
            $"of type {typeof(T).Name}."
        );
    }


    // =========================================================
    // Dispose
    // =========================================================

    public void Dispose()
    {
        layerList.selectionChanged -=
            OnSelectionChanged;


        context.ActiveLayerChanged -=
            OnActiveLayerChanged;
    }
}