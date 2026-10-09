using System;
using UnityEngine.UIElements;

public static class RoomEditorUIQuery
{
    public static T Require<T>(
        VisualElement root,
        string name)
        where T : VisualElement
    {
        if (root == null)
        {
            throw new ArgumentNullException(
                nameof(root),
                "RoomEditor UI root is null."
            );
        }

        T element =
            root.Q<T>(name);

        if (element != null)
            return element;

        throw new InvalidOperationException(
            $"RoomEditor UI : element '{name}' " +
            $"of type '{typeof(T).Name}' was not found in the loaded UXML."
        );
    }
}