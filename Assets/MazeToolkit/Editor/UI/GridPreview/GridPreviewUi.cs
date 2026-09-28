using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class GridPreviewUI
    : VisualElement
{
    private const float MinZoom = 0.25f;
    private const float MaxZoom = 4f;


    private readonly MazeEditorContext context;


    private readonly ScrollView viewport;
    private readonly VisualElement gridCanvas;

    private readonly Label gridInfoLabel;
    private readonly Label zoomLabel;

    private readonly Button fitButton;
    private readonly Button resetZoomButton;


    private float zoom = 1f;

    private bool pointerPressed;


    public event Action<Vector2>
        PointerDown;

    public event Action<Vector2>
        PointerDrag;

    public event Action<Vector2>
        PointerUp;


    public GridPreviewUI(
        MazeEditorContext context)
    {
        this.context = context;


        VisualTreeAsset template =
            AssetDatabase
                .LoadAssetAtPath<VisualTreeAsset>(
                    MazeUIPaths.GridPreview
                );

        template.CloneTree(this);


        viewport =
            this.Q<ScrollView>(
                "grid-scroll"
            );

        gridCanvas =
            this.Q<VisualElement>(
                "grid-canvas"
            );

        gridInfoLabel =
            this.Q<Label>(
                "grid-info-label"
            );

        zoomLabel =
            this.Q<Label>(
                "zoom-label"
            );

        fitButton =
            this.Q<Button>(
                "fit-button"
            );

        resetZoomButton =
            this.Q<Button>(
                "reset-zoom-button"
            );


        viewport.mode =
            ScrollViewMode
                .VerticalAndHorizontal;


        gridCanvas.generateVisualContent +=
            DrawGrid;


        fitButton.clicked +=
            FitToViewport;

        resetZoomButton.clicked +=
            () => SetZoom(1f);


        gridCanvas.RegisterCallback<
            PointerDownEvent
        >(OnPointerDown);

        gridCanvas.RegisterCallback<
            PointerMoveEvent
        >(OnPointerMove);

        gridCanvas.RegisterCallback<
            PointerUpEvent
        >(OnPointerUp);

        gridCanvas.RegisterCallback<
            PointerCancelEvent
        >(OnPointerCancel);


        viewport.RegisterCallback<
            WheelEvent
        >(OnWheel);


        RegisterCallback<
            AttachToPanelEvent
        >(OnAttach);

        RegisterCallback<
            DetachFromPanelEvent
        >(OnDetach);


        Refresh();
    }


    // =========================================================
    // Context
    // =========================================================

    private void OnAttach(
        AttachToPanelEvent evt)
    {
        context.OnGridChanged += RefreshGrid;

        context.OnSettingsChanged += Refresh;
    }


    private void OnDetach(
        DetachFromPanelEvent evt)
    {
        context.OnGridChanged -= RefreshGrid;

        context.OnSettingsChanged -= Refresh;
    }


    private void Refresh()
    {
        gridInfoLabel.text =
            $"{context.Width} × {context.Height}";

        zoomLabel.text =
            $"{Mathf.RoundToInt(zoom * 100f)}%";

        UpdateCanvasSize();

        RefreshGrid();
    }


    private void RefreshGrid()
    {
        gridCanvas.MarkDirtyRepaint();
    }


    // =========================================================
    // Size
    // =========================================================

    private void UpdateCanvasSize()
    {
        float cellSize =
            context.CellSize * zoom;

        gridCanvas.style.width =
            context.Width * cellSize;

        gridCanvas.style.height =
            context.Height * cellSize;
    }


    // =========================================================
    // Zoom
    // =========================================================

    private void SetZoom(
        float value)
    {
        zoom =
            Mathf.Clamp(
                value,
                MinZoom,
                MaxZoom
            );

        Refresh();
    }


    private void FitToViewport()
    {
        float sourceWidth =
            context.Width *
            context.CellSize;

        float sourceHeight =
            context.Height *
            context.CellSize;


        if (
            sourceWidth <= 0f ||
            sourceHeight <= 0f)
        {
            return;
        }


        float availableWidth =
            viewport.contentRect.width - 30f;

        float availableHeight =
            viewport.contentRect.height - 30f;


        if (
            availableWidth <= 0 ||
            availableHeight <= 0)
        {
            return;
        }


        float zoomX =
            availableWidth /
            sourceWidth;

        float zoomY =
            availableHeight /
            sourceHeight;


        SetZoom(
            Mathf.Min(
                zoomX,
                zoomY
            )
        );
    }


    private void OnWheel(
        WheelEvent evt)
    {
        if (!evt.ctrlKey)
            return;


        float multiplier =
            evt.delta.y > 0f
                ? 0.9f
                : 1.1f;


        SetZoom(
            zoom * multiplier
        );


        evt.StopPropagation();
    }


    // =========================================================
    // Pointer
    // =========================================================

    private void OnPointerDown(
        PointerDownEvent evt)
    {
        if (evt.button != 0)
            return;


        pointerPressed = true;


        gridCanvas.CapturePointer(
            evt.pointerId
        );


        PointerDown?.Invoke(
            PointerToGrid(
                evt.position
            )
        );


        evt.StopPropagation();
    }


    private void OnPointerMove(
        PointerMoveEvent evt)
    {
        if (!pointerPressed)
            return;


        PointerDrag?.Invoke(
            PointerToGrid(
                evt.position
            )
        );
    }


    private void OnPointerUp(
        PointerUpEvent evt)
    {
        if (!pointerPressed)
            return;


        pointerPressed = false;


        PointerUp?.Invoke(
            PointerToGrid(
                evt.position
            )
        );


        if (
            gridCanvas.HasPointerCapture(
                evt.pointerId
            ))
        {
            gridCanvas.ReleasePointer(
                evt.pointerId
            );
        }


        evt.StopPropagation();
    }


    private void OnPointerCancel(
        PointerCancelEvent evt)
    {
        pointerPressed = false;
    }


    private Vector2 PointerToGrid(
        Vector3 panelPosition)
    {
        Vector2 local =
            gridCanvas.WorldToLocal(
                new Vector2(
                    panelPosition.x,
                    panelPosition.y
                )
            );

        /*
         * Le canvas affiché est zoomé.
         * L'InputHandler doit cependant recevoir
         * les coordonnées de grille originales.
         */
        return local / zoom;
    }


    // =========================================================
    // Draw
    // =========================================================

    private void DrawGrid(
        MeshGenerationContext mgc)
    {
        Painter2D painter =
            mgc.painter2D;


        float cellSize =
            context.CellSize * zoom;


        DrawCells(
            painter,
            cellSize
        );

        DrawLines(
            painter,
            cellSize
        );
    }


    private void DrawCells(
        Painter2D painter,
        float cellSize)
    {
        for (
            int x = 0;
            x < context.Width;
            x++)
        {
            for (
                int y = 0;
                y < context.Height;
                y++)
            {
                Cell cell =
                    context.Cells[x, y];


                painter.fillColor =
                    GetCellColor(
                        cell.Type
                    );


                float left =
                    x * cellSize;

                float top =
                    y * cellSize;

                float right =
                    left + cellSize;

                float bottom =
                    top + cellSize;


                painter.BeginPath();

                painter.MoveTo(
                    new Vector2(
                        left,
                        top
                    )
                );

                painter.LineTo(
                    new Vector2(
                        right,
                        top
                    )
                );

                painter.LineTo(
                    new Vector2(
                        right,
                        bottom
                    )
                );

                painter.LineTo(
                    new Vector2(
                        left,
                        bottom
                    )
                );

                painter.ClosePath();

                painter.Fill();
            }
        }
    }


    private void DrawLines(
        Painter2D painter,
        float cellSize)
    {
        float width =
            context.Width *
            cellSize;

        float height =
            context.Height *
            cellSize;


        painter.strokeColor =
            EditorGUIUtility.isProSkin
                ? new Color(
                    1f,
                    1f,
                    1f,
                    0.12f
                )
                : new Color(
                    0f,
                    0f,
                    0f,
                    0.18f
                );


        painter.lineWidth = 1f;

        painter.BeginPath();


        for (
            int x = 0;
            x <= context.Width;
            x++)
        {
            float position =
                x * cellSize;

            painter.MoveTo(
                new Vector2(
                    position,
                    0
                )
            );

            painter.LineTo(
                new Vector2(
                    position,
                    height
                )
            );
        }


        for (
            int y = 0;
            y <= context.Height;
            y++)
        {
            float position =
                y * cellSize;

            painter.MoveTo(
                new Vector2(
                    0,
                    position
                )
            );

            painter.LineTo(
                new Vector2(
                    width,
                    position
                )
            );
        }


        painter.Stroke();
    }


    private Color GetCellColor(
        CellType type)
    {
        bool dark =
            EditorGUIUtility.isProSkin;


        return type switch
        {
            CellType.Empty =>
                dark
                    ? new Color(
                        0.15f,
                        0.16f,
                        0.18f
                    )
                    : new Color(
                        0.90f,
                        0.90f,
                        0.91f
                    ),

            CellType.Path =>
                dark
                    ? new Color(
                        0.34f,
                        0.36f,
                        0.40f
                    )
                    : new Color(
                        0.70f,
                        0.72f,
                        0.75f
                    ),

            CellType.Wall =>
                dark
                    ? new Color(
                        0.07f,
                        0.08f,
                        0.10f
                    )
                    : new Color(
                        0.23f,
                        0.24f,
                        0.27f
                    ),

            _ =>
                Color.magenta
        };
    }
}