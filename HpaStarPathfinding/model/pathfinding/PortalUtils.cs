using HpaStarPathfinding.model.map;
using HpaStarPathfinding.model.math;
using HpaStarPathfinding.pathfinding;

using static HpaStarPathfinding.model.map.DirectionsAsByte;
using static HpaStarPathfinding.ViewModel.MainWindowViewModel;
namespace HpaStarPathfinding.model.pathfinding;

public static class PortalUtils
{

    private static int OffsetChunkByY = MaxPortalsInChunk * ChunkMapSizeX;
    private static int OffsetChunkByX = MaxPortalsInChunk;
    private static int[] OppositePortalKeyOffsets = []; 
    private static int[] DiagonalPortalKeyOffsets = []; 
    private static int[] DiagonalSpecialPortalKeyOffsets = [];
    private static Vector2D[] StartCellOffset = [];
    private static int[] PortalDiagonalPosOffset = []; 
    
    private static readonly Vector2D[] DirectionsVectorArray = [DirectionsVector.N, DirectionsVector.E, DirectionsVector.S, DirectionsVector.W]; 
    private static readonly Vector2D[] SteppingInDirVectorArray = [DirectionsVector.E, DirectionsVector.S, DirectionsVector.E, DirectionsVector.S];
    private static readonly byte[][] DirToCheckStraight = [
        [NW | N | NE, N, E | NE, NW, W, NE, E | SE, E, S],
        [NE | E | SE, E, S | SE, NE, N, SE, S | SW, S, W],
        [SW | S | SE, S, E | SE, SW, W, SE, E | NE, E, N],
        [NW | W | SW, W, S | SW, NW, N, SW, S | SE, S, E]
    ];
    private static readonly byte[][] DirToCheckDiagonal = [
        [NW, N, W, NE, E],
        [NE, E, N, SE, S],
        [SE, S, E, SW, W],
        [SW, W, S, NW, N]
    ];
    
    public static void InitPortalUtilsValues()
    {

        OffsetChunkByY = MaxPortalsInChunk * ChunkMapSizeX;
        OffsetChunkByX = MaxPortalsInChunk;
        OppositePortalKeyOffsets =
        [
            -OffsetChunkByY + ChunkSize * 2, //North
            OffsetChunkByX + ChunkSize * 2, //EAST
            OffsetChunkByY - ChunkSize * 2, //SOUTH
            -OffsetChunkByX - ChunkSize * 2 //WEST
        ];
            
        DiagonalSpecialPortalKeyOffsets =
        [
            -OffsetChunkByY + ChunkSize * 4 - 1, //North
            OffsetChunkByX - ChunkSize, //EAST
            OffsetChunkByY - (ChunkSize * 2 - 1), //SOUTH
            -OffsetChunkByX - ChunkSize //WEST
        ];
            
        DiagonalPortalKeyOffsets =
        [
            -OffsetChunkByY - OffsetChunkByX + ChunkSize * 3 - 1, //North //offset inside the chunk from top left 0 -> 29 to right bottom 
            -OffsetChunkByY + OffsetChunkByX + ChunkSize * 3 - 1, //EAST //offset inside the chunk from top right is 10 -> 39 to bottom left
            OffsetChunkByY + OffsetChunkByX - (ChunkSize * 3 - 1), //SOUTH //offset inside the chunk from bottom right is 29 -> 0 to top left
            OffsetChunkByY - OffsetChunkByX - (ChunkSize * 3 - 1) //WEST //offset inside the chunk from bottom left is 39 -> 10 to top right
        ];
        
        StartCellOffset = [new Vector2D(0,0), new Vector2D(ChunkSize - 1,0), new Vector2D(0,ChunkSize - 1), new Vector2D(0,0)];
        PortalDiagonalPosOffset = [0, 0, ChunkSize - 1, ChunkSize - 1]; 
    }
    
    public static void ConnectInternalPortalsInDiagonalDir(Cell[] cells, ref Portal?[] portals, int chunkId, byte portalId)
    {
        List<byte> portalHolders = [];
        int firstPortalKey = GetAllPortalsInChunkAndFirstPortalKey(portals, chunkId, portalHolders);
        int startIndex = 0;
        List<CostHolder> costs = GetCostsForNewPortals(cells, portals, portalHolders, portalId, ref startIndex, portalId, firstPortalKey);
        UpdateConnectionForUnchangedPortals(cells,portals, 0, startIndex, portalHolders, firstPortalKey, portalId, portalId, costs);
        CreateConnectionForNewPortals(portals, costs, firstPortalKey, portalHolders);
        UpdateConnectionForUnchangedPortals(cells, portals, startIndex + costs.Count, portalHolders.Count, portalHolders, firstPortalKey, portalId, portalId, costs);
    }
    
