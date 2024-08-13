using System.Collections.Generic;

namespace HpaStarPathfinding.ViewModel
{
    public class Chunk
    {
        public int PosX;
        public int PosY;
        public List<Portal>[] portals = new List<Portal>[Directions.AllDirections.Length];
        public List<int> portalsHashes = new List<int>();//HashValue With 1 Bit Direction, 4 Bits Length, 4 bits posX and 4 bits posY and 10 bits chunkId
        
        public void RebuildPortals()
        {
        
        }
        
        public void RebuildPortalsInDirection()
        {
        
        }
    }
    
}