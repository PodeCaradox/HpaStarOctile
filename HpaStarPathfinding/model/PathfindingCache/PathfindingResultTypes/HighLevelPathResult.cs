using HpaStarPathfinding.model.math;

namespace HpaStarPathfinding.model.PathfindingCache.PathfindingResultTypes;

public record HighLevelPathResult(long PathId) //Vector2D Destination,  int CurrentGoalPortal
    : PathfindingResult(PathfindingType.HighLevelPath);