    public static void DisconnectInternalPortalsInDiagonalDir(Cell[] cells, ref Portal?[] portals, int chunkId, byte portalId)
    {
        List<byte> portalHolders = [];
        int firstPortalKey = GetAllPortalsInChunkAndFirstPortalKey(portals, chunkId, portalHolders);
        UpdateConnectionForUnchangedPortals(cells,portals, 0, 0, portalHolders, firstPortalKey, portalId, portalId, []);
        CreateConnectionForNewPortals(portals, [], firstPortalKey, portalHolders);
        UpdateConnectionForUnchangedPortals(cells, portals, 0, portalHolders.Count, portalHolders, firstPortalKey, portalId, portalId, []);
    }
    
    public static void ConnectInternalPortalsInDir(Cell[] cells, ref Portal?[] portals, Directions direction, int chunkId)
    {
        List<byte> portalHolders = [];
        int firstPortalKey = GetAllPortalsInChunkAndFirstPortalKey(portals, chunkId, portalHolders);
        byte start = (byte)((byte)direction * ChunkSize);
        byte end = (byte)(start + ChunkSize - 1);
        int startIndex = 0;
        List<CostHolder> costs = GetCostsForNewPortals(cells, portals, portalHolders, start, ref startIndex, end, firstPortalKey);
        UpdateConnectionForUnchangedPortals(cells,portals, 0, startIndex, portalHolders, firstPortalKey, start, end, costs);
        CreateConnectionForNewPortals(portals, costs, firstPortalKey, portalHolders);
        UpdateConnectionForUnchangedPortals(cells, portals, startIndex + costs.Count, portalHolders.Count, portalHolders, firstPortalKey, start, end, costs);
    }

    private static List<CostHolder> GetCostsForNewPortals(Cell[] cells, Portal?[] portals, List<byte> portalHolders, 
        int start, ref int startIndex, int end, int firstPortalKey)
    {
        List<CostHolder> costs = [];
        foreach (var portalKey in portalHolders)
        {
            if (portalKey < start)
            {
                startIndex++;
                continue;
            }

            if (portalKey > end)
            {
                break;
            }

            var portal = portals[firstPortalKey + portalKey]!;
            costs.Add(new CostHolder(portalKey, BFS.BfsFromStartPosWithRegionFill(cells, portal.CenterPos, portalKey)));
        }

        return costs;
    }

    private static void CreateConnectionForNewPortals(Portal?[] portals, List<CostHolder> costs, int firstPortalKey, List<byte> portalKeys)
    {
        foreach (var costHolder in costs)
        {
            int portalKey = firstPortalKey + costHolder.Key;
            ref var portal = ref portals[portalKey]!;
            int counter = 0;
            foreach (var intPortalKey in portalKeys)
            {
                if(intPortalKey == costHolder.Key) continue;
                int otherPortalKey = firstPortalKey + intPortalKey;
                var cost = BFS.GetCostForPath(costHolder.Cost, portals[otherPortalKey]!.CenterPos);
                if(cost == ushort.MaxValue) continue;
                portal.InternalPortalConnections[counter++] = new Connection(intPortalKey, cost);
            }
            portal.InternalPortalCount = (byte)counter;
        }
    }

    private static void UpdateConnectionForUnchangedPortals(Cell[] cells, Portal?[] portals, int startIndex, int endIndex, List<byte> portalHolders, int firstPortalKey,
        byte start, byte end, List<CostHolder> costs)
    {
        for (int i = startIndex; i < endIndex; i++)
        {
            var intPortalKey = portalHolders[i];
            int portalKey = firstPortalKey + intPortalKey;
            var portal = portals[portalKey]!;
            
            CheckRegion(cells, portal, intPortalKey);
            var oldConnections = (Connection[])portal.InternalPortalConnections.Clone();
            int counter = 0;

            for (int j = 0; j < portal.InternalPortalCount; j++)
            {
                if (portal.InternalPortalConnections[j].portalKey >= start)
                    break;
                portal.InternalPortalConnections[counter++] = portal.InternalPortalConnections[j];
            }
            
            int counterOldList;
            for (counterOldList = counter; counterOldList < portal.InternalPortalCount; counterOldList++)
            {
                if (oldConnections[counterOldList].portalKey > end)
                    break;
            }

            foreach (var costHolder in costs)
            {
                var cost = BFS.GetCostForPath(costHolder.Cost, portal.CenterPos);
                if(cost == ushort.MaxValue) continue;
                portal.InternalPortalConnections[counter++] = new Connection(costHolder.Key, cost);
            }

            while (counterOldList < portal.InternalPortalCount)
            {
                portal.InternalPortalConnections[counter++] = oldConnections[counterOldList++];
            }
            portal.InternalPortalCount = (byte)counter;
        }
    }

