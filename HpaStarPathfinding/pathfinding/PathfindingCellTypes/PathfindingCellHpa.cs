namespace HpaStarPathfinding.pathfinding.PathfindingCellTypes;

public class PathfindingCellHpa(int portalKey) : PathfindingCell
{
    public PathfindingCellHpa? Parent;
    public readonly int PortalKey = portalKey;

    //Prepares the pooled node for a new search
    public void Reset(int searchId)
    {
        SearchId = searchId;
        Parent = null;
        GCost = 0;
        HCost = 0;
    }
}
