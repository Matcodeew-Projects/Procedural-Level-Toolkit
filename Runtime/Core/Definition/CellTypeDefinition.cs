using UnityEngine;

[CreateAssetMenu(
    fileName = "CellType",
    menuName = "Procedural Level Toolkit/Room/Cell Type"
)]
public sealed class CellTypeDefinition : ScriptableObject
{
    [SerializeField]
    private string displayName = "Cell Type";

    [SerializeField]
    private Color editorColor = new(0.45f, 0.45f, 0.45f, 1f);

    public string DisplayName =>
        string.IsNullOrWhiteSpace(displayName)
            ? name
            : displayName;

    public Color EditorColor => editorColor;
}
