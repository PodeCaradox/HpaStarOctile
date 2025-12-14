using HpaStarPathfinding.pathfinding;
using static HpaStarPathfinding.model.pathfinding.DirectionsAsByte;
using static HpaStarPathfinding.ViewModel.MainWindowViewModel;

namespace HpaStarPathfinding.model.pathfinding
{
    public class Chunk
    {
        private const byte SW_S_SE = SW | S | SE;
        private const byte E_SE = E | SE;
        private const byte NW_N_NE = NW | N | NE;
        private const byte E_NE = E | NE;
        private const byte NE_E_SE = NE | E | SE;
        private const byte S_SE = S | SE;
        private const byte NW_W_SW = NW | W | SW;
        private const byte S_SW = S | SW;

        public static void InitChunkValues()
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
                -OffsetChunkByY - OffsetChunkByX + ChunkSize * 3 -
                1, //North //offset inside the chunk from top left 0 -> 29 to right bottom 
                -OffsetChunkByY + OffsetChunkByX + ChunkSize * 3 -
                1, //EAST //offset inside the chunk from top right is 10 -> 39 to bottom left
                OffsetChunkByY + OffsetChunkByX -
                (ChunkSize * 3 -
                 1), //SOUTH //offset inside the chunk from bottom right is 29 -> 0 to top left
                OffsetChunkByY - OffsetChunkByX -
                (ChunkSize * 3 -
                 1) //WEST //offset inside the chunk from bottom left is 39 -> 10 to top right
            ];
        }

        private static int OffsetChunkByY = MaxPortalsInChunk * ChunkMapSizeX;
        private static int OffsetChunkByX = MaxPortalsInChunk;

        //public byte ClearChunk;// all tiles are connected flood fill.


        private static int[] OppositePortalKeyOffsets = []; 

        private static int[] DiagonalPortalKeyOffsets = []; 

        private static int[] DiagonalSpecialPortalKeyOffsets = []; 
        
        private static readonly Vector2D[] DirectionsVectorArray = [DirectionsVector.N, DirectionsVector.E, DirectionsVector.S, DirectionsVector.W];

        public static void CreateRegionsAndConnectInternalPortals(Cell[] cells, ref Chunk chunk, ref Portal?[] portals, int chunkKey)
        {
            ResetRegions(cells, chunkKey);
            HashSet<byte> portalsFromRegionFillAdded = [];
            int chunkIdX = chunkKey % ChunkMapSizeX;
            int chunkIdY = chunkKey / ChunkMapSizeX;
            List<PortalHolder> portalsHolder = [];
            int firstPortalKey = GetAllPortalsInChunkAndFirstPortalKey(portals, chunkKey, portalsHolder);
            Vector2D min = new Vector2D(chunkIdX * ChunkSize, chunkIdY * ChunkSize);
            Vector2D max = new Vector2D(min.x + ChunkSize, min.y + ChunkSize);
            for (int i = 0; i < portalsHolder.Count - 1; i++)
            {
                var portal1 = portalsHolder[i];
                var costFields = GetCostFieldsAndUpdateRegions(cells, portal1, min, max, portalsFromRegionFillAdded);
                int portalKey1 = firstPortalKey + portal1.Key;
                for (int j = i + 1; j < portalsHolder.Count; j++)
                {
                    var portal2 = portalsHolder[j];
                    ushort cost = BFS.GetCostForPath(costFields, portal2.Pos);
                    if (cost == ushort.MaxValue) continue;
                    portalsFromRegionFillAdded.Add(portal2.Key);
                    int portalKey2 = firstPortalKey + portal2.Key;
                    portals[portalKey1]!.ExtIntPortalCount++;
                    portals[portalKey2]!.ExtIntPortalCount++;
                    ref var intPortalConn1 = ref portals[portalKey1]!.InternalPortalConnections[portal1.ArrayIndex++];
                    ref var intPortalConn2 = ref portals[portalKey2]!.InternalPortalConnections[portal2.ArrayIndex++];
                    intPortalConn1.cost = cost;
                    intPortalConn1.portalKey = portal2.Key;
                    intPortalConn2.cost = cost;
                    intPortalConn2.portalKey = portal1.Key;
                }
            }
            var lastPortal = portalsHolder[^1];
            if(!portalsFromRegionFillAdded.Contains(lastPortal.Key)) GetCostFieldsAndUpdateRegions(cells, lastPortal, min, max, portalsFromRegionFillAdded);
        }

        private static ushort[] GetCostFieldsAndUpdateRegions(Cell[] cells, PortalHolder portal, Vector2D min, Vector2D max,
            HashSet<byte> portalsFromRegionFillAdded)
        {
            ushort[] costFields;
            if (portalsFromRegionFillAdded.Add(portal.Key))
            {
                costFields = BFS.BfsFromStartPosWithRegionFill(cells, portal.Pos, min, max, portal.Key);
            }
            else
            {
                costFields = BFS.BfsFromStartPos(cells, portal.Pos, min, max);
            }

            return costFields;
        }

        private static int GetAllPortalsInChunkAndFirstPortalKey(Portal?[] portals, int chunkId,
            List<PortalHolder> portalsHolder)
        {
            int key = Portal.GeneratePortalKey(chunkId, 0, 0);
            for (byte i = 0; i < MaxPortalsInChunk; i++)
            {
                int portalKey = key + i;
                if (portals[portalKey] == null)
                    continue;
                var portalHolder = new PortalHolder(portals[portalKey]!.CenterPos, i, 0);
                portalsHolder.Add(portalHolder);
            }

            return key;
        }

        public static void RebuildAllPortals(Cell[] cells, ref Portal?[] portals, int chunkKey)
        {
            
            int chunkIdX = chunkKey % ChunkMapSizeX;
            int chunkIdY = chunkKey / ChunkMapSizeX;
            foreach (var direction in Enum.GetValues(typeof(Directions)).Cast<Directions>())
            {
                RebuildPortalsInDirection(direction, cells, ref portals, chunkIdX, chunkIdY, chunkKey);
            }
        }

        private static void RebuildPortalsInDirection(Directions dir, Cell[] cells, ref Portal?[] portals, int chunkIdX,
            int chunkIdY, int chunkId)
        {
            int startX;
            int startY;
            byte[] dirToCheck;
            Vector2D steppingInDirVector;
            byte[] checkDiagonalChunk;
            int portalDiagonalPosOffset;
            switch (dir)
            {
                case Directions.N:
                    startX = chunkIdX * ChunkSize;
                    startY = chunkIdY * ChunkSize;
                    steppingInDirVector = new Vector2D(1, 0);
                    dirToCheck = [NW_N_NE, N, E_NE, NW, W, NE, E_SE, E, S];
                    checkDiagonalChunk = [NW, N, W];
                    portalDiagonalPosOffset = 0;
                    break;
                case Directions.E:
                    startX = chunkIdX * ChunkSize + ChunkSize - 1;
                    startY = chunkIdY * ChunkSize;
                    steppingInDirVector = new Vector2D(0, 1);
                    dirToCheck = [NE_E_SE, E, S_SE, NE, N, SE, S_SW, S, W];
                    checkDiagonalChunk = [NE, E, N];
                    portalDiagonalPosOffset = 0;
                    break;
                case Directions.S:
                    startX = chunkIdX * ChunkSize;
                    startY = chunkIdY * ChunkSize + ChunkSize - 1;
                    steppingInDirVector = new Vector2D(1, 0);
                    dirToCheck = [SW_S_SE, S, E_SE, SW, W, SE, E_NE, E, N];
                    checkDiagonalChunk = [SE, S, E];
                    portalDiagonalPosOffset = ChunkSize - 1;
                    break;
                case Directions.W:
                    startX = chunkIdX * ChunkSize;
                    startY = chunkIdY * ChunkSize;
                    steppingInDirVector = new Vector2D(0, 1);
                    dirToCheck = [NW_W_SW, W, S_SW, NW, N, SW, S_SE, S, E];
                    checkDiagonalChunk = [SW, W, S];
                    portalDiagonalPosOffset = ChunkSize - 1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dir), dir, null);
            }

            TryCreatePortalsInChunkDirection(cells, ref portals, chunkId, startX, startY, dir, steppingInDirVector,
                dirToCheck);
             TryCreatePortalForDiagonalChunk(cells, ref portals, chunkId, startX, startY, dir, steppingInDirVector,
                  portalDiagonalPosOffset, checkDiagonalChunk);
        }

        private static void TryCreatePortalForDiagonalChunk(Cell[] cells, ref Portal?[] portals, int chunkId, int startX,
            int startY, Directions dir,
            Vector2D steppingInDirVector, int portalPos, byte[] checkDiagonalConnection)
        {
            startX += steppingInDirVector.x * portalPos;
            startY += steppingInDirVector.y * portalPos;
            ref Cell cell = ref cells[startY * MapSizeX + startX];
            Vector2D startPos = new Vector2D(startX, startY);
            int key = Portal.GeneratePortalKey(chunkId, portalPos, dir);
            //Diagonal Portal Direction NW
            if ((cell.Connections & checkDiagonalConnection[0]) == WALKABLE)
            {
                portals[key] ??= new Portal();
                int externalKey = key + DiagonalPortalKeyOffsets[(int)dir];
                AddExternalPortalConnection(portals, startPos, 1, 0, 0, key, externalKey, steppingInDirVector);
            }
            
            //Connect Diagonal Portal in Direction N if there is one that has a diagonal connection to SW
            if ((cell.Connections & checkDiagonalConnection[1]) == WALKABLE)
            {
                portals[key] ??= new Portal();
                int externalKey = key + DiagonalSpecialPortalKeyOffsets[(int)dir];
                AddExternalPortalConnection(portals, startPos, 1, 0, 0, key, externalKey, steppingInDirVector);
            }
                
            //Connect Diagonal Portal in Direction W if there is one that has a diagonal connection to NE
            if ((cell.Connections & checkDiagonalConnection[2]) == WALKABLE)
            {
                portals[key] ??= new Portal();
                int externalKey = key - DiagonalSpecialPortalKeyOffsets[((int)dir + 1) % 4];
                AddExternalPortalConnection(portals, startPos, 1, 0, 0, key, externalKey, steppingInDirVector);
            }
        }

        private static void TryCreatePortalsInChunkDirection(Cell[] cells, ref Portal?[] portals, int chunkId, int startX,
            int startY, Directions direction, Vector2D steppingInDirVector,
            byte[] checkDir)
        {
            RemoveDirtyPortals(portals, chunkId, direction);

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
                ref Cell cell = ref cells[yCell * MapSizeX + xCell];
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
                    var oppositeCell = cells[(yCell + otherCellToCheck.y) * MapSizeX + xCell + otherCellToCheck.x];
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
                        var oppositeDiagonalCell = cells[(yCell + otherCellToCheck.y - steppingInDirVector.y) * MapSizeX +
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
                        var oppositeDiagonalCell = cells[(yCell + otherCellToCheck.y + steppingInDirVector.y) * MapSizeX +
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

        private static void CloseSinglePortal(ref Portal?[] portals, int chunkId, Directions direction, Cell cell, int i,
            int otherPortalOffset, Vector2D steppingInDirVector)
        {
            var tempPortalPos = i;
            //outside portals are handled by the diagonal Portals which are calculated extra
            if (IsDiagonalOppositeChunk(tempPortalPos + otherPortalOffset, -1) ||
                IsDiagonalOppositeChunk(tempPortalPos + otherPortalOffset, ChunkSize))
                return;
            var tempStartPos = cell.Position;
            var tempPortalSize = 1;
            var tempOtherPortalOffset = otherPortalOffset;
            var tempOffsetEnd = 0;
            var tempOffsetStart = 0;
            TryCreateOrUpdatePortal(ref portals, chunkId, true, ref tempStartPos, ref tempPortalSize, direction,
                tempPortalPos, ref tempOffsetStart, ref tempOtherPortalOffset, ref tempOffsetEnd, steppingInDirVector);
        }

        private static bool IsDiagonalOppositeChunk(int chunkPosition, int outsidePosition)
        {
            return chunkPosition == outsidePosition;
        }

        private static Vector2D SetStartPos(Vector2D? startPos, Cell cell, int i, ref int portalSize, ref int portalPos)
        {
            if (startPos != null) return startPos;

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
            portals[key]!.AddExternalConnection(externalKey);
        }

        private static void ResetRegions(Cell[] vmMap, int chunkKey)
        {
            for (int y = 0; y < ChunkSize; y++)
            {
                for (int x = 0; x < ChunkSize; x++)
                {
                    vmMap[chunkKey + x].Region = byte.MaxValue;
                }
                
                chunkKey += MapSizeX;
            }
        }
    }
}