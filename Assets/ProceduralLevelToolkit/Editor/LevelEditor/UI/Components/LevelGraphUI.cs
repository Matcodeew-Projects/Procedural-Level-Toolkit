using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class LevelGraphUI
    : VisualElement
{
    private const float NodeWidth =
        180f;

    private const float NodeHeight =
        72f;

    private const float ConnectionHitDistance =
        10f;

    private const int ConnectionHitSamples =
        32;

    private const float DefaultGridStep =
        40f;

    private const float MinZoom =
        0.0001f;

    private const float MaxZoom =
        1000f;


    private readonly LevelEditorContext context;

    private readonly VisualElement viewport;

    private readonly Label zoomLabel;

    private readonly Label positionLabel;


    private readonly Dictionary<
        string,
        NodeVisual
    > nodeVisuals =
        new Dictionary<
            string,
            NodeVisual
        >();


    private Vector2 cameraWorldCenter =
        new Vector2(
            500f,
            300f
        );

    private float zoom =
        1f;


    private bool panning;

    private int capturedPanPointerId =
        -1;

    private Vector2 previousPanPointer;


    public float Zoom =>
        zoom;


    public LevelGraphUI(
        LevelEditorContext context
    )
    {
        this.context =
            context;


        AddToClassList(
            "level-panel"
        );


        style.flexGrow =
            1;

        style.minWidth =
            0f;

        style.minHeight =
            0f;


        focusable =
            true;


        // =====================================================
        // Toolbar
        // =====================================================

        VisualElement toolbar =
            new VisualElement();


        toolbar.AddToClassList(
            "level-graph-toolbar"
        );


        Button fitButton =
            new Button(
                Fit
            )
            {
                text =
                    "Fit"
            };


        Button resetButton =
            new Button(
                ResetView
            )
            {
                text =
                    "100%"
            };


        zoomLabel =
            new Label();


        zoomLabel.AddToClassList(
            "level-graph-toolbar__zoom"
        );


        positionLabel =
            new Label();


        positionLabel.AddToClassList(
            "level-graph-toolbar__position"
        );


        Label help =
            new Label(
                "Shift + node: connect  •  Delete: remove  •  Arrows: navigate  •  Wheel: zoom  •  Middle / Alt+Left: pan"
            );


        help.AddToClassList(
            "level-graph-toolbar__help"
        );


        toolbar.Add(
            fitButton
        );


        toolbar.Add(
            resetButton
        );


        toolbar.Add(
            zoomLabel
        );


        toolbar.Add(
            positionLabel
        );


        toolbar.Add(
            help
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
            "level-graph-infinite-viewport";


        viewport.AddToClassList(
            "level-graph-viewport"
        );


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
            DrawGraph;


        viewport.RegisterCallback<
            WheelEvent
        >(
            OnWheel
        );


        viewport.RegisterCallback<
            PointerDownEvent
        >(
            OnViewportPointerDown
        );


        viewport.RegisterCallback<
            PointerMoveEvent
        >(
            OnViewportPointerMove
        );


        viewport.RegisterCallback<
            PointerUpEvent
        >(
            OnViewportPointerUp
        );


        viewport.RegisterCallback<
            PointerCancelEvent
        >(
            OnViewportPointerCancel
        );


        viewport.RegisterCallback<
            PointerCaptureOutEvent
        >(
            OnViewportPointerCaptureOut
        );


        viewport.RegisterCallback<
            KeyDownEvent
        >(
            OnKeyDown
        );


        viewport.RegisterCallback<
            GeometryChangedEvent
        >(
            _ =>
            {
                RefreshVisualTransforms();

                viewport.MarkDirtyRepaint();
            }
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

        UpdateToolbar();
    }


    // =========================================================
    // Context Events
    // =========================================================

    private void OnAttach(
        AttachToPanelEvent evt
    )
    {
        context.OnGraphChanged +=
            Refresh;

        context.OnLevelChanged +=
            Refresh;

        context.OnSelectionChanged +=
            RefreshSelection;

        context.OnNodePositionChanged +=
            RefreshNodePosition;
    }


    private void OnDetach(
        DetachFromPanelEvent evt
    )
    {
        context.OnGraphChanged -=
            Refresh;

        context.OnLevelChanged -=
            Refresh;

        context.OnSelectionChanged -=
            RefreshSelection;

        context.OnNodePositionChanged -=
            RefreshNodePosition;
    }


    // =========================================================
    // Public View API
    // =========================================================

    public Vector2 GetSuggestedNodePosition()
    {
        Vector2 viewportCenter =
            new Vector2(
                viewport.resolvedStyle.width *
                0.5f,
                viewport.resolvedStyle.height *
                0.5f
            );


        Vector2 worldCenter =
            ScreenToWorld(
                viewportCenter
            );


        int count =
            context.Graph?.NodeCount
            ?? 0;


        int column =
            count %
            3;

        int row =
            count /
            3;


        return worldCenter +
            new Vector2(
                (
                    column -
                    1
                ) *
                230f,
                row *
                130f
            );
    }


    public void FocusGraph()
    {
        viewport.Focus();
    }


    public void ResetView()
    {
        zoom =
            1f;


        if (context.Graph != null &&
            context.Graph.NodeCount >
            0)
        {
            Fit();

            return;
        }


        cameraWorldCenter =
            Vector2.zero;


        RefreshView();
    }


    public void Fit()
    {
        if (context.Graph == null ||
            context.Graph.NodeCount ==
            0)
        {
            cameraWorldCenter =
                Vector2.zero;


            zoom =
                1f;


            RefreshView();

            return;
        }


        float viewportWidth =
            viewport.resolvedStyle.width;

        float viewportHeight =
            viewport.resolvedStyle.height;


        if (viewportWidth <=
                1f ||
            viewportHeight <=
                1f)
        {
            return;
        }


        bool hasBounds =
            false;


        Rect total =
            new Rect();


        for (int i = 0;
             i < context.Graph.Nodes.Count;
             i++)
        {
            LevelNodeData node =
                context.Graph.Nodes[i];


            if (node == null)
            {
                continue;
            }


            Rect bounds =
                new Rect(
                    node.GraphPosition.x,
                    node.GraphPosition.y,
                    NodeWidth,
                    NodeHeight
                );


            if (!hasBounds)
            {
                total =
                    bounds;


                hasBounds =
                    true;
            }
            else
            {
                total =
                    Rect.MinMaxRect(
                        Mathf.Min(
                            total.xMin,
                            bounds.xMin
                        ),
                        Mathf.Min(
                            total.yMin,
                            bounds.yMin
                        ),
                        Mathf.Max(
                            total.xMax,
                            bounds.xMax
                        ),
                        Mathf.Max(
                            total.yMax,
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
            total.center;


        float paddedWidth =
            Mathf.Max(
                total.width +
                160f,
                total.width *
                1.15f
            );


        float paddedHeight =
            Mathf.Max(
                total.height +
                120f,
                total.height *
                1.15f
            );


        float zoomX =
            viewportWidth /
            Mathf.Max(
                1f,
                paddedWidth
            );


        float zoomY =
            viewportHeight /
            Mathf.Max(
                1f,
                paddedHeight
            );


        zoom =
            Mathf.Clamp(
                Mathf.Min(
                    zoomX,
                    zoomY
                ),
                MinZoom,
                MaxZoom
            );


        RefreshView();
    }


    // =========================================================
    // Refresh
    // =========================================================

    public void Refresh()
    {
        foreach (
            KeyValuePair<
                string,
                NodeVisual
            > pair
            in nodeVisuals
        )
        {
            pair.Value
                .RemoveFromHierarchy();
        }


        nodeVisuals.Clear();


        if (context.Graph != null)
        {
            for (int i = 0;
                 i < context.Graph.Nodes.Count;
                 i++)
            {
                LevelNodeData node =
                    context.Graph.Nodes[i];


                if (node == null)
                {
                    continue;
                }


                CreateNodeVisual(
                    node
                );
            }
        }


        RefreshVisualTransforms();

        RefreshSelection();

        viewport.MarkDirtyRepaint();
    }


    private void RefreshView()
    {
        RefreshVisualTransforms();

        UpdateToolbar();

        viewport.MarkDirtyRepaint();
    }


    private void RefreshVisualTransforms()
    {
        foreach (
            KeyValuePair<
                string,
                NodeVisual
            > pair
            in nodeVisuals
        )
        {
            UpdateNodeVisualTransform(
                pair.Value
            );
        }
    }


    private void UpdateNodeVisualTransform(
        NodeVisual visual
    )
    {
        if (visual == null)
        {
            return;
        }


        Vector2 screen =
            WorldToScreen(
                visual.Node.GraphPosition
            );


        /*
         * UI Toolkit scales around the element center by default.
         * Compensate left/top so the logical top-left remains at
         * the exact transformed graph position.
         */
        visual.style.left =
            screen.x +
            (
                zoom -
                1f
            ) *
            NodeWidth *
            0.5f;


        visual.style.top =
            screen.y +
            (
                zoom -
                1f
            ) *
            NodeHeight *
            0.5f;


        visual.transform.scale =
            new Vector3(
                zoom,
                zoom,
                1f
            );
    }


    private void RefreshSelection()
    {
        foreach (
            KeyValuePair<
                string,
                NodeVisual
            > pair
            in nodeVisuals
        )
        {
            bool selected =
                context.SelectedNode != null &&
                context.SelectedNode.Id ==
                pair.Key;


            pair.Value.EnableInClassList(
                "level-node--selected",
                selected
            );
        }


        viewport.MarkDirtyRepaint();
    }


    private void RefreshNodePosition(
        LevelNodeData node
    )
    {
        if (node == null)
        {
            return;
        }


        if (nodeVisuals.TryGetValue(
                node.Id,
                out NodeVisual visual
            ))
        {
            UpdateNodeVisualTransform(
                visual
            );
        }


        viewport.MarkDirtyRepaint();
    }


    private void UpdateToolbar()
    {
        float percentage =
            zoom *
            100f;


        zoomLabel.text =
            percentage >=
            0.01f
                ? $"{percentage:0.##}%"
                : $"{percentage:0.####}%";


        positionLabel.text =
            $"Center {cameraWorldCenter.x:0.#}, {cameraWorldCenter.y:0.#}";
    }


    // =========================================================
    // Node
    // =========================================================

    private void CreateNodeVisual(
        LevelNodeData node
    )
    {
        NodeVisual visual =
            new NodeVisual(
                this,
                context,
                node
            );


        viewport.Add(
            visual
        );


        nodeVisuals[
            node.Id
        ] =
            visual;


        UpdateNodeVisualTransform(
            visual
        );
    }


    // =========================================================
    // Zoom
    // =========================================================

    private void OnWheel(
        WheelEvent evt
    )
    {
        Vector2 mouseScreen =
            evt.localMousePosition;


        Vector2 worldBefore =
            ScreenToWorld(
                mouseScreen
            );


        float factor =
            Mathf.Exp(
                -evt.delta.y *
                0.08f
            );


        float newZoom =
            Mathf.Clamp(
                zoom *
                factor,
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


        Vector2 center =
            GetViewportCenter();


        cameraWorldCenter =
            worldBefore -
            (
                mouseScreen -
                center
            ) /
            zoom;


        RefreshView();


        evt.StopPropagation();
    }


    // =========================================================
    // Viewport Input
    // =========================================================

    private void OnViewportPointerDown(
        PointerDownEvent evt
    )
    {
        FocusGraph();


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


        if (evt.target !=
            viewport)
        {
            return;
        }


        LevelConnectionData connection =
            FindConnectionAtPosition(
                localPosition
            );


        if (connection != null)
        {
            if (evt.shiftKey)
            {
                context.RemoveConnection(
                    connection
                );
            }
            else
            {
                context.SelectConnection(
                    connection
                );
            }


            evt.StopPropagation();

            return;
        }


        context.ClearSelection();


        evt.StopPropagation();
    }


    private void BeginPan(
        PointerDownEvent evt,
        Vector2 localPosition
    )
    {
        panning =
            true;


        capturedPanPointerId =
            evt.pointerId;


        previousPanPointer =
            localPosition;


        PointerCaptureHelper
            .CapturePointer(
                viewport,
                capturedPanPointerId
            );


        evt.StopPropagation();
    }


    private void OnViewportPointerMove(
        PointerMoveEvent evt
    )
    {
        if (!panning ||
            evt.pointerId !=
            capturedPanPointerId)
        {
            return;
        }


        if (!PointerCaptureHelper
                .HasPointerCapture(
                    viewport,
                    capturedPanPointerId
                ))
        {
            return;
        }


        Vector2 current =
            viewport.WorldToLocal(
                evt.position
            );


        Vector2 delta =
            current -
            previousPanPointer;


        cameraWorldCenter -=
            delta /
            zoom;


        previousPanPointer =
            current;


        RefreshView();


        evt.StopPropagation();
    }


    private void OnViewportPointerUp(
        PointerUpEvent evt
    )
    {
        if (!panning ||
            evt.pointerId !=
            capturedPanPointerId)
        {
            return;
        }


        EndPan();


        evt.StopPropagation();
    }


    private void OnViewportPointerCancel(
        PointerCancelEvent evt
    )
    {
        if (evt.pointerId ==
            capturedPanPointerId)
        {
            EndPan();
        }
    }


    private void OnViewportPointerCaptureOut(
        PointerCaptureOutEvent evt
    )
    {
        panning =
            false;


        capturedPanPointerId =
            -1;
    }


    private void EndPan()
    {
        panning =
            false;


        if (capturedPanPointerId <
            0)
        {
            return;
        }


        if (PointerCaptureHelper
                .HasPointerCapture(
                    viewport,
                    capturedPanPointerId
                ))
        {
            PointerCaptureHelper
                .ReleasePointer(
                    viewport,
                    capturedPanPointerId
                );
        }


        capturedPanPointerId =
            -1;
    }


    // =========================================================
    // Keyboard
    // =========================================================

    private void OnKeyDown(
        KeyDownEvent evt
    )
    {
        switch (evt.keyCode)
        {
            case KeyCode.Delete:
            case KeyCode.Backspace:
                DeleteSelection();

                evt.StopPropagation();

                break;


            case KeyCode.LeftArrow:
                NavigateSelection(
                    Vector2.left
                );

                evt.StopPropagation();

                break;


            case KeyCode.RightArrow:
                NavigateSelection(
                    Vector2.right
                );

                evt.StopPropagation();

                break;


            case KeyCode.UpArrow:
                NavigateSelection(
                    Vector2.up
                );

                evt.StopPropagation();

                break;


            case KeyCode.DownArrow:
                NavigateSelection(
                    Vector2.down
                );

                evt.StopPropagation();

                break;
        }
    }


    private void DeleteSelection()
    {
        if (context.SelectedNode != null)
        {
            context.RemoveNode(
                context.SelectedNode
            );


            return;
        }


        if (context.SelectedConnection != null)
        {
            context.RemoveConnection(
                context.SelectedConnection
            );
        }
    }


    private void NavigateSelection(
        Vector2 requestedDirection
    )
    {
        if (context.Graph == null)
        {
            return;
        }


        if (context.SelectedNode != null)
        {
            NavigateFromNode(
                context.SelectedNode,
                requestedDirection
            );


            return;
        }


        if (context.SelectedConnection != null)
        {
            NavigateFromConnection(
                context.SelectedConnection,
                requestedDirection
            );


            return;
        }


        if (context.Graph.NodeCount >
            0)
        {
            context.SelectNode(
                context.Graph.Nodes[0]
            );
        }
    }


    private void NavigateFromNode(
        LevelNodeData node,
        Vector2 requestedDirection
    )
    {
        LevelConnectionData bestConnection =
            null;


        float bestScore =
            float.NegativeInfinity;


        Vector2 nodeCenter =
            GetNodeCenter(
                node
            );


        for (int i = 0;
             i < context.Graph.Connections.Count;
             i++)
        {
            LevelConnectionData connection =
                context.Graph.Connections[i];


            if (connection == null ||
                !connection.TouchesNode(
                    node.Id
                ))
            {
                continue;
            }


            string otherId =
                connection.FromNodeId ==
                node.Id
                    ? connection.ToNodeId
                    : connection.FromNodeId;


            LevelNodeData other =
                context.Graph.FindNode(
                    otherId
                );


            if (other == null)
            {
                continue;
            }


            Vector2 delta =
                GetNodeCenter(
                    other
                )
                -
                nodeCenter;


            float score =
                GetNavigationScore(
                    delta,
                    requestedDirection
                );


            if (score >
                bestScore)
            {
                bestScore =
                    score;


                bestConnection =
                    connection;
            }
        }


        if (bestConnection != null &&
            bestScore >
            0f)
        {
            context.SelectConnection(
                bestConnection
            );
        }
    }


    private void NavigateFromConnection(
        LevelConnectionData connection,
        Vector2 requestedDirection
    )
    {
        LevelNodeData from =
            context.Graph.FindNode(
                connection.FromNodeId
            );


        LevelNodeData to =
            context.Graph.FindNode(
                connection.ToNodeId
            );


        if (from == null ||
            to == null)
        {
            return;
        }


        Vector2 connectionCenter =
            (
                GetNodeCenter(
                    from
                )
                +
                GetNodeCenter(
                    to
                )
            )
            *
            0.5f;


        float fromScore =
            GetNavigationScore(
                GetNodeCenter(
                    from
                )
                -
                connectionCenter,
                requestedDirection
            );


        float toScore =
            GetNavigationScore(
                GetNodeCenter(
                    to
                )
                -
                connectionCenter,
                requestedDirection
            );


        if (fromScore <=
                0f &&
            toScore <=
                0f)
        {
            return;
        }


        context.SelectNode(
            toScore >
            fromScore
                ? to
                : from
        );
    }


    private static float GetNavigationScore(
        Vector2 delta,
        Vector2 requestedDirection
    )
    {
        if (delta.sqrMagnitude <=
            Mathf.Epsilon)
        {
            return float.NegativeInfinity;
        }


        Vector2 normalized =
            delta.normalized;


        float alignment =
            Vector2.Dot(
                normalized,
                requestedDirection
            );


        if (alignment <=
            0.05f)
        {
            return float.NegativeInfinity;
        }


        /*
         * Prefer items strongly aligned with the requested arrow,
         * then prefer shorter distances.
         */
        return
            alignment *
            100000f
            -
            delta.magnitude;
    }


    // =========================================================
    // Connection Hit Test
    // =========================================================

    private LevelConnectionData
        FindConnectionAtPosition(
            Vector2 pointerPosition
        )
    {
        if (context.Graph == null)
        {
            return null;
        }


        for (int i =
                 context.Graph.Connections.Count - 1;
             i >= 0;
             i--)
        {
            LevelConnectionData connection =
                context.Graph.Connections[i];


            if (connection == null)
            {
                continue;
            }


            if (IsPointNearConnection(
                    pointerPosition,
                    connection
                ))
            {
                return connection;
            }
        }


        return null;
    }


    private bool IsPointNearConnection(
        Vector2 pointerPosition,
        LevelConnectionData connection
    )
    {
        if (!TryGetConnectionCurve(
                connection,
                out Vector2 start,
                out Vector2 controlA,
                out Vector2 controlB,
                out Vector2 end
            ))
        {
            return false;
        }


        Vector2 previous =
            start;


        for (int i = 1;
             i <= ConnectionHitSamples;
             i++)
        {
            float t =
                i /
                (float)
                ConnectionHitSamples;


            Vector2 current =
                EvaluateBezier(
                    start,
                    controlA,
                    controlB,
                    end,
                    t
                );


            float distance =
                DistancePointToSegment(
                    pointerPosition,
                    previous,
                    current
                );


            if (distance <=
                ConnectionHitDistance)
            {
                return true;
            }


            previous =
                current;
        }


        return false;
    }


    private static float
        DistancePointToSegment(
            Vector2 point,
            Vector2 segmentStart,
            Vector2 segmentEnd
        )
    {
        Vector2 segment =
            segmentEnd -
            segmentStart;


        float lengthSquared =
            segment.sqrMagnitude;


        if (lengthSquared <=
            Mathf.Epsilon)
        {
            return Vector2.Distance(
                point,
                segmentStart
            );
        }


        float t =
            Vector2.Dot(
                point -
                segmentStart,
                segment
            ) /
            lengthSquared;


        t =
            Mathf.Clamp01(
                t
            );


        Vector2 closestPoint =
            segmentStart +
            segment *
            t;


        return Vector2.Distance(
            point,
            closestPoint
        );
    }


    // =========================================================
    // Connection Curve
    // =========================================================

    private bool TryGetConnectionCurve(
        LevelConnectionData connection,
        out Vector2 start,
        out Vector2 controlA,
        out Vector2 controlB,
        out Vector2 end
    )
    {
        start =
            Vector2.zero;

        controlA =
            Vector2.zero;

        controlB =
            Vector2.zero;

        end =
            Vector2.zero;


        if (context.Graph == null ||
            connection == null)
        {
            return false;
        }


        LevelNodeData from =
            context.Graph.FindNode(
                connection.FromNodeId
            );


        LevelNodeData to =
            context.Graph.FindNode(
                connection.ToNodeId
            );


        if (from == null ||
            to == null)
        {
            return false;
        }


        Vector2 startWorld =
            from.GraphPosition +
            new Vector2(
                NodeWidth,
                NodeHeight *
                0.5f
            );


        Vector2 endWorld =
            to.GraphPosition +
            new Vector2(
                0f,
                NodeHeight *
                0.5f
            );


        start =
            WorldToScreen(
                startWorld
            );


        end =
            WorldToScreen(
                endWorld
            );


        float distance =
            Mathf.Max(
                60f *
                zoom,
                Mathf.Abs(
                    end.x -
                    start.x
                ) *
                0.45f
            );


        controlA =
            start +
            Vector2.right *
            distance;


        controlB =
            end +
            Vector2.left *
            distance;


        return true;
    }


    private static Vector2 EvaluateBezier(
        Vector2 start,
        Vector2 controlA,
        Vector2 controlB,
        Vector2 end,
        float t
    )
    {
        float oneMinusT =
            1f -
            t;


        float oneMinusTSquared =
            oneMinusT *
            oneMinusT;


        float tSquared =
            t *
            t;


        return
            oneMinusTSquared *
            oneMinusT *
            start
            +
            3f *
            oneMinusTSquared *
            t *
            controlA
            +
            3f *
            oneMinusT *
            tSquared *
            controlB
            +
            tSquared *
            t *
            end;
    }


    // =========================================================
    // Draw
    // =========================================================

    private void DrawGraph(
        MeshGenerationContext mgc
    )
    {
        Painter2D painter =
            mgc.painter2D;


        DrawBackgroundGrid(
            painter
        );


        if (context.Graph == null)
        {
            return;
        }


        for (int i = 0;
             i < context.Graph.Connections.Count;
             i++)
        {
            LevelConnectionData connection =
                context.Graph.Connections[i];


            if (connection == null)
            {
                continue;
            }


            DrawConnection(
                painter,
                connection
            );
        }
    }


    private void DrawBackgroundGrid(
        Painter2D painter
    )
    {
        float width =
            viewport.resolvedStyle.width;

        float height =
            viewport.resolvedStyle.height;


        if (width <=
                0f ||
            height <=
                0f)
        {
            return;
        }


        Vector2 topLeft =
            ScreenToWorld(
                Vector2.zero
            );


        Vector2 bottomRight =
            ScreenToWorld(
                new Vector2(
                    width,
                    height
                )
            );


        float minX =
            Mathf.Min(
                topLeft.x,
                bottomRight.x
            );

        float maxX =
            Mathf.Max(
                topLeft.x,
                bottomRight.x
            );

        float minY =
            Mathf.Min(
                topLeft.y,
                bottomRight.y
            );

        float maxY =
            Mathf.Max(
                topLeft.y,
                bottomRight.y
            );


        float minorStep =
            CalculateNiceGridStep(
                DefaultGridStep /
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
        painter.strokeColor =
            EditorGUIUtility.isProSkin
                ? new Color(
                    1f,
                    1f,
                    1f,
                    major
                        ? 0.075f
                        : 0.03f
                )
                : new Color(
                    0f,
                    0f,
                    0f,
                    major
                        ? 0.10f
                        : 0.04f
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


        float nice;


        if (normalized <=
            1f)
        {
            nice =
                1f;
        }
        else if (normalized <=
                 2f)
        {
            nice =
                2f;
        }
        else if (normalized <=
                 5f)
        {
            nice =
                5f;
        }
        else
        {
            nice =
                10f;
        }


        return
            nice *
            power;
    }


    private void DrawConnection(
        Painter2D painter,
        LevelConnectionData connection
    )
    {
        if (!TryGetConnectionCurve(
                connection,
                out Vector2 start,
                out Vector2 controlA,
                out Vector2 controlB,
                out Vector2 end
            ))
        {
            return;
        }


        bool selected =
            context.SelectedConnection != null &&
            context.SelectedConnection.Id ==
            connection.Id;


        if (selected)
        {
            painter.strokeColor =
                new Color(
                    0.25f,
                    0.60f,
                    1f,
                    1f
                );


            painter.lineWidth =
                Mathf.Max(
                    2f,
                    4f *
                    zoom
                );
        }
        else
        {
            LevelConnectionSocketState state =
                LevelSocketUtility
                    .GetConnectionState(
                        context.Graph,
                        connection,
                        out _
                    );


            painter.strokeColor =
                GetConnectionColor(
                    state
                );


            painter.lineWidth =
                Mathf.Max(
                    1f,
                    (
                        state ==
                        LevelConnectionSocketState.Invalid
                            ? 3f
                            : 2f
                    )
                    *
                    zoom
                );
        }


        painter.BeginPath();


        painter.MoveTo(
            start
        );


        painter.BezierCurveTo(
            controlA,
            controlB,
            end
        );


        painter.Stroke();
    }


    private static Color GetConnectionColor(
        LevelConnectionSocketState state
    )
    {
        switch (state)
        {
            case LevelConnectionSocketState.Valid:
                return new Color(
                    0.35f,
                    0.78f,
                    0.45f,
                    0.95f
                );


            case LevelConnectionSocketState.Partial:
                return new Color(
                    0.95f,
                    0.72f,
                    0.20f,
                    0.95f
                );


            case LevelConnectionSocketState.Invalid:
                return new Color(
                    0.95f,
                    0.28f,
                    0.25f,
                    0.95f
                );


            case LevelConnectionSocketState.Unassigned:
            default:
                return EditorGUIUtility.isProSkin
                    ? new Color(
                        0.75f,
                        0.75f,
                        0.75f,
                        0.65f
                    )
                    : new Color(
                        0.25f,
                        0.25f,
                        0.25f,
                        0.65f
                    );
        }
    }


    // =========================================================
    // Coordinates
    // =========================================================

    private Vector2 GetViewportCenter()
    {
        return new Vector2(
            viewport.resolvedStyle.width *
            0.5f,
            viewport.resolvedStyle.height *
            0.5f
        );
    }


    private Vector2 WorldToScreen(
        Vector2 world
    )
    {
        return
            GetViewportCenter()
            +
            (
                world -
                cameraWorldCenter
            )
            *
            zoom;
    }


    private Vector2 ScreenToWorld(
        Vector2 screen
    )
    {
        return
            cameraWorldCenter
            +
            (
                screen -
                GetViewportCenter()
            )
            /
            zoom;
    }


    private static Vector2 GetNodeCenter(
        LevelNodeData node
    )
    {
        return
            node.GraphPosition
            +
            new Vector2(
                NodeWidth *
                0.5f,
                NodeHeight *
                0.5f
            );
    }


    // =========================================================
    // Node Visual
    // =========================================================

    private sealed class NodeVisual
        : VisualElement
    {
        private readonly LevelGraphUI owner;

        private readonly LevelEditorContext context;


        public LevelNodeData Node
        {
            get;
        }


        private bool dragging;

        private int capturedPointerId =
            -1;

        private Vector2 dragStartPointer;

        private Vector2 dragStartPosition;


        public NodeVisual(
            LevelGraphUI owner,
            LevelEditorContext context,
            LevelNodeData node
        )
        {
            this.owner =
                owner;


            this.context =
                context;


            Node =
                node;


            AddToClassList(
                "level-node"
            );


            string titleText =
                node.Module != null
                    ? node.Module.DisplayName
                    : (
                        node.Room != null
                            ? node.Room.name
                            : "Missing Room"
                    );


            Label title =
                new Label(
                    titleText
                );


            title.AddToClassList(
                "level-node__title"
            );


            int socketCount =
                node.Room?.Sockets?.Count
                ?? 0;


            Label socketInfo =
                new Label(
                    $"{socketCount} socket(s)"
                );


            socketInfo.AddToClassList(
                "level-node__meta"
            );


            string shortId =
                node.Id != null &&
                node.Id.Length >
                8
                    ? node.Id.Substring(
                        0,
                        8
                    )
                    : node.Id;


            Label id =
                new Label(
                    shortId
                );


            id.AddToClassList(
                "level-node__id"
            );


            Add(
                title
            );


            Add(
                socketInfo
            );


            Add(
                id
            );


            RegisterCallback<
                PointerDownEvent
            >(
                OnPointerDown
            );


            RegisterCallback<
                PointerMoveEvent
            >(
                OnPointerMove
            );


            RegisterCallback<
                PointerUpEvent
            >(
                OnPointerUp
            );


            RegisterCallback<
                PointerCancelEvent
            >(
                OnPointerCancel
            );


            RegisterCallback<
                PointerCaptureOutEvent
            >(
                OnPointerCaptureOut
            );
        }


        private void OnPointerDown(
            PointerDownEvent evt
        )
        {
            owner.FocusGraph();


            if (evt.button !=
                0)
            {
                return;
            }


            if (evt.altKey)
            {
                return;
            }


            if (evt.shiftKey &&
                context.SelectedNode != null &&
                context.SelectedNode !=
                Node)
            {
                context.ConnectNodes(
                    context.SelectedNode,
                    Node
                );


                evt.StopPropagation();


                return;
            }


            context.SelectNode(
                Node
            );


            dragging =
                true;


            capturedPointerId =
                evt.pointerId;


            dragStartPointer =
                evt.position;


            dragStartPosition =
                Node.GraphPosition;


            context.BeginNodeMove(
                Node
            );


            PointerCaptureHelper
                .CapturePointer(
                    this,
                    capturedPointerId
                );


            evt.StopPropagation();
        }


        private void OnPointerMove(
            PointerMoveEvent evt
        )
        {
            if (!dragging ||
                evt.pointerId !=
                capturedPointerId)
            {
                return;
            }


            if (!PointerCaptureHelper
                    .HasPointerCapture(
                        this,
                        capturedPointerId
                    ))
            {
                return;
            }


            Vector2 deltaScreen =
                evt.position -
                (Vector3)dragStartPointer;


            Vector2 position =
                dragStartPosition
                +
                deltaScreen /
                owner.Zoom;


            /*
             * No clamping.
             * The graph world is intentionally unbounded.
             */
            context.MoveNodeContinuous(
                Node,
                position
            );


            evt.StopPropagation();
        }


        private void OnPointerUp(
            PointerUpEvent evt
        )
        {
            if (!dragging ||
                evt.pointerId !=
                capturedPointerId)
            {
                return;
            }


            EndDrag();


            evt.StopPropagation();
        }


        private void OnPointerCancel(
            PointerCancelEvent evt
        )
        {
            if (evt.pointerId ==
                capturedPointerId)
            {
                EndDrag();
            }
        }


        private void OnPointerCaptureOut(
            PointerCaptureOutEvent evt
        )
        {
            dragging =
                false;


            capturedPointerId =
                -1;
        }


        private void EndDrag()
        {
            dragging =
                false;


            if (capturedPointerId <
                0)
            {
                return;
            }


            if (PointerCaptureHelper
                    .HasPointerCapture(
                        this,
                        capturedPointerId
                    ))
            {
                PointerCaptureHelper
                    .ReleasePointer(
                        this,
                        capturedPointerId
                    );
            }


            capturedPointerId =
                -1;
        }
    }
}
