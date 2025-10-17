using System;
using System.Collections.Generic;

namespace HpaStarPathfinding.ViewModel
{
    public class Portal
    {
        //only thing needed to know, to save data you can just implement it in your game with the length saved.
        public byte portalLength;
        public Connection[] internalPortalConnections;
        public int[] externalPortalConnections;//only 2 at a time
        
        //debug stuff
        public Vector2D centerPos;//not needed is in hash
        public Vector2D startPos; //not needed is in hash
        public Directions direction; //not needed is in hash
        
        //Future: i can calculate the Connection with the hash, so i dont need to store them but instead just look up if the portal on the otherside is null :)
        
        public Portal(Vector2D startPos, int length, Directions direction) {
            this.startPos = startPos;
            this.portalLength = (byte)length;
            this.direction = direction;
            internalPortalConnections = new Connection[MainWindowViewModel.MaxPortalsInChunk - 1];
            for (int i = 0; i < internalPortalConnections.Length; i++)
            {
                internalPortalConnections[i].portal = Byte.MaxValue;
            }

            externalPortalConnections = new int[3];
            for (int i = 0; i < externalPortalConnections.Length; i++)
            {
                externalPortalConnections[i] = -1;
            }
            centerPos = CalcCenterPos(direction, length, startPos);
        }

        public Vector2D CalcCenterPos(Directions direction, int length, Vector2D startPos)
        {
            int offset = length / 2;
            switch (direction)
            {
                case Directions.N:
                    centerPos = new Vector2D(startPos.x + offset, startPos.y);
                    break;
                case Directions.S:
                    centerPos = new Vector2D(startPos.x - offset + offset % 2, startPos.y);
                    break;
                case Directions.E:
                    centerPos = new Vector2D(startPos.x, startPos.y + offset);
                    break;
                case Directions.W:
                    centerPos = new Vector2D(startPos.x, startPos.y - offset + offset % 2);
                    break;
            }

            return centerPos;
        }
        
        public static int GenerateOppositePortalKey(int portalKey, Directions portalDirection)
        {
            Vector2D posOtherPortal = PortalKeyToWorldPos(portalKey);
            switch (portalDirection)
            {
                case Directions.N:
                    posOtherPortal.y -= MainWindowViewModel.MapSize;
                    break;
                case Directions.E:
                    posOtherPortal.x += 1;
                    break;
                case Directions.S:
                    posOtherPortal.y += MainWindowViewModel.MapSize;
                    break;
                case Directions.W:
                    posOtherPortal.x -= 1;
                    break;
            }
            // int otherKey = posToPortalKey(posOtherPortal, portalDirection + 2);
            //
            // if (oherPortalKey)
            // {
            //     int oherPortalKey = 
            // }
            // int key = position + (int)direction  * MainWindowViewModel.ChunkSize + chunkIndex * MainWindowViewModel.MaxPortalsInChunk;
            return -1;
        }

        private static int posToPortalKey(Vector2D posOtherPortal, Directions portalDirection)
        {
            throw new NotImplementedException();
        }

        public static int GeneratePortalKey(int chunkIndex, int position, Directions direction)
        {
            int key = position + (int)direction  * MainWindowViewModel.ChunkSize + chunkIndex * MainWindowViewModel.MaxPortalsInChunk;
            return key;
        }
        
        public static int CalculateOtherPortalKeyFromConnection(int portalKey, byte connectionPortal)
        {
            int key = portalKey - (portalKey % MainWindowViewModel.MaxPortalsInChunk) + connectionPortal;
            return key;
        }
        
        public static Vector2D PortalKeyToWorldPos(int key)
        {
            int chunkId = key / MainWindowViewModel.MaxPortalsInChunk;
            int dirAndPos = key % MainWindowViewModel.MaxPortalsInChunk;
            Directions dir = (Directions)(dirAndPos / MainWindowViewModel.ChunkSize);
            int pos = dirAndPos % MainWindowViewModel.ChunkSize;
            Vector2D worldPos = new Vector2D(chunkId % MainWindowViewModel.ChunkMapSize * MainWindowViewModel.ChunkSize, chunkId / MainWindowViewModel.ChunkMapSize * MainWindowViewModel.ChunkSize);
            int dummy = MainWindowViewModel.ChunkSize - 1;
            switch (dir)
            {
                case Directions.N:
                    worldPos.x += pos;
                    break;
                case Directions.E:
                    worldPos.x += dummy;
                    worldPos.y += pos;
                    break;
                case Directions.S:
                    worldPos.x = worldPos.x - pos + dummy;
                    worldPos.y += dummy;
                    break;
                case Directions.W:
                    worldPos.y = worldPos.y - pos + dummy;
                    break;
            }
            return worldPos;
        }
    }
}