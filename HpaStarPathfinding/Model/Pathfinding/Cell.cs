namespace HpaStarPathfinding.ViewModel
{
    public class Cell
    {
        public readonly Vector2D Position;
        public bool Walkable;
        public Cell Parent;
        public float GCost;
        public float HCost;
        public float fCost => GCost + HCost;

        public Cell(Vector2D pos, bool walkable = true)
        {
            Position = pos;
            Walkable = walkable;
        }
    }
}