using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class PreviewPanelUI
    : IDisposable
{
    private readonly ObjectField settingsField;
    private readonly Toggle autoRefreshToggle;
    private readonly Button refreshButton;
    private readonly Button fitButton;
    private readonly Button buildButton;
    private readonly Label statusLabel;
    private readonly VisualElement previewHost;
    private readonly IMGUIContainer previewContainer;

    private Action<Rect> renderCallback;
    private IVisualElementScheduledItem scheduledRefresh;

    public event Action RefreshRequested;
    public event Action FitRequested;
    public event Action BuildRequested;

    public RoomGenerationSettings GenerationSettings =>
        settingsField.value
        as RoomGenerationSettings;

    public bool AutoRefresh =>
        autoRefreshToggle.value;

    public PreviewPanelUI(
        VisualElement root)
    {
        settingsField =
            RoomEditorUIQuery.Require<ObjectField>(
                root,
                "preview-settings-field"
            );

        autoRefreshToggle =
            RoomEditorUIQuery.Require<Toggle>(
                root,
                "preview-auto-refresh-toggle"
            );

        refreshButton =
            RoomEditorUIQuery.Require<Button>(
                root,
                "preview-refresh-button"
            );

        fitButton =
            RoomEditorUIQuery.Require<Button>(
                root,
                "preview-fit-button"
            );

        buildButton =
            RoomEditorUIQuery.Require<Button>(
                root,
                "preview-build-button"
            );

        statusLabel =
            RoomEditorUIQuery.Require<Label>(
                root,
                "preview-status-label"
            );

        previewHost =
            RoomEditorUIQuery.Require<VisualElement>(
                root,
                "preview-host"
            );

        settingsField.objectType =
            typeof(RoomGenerationSettings);

        settingsField.allowSceneObjects =
            false;

        previewContainer =
            new IMGUIContainer(
                DrawPreviewGUI
            );

        previewContainer.style.flexGrow =
            1f;

        previewContainer.style.width =
            Length.Percent(100f);

        previewContainer.style.height =
            Length.Percent(100f);

        previewHost.Add(
            previewContainer
        );

        settingsField.RegisterValueChangedCallback(
            OnSettingsChanged
        );

        refreshButton.clicked +=
            OnRefreshClicked;

        fitButton.clicked +=
            OnFitClicked;

        buildButton.clicked +=
            OnBuildClicked;

        TryUseSingleGenerationSettings();
    }

    public PreviewPanelUI(
        VisualElement root,
        RoomEditorContext context)
        : this(root)
    {
    }

    public void SetRenderCallback(
        Action<Rect> callback)
    {
        renderCallback = callback;
        MarkPreviewDirty();
    }

    public void SetStatus(
        string value)
    {
        statusLabel.text =
            value ?? string.Empty;
    }

    public void NotifyRoomContentChanged()
    {
        if (!AutoRefresh)
            return;

        scheduledRefresh?.Pause();

        scheduledRefresh =
            previewHost
                .schedule
                .Execute(
                    () =>
                        RefreshRequested?.Invoke()
                )
                .StartingIn(150);
    }

    public void MarkPreviewDirty()
    {
        previewContainer.MarkDirtyRepaint();
    }

    private void DrawPreviewGUI()
    {
        Rect rect =
            new Rect(
                0f,
                0f,
                Mathf.Max(
                    1f,
                    previewContainer.contentRect.width
                ),
                Mathf.Max(
                    1f,
                    previewContainer.contentRect.height
                )
            );

        if (renderCallback == null)
        {
            EditorGUI.HelpBox(
                rect,
                "3D Preview unavailable.",
                MessageType.Info
            );

            return;
        }

        renderCallback.Invoke(
            rect
        );
    }

    private void OnSettingsChanged(
        ChangeEvent<UnityEngine.Object> evt)
    {
        if (AutoRefresh)
        {
            NotifyRoomContentChanged();
        }
        else
        {
            SetStatus(
                "Generation settings changed. Press Refresh."
            );
        }
    }

    private void OnRefreshClicked()
    {
        RefreshRequested?.Invoke();
    }

    private void OnFitClicked()
    {
        FitRequested?.Invoke();
    }

    private void OnBuildClicked()
    {
        BuildRequested?.Invoke();
    }

    private void TryUseSingleGenerationSettings()
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "t:RoomGenerationSettings"
            );

        if (guids.Length != 1)
            return;

        string path =
            AssetDatabase.GUIDToAssetPath(
                guids[0]
            );

        RoomGenerationSettings settings =
            AssetDatabase.LoadAssetAtPath<
                RoomGenerationSettings
            >(
                path
            );

        if (settings == null)
            return;

        settingsField.SetValueWithoutNotify(
            settings
        );
    }

    public void Dispose()
    {
        scheduledRefresh?.Pause();

        settingsField.UnregisterValueChangedCallback(
            OnSettingsChanged
        );

        refreshButton.clicked -=
            OnRefreshClicked;

        fitButton.clicked -=
            OnFitClicked;

        buildButton.clicked -=
            OnBuildClicked;

        renderCallback =
            null;
    }
}
