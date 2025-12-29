using HpaStarPathfinding.model.map;

namespace HpaStarPathfinding.model.pathfinding;

public class ChunkDirty(List<Directions> directionsDirty, bool changed)
{
    private readonly List<Directions> DirectionsDirty = directionsDirty;
    private readonly bool ChunkHasCellChanges = changed;
}