    private static void CheckRegion(Cell[] cells, Portal portal, byte intPortalKey)
    {
        if (cells[portal.CenterPos.x + portal.CenterPos.y * CorrectedMapSizeX].Region != byte.MaxValue) return;
        
        BFS.BfsFromStartPosWithRegionFill(cells, portal.CenterPos, intPortalKey);
    }

    public static void ConnectInternalPortalsAllDir(Cell[] cells, ref Portal?[] portals, int chunkKey)
    {
        HashSet<byte> portalsFromRegionFillAdded = [];
        List<byte> portalsHolder = [];
        int firstPortalKey = GetAllPortalsInChunkAndFirstPortalKey(portals, chunkKey, portalsHolder);
        foreach (var portalIntKey in portalsHolder)
        {
            int portalKey = firstPortalKey + portalIntKey;
            portals[portalKey]!.InternalPortalCount = 0;
        }

        if(portalsHolder.Count == 0) return;
        for (int i = 0; i < portalsHolder.Count - 1; i++)
        {        
            byte intPortalKey1 = portalsHolder[i];
            int portalKey1 = firstPortalKey + portalsHolder[i];
            var portal1 = portals[portalKey1]!;
            var costFields = RegionUtils.GetCostFieldsAndUpdateRegions(cells, portal1, intPortalKey1, portalsFromRegionFillAdded);

            for (int j = i + 1; j < portalsHolder.Count; j++)
            {
                byte intPortalKey2 = portalsHolder[j];
                int portalKey2 = firstPortalKey + intPortalKey2;
                var portal2 = portals[portalKey2]!;
                ushort cost = BFS.GetCostForPath(costFields, portal2.CenterPos);
                if (cost == ushort.MaxValue) continue;
                portalsFromRegionFillAdded.Add(intPortalKey2);
                ref var intPortalConn1 = ref portals[portalKey1]!.InternalPortalConnections[portal1.InternalPortalCount++];
                ref var intPortalConn2 = ref portals[portalKey2]!.InternalPortalConnections[portal2.InternalPortalCount++];
                intPortalConn1 = new Connection(intPortalKey2, cost);
                intPortalConn2 = new Connection(intPortalKey1, cost);
            }
        }
        var lastIntPortalKey = portalsHolder[^1];
        int lastPortalKey = firstPortalKey + lastIntPortalKey;
        var portal = portals[lastPortalKey]!;
        if(!portalsFromRegionFillAdded.Contains(lastIntPortalKey)) RegionUtils.GetCostFieldsAndUpdateRegions(cells, portal, lastIntPortalKey, portalsFromRegionFillAdded);
    }
    
    private static int GetAllPortalsInChunkAndFirstPortalKey(Portal?[] portals, int chunkId,
        List<byte> portalsHolder)
    {
        int key = Portal.GeneratePortalKey(chunkId, 0, 0);
        for (byte i = 0; i < MaxPortalsInChunk; i++)
        {
            int portalKey = key + i;
            if (portals[portalKey] == null)
                continue;
            portalsHolder.Add(i);
        }

        return key;
    }
    
    public static void UpdateChunkPortalsInDir(Cell[] cells, ref Portal?[] portals, Directions dir, int chunkId)
    {
        RemoveDirtyPortals(portals, chunkId, dir);
        int chunkIdX = chunkId % ChunkMapSizeX;
        int chunkIdY = chunkId / ChunkMapSizeX;
        int startX = chunkIdX * ChunkSize + StartCellOffset[(int)dir].x;
        int startY = chunkIdY * ChunkSize + StartCellOffset[(int)dir].y;
        byte[] dirToCheck = DirToCheckStraight[(int)dir];
        Vector2D steppingInDirVector = SteppingInDirVectorArray[(int)dir];
        TryCreatePortalsInStraightChunkDir(cells, ref portals, chunkId, startX, startY, dir, steppingInDirVector,
            dirToCheck);
    }
    
