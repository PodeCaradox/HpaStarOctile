namespace HpaStarPathfinding.model.pathfinding;

public class CostHolder(byte key, ushort[] cost)
{
    public readonly byte Key = key;
    public readonly ushort[] Cost = cost;
}