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
            get { return changeEnd; }
            set
            {
                if (value != changeEnd)
                {
                    if (value) ChangeStart = false;
                    changeEnd = value;
                    OnPropertyChanged();
                }
            }
        }
        
        private Cell[,] cells;

        public Cell[,] Cells
        {
            get { return cells; }
            set
            {
                if (value != cells)
                {
                    cells = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        public bool PathPointIsWall(Vector2D vector2D)
        {
            return cells[vector2D.X, vector2D.Y].Wall;
        }
    }
}