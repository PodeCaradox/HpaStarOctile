﻿using System;
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
        private const byte SW_S_SE = SW | S | SE;
        private const byte W_SW = W | SW;
        private const byte NW_N_NE = NW | N | NE;
        private const byte E_NE = E | NE;
        private const byte NE_E_SE = NE | E | SE;
        private const byte S_SE = S | SE;
        private const byte NW_W_SW = NW | W | SW;
        private const byte N_NW = N | NW;
        
        //maxMapSize is 1270 x 1270
        public List<Portal> portals = new List<Portal>();//Hashmap
        //public List<int> portalsHashes = new List<int>();//HashValue With 1 Bit Direction, 4 Bits Length, 4 bits posX and 4 bits posY and 7 bits chunkId = 20 bits
        
        
        //5 bits position + direction -> direction = /10 und position % 10
        //12 bits chunkId           17 Bits = 131071 Einräge
        //or 13 bits chunkId
        public Chunk()
        {
        }
        
        public void RebuildPortals(Cell[,] cells, int chunkIdX, int chunkIdY)
        {
            portals = new List<Portal>();
            foreach (var direction in Enum.GetValues(typeof(Directions)).Cast<Directions>())
            {
                RebuildPortalsInDirection(direction, cells, chunkIdX, chunkIdY);
            }
        }
        
        public void RebuildPortalsInDirection(Directions dir, Cell[,] cells, int chunkIdX, int chunkIdY)
        {
            
            if (dir == Directions.N)
            {
                int startX = chunkIdX * MainWindowViewModel.ChunkSize;
                int startY = MainWindowViewModel.ChunkSize * chunkIdY;
                byte[] dirToCheck = { NW_N_NE, N, E_NE, NW, W, NE, E};
                CheckInDirection(cells, startX, startY, Directions.N, new Vector2D(1, 0), dirToCheck);
            }
            else if (dir == Directions.E)
            {
                int startX = chunkIdX * MainWindowViewModel.ChunkSize + MainWindowViewModel.ChunkSize - 1;
                int startY = MainWindowViewModel.ChunkSize * chunkIdY;
                byte[] dirToCheck = { NE_E_SE, E, S_SE, NE, N, SE, S};
                CheckInDirection(cells, startX, startY, Directions.E, new Vector2D(0, 1), dirToCheck);
            }            
            else if (dir == Directions.S)
            {
                int startX = chunkIdX * MainWindowViewModel.ChunkSize + MainWindowViewModel.ChunkSize - 1;
                int startY = MainWindowViewModel.ChunkSize * chunkIdY + MainWindowViewModel.ChunkSize - 1;
                byte[] dirToCheck = { SW_S_SE, S, W_SW, SE, E, SW, W};
                CheckInDirection(cells, startX, startY, Directions.S, new Vector2D(-1, 0), dirToCheck);
            }
            else if (dir == Directions.W)
            {
                int startX = chunkIdX * MainWindowViewModel.ChunkSize;
                int startY = MainWindowViewModel.ChunkSize * chunkIdY + MainWindowViewModel.ChunkSize - 1;
                byte[] dirToCheck = { NW_W_SW, W, N_NW, SW, S, NW, N};
                CheckInDirection(cells, startX, startY, Directions.W, new Vector2D(0, -1), dirToCheck);
            }
        }

        private void CheckInDirection(Cell[,] cells, int startX, int startY, Directions direction, Vector2D directionVector,
        byte[] checkDir)
        {
            //INIT VALUES
            bool closePortal = false;
            int portalSize = 0;
            Vector2D startPos = null;
            for (int i = 0; i < MainWindowViewModel.ChunkSize; i++)
            {
                ref Cell cell = ref cells[startY + directionVector.y * i, startX + directionVector.x * i];
                //Is there no Connection in NORTH-WEST_NORTH_NORTH-EAST Direction, do nothing
                if ((cell.Connections & checkDir[0]) == checkDir[0])
                {
                    closePortal = ClosePortal(closePortal, ref startPos, ref portalSize, direction);
                    continue;
                }

                if (startPos == null)
                { 
                    portalSize = 0;
                    startPos = cell.Position;
                }
                    
                // Check Connection to NORTH
                if ((cell.Connections & checkDir[1]) == WALKABLE) 
                {
                    closePortal = ClosePortal(closePortal, ref startPos, ref portalSize, direction);
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
                        portalSize = 1;
                    }
                }
                closePortal = ClosePortal(closePortal, ref startPos, ref portalSize, direction);
                
                //Check Connection to NORTH-EAST
                if ((cell.Connections & checkDir[5]) == WALKABLE)
                {
                    //Check Connection to EAST if we can add this Tile to the new Portal
                    if ((cell.Connections & checkDir[6]) == WALKABLE)
                    {
                        startPos = cell.Position;
                        portalSize = 1;
                    }
                    else
                    {//I'm my own Portal in Direction NORTH-EAST
                        startPos = cell.Position;
                        portalSize = 1;
                        closePortal = true;
                    }
                }
                
                closePortal = ClosePortal(closePortal, ref startPos, ref portalSize, direction);
            }
                
            //If the portal is not closed at the end close it.
            ClosePortal(portalSize > 0, ref startPos, ref portalSize, direction);
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