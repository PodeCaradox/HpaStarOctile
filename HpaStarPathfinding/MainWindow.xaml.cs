using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using HpaStarPathfinding.Model;
using HpaStarPathfinding.ViewModel;
using static HpaStarPathfinding.ViewModel.MainWindowViewModel;

namespace HpaStarPathfinding
{
    
    public partial class MainWindow
    {
        #region Properties

        private const byte NOT_WALKABLE = 0b_1;
        private const byte BLOCKED = 0b_1111_1111;
        private const byte WALKABLE = 0b_0000_0000;
        private bool drawPortals;
        private bool drawPortalsInternalConnections;
        private bool drawPortalsExternalConnections;

        private Rectangle[,] _chunks;

        private Dictionary<int, (Rectangle, Rectangle)> _portals;

        private readonly List<Line> _lines = new List<Line>();

        private readonly CircleUi[] _pathStartEnd = { new CircleUi(Brushes.Green), new CircleUi(Brushes.Red) };

        private MainWindowViewModel _vm;

        private HashSet<Vector2D> _dirtyChunks;
        private HashSet<Vector2D> _dirtyTiles;

        private readonly List<Line> _portalInternalConnections = new List<Line>();
        private readonly List<Line> _portalExternalConnections = new List<Line>();
        private readonly List<Line> _hoverPortalConnections = new List<Line>();
        private readonly List<Rect> _bordersInCell = new List<Rect>();
        
        private Rectangle _selectionRectangle;

        private int currentPortal = -1;
        
        private Image[,] _mapUi;
        
        
        
        #endregion

        #region Constructor

        public MainWindow()
        {
            DataContextChanged += OnDataContextChanged;
            InitializeComponent();
            PathfindingWindow.SizeToContent = SizeToContent.Width;
            
            PathCanvas.Height = cellSize * MapSize;
            PathCanvas.Width = cellSize * MapSize;
            PathCanvas.MouseDown += MapOnMouseButtonDown;
            InitBitmaps();
            Init();
            AccessKeyManager.Register("t", ChangePathToggleButton);
            AccessKeyManager.Register("c", ClearPathToggleButton);
            AccessKeyManager.Register("p", PathfindingAlgorithmComboBox);
            AccessKeyManager.AddAccessKeyPressedHandler(PathfindingAlgorithmComboBox, ChangePathfindingAlgorithm);
        }

       

        #endregion

        #region Init
        
        private void InitBitmaps()
        {
            _bordersInCell.Add(new Rect(5, 1, 10, 2));   // N,
            _bordersInCell.Add(new Rect(15, 1, 4, 4));   // NE
            _bordersInCell.Add(new Rect(17, 5, 2, 10));  // E, 
            _bordersInCell.Add(new Rect(15, 15, 4, 4));  // SO
            _bordersInCell.Add(new Rect(5, 17, 10, 2));  // S, 
            _bordersInCell.Add(new Rect(1, 15, 4, 4));   // SW
            _bordersInCell.Add(new Rect(1, 5, 2, 10));   // W, 
            _bordersInCell.Add(new Rect(1, 1, 4, 4));    // NW
            WriteableBitmap[] bmps = new WriteableBitmap[byte.MaxValue + 1];
            int stride = cellSize;
            for (int i = 0; i <= byte.MaxValue; i++)
            {
                bmps[i] = new WriteableBitmap(cellSize, cellSize, 100, 100, PixelFormats.Gray8, null);
               
                byte[] pixelData = Enumerable.Repeat(byte.MaxValue, stride * cellSize).ToArray();

                //Borders
                for (int j = 0; j < DirectionsAsByte.AllDirectionsAsByte.Length; j++)
                {
                    if ((i & DirectionsAsByte.AllDirectionsAsByte[j]) == DirectionsAsByte.AllDirectionsAsByte[j])
                    {
                        WritePixelData(ref pixelData, (int)_bordersInCell[j].X, (int)_bordersInCell[j].Y, (int)_bordersInCell[j].Width, (int)_bordersInCell[j].Height);
                    }
                }
                
                bmps[i].WritePixels(new Int32Rect(0, 0, cellSize, cellSize), pixelData, stride, 0);
            }
            
            _vm.CellStates = bmps;
        }

