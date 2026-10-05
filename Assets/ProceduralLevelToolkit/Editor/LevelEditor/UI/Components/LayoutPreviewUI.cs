using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class LayoutPreviewUI
    : VisualElement
{
    // =========================================================
    // Zoom
    // =========================================================
    //
    // The preview no longer has a finite canvas.
    //
    // These limits only protect floating-point calculations.
    // In practice the user can zoom out far beyond any useful
    // level size.
    // =========================================================

    private const float MinZoom =
        0.0001f;

    private const float MaxZoom =
        100000f;

    private const float DefaultZoom =
        42f;


    private readonly LevelEditorContext context;


    private readonly VisualElement viewport;

    private readonly Label zoomLabel;

    private readonly Label positionLabel;


    // =========================================================
    // Camera
    // =========================================================

    private Vector2 cameraWorldCenter =
        Vector2.zero;

    private float zoom =
        DefaultZoom;


    // =========================================================
    // Pan
    // =========================================================

    private bool panning;

    private int capturedPointerId =
        -1;

    private Vector2 previousPointerPosition;


    // =========================================================
    // Construction
    // =========================================================

    public LayoutPreviewUI(
        LevelEditorContext context
    )
    {
        this.context =
            context;


        style.flexGrow =
            1;

        style.minWidth =
            0f;

        style.minHeight =
            0f;


        // =====================================================
        // Toolbar
        // =====================================================

        VisualElement toolbar =
            new VisualElement();


        toolbar.AddToClassList(
            "layout-preview-toolbar"
        );


        Button fitButton =
            new Button(
                Fit
            )
            {
                text =
                    "Fit"
            };


        Button resetViewButton =
            new Button(
                ResetView
            )
            {
                text =
                    "1:1"
            };


        zoomLabel =
            new Label();


        zoomLabel.style.minWidth =
            90f;


        positionLabel =
            new Label();


        positionLabel.style.opacity =
            0.6f;


        Label controlsLabel =
            new Label(
                "Wheel: zoom   •   Middle Mouse / Alt + Left: pan"
            );


        controlsLabel.style.opacity =
            0.55f;


        toolbar.Add(
            fitButton
        );


        toolbar.Add(
            resetViewButton
        );


        toolbar.Add(
            zoomLabel
        );


        toolbar.Add(
            positionLabel
        );


        toolbar.Add(
            controlsLabel
        );


        Add(
            toolbar
        );


        // =====================================================
        // Infinite viewport
        // =====================================================

        viewport =
            new VisualElement();


        viewport.name =
            "layout-infinite-viewport";


        viewport.style.flexGrow =
            1;

        viewport.style.minWidth =
            0f;

        viewport.style.minHeight =
            0f;

        viewport.style.overflow =
            Overflow.Hidden;


        viewport.focusable =
            true;


        viewport.generateVisualContent +=
            Draw;


        viewport.RegisterCallback<
            WheelEvent
        >(
            OnWheel
        );


        viewport.RegisterCallback<
            PointerDownEvent
        >(
            OnPointerDown
        );


        viewport.RegisterCallback<
            PointerMoveEvent
        >(
            OnPointerMove
        );


        viewport.RegisterCallback<
            PointerUpEvent
        >(
            OnPointerUp
        );


        viewport.RegisterCallback<
            PointerCancelEvent
        >(
            OnPointerCancel
        );


        viewport.RegisterCallback<
            PointerCaptureOutEvent
        >(
            OnPointerCaptureOut
        );


        viewport.RegisterCallback<
            GeometryChangedEvent
        >(
            _ =>
                viewport.MarkDirtyRepaint()
        );


        Add(
            viewport
        );


        // =====================================================
        // Context
        // =====================================================

        RegisterCallback<
            AttachToPanelEvent
        >(
            OnAttach
        );


        RegisterCallback<
            DetachFromPanelEvent
        >(
            OnDetach
        );


        Refresh();
    }


    // =========================================================
    // Context Events
    // =========================================================

    private void OnAttach(
        AttachToPanelEvent evt
    )
    {
        context.OnLayoutChanged +=
            Refresh;

        context.OnLevelChanged +=
            Refresh;

        context.OnSelectionChanged +=
            Refresh;
    }


    private void OnDetach(
        DetachFromPanelEvent evt
    )
    {
        context.OnLayoutChanged -=
            Refresh;

        context.OnLevelChanged -=
            Refresh;

        context.OnSelectionChanged -=
            Refresh;
    }


    // =========================================================
    // Refresh
    // =========================================================

    public void Refresh()
    {
        UpdateToolbar();

        viewport.MarkDirtyRepaint();
    }


    private void UpdateToolbar()
    {
        float percentage =
            zoom /
            DefaultZoom *
            100f;


        if (percentage >=
            0.01f)
        {
            zoomLabel.text =
                $"Zoom {percentage:0.##}%";
        }
        else
        {
            zoomLabel.text =
                $"Zoom {percentage:0.####}%";
        }


        positionLabel.text =
            $"Center ({cameraWorldCenter.x:0.##}, {cameraWorldCenter.y:0.##})";
    }


    // =========================================================
    // View Controls
    // =========================================================

    public void ResetView()
    {
        cameraWorldCenter =
            Vector2.zero;


        zoom =
            DefaultZoom;


        Refresh();
    }


    public void Fit()
    {
        SpatialLayoutData layout =
            context.SpatialLayout;


        if (layout == null ||
            layout.ModuleCount ==
            0)
        {
            cameraWorldCenter =
                Vector2.zero;


            zoom =
                DefaultZoom;


            Refresh();

            return;
        }


        float viewportWidth =
            viewport.resolvedStyle.width;


        float viewportHeight =
            viewport.resolvedStyle.height;


        if (viewportWidth <= 1f ||
            viewportHeight <= 1f)
        {
            return;
        }


        bool hasBounds =
            false;


        Rect totalBounds =
            new Rect();


        IReadOnlyList<
            LevelModuleInstanceData
        > modules =
            layout.Modules;


        for (int i = 0;
             i < modules.Count;
             i++)
        {
            LevelModuleInstanceData module =
                modules[i];


            if (module == null ||
                module.Room == null)
            {
                continue;
            }


            Rect bounds =
                LayoutGeometryUtility
                    .GetWorldBounds2D(
                        module
                    );


            if (!hasBounds)
            {
                totalBounds =
                    bounds;


                hasBounds =
                    true;
            }
            else
            {
                totalBounds =
                    Rect.MinMaxRect(
                        Mathf.Min(
                            totalBounds.xMin,
                            bounds.xMin
                        ),
                        Mathf.Min(
                            totalBounds.yMin,
                            bounds.yMin
                        ),
                        Mathf.Max(
                            totalBounds.xMax,
                            bounds.xMax
                        ),
                        Mathf.Max(
                            totalBounds.yMax,
                            bounds.yMax
                        )
                    );
            }
        }


        if (!hasBounds)
        {
            return;
        }


        cameraWorldCenter =
            totalBounds.center;


        /*
         * Add a small logical margin around the entire map.
         */
        float paddedWidth =
            Mathf.Max(
                totalBounds.width *
                1.15f,
                totalBounds.width +
                2f
            );


        float paddedHeight =
            Mathf.Max(
                totalBounds.height *
                1.15f,
                totalBounds.height +
                2f
            );


        float horizontalZoom =
            viewportWidth /
            Mathf.Max(
                paddedWidth,
                0.0001f
            );


        float verticalZoom =
            viewportHeight /
            Mathf.Max(
                paddedHeight,
                0.0001f
            );


        zoom =
            Mathf.Clamp(
                Mathf.Min(
                    horizontalZoom,
                    verticalZoom
                ),
                MinZoom,
                MaxZoom
            );


        Refresh();
    }


    // =========================================================
    // Wheel Zoom
    // =========================================================

    private void OnWheel(
        WheelEvent evt
    )
    {
        Vector2 mouseScreen =
            evt.localMousePosition;


        /*
         * Keep the world position under the mouse fixed while
         * zooming. This makes large-layout navigation much more
         * comfortable than zooming around the viewport center.
         */

        Vector2 worldBeforeZoom =
            ScreenToWorld(
                mouseScreen
            );


        float zoomFactor =
            Mathf.Exp(
                -evt.delta.y *
                0.08f
            );


        float newZoom =
            Mathf.Clamp(
                zoom *
                zoomFactor,
                MinZoom,
                MaxZoom
            );


        if (Mathf.Approximately(
                newZoom,
                zoom
            ))
        {
            return;
        }


        zoom =
            newZoom;


        Vector2 relativeScreen =
            ScreenOffsetFromCenter(
                mouseScreen
            );


        cameraWorldCenter =
            worldBeforeZoom -
            relativeScreen /
            zoom;


        Refresh();


        evt.StopPropagation();
    }


    // =========================================================
    // Pointer Input
    // =========================================================

    private void OnPointerDown(
        PointerDownEvent evt
    )
    {
        Vector2 localPosition =
            viewport.WorldToLocal(
                evt.position
            );


        bool wantsPan =
            evt.button ==
            2
            ||
            (
                evt.button ==
                0 &&
                evt.altKey
            );


        if (wantsPan)
        {
            BeginPan(
                evt,
                localPosition
            );


            return;
        }


        if (evt.button !=
            0)
        {
            return;
        }


        SelectModuleAt(
            localPosition
        );


        evt.StopPropagation();
    }


    private void BeginPan(
        PointerDownEvent evt,
        Vector2 localPosition
    )
    {
        panning =
            true;


        capturedPointerId =
            evt.pointerId;


        previousPointerPosition =
            localPosition;


        PointerCaptureHelper
            .CapturePointer(
                viewport,
                capturedPointerId
            );


        evt.StopPropagation();
    }


    private void OnPointerMove(
        PointerMoveEvent evt
    )
    {
        if (!panning ||
            evt.pointerId !=
            capturedPointerId)
        {
            return;
        }


        if (!PointerCaptureHelper
                .HasPointerCapture(
                    viewport,
                    capturedPointerId
                ))
        {
            return;
        }


        Vector2 currentPointerPosition =
            viewport.WorldToLocal(
                evt.position
            );


        Vector2 delta =
            currentPointerPosition -
            previousPointerPosition;


        /*
         * Horizontal screen movement maps directly to world X.
         * Screen Y grows downward, while layout Y grows upward.
         */

        cameraWorldCenter.x -=
            delta.x /
            zoom;


        cameraWorldCenter.y +=
            delta.y /
            zoom;


        previousPointerPosition =
            currentPointerPosition;


        Refresh();


        evt.StopPropagation();
    }


    private void OnPointerUp(
        PointerUpEvent evt
    )
    {
        if (!panning ||
            evt.pointerId !=
            capturedPointerId)
        {
            return;
        }


        EndPan();


        evt.StopPropagation();
    }


    private void OnPointerCancel(
        PointerCancelEvent evt
    )
    {
        if (evt.pointerId !=
            capturedPointerId)
        {
            return;
        }


        EndPan();
    }


    private void OnPointerCaptureOut(
        PointerCaptureOutEvent evt
    )
    {
        panning =
            false;


        capturedPointerId =
            -1;
    }


    private void EndPan()
    {
        panning =
            false;


        if (capturedPointerId <
            0)
        {
            return;
        }


        if (PointerCaptureHelper
                .HasPointerCapture(
                    viewport,
                    capturedPointerId
                ))
        {
            PointerCaptureHelper
                .ReleasePointer(
                    viewport,
                    capturedPointerId
                );
        }


        capturedPointerId =
            -1;
    }


    // =========================================================
    // Selection
    // =========================================================

    private void SelectModuleAt(
        Vector2 screenPosition
    )
    {
        SpatialLayoutData layout =
            context.SpatialLayout;


        if (layout == null)
        {
            context.ClearSelection();

            return;
        }


        Vector2 worldPosition =
            ScreenToWorld(
                screenPosition
            );


        IReadOnlyList<
            LevelModuleInstanceData
        > modules =
            layout.Modules;


        /*
         * Reverse iteration makes the last-drawn room win if
         * several footprints ever overlap visually.
         */

        for (int i =
                 modules.Count - 1;
             i >= 0;
             i--)
        {
            LevelModuleInstanceData module =
                modules[i];


            if (module == null ||
                module.Room == null)
            {
                continue;
            }


            Rect bounds =
                LayoutGeometryUtility
                    .GetWorldBounds2D(
                        module
                    );


            if (!bounds.Contains(
                    worldPosition
                ))
            {
                continue;
            }


            LevelNodeData node =
                context.Graph?
                    .FindNode(
                        module.NodeId
                    );


            if (node != null)
            {
                context.SelectNode(
                    node
                );
            }


            return;
        }


        context.ClearSelection();
    }


    // =========================================================
    // Drawing
    // =========================================================

    private void Draw(
        MeshGenerationContext mgc
    )
    {
        Painter2D painter =
            mgc.painter2D;


        DrawInfiniteGrid(
            painter
        );


        SpatialLayoutData layout =
            context.SpatialLayout;


        if (layout == null)
        {
            return;
        }


        DrawConnections(
            painter,
            layout
        );


        DrawModules(
            painter,
            layout
        );
    }


    // =========================================================
    // Infinite Grid
    // =========================================================

    private void DrawInfiniteGrid(
        Painter2D painter
    )
    {
        float width =
            viewport.resolvedStyle.width;


        float height =
            viewport.resolvedStyle.height;


        if (width <= 0f ||
            height <= 0f)
        {
            return;
        }


        Vector2 topLeftWorld =
            ScreenToWorld(
                Vector2.zero
            );


        Vector2 bottomRightWorld =
            ScreenToWorld(
                new Vector2(
                    width,
                    height
                )
            );


        float minX =
            Mathf.Min(
                topLeftWorld.x,
                bottomRightWorld.x
            );


        float maxX =
            Mathf.Max(
                topLeftWorld.x,
                bottomRightWorld.x
            );


        float minY =
            Mathf.Min(
                topLeftWorld.y,
                bottomRightWorld.y
            );


        float maxY =
            Mathf.Max(
                topLeftWorld.y,
                bottomRightWorld.y
            );


        /*
         * Adaptive world-grid spacing.
         *
         * No matter how far the user zooms out, we draw only a
         * reasonable number of lines. The grid therefore remains
         * fast even for enormous layouts.
         */

        float minorStep =
            CalculateNiceGridStep(
                38f /
                zoom
            );


        float majorStep =
            minorStep *
            5f;


        DrawGridLayer(
            painter,
            minX,
            maxX,
            minY,
            maxY,
            minorStep,
            false
        );


        DrawGridLayer(
            painter,
            minX,
            maxX,
            minY,
            maxY,
            majorStep,
            true
        );


        DrawOriginAxes(
            painter,
            minX,
            maxX,
            minY,
            maxY
        );
    }


    private void DrawGridLayer(
        Painter2D painter,
        float minX,
        float maxX,
        float minY,
        float maxY,
        float step,
        bool major
    )
    {
        if (step <=
            0f)
        {
            return;
        }


        painter.strokeColor =
            EditorGUIUtility.isProSkin
                ? new Color(
                    1f,
                    1f,
                    1f,
                    major
                        ? 0.09f
                        : 0.035f
                )
                : new Color(
                    0f,
                    0f,
                    0f,
                    major
                        ? 0.12f
                        : 0.05f
                );


        painter.lineWidth =
            major
                ? 1.25f
                : 1f;


        float firstX =
            Mathf.Floor(
                minX /
                step
            ) *
            step;


        float firstY =
            Mathf.Floor(
                minY /
                step
            ) *
            step;


        painter.BeginPath();


        for (float x = firstX;
             x <= maxX;
             x += step)
        {
            Vector2 a =
                WorldToScreen(
                    new Vector2(
                        x,
                        minY
                    )
                );


            Vector2 b =
                WorldToScreen(
                    new Vector2(
                        x,
                        maxY
                    )
                );


            painter.MoveTo(
                a
            );


            painter.LineTo(
                b
            );
        }


        for (float y = firstY;
             y <= maxY;
             y += step)
        {
            Vector2 a =
                WorldToScreen(
                    new Vector2(
                        minX,
                        y
                    )
                );


            Vector2 b =
                WorldToScreen(
                    new Vector2(
                        maxX,
                        y
                    )
                );


            painter.MoveTo(
                a
            );


            painter.LineTo(
                b
            );
        }


        painter.Stroke();
    }


    private void DrawOriginAxes(
        Painter2D painter,
        float minX,
        float maxX,
        float minY,
        float maxY
    )
    {
        painter.strokeColor =
            new Color(
                0.45f,
                0.55f,
                0.70f,
                0.38f
            );


        painter.lineWidth =
            1.5f;


        painter.BeginPath();


        if (minX <= 0f &&
            maxX >= 0f)
        {
            painter.MoveTo(
                WorldToScreen(
                    new Vector2(
                        0f,
                        minY
                    )
                )
            );


            painter.LineTo(
                WorldToScreen(
                    new Vector2(
                        0f,
                        maxY
                    )
                )
            );
        }


        if (minY <= 0f &&
            maxY >= 0f)
        {
            painter.MoveTo(
                WorldToScreen(
                    new Vector2(
                        minX,
                        0f
                    )
                )
            );


            painter.LineTo(
                WorldToScreen(
                    new Vector2(
                        maxX,
                        0f
                    )
                )
            );
        }


        painter.Stroke();
    }


    private static float CalculateNiceGridStep(
        float targetWorldStep
    )
    {
        targetWorldStep =
            Mathf.Max(
                targetWorldStep,
                0.000001f
            );


        float power =
            Mathf.Pow(
                10f,
                Mathf.Floor(
                    Mathf.Log10(
                        targetWorldStep
                    )
                )
            );


        float normalized =
            targetWorldStep /
            power;


        float niceNormalized;


        if (normalized <=
            1f)
        {
            niceNormalized =
                1f;
        }
        else if (normalized <=
                 2f)
        {
            niceNormalized =
                2f;
        }
        else if (normalized <=
                 5f)
        {
            niceNormalized =
                5f;
        }
        else
        {
            niceNormalized =
                10f;
        }


        return
            niceNormalized *
            power;
    }


    // =========================================================
    // Modules
    // =========================================================

    private void DrawModules(
        Painter2D painter,
        SpatialLayoutData layout
    )
    {
        IReadOnlyList<
            LevelModuleInstanceData
        > modules =
            layout.Modules;


        for (int i = 0;
             i < modules.Count;
             i++)
        {
            LevelModuleInstanceData module =
                modules[i];


            if (module == null ||
                module.Room == null)
            {
                continue;
            }


            Rect worldBounds =
                LayoutGeometryUtility
                    .GetWorldBounds2D(
                        module
                    );


            Rect screenBounds =
                WorldRectToScreen(
                    worldBounds
                );


            /*
             * Skip objects fully outside the visible viewport.
             * This becomes useful once very large levels exist.
             */

            if (!IsVisible(
                    screenBounds
                ))
            {
                continue;
            }


            bool selected =
                context.SelectedNode != null &&
                context.SelectedNode.Id ==
                module.NodeId;


            painter.fillColor =
                selected
                    ? new Color(
                        0.20f,
                        0.42f,
                        0.72f,
                        0.55f
                    )
                    : new Color(
                        0.28f,
                        0.31f,
                        0.36f,
                        0.80f
                    );


            painter.strokeColor =
                selected
                    ? new Color(
                        0.25f,
                        0.62f,
                        1f,
                        1f
                    )
                    : new Color(
                        0.78f,
                        0.80f,
                        0.84f,
                        0.75f
                    );


            painter.lineWidth =
                selected
                    ? 3f
                    : 1.5f;


            DrawRect(
                painter,
                screenBounds
            );


            DrawModuleSockets(
                painter,
                module
            );
        }
    }


    private void DrawModuleSockets(
        Painter2D painter,
        LevelModuleInstanceData module
    )
    {
        IReadOnlyList<
            RoomSocketData
        > sockets =
            module.Room.Sockets;


        if (sockets == null)
        {
            return;
        }


        /*
         * Keep socket markers readable while zooming out,
         * without allowing them to become huge while zooming in.
         */

        float radius =
            Mathf.Clamp(
                4f,
                2.5f,
                6f
            );


        for (int i = 0;
             i < sockets.Count;
             i++)
        {
            RoomSocketData socket =
                sockets[i];


            if (socket == null)
            {
                continue;
            }


            Vector2 world =
                LayoutGeometryUtility
                    .GetWorldSocketAnchor2D(
                        module,
                        socket
                    );


            Vector2 screen =
                WorldToScreen(
                    world
                );


            if (!IsVisiblePoint(
                    screen,
                    10f
                ))
            {
                continue;
            }


            painter.fillColor =
                new Color(
                    1f,
                    0.72f,
                    0.25f,
                    1f
                );


            painter.BeginPath();


            painter.Arc(
                screen,
                radius,
                0f,
                360f
            );


            painter.Fill();
        }
    }


    // =========================================================
    // Connections
    // =========================================================

    private void DrawConnections(
        Painter2D painter,
        SpatialLayoutData layout
    )
    {
        IReadOnlyList<
            LevelSocketConnectionData
        > connections =
            layout.SocketConnections;


        painter.strokeColor =
            new Color(
                0.35f,
                0.82f,
                0.46f,
                1f
            );


        painter.lineWidth =
            3f;


        for (int i = 0;
             i < connections.Count;
             i++)
        {
            LevelSocketConnectionData connection =
                connections[i];


            if (connection == null)
            {
                continue;
            }


            Vector2 point =
                WorldToScreen(
                    new Vector2(
                        connection.WorldPosition.x,
                        connection.WorldPosition.z
                    )
                );


            if (!IsVisiblePoint(
                    point,
                    12f
                ))
            {
                continue;
            }


            const float size =
                8f;


            painter.BeginPath();


            painter.MoveTo(
                point +
                new Vector2(
                    -size,
                    0f
                )
            );


            painter.LineTo(
                point +
                new Vector2(
                    size,
                    0f
                )
            );


            painter.MoveTo(
                point +
                new Vector2(
                    0f,
                    -size
                )
            );


            painter.LineTo(
                point +
                new Vector2(
                    0f,
                    size
                )
            );


            painter.Stroke();
        }
    }


    // =========================================================
    // Rect Drawing
    // =========================================================

    private static void DrawRect(
        Painter2D painter,
        Rect rect
    )
    {
        painter.BeginPath();


        painter.MoveTo(
            new Vector2(
                rect.xMin,
                rect.yMin
            )
        );


        painter.LineTo(
            new Vector2(
                rect.xMax,
                rect.yMin
            )
        );


        painter.LineTo(
            new Vector2(
                rect.xMax,
                rect.yMax
            )
        );


        painter.LineTo(
            new Vector2(
                rect.xMin,
                rect.yMax
            )
        );


        painter.ClosePath();


        painter.Fill();

        painter.Stroke();
    }


    // =========================================================
    // Coordinate Conversion
    // =========================================================

    private Vector2 WorldToScreen(
        Vector2 world
    )
    {
        Vector2 center =
            GetViewportCenter();


        Vector2 delta =
            world -
            cameraWorldCenter;


        return new Vector2(
            center.x +
            delta.x *
            zoom,

            center.y -
            delta.y *
            zoom
        );
    }


    private Vector2 ScreenToWorld(
        Vector2 screen
    )
    {
        Vector2 center =
            GetViewportCenter();


        return new Vector2(
            cameraWorldCenter.x +
            (
                screen.x -
                center.x
            ) /
            zoom,

            cameraWorldCenter.y -
            (
                screen.y -
                center.y
            ) /
            zoom
        );
    }


    private Vector2 ScreenOffsetFromCenter(
        Vector2 screen
    )
    {
        Vector2 center =
            GetViewportCenter();


        return new Vector2(
            (
                screen.x -
                center.x
            ),

            -(
                screen.y -
                center.y
            )
        );
    }


    private Vector2 GetViewportCenter()
    {
        return new Vector2(
            viewport.resolvedStyle.width *
            0.5f,

            viewport.resolvedStyle.height *
            0.5f
        );
    }


    private Rect WorldRectToScreen(
        Rect world
    )
    {
        Vector2 topLeft =
            WorldToScreen(
                new Vector2(
                    world.xMin,
                    world.yMax
                )
            );


        Vector2 bottomRight =
            WorldToScreen(
                new Vector2(
                    world.xMax,
                    world.yMin
                )
            );


        return Rect.MinMaxRect(
            topLeft.x,
            topLeft.y,
            bottomRight.x,
            bottomRight.y
        );
    }


    // =========================================================
    // Visibility
    // =========================================================

    private bool IsVisible(
        Rect screenRect
    )
    {
        Rect visible =
            new Rect(
                0f,
                0f,
                viewport.resolvedStyle.width,
                viewport.resolvedStyle.height
            );


        return visible.Overlaps(
            screenRect,
            true
        );
    }


    private bool IsVisiblePoint(
        Vector2 point,
        float margin
    )
    {
        return
            point.x >=
            -margin
            &&
            point.y >=
            -margin
            &&
            point.x <=
            viewport.resolvedStyle.width +
            margin
            &&
            point.y <=
            viewport.resolvedStyle.height +
            margin;
    }
}