    public static void UpdateChunkPortalsInDirDiagonal(Cell[] cells, ref Portal?[] portals, Directions dir, int chunkId)
    {
        int chunkIdX = chunkId % ChunkMapSizeX;
        int chunkIdY = chunkId / ChunkMapSizeX;
        int startX = chunkIdX * ChunkSize + StartCellOffset[(int)dir].x;
        int startY = chunkIdY * ChunkSize + StartCellOffset[(int)dir].y;
        Vector2D steppingInDirVector = SteppingInDirVectorArray[(int)dir];
        byte[] checkDiagonalChunk = DirToCheckDiagonal[(int)dir];
        int portalDiagonalPosOffset = PortalDiagonalPosOffset[(int)dir];
        TryCreatePortalInDiagonalChunkDir(cells, ref portals, chunkId, startX, startY, dir, steppingInDirVector,
            portalDiagonalPosOffset, checkDiagonalChunk);
    }

    private static void TryCreatePortalInDiagonalChunkDir(Cell[] cells, ref Portal?[] portals, int chunkId, int startX,
        int startY, Directions dir,
        Vector2D steppingInDirVector, int portalPos, byte[] checkDiagonalConnection)
    {
        startX += steppingInDirVector.x * portalPos;
        startY += steppingInDirVector.y * portalPos;
        Vector2D startPos = new Vector2D(startX, startY);
        int key = Portal.GeneratePortalKey(chunkId, portalPos, dir);
        portals[key] = null;
        CheckDiagonalChunksForConnection(cells, portals, dir, steppingInDirVector, checkDiagonalConnection, key, startPos);
    }

    private static void CheckDiagonalChunksForConnection(Cell[] cells, Portal?[] portals, Directions dir, Vector2D steppingInDirVector,
        byte[] checkDiagonalConnection, int key, Vector2D startPos)
    {
        Cell cell = cells[startPos.y * CorrectedMapSizeX + startPos.x];
        //Diagonal Portal Direction NW
        if ((cell.Connections & checkDiagonalConnection[0]) == WALKABLE)
        {
            portals[key] ??= new Portal();
            int externalKey = key + DiagonalPortalKeyOffsets[(int)dir];
            AddExternalPortalConnection(portals, startPos, 1, 0, 0, key, externalKey, steppingInDirVector);
        }
            
        //Connect Diagonal Portal in Direction N
        if ((cell.Connections & checkDiagonalConnection[1]) == WALKABLE)
        {
            portals[key] ??= new Portal();
            int externalKey = key + DiagonalSpecialPortalKeyOffsets[(int)dir];
            AddExternalPortalConnection(portals, startPos, 1, 0, 0, key, externalKey, steppingInDirVector);
            
            
            var dirVec = DirectionsVectorArray[(int)dir] + startPos;
            var oppositeCell = cells[dirVec.y * CorrectedMapSizeX + dirVec.x];
            //Connect Diagonal Portal in Direction NE if NE Walkable and oppositeCell in Dir E is Blocked
            if ((oppositeCell.Connections & checkDiagonalConnection[4]) == checkDiagonalConnection[4] && (cell.Connections & checkDiagonalConnection[3]) == WALKABLE)
            {
                int side = 1 - 2 * ((int)dir / 2);
                externalKey = key + OppositePortalKeyOffsets[(int)dir] + side;
                AddExternalPortalConnection(portals, startPos, 1, 0, 0, key, externalKey, steppingInDirVector);
            }
        } 
        //Connect Diagonal Portal in Direction NE if walkable and N Blocked
        else if ((cell.Connections & checkDiagonalConnection[3]) == WALKABLE)
        {
            
            portals[key] ??= new Portal();
            int side = 1 - 2 * ((int)dir / 2);
            int externalKey = key + OppositePortalKeyOffsets[(int)dir] + side;
            AddExternalPortalConnection(portals, startPos, 1, 0, 0, key, externalKey, steppingInDirVector);
        }
                
        //Connect Diagonal Portal in Direction W
        if ((cell.Connections & checkDiagonalConnection[2]) == WALKABLE)
        {
            portals[key] ??= new Portal();
            int externalKey = key - DiagonalSpecialPortalKeyOffsets[((int)dir + 1) % 4];
            AddExternalPortalConnection(portals, startPos, 1, 0, 0, key, externalKey, steppingInDirVector);
        }
        
    }

