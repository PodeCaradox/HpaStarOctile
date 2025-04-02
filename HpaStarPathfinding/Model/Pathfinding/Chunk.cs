using System.Collections.Generic;
using System.Linq;

namespace HpaStarPathfinding.ViewModel
{
    public class Chunk
    {
                
        private const byte NOT_WALKABLE = 0b_1;
        private const byte WALKABLE = 0b_0;
        private const byte BLOCKED = 0b_1111_1111;
        private const byte FREE = 0b_0000_0000;
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
                int portalSize = 0;
                Vector2D startPos = null;
                int idX = ChunkIdX * MainWindowViewModel.ChunkSize;
                int idY = MainWindowViewModel.ChunkSize * ChunkIdY + MainWindowViewModel.ChunkSize - 1;
                if (idY + MainWindowViewModel.ChunkSize > cells.GetLength(0) - 1)
                {
                    return;
                }
                for (int x = 0; x < MainWindowViewModel.ChunkSize; x++)
                {
                    ref Cell cell = ref cells[idY, idX + x];
                    //check border
                    ref Cell oherCell = ref cells[idY + 1, idX + x];
                    if (!cell.Walkable && !oherCell.Walkable)
                    {//cell.Connections == BLOCKED 
                        portalSize = 0;
                        startPos = null;
                        continue;
                    }

                    if (cell.Walkable && oherCell.Walkable)
                    {
                        if (portalSize == 0)
                        {
                            startPos = cell.Position;
                        }
                        portalSize++; 
                        
                        if (x + 1 >= MainWindowViewModel.ChunkSize)
                        {
                            if (startPos != null)
                            {
                                portals.Add(new Portal(startPos, portalSize, Directions.E));
                            }

                            break;
                        }
                        
                        ref Cell cellNew = ref cells[idY, idX + x + 1];
                        if (!cellNew.Walkable)
                        {
                            portals.Add(new Portal(startPos, portalSize, Directions.E));
                            portalSize = 0;
                            startPos = null;
                            continue;
                        }
                        
                        ref Cell oherCellNew = ref cells[idY + 1, idX + x + 1];
                        if (!oherCellNew.Walkable)
                        {
                            portalSize++;
                            portals.Add(new Portal(startPos, portalSize, Directions.E));
                            portalSize = 0;
                            startPos = null;
                        }

                    }
                    else if (cell.Walkable)//Schritt 2
                    {
                        //beende altes portal da hinderniss
                        if (startPos != null)
                        {
                            portals.Add(new Portal(startPos, portalSize, Directions.E));
                            startPos = null;
                            portalSize = 0;
                        }
                        
                        //falls am ende angekommen fertig
                        if (x + 1 >= MainWindowViewModel.ChunkSize)
                        {
                            break;
                        }
                        ref Cell oherCellNew = ref cells[idY + 1, idX + x + 1];
                        
                        if (oherCellNew.Walkable)
                        {
                            startPos = cell.Position;
                            portalSize = 1;
                            
                            //Schritt 3
                            ref Cell cellNew = ref cells[idY, idX + x + 1];
                            if (!cellNew.Walkable)
                            {
                                portals.Add(new Portal(startPos, portalSize, Directions.E));
                                startPos = null;
                                portalSize = 0;
                            }
                        }
                        
                       
                    }
                    else if (oherCell.Walkable)//Schritt 2
                    {
                        //beende altes portal da hinderniss
                        if (startPos != null)
                        {
                            portals.Add(new Portal(startPos, portalSize, Directions.E));
                            startPos = null;
                            portalSize = 0;
                        }
                        
                        //falls am ende angekommen fertig
                        if (x + 1 >= MainWindowViewModel.ChunkSize)
                        {
                            break;
                        }
                        ref Cell cellNew = ref cells[idY, idX + x + 1];
                        ref Cell oherCellNew = ref cells[idY + 1, idX + x + 1];
                        if (cellNew.Walkable && !oherCellNew.Walkable) 
                        {
                            startPos = cellNew.Position;
                            portalSize = 1;
                        }
                    }
                }
            }
        }
    }
    
}