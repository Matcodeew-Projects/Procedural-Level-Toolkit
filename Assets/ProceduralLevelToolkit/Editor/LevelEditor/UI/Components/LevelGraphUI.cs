using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class LevelGraphUI
    : VisualElement
{
    private const float CanvasWidth =
        2400f;

    private const float CanvasHeight =
        1600f;

    private const float NodeWidth =
        180f;

    private const float NodeHeight =
        72f;

    private const float ConnectionHitDistance =
        10f;

    private const int ConnectionHitSamples =
        32;


    private readonly LevelEditorContext context;

    private readonly ScrollView scrollView;

    private readonly VisualElement canvas;


    private readonly Dictionary<
        string,
        NodeVisual
    > nodeVisuals =
        new Dictionary<string, NodeVisual>();


    public LevelGraphUI(
        LevelEditorContext context
    )
    {
        this.context =
            context;


        AddToClassList(
            "level-panel"
        );


        // =====================================================
        // Toolbar
        // =====================================================

        VisualElement toolbar =
            new VisualElement();


        toolbar.AddToClassList(
            "level-graph-toolbar"
        );


        Label info =
            new Label(
                "Click: select   •   Shift + node: connect   •   Shift + link: delete   •   Drag: move"
            );


        info.style.opacity =
            0.65f;


        toolbar.Add(
            info
        );


        Add(
            toolbar
        );


        // =====================================================
        // Scroll
        // =====================================================

        scrollView =
            new ScrollView(
                ScrollViewMode
                    .VerticalAndHorizontal
            );


        scrollView.AddToClassList(
            "level-graph-scroll"
        );


        canvas =
            new VisualElement();


        canvas.AddToClassList(
            "level-graph-canvas"
        );


        canvas.style.width =
            CanvasWidth;


        canvas.style.height =
            CanvasHeight;


        canvas.generateVisualContent +=
            DrawGraph;


        canvas.RegisterCallback<
            PointerDownEvent
        >(
            OnCanvasPointerDown
        );


        scrollView.Add(
            canvas
        );


        Add(
            scrollView
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
    // Events
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
    // Suggested position
    // =========================================================

    public Vector2 GetSuggestedNodePosition()
    {
        int count =
            context.Graph?.NodeCount
            ?? 0;


        int column =
            count % 4;

        int row =
            count / 4;


        return new Vector2(
            100f +
            column * 240f,

            100f +
            row * 140f
        );
    }


    // =========================================================
    // Refresh
    // =========================================================

    public void Refresh()
    {
        canvas.Clear();

        nodeVisuals.Clear();


        if (context.Graph == null)
        {
            canvas.MarkDirtyRepaint();

            return;
        }


        IReadOnlyList<
            LevelNodeData
        > nodes =
            context.Graph.Nodes;


        for (int i = 0;
             i < nodes.Count;
             i++)
        {
            LevelNodeData node =
                nodes[i];


            if (node == null)
            {
                continue;
            }


            CreateNodeVisual(
                node
            );
        }


        RefreshSelection();

        canvas.MarkDirtyRepaint();
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


        canvas.MarkDirtyRepaint();
    }


    private void RefreshNodePosition(
        LevelNodeData node
    )
    {
        if (node == null)
        {
            return;
        }


        if (!nodeVisuals.TryGetValue(
                node.Id,
                out NodeVisual visual
            ))
        {
            return;
        }


        visual.style.left =
            node.GraphPosition.x;


        visual.style.top =
            node.GraphPosition.y;


        canvas.MarkDirtyRepaint();
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
                context,
                node
            );


        visual.style.left =
            node.GraphPosition.x;


        visual.style.top =
            node.GraphPosition.y;


        canvas.Add(
            visual
        );


        nodeVisuals[node.Id] =
            visual;
    }


    // =========================================================
    // Canvas Input
    // =========================================================

    private void OnCanvasPointerDown(
        PointerDownEvent evt
    )
    {
        if (evt.button != 0)
        {
            return;
        }


        if (evt.target !=
            canvas)
        {
            return;
        }


        Vector2 localPosition =
            canvas.WorldToLocal(
                evt.position
            );


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


        IReadOnlyList<
            LevelConnectionData
        > connections =
            context.Graph.Connections;


        for (int i =
                 connections.Count - 1;
             i >= 0;
             i--)
        {
            LevelConnectionData connection =
                connections[i];


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
            segment * t;


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


        start =
            from.GraphPosition +
            new Vector2(
                NodeWidth,
                NodeHeight * 0.5f
            );


        end =
            to.GraphPosition +
            new Vector2(
                0f,
                NodeHeight * 0.5f
            );


        float distance =
            Mathf.Max(
                60f,
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


        IReadOnlyList<
            LevelConnectionData
        > connections =
            context.Graph.Connections;


        for (int i = 0;
             i < connections.Count;
             i++)
        {
            LevelConnectionData connection =
                connections[i];


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
        painter.strokeColor =
            EditorGUIUtility.isProSkin
                ? new Color(
                    1f,
                    1f,
                    1f,
                    0.055f
                )
                : new Color(
                    0f,
                    0f,
                    0f,
                    0.08f
                );


        painter.lineWidth =
            1f;


        const float grid =
            40f;


        painter.BeginPath();


        for (float x = 0f;
             x <= CanvasWidth;
             x += grid)
        {
            painter.MoveTo(
                new Vector2(
                    x,
                    0f
                )
            );


            painter.LineTo(
                new Vector2(
                    x,
                    CanvasHeight
                )
            );
        }


        for (float y = 0f;
             y <= CanvasHeight;
             y += grid)
        {
            painter.MoveTo(
                new Vector2(
                    0f,
                    y
                )
            );


            painter.LineTo(
                new Vector2(
                    CanvasWidth,
                    y
                )
            );
        }


        painter.Stroke();
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
                4f;
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
                state ==
                LevelConnectionSocketState.Invalid
                    ? 3f
                    : 2f;
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
    // Node Visual
    // =========================================================

    private sealed class NodeVisual
        : VisualElement
    {
        private readonly LevelEditorContext context;

        private readonly LevelNodeData node;


        private bool dragging;

        private int capturedPointerId =
            -1;

        private Vector2 dragStartPointer;

        private Vector2 dragStartPosition;


        public NodeVisual(
            LevelEditorContext context,
            LevelNodeData node
        )
        {
            this.context =
                context;

            this.node =
                node;


            AddToClassList(
                "level-node"
            );


            Label title =
                new Label(
                    node.Room != null
                        ? node.Room.name
                        : "Missing Room"
                );


            title.AddToClassList(
                "level-node__title"
            );


            string shortId =
                node.Id != null &&
                node.Id.Length > 8
                    ? node.Id.Substring(
                        0,
                        8
                    )
                    : node.Id;


            int socketCount =
                node.Room?.Sockets?.Count
                ?? 0;


            Label socketInfo =
                new Label(
                    $"{socketCount} socket(s)"
                );


            socketInfo.style.opacity =
                0.65f;


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


        // =====================================================
        // Pointer Down
        // =====================================================

        private void OnPointerDown(
            PointerDownEvent evt
        )
        {
            if (evt.button != 0)
            {
                return;
            }


            if (evt.shiftKey &&
                context.SelectedNode != null &&
                context.SelectedNode !=
                node)
            {
                context.ConnectNodes(
                    context.SelectedNode,
                    node
                );


                evt.StopPropagation();

                return;
            }


            context.SelectNode(
                node
            );


            dragging =
                true;


            capturedPointerId =
                evt.pointerId;


            dragStartPointer =
                evt.position;


            dragStartPosition =
                node.GraphPosition;


            context.BeginNodeMove(
                node
            );


            PointerCaptureHelper
                .CapturePointer(
                    this,
                    capturedPointerId
                );


            evt.StopPropagation();
        }


        // =====================================================
        // Pointer Move
        // =====================================================

        private void OnPointerMove(
            PointerMoveEvent evt
        )
        {
            if (!dragging)
            {
                return;
            }


            if (evt.pointerId !=
                capturedPointerId)
            {
                return;
            }


            bool hasCapture =
                PointerCaptureHelper
                    .HasPointerCapture(
                        this,
                        capturedPointerId
                    );


            if (!hasCapture)
            {
                return;
            }


            Vector2 delta =
                evt.position -
                (Vector3)dragStartPointer;


            Vector2 position =
                dragStartPosition +
                delta;


            position.x =
                Mathf.Max(
                    0f,
                    position.x
                );


            position.y =
                Mathf.Max(
                    0f,
                    position.y
                );


            context.MoveNodeContinuous(
                node,
                position
            );


            evt.StopPropagation();
        }


        // =====================================================
        // Pointer Up / Cancel
        // =====================================================

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
            if (evt.pointerId !=
                capturedPointerId)
            {
                return;
            }


            EndDrag();
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


        // =====================================================
        // End Drag
        // =====================================================

        private void EndDrag()
        {
            dragging =
                false;


            if (capturedPointerId <
                0)
            {
                return;
            }


            bool hasCapture =
                PointerCaptureHelper
                    .HasPointerCapture(
                        this,
                        capturedPointerId
                    );


            if (hasCapture)
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
