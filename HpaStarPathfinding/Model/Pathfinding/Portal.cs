using System.Collections.Generic;

namespace HpaStarPathfinding.ViewModel
{
    public class Portal
    {

        public Vector2D startPos;
        public int length;
        public Directions direction;
        public int Connection1;
        public int Connection2;
        public int Connection3;
        
        public Portal(Vector2D startPos, int length, Directions direction) {
            this.startPos = startPos;
            this.length = length;
            this.direction = direction;
        }
        //2 bits direction + 4 bits position + rest chunkindex.
        public int GeneratePortalKey(int chunkIndex, int position, Directions direction)
        {
            int key = position + ((int)direction << 4) + chunkIndex << 6;
            return key;
        }
        
    }
}