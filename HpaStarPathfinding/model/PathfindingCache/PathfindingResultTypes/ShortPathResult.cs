using HpaStarPathfinding.model.math;

namespace HpaStarPathfinding.model.PathfindingCache.PathfindingResultTypes;

public record ShortPathResult(int PathId) : PathfindingResult(PathfindingType.ShortPath); //Vector2D Destination, 