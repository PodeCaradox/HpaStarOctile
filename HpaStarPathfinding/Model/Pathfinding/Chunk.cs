using System;
using System.Collections.Generic;
using System.Linq;
using HpaStarPathfinding.pathfinding;

namespace HpaStarPathfinding.ViewModel
{
    public class Chunk
    {
                
        private const byte NOT_WALKABLE = 0b_1;
        private const byte WALKABLE = 0b_0;
        private const byte N = 0b_0000_0001;
        private const byte NE = 0b_0000_0010;
        private const byte E = 0b_0000_0100;
        private const byte SE = 0b_0000_1000;
        private const byte S = 0b_0001_0000;
        private const byte SW = 0b_0010_0000;
        private const byte W = 0b_0100_0000;
        private const byte NW = 0b_1000_0000;
        private const byte SW_S_SE = SW | S | SE;
        private const byte W_SW = W | SW;
        private const byte NW_N_NE = NW | N | NE;
        private const byte E_NE = E | NE;
        private const byte NE_E_SE = NE | E | SE;
        private const byte S_SE = S | SE;
        private const byte NW_W_SW = NW | W | SW;
        private const byte N_NW = N | NW;

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
                    float cost = AStarCustom.FindPath(cells, portalsHolder[i].pos, portalsHolder[j].pos, min, max);
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

        public void ConnectToExternalPortals()
        {
            
        }
        
        public void RebuildPortals(Cell[,] cells, ref Portal[] portals, int chunkIdX, int chunkIdY)
        {
            int chunkId = chunkIdX + MainWindowViewModel.ChunkMapSize * chunkIdY;
            foreach (var direction in Enum.GetValues(typeof(Directions)).Cast<Directions>())
            {
                RebuildPortalsInDirection(direction, cells, ref portals, chunkIdX, chunkIdY, chunkId);
            }
        }
        
        public void RebuildPortalsInDirection(Directions dir, Cell[,] cells, ref Portal[] portals, int chunkIdX, int chunkIdY, int chunkId)
        {
           
            int startX;
            int startY;
            byte[] dirToCheck;
            Vector2D steppingInDirVector;
            byte checkDiagonalChunk;

            switch (dir)
            {
                case Directions.S:
                    startX = chunkIdX * MainWindowViewModel.ChunkSize + MainWindowViewModel.ChunkSize - 1;
                    startY = MainWindowViewModel.ChunkSize * chunkIdY + MainWindowViewModel.ChunkSize - 1;
                    dirToCheck = new [] { SW_S_SE, S, W_SW, SE, E, SW, W};
                    steppingInDirVector = new Vector2D(-1, 0);
                    checkDiagonalChunk = SE; 
                    break;
                case Directions.N:
                    startX = chunkIdX * MainWindowViewModel.ChunkSize;
                    startY = MainWindowViewModel.ChunkSize * chunkIdY;
                    dirToCheck = new [] { NW_N_NE, N, E_NE, NW, W, NE, E};
                    steppingInDirVector = new Vector2D(1, 0);
                    checkDiagonalChunk = NW;
                    break;
                case Directions.E:
                    startX = chunkIdX * MainWindowViewModel.ChunkSize + MainWindowViewModel.ChunkSize - 1;
                    startY = MainWindowViewModel.ChunkSize * chunkIdY;
                    dirToCheck = new [] { NE_E_SE, E, S_SE, NE, N, SE, S};
                    steppingInDirVector = new Vector2D(0, 1);
                    checkDiagonalChunk = NE;
                    break;
                case Directions.W:
                    startX = chunkIdX * MainWindowViewModel.ChunkSize;
                    startY = MainWindowViewModel.ChunkSize * chunkIdY + MainWindowViewModel.ChunkSize - 1;
                    dirToCheck = new [] { NW_W_SW, W, N_NW, SW, S, NW, N};
                    steppingInDirVector = new Vector2D(0, -1);
                    checkDiagonalChunk = SW;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dir), dir, null);
            }
            
