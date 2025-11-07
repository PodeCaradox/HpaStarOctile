using System;
using System.Collections.Generic;
using System.Linq;
using HpaStarPathfinding.pathfinding;
using static HpaStarPathfinding.ViewModel.DirectionsAsByte;

namespace HpaStarPathfinding.ViewModel
{
    public class Chunk
    {
        //Todo connect diagonal nodes witch each other side ;)        

        private const byte SW_S_SE = SW | S | SE;
        private const byte E_SE = E | SE;
        private const byte NW_N_NE = NW | N | NE;
        private const byte E_NE = E | NE;
        private const byte NE_E_SE = NE | E | SE;
        private const byte S_SE = S | SE;
        private const byte NW_W_SW = NW | W | SW;
        private const byte S_SW = S | SW;

        private class PortalHolder
        {
            public Portal portal;
            public int key;
            public int arrayIndex;
            public Vector2D pos;
        }

        public void ConnectInternalPortals(Cell[,] cells, ref Portal[] portals, int chunkIdX, int chunkIdY)
        {
            List<PortalHolder> portalsHolder = new List<PortalHolder>();
            GetAllPortalsInChunk(portals, chunkIdX, chunkIdY, portalsHolder);
            Vector2D min = new Vector2D(chunkIdX * MainWindowViewModel.ChunkSize, chunkIdY * MainWindowViewModel.ChunkSize);
            Vector2D max = new Vector2D(min.x + MainWindowViewModel.ChunkSize, min.y + MainWindowViewModel.ChunkSize);
            //here we could also cache the path
            for (int i = 0; i < portalsHolder.Count - 1; i++)
            {
                for (int j = i + 1; j < portalsHolder.Count; j++)
                {
                    float cost = AStarOnlyCost.FindPath(cells, portalsHolder[i].pos, portalsHolder[j].pos, min, max);
                    if (cost < 0) continue; 
                    var portalHolder1 = portalsHolder[i];
                    var portalHolder2 = portalsHolder[j];
                    ref var internalPortalConnection1 = ref portalHolder1.portal.internalPortalConnections[portalHolder1.arrayIndex];
                    ref var internalPortalConnection2 = ref portalHolder2.portal.internalPortalConnections[portalHolder2.arrayIndex];
                    internalPortalConnection1.cost = (byte)cost;
                    internalPortalConnection1.portal = (byte)(portalHolder2.key % MainWindowViewModel.MaxPortalsInChunk);
                    internalPortalConnection2.cost = (byte)cost;
                    internalPortalConnection2.portal = (byte)(portalHolder1.key % MainWindowViewModel.MaxPortalsInChunk);
                    portalHolder1.arrayIndex++;
                    portalHolder2.arrayIndex++;
                }
            }

            
           
        }

        private static void GetAllPortalsInChunk(Portal[] portals, int chunkIdX, int chunkIdY, List<PortalHolder> portalsHolder)
        {
            int chunkId = chunkIdX + MainWindowViewModel.ChunkMapSize * chunkIdY;
            foreach (var direction in Enum.GetValues(typeof(Directions)).Cast<Directions>())
            {
                for (int i = 0; i < MainWindowViewModel.ChunkSize; i++)
                {
                    int key = Portal.GeneratePortalKey(chunkId, i, direction);
                    if(portals[key] == null)
                        continue;
                    var portalHolder = new PortalHolder
                    {
                        portal = portals[key],
                        key = key,
                        arrayIndex = 0,
                        pos = Portal.PortalKeyToWorldPos(key)
                    };
                    portalsHolder.Add(portalHolder);
                }
            }
        }
        
        public void RebuildAllPortals(Cell[,] cells, ref Portal[] portals, int chunkIdX, int chunkIdY)
        {
            int chunkId = chunkIdX + MainWindowViewModel.ChunkMapSize * chunkIdY;
            foreach (var direction in Enum.GetValues(typeof(Directions)).Cast<Directions>())
            {
                RebuildPortalsInDirection(direction, cells, ref portals, chunkIdX, chunkIdY, chunkId);
            }
        }
        
