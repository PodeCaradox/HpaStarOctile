namespace HpaStarPathfinding.model.pathfinding.PathfindingCellTypes;

public abstract class PathfindingCell
{
    public int FCost;
    public int GCost;
    public int HCost;
    public int QueueIndex;
}