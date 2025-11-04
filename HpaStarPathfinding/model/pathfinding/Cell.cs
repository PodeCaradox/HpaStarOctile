using System;
using static HpaStarPathfinding.ViewModel.DirectionsAsByte;

namespace HpaStarPathfinding.ViewModel
{
    public class Cell
    {
        public readonly Vector2D Position;
        //all sides Connections
        public byte Connections; 
        
        public bool Obstacle;

        public Cell(Vector2D pos)
        {
            Obstacle = false;
            Position = pos;
        }

        public void UpdateConnection(Cell[,] map)
        {
            for (byte i = 0; i < DirectionsVector.AllDirections.Length; i++)
            {
                var dirVec = DirectionsVector.AllDirections[i];
                if (Position.y + dirVec.y >= map.GetLength(0) ||
                    Position.x + dirVec.x >= map.GetLength(1) || Position.x + dirVec.x < 0 ||
                    Position.y + dirVec.y < 0)
                {
                    map[Position.y, Position.x].Connections |= (byte)(0b_0000_0001 << i);
                    continue;
                }
                
                ref var otherCell = ref map[Position.y + dirVec.y, Position.x + dirVec.x];
                byte walkable = (byte)(0b_0000_0001 << i);
                byte connection = (byte)(map[Position.y, Position.x].Connections & walkable);
                
                if (connection == WALKABLE)
                {
                    if (otherCell.Obstacle)
                    {
                        Connections |= walkable;
                        continue;
                    }
                    otherCell.Connections &= (byte)~RotateLeft(walkable, 4);
                  
                }
                else
                {
                    otherCell.Connections |= RotateLeft(walkable, 4);
                }
            }
        }


        public override string ToString() => $"[{Position.x},{Position.y}]";

        private static byte RotateLeft(byte value, int count)
        {
            count %= 8; // Ensure count is within 0-7
            return (byte)((value << count) | (value >> (8 - count)));
        }
    }
    
   
}