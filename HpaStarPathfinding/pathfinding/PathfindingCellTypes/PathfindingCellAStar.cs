using HpaStarPathfinding.model.map;
using HpaStarPathfinding.model.math;

namespace HpaStarPathfinding.pathfinding.PathfindingCellTypes;

public class PathfindingCellAStar(Cell cell) : PathfindingCell
{
    public PathfindingCellAStar? Parent;
    public readonly Vector2D Position = cell.Position;
    public readonly byte Connections = cell.Connections; 
}