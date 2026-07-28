namespace HpaStarPathfinding.pathfinding.PathfindingCellTypes;

public abstract class PathfindingCell
{
    public int FCost;
    public int GCost;
    public int HCost;
    public int QueueIndex;
    //Id of the search this node was last touched by, so pooled nodes can be reused without clearing them
    public int SearchId;
}
