using System;
using UnityEngine.UIElements;

public sealed class RoomEditorUI
    : IDisposable
{
    private readonly Label
        statusMessageLabel;


    public ToolbarUI Toolbar
    {
        get;
    }

    public ToolPaletteUI ToolPalette
    {
        get;
    }

    public LayersPanelUI Layers
    {
        get;
    }

    public GridCanvasUI GridCanvas
    {
        get;
    }

    public InspectorPanelUI Inspector
    {
        get;
    }

    public PreviewPanelUI Preview
    {
        get;
    }


    public RoomEditorUI(
        VisualElement root,
        RoomEditorContext context)
    {
        if (root == null)
        {
            throw new ArgumentNullException(
                nameof(root)
            );
        }

        if (context == null)
        {
            throw new ArgumentNullException(
                nameof(context)
            );
        }


        statusMessageLabel =
            RoomEditorUIQuery.Require<Label>(
                root,
                "status-message-label"
            );


        Toolbar =
            new ToolbarUI(
                root
            );


        ToolPalette =
            new ToolPaletteUI(
                root,
                context
            );


        Layers =
            new LayersPanelUI(
                root,
                context
            );


        GridCanvas =
            new GridCanvasUI(
                root,
                context
            );


        Inspector =
            new InspectorPanelUI(
                root
            );


        Preview =
            new PreviewPanelUI(
                root
            );
    }


    public void SetStatus(
        string message)
    {
        statusMessageLabel.text =
            string.IsNullOrWhiteSpace(
                message
            )
                ? string.Empty
                : message;
    }


    public void Dispose()
    {
        ToolPalette?.Dispose();

        Layers?.Dispose();

        GridCanvas?.Dispose();

        Inspector?.Dispose();

        Preview?.Dispose();
    }
}