        private void RebuildPortalsInDirection(Directions dir, Cell[,] cells, ref Portal[] portals, int chunkIdX, int chunkIdY, int chunkId)
        {
           
            int startX;
            int startY;
            byte[] dirToCheck;
            Vector2D steppingInDirVector;
            Vector2D otherCellToCheck;
            byte checkDiagonalChunk;

            switch (dir)
            {
                case Directions.S:
                    startX = chunkIdX * MainWindowViewModel.ChunkSize;
                    startY = MainWindowViewModel.ChunkSize * chunkIdY + MainWindowViewModel.ChunkSize - 1;
                    dirToCheck = new [] { SW_S_SE, S, E_SE, SW, W, SE, E_NE, E};
                    steppingInDirVector = new Vector2D(1, 0);
                    otherCellToCheck = new Vector2D(0, 1);
                    checkDiagonalChunk = SE; 
                    break;
                case Directions.N:
                    startX = chunkIdX * MainWindowViewModel.ChunkSize;
                    startY =  chunkIdY * MainWindowViewModel.ChunkSize;
                    dirToCheck = new [] { NW_N_NE, N, E_NE, NW, W, NE, E_SE, E};
                    steppingInDirVector = new Vector2D(1, 0);
                    otherCellToCheck = new Vector2D(0, -1);
                    checkDiagonalChunk = NW;
                    break;
                case Directions.E:
                    startX = chunkIdX * MainWindowViewModel.ChunkSize + MainWindowViewModel.ChunkSize - 1;
                    startY = chunkIdY * MainWindowViewModel.ChunkSize;
                    dirToCheck = new [] { NE_E_SE, E, S_SE, NE, N, SE, S_SW, S};
                    steppingInDirVector = new Vector2D(0, 1);
                    otherCellToCheck = new Vector2D(1, 0);
                    checkDiagonalChunk = NE;
                    break;
                case Directions.W:
                    startX = chunkIdX * MainWindowViewModel.ChunkSize;
                    startY = chunkIdY * MainWindowViewModel.ChunkSize;
                    dirToCheck = new [] { NW_W_SW, W, S_SW, NW, N, SW, S_SE, S};
                    steppingInDirVector = new Vector2D(0, 1);
                    otherCellToCheck = new Vector2D(-1, 0);
                    checkDiagonalChunk = SW;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dir), dir, null);
            }
            
