namespace HpaStarPathfinding.pathfinding.PathfindingCache.PathfindingResultTypes;

public record ShortPathResult(int PathId) : PathfindingResult(PathfindingType.ShortPath); //Vector2D Destination, 