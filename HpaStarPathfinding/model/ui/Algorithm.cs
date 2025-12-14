
using System.Windows.Media;

namespace HpaStarPathfinding.model.ui;

public static class Algorithm
{
    public static readonly AlgorithmSelection AStar = new (Brushes.Yellow, "A Star");
    public static readonly AlgorithmSelection HPAStar = new (Brushes.Red, "HPA Star");
}

public class AlgorithmSelection(Brush brush, string name)
{
    public Brush Brush { get; } = brush;
    public string Name { get; } = name;
}