            CreatePortalsInChunkDirection(cells, ref portals, chunkId, startX, startY, dir, steppingInDirVector, dirToCheck, otherCellToCheck);
            CreatePortalForDiagonalChunk(cells, ref portals, chunkId, startX, startY, dir, steppingInDirVector, checkDiagonalChunk);
        }

        private void CreatePortalForDiagonalChunk(Cell[,] cells, ref Portal[] portals, int chunkId, int startX, int startY, Directions dir, Vector2D steppingInDirVector, byte checkDiagonalConnection)
        {
            int portalPos = 0;
            int otherPortalOffset;
            if (dir == Directions.S || dir == Directions.W)
            {
                portalPos = MainWindowViewModel.ChunkSize - 1;
                startX += steppingInDirVector.x * portalPos;
                startY += steppingInDirVector.y * portalPos;
                otherPortalOffset = 1;
            }
            else
            {
                otherPortalOffset = -1;
            }
            
            ref Cell cell = ref cells[startY, startX];
            Vector2D startPos = new Vector2D(startX, startY);
            int portalSize = 1;
            int offsetStart = 0;
            int offsetEnd = 0;
            if ((cell.Connections & checkDiagonalConnection) == WALKABLE)
            {
                ClosePortal(ref portals, chunkId, true, ref startPos, ref portalSize, dir, portalPos, ref offsetStart, ref otherPortalOffset, ref offsetEnd);
            }
        }

        private void CreatePortalsInChunkDirection(Cell[,] cells, ref Portal[] portals, int chunkId, int startX, int startY, Directions direction, Vector2D steppingInDirVector,
        byte[] checkDir, Vector2D otherCellToCheck)
        {
            //remove old portals
            for (int i = 0; i < MainWindowViewModel.ChunkSize; i++)
            {
                int key = Portal.GeneratePortalKey(chunkId, i, direction);
                portals[key] = null;
            }
            
            //INIT VALUES
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
                //Is there no Connection in NORTH-WEST_NORTH_NORTH-EAST Direction, do nothing
                if ((cell.Connections & checkDir[0]) == checkDir[0])
                {
                    closePortal = ClosePortal(ref portals, chunkId, closePortal, ref startPos, ref portalSize, direction, portalPos, ref offsetStart, ref otherPortalOffset, ref offsetEnd);
                    continue;
                }
                
                    
                // Check Connection to NORTH
                if ((cell.Connections & checkDir[1]) == WALKABLE) 
                {
                    closePortal = ClosePortal(ref portals, chunkId, closePortal, ref startPos, ref portalSize, direction, portalPos, ref offsetStart, ref otherPortalOffset, ref offsetEnd);
                    startPos = SetStartPos(startPos, cell, i, ref portalSize, ref portalPos);
                    portalSize++;   
                    //Am I at the end of my Portal in Direction
                    // Check Connection to EAST
                    // Check Connection to NORTH-EAST                    check other cell if there is a obstacle.
                    if ((cell.Connections & checkDir[2]) != WALKABLE || (cells[yCell + otherCellToCheck.y, xCell + otherCellToCheck.x].Connections & checkDir[6]) != WALKABLE)
                    {
                        closePortal = true;
                    }
                    continue;
                }
                
                //Check Connection to NORTH-WEST
                if ((cell.Connections & checkDir[3]) == WALKABLE)
                {
                    //Is there already a Portal?
                    if (closePortal)
                    {
                        //Do I belong to the Portal in the WEST
                        if ((cell.Connections & checkDir[4]) == WALKABLE)
                        {
                            offsetEnd = 1; 
                            portalSize++;
                        }
                    }
                    else
                    {//I'm my own Portal in Direction NORTH-WEST
                        closePortal = true;
                        startPos = cell.Position;
                        portalPos = i;
                        portalSize = 1;
                        otherPortalOffset = -1;
                    }
                }
                closePortal = ClosePortal(ref portals, chunkId, closePortal, ref startPos, ref portalSize, direction, portalPos, ref offsetStart, ref otherPortalOffset, ref offsetEnd);
                
                
                //Check Connection to NORTH-EAST
                if ((cell.Connections & checkDir[5]) == WALKABLE)
                {
                    //Check Connection to EAST if we can add this Tile to the new Portal
                    if ((cell.Connections & checkDir[7]) == WALKABLE)
                    {
                        startPos = cell.Position;
                        portalPos = i;
                        portalSize = 1;
                        offsetStart = 1;
                    }
                    else
                    {//I'm my own Portal in Direction NORTH-EAST
                        startPos = cell.Position;
                        portalPos = i;
                        portalSize = 1;
                        closePortal = true;
                        otherPortalOffset = 1;
                    }
                }
                
                closePortal = ClosePortal(ref portals, chunkId, closePortal, ref startPos, ref portalSize, direction, portalPos, ref offsetStart, ref otherPortalOffset, ref offsetEnd);
            }
                
            //If the portal is not closed at the end close it.
            ClosePortal(ref portals, chunkId, portalSize > 0, ref startPos, ref portalSize, direction, portalPos, ref offsetStart, ref otherPortalOffset, ref offsetEnd);
        }

        private static Vector2D SetStartPos(Vector2D startPos, Cell cell, int i, ref int portalSize, ref int portalPos)
        {
            if (startPos == null)
            { 
                portalSize = 0;
                startPos = cell.Position;
                portalPos = i;
            }

            return startPos;
        }

        private bool ClosePortal(ref Portal[] portals, int chunkId, bool closePortal, ref Vector2D startPos, ref int portalSize, Directions dir, int portalPos
            ,ref int offsetStart, ref int otherPortalOffset, ref int offsetEnd)
        {
            
            if (closePortal)
            {
                int centerPos = portalPos + offsetStart + (portalSize - offsetEnd - offsetStart) / 2;
                Portal portal = new Portal(startPos, portalSize, dir, offsetStart, offsetEnd);
                int key = Portal.GeneratePortalKey(chunkId, centerPos, dir);
                int externalKey = Portal.GetOppositePortalInOtherChunk(key, dir, centerPos, otherPortalOffset);
                if (portals[key] == null)
                {
                    portal.externalPortalConnections[0] = externalKey;
                    portals[key] = portal;
                }
                else
                {
                    for (int i = 1; i < portals[key].externalPortalConnections.Length; i++)
                    {

                        if (portals[key].externalPortalConnections[i] != -1)
                        {
                            portals[key].externalPortalConnections[i] = externalKey;
                            break;
                        }
                    }
                    
                }
                startPos = null;
                portalSize = 0;
                offsetStart = 0;
                otherPortalOffset = 0;
                offsetEnd = 0;
            }

            return false;
        }
    }
    
}