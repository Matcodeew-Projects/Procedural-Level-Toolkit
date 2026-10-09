using System;
using UnityEngine.UIElements;

public sealed class ToolbarUI
{
    private readonly Label roomNameLabel;
    private readonly Label dirtyIndicator;
    private readonly Label validationStateLabel;

    public event Action NewRequested;
    public event Action LoadRequested;
    public event Action SaveRequested;
    public event Action UndoRequested;
    public event Action RedoRequested;
    public event Action ValidateRequested;

    public ToolbarUI(VisualElement root)
    {
        roomNameLabel = root.Q<Label>("room-name-label");
        dirtyIndicator = root.Q<Label>("dirty-indicator");
        validationStateLabel = root.Q<Label>("validation-state-label");

        root.Q<Button>("new-room-button").clicked +=
            () => NewRequested?.Invoke();

        root.Q<Button>("load-room-button").clicked +=
            () => LoadRequested?.Invoke();

        root.Q<Button>("save-room-button").clicked +=
            () => SaveRequested?.Invoke();

        root.Q<Button>("undo-button").clicked +=
            () => UndoRequested?.Invoke();

        root.Q<Button>("redo-button").clicked +=
            () => RedoRequested?.Invoke();

        root.Q<Button>("validate-room-button").clicked +=
            () => ValidateRequested?.Invoke();
    }

    public void SetRoomName(string roomName)
    {
        roomNameLabel.text = string.IsNullOrWhiteSpace(roomName)
            ? "Untitled Room"
            : roomName;
    }

    public void SetDirty(bool dirty)
    {
        dirtyIndicator.EnableInClassList("hidden", !dirty);
    }

    public void SetValidationState(string text)
    {
        validationStateLabel.text = text;
    }
}
