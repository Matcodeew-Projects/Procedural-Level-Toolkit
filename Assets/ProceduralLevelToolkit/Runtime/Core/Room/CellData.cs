using System;

[Serializable]
public sealed class CellData
{
    [UnityEngine.SerializeField]
    private CellTypeDefinition type;

    [UnityEngine.SerializeField]
    private bool enabled = true;

    public CellTypeDefinition Type => type;
    public bool Enabled => enabled;

    public CellData()
    {
    }

    public CellData(
        CellTypeDefinition type,
        bool enabled = true)
    {
        this.type = type;
        this.enabled = enabled;
    }

    public void SetType(CellTypeDefinition value)
    {
        type = value;
    }

    public void SetEnabled(bool value)
    {
        enabled = value;
    }

    public CellData Clone()
    {
        return new CellData(
            type,
            enabled
        );
    }
}
