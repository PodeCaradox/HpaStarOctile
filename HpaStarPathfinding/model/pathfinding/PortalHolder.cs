namespace HpaStarPathfinding.model.pathfinding
{
    public class PortalHolder(Vector2D pos, byte key, int arrayIndex)
    {
        public byte Key = key;
        public int ArrayIndex = arrayIndex;
        public Vector2D Pos = pos;

    }
}