using System;
using UnityEngine.UIElements;

public sealed class PreviewPanelUI : IDisposable
{
    private readonly Toggle livePreviewToggle;
    private readonly Label placeholderLabel;

    public event Action<bool> LivePreviewChanged;
    public event Action RefreshRequested;
    public event Action FrameRequested;

    public VisualElement Viewport { get; }

    public PreviewPanelUI(VisualElement root)
    {
        Viewport = root.Q<VisualElement>("preview-3d-viewport");
        livePreviewToggle = root.Q<Toggle>("live-preview-toggle");
        placeholderLabel = root.Q<Label>("preview-placeholder-label");

        livePreviewToggle.RegisterValueChangedCallback(evt =>
            LivePreviewChanged?.Invoke(evt.newValue));

        root.Q<Button>("refresh-preview-button").clicked +=
            () => RefreshRequested?.Invoke();

        root.Q<Button>("frame-preview-button").clicked +=
            () => FrameRequested?.Invoke();
    }

    public void SetPlaceholderVisible(bool visible)
    {
        placeholderLabel.EnableInClassList("hidden", !visible);
    }

    public void Dispose()
    {
    }
}
