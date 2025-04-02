using System.Collections.Generic;

namespace HpaStarPathfinding.ViewModel
{
    public class Portal
    {
    //5 bits position + direction -> direction = /10 und position % 10
    //12 bits chunkId           17 Bits = 131071 Einräge
        public Vector2D startPos;
        public int length;
        public Directions direction;
        
        public Portal(Vector2D startPos, int length, Directions direction) {
            this.startPos = startPos;
            this.length = length;
            this.direction = direction;
        }
        
    }
}