using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class GridCanvasUI : IDisposable
{
    private readonly RoomEditorContext context;

    private readonly VisualElement gridCanvas;
    private readonly VisualElement gridEmptyState;
    private readonly Label gridSizeLabel;
    private readonly Label zoomLabel;
    private readonly Label cursorCellLabel;

    private bool pointerPressed;

    public event Action<Vector2Int> PointerDown;
    public event Action<Vector2Int> PointerDrag;
    public event Action<Vector2Int> PointerUp;

    public Func<Vector2Int, Color?> CellColorProvider { get; set; }

    public GridCanvasUI(
        VisualElement root,
        RoomEditorContext context)
    {
        this.context = context;

        gridCanvas = root.Q<VisualElement>("room-grid-canvas");
        gridEmptyState = root.Q<VisualElement>("grid-empty-state");
        gridSizeLabel = root.Q<Label>("grid-size-label");
        zoomLabel = root.Q<Label>("grid-zoom-label");
        cursorCellLabel = root.Q<Label>("cursor-cell-label");

        gridCanvas.generateVisualContent += DrawGrid;

        gridCanvas.RegisterCallback<PointerDownEvent>(OnPointerDown);
        gridCanvas.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        gridCanvas.RegisterCallback<PointerUpEvent>(OnPointerUp);
        gridCanvas.RegisterCallback<PointerCancelEvent>(OnPointerCancel);
        gridCanvas.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);

        root.Q<Button>("grid-fit-button").clicked += FitToViewport;
        root.Q<Button>("grid-zoom-out-button").clicked +=
            () => context.SetZoom(context.Zoom / 1.1f);
        root.Q<Button>("grid-zoom-in-button").clicked +=
            () => context.SetZoom(context.Zoom * 1.1f);

        gridCanvas.RegisterCallback<WheelEvent>(OnWheel);

        context.GridMetricsChanged += Refresh;
        context.ZoomChanged += Refresh;
        context.RoomChanged += Refresh;

        Refresh();
    }

    public void Refresh()
    {
        gridSizeLabel.text = $"{context.Width} × {context.Height}";
        zoomLabel.text = $"{Mathf.RoundToInt(context.Zoom * 100f)}%";

        if (gridEmptyState != null)
        {
            gridEmptyState.EnableInClassList(
                "hidden",
                context.CurrentRoom != null
            );
        }

        gridCanvas.MarkDirtyRepaint();
    }

    private void FitToViewport()
    {
        float sourceWidth = context.Width * context.CellSize;
        float sourceHeight = context.Height * context.CellSize;

        if (sourceWidth <= 0f || sourceHeight <= 0f)
            return;

        float availableWidth = Mathf.Max(1f, gridCanvas.contentRect.width - 24f);
        float availableHeight = Mathf.Max(1f, gridCanvas.contentRect.height - 24f);

        float zoomX = availableWidth / sourceWidth;
        float zoomY = availableHeight / sourceHeight;

        context.SetZoom(Mathf.Min(zoomX, zoomY));
    }

    private void OnWheel(WheelEvent evt)
    {
        if (!evt.ctrlKey)
            return;

        float multiplier = evt.delta.y > 0f ? 0.9f : 1.1f;
        context.SetZoom(context.Zoom * multiplier);
        evt.StopPropagation();
    }

    private void OnPointerDown(PointerDownEvent evt)
    {
        if (evt.button != 0)
            return;

        if (!TryPointerToCell(evt.position, out Vector2Int cell))
            return;

        pointerPressed = true;
        gridCanvas.CapturePointer(evt.pointerId);

        PointerDown?.Invoke(cell);
        evt.StopPropagation();
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (TryPointerToCell(evt.position, out Vector2Int cell))
        {
            cursorCellLabel.text = $"Cell: {cell.x}, {cell.y}";

            if (pointerPressed)
                PointerDrag?.Invoke(cell);
        }
        else
        {
            cursorCellLabel.text = "Cell: —, —";
        }
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        if (!pointerPressed)
            return;

        pointerPressed = false;

        if (TryPointerToCell(evt.position, out Vector2Int cell))
            PointerUp?.Invoke(cell);

        if (gridCanvas.HasPointerCapture(evt.pointerId))
            gridCanvas.ReleasePointer(evt.pointerId);

        evt.StopPropagation();
    }

    private void OnPointerCancel(PointerCancelEvent evt)
    {
        pointerPressed = false;
    }

    private void OnPointerLeave(PointerLeaveEvent evt)
    {
        if (!pointerPressed)
            cursorCellLabel.text = "Cell: —, —";
    }

    private bool TryPointerToCell(
        Vector3 panelPosition,
        out Vector2Int cell)
    {
        Vector2 local = gridCanvas.WorldToLocal(
            new Vector2(panelPosition.x, panelPosition.y)
        );

        float displayedCellSize = context.CellSize * context.Zoom;

        cell = new Vector2Int(
            Mathf.FloorToInt(local.x / displayedCellSize),
            Mathf.FloorToInt(local.y / displayedCellSize)
        );

        return context.IsInside(cell);
    }

    private void DrawGrid(MeshGenerationContext mgc)
    {
        Painter2D painter = mgc.painter2D;
        float cellSize = context.CellSize * context.Zoom;

        DrawCellBackgrounds(painter, cellSize);
        DrawGridLines(painter, cellSize);
    }

    private void DrawCellBackgrounds(
        Painter2D painter,
        float cellSize)
    {
        if (CellColorProvider == null)
            return;

        for (int x = 0; x < context.Width; x++)
        {
            for (int y = 0; y < context.Height; y++)
            {
                Vector2Int cell = new(x, y);
                Color? color = CellColorProvider(cell);

                if (!color.HasValue)
                    continue;

                painter.fillColor = color.Value;

                float left = x * cellSize;
                float top = y * cellSize;
                float right = left + cellSize;
                float bottom = top + cellSize;

                painter.BeginPath();
                painter.MoveTo(new Vector2(left, top));
                painter.LineTo(new Vector2(right, top));
                painter.LineTo(new Vector2(right, bottom));
                painter.LineTo(new Vector2(left, bottom));
                painter.ClosePath();
                painter.Fill();
            }
        }
    }

    private void DrawGridLines(
        Painter2D painter,
        float cellSize)
    {
        float width = context.Width * cellSize;
        float height = context.Height * cellSize;

        painter.strokeColor = EditorGUIUtility.isProSkin
            ? new Color(1f, 1f, 1f, 0.12f)
            : new Color(0f, 0f, 0f, 0.18f);

        painter.lineWidth = 1f;
        painter.BeginPath();

        for (int x = 0; x <= context.Width; x++)
        {
            float position = x * cellSize;
            painter.MoveTo(new Vector2(position, 0f));
            painter.LineTo(new Vector2(position, height));
        }

        for (int y = 0; y <= context.Height; y++)
        {
            float position = y * cellSize;
            painter.MoveTo(new Vector2(0f, position));
            painter.LineTo(new Vector2(width, position));
        }

        painter.Stroke();
    }

    public void Dispose()
    {
        context.GridMetricsChanged -= Refresh;
        context.ZoomChanged -= Refresh;
        context.RoomChanged -= Refresh;
    }
}
