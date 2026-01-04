using HpaStarPathfinding.model.map;
using HpaStarPathfinding.model.math;
using HpaStarPathfinding.ViewModel;

namespace HpaStarPathfinding.model.pathfinding;

[Flags]
public enum PortalLength : byte
{
    //Offset = 0b_1111_1111_0000_0000,
    TotalLength = 0b0000_1111,
    OffsetShift = 4
}
    
public class Portal
{
    public Vector2D CenterPos = null!;
    public byte Offset;
    public byte Length;
    public byte ExternalPortalCount;
    public byte InternalPortalCount;
    public readonly int[] ExternalPortalConnections = new int[4]; //diagonal can need 4(portals can be above each other in special cases, only diagonal), straight ones 3
    public readonly Connection[] InternalPortalConnections = new Connection[MainWindowViewModel.MaxPortalsInChunk - 1];

    private void CalcCenterPos(Vector2D startPos, int length, int offsetStart,
        int offsetEnd, Vector2D steppingInDirVector)
    {
        var offset = offsetStart + (length - offsetEnd - offsetStart) / 2;
        Offset = (byte)offset;
        CenterPos = new Vector2D(startPos.x + offset * steppingInDirVector.x, startPos.y + offset * steppingInDirVector.y);
    }
        
    public void ChangeLength(Vector2D portalPos, byte portalSize, int offsetStart, int offsetEnd, Vector2D steppingInDirVector)
    {
        if (portalSize <= Length) return;
            
        Length = portalSize;
        CalcCenterPos(portalPos, Length, offsetStart, offsetEnd, steppingInDirVector);
    }

    public static int GeneratePortalKey(int chunkIndex, int position, Directions direction)
    {
        var key = position + (int)direction * MainWindowViewModel.ChunkSize +
                  chunkIndex * MainWindowViewModel.MaxPortalsInChunk;
        return key;
    }

    public static int GetPortalKeyFromInternalConnection(int portalKey)
    {
        var key = portalKey - portalKey % MainWindowViewModel.MaxPortalsInChunk;
        return key;
    }

    public void AddExternalConnection(int externalKey)
    {
        ExternalPortalConnections[ExternalPortalCount++] = externalKey;
    }
        
    // public static Vector2D PortalKeyToWorldPos(int key)
    // {
    //     var chunkId = key / MainWindowViewModel.MaxPortalsInChunk;
    //     var dirAndPos = key % MainWindowViewModel.MaxPortalsInChunk;
    //     var dir = (Directions)(dirAndPos / MainWindowViewModel.ChunkSize);
    //     var pos = dirAndPos % MainWindowViewModel.ChunkSize;
    //     var worldPos = new Vector2D(chunkId % MainWindowViewModel.ChunkMapSizeX * MainWindowViewModel.ChunkSize,
    //         chunkId / MainWindowViewModel.ChunkMapSizeX * MainWindowViewModel.ChunkSize);
    //     switch (dir)
    //     {
    //         case Directions.N:
    //             worldPos.x += pos;
    //             break;
    //         case Directions.E:
    //             worldPos.x += MainWindowViewModel.ChunkSize - 1;
    //             worldPos.y += pos;
    //             break;
    //         case Directions.S:
    //             worldPos.x += pos;
    //             worldPos.y += MainWindowViewModel.ChunkSize - 1;
    //             break;
    //         case Directions.W:
    //             worldPos.y += pos;
    //             break;
    //     }
    //
    //     return worldPos;
    // }
}