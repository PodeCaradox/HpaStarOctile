namespace HpaStarPathfinding.model.pathfinding;

public readonly struct Connection(byte portalKey, ushort cost)
{
    public readonly byte portalKey = portalKey;
    public readonly ushort cost = cost;
}
