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


    private bool dragging;

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
                    "Rebuild Preview"
            };


        Button frameButton =
            new Button(
                Frame
            )
            {
                text =
                    "Frame"
            };


        statusLabel =
            new Label(
                "3D Preview"
            );


        statusLabel.style.marginLeft =
            8f;

        statusLabel.style.opacity =
            0.75f;

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
        if (controller ==
            null)
        {
            controller =
                new LevelPreviewController();
        }
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
            "Left drag: orbit  •  " +
            "Middle/Alt+Left: pan  •  Wheel: zoom";
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
        Event evt
    )
    {
        if (evt ==
            null)
        {
            return;
        }


        bool inside =
            rect.Contains(
                evt.mousePosition
            );


        switch (evt.type)
        {
            case EventType.MouseDown:
                {
                    if (!inside)
                    {
                        return;
                    }


                    bool wantsOrbit =
                        evt.button ==
                        0 &&
                        !evt.alt;


                    bool wantsPan =
                        evt.button ==
                        2
                        ||
                        (
                            evt.button ==
                            0 &&
                            evt.alt
                        );


                    if (!wantsOrbit &&
                        !wantsPan)
                    {
                        return;
                    }


                    dragging =
                        true;


                    panning =
                        wantsPan;


                    evt.Use();


                    break;
                }


            case EventType.MouseDrag:
                {
                    if (!dragging)
                    {
                        return;
                    }


                    if (panning)
                    {
                        controller.Pan(
                            evt.delta
                        );
                    }
                    else
                    {
                        controller.Orbit(
                            evt.delta
                        );
                    }


                    previewContainer
                        .MarkDirtyRepaint();


                    evt.Use();


                    break;
                }


            case EventType.MouseUp:
                {
                    if (!dragging)
                    {
                        return;
                    }


                    dragging =
                        false;


                    panning =
                        false;


                    evt.Use();


                    break;
                }


            case EventType.ScrollWheel:
                {
                    if (!inside)
                    {
                        return;
                    }


                    controller.Zoom(
                        evt.delta.y
                    );


                    previewContainer
                        .MarkDirtyRepaint();


                    evt.Use();


                    break;
                }


            case EventType.MouseLeaveWindow:
                {
                    dragging =
                        false;


                    panning =
                        false;


                    break;
                }
        }
    }
}