            CreatePortalsInChunkDirection(cells, ref portals, chunkId, startX, startY, dir, steppingInDirVector, dirToCheck);
            CreatePortalForDiagonalChunk(cells, ref portals, chunkId, startX, startY, dir, checkDiagonalChunk);
        }

        private void CreatePortalForDiagonalChunk(Cell[,] cells, ref Portal[] portals, int chunkId, int startX, int startY, Directions dir, byte checkDiagonalConnection)
        {
            ref Cell cell = ref cells[startY, startX];
            Vector2D startPos = new Vector2D(startX, startY);
            int portalSize = 1;
            if ((cell.Connections & checkDiagonalConnection) == WALKABLE)
            {
                ClosePortal(ref portals, chunkId, true, ref startPos, ref portalSize, dir, 0);
            }
        }

        private void CreatePortalsInChunkDirection(Cell[,] cells, ref Portal[] portals, int chunkId, int startX, int startY, Directions direction, Vector2D steppingInDirVector,
        byte[] checkDir)
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
            for (int i = 0; i < MainWindowViewModel.ChunkSize; i++)
            {
                ref Cell cell = ref cells[startY + steppingInDirVector.y * i, startX + steppingInDirVector.x * i];
                //Is there no Connection in NORTH-WEST_NORTH_NORTH-EAST Direction, do nothing
                if ((cell.Connections & checkDir[0]) == checkDir[0])
                {
                    closePortal = ClosePortal(ref portals, chunkId, closePortal, ref startPos, ref portalSize, direction, portalPos);
                    continue;
                }

                if (startPos == null)
                { 
                    portalSize = 0;
                    startPos = cell.Position;
                    portalPos = i;
                }
                    
                // Check Connection to NORTH
                if ((cell.Connections & checkDir[1]) == WALKABLE) 
                {
                    closePortal = ClosePortal(ref portals, chunkId, closePortal, ref startPos, ref portalSize, direction, portalPos);
                    portalSize++; 
                        
                    //Am I at the end of my Portal in Direction EAST
                    // Check Connection to EAST
                    // Check Connection to NORTH-EAST
                    if ((cell.Connections & checkDir[2]) != WALKABLE)
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
                            portalSize++;
                        }
                    }
                    else
                    {//I'm my own Portal in Direction NORTH-WEST
                        closePortal = true;
                        startPos = cell.Position;
                        portalPos = i;
                        portalSize = 1;
                    }
                }
                closePortal = ClosePortal(ref portals, chunkId, closePortal, ref startPos, ref portalSize, direction, portalPos);
                
                
                //Check Connection to NORTH-EAST
                if ((cell.Connections & checkDir[5]) == WALKABLE)
                {
                    //Check Connection to EAST if we can add this Tile to the new Portal
                    if ((cell.Connections & checkDir[6]) == WALKABLE)
                    {
                        startPos = cell.Position;
                        portalPos = i;
                        portalSize = 1;
                    }
                    else
                    {//I'm my own Portal in Direction NORTH-EAST
                        startPos = cell.Position;
                        portalPos = i;
                        portalSize = 1;
                        closePortal = true;
                    }
                }
                
                closePortal = ClosePortal(ref portals, chunkId, closePortal, ref startPos, ref portalSize, direction, portalPos);
            }
                
            //If the portal is not closed at the end close it.
            ClosePortal(ref portals, chunkId, portalSize > 0, ref startPos, ref portalSize, direction, portalPos);
        }

        private bool ClosePortal(ref Portal[] portals, int chunkId, bool closePortal, ref Vector2D startPos, ref int portalSize, Directions dir, int portalPos)
        {
            if (closePortal)
            {
                Portal portal = new Portal(startPos, portalSize, dir);
                int key = Portal.GeneratePortalKey(chunkId, portalPos + portalSize / 2, dir);
                portals[key] = portal;
                startPos = null;
                portalSize = 0;
            }

            return false;
        }
    }
    
}