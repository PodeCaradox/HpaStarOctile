using System;

namespace HpaStarPathfinding.ViewModel
{
    public class Portal
    {
        //debug stuff
        public Vector2D startPos; //not needed is in hash
        public Vector2D centerPos; //not needed is in hash
        public Directions direction; //not needed is in hash
        
        public int[] externalPortalConnections; //only 2 at a time
        public Connection[] internalPortalConnections;

        //only thing needed to know, to save data you can just implement it in your game with the length saved.
        public byte portalLength;

        //Future: i can calculate the Connection with the hash, so i dont need to store them but instead just look up if the portal on the otherside is null :)
        public Portal(Vector2D startPos, int length, Directions direction, int offsetStart, int offsetEnd)
        {
            this.startPos = startPos;
            portalLength = (byte)length;
            this.direction = direction;
            internalPortalConnections = new Connection[MainWindowViewModel.MaxPortalsInChunk - 1];
            for (var i = 0; i < internalPortalConnections.Length; i++)
                internalPortalConnections[i].portal = byte.MaxValue;

            //4 because a portal can have 4 other portals it connects too(diagonals + 1)
            externalPortalConnections = new int[4];
            for (var i = 0; i < externalPortalConnections.Length; i++) externalPortalConnections[i] = -1;

            centerPos = CalcCenterPos(portalLength, offsetStart, offsetEnd);
        }

        private Vector2D CalcCenterPos(int length, int offsetStart,
            int offsetEnd)
        {
            var offset = offsetStart + (length - offsetEnd - offsetStart) / 2;
            switch (direction)
            {
                case Directions.N:
                    centerPos = new Vector2D(startPos.x + offset, startPos.y);
                    break;
                case Directions.S:
                    centerPos = new Vector2D(startPos.x + offset, startPos.y);
                    break;
                case Directions.E:
                    centerPos = new Vector2D(startPos.x, startPos.y + offset);
                    break;
                case Directions.W:
                    centerPos = new Vector2D(startPos.x, startPos.y + offset);
                    break;
            }

            return centerPos;
        }
        
        public void ChangeLength(Vector2D portalPos, byte portalSize, int offsetStart, int offsetEnd)
        {
            if (portalSize >= portalLength) return;
            
            portalLength = portalSize;
            startPos = portalPos;
            centerPos = CalcCenterPos( portalLength, offsetStart, offsetEnd);
            
           
        }

        public static int GeneratePortalKey(int chunkIndex, int position, Directions direction)
        {
            var key = position + (int)direction * MainWindowViewModel.ChunkSize +
                      chunkIndex * MainWindowViewModel.MaxPortalsInChunk;
            return key;
        }

        public static int GetPortalKeyFromInternalConnection(int portalKey, byte connectionPortal)
        {
            var key = portalKey - portalKey % MainWindowViewModel.MaxPortalsInChunk + connectionPortal;
            return key;
        }

        public static Vector2D PortalKeyToWorldPos(int key)
        {
            var chunkId = key / MainWindowViewModel.MaxPortalsInChunk;
            var dirAndPos = key % MainWindowViewModel.MaxPortalsInChunk;
            var dir = (Directions)(dirAndPos / MainWindowViewModel.ChunkSize);
            var pos = dirAndPos % MainWindowViewModel.ChunkSize;
            var worldPos = new Vector2D(chunkId % MainWindowViewModel.ChunkMapSize * MainWindowViewModel.ChunkSize,
                chunkId / MainWindowViewModel.ChunkMapSize * MainWindowViewModel.ChunkSize);
            switch (dir)
            {
                case Directions.N:
                    worldPos.x += pos;
                    break;
                case Directions.E:
                    worldPos.x += MainWindowViewModel.ChunkSize - 1;
                    worldPos.y += pos;
                    break;
                case Directions.S:
                    worldPos.x += pos;
                    worldPos.y += MainWindowViewModel.ChunkSize - 1;
                    break;
                case Directions.W:
                    worldPos.y += pos;
                    break;
            }

            return worldPos;
        }
        
    }
}