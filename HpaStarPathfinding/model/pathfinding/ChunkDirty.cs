using HpaStarPathfinding.model.map;

namespace HpaStarPathfinding.model.pathfinding;

public class ChunkDirty(bool changed)
{
    public byte DirectionsDirty; 
    public bool ChunkHasCellChanges = changed;

    public void SetBit(DirtyDirections dir)
    {
        DirectionsDirty |= (byte)(1 << (int)dir);
    }
}