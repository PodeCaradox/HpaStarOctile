using System.Collections.Generic;
using System.Linq;

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
        private const byte SE_S_SW = SE | S | SW;
        private const byte E_SE = E | SE;
        
        //maxMapSize is 1270 x 1270
        public int ChunkIdX;
        public int ChunkIdY;
        public List<Portal> portals = new List<Portal>();//Hashmap
        //public List<int> portalsHashes = new List<int>();//HashValue With 1 Bit Direction, 4 Bits Length, 4 bits posX and 4 bits posY and 7 bits chunkId = 20 bits

        public Chunk(int x, int y)
        {
            ChunkIdX = x;
            ChunkIdY = y;
        }
        
        public void RebuildPortals(Cell[,] cells)
        {
            portals = new List<Portal>();
            foreach (var direction in Directions.GetValues(typeof(Directions)).Cast<Directions>())
            {
                RebuildPortalsInDirection(direction, cells);
            }
        }
        
        public void RebuildPortalsInDirection(Directions dir, Cell[,] cells)
        {
            if (dir == Directions.S)
            {
                //I don't need to check the last Chunk.
                int idX = ChunkIdX * MainWindowViewModel.ChunkSize;
                int idY = MainWindowViewModel.ChunkSize * ChunkIdY + MainWindowViewModel.ChunkSize - 1;
                if (idY + MainWindowViewModel.ChunkSize > cells.GetLength(0) - 1)
                {
                    return;
                }
                
                //INIT VALUES
                bool closePortal = false;
                int portalSize = 0;
                Vector2D startPos = null;
                for (int x = 0; x < MainWindowViewModel.ChunkSize; x++)
                {
                    ref Cell cell = ref cells[idY, idX + x];
                    //Is there no Connection in SOUTH/SOUTH-WEST/SOUTH-EAST Direction, do nothing
                    if ((cell.Connections & SE_S_SW) == SE_S_SW)
                    {
                        closePortal = ClosePortal(closePortal, ref startPos, ref portalSize, Directions.E);
                        continue;
                    }

                    if (startPos == null)
                    { 
                        portalSize = 0;
                        startPos = cell.Position;
                    }
                    
                    // Check Connection to South
                    if ((cell.Connections & S) == WALKABLE) 
                    {
                        closePortal = ClosePortal(closePortal, ref startPos, ref portalSize, Directions.E);
                        portalSize++; 
                        
                        //Am i at the end of my Portal in Direction EAST
                        // Check Connection to EAST
                        // Check Connection to SOUTH-EAST
                        if ((cell.Connections & E_SE) != WALKABLE)
                        {
                            closePortal = true;
                        }
                        continue;
                    }
                    
                    if ((cell.Connections & SW) == WALKABLE)
                    {
                        //There is already a Portal and I'm belong to it
                        if (closePortal)
                        {
                            if ((cell.Connections & W) == WALKABLE)
                            {
                                portalSize++;
                            }
                        }
                        else
                        {//I'm my own Portal in Direction South West
                            closePortal = true;
                            startPos = cell.Position;
                            portalSize = 1;
                        }
                    }
                    
                    closePortal = ClosePortal(closePortal, ref startPos, ref portalSize, Directions.E);
                    
                    if ((cell.Connections & SE) == WALKABLE)
                    {
                        //Check Connection to EAST if im a new Portal in this direction
                        if ((cell.Connections & E) == WALKABLE)
                        {
                            startPos = cell.Position;
                            portalSize = 1;
                        }
                        else
                        {//I'm my own Portal
                            startPos = cell.Position;
                            portalSize = 1;
                            closePortal = true;
                        }
                    }
                    closePortal = ClosePortal(closePortal, ref startPos, ref portalSize, Directions.E);
                }
                
                //If the portal is not closed at the end close it.
                ClosePortal(portalSize > 0, ref startPos, ref portalSize, Directions.E);
            }
        }

        private bool ClosePortal(bool createPortal, ref Vector2D startPos, ref int portalSize, Directions dir)
        {
            if (createPortal)
            {
                portals.Add(new Portal(startPos, portalSize, dir));
                startPos = null;
                portalSize = 0;
            }

            return false;
        }
    }
    
}