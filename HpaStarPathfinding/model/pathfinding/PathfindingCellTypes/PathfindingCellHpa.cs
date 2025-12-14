using HpaStarPathfinding.model.map;

namespace HpaStarPathfinding.model.pathfinding.PathfindingCellTypes;

public class PathfindingCellHpa(int portalKey) : PathfindingCell
{
    public PathfindingCellHpa? Parent;
    public readonly int PortalKey = portalKey;
}