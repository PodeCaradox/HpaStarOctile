using System;
using System.Windows.Media;

namespace HpaStarPathfinding.ViewModel
{
    public static class Algorithm
    {
    public static AlgorithmSelection AStar = new AlgorithmSelection(Brushes.Yellow, "A Star");
    public static AlgorithmSelection HPAStar = new AlgorithmSelection(Brushes.Red, "HPA Star");
    }

    public class AlgorithmSelection(Brush brush, string name)
    {
        public Brush Brush { get; } = brush;
        public string Name { get; } = name;
    }
}