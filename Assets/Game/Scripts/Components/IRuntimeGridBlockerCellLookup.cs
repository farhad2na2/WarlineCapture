namespace Game.Components
{
    public interface IRuntimeGridBlockerCellLookup
    {
        bool IsRuntimeBlockerCell(int x, int y, int gridWidth, int gridHeight);
    }
}
