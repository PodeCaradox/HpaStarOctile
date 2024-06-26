﻿using System.Windows;

namespace HpaStarPathfinding.ViewModel
{
    public class Vector2D
    {
        public Vector2D(int x, int y)
        {
            X = x;
            Y = y;
        }
        

        public int X { get; set; }
        public int Y { get; set; }
        
        
        public static Vector2D ConvertVector2DToMapPosCentered(Vector2D point)
        {
            int x = point.X * MainWindow.CellSize + MainWindow.CellSize / 2;
            int y = point.Y * MainWindow.CellSize + MainWindow.CellSize / 2;
            return new Vector2D(x, y);
        }
        
        public static Vector2D ConvertPointToIndex(Point point)
        {
            int x = (int)point.X / MainWindow.CellSize;
            int y = (int)point.Y / MainWindow.CellSize;
            return new Vector2D(x, y);
        }
    }
}