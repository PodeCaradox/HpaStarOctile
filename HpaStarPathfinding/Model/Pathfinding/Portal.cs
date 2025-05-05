using System;
using System.Collections.Generic;

namespace HpaStarPathfinding.ViewModel
{
    public class Portal
    {

        public Vector2D startPos; //not needed is in hash
        public Directions direction; //not needed is in hash
        
        //only thing needed to know, to save data you can just implement it in your game with the length saved.
        public byte length;
        //public byte lengthFromCenter;
        public Connection[] innerConnections;
        
        //public byte innerConnections; //Position + Direction only 6 bits + cost to Path.
        
        //testing stuff
        public Vector2D centerPos;//not needed is in hash
        
        //i can calculate the Connection with the hash, so i dont need to store them but instead just look up if the portal on the otherside is null :)
        //public uint OuterConnection1Hash;
        //public uint OuterConnection2Hash;
        //public uint OuterConnection3Hash;
        
        public Portal(Vector2D startPos, int length, Directions direction) {
            this.startPos = startPos;
            this.length = (byte)length;
            this.direction = direction;
            this.centerPos = CalcCenterPos(direction, length, startPos);
        }

        public Vector2D CalcCenterPos(Directions direction, int length, Vector2D startPos)
        {
            int offset = length / 2;
            switch (direction)
            {
                case Directions.N:
                case Directions.S:
                    centerPos = new Vector2D(startPos.x + offset, startPos.y);
                    break;
                case Directions.E:
                case Directions.W:
                    centerPos = new Vector2D(startPos.x, startPos.y + offset);
                    break;
            }

            return centerPos;
        }

        //2 bits direction + 4 bits position + rest chunkindex.
        public static int GeneratePortalKey(int chunkIndex, int position, Directions direction)
        {
            int key = position + ((int)direction  * MainWindowViewModel.ChunkSize) + chunkIndex * MainWindowViewModel.MaxPortalsInChunk;
            return key;
        }
        //also es kann 40 Portale in einem chunk geben und danach ist nur interessant wieviele chunks ich habe. Also 40 * Chunks
        
    }
}