        private void WritePixelData(ref byte[] pixelData, int xOffset, int yOffset, int width, int height)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    pixelData[(y + yOffset) * cellSize + x + xOffset] = 0;
                }
            }
        }

        private void Init()
        {
            PathCanvas.IsEnabled = false;
            _vm.Init();
            
            _mapUi = new Image[MapSize, MapSize];
            InitializeGridMap();
            InitializeGridChunks();
            InitializePortals();
            _selectionRectangle = new Rectangle
            {
                StrokeThickness = 2,
                SnapsToDevicePixels = true,
                StrokeDashArray= new DoubleCollection(new[]{ 1.0, 1.0 }),
                Width = cellSize,
                Height = cellSize,
                Stroke = Brushes.Gold,
                Fill = Brushes.Transparent,
                Visibility = Visibility.Hidden,
                IsHitTestVisible = false,
                IsManipulationEnabled = false,
                IsEnabled = false
                
                
            };
            PathCanvas.Children.Add(_selectionRectangle);

            _dirtyTiles = new HashSet<Vector2D>();
            _dirtyChunks = new HashSet<Vector2D>();
            PathCanvas.IsEnabled = true;
        }


        private void InitializePortals()
        {
            _portals = new Dictionary<int, (Rectangle, Rectangle)>();
            Parallel.For(0, _vm.chunks.GetLength(0), y =>
            {
                for (int x = 0; x < _vm.chunks.GetLength(1); x++)
                {
                    ref Chunk chunk = ref _vm.chunks[y, x];
                    chunk.RebuildAllPortals(_vm.Map, ref _vm.Portals, x, y);
                    chunk.ConnectInternalPortals(_vm.Map, ref _vm.Portals, x, y);
                }
            });

            UpdatePortalsOnCanvas();
        }

        private void UpdatePortalsOnCanvas()
        {
            for (int y = 0; y < _vm.chunks.GetLength(0); y++)
            {
                for (int x = 0; x < _vm.chunks.GetLength(1); x++)
                {
                    ref Chunk chunk = ref _vm.chunks[y, x];
                    int chunkId = x + y * ChunkMapSize;
                    CreatePortalsOnCanvas(chunk, chunkId);
                }
            }
        }

        private void CreatePortalsOnCanvas(Chunk chunk, int chunkId)
        {
            double opacity = 0.0;
            if (drawPortals)
            {
                opacity = 1.0;
            }

            var brushes = new Brush[] { Brushes.Red, Brushes.Green, Brushes.Blue, Brushes.Violet };
            int i = 0; //Looking in Directions.N        Directions.E        Directions.S        Directions.W
            var steppingVector = new[]
                { DirectionsVector.E, DirectionsVector.S, DirectionsVector.E, DirectionsVector.S };

            foreach (Directions dirVec in Enum.GetValues(typeof(Directions)))
            {
                for (int j = 0; j < ChunkSize; j++)
                {
                    int key = Portal.GeneratePortalKey(chunkId, j, dirVec);
                    var portal = _vm.Portals[key];
                    if (portal == null) continue;
                    var dir = steppingVector[(int)portal.direction];

                    int startPosX = portal.startPos.x;
                    int startPosY = portal.startPos.y;
                    int centerPosX = portal.centerPos.x;
                    int centerPosY = portal.centerPos.y;
                    int width = dir.x * portal.portalLength;
                    int height = dir.y * portal.portalLength;

                    Rectangle rect = new Rectangle
                    {
                        Width = Math.Max(cellSize * (width) - 8,
                            cellSize - 8),
                        Height = Math.Max(cellSize * (height) - 8,
                            cellSize - 8),
                        Stroke = brushes[i],
                        Fill = Brushes.Transparent,
                        Opacity = opacity,
                        IsHitTestVisible = false,
                        IsManipulationEnabled = false,
                        IsEnabled = false
                    };
                    i++;
                    if (i >= brushes.Length)
                    {
                        i = 0;
                    }

                    Canvas.SetLeft(rect, startPosX * cellSize + 4);
                    Canvas.SetTop(rect, startPosY * cellSize + 4);

                    Brush color = Brushes.Green;
                    
                    Rectangle center = new Rectangle
                    {
                        Width = cellSize - 10,
                        Height = cellSize - 10,
                        Stroke = Brushes.Transparent,
                        Fill = color,
                        Opacity = opacity,
                        IsHitTestVisible = false,
                        IsManipulationEnabled = false,
                        IsEnabled = false
                    };

                    Canvas.SetLeft(center, centerPosX * cellSize + 5);
                    Canvas.SetTop(center, centerPosY * cellSize + 5);

                    _portals.Add(key, (rect, center));
                    PathCanvas.Children.Add(rect);
                    PathCanvas.Children.Add(center);
                }
            }
        }

        private void InitializeGridMap()
        {
            for (int y = 0; y < _vm.Map.GetLength(0); y++)
            {
                for (int x = 0; x < _vm.Map.GetLength(1); x++)
                {
                    var node = _vm.Map[y, x];
                    Image rect = new Image
                    {
                        Source = GetCellColor(node),
                        Width = cellSize,
                        Height = cellSize,
                        Stretch = Stretch.UniformToFill,
                        SnapsToDevicePixels = true
                    };
                    rect.MouseDown += MapCellMouseButtonDown;
                    rect.MouseEnter += MapCellOnMouseEnter;
                    rect.Tag = node;
                    Canvas.SetLeft(rect, x * cellSize);
                    Canvas.SetTop(rect, y * cellSize);
                    PathCanvas.Children.Add(rect);
                    _mapUi[y, x] = rect;
                }
            }
        }

        private void InitializeGridChunks()
        {
            _chunks = new Rectangle[MapSize / ChunkSize,
                MapSize / ChunkSize];
            for (int y = 0; y < MapSize / ChunkSize; y++)
            {
                for (int x = 0; x < MapSize / ChunkSize; x++)
                {
                    _vm.chunks[y, x] = new Chunk();
                    Rectangle rect = new Rectangle
                    {
                        Width = cellSize * ChunkSize - 4,
                        Height = cellSize * ChunkSize - 4,
                        Stroke = Brushes.Yellow,
                        Fill = Brushes.Transparent,
                        Opacity = 0.0,
                        IsHitTestVisible = false,
                        IsManipulationEnabled = false,
                        IsEnabled = false
                    };
                    rect.Tag = _vm.chunks[y, x];
                    Canvas.SetLeft(rect, x * cellSize * ChunkSize + 2);
                    Canvas.SetTop(rect, y * cellSize * ChunkSize + 2);
                    _chunks[y, x] = rect;
                    PathCanvas.Children.Add(rect);
                }
            }
        }

        #endregion

        #region Events
        
        private void ChangePathfindingAlgorithm(object sender, AccessKeyPressedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                int currentIndex = comboBox.SelectedIndex;
                int itemCount = comboBox.Items.Count;
                comboBox.SelectedIndex = (currentIndex + 1) % itemCount;
                e.Handled = true;
            }
        }

        private void ClearClicked(object sender, RoutedEventArgs e)
        {
            DrawChunksButton.IsChecked = false;
            DrawPortalsButton.IsChecked = false;
            drawPortals = false;
            ChangeSelection(Visibility.Hidden, null);
            PathCanvas.Children.Clear();
            Init();
        }

        private void ChangeSelection(Visibility visibility, Cell mapCell)
        {
            _selectionRectangle.Visibility = visibility;
            if (mapCell == null)
            {
                _vm.EnabledChangeCellBorderImage = false;
                _vm.CurrentSelectedCell = null;
                _vm.CurrentSelectedCellSource = null;
            }else

            {
                _vm.EnabledChangeCellBorderImage = true;
                _vm.CurrentSelectedCell = mapCell;
                _vm.CurrentSelectedCellSource = _vm.CellStates[mapCell.Connections];
            }
            
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            _vm = DataContext as MainWindowViewModel;
        }

        private void MapOnMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_vm.changePathfindingNodeEnabled)
                return;
            
            Point p = Mouse.GetPosition(PathCanvas);
            Vector2D mapPoint = Vector2D.ConvertPointToMapPoint(p);
            if (_vm.PathPointIsWall(mapPoint))
                return;

            var screenPoint = Vector2D.ConvertMapPointToCanvasPos(mapPoint);
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                ChangePathfindingStartPoint(mapPoint, screenPoint);
            }
            else if (e.RightButton == MouseButtonState.Pressed)
            {
                
                ChangePathfindingEndPoint(mapPoint, screenPoint);
            }
            CalcPath();
        }

        private void MapCellMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_vm.changePathfindingNodeEnabled)
                return;
            
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (!GetCell(sender, out Cell cell) && currentChangedCell == cell) return;
                currentChangedCell = cell;
                byte newValue = (cell.Connections == BLOCKED) ? WALKABLE : BLOCKED;
                ChangeMapCell(cell, newValue);
                
            } 
            else if (e.RightButton == MouseButtonState.Pressed)
            {
                if (!GetCell(sender, out Cell cell)) return;
                
                SelectCell(cell);
            }
        }

        private bool GetCell(object sender, out Cell cell)
        {
            cell = null;
            if (!(sender is Image image))
                return false;

            if (!(image.Tag is Cell mapCell))
                return false;

            if (mapCell.Position.Equals(_vm.pathStart) || mapCell.Position.Equals(_vm.pathEnd))
                return false;
            
            cell = mapCell;
            return true;
        }

        private void SelectCell(Cell mapCell)
        {
            
            Canvas.SetLeft(_selectionRectangle, mapCell.Position.x * cellSize);
            Canvas.SetTop(_selectionRectangle, mapCell.Position.y * cellSize);
            ChangeSelection(Visibility.Visible, mapCell);
        }

        private Cell currentChangedCell;
        private void MapCellOnMouseEnter(object sender, MouseEventArgs e)
        {
            DrawConnectionsOnHover(sender);
            if (e.LeftButton != MouseButtonState.Pressed || _vm.changePathfindingNodeEnabled)
                return;

            if (!GetCell(sender, out Cell cell) && currentChangedCell == cell) return;
            currentChangedCell = cell;
            byte newValue = (cell.Connections == BLOCKED) ? WALKABLE : BLOCKED;
            
            ChangeMapCell(cell, newValue);
        }

        private void DrawConnectionsOnHover(object sender)
        {
            if (!drawPortals || !(sender is Image rect) || !(rect.Tag is Cell mapCell))
            {
                RemoveIfNotHovered();
                return;
            }

            List<int> keys = new List<int>();
            int startX = mapCell.Position.x % ChunkSize;
            int startY = mapCell.Position.y % ChunkSize;
            int xOffset = mapCell.Position.x / ChunkSize * MaxPortalsInChunk;
            int yOffset = mapCell.Position.y / ChunkSize * MaxPortalsInChunk * ChunkMapSize;
            if(startX == 0)
            {
                var portalKey = startY + xOffset + yOffset + ChunkSize * 3;
                keys.Add(portalKey);
            } 
            
            if (startY == 0)
            {  
                var portalKey = startX + xOffset + yOffset;
                keys.Add(portalKey);
            }
            
            if (startX == 9)
            {
                var portalKey = startY + xOffset + yOffset + ChunkSize * 1;
                keys.Add(portalKey);
            }
            
            if (startY == 9)
            {
                var portalKey = startX + xOffset + yOffset + ChunkSize * 2;
                keys.Add(portalKey);
            }

            if (keys.Count == 0)
            {
                RemoveIfNotHovered();
                return;
            }
            if (currentPortal == keys[0]) return;
            currentPortal = keys[0];
            RemoveIfNotHovered();
            foreach (var key in keys)
            {
                DrawPortalConnectionsOnHover(key);
            }
           
        }

        private void RemoveIfNotHovered()
        {
            foreach (var line in _hoverPortalConnections)
            {
                PathCanvas.Children.Remove(line);
            }
            _hoverPortalConnections.Clear();
        }

        public void DrawPortalConnectionsOnHover(int key)
        {
            ref var portal = ref _vm.Portals[key];
            if (portal == null) return;
                
            int chunkIndexinPortalArray = key / MaxPortalsInChunk * MaxPortalsInChunk;
            foreach (var connection in portal.internalPortalConnections)
            {
                if (connection.portal == byte.MaxValue) break;
                    
                int keyOtherPortal = chunkIndexinPortalArray + connection.portal;
                var otherPortal = _vm.Portals[keyOtherPortal];
                var point1 = Vector2D.ConvertMapPointToCanvasPos(portal.centerPos);
                var point2 = Vector2D.ConvertMapPointToCanvasPos(otherPortal.centerPos);
                Line line = new Line
                {
                    StrokeThickness = 2,
                    X1 = point1.x,
                    X2 = point2.x,
                    Y1 = point1.y,
                    Y2 = point2.y,
                    Stroke = Brushes.CornflowerBlue,
                    IsHitTestVisible = false,
                    IsManipulationEnabled = false,
                    IsEnabled = false
                };
                _hoverPortalConnections.Add(line);
                PathCanvas.Children.Add(line);
            }
            
            foreach (var keyOtherPortal in portal.externalPortalConnections)
            {
                if (keyOtherPortal == -1) break;
                    
                var otherPortal = _vm.Portals[keyOtherPortal];
                var point1 = Vector2D.ConvertMapPointToCanvasPos(portal.centerPos);
                var point2 = Vector2D.ConvertMapPointToCanvasPos(otherPortal.centerPos);
                Line line = new Line
                {
                    StrokeThickness = 2,
                    X1 = point1.x,
                    X2 = point2.x,
                    Y1 = point1.y,
                    Y2 = point2.y,
                    Stroke = Brushes.CornflowerBlue,
                    IsHitTestVisible = false,
                    IsManipulationEnabled = false,
                    IsEnabled = false
                };
                _hoverPortalConnections.Add(line);
                PathCanvas.Children.Add(line);
            }
        }

        #endregion

        #region Methods

        private void ChangeMapCell(Cell mapCell, byte newValue)
        {
            ChangeSelection(Visibility.Hidden, null);

            mapCell.Obstacle = newValue == BLOCKED;
            mapCell.Connections = newValue;
            _vm.Map[mapCell.Position.y, mapCell.Position.x] = mapCell;

            _dirtyTiles.Add(mapCell.Position);

            Vector2D chunkPos = new Vector2D(mapCell.Position.x / ChunkSize,
                mapCell.Position.y / ChunkSize);
            _dirtyChunks.Add(chunkPos);
            foreach (var dir in DirectionsVector.AllDirections)
            {
                var pos = chunkPos + dir;
                if (pos.x >= _vm.chunks.GetLength(1)
                    || pos.y >= _vm.chunks.GetLength(0)
                    || pos.x < 0
                    || pos.y < 0) continue;
                _dirtyChunks.Add(pos);
            }


            RebuildTiles();
        }

        private void RebuildTiles()
        {
            PathCanvas.IsEnabled = false;
            foreach (var mapCellPos in _dirtyTiles)
            {
                ref Cell mapCell = ref _vm.Map[mapCellPos.y, mapCellPos.x];
                mapCell.UpdateConnection(_vm.Map);
                UpdateUICell(mapCellPos);
            }
            
            _dirtyTiles.Clear();

            RebuildPortals();

            PathCanvas.IsEnabled = true;
        }

        private void UpdateUICell(Vector2D mapCellPos)
        {
            
            _mapUi[mapCellPos.y, mapCellPos.x].Source = GetCellColor(_vm.Map[mapCellPos.y, mapCellPos.x]);
            for (byte i = 0; i < DirectionsVector.AllDirections.Length; i++)
            {
                var dirVec = DirectionsVector.AllDirections[i];
                if (mapCellPos.y + dirVec.y >= _vm.Map.GetLength(0) ||
                    mapCellPos.x + dirVec.x >= _vm.Map.GetLength(1) || mapCellPos.x + dirVec.x < 0 ||
                    mapCellPos.y + dirVec.y < 0)
                {
                    continue;
                }
                _mapUi[mapCellPos.y + dirVec.y, mapCellPos.x + dirVec.x].Source = GetCellColor(_vm.Map[mapCellPos.y + dirVec.y, mapCellPos.x + dirVec.x]);

            }
            
        }


        private void RebuildPortals()
        {
            //_timerRedrawPortals.Stop(); // Stop timer so it doesn’t repeat
            foreach (var chunk in _dirtyChunks)
            {
                RebuildPortalsInChunk(chunk);
            }
            DeletePortalInternalConnectionsDrawn();
            DeletePortalExternalConnectionsDrawn();
            DrawPortalInternalConnections();
            DrawPortalExternalConnections();
            _dirtyChunks.Clear();

            CalcPath();
        }

        private void RebuildPortalsInChunk(Vector2D chunkPos)
        {
            ref Chunk chunk = ref _vm.chunks[chunkPos.y, chunkPos.x];
            int chunkId = chunkPos.x + chunkPos.y * ChunkMapSize;
            foreach (Directions dirVec in Enum.GetValues(typeof(Directions)))
            {
                for (int j = 0; j < ChunkSize; j++)
                {
                    int key = Portal.GeneratePortalKey(chunkId, j, dirVec);
                    if (!_portals.ContainsKey(key)) continue;
                    PathCanvas.Children.Remove(_portals[key].Item1);
                    PathCanvas.Children.Remove(_portals[key].Item2);
                    _portals.Remove(key);
                }
            }

            chunk.RebuildAllPortals(_vm.Map, ref _vm.Portals, chunkPos.x, chunkPos.y);
            chunk.ConnectInternalPortals(_vm.Map, ref _vm.Portals, chunkPos.x, chunkPos.y);
            CreatePortalsOnCanvas(chunk, chunkId);

        }

        private WriteableBitmap GetCellColor(Cell mapCell)
        {
            return _vm.CellStates[mapCell.Connections];
        }

        private void CalcPath()
        {
            ResetPathUi();

            _vm.FindPath();

            if (_vm.path == null || _vm.path.Count < 2)
                return;

            var path = _vm.path.ToArray();

            
            DrawPathUi(path, _vm.SelectedAlgorithm.Brush);
            DrawPathUi(_vm.OtherPath.ToArray(), Brushes.Gray, 0.4 );
        }

        private void DrawPathUi(Vector2D[] path, Brush brush, double opacity = 1)
        {
            for (int i = 1; i < path.Length; i++)
            {
                var point1 = Vector2D.ConvertMapPointToCanvasPos(path[i - 1]);
                var point2 = Vector2D.ConvertMapPointToCanvasPos(path[i]);
                Line line = new Line
                {
                    StrokeThickness = 2,
                    X1 = point1.x,
                    X2 = point2.x,
                    Y1 = point1.y,
                    Y2 = point2.y,
                    Opacity = opacity,
                    Stroke = brush,
                              IsHitTestVisible = false,
                              IsManipulationEnabled = false,
                              IsEnabled = false
                };

                _lines.Add(line);
                PathCanvas.Children.Add(line);
            }
        }

        private void ResetPathUi()
        {
            foreach (var line in _lines)
            {
                PathCanvas.Children.Remove(line);
            }

            _lines.Clear();
        }

        private void ChangePathfindingEndPoint(Vector2D mapPoint, Vector2D screenPoint)
        {
            _vm.pathEnd = mapPoint;
            _pathStartEnd[1].ChangePosition(PathCanvas, screenPoint);
        }

        private void ChangePathfindingStartPoint(Vector2D mapPoint, Vector2D screenPoint)
        {
            _vm.pathStart = mapPoint;
            _pathStartEnd[0].ChangePosition(PathCanvas, screenPoint);
        }

        #endregion
        
        private void DrawChunksButtonChecked(object sender, RoutedEventArgs e)
        {
            foreach (var chunk in _chunks)
            {
                chunk.Opacity = 1.0;
            }
        }


        private void DrawPortalsButtonChecked(object sender, RoutedEventArgs e)
        {
            drawPortals = true;
            foreach (var portal in _portals)
            {
                portal.Value.Item1.Opacity = 1.0;
                portal.Value.Item2.Opacity = 1.0;
            }
        }
        
        private void DrawChunksButtonUnchecked(object sender, RoutedEventArgs e)
        {
            foreach (var chunk in _chunks)
            {
                chunk.Opacity = 0.0;
            }
        }

        private void DrawPortalsButtonUnchecked(object sender, RoutedEventArgs e)
        {
            drawPortals = false;
            foreach (var portal in _portals)
            {
                portal.Value.Item1.Opacity = 0.0;
                portal.Value.Item2.Opacity = 0.0;
            }
        }

        private void DrawPortalsInternalConnectionsChecked(object sender, RoutedEventArgs e)
        {
            drawPortalsInternalConnections = true;
            DrawPortalInternalConnections();
        }

        private void DrawPortalExternalConnections()
        {
            if (!drawPortalsExternalConnections) return;
             for (int key = 0; key < _vm.Portals.Length; key++)
            {
                ref var portal = ref _vm.Portals[key];
                if (portal == null) continue;
                
                foreach (var keyOtherPortal in portal.externalPortalConnections)
                {
                    if (keyOtherPortal == -1) break;
                    
                    var otherPortal = _vm.Portals[keyOtherPortal];
                    var point1 = Vector2D.ConvertMapPointToCanvasPos(portal.centerPos);
                    var point2 = Vector2D.ConvertMapPointToCanvasPos(otherPortal.centerPos);
                    Line line = new Line
                    {
                        StrokeThickness = 2,
                        X1 = point1.x,
                        X2 = point2.x,
                        Y1 = point1.y,
                        Y2 = point2.y,
                        Stroke = Brushes.Yellow,
                        IsHitTestVisible = false,
                        IsManipulationEnabled = false,
                        IsEnabled = false
                    };
                    _portalExternalConnections.Add(line);
                    PathCanvas.Children.Add(line);
                }
            }
        }

        private void DrawPortalInternalConnections()
        {
            if (!drawPortalsInternalConnections) return;
            HashSet<int> alreadyDrawn = new HashSet<int>();
            for (int key = 0; key < _vm.Portals.Length; key++)
            {
                if(key % MaxPortalsInChunk == 0) alreadyDrawn.Clear();
                ref var portal = ref _vm.Portals[key];
                if (portal == null) continue;
                int chunkIndexinPortalArray = key / MaxPortalsInChunk * MaxPortalsInChunk;
                foreach (var connection in portal.internalPortalConnections)
                {
                    if (connection.portal == byte.MaxValue) break;
                    
                    int keyOtherPortal = chunkIndexinPortalArray + connection.portal;
                    
                    var otherPortal = _vm.Portals[keyOtherPortal];
                    var keyInChunk = key % MaxPortalsInChunk;
                    int connectionKey1 = keyInChunk * MaxPortalsInChunk + connection.portal;
                    int connectionKey2 = keyInChunk + connection.portal * MaxPortalsInChunk;
                    if(!alreadyDrawn.Add(connectionKey1)) continue;
                    if(!alreadyDrawn.Add(connectionKey2)) continue;
                    var point1 = Vector2D.ConvertMapPointToCanvasPos(portal.centerPos);
                    var point2 = Vector2D.ConvertMapPointToCanvasPos(otherPortal.centerPos);
                    Line line = new Line
                    {
                        StrokeThickness = 2,
                        X1 = point1.x,
                        X2 = point2.x,
                        Y1 = point1.y,
                        Y2 = point2.y,
                        Stroke = Brushes.Purple,
                        IsHitTestVisible = false,
                        IsManipulationEnabled = false,
                        IsEnabled = false
                    };
                    _portalInternalConnections.Add(line);
                    PathCanvas.Children.Add(line);
                }
            }
        }


        private void DrawPortalsInternalConnectionsUnchecked(object sender, RoutedEventArgs e)
        {
            drawPortalsInternalConnections = false;
            DeletePortalInternalConnectionsDrawn();
        }

        private void DrawPortalsExternalConnectionsChecked(object sender, RoutedEventArgs e)
        {
            drawPortalsExternalConnections = true;
            DrawPortalExternalConnections();
        }

        private void DrawPortalsExternalConnectionsUnchecked(object sender, RoutedEventArgs e)
        {
            drawPortalsExternalConnections = false;
            DeletePortalExternalConnectionsDrawn();
        }

        private void DeletePortalExternalConnectionsDrawn()
        {
            foreach (var portalConnection in _portalExternalConnections)
            {
                PathCanvas.Children.Remove(portalConnection);
            }

            _portalExternalConnections.Clear();
        }

        private void DeletePortalInternalConnectionsDrawn()
        {
            foreach (var portalConnection in _portalInternalConnections)
            {
                PathCanvas.Children.Remove(portalConnection);
            }

            _portalInternalConnections.Clear();
        }

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ResetPathUi();

            _vm.FindPath();
            
            DrawPathUi(_vm.path.ToArray(), _vm.SelectedAlgorithm.Brush);
            DrawPathUi(_vm.OtherPath.ToArray(), Brushes.Gray, 0.4 );
        }

        private void ChangePathToggleButton_OnChecked(object sender, RoutedEventArgs e)
        {
            ChangeSelection(Visibility.Hidden, null);
        }

        private void ChangeCellBorderClicked(object sender, MouseButtonEventArgs e)
        {
            var image = sender as Image;
            if (image == null || image.Source == null) return;

            Point clickPosition = e.GetPosition(image);
            
            BitmapSource bitmapSource = image.Source as BitmapSource;
            if (bitmapSource == null) return;

            double xRatio = bitmapSource.PixelWidth / image.ActualWidth;
            double yRatio = bitmapSource.PixelHeight / image.ActualHeight;

            int pixelX = (int)(clickPosition.X * xRatio);
            int pixelY = (int)(clickPosition.Y * yRatio);

            
        }
    }
}