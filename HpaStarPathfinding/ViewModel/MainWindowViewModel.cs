using System.Collections.Generic;

namespace HpaStarPathfinding.ViewModel
{
    public class MainWindowViewModel: ViewModelBase
    {
        public static int GridSize = 20;
        public static int ChunkSize = 10;
        
        #region Propertys UI

        private bool changeStart = false;

        public bool ChangeStart
        {
            get { return changeStart; }
            set
            {
                if (value != changeStart)
                {
                    if (value) ChangeEnd = false;
                    changeStart = value;
                    OnPropertyChanged();
                }
            }
        }
        
        private bool changeEnd = false;

        public bool ChangeEnd
        {
            get => changeEnd;
            set
            {
                if (value == changeEnd) return;
                if (value) ChangeStart = false;
                changeEnd = value;
                OnPropertyChanged();
            }
        }
        
        #endregion
        
        #region Propertys
        
        private Vector2D pathEnd;

        public Vector2D PathEnd
        {
            get => pathEnd;
            set
            {
                if (value == pathEnd) return;
                pathEnd = value;
                OnPropertyChanged();
            }
        }
        
        private List<Vector2D> path = new List<Vector2D>();

        public List<Vector2D> Path
        {
            get => path;
            set
            {
                if (value == path) return;
                path = value;
                OnPropertyChanged();
            }
        }
        
        private Vector2D pathStart;

        public Vector2D PathStart
        {
            get => pathStart;
            set
            {
                if (value == pathStart) return;
                pathStart = value;
                OnPropertyChanged();
            }
        }
        
        private Cell[,] cells;

        public Cell[,] Cells
        {
            get => cells;
            set
            {
                if (value == cells) return;
                cells = value;
                OnPropertyChanged();
            }
        }
        
        private Chunk[,] chunks;

        public Chunk[,] Chunks
        {
            get => chunks;
            set
            {
                if (value == chunks) return;
                chunks = value;
                OnPropertyChanged();
            }
        }
        
        #endregion

        public bool PathPointIsWall(Vector2D vector2D)
        {
            return cells[vector2D.X, vector2D.Y].wall;
        }

        public void Init()
        {
            Cells = new Cell[GridSize, GridSize];
            Chunks = new Chunk[GridSize / ChunkSize, GridSize / ChunkSize];
            Path = new List<Vector2D>();
            
            Path.Add(new Vector2D(2,2));
            Path.Add(new Vector2D(2,4));
            Path.Add(new Vector2D(4,4));
            Path.Add(new Vector2D(6,4));
            Path.Add(new Vector2D(6,10));

        }
    }
}