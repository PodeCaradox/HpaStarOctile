namespace HpaStarPathfinding.ViewModel
{
    public class Cell
    {
        public bool Walkable;
        public readonly Vector2D Position;
        //all sides Connections
        public byte Connections; 

        public Cell(Vector2D pos, bool walkable = true)
        {
            Position = pos;
            Walkable = walkable;
        }

        public void UpdateConnection(Cell[,] map)
        {
            if (!Walkable)
            {
                return;
            }
            for (byte i = 0; i < DirectionsVector.AllDirections.Length; i++)
            {
                var dirVec = DirectionsVector.AllDirections[i];
                bool blocked;
                if (Position.y + dirVec.y >= map.GetLength(0) ||
                    Position.x + dirVec.x >= map.GetLength(1) || Position.x + dirVec.x < 0 ||
                    Position.y + dirVec.y < 0)
                {
                    blocked = true;
                }
                else
                {
                    var otherCell = map[Position.y + dirVec.y, Position.x + dirVec.x];
                    blocked = !otherCell.Walkable;
                }

                byte walkable = (byte)(0b_0000_0001 << i);
                if (blocked)
                {
                    Connections |= walkable;
                }
                else
                {
                    Connections &= (byte)~walkable;
                }
            }
        }
        
        public override string ToString() => $"[{Position.x},{Position.y}]";
    }
}