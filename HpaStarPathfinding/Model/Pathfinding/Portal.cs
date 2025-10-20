using System;

namespace HpaStarPathfinding.ViewModel
{
    public class Portal
    {
        //debug stuff
        public Vector2D centerPos; //not needed is in hash
        public Directions direction; //not needed is in hash
        public int[] externalPortalConnections; //only 2 at a time

        public Connection[] internalPortalConnections;

        //only thing needed to know, to save data you can just implement it in your game with the length saved.
        public byte portalLength;
        public Vector2D startPos; //not needed is in hash

        //Future: i can calculate the Connection with the hash, so i dont need to store them but instead just look up if the portal on the otherside is null :)
        public Portal(Vector2D startPos, int length, Directions direction, int offsetStart, int offsetEnd)
        {
            this.startPos = startPos;
            portalLength = (byte)length;
            this.direction = direction;
            internalPortalConnections = new Connection[MainWindowViewModel.MaxPortalsInChunk - 1];
            for (var i = 0; i < internalPortalConnections.Length; i++)
                internalPortalConnections[i].portal = byte.MaxValue;

            //3 because a portal can have 3 other portals it connects too
            externalPortalConnections = new int[3];
            for (var i = 0; i < externalPortalConnections.Length; i++) externalPortalConnections[i] = -1;

            centerPos = CalcCenterPos(direction, length, startPos, offsetStart, offsetEnd);
        }

        public Vector2D CalcCenterPos(Directions direction, int length, Vector2D startPos, int offsetStart,
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

        public static int GetOppositePortalInOtherChunk(int portalKey, Directions dir, int portalPos,
            int otherPortalOffset)
        {
            const int offsetChunkByY = MainWindowViewModel.MaxPortalsInChunk * MainWindowViewModel.ChunkMapSize;
            const int offsetChunkByX = MainWindowViewModel.MaxPortalsInChunk;
            switch (dir)
            {
                case Directions.N:
                    if (IsOppositeDiagonalChunk(portalPos + otherPortalOffset, -1))
                    {
                        //offset inside the chunk the from top left 0 -> 29 to right bottom 
                        return portalKey - offsetChunkByY - offsetChunkByX + MainWindowViewModel.ChunkSize * 3 - 1;
                    }
                    if (IsOppositeDiagonalChunk(portalPos + otherPortalOffset, MainWindowViewModel.ChunkSize)) return -1;
                    return portalKey + MainWindowViewModel.ChunkSize * 2 - offsetChunkByY + otherPortalOffset;
                case Directions.E:
                    if (IsOppositeDiagonalChunk(portalPos + otherPortalOffset, -1))
                    {
                        //offset inside the chunk the from top right is 10 -> 39 to bottom left
                        return portalKey + offsetChunkByX - offsetChunkByY + MainWindowViewModel.ChunkSize * 3 - 1;
                    }
                    if (IsOppositeDiagonalChunk(portalPos + otherPortalOffset, MainWindowViewModel.ChunkSize)) return -1;
                    return portalKey + MainWindowViewModel.ChunkSize * 2 + offsetChunkByX + otherPortalOffset;
                case Directions.S:
                    if (IsOppositeDiagonalChunk(portalPos + otherPortalOffset, MainWindowViewModel.ChunkSize))
                    {
                        //offset inside the chunk the from bottom right is 29 -> 0 to top left
                        return portalKey + offsetChunkByY + offsetChunkByX - (MainWindowViewModel.ChunkSize * 3 - 1);
                    }
                    if (IsOppositeDiagonalChunk(portalPos + otherPortalOffset, -1)) return -1;
                    return portalKey - MainWindowViewModel.ChunkSize * 2 + offsetChunkByY + otherPortalOffset;
                case Directions.W:
                    if (IsOppositeDiagonalChunk(portalPos + otherPortalOffset, MainWindowViewModel.ChunkSize))
                    {
                        //offset inside the chunk the from bottom left is 39 -> 10 to top right
                        return portalKey - offsetChunkByX + offsetChunkByY - (MainWindowViewModel.ChunkSize * 3 - 1);
                    }
                    if (IsOppositeDiagonalChunk(portalPos + otherPortalOffset, -1)) return -1;
                    return portalKey - MainWindowViewModel.ChunkSize * 2 - offsetChunkByX + otherPortalOffset;
            }
            return -1;
        }

        private static bool IsOppositeDiagonalChunk(int chunkPosition, int outsidePosition)
        {
            if (chunkPosition == outsidePosition) return true;
            return false;
        }
    }
}