using System;

namespace HpaStarPathfinding.ViewModel
{
    
    [Flags]
    public enum PortalLength : byte
    {
        Offset = 0b_1111_0000,
        TotalLength = 0b_0000_1111,
        OffsetShift = 4,
    }
    public class Portal
    {
        public Vector2D CenterPos;
        public byte PortalOffsetAndLength;
        public readonly int[] ExternalPortalConnections; //only 2 at a time
        public readonly Connection[] InternalPortalConnections;

        public Portal(Vector2D startPos, int length, Directions direction, int offsetStart, int offsetEnd)
        {
            PortalOffsetAndLength = (byte)length;
            InternalPortalConnections = new Connection[MainWindowViewModel.MaxPortalsInChunk - 1];
            for (var i = 0; i < InternalPortalConnections.Length; i++)
                InternalPortalConnections[i].portal = byte.MaxValue;

            ExternalPortalConnections = new int[5];//diagonal can need 5(portals can be above each other in special cases, only diagonal), straight ones 3
            for (var i = 0; i < ExternalPortalConnections.Length; i++) ExternalPortalConnections[i] = -1;

            CalcCenterPos(direction, startPos, PortalOffsetAndLength, offsetStart, offsetEnd);
        }

        private void CalcCenterPos(Directions direction, Vector2D startPos, int length, int offsetStart,
            int offsetEnd)
        {
            var offset = offsetStart + (length - offsetEnd - offsetStart) / 2;
            PortalOffsetAndLength |= (byte)(offset << (int)PortalLength.OffsetShift);
            switch (direction)
            {
                case Directions.N:
                    CenterPos = new Vector2D(startPos.x + offset, startPos.y);
                    break;
                case Directions.S:
                    CenterPos = new Vector2D(startPos.x + offset, startPos.y);
                    break;
                case Directions.E:
                    CenterPos = new Vector2D(startPos.x, startPos.y + offset);
                    break;
                case Directions.W:
                    CenterPos = new Vector2D(startPos.x, startPos.y + offset);
                    break;
            }
        }
        
        public void ChangeLength(Directions dir,Vector2D portalPos, byte portalSize, int offsetStart, int offsetEnd)
        {
            if (portalSize >= (PortalOffsetAndLength & (int)PortalLength.TotalLength)) return;
            
            PortalOffsetAndLength = portalSize;
            CalcCenterPos(dir, portalPos, PortalOffsetAndLength, offsetStart, offsetEnd);
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