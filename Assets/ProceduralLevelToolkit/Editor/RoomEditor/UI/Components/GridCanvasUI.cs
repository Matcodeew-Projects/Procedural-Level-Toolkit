using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class GridCanvasUI
    : IDisposable
{
    private readonly RoomEditorContext
        context;


    private readonly VisualElement
        gridViewport;


    private readonly VisualElement
        gridCanvas;


    private readonly VisualElement
        gridEmptyState;


    private readonly Label
        gridSizeLabel;


    private readonly Label
        zoomLabel;


    private readonly Label
        cursorCellLabel;


    private readonly Button
        fitButton;


    private readonly Button
        zoomOutButton;


    private readonly Button
        zoomInButton;


    private bool pointerPressed;


    private int capturedPointerId =
        -1;


    private Vector2Int?
        hoveredCell;


    public event Action<Vector2Int>
        PointerDown;


    public event Action<Vector2Int>
        PointerDrag;


    public event Action<Vector2Int>
        PointerUp;


    public Func<Vector2Int, Color?>
        CellColorProvider
    {
        get;
        set;
    }


    public Func<Vector2Int, RoomSocketData>
        SocketProvider
    {
        get;
        set;
    }


    public GridCanvasUI(
        VisualElement root,
        RoomEditorContext context)
    {
        this.context =
            context;


        gridViewport =
            RequireAny<VisualElement>(
                root,
                "grid-viewport",
                "grid-scroll"
            );


        gridCanvas =
            RequireAny<VisualElement>(
                root,
                "room-grid-canvas",
                "grid-canvas"
            );


        gridEmptyState =
            FindAny<VisualElement>(
                root,
                "grid-empty-state"
            );


        gridSizeLabel =
            RequireAny<Label>(
                root,
                "grid-size-label",
                "grid-info-label"
            );


        zoomLabel =
            RequireAny<Label>(
                root,
                "zoom-label",
                "grid-zoom-label"
            );


        cursorCellLabel =
            FindAny<Label>(
                root,
                "cursor-cell-label"
            );


        fitButton =
            FindAny<Button>(
                root,
                "fit-grid-button",
                "grid-fit-button",
                "fit-button"
            );


        zoomOutButton =
            FindAny<Button>(
                root,
                "zoom-out-button",
                "grid-zoom-out-button"
            );


        zoomInButton =
            FindAny<Button>(
                root,
                "zoom-in-button",
                "grid-zoom-in-button"
            );


        gridCanvas.generateVisualContent +=
            DrawGrid;


        gridCanvas.RegisterCallback<
            PointerDownEvent
        >(
            OnPointerDown
        );


        gridCanvas.RegisterCallback<
            PointerMoveEvent
        >(
            OnPointerMove
        );


        gridCanvas.RegisterCallback<
            PointerUpEvent
        >(
            OnPointerUp
        );


        gridCanvas.RegisterCallback<
            PointerCancelEvent
        >(
            OnPointerCancel
        );


        gridCanvas.RegisterCallback<
            PointerLeaveEvent
        >(
            OnPointerLeave
        );


        gridViewport.RegisterCallback<
            WheelEvent
        >(
            OnWheel
        );


        gridViewport.RegisterCallback<
            GeometryChangedEvent
        >(
            OnViewportGeometryChanged
        );


        if (fitButton != null)
        {
            fitButton.clicked +=
                FitToViewport;
        }


        if (zoomOutButton != null)
        {
            zoomOutButton.clicked +=
                ZoomOut;
        }


        if (zoomInButton != null)
        {
            zoomInButton.clicked +=
                ZoomIn;
        }


        context.GridMetricsChanged +=
            Refresh;


        context.ZoomChanged +=
            Refresh;


        context.RoomChanged +=
            Refresh;


        context.ActiveLayerChanged +=
            Refresh;


        context.SelectionChanged +=
            RefreshGridOnly;


        Refresh();
    }


    // =========================================================
    // Refresh
    // =========================================================

    public void Refresh()
    {
        gridSizeLabel.text =
            context.CurrentRoom !=
            null
                ? $"{context.Width} × {context.Height}"
                : "— × —";


        zoomLabel.text =
            $"{Mathf.RoundToInt(context.Zoom * 100f)}%";


        if (
            gridEmptyState !=
            null)
        {
            gridEmptyState.EnableInClassList(
                "hidden",
                context.CurrentRoom !=
                null
            );
        }


        UpdateCanvasBounds();


        gridCanvas.MarkDirtyRepaint();
    }


    private void RefreshGridOnly()
    {
        gridCanvas.MarkDirtyRepaint();
    }


    // =========================================================
    // Layout
    // =========================================================

    private void UpdateCanvasBounds()
    {
        float width =
            context.Width *
            context.CellSize *
            context.Zoom;


        float height =
            context.Height *
            context.CellSize *
            context.Zoom;


        gridCanvas.style.width =
            Mathf.Max(
                1f,
                width
            );


        gridCanvas.style.height =
            Mathf.Max(
                1f,
                height
            );


        float viewportWidth =
            gridViewport
                .contentRect
                .width;


        float viewportHeight =
            gridViewport
                .contentRect
                .height;


        gridCanvas.style.marginLeft =
            Mathf.Max(
                0f,
                (
                    viewportWidth -
                    width
                ) *
                0.5f
            );


        gridCanvas.style.marginTop =
            Mathf.Max(
                0f,
                (
                    viewportHeight -
                    height
                ) *
                0.5f
            );
    }


    private void OnViewportGeometryChanged(
        GeometryChangedEvent evt)
    {
        UpdateCanvasBounds();
    }


    // =========================================================
    // Zoom
    // =========================================================

    private void ZoomIn()
    {
        context.SetZoom(
            context.Zoom *
            1.15f
        );
    }


    private void ZoomOut()
    {
        context.SetZoom(
            context.Zoom /
            1.15f
        );
    }


    private void OnWheel(
        WheelEvent evt)
    {
        if (!evt.ctrlKey)
            return;


        if (
            evt.delta.y >
            0f)
        {
            ZoomOut();
        }
        else
        {
            ZoomIn();
        }


        evt.StopPropagation();
    }


    private void FitToViewport()
    {
        if (
            context.CurrentRoom ==
            null)
        {
            return;
        }


        float sourceWidth =
            context.Width *
            context.CellSize;


        float sourceHeight =
            context.Height *
            context.CellSize;


        float availableWidth =
            Mathf.Max(
                1f,
                gridViewport
                    .contentRect
                    .width -
                40f
            );


        float availableHeight =
            Mathf.Max(
                1f,
                gridViewport
                    .contentRect
                    .height -
                40f
            );


        context.SetZoom(
            Mathf.Min(
                availableWidth /
                sourceWidth,

                availableHeight /
                sourceHeight
            )
        );
    }


    // =========================================================
    // Pointer
    // =========================================================

    private void OnPointerDown(
        PointerDownEvent evt)
    {
        if (
            evt.button !=
            0)
        {
            return;
        }


        if (
            !TryPointerToCell(
                evt.position,
                out Vector2Int cell
            ))
        {
            return;
        }


        pointerPressed =
            true;


        capturedPointerId =
            evt.pointerId;


        gridCanvas.Focus();


        gridCanvas.CapturePointer(
            evt.pointerId
        );


        hoveredCell =
            cell;


        UpdateCursorLabel(
            cell
        );


        PointerDown?.Invoke(
            cell
        );


        gridCanvas.MarkDirtyRepaint();


        evt.StopPropagation();
    }


    private void OnPointerMove(
        PointerMoveEvent evt)
    {
        if (
            TryPointerToCell(
                evt.position,
                out Vector2Int cell
            ))
        {
            bool changed =
                !hoveredCell.HasValue ||
                hoveredCell.Value !=
                cell;


            hoveredCell =
                cell;


            UpdateCursorLabel(
                cell
            );


            if (
                pointerPressed &&
                changed)
            {
                PointerDrag?.Invoke(
                    cell
                );
            }


            if (changed)
            {
                gridCanvas.MarkDirtyRepaint();
            }
        }
    }


    private void OnPointerUp(
        PointerUpEvent evt)
    {
        if (!pointerPressed)
            return;


        pointerPressed =
            false;


        if (
            TryPointerToCell(
                evt.position,
                out Vector2Int cell
            ))
        {
            PointerUp?.Invoke(
                cell
            );
        }


        ReleasePointer();


        evt.StopPropagation();
    }


    private void OnPointerCancel(
        PointerCancelEvent evt)
    {
        pointerPressed =
            false;


        ReleasePointer();
    }


    private void OnPointerLeave(
        PointerLeaveEvent evt)
    {
        if (pointerPressed)
            return;


        hoveredCell =
            null;


        UpdateCursorLabel(
            null
        );


        gridCanvas.MarkDirtyRepaint();
    }


    private void ReleasePointer()
    {
        if (
            capturedPointerId >=
            0 &&
            gridCanvas.HasPointerCapture(
                capturedPointerId
            ))
        {
            gridCanvas.ReleasePointer(
                capturedPointerId
            );
        }


        capturedPointerId =
            -1;
    }


    private void UpdateCursorLabel(
        Vector2Int? cell)
    {
        if (
            cursorCellLabel ==
            null)
        {
            return;
        }


        cursorCellLabel.text =
            cell.HasValue
                ? $"Cell: {cell.Value.x}, {cell.Value.y}"
                : "Cell: —, —";
    }


    private bool TryPointerToCell(
        Vector3 panelPosition,
        out Vector2Int cell)
    {
        cell =
            default;


        if (
            context.CurrentRoom ==
            null)
        {
            return false;
        }


        Vector2 local =
            gridCanvas.WorldToLocal(
                new Vector2(
                    panelPosition.x,
                    panelPosition.y
                )
            );


        float size =
            context.CellSize *
            context.Zoom;


        cell =
            new Vector2Int(
                Mathf.FloorToInt(
                    local.x /
                    size
                ),
                Mathf.FloorToInt(
                    local.y /
                    size
                )
            );


        return
            context.IsInside(
                cell
            );
    }


    // =========================================================
    // Draw
    // =========================================================

    private void DrawGrid(
        MeshGenerationContext mgc)
    {
        if (
            context.CurrentRoom ==
            null)
        {
            return;
        }


        Painter2D painter =
            mgc.painter2D;


        float cellSize =
            context.CellSize *
            context.Zoom;


        DrawCells(
            painter,
            cellSize
        );


        DrawLines(
            painter,
            cellSize
        );


        DrawSockets(
            painter,
            cellSize
        );


        DrawHover(
            painter,
            cellSize
        );


        DrawSelection(
            painter,
            cellSize
        );
    }


    private void DrawCells(
        Painter2D painter,
        float cellSize)
    {
        Color defaultColor =
            EditorGUIUtility
                .isProSkin
                ? new Color(
                    0.12f,
                    0.13f,
                    0.15f
                )
                : new Color(
                    0.83f,
                    0.83f,
                    0.84f
                );


        for (
            int y = 0;
            y < context.Height;
            y++)
        {
            for (
                int x = 0;
                x < context.Width;
                x++)
            {
                Vector2Int cell =
                    new(
                        x,
                        y
                    );


                DrawFilledCell(
                    painter,
                    cell,
                    cellSize,
                    CellColorProvider
                        ?.Invoke(cell)
                    ??
                    defaultColor
                );
            }
        }
    }


    private static void DrawFilledCell(
        Painter2D painter,
        Vector2Int position,
        float size,
        Color color)
    {
        float left =
            position.x *
            size;


        float top =
            position.y *
            size;


        float right =
            left +
            size;


        float bottom =
            top +
            size;


        painter.fillColor =
            color;


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
            EditorGUIUtility
                .isProSkin
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


        painter.lineWidth =
            1f;


        painter.BeginPath();


        for (
            int x = 0;
            x <= context.Width;
            x++)
        {
            float px =
                x *
                cellSize;


            painter.MoveTo(
                new Vector2(
                    px,
                    0f
                )
            );


            painter.LineTo(
                new Vector2(
                    px,
                    height
                )
            );
        }


        for (
            int y = 0;
            y <= context.Height;
            y++)
        {
            float py =
                y *
                cellSize;


            painter.MoveTo(
                new Vector2(
                    0f,
                    py
                )
            );


            painter.LineTo(
                new Vector2(
                    width,
                    py
                )
            );
        }


        painter.Stroke();
    }


    // =========================================================
    // Socket marker
    // =========================================================

    private void DrawSockets(
        Painter2D painter,
        float cellSize)
    {
        if (
            SocketProvider ==
            null)
        {
            return;
        }


        for (
            int y = 0;
            y < context.Height;
            y++)
        {
            for (
                int x = 0;
                x < context.Width;
                x++)
            {
                Vector2Int cell =
                    new(
                        x,
                        y
                    );


                RoomSocketData socket =
                    SocketProvider(
                        cell
                    );


                if (socket == null)
                    continue;


                float centerX =
                    (
                        x +
                        0.5f
                    ) *
                    cellSize;


                float centerY =
                    (
                        y +
                        0.5f
                    ) *
                    cellSize;


                float radius =
                    Mathf.Max(
                        3f,
                        cellSize *
                        0.22f
                    );


                painter.fillColor =
                    new Color(
                        0.3f,
                        0.75f,
                        1f,
                        1f
                    );


                painter.BeginPath();


                painter.MoveTo(
                    new Vector2(
                        centerX,
                        centerY -
                        radius
                    )
                );


                painter.LineTo(
                    new Vector2(
                        centerX +
                        radius,
                        centerY
                    )
                );


                painter.LineTo(
                    new Vector2(
                        centerX,
                        centerY +
                        radius
                    )
                );


                painter.LineTo(
                    new Vector2(
                        centerX -
                        radius,
                        centerY
                    )
                );


                painter.ClosePath();

                painter.Fill();
            }
        }
    }


    // =========================================================
    // Hover
    // =========================================================

    private void DrawHover(
        Painter2D painter,
        float cellSize)
    {
        if (
            !hoveredCell
                .HasValue)
        {
            return;
        }


        DrawOutline(
            painter,
            hoveredCell.Value,
            cellSize,
            new Color(
                1f,
                1f,
                1f,
                0.7f
            ),
            1.5f
        );
    }


    // =========================================================
    // Multi selection
    // =========================================================

    private void DrawSelection(
        Painter2D painter,
        float cellSize)
    {
        foreach (
            Vector2Int cell
            in context.SelectedCells)
        {
            DrawOutline(
                painter,
                cell,
                cellSize,
                new Color(
                    1f,
                    0.65f,
                    0.15f,
                    1f
                ),
                2.5f
            );
        }
    }


    private static void DrawOutline(
        Painter2D painter,
        Vector2Int position,
        float size,
        Color color,
        float lineWidth)
    {
        const float inset =
            2f;


        float left =
            position.x *
            size +
            inset;


        float top =
            position.y *
            size +
            inset;


        float right =
            (
                position.x +
                1
            ) *
            size -
            inset;


        float bottom =
            (
                position.y +
                1
            ) *
            size -
            inset;


        painter.strokeColor =
            color;


        painter.lineWidth =
            lineWidth;


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

        painter.Stroke();
    }


    // =========================================================
    // Query
    // =========================================================

    private static T RequireAny<T>(
        VisualElement root,
        params string[] names)
        where T : VisualElement
    {
        T result =
            FindAny<T>(
                root,
                names
            );


        if (result != null)
            return result;


        throw new InvalidOperationException(
            $"[RoomEditor] Missing {typeof(T).Name}: " +
            string.Join(
                ", ",
                names
            )
        );
    }


    private static T FindAny<T>(
        VisualElement root,
        params string[] names)
        where T : VisualElement
    {
        foreach (
            string name
            in names)
        {
            T element =
                root.Q<T>(
                    name
                );


            if (element != null)
                return element;
        }


        return null;
    }


    public void Dispose()
    {
        context.GridMetricsChanged -=
            Refresh;


        context.ZoomChanged -=
            Refresh;


        context.RoomChanged -=
            Refresh;


        context.ActiveLayerChanged -=
            Refresh;


        context.SelectionChanged -=
            RefreshGridOnly;


        gridCanvas.generateVisualContent -=
            DrawGrid;


        ReleasePointer();
    }
}