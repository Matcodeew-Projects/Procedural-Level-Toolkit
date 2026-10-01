using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

public sealed class LayersPanelUI : IDisposable
{
    private readonly RoomEditorContext context;
    private readonly ListView layerList;
    private readonly Label layerCountLabel;
    private readonly Label activeLayerLabel;

    private readonly List<RoomLayerData> layers = new();
    private Func<RoomLayerData, string> displayNameProvider;

    public event Action AddRequested;
    public event Action DuplicateRequested;
    public event Action DeleteRequested;
    public event Action MoveUpRequested;
    public event Action MoveDownRequested;
    public event Action<RoomLayerData> SelectionChanged;

    public LayersPanelUI(
        VisualElement root,
        RoomEditorContext context)
    {
        this.context = context;

        layerList = root.Q<ListView>("layer-list");
        layerCountLabel = root.Q<Label>("layer-count-label");
        activeLayerLabel = root.Q<Label>("active-layer-label");

        layerList.makeItem = () => new Label();
        layerList.bindItem = BindLayerItem;
        layerList.selectionType = SelectionType.Single;
        layerList.selectionChanged += OnSelectionChanged;

        root.Q<Button>("add-layer-button").clicked +=
            () => AddRequested?.Invoke();

        root.Q<Button>("duplicate-layer-button").clicked +=
            () => DuplicateRequested?.Invoke();

        root.Q<Button>("delete-layer-button").clicked +=
            () => DeleteRequested?.Invoke();

        root.Q<Button>("move-layer-up-button").clicked +=
            () => MoveUpRequested?.Invoke();

        root.Q<Button>("move-layer-down-button").clicked +=
            () => MoveDownRequested?.Invoke();

        context.ActiveLayerChanged += RefreshActiveLayerLabel;

        SetLayers(Array.Empty<RoomLayerData>());
    }

    public void SetLayers(
        IEnumerable<RoomLayerData> source,
        Func<RoomLayerData, string> displayName = null)
    {
        layers.Clear();

        if (source != null)
            layers.AddRange(source.Where(layer => layer != null));

        displayNameProvider = displayName;

        layerList.itemsSource = layers;
        layerList.Rebuild();

        layerCountLabel.text = layers.Count.ToString();
        RefreshActiveLayerLabel();
    }

    private void BindLayerItem(VisualElement element, int index)
    {
        Label label = (Label)element;
        RoomLayerData layer = layers[index];

        label.text = displayNameProvider != null
            ? displayNameProvider(layer)
            : $"Layer {index + 1}";
    }

    private void OnSelectionChanged(IEnumerable<object> selection)
    {
        RoomLayerData layer = selection
            .OfType<RoomLayerData>()
            .FirstOrDefault();

        context.SetActiveLayer(layer);
        SelectionChanged?.Invoke(layer);
    }

    private void RefreshActiveLayerLabel()
    {
        if (context.ActiveLayer == null)
        {
            activeLayerLabel.text = "Layer: —";
            return;
        }

        int index = layers.IndexOf(context.ActiveLayer);

        string name = displayNameProvider != null
            ? displayNameProvider(context.ActiveLayer)
            : index >= 0
                ? $"Layer {index + 1}"
                : "Layer";

        activeLayerLabel.text = $"Layer: {name}";
    }

    public void Dispose()
    {
        layerList.selectionChanged -= OnSelectionChanged;
        context.ActiveLayerChanged -= RefreshActiveLayerLabel;
    }
}
