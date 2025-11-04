using System;
using System.Windows.Media;

namespace HpaStarPathfinding.ViewModel
{
    public static class Algorithm
    {
    public static AlgorithmSelection AStar = new AlgorithmSelection(){Brush = Brushes.Yellow, Name = "A Star"};
    public static AlgorithmSelection HPAStar = new AlgorithmSelection(){Brush = Brushes.Red, Name = "HPA Star"};
    }

    public class AlgorithmSelection
    {
        public Brush Brush { get; set; }
        public String Name { get; set; }
    }
}