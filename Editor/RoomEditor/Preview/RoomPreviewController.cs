using System;
using UnityEditor;
using UnityEngine;

public sealed class RoomPreviewController
    : IDisposable
{
    private readonly RoomEditorContext context;
    private readonly PreviewPanelUI ui;

    private PreviewRenderUtility previewUtility;
    private RoomGenerationResult generationResult;

    private Vector3 target;
    private Vector3 panOffset;

    private float yaw = 45f;
    private float pitch = 35f;
    private float distance = 10f;

    public RoomPreviewController(
        RoomEditorContext context,
        PreviewPanelUI ui)
    {
        this.context =
            context
            ?? throw new ArgumentNullException(
                nameof(context)
            );

        this.ui =
            ui
            ?? throw new ArgumentNullException(
                nameof(ui)
            );

        ui.SetRenderCallback(
            DrawPreview
        );

        ui.RefreshRequested +=
            RefreshPreview;

        ui.FitRequested +=
            FitToRoom;

        ui.BuildRequested +=
            OpenBuildWindow;

        RefreshPreview();
    }

    public void RefreshPreview()
    {
        ClearPreview();

        RoomDefinition room =
            context.CurrentRoom;

        if (room == null)
        {
            ui.SetStatus(
                "Create or load a Room to preview it."
            );

            ui.MarkPreviewDirty();
            return;
        }

        RoomGenerationSettings settings =
            ui.GenerationSettings;

        if (settings == null)
        {
            ui.SetStatus(
                "Select RoomGenerationSettings."
            );

            ui.MarkPreviewDirty();
            return;
        }

        try
        {
            CreatePreviewUtility();

            RoomGenerator generator =
                new RoomGenerator();

            generationResult =
                generator.Generate(
                    room,
                    settings
                );

            previewUtility.AddSingleGO(
                generationResult.Root
            );

            FitToRoom();

            ui.SetStatus(
                generationResult.HasWarnings
                    ? $"Preview generated with {generationResult.Warnings.Count} warning(s)."
                    : "Left drag: orbit • Middle drag: pan • Wheel: zoom"
            );
        }
        catch (Exception exception)
        {
            Debug.LogException(
                exception
            );

            ui.SetStatus(
                $"Preview failed: {exception.Message}"
            );

            ClearPreview();
        }

        ui.MarkPreviewDirty();
    }

    private void CreatePreviewUtility()
    {
        previewUtility =
            new PreviewRenderUtility();

        previewUtility.camera.clearFlags =
            CameraClearFlags.Color;

        previewUtility.camera.backgroundColor =
            EditorGUIUtility.isProSkin
                ? new Color(
                    0.105f,
                    0.11f,
                    0.125f,
                    1f
                )
                : new Color(
                    0.72f,
                    0.73f,
                    0.75f,
                    1f
                );

        previewUtility.camera.fieldOfView =
            30f;

        previewUtility.camera.nearClipPlane =
            0.01f;

        previewUtility.camera.farClipPlane =
            10000f;

        if (
            previewUtility.lights != null &&
            previewUtility.lights.Length > 0)
        {
            previewUtility.lights[0].intensity =
                1.2f;

            previewUtility.lights[0]
                .transform
                .rotation =
                    Quaternion.Euler(
                        50f,
                        -30f,
                        0f
                    );
        }

        if (
            previewUtility.lights != null &&
            previewUtility.lights.Length > 1)
        {
            previewUtility.lights[1].intensity =
                0.6f;

            previewUtility.lights[1]
                .transform
                .rotation =
                    Quaternion.Euler(
                        340f,
                        140f,
                        0f
                    );
        }

        previewUtility.ambientColor =
            new Color(
                0.35f,
                0.35f,
                0.35f,
                1f
            );
    }

    private void DrawPreview(
        Rect rect)
    {
        if (
            previewUtility == null ||
            generationResult?.Root == null)
        {
            EditorGUI.DrawRect(
                rect,
                EditorGUIUtility.isProSkin
                    ? new Color(
                        0.105f,
                        0.11f,
                        0.125f,
                        1f
                    )
                    : new Color(
                        0.72f,
                        0.73f,
                        0.75f,
                        1f
                    )
            );

            return;
        }

        HandleInput(
            rect
        );

        UpdateCamera();

        previewUtility.BeginPreview(
            rect,
            GUIStyle.none
        );

        previewUtility.camera.Render();

        Texture texture =
            previewUtility.EndPreview();

        if (texture != null)
        {
            GUI.DrawTexture(
                rect,
                texture,
                ScaleMode.StretchToFill,
                false
            );
        }
    }

    private void UpdateCamera()
    {
        if (previewUtility == null)
            return;

        Quaternion rotation =
            Quaternion.Euler(
                pitch,
                yaw,
                0f
            );

        Vector3 pivot =
            target +
            panOffset;

        Vector3 forward =
            rotation *
            Vector3.forward;

        previewUtility.camera
            .transform
            .position =
                pivot -
                forward *
                distance;

        previewUtility.camera
            .transform
            .rotation =
                rotation;
    }

    private void HandleInput(
        Rect rect)
    {
        Event current =
            Event.current;

        if (
            current == null ||
            !rect.Contains(
                current.mousePosition
            ))
        {
            return;
        }

        if (
            current.type ==
            EventType.MouseDrag)
        {
            if (current.button == 0)
            {
                yaw +=
                    current.delta.x *
                    0.6f;

                /*
                 * Vertical orbit intentionally inverted:
                 * mouse down -> camera looks upward,
                 * mouse up   -> camera looks downward.
                 */
                pitch +=
                    current.delta.y *
                    0.6f;

                pitch =
                    Mathf.Clamp(
                        pitch,
                        -85f,
                        85f
                    );

                current.Use();

                ui.MarkPreviewDirty();
                return;
            }

            if (current.button == 2)
            {
                float scale =
                    Mathf.Max(
                        0.001f,
                        distance *
                        0.0025f
                    );

                panOffset -=
                    previewUtility
                        .camera
                        .transform
                        .right
                    *
                    current.delta.x
                    *
                    scale;

                panOffset +=
                    previewUtility
                        .camera
                        .transform
                        .up
                    *
                    current.delta.y
                    *
                    scale;

                current.Use();

                ui.MarkPreviewDirty();
                return;
            }
        }

        if (
            current.type ==
            EventType.ScrollWheel)
        {
            float zoomFactor =
                1f +
                current.delta.y *
                0.06f;

            zoomFactor =
                Mathf.Max(
                    0.1f,
                    zoomFactor
                );

            distance *=
                zoomFactor;

            distance =
                Mathf.Clamp(
                    distance,
                    0.05f,
                    10000f
                );

            current.Use();

            ui.MarkPreviewDirty();
        }
    }

    private void FitToRoom()
    {
        if (
            generationResult?.Root ==
            null)
        {
            return;
        }

        Renderer[] renderers =
            generationResult.Root
                .GetComponentsInChildren<
                    Renderer
                >(
                    true
                );

        if (
            renderers == null ||
            renderers.Length == 0)
        {
            target =
                Vector3.zero;

            panOffset =
                Vector3.zero;

            distance =
                Mathf.Max(
                    5f,
                    Mathf.Max(
                        context.CurrentRoom?.Width ?? 1,
                        context.CurrentRoom?.Height ?? 1
                    )
                );

            ui.MarkPreviewDirty();
            return;
        }

        Bounds bounds =
            renderers[0].bounds;

        for (
            int i = 1;
            i < renderers.Length;
            i++)
        {
            bounds.Encapsulate(
                renderers[i].bounds
            );
        }

        target =
            bounds.center;

        panOffset =
            Vector3.zero;

        float radius =
            Mathf.Max(
                0.5f,
                bounds.extents.magnitude
            );

        float halfFovRadians =
            Mathf.Deg2Rad *
            previewUtility
                .camera
                .fieldOfView *
            0.5f;

        distance =
            radius /
            Mathf.Tan(
                halfFovRadians
            );

        distance *=
            1.25f;

        ui.MarkPreviewDirty();
    }

    private void OpenBuildWindow()
    {
        if (
            context.CurrentRoom ==
            null)
        {
            ui.SetStatus(
                "Create or load a Room before building."
            );

            return;
        }

        RoomPrefabBuildWindow.Open(
            context.CurrentRoom
        );
    }

    private void ClearPreview()
    {
        generationResult =
            null;

        if (previewUtility == null)
            return;

        previewUtility.Cleanup();

        previewUtility =
            null;
    }

    public void Dispose()
    {
        ui.RefreshRequested -=
            RefreshPreview;

        ui.FitRequested -=
            FitToRoom;

        ui.BuildRequested -=
            OpenBuildWindow;

        ui.SetRenderCallback(
            null
        );

        ClearPreview();
    }
}
