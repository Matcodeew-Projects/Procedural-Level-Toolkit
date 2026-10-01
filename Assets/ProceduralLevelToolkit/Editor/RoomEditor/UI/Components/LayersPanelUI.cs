using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

public sealed class LayersPanelUI
    : IDisposable
{
    private readonly RoomEditorContext
        context;


    private readonly ListView layerList;

    private readonly Label layerCountLabel;

    private readonly Label activeLayerLabel;


    private readonly List<RoomLayerData>
        layers =
            new();


    public event Action AddRequested;

    public event Action DuplicateRequested;

    public event Action DeleteRequested;

    public event Action MoveUpRequested;

    public event Action MoveDownRequested;


    public event Action<RoomLayerData>
        SelectionChanged;


    public event Action<
        RoomLayerData,
        string
    >
        RenameRequested;


    public event Action<
        RoomLayerData,
        bool
    >
        VisibilityChanged;


    public event Action<
        RoomLayerData,
        bool
    >
        LockChanged;


    public LayersPanelUI(
        VisualElement root,
        RoomEditorContext context)
    {
        this.context =
            context;


        layerList =
            root.Q<ListView>(
                "layer-list"
            )
            ??
            throw new InvalidOperationException(
                "[RoomEditor] Missing 'layer-list'."
            );


        layerCountLabel =
            root.Q<Label>(
                "layer-count-label"
            );


        activeLayerLabel =
            root.Q<Label>(
                "active-layer-label"
            );


        layerList.makeItem =
            CreateLayerRow;


        layerList.bindItem =
            BindLayerRow;


        layerList.selectionType =
            SelectionType.Single;


        layerList.selectionChanged +=
            OnSelectionChanged;


        root.Q<Button>(
            "add-layer-button"
        ).clicked +=
            () =>
                AddRequested?.Invoke();


        root.Q<Button>(
            "duplicate-layer-button"
        ).clicked +=
            () =>
                DuplicateRequested?.Invoke();


        root.Q<Button>(
            "delete-layer-button"
        ).clicked +=
            () =>
                DeleteRequested?.Invoke();


        root.Q<Button>(
            "move-layer-up-button"
        ).clicked +=
            () =>
                MoveUpRequested?.Invoke();


        root.Q<Button>(
            "move-layer-down-button"
        ).clicked +=
            () =>
                MoveDownRequested?.Invoke();


        context.ActiveLayerChanged +=
            RefreshActiveLayerLabel;


        SetLayers(
            Array.Empty<RoomLayerData>()
        );
    }


    // =========================================================
    // Row
    // =========================================================

    private VisualElement CreateLayerRow()
    {
        VisualElement row =
            new();


        row.style.flexDirection =
            FlexDirection.Row;


        row.style.alignItems =
            Align.Center;


        row.style.height =
            24f;


        Toggle visible =
            new()
            {
                name =
                    "layer-row-visible",

                tooltip =
                    "Visible"
            };


        visible.style.width =
            28f;


        Toggle locked =
            new()
            {
                name =
                    "layer-row-locked",

                tooltip =
                    "Locked"
            };


        locked.style.width =
            28f;


        TextField nameField =
            new()
            {
                name =
                    "layer-row-name",

                isDelayed =
                    true
            };


        nameField.style.flexGrow =
            1f;


        row.Add(
            visible
        );


        row.Add(
            locked
        );


        row.Add(
            nameField
        );


        row.RegisterCallback<
            PointerDownEvent
        >(
            OnLayerRowPointerDown,
            TrickleDown.TrickleDown
        );


        visible
            .RegisterValueChangedCallback(
                OnVisibilityChanged
            );


        locked
            .RegisterValueChangedCallback(
                OnLockChanged
            );


        nameField
            .RegisterValueChangedCallback(
                OnNameChanged
            );


        return row;
    }


    private void BindLayerRow(
        VisualElement row,
        int index)
    {
        RoomLayerData layer =
            layers[index];


        row.userData =
            layer;


        Toggle visible =
            row.Q<Toggle>(
                "layer-row-visible"
            );


        Toggle locked =
            row.Q<Toggle>(
                "layer-row-locked"
            );


        TextField nameField =
            row.Q<TextField>(
                "layer-row-name"
            );


        visible.userData =
            layer;


        locked.userData =
            layer;


        nameField.userData =
            layer;


        visible
            .SetValueWithoutNotify(
                layer.Visible
            );


        locked
            .SetValueWithoutNotify(
                layer.Locked
            );


        nameField
            .SetValueWithoutNotify(
                layer.DisplayName
            );
    }


    private void OnLayerRowPointerDown(
        PointerDownEvent evt)
    {
        VisualElement row =
            evt.currentTarget
            as VisualElement;


        RoomLayerData layer =
            row?.userData
            as RoomLayerData;


        if (layer == null)
            return;


        int index =
            layers.IndexOf(
                layer
            );


        if (index >= 0)
        {
            layerList.SetSelection(
                index
            );
        }
    }


    private void OnNameChanged(
        ChangeEvent<string> evt)
    {
        TextField field =
            evt.target
            as TextField;


        RoomLayerData layer =
            field?.userData
            as RoomLayerData;


        if (layer == null)
            return;


        RenameRequested?.Invoke(
            layer,
            evt.newValue
        );
    }


    private void OnVisibilityChanged(
        ChangeEvent<bool> evt)
    {
        Toggle toggle =
            evt.target
            as Toggle;


        RoomLayerData layer =
            toggle?.userData
            as RoomLayerData;


        if (layer == null)
            return;


        VisibilityChanged?.Invoke(
            layer,
            evt.newValue
        );
    }


    private void OnLockChanged(
        ChangeEvent<bool> evt)
    {
        Toggle toggle =
            evt.target
            as Toggle;


        RoomLayerData layer =
            toggle?.userData
            as RoomLayerData;


        if (layer == null)
            return;


        LockChanged?.Invoke(
            layer,
            evt.newValue
        );
    }


    // =========================================================
    // Data
    // =========================================================

    public void SetLayers(
        IEnumerable<RoomLayerData> source,
        Func<
            RoomLayerData,
            string
        > displayName = null)
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


        if (layerCountLabel != null)
        {
            layerCountLabel.text =
                layers.Count
                    .ToString();
        }


        SelectLayer(
            context.ActiveLayer
        );


        RefreshActiveLayerLabel();
    }


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


        context.SetActiveLayer(
            layer
        );


        SelectionChanged?.Invoke(
            layer
        );
    }


    private void RefreshActiveLayerLabel()
    {
        if (
            activeLayerLabel ==
            null)
        {
            return;
        }


        activeLayerLabel.text =
            context.ActiveLayer !=
            null
                ? $"Layer: {context.ActiveLayer.DisplayName}"
                : "Layer: —";
    }


    public void Dispose()
    {
        layerList.selectionChanged -=
            OnSelectionChanged;


        context.ActiveLayerChanged -=
            RefreshActiveLayerLabel;
    }
}