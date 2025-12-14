using HpaStarPathfinding.model.math;

namespace HpaStarPathfinding.model.pathfinding;

public class PortalHolder(Vector2D pos, byte key, int arrayIndex)
{
    public readonly byte Key = key;
    public int ArrayIndex = arrayIndex;
    public readonly Vector2D Pos = pos;
}