    private static void TryCreatePortalsInStraightChunkDir(Cell[] cells, ref Portal?[] portals, int chunkId, int startX,
        int startY, Directions direction, Vector2D steppingInDirVector,
        byte[] checkDir)
    {
        //INIT VALUES
        Vector2D otherCellToCheck = DirectionsVectorArray[(int)direction];
        bool closePortal = false;
        int portalSize = 0;
        int portalPos = 0;
        Vector2D? startPos = null;
        int offsetStart = 0;
        int otherPortalOffset = 0;
        int offsetEnd = 0;
        for (int i = 0; i < ChunkSize; i++)
        {
            int yCell = startY + steppingInDirVector.y * i;
            int xCell = startX + steppingInDirVector.x * i;
            ref Cell cell = ref cells[yCell * CorrectedMapSizeX + xCell];
            //Is there no Connection in NORTH-WEST and NORTH and NORTH-EAST Direction, do nothing
            if ((cell.Connections & checkDir[0]) == checkDir[0])
            {
                closePortal = TryCreateOrUpdatePortal(ref portals, chunkId, closePortal, ref startPos,
                    ref portalSize, direction, portalPos, ref offsetStart, ref otherPortalOffset, ref offsetEnd, steppingInDirVector);
                continue;
            }

            // Check Connection to NORTH
            if ((cell.Connections & checkDir[1]) == WALKABLE)
            {
                closePortal = TryCreateOrUpdatePortal(ref portals, chunkId, closePortal, ref startPos,
                    ref portalSize, direction, portalPos, ref offsetStart, ref otherPortalOffset, ref offsetEnd, steppingInDirVector);
                startPos = SetStartPos(startPos, cell, i, ref portalSize, ref portalPos);
                portalSize++;
                otherPortalOffset = 0;

                //Opposite Cell in North
                var oppositeCell = cells[(yCell + otherCellToCheck.y) * CorrectedMapSizeX + xCell + otherCellToCheck.x];
                //Am I at the end of my Portal in Direction
                if ((cell.Connections & checkDir[2]) != WALKABLE || //Connection to EAST or NORTH-EAST not Walkable 
                    (oppositeCell.Connections & checkDir[6]) !=
                    WALKABLE) // Connection other cell EAST or SOUTH-EAST not Walkable
                {
                    closePortal = true;
                }

                //Check Diagonal Connection to NORTH-WEST
                if ((cell.Connections & checkDir[3]) == WALKABLE)
                {
                    //OppositeDiagonalCell NORTH-WEST
                    var oppositeDiagonalCell = cells[(yCell + otherCellToCheck.y - steppingInDirVector.y) * CorrectedMapSizeX +
                        xCell + otherCellToCheck.x - steppingInDirVector.x];
                    //Check in direction South Not Walkable:
                    if ((oppositeDiagonalCell.Connections & checkDir[8]) != WALKABLE)
                    {
                        CloseSinglePortal(ref portals, chunkId, direction, cell, i, -1, steppingInDirVector);
                    }
                }

                //Check Diagonal Connection to NORTH-EAST
                if ((cell.Connections & checkDir[5]) == WALKABLE)
                {
                    //OppositeDiagonalCell NORTH-EAST
                    var oppositeDiagonalCell = cells[(yCell + otherCellToCheck.y + steppingInDirVector.y) * CorrectedMapSizeX +
                                                     xCell + otherCellToCheck.x + steppingInDirVector.x];
                    //Check in direction South Not Walkable:
                    if ((oppositeDiagonalCell.Connections & checkDir[8]) != WALKABLE)
                    {
                        CloseSinglePortal(ref portals, chunkId, direction, cell, i, 1, steppingInDirVector);
                    }
                }

                continue;
            }

            //Check Diagonal Connection to NORTH-WEST
            if ((cell.Connections & checkDir[3]) == WALKABLE)
            {
                CloseSinglePortal(ref portals, chunkId, direction, cell, i, -1, steppingInDirVector);
                //Do I belong to the Portal in the WEST
                if (closePortal && (cell.Connections & checkDir[4]) == WALKABLE)
                {
                    offsetEnd = 1;
                    portalSize++;
                }
            }

            closePortal = TryCreateOrUpdatePortal(ref portals, chunkId, closePortal, ref startPos, ref portalSize,
                direction, portalPos, ref offsetStart, ref otherPortalOffset, ref offsetEnd, steppingInDirVector);

            //Check Diagonal Connection to NORTH-EAST
            if ((cell.Connections & checkDir[5]) == WALKABLE)
            {
                CloseSinglePortal(ref portals, chunkId, direction, cell, i, 1, steppingInDirVector);
                //Check Connection to EAST if we can add this Tile to the new Portal
                if ((cell.Connections & checkDir[7]) == WALKABLE)
                {
                    startPos = cell.Position;
                    portalPos = i;
                    portalSize = 1;
                    offsetStart = 1;
                    otherPortalOffset = 1;
                }
            }

            closePortal = TryCreateOrUpdatePortal(ref portals, chunkId, closePortal, ref startPos, ref portalSize,
                direction, portalPos, ref offsetStart, ref otherPortalOffset, ref offsetEnd, steppingInDirVector);
        }

        //If the portal is not closed at the end close it.
        if (portalSize > 0 && (offsetStart != 1 || portalPos != ChunkSize - 1))
            TryCreateOrUpdatePortal(ref portals, chunkId, true, ref startPos, ref portalSize, direction, portalPos,
                ref offsetStart, ref otherPortalOffset, ref offsetEnd, steppingInDirVector);
    }
    
