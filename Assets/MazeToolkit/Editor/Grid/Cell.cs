public sealed class Cell
{
    public CellType Type { get; private set; }

    public Cell(CellType type = CellType.Empty)
    {
        Type = type;
    }

    public void SetType(CellType type)
    {
        Type = type;
    }
}