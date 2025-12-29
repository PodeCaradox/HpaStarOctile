namespace HpaStarPathfinding.pathfinding.PathfindingCache.PathfindingResultTypes;

public record HighLevelPathResult(long PathId) //Vector2D Destination,  int CurrentGoalPortal
    : PathfindingResult(PathfindingType.HighLevelPath);