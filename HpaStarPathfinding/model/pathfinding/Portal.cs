using System;

namespace HpaStarPathfinding.ViewModel
{
    
    [Flags]
    public enum PortalLength : byte
    {
        Offset = 0b_1111_0000,
        TotalLength = 0b_0000_1111,
        OffsetShift = 4
    }
    
    [Flags]
    public enum ExternalInternalLength : ushort
    {
        ExternalLength = 0b_1111_1111_0000_0000,
        InternalLength = 0b_0000_0000_1111_1111,
        OffsetExtLength = 8
    }
    
    public class Portal
    {
        public Vector2D CenterPos;
        public byte PortalOffsetAndLength = 0;
        public ushort ExtIntLength = 0;
        public readonly int[] ExternalPortalConnections = new int[5]; //diagonal can need 5(portals can be above each other in special cases, only diagonal), straight ones 3
        public readonly Connection[] InternalPortalConnections = new Connection[MainWindowViewModel.MaxPortalsInChunk - 1];

        private void CalcCenterPos(Vector2D startPos, int length, int offsetStart,
            int offsetEnd, Vector2D steppingInDirVector)
        {
            var offset = offsetStart + (length - offsetEnd - offsetStart) / 2;
            PortalOffsetAndLength |= (byte)(offset << (int)PortalLength.OffsetShift);
            CenterPos = new Vector2D(startPos.x + offset * steppingInDirVector.x, startPos.y+ offset * steppingInDirVector.y);
        }
        
        public void ChangeLength(Vector2D portalPos, byte portalSize, int offsetStart, int offsetEnd, Vector2D steppingInDirVector)
        {
            if (portalSize <= (PortalOffsetAndLength & (int)PortalLength.TotalLength)) return;
            
            PortalOffsetAndLength = portalSize;
            CalcCenterPos(portalPos, PortalOffsetAndLength, offsetStart, offsetEnd, steppingInDirVector);
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

        public void AddExternalConnection(int externalKey)
        {
            int index = ExtIntLength >> (int)ExternalInternalLength.OffsetExtLength;
            ExternalPortalConnections[index] = externalKey;
            ExtIntLength = (ushort)(ExtIntLength + (1 << (int)ExternalInternalLength.OffsetExtLength));
        }
    }
}