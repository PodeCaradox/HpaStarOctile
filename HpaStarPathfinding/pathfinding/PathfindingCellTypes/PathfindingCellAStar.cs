namespace HpaStarPathfinding.pathfinding.PathfindingCellTypes;

public class PathfindingCellAStar(int poolIndex) : PathfindingCell
{
    public PathfindingCellAStar? Parent;
    //Index of the cell in the grid array; x/y are derived from it so pooled nodes stay valid after map resizes
    public readonly int PoolIndex = poolIndex;
    public byte Connections;

    //Prepares the pooled node for a new search and picks up connection changes (e.g. toggled walls)
    public void Reset(int searchId, byte connections)
    {
        SearchId = searchId;
        Connections = connections;
        Parent = null;
        GCost = 0;
        HCost = 0;
    }
}
