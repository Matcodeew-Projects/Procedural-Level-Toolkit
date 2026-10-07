using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class LevelPreview3DUI
    : VisualElement
{
    private readonly LevelEditorContext context;


    private LevelPreviewController controller;


    private readonly IMGUIContainer previewContainer;

    private readonly Label statusLabel;


    // =========================================================
    // Drag State
    // =========================================================

    private bool orbiting;

    private bool panning;


    public LevelPreview3DUI(
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


        toolbar.style.height =
            34f;

        toolbar.style.flexShrink =
            0f;

        toolbar.style.flexDirection =
            FlexDirection.Row;

        toolbar.style.alignItems =
            Align.Center;

        toolbar.style.paddingLeft =
            6f;

        toolbar.style.paddingRight =
            6f;


        Button rebuildButton =
            new Button(
                Rebuild
            )
            {
                text =
                    "Refresh"
            };


        Button frameButton =
            new Button(
                Frame
            )
            {
                text =
                    "Fit"
            };


        statusLabel =
            new Label(
                "3D Preview"
            );


        statusLabel.style.marginLeft =
            8f;

        statusLabel.style.opacity =
            0.65f;

        statusLabel.style.whiteSpace =
            WhiteSpace.Normal;


        toolbar.Add(
            rebuildButton
        );


        toolbar.Add(
            frameButton
        );


        toolbar.Add(
            statusLabel
        );


        Add(
            toolbar
        );


        // =====================================================
        // Preview Surface
        // =====================================================

        previewContainer =
            new IMGUIContainer(
                DrawPreviewGUI
            );


        previewContainer.style.flexGrow =
            1;

        previewContainer.style.minWidth =
            0f;

        previewContainer.style.minHeight =
            180f;


        Add(
            previewContainer
        );


        // =====================================================
        // Lifetime
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
    }


    // =========================================================
    // Lifetime
    // =========================================================

    private void OnAttach(
        AttachToPanelEvent evt
    )
    {
        EnsureController();


        context.OnLayoutChanged +=
            Rebuild;


        context.OnLevelChanged +=
            Rebuild;


        Rebuild();
    }


    private void OnDetach(
        DetachFromPanelEvent evt
    )
    {
        context.OnLayoutChanged -=
            Rebuild;


        context.OnLevelChanged -=
            Rebuild;


        if (controller != null)
        {
            controller.Dispose();


            controller =
                null;
        }
    }


    private void EnsureController()
    {
        if (controller != null)
        {
            return;
        }


        controller =
            new LevelPreviewController();
    }


    // =========================================================
    // Actions
    // =========================================================

    public void Rebuild()
    {
        EnsureController();


        controller.Rebuild(
            context.CurrentLevel
        );


        RefreshStatus();


        previewContainer
            .MarkDirtyRepaint();
    }


    public void Frame()
    {
        EnsureController();


        controller.Frame();


        RefreshStatus();


        previewContainer
            .MarkDirtyRepaint();
    }


    private void RefreshStatus()
    {
        if (!string.IsNullOrWhiteSpace(
                controller.LastError
            ))
        {
            statusLabel.text =
                controller.LastError;


            return;
        }


        statusLabel.text =
            $"Instances: {controller.InstanceCount}  •  " +
            "Left: orbit  •  Middle: pan  •  Wheel: zoom";
    }


    // =========================================================
    // IMGUI
    // =========================================================

    private void DrawPreviewGUI()
    {
        EnsureController();


        Rect rect =
            GUILayoutUtility.GetRect(
                64f,
                100000f,
                64f,
                100000f,
                GUILayout.ExpandWidth(
                    true
                ),
                GUILayout.ExpandHeight(
                    true
                )
            );


        HandleInput(
            rect,
            Event.current
        );


        if (Event.current.type !=
            EventType.Repaint)
        {
            return;
        }


        EditorGUI.DrawRect(
            rect,
            EditorGUIUtility.isProSkin
                ? new Color(
                    0.095f,
                    0.105f,
                    0.12f,
                    1f
                )
                : new Color(
                    0.72f,
                    0.74f,
                    0.77f,
                    1f
                )
        );


        controller.Draw(
            rect
        );
    }


    // =========================================================
    // Input
    // =========================================================

    private void HandleInput(
        Rect rect,
        Event current
    )
    {
        if (current == null)
        {
            return;
        }


        bool mouseInside =
            rect.Contains(
                current.mousePosition
            );


        switch (current.type)
        {
            // =================================================
            // Start Orbit / Pan
            // =================================================

            case EventType.MouseDown:
                {
                    if (!mouseInside)
                    {
                        return;
                    }


                    /*
                     * Same controls as RoomEditor:
                     *
                     * Left Mouse   -> Orbit
                     * Middle Mouse -> Pan
                     */

                    if (current.button ==
                        0)
                    {
                        orbiting =
                            true;


                        panning =
                            false;


                        current.Use();


                        return;
                    }


                    if (current.button ==
                        2)
                    {
                        panning =
                            true;


                        orbiting =
                            false;


                        current.Use();
                    }


                    break;
                }


            // =================================================
            // Drag
            // =================================================

            case EventType.MouseDrag:
                {
                    if (orbiting)
                    {
                        controller.Orbit(
                            current.delta
                        );


                        previewContainer
                            .MarkDirtyRepaint();


                        current.Use();


                        return;
                    }


                    if (panning)
                    {
                        controller.Pan(
                            current.delta
                        );


                        previewContainer
                            .MarkDirtyRepaint();


                        current.Use();
                    }


                    break;
                }


            // =================================================
            // Stop
            // =================================================

            case EventType.MouseUp:
                {
                    if (!orbiting &&
                        !panning)
                    {
                        return;
                    }


                    orbiting =
                        false;


                    panning =
                        false;


                    current.Use();


                    break;
                }


            // =================================================
            // Zoom
            // =================================================

            case EventType.ScrollWheel:
                {
                    if (!mouseInside)
                    {
                        return;
                    }


                    controller.Zoom(
                        current.delta.y
                    );


                    previewContainer
                        .MarkDirtyRepaint();


                    current.Use();


                    break;
                }


            // =================================================
            // Safety
            // =================================================

            case EventType.MouseLeaveWindow:
                {
                    orbiting =
                        false;


                    panning =
                        false;


                    break;
                }
        }
    }
}