namespace HpaStarPathfinding.ViewModel
{
    public class Node
    {
        public Vector2D Position;
        public bool Walkable;
        public Node Parent;
        public float GCost;
        public float HCost;
        public float FCost => GCost + HCost;

        public Node(Vector2D pos, bool walkable)
        {
            Position = pos;
            Walkable = walkable;
        }
    }
}