    private static void RemoveDirtyPortals(Portal?[] portals, int chunkId, Directions direction)
    {
        for (int i = 0; i < ChunkSize; i++)
        {
            int key = Portal.GeneratePortalKey(chunkId, i, direction);
            portals[key] = null;
        }
    }

    private static void CloseSinglePortal(ref Portal?[] portals, int chunkId, Directions direction, Cell cell, int tempPortalPos,
        int otherPortalOffset, Vector2D steppingInDirVector)
    {
        //outside portals are handled by the diagonal Portals which are calculated extra
        if (Chunk.IsDiagonalOppositeChunk(tempPortalPos + otherPortalOffset, -1) ||
            Chunk.IsDiagonalOppositeChunk(tempPortalPos + otherPortalOffset, ChunkSize))
            return;
        var tempStartPos = cell.Position;
        var tempPortalSize = 1;
        var tempOtherPortalOffset = otherPortalOffset;
        var tempOffsetEnd = 0;
        var tempOffsetStart = 0;
        TryCreateOrUpdatePortal(ref portals, chunkId, true, ref tempStartPos, ref tempPortalSize, direction,
            tempPortalPos, ref tempOffsetStart, ref tempOtherPortalOffset, ref tempOffsetEnd, steppingInDirVector);
    }
    
    private static Vector2D SetStartPos(Vector2D? startPos, Cell cell, int i, ref int portalSize, ref int portalPos)
    {
        if (startPos is not null) return startPos;

        portalSize = 0;
        startPos = cell.Position;
        portalPos = i;
        return startPos;
    }

    private static bool TryCreateOrUpdatePortal(ref Portal?[] portals, int chunkId, bool closePortal, ref Vector2D? startPos,
        ref int portalSize, Directions dir, int portalPos
        , ref int offsetStart, ref int otherPortalOffset, ref int offsetEnd, Vector2D steppingInDirVector)
    {
        if (!closePortal) return false;

        var key = TryCreatePortal(ref portals, chunkId, portalSize, dir, offsetStart, offsetEnd,
            portalPos);
        int externalKey = key + OppositePortalKeyOffsets[(int)dir] + otherPortalOffset;
        AddExternalPortalConnection(portals, startPos!, portalSize, offsetStart, offsetEnd, key, externalKey, steppingInDirVector);

        startPos = null;
        portalSize = 0;
        offsetStart = 0;
        otherPortalOffset = 0;
        offsetEnd = 0;

        return false;
    }

    private static int TryCreatePortal(ref Portal?[] portals, int chunkId, int portalSize,
        Directions dir,
        int offsetStart, int offsetEnd, int portalPos)
    {
        int centerPos = portalPos + offsetStart + (portalSize - offsetEnd - offsetStart) / 2;
        int key = Portal.GeneratePortalKey(chunkId, centerPos, dir);
        portals[key] ??= new Portal();
        return key;
    }

    private static void AddExternalPortalConnection(Portal?[] portals, Vector2D startPos, int portalSize,
        int offsetStart,
        int offsetEnd, int key, int externalKey, Vector2D steppingInDirVector)
    {
        portals[key]!.ChangeLength(startPos, (byte)portalSize, offsetStart, offsetEnd, steppingInDirVector);
        if (portals[key]!.ExternalPortalCount == 4)
        {
            
        }
        portals[key]!.AddExternalConnection(externalKey);
    }
}