namespace HpaStarPathfinding.model.pathfinding;

public class ChunkDirty(bool changed)
{
    public readonly bool[] DirectionDirty= [false, false, false, false];
    public bool ChunkHasCellChanges = changed;
}