using System;
using System.Collections.Generic;
using System.Linq;
using HpaStarPathfinding.pathfinding;
using static HpaStarPathfinding.ViewModel.DirectionsAsByte;

namespace HpaStarPathfinding.ViewModel
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

        private const int OffsetChunkByY = MainWindowViewModel.MaxPortalsInChunk * MainWindowViewModel.ChunkMapSize;
        private const int OffsetChunkByX = MainWindowViewModel.MaxPortalsInChunk;

        private static readonly int[] OppositePortalKeyOffsets =
        {
            -OffsetChunkByY + MainWindowViewModel.ChunkSize * 2, //North
            OffsetChunkByX + MainWindowViewModel.ChunkSize * 2, //EAST
            OffsetChunkByY - MainWindowViewModel.ChunkSize * 2, //SOUTH
            -OffsetChunkByX - MainWindowViewModel.ChunkSize * 2 //WEST
        };

        private static readonly int[] DiagonalPortalKeyOffsets =
        {
            -OffsetChunkByY - OffsetChunkByX + MainWindowViewModel.ChunkSize * 3 -
            1, //North //offset inside the chunk the from top left 0 -> 29 to right bottom 
            -OffsetChunkByY + OffsetChunkByX + MainWindowViewModel.ChunkSize * 3 -
            1, //EAST //offset inside the chunk the from top right is 10 -> 39 to bottom left
            OffsetChunkByY + OffsetChunkByX -
            (MainWindowViewModel.ChunkSize * 3 -
             1), //SOUTH //offset inside the chunk the from bottom right is 29 -> 0 to top left
            OffsetChunkByY - OffsetChunkByX -
            (MainWindowViewModel.ChunkSize * 3 -
             1) //WEST //offset inside the chunk the from bottom left is 39 -> 10 to top right
        };

        private static readonly int[] DiagonalSpecialPortalKeyOffsets =
        {
            -OffsetChunkByY + MainWindowViewModel.ChunkSize * 4 - 1, //North
            OffsetChunkByX - MainWindowViewModel.ChunkSize, //EAST
            OffsetChunkByY - (MainWindowViewModel.ChunkSize * 2 - 1), //SOUTH
            -OffsetChunkByX - MainWindowViewModel.ChunkSize //WEST
        };
        
        private static readonly Vector2D[] DirectionsVectorArray =
            { DirectionsVector.N, DirectionsVector.E, DirectionsVector.S, DirectionsVector.W };

        public static void ConnectInternalPortals(Cell[,] cells, ref Portal[] portals, int chunkIdX, int chunkIdY)
        {
            List<PortalHolder> portalsHolder = new List<PortalHolder>();
            GetAllPortalsInChunk(portals, chunkIdX, chunkIdY, portalsHolder);
            Vector2D min = new Vector2D(chunkIdX * MainWindowViewModel.ChunkSize,
                chunkIdY * MainWindowViewModel.ChunkSize);
            Vector2D max = new Vector2D(min.x + MainWindowViewModel.ChunkSize, min.y + MainWindowViewModel.ChunkSize);
            //here we could also cache the path
            for (int i = 0; i < portalsHolder.Count - 1; i++)
            {
                for (int j = i + 1; j < portalsHolder.Count; j++)
                {
                    float cost = AStarOnlyCost.FindPath(cells, portalsHolder[i].Pos, portalsHolder[j].Pos, min, max);
                    if (cost < 0) continue;
                    var portalHolder1 = portalsHolder[i];
                    var portalHolder2 = portalsHolder[j];
                    ref var internalPortalConnection1 =
                        ref portalHolder1.Portal.InternalPortalConnections[portalHolder1.ArrayIndex];
                    ref var internalPortalConnection2 =
                        ref portalHolder2.Portal.InternalPortalConnections[portalHolder2.ArrayIndex];
                    internalPortalConnection1.cost = (byte)cost;
                    internalPortalConnection1.portal =
                        (byte)(portalHolder2.Key % MainWindowViewModel.MaxPortalsInChunk);
                    internalPortalConnection2.cost = (byte)cost;
                    internalPortalConnection2.portal =
                        (byte)(portalHolder1.Key % MainWindowViewModel.MaxPortalsInChunk);
                    portalHolder1.ArrayIndex++;
                    portalHolder2.ArrayIndex++;
                }
            }
        }

        private static void GetAllPortalsInChunk(Portal[] portals, int chunkIdX, int chunkIdY,
            List<PortalHolder> portalsHolder)
        {
            int chunkId = chunkIdX + MainWindowViewModel.ChunkMapSize * chunkIdY;
            foreach (var direction in Enum.GetValues(typeof(Directions)).Cast<Directions>())
            {
                for (int i = 0; i < MainWindowViewModel.ChunkSize; i++)
                {
                    int key = Portal.GeneratePortalKey(chunkId, i, direction);
                    if (portals[key] == null)
                        continue;
                    var portalHolder = new PortalHolder
                    {
                        Portal = portals[key],
                        Key = key,
                        ArrayIndex = 0,
                        Pos = Portal.PortalKeyToWorldPos(key)
                    };
                    portalsHolder.Add(portalHolder);
                }
            }
        }

        public static void RebuildAllPortals(Cell[,] cells, ref Portal[] portals, int chunkIdX, int chunkIdY)
        {
            int chunkId = chunkIdX + MainWindowViewModel.ChunkMapSize * chunkIdY;
            foreach (var direction in Enum.GetValues(typeof(Directions)).Cast<Directions>())
            {
                RebuildPortalsInDirection(direction, cells, ref portals, chunkIdX, chunkIdY, chunkId);
            }
        }

        private static void RebuildPortalsInDirection(Directions dir, Cell[,] cells, ref Portal[] portals, int chunkIdX,
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
                    startX = chunkIdX * MainWindowViewModel.ChunkSize;
                    startY = chunkIdY * MainWindowViewModel.ChunkSize;
                    steppingInDirVector = new Vector2D(1, 0);
                    dirToCheck = new[] { NW_N_NE, N, E_NE, NW, W, NE, E_SE, E, S };
                    checkDiagonalChunk = new[] { NW, N, SW, W, NE };
                    portalDiagonalPosOffset = 0;
                    break;
                case Directions.E:
                    startX = chunkIdX * MainWindowViewModel.ChunkSize + MainWindowViewModel.ChunkSize - 1;
                    startY = chunkIdY * MainWindowViewModel.ChunkSize;
                    steppingInDirVector = new Vector2D(0, 1);
                    dirToCheck = new[] { NE_E_SE, E, S_SE, NE, N, SE, S_SW, S, W };
                    checkDiagonalChunk = new[] { NE, E, NW, N, SE };
                    portalDiagonalPosOffset = 0;
                    break;
                case Directions.S:
                    startX = chunkIdX * MainWindowViewModel.ChunkSize;
                    startY = chunkIdY * MainWindowViewModel.ChunkSize + MainWindowViewModel.ChunkSize - 1;
                    steppingInDirVector = new Vector2D(1, 0);
                    dirToCheck = new[] { SW_S_SE, S, E_SE, SW, W, SE, E_NE, E, N };
                    checkDiagonalChunk = new[] { SE, S, NE, E, SW };
                    portalDiagonalPosOffset = MainWindowViewModel.ChunkSize - 1;
                    break;
                case Directions.W:
                    startX = chunkIdX * MainWindowViewModel.ChunkSize;
                    startY = chunkIdY * MainWindowViewModel.ChunkSize;
                    steppingInDirVector = new Vector2D(0, 1);
                    dirToCheck = new[] { NW_W_SW, W, S_SW, NW, N, SW, S_SE, S, E };
                    checkDiagonalChunk = new[] { SW, W, SE, S, NW };
                    portalDiagonalPosOffset = MainWindowViewModel.ChunkSize - 1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dir), dir, null);
            }

            TryCreatePortalsInChunkDirection(cells, ref portals, chunkId, startX, startY, dir, steppingInDirVector,
                dirToCheck);
             TryCreatePortalForDiagonalChunk(cells, ref portals, chunkId, startX, startY, dir, steppingInDirVector,
                  portalDiagonalPosOffset, checkDiagonalChunk);
        }

        private static void TryCreatePortalForDiagonalChunk(Cell[,] cells, ref Portal[] portals, int chunkId, int startX,
            int startY, Directions dir,
            Vector2D steppingInDirVector, int portalPos, byte[] checkDiagonalConnection)
        {
            startX += steppingInDirVector.x * portalPos;
            startY += steppingInDirVector.y * portalPos;
            ref Cell cell = ref cells[startY, startX];
            Vector2D startPos = new Vector2D(startX, startY);
            int portalSize = 1;
            //Diagonal Portal Direction NW
            if ((cell.Connections & checkDiagonalConnection[0]) == WALKABLE)
            {
                int key = TryCreatePortal(ref portals, chunkId, startPos, portalSize, dir, 0,
                    0, portalPos);
                int externalKey = key + DiagonalPortalKeyOffsets[(int)dir];
                AddExternalPortalConnection(portals, dir, startPos, portalSize, 0, 0, key, externalKey);

                //Connect Diagonal Portal in Direction N if there is one that has a diagonal connection to SW
                Vector2D oppositeCell = DirectionsVectorArray[(int)dir];
                if ((cell.Connections & checkDiagonalConnection[1]) == WALKABLE &&
                    (cells[startY + oppositeCell.y, startX + oppositeCell.x].Connections &
                     checkDiagonalConnection[2]) == WALKABLE)
                {
                    externalKey = key + DiagonalSpecialPortalKeyOffsets[(int)dir];
                    AddExternalPortalConnection(portals, dir, startPos, portalSize, 0, 0, key, externalKey);
                }
                
                //Connect Diagonal Portal in Direction W if there is one that has a diagonal connection to NE
                oppositeCell = DirectionsVectorArray[((int)dir + 3) % 4];
                if ((cell.Connections & checkDiagonalConnection[3]) == WALKABLE &&
                    (cells[startY + oppositeCell.y, startX + oppositeCell.x].Connections &
                     checkDiagonalConnection[4]) == WALKABLE)
                {
                    externalKey = key - DiagonalSpecialPortalKeyOffsets[((int)dir + 1) % 4];
                    AddExternalPortalConnection(portals, dir, startPos, portalSize, 0, 0, key, externalKey);
                }
            }
        }

        private static void TryCreatePortalsInChunkDirection(Cell[,] cells, ref Portal[] portals, int chunkId, int startX,
            int startY, Directions direction, Vector2D steppingInDirVector,
            byte[] checkDir)
        {
            RemoveDirtyPortals(portals, chunkId, direction);

            //INIT VALUES
            Vector2D otherCellToCheck = DirectionsVectorArray[(int)direction];
            bool closePortal = false;
            int portalSize = 0;
            int portalPos = 0;
            Vector2D startPos = null;
            int offsetStart = 0;
            int otherPortalOffset = 0;
            int offsetEnd = 0;
            for (int i = 0; i < MainWindowViewModel.ChunkSize; i++)
            {
                int yCell = startY + steppingInDirVector.y * i;
                int xCell = startX + steppingInDirVector.x * i;
                ref Cell cell = ref cells[yCell, xCell];
                //Is there no Connection in NORTH-WEST and NORTH and NORTH-EAST Direction, do nothing
                if ((cell.Connections & checkDir[0]) == checkDir[0])
                {
                    closePortal = TryCreateOrUpdatePortal(ref portals, chunkId, closePortal, ref startPos,
                        ref portalSize, direction, portalPos, ref offsetStart, ref otherPortalOffset, ref offsetEnd);
                    continue;
                }

                // Check Connection to NORTH
                if ((cell.Connections & checkDir[1]) == WALKABLE)
                {
                    closePortal = TryCreateOrUpdatePortal(ref portals, chunkId, closePortal, ref startPos,
                        ref portalSize, direction, portalPos, ref offsetStart, ref otherPortalOffset, ref offsetEnd);
                    startPos = SetStartPos(startPos, cell, i, ref portalSize, ref portalPos);
                    portalSize++;
                    otherPortalOffset = 0;

                    //Opposite Cell in North
                    var oppositeCell = cells[yCell + otherCellToCheck.y, xCell + otherCellToCheck.x];
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
                        var oppositeDiagonalCell = cells[yCell + otherCellToCheck.y - steppingInDirVector.y,
                            xCell + otherCellToCheck.x - steppingInDirVector.x];
                        //Check in direction South Not Walkable:
                        if ((oppositeDiagonalCell.Connections & checkDir[8]) != WALKABLE)
                        {
                            CloseSinglePortal(ref portals, chunkId, direction, cell, i, -1);
                        }
                    }

                    //Check Diagonal Connection to NORTH-EAST
                    if ((cell.Connections & checkDir[5]) == WALKABLE)
                    {
                        //OppositeDiagonalCell NORTH-EAST
                        var oppositeDiagonalCell = cells[yCell + otherCellToCheck.y + steppingInDirVector.y,
                            xCell + otherCellToCheck.x + steppingInDirVector.x];
                        //Check in direction South Not Walkable:
                        if ((oppositeDiagonalCell.Connections & checkDir[8]) != WALKABLE)
                        {
                            CloseSinglePortal(ref portals, chunkId, direction, cell, i, 1);
                        }
                    }

                    continue;
                }

                //Check Diagonal Connection to NORTH-WEST
                if ((cell.Connections & checkDir[3]) == WALKABLE)
                {
                    CloseSinglePortal(ref portals, chunkId, direction, cell, i, -1);
                    //Do I belong to the Portal in the WEST
                    if (closePortal && (cell.Connections & checkDir[4]) == WALKABLE)
                    {
                        offsetEnd = 1;
                        portalSize++;
                    }
                }

                closePortal = TryCreateOrUpdatePortal(ref portals, chunkId, closePortal, ref startPos, ref portalSize,
                    direction, portalPos, ref offsetStart, ref otherPortalOffset, ref offsetEnd);

                //Check Diagonal Connection to NORTH-EAST
                if ((cell.Connections & checkDir[5]) == WALKABLE)
                {
                    CloseSinglePortal(ref portals, chunkId, direction, cell, i, 1);
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
                    direction, portalPos, ref offsetStart, ref otherPortalOffset, ref offsetEnd);
            }

            //If the portal is not closed at the end close it.
            if (portalSize > 0 && (offsetStart != 1 || portalPos != MainWindowViewModel.ChunkSize - 1))
                TryCreateOrUpdatePortal(ref portals, chunkId, true, ref startPos, ref portalSize, direction, portalPos,
                    ref offsetStart, ref otherPortalOffset, ref offsetEnd);
        }

        private static void RemoveDirtyPortals(Portal[] portals, int chunkId, Directions direction)
        {
            for (int i = 0; i < MainWindowViewModel.ChunkSize; i++)
            {
                int key = Portal.GeneratePortalKey(chunkId, i, direction);
                portals[key] = null;
            }
        }

        private static void CloseSinglePortal(ref Portal[] portals, int chunkId, Directions direction, Cell cell, int i,
            int otherPortalOffset)
        {
            var tempPortalPos = i;
            //outside portals are handled by the diagonal Portals which are calculated extra
            if (IsDiagonalOppositeChunk(tempPortalPos + otherPortalOffset, -1) ||
                IsDiagonalOppositeChunk(tempPortalPos + otherPortalOffset, MainWindowViewModel.ChunkSize))
                return;
            var tempStartPos = cell.Position;
            var tempPortalSize = 1;
            var tempOtherPortalOffset = otherPortalOffset;
            var tempOffsetEnd = 0;
            var tempOffsetStart = 0;
            TryCreateOrUpdatePortal(ref portals, chunkId, true, ref tempStartPos, ref tempPortalSize, direction,
                tempPortalPos, ref tempOffsetStart, ref tempOtherPortalOffset, ref tempOffsetEnd);
        }

        private static bool IsDiagonalOppositeChunk(int chunkPosition, int outsidePosition)
        {
            return chunkPosition == outsidePosition;
        }

        private static Vector2D SetStartPos(Vector2D startPos, Cell cell, int i, ref int portalSize, ref int portalPos)
        {
            if (startPos != null) return startPos;

            portalSize = 0;
            startPos = cell.Position;
            portalPos = i;
            return startPos;
        }

        private static bool TryCreateOrUpdatePortal(ref Portal[] portals, int chunkId, bool closePortal, ref Vector2D startPos,
            ref int portalSize, Directions dir, int portalPos
            , ref int offsetStart, ref int otherPortalOffset, ref int offsetEnd)
        {
            if (!closePortal) return false;

            var key = TryCreatePortal(ref portals, chunkId, startPos, portalSize, dir, offsetStart, offsetEnd,
                portalPos);
            int externalKey = key + OppositePortalKeyOffsets[(int)dir] + otherPortalOffset;
            AddExternalPortalConnection(portals, dir, startPos, portalSize, offsetStart, offsetEnd, key, externalKey);

            startPos = null;
            portalSize = 0;
            offsetStart = 0;
            otherPortalOffset = 0;
            offsetEnd = 0;

            return false;
        }

        private static int TryCreatePortal(ref Portal[] portals, int chunkId, Vector2D startPos, int portalSize,
            Directions dir,
            int offsetStart, int offsetEnd, int portalPos)
        {
            int centerPos = portalPos + offsetStart + (portalSize - offsetEnd - offsetStart) / 2;
            int key = Portal.GeneratePortalKey(chunkId, centerPos, dir);
            if (portals[key] == null)
            {
                portals[key] = new Portal(startPos, portalSize, dir, offsetStart, offsetEnd);
            }

            return key;
        }

        private static void AddExternalPortalConnection(Portal[] portals, Directions dir, Vector2D startPos, int portalSize,
            int offsetStart,
            int offsetEnd, int key, int externalKey)
        {
            portals[key].ChangeLength(dir, startPos, (byte)portalSize, offsetStart, offsetEnd);
            int i = 0;
            while (portals[key].ExternalPortalConnections[i] != -1)
            {
                i++;
            }
            portals[key].ExternalPortalConnections[i] = externalKey;
        }
    }
}