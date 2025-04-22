using System;
using System.Collections.Generic;

namespace HpaStarPathfinding.ViewModel
{
    public class Portal
    {

        public Vector2D startPos; //not needed is in hash
        public Directions direction; //not needed is in hash
        
        //only thing needed to know, to save data you can just implement it in your game with the length saved.
        public uint length;

        public byte lengthFromCenter;
        public byte outerConnection;//no cost
        
        //public byte innerConnections; //Position + Direction only 6 bits + cost to Path.
        
        //testing stuff
        public Vector2D centerPos;
        
        //i can calculate the Connection with the hash, so i don need to store them but instead just look up if the portal on the otherside is null :)
        //public uint Connection1Hash;
        //public uint Connection2Hash;
        //public uint Connection3Hash;
        
        public Portal(Vector2D startPos, int length, Directions direction) {
            this.startPos = startPos;
            this.length = (uint)length;
            this.direction = direction;
            int offset = (length + length % 2) / 2 - 1;
        
            switch (direction)
            {
                case Directions.N:
                case Directions.S:
                    centerPos = new Vector2D(this.startPos.x + offset, this.startPos.y);
                    break;
                case Directions.E:
                case Directions.W:
                    centerPos = new Vector2D(this.startPos.x, this.startPos.y + offset);
                    break;
            }
        }
        //2 bits direction + 3 bits position + rest chunkindex.
        public int GeneratePortalKey(int chunkIndex, int position, Directions direction)
        {
            int key = position + ((int)direction << 4) + chunkIndex << 6;
            return key;
        }
        
    }
}