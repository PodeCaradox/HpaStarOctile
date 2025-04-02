namespace HpaStarPathfinding.ViewModel
{
    public class Cell
    {
        public float fCost => GCost + HCost;
        public float GCost;
        public float HCost;
        public readonly Vector2D Position;
        public bool Walkable;
        public Cell Parent;
        public byte Connections; 

        public Cell(Vector2D pos, bool walkable = true)
        {
            Position = pos;
            Walkable = walkable;
        }
    }
}