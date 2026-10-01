using System;
using UnityEngine.UIElements;

public sealed class RoomEditorUI : IDisposable
{
    private readonly Label statusMessageLabel;

    public ToolbarUI Toolbar { get; }
    public ToolPaletteUI ToolPalette { get; }
    public LayersPanelUI Layers { get; }
    public GridCanvasUI GridCanvas { get; }
    public InspectorPanelUI Inspector { get; }
    public PreviewPanelUI Preview { get; }

    public RoomEditorUI(
        VisualElement root,
        RoomEditorContext context)
    {
        statusMessageLabel = root.Q<Label>("status-message-label");

        Toolbar = new ToolbarUI(root);
        ToolPalette = new ToolPaletteUI(root, context);
        Layers = new LayersPanelUI(root, context);
        GridCanvas = new GridCanvasUI(root, context);
        Inspector = new InspectorPanelUI(root);
        Preview = new PreviewPanelUI(root);
    }

    public void SetStatus(string message)
    {
        if (statusMessageLabel != null)
            statusMessageLabel.text = message;
    }

    public void Dispose()
    {
        ToolPalette.Dispose();
        Layers.Dispose();
        GridCanvas.Dispose();
        Inspector.Dispose();
        Preview.Dispose();
    }
}
