using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;
using static Project.MainWindow;

namespace Project
{
    public class PenaltyLabelInfo
    {
        public TextBlock Label { get; set; }
        public double InitialY { get; set; }
        public DateTime StartTime { get; set; }
    }
    public class DifficultySettings
    {
        public int TimeLimit { get; set; }
        public int WrongClickPenalty { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
    public enum PowerUpType
    {
        DoublePoints,
        TimeFreeze,
        Hint,
        Skip
    }
    public class PowerUp
    {
        public PowerUpType Type { get; set; }
        public Button Button { get; set; }
        public DateTime AppearTime { get; set; }
        public bool IsActive { get; set; }
    }

    public partial class Map : Window
    {
        public enum SortingMode
        {
            Category,
            Rating,
            Closest
        }
        private SortingMode currentSortingMode = SortingMode.Category;
        private const double ZoomFactor = 0.1;
        private const double MinScale = 1;
        private const double MaxScale = 2.0;
        private Point _lastMousePosition;
        private MainWindow mainWindow;
        private bool isRatingFilterEnabled = false;
        private bool isDistanceFilterEnabled = false;
        private bool isOpenFilterEnabled = false;
        static public List<Place> placesList = new List<Place>();
        public PlaceService placeService = new PlaceService();
        private Line currentLine;
        private TextBlock currentTextBlock;
        private bool showLines = false;

        private DispatcherTimer gameTimer;
        private Place currentTarget;
        private int gameScore = 0;
        private int previousHighScore = 0;
        private List<Place> remainingPlaces;
        private TextBlock targetDisplay;
        private ProgressBar timeBar;
        private const int GAME_TIME = 5;
        private DateTime roundStartTime;
        private bool gameInProgress = false;
        private Dictionary<Point, PenaltyLabelInfo> penaltyLabels = new Dictionary<Point, PenaltyLabelInfo>();
        private DispatcherTimer labelCleanupTimer;
        private Button lastBigRedButton;
        private GameDifficulty currentDifficulty = GameDifficulty.Normal;
        private Dictionary<GameDifficulty, DifficultySettings> difficultySettings;
        private bool isSpeedMode = false;
        private DateTime speedModeStartTime;
        private TextBlock timerDisplay;
        private DispatcherTimer speedTimer;
        private List<PowerUp> activePowerUps = new List<PowerUp>();
        private bool isDoublePointsActive = false;
        private bool isTimeFrozen = false;
        private DateTime? timeFreezeStart = null;
        private Button hintHighlight = null;
        private Random random = new Random();
        public enum GameDifficulty
        {
            Easy,
            Normal,
            Hard,
            Expert
        }


        public Map(MainWindow main)
        {
            InitializeComponent();
            InitializeGameControls();
            InitializeLabelCleanupTimer();
            InitializeDifficultySettings();
            InitializeSpeedMode();
            Button poiHuntButton = FindName("POIHuntButton") as Button;
            if (poiHuntButton != null)
            {
                poiHuntButton.Click += StartPOIHunt;
            }
            mainWindow = main;
            this.Closing += Map_Closing;
            LoadCategories();
            categoryComboBox.SelectionChanged += categoryComboBox_SelectionChanged;
            MapCanvas.MouseWheel += MapCanvas_MouseWheel;
            MapCanvas.MouseDown += MapCanvas_MouseDown;
            MapCanvas.MouseMove += MapCanvas_MouseMove;
            MapCanvas.MouseUp += MapCanvas_MouseUp;

            foreach (var place in placesList)
            {
                Button redButton = new Button
                {
                    Width = 8,
                    Height = 8,
                    Background = Brushes.Red,
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(1),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Tag = place
                };

                Canvas.SetLeft(redButton, place.X);
                Canvas.SetTop(redButton, place.Y);

                Button placeButton = new Button
                {
                    Content = place.Name,
                    Width = 80,
                    Height = 30,
                    Background = Brushes.Black,
                    BorderBrush = Brushes.White,
                    BorderThickness = new Thickness(2),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Tag = place,
                    Visibility = Visibility.Hidden
                };

                placeButton.Click += PlaceButton_Click;

                Panel.SetZIndex(placeButton, +1);
                Canvas.SetLeft(placeButton, place.X-35);
                Canvas.SetTop(placeButton, place.Y-13);

               

                redButton.MouseEnter += (sender, e) =>
                {
                    placeButton.Visibility = Visibility.Visible;
                    
                };

                placeButton.MouseEnter += (sender, e) =>
                {
                    placeButton.Visibility = Visibility.Visible;
                };

                placeButton.MouseLeave += (sender, e) =>
                {
                    placeButton.Visibility = Visibility.Hidden;
                };

                MapCanvas.Children.Add(redButton);
                MapCanvas.Children.Add(placeButton);
            }

        }

        // Tool bar

        private void LoadCategories()
        {
            var categories = new List<string> { "Viss" };
            categories.AddRange(PlaceService.GetAllPlaces().Select(p => p.Category).Distinct());

            categoryComboBox.ItemsSource = categories;
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            string searchTerm = SearchTextBox.Text;
            placesList = placeService.SearchPlaces(searchTerm);
            ApplyFiltersAndSorting();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchTerm = SearchTextBox.Text;
            placesList = placeService.SearchPlaces(searchTerm);
            ApplyFiltersAndSorting();
        }

        private void FilterCheckBoxR_Click(object sender, RoutedEventArgs e)
        {
            isRatingFilterEnabled = RatingCheckBox.IsChecked ?? false;
            ApplyFiltersAndSorting();
        }

        private void FilterCheckBoxD_Click(object sender, RoutedEventArgs e)
        {
            isDistanceFilterEnabled = DistanceCheckBox.IsChecked ?? false;
            ApplyFiltersAndSorting();
        }

        private void FilterCheckBoxO_Click(object sender, RoutedEventArgs e)
        {
            isOpenFilterEnabled = Open247CheckBox.IsChecked ?? false;
            ApplyFiltersAndSorting();
        }

        private void ApplyFiltersAndSorting()
        {
            string selectedCategory = categoryComboBox.SelectedItem?.ToString() ?? "Viss";
            string searchTerm = SearchTextBox.Text?.ToLower() ?? "";

            var filteredPlaces = PlaceService.GetAllPlaces();

            if (selectedCategory != "Viss")
            {
                filteredPlaces = filteredPlaces
                    .Where(p => p.Category == selectedCategory)
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                filteredPlaces = filteredPlaces
                    .Where(p => p.Name.ToLower().Contains(searchTerm) || p.Category.ToLower().Contains(searchTerm))
                    .ToList();
            }
            if (isRatingFilterEnabled)
            {
                filteredPlaces = filteredPlaces
                    .Where(p => p.Rating >= 4.0)
                    .ToList();
            }

            if (isDistanceFilterEnabled)
            {
                filteredPlaces = filteredPlaces
                    .Where(p => PlaceService.CalculateDistance(p.X, p.Y) <= 1.0)
                    .ToList();
            }

            if (isOpenFilterEnabled)
            {
                filteredPlaces = filteredPlaces
                    .Where(p => p.Schedule == PlacesData.scheduleAlwaysOpen)
                    .ToList();


            }
            switch (currentSortingMode)
            {
                case SortingMode.Category:
                    filteredPlaces = PlaceService.SortByCategory(filteredPlaces);
                    break;
                case SortingMode.Rating:
                    filteredPlaces = PlaceService.SortByRating(filteredPlaces);
                    break;
                case SortingMode.Closest:
                    filteredPlaces = PlaceService.SortByDistance(filteredPlaces);
                    break;
            }

           placesList = filteredPlaces;
           RenderPlaces();
        }

        public void categoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFiltersAndSorting();
            string selectedCategory = categoryComboBox.SelectedItem.ToString();

            List<Place> filteredPlaces = placesList;

            if (selectedCategory == "Viss")
            {
                filteredPlaces = PlaceService.GetAllPlaces()
                    .Where(p => p.Category == selectedCategory)
                    .ToList();
            }
            else if (selectedCategory == "Elektronika")
            {
                filteredPlaces = PlaceService.GetAllPlaces()
                                    .Where(p => p.Category == selectedCategory)
                                    .ToList();
            }
            else if (selectedCategory == "Ediens")
            {
                filteredPlaces = PlaceService.GetAllPlaces()
                                    .Where(p => p.Category == selectedCategory)
                                    .ToList();
            }
            else if (selectedCategory == "Clubi/Bari")
            {
                filteredPlaces = PlaceService.GetAllPlaces()
                                    .Where(p => p.Category == selectedCategory)
                                    .ToList();
            }
            else if (selectedCategory == "Parki")
            {
                filteredPlaces = PlaceService.GetAllPlaces()
                                    .Where(p => p.Category == selectedCategory)
                                    .ToList();
            }
            else if (selectedCategory == "Veikali")
            {
                filteredPlaces = PlaceService.GetAllPlaces()
                                    .Where(p => p.Category == selectedCategory)
                                    .ToList();
            }
            else if (selectedCategory == "Skaistumi")
            {
                filteredPlaces = PlaceService.GetAllPlaces()
                                    .Where(p => p.Category == selectedCategory)
                                    .ToList();
            }
            if (placesList != null)
            {
                placesList = filteredPlaces;
            }
            ApplyFiltersAndSorting();
        }

        private void SortingButton_Click(object sender, RoutedEventArgs e)
        {
            currentSortingMode = currentSortingMode switch
            {
                SortingMode.Category => SortingMode.Rating,
                SortingMode.Rating => SortingMode.Closest,
                SortingMode.Closest => SortingMode.Category,
                _ => SortingMode.Category
            };

            ApplyFiltersAndSorting();
        }

        private void RedButton_MouseEnter(object sender, MouseEventArgs e)
        {
            Button hoverButton = new Button
            {
                Width = 50,
                Height = 20,
                Content = "Hovered!",
                Background = Brushes.LightGray,
                BorderBrush = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(100, 50, 0, 0)
            };

            ((Grid)this.Content).Children.Add(hoverButton);

            ((Button)sender).MouseLeave += (s, args) =>
            {
                ((Grid)this.Content).Children.Remove(hoverButton);
            };
        }

        // Map interaction

        private void MapCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var transformGroup = (TransformGroup)MapCanvas.RenderTransform;
            var scaleTransform = (ScaleTransform)transformGroup.Children[0];
            var translateTransform = (TranslateTransform)transformGroup.Children[1];

            double currentScale = scaleTransform.ScaleX;

            double newScale = currentScale + (e.Delta > 0 ? ZoomFactor : -ZoomFactor);
            newScale = Math.Max(MinScale, Math.Min(MaxScale, newScale));

            var mousePosition = e.GetPosition(MapCanvas);

            translateTransform.X = mousePosition.X - (mousePosition.X - translateTransform.X) * newScale / currentScale;
            translateTransform.Y = mousePosition.Y - (mousePosition.Y - translateTransform.Y) * newScale / currentScale;

            scaleTransform.ScaleX = newScale;
            scaleTransform.ScaleY = newScale;

            EnforceBoundaries();
        }

        private void MapCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                _lastMousePosition = e.GetPosition(this);
                MapCanvas.CaptureMouse();
            }
        }

        private void MapCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Pressed && MapCanvas.IsMouseCaptured)
            {
                var transformGroup = (TransformGroup)MapCanvas.RenderTransform;
                var translateTransform = (TranslateTransform)transformGroup.Children[1];

                var currentPosition = e.GetPosition(this);
                var delta = currentPosition - _lastMousePosition;

                translateTransform.X += delta.X;
                translateTransform.Y += delta.Y;

                _lastMousePosition = currentPosition;

                EnforceBoundaries();
            }
        }

        private void EnforceBoundaries()
        {
            var transformGroup = (TransformGroup)MapCanvas.RenderTransform;
            var scaleTransform = (ScaleTransform)transformGroup.Children[0];
            var translateTransform = (TranslateTransform)transformGroup.Children[1];

            double scale = scaleTransform.ScaleX;

            double imageWidth = MapCanvas.ActualWidth * scale;
            double imageHeight = MapCanvas.ActualHeight * scale;

            double viewportWidth = this.ActualWidth;
            double viewportHeight = this.ActualHeight;

            double minX = Math.Min(0, viewportWidth - imageWidth);
            double maxX = 0;

            double minY = Math.Min(0, viewportHeight - imageHeight);
            double maxY = 0;

            translateTransform.X = Math.Max(minX, Math.Min(maxX, translateTransform.X));
            translateTransform.Y = Math.Max(minY, Math.Min(maxY, translateTransform.Y));
        }

        private void MapCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Released)
            {
                MapCanvas.ReleaseMouseCapture();
            }
        }

        private void PlaceButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is Place clickedPlace)
            {
                if (gameInProgress || isSpeedMode)
                {
                    if (isSpeedMode)
                    {
                        HandleSpeedModeClick(clickedPlace, button);
                    }
                    double x = Canvas.GetLeft(button);
                    double y = Canvas.GetTop(button);

                    if (clickedPlace == currentTarget)
                    {
                        gameTimer.Stop();
                        TimeSpan elapsed = DateTime.Now - roundStartTime;
                        int pointsEarned = CalculatePoints(elapsed.TotalSeconds);
                        gameScore += pointsEarned;
                        MessageBox.Show($"Correct! You earned {pointsEarned} points!", "Good job!");
                        StartNewRound();
                    }
                    else if (currentDifficulty == GameDifficulty.Expert)
                    {
                        gameTimer.Stop();
                        ShowGameOverPenalty(x, y);
                        MessageBox.Show($"Game Over!\nWrong location in Expert mode.\nFinal Score: {gameScore}", "Game Over");
                        EndGame();
                    }
                    else
                    {
                        int penalty = difficultySettings[currentDifficulty].WrongClickPenalty;
                        if (penalty > 0)
                        {
                            gameScore = Math.Max(0, gameScore - penalty);
                            ShowPenaltyLabel(x, y, $"-{penalty}");
                        }

                        if (targetDisplay != null)
                        {
                            UpdateScoreDisplay();
                        }
                    }
                }
                else
                {
                    mainWindow.SelectedPlace = clickedPlace;
                    mainWindow.DataContext = clickedPlace;
                    mainWindow.UpdatePlaceDetails();
                    mainWindow.Show();
                    this.Close();
                }
            }
        }

        private void ToListView_Click(object sender, RoutedEventArgs e)
        {
                gameInProgress = false;
                mainWindow.Show();
                mainWindow.panel.Visibility = Visibility.Hidden;
                this.Close();
        }

        private void Map_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            gameInProgress = false;
            mainWindow.RestoreMainWindow();
            mainWindow.panel.Visibility = Visibility.Visible;
        }

        // Rendering

        private void RenderPlaces()
        {
            var elementsToRemove = MapCanvas.Children
        .OfType<UIElement>()
        .Where(child => child is Button || child is Line)
        .ToList();

            foreach (var element in elementsToRemove)
            {
                MapCanvas.Children.Remove(element);
            }

            foreach (var place in placesList)
            {
                Button redButton = new Button
                {
                    Width = 8,
                    Height = 8,
                    Background = Brushes.Red,
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(1),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Tag = place
                };

                Canvas.SetLeft(redButton, place.X);
                Canvas.SetTop(redButton, place.Y);
                Panel.SetZIndex(redButton, 2);

                Button placeButton = new Button
                {
                    Content = place.Name,
                    Width = 80,
                    Height = 30,
                    Background = Brushes.Black,
                    BorderBrush = Brushes.White,
                    BorderThickness = new Thickness(2),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Tag = place,
                    Visibility = Visibility.Hidden
                };

                placeButton.Click += PlaceButton_Click;

                Panel.SetZIndex(placeButton, 3);
                Canvas.SetLeft(placeButton, place.X - 35);
                Canvas.SetTop(placeButton, place.Y - 13);

                redButton.MouseEnter += (sender, e) =>
                {
                    placeButton.Visibility = Visibility.Visible;
                    if (showLines)
                    {
                        DrawLineToPlace(place.X, place.Y);
                        DrawDistanceBlockToPlace(place.X, place.Y, place.DistanceFromCenter);
                    }
                };

                redButton.MouseLeave += (sender, e) =>
                {
                    placeButton.Visibility = Visibility.Hidden;
                    if (showLines)
                    {
                        RemoveLine();
                        RemoveDistanceBlock();
                    }
                       
                };

                placeButton.MouseEnter += (sender, e) =>
                {
                    placeButton.Visibility = Visibility.Visible;
                    if (showLines) {
                        DrawDistanceBlockToPlace(place.X, place.Y, place.DistanceFromCenter);
                        DrawLineToPlace(place.X, place.Y); }
                };

                placeButton.MouseLeave += (sender, e) =>
                {
                    placeButton.Visibility = Visibility.Hidden;
                    if (showLines)
                    {
                        RemoveLine();
                        RemoveDistanceBlock();
                    }
                        
                };

                MapCanvas.Children.Add(redButton);
                MapCanvas.Children.Add(placeButton);
            }
        }
        private void DrawLineToPlace(double placeX, double placeY)
        {

            RemoveLine();

            currentLine = new Line
            {
                X1 = 606,
                Y1 = 338,
                X2 = placeX,
                Y2 = placeY,
                Stroke = Brushes.Red,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 2, 2 }
            };
            MapCanvas.Children.Add(currentLine);
        }

        private void DrawDistanceBlockToPlace (double placeX, double placeY, double placeDistance)
        {
            currentTextBlock = new TextBlock
            {
                Text = $"{placeDistance} km",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Background = Brushes.Black,
                
            };
            if (placeY < 335)
            {
                Canvas.SetLeft(currentTextBlock, placeX - 25);
                Canvas.SetTop(currentTextBlock, placeY - 35);
            }
            if (placeY > 335)
            {
                Canvas.SetLeft(currentTextBlock, placeX - 25);
                Canvas.SetTop(currentTextBlock, placeY + 20);
            }

            MapCanvas.Children.Add(currentTextBlock);
        }

        private void RemoveLine()
        {
            if (currentLine != null)
            {
                MapCanvas.Children.Remove(currentLine);
                currentLine = null;
            }
        }

        private void RemoveDistanceBlock ()
        {
            if (currentTextBlock != null)
            {
                MapCanvas.Children.Remove(currentTextBlock);
                currentTextBlock = null;
            }
        }

        private void LineCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            showLines = true;
        }
        private void LineCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            showLines = false;
            RemoveLine();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RenderPlaces();
        }

        private void InitializeGameControls()
        {
            targetDisplay = new TextBlock
            {
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 10, 0, 0),
                Visibility = Visibility.Collapsed
            };
            Grid.SetRow(targetDisplay, 0);
            mainGrid.Children.Add(targetDisplay);

            timeBar = new ProgressBar
            {
                Height = 20,
                Width = 200,
                Maximum = GAME_TIME,
                Minimum = 0,
                Value = GAME_TIME,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 50, 0, 0),
                Visibility = Visibility.Collapsed
            };
            Grid.SetRow(timeBar, 0);
            mainGrid.Children.Add(timeBar);

            gameTimer = new DispatcherTimer();
            gameTimer.Interval = TimeSpan.FromMilliseconds(10);
            gameTimer.Tick += GameTimer_Tick;
        }

        private void InitializeDifficultySettings()
        {
            difficultySettings = new Dictionary<GameDifficulty, DifficultySettings>
    {
        {GameDifficulty.Easy, new DifficultySettings
        {
            TimeLimit = 15,
            WrongClickPenalty = 0,
            Name = "Easy",
            Description = "15 seconds per location, no penalties"
        }},
        {GameDifficulty.Normal, new DifficultySettings
        {
            TimeLimit = 10,
            WrongClickPenalty = 1,
            Name = "Normal",
            Description = "10 seconds per location, -1 point penalty"
        }},
        {GameDifficulty.Hard, new DifficultySettings
        {
            TimeLimit = 5,
            WrongClickPenalty = 2,
            Name = "Hard",
            Description = "5 seconds per location, -2 points penalty"
        }},
        {GameDifficulty.Expert, new DifficultySettings
        {
            TimeLimit = 5,
            Name = "Expert",
            Description = "5 seconds per location, mistake will end game"
        }}
    };
        }

        private void ShowDifficultySelectionDialog()
        {
            var dialog = new Window
            {
                Title = "Select Difficulty",
                Width = 400,
                Height = 350,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize
            };

            var stackPanel = new StackPanel
            {
                Margin = new Thickness(20)
            };

            foreach (var difficulty in Enum.GetValues(typeof(GameDifficulty)))
            {
                var settings = difficultySettings[(GameDifficulty)difficulty];
                var button = new Button
                {
                    Content = new TextBlock
                    {
                        Text = $"{settings.Name}\n{settings.Description}",
                        TextAlignment = TextAlignment.Center,
                        TextWrapping = TextWrapping.Wrap
                    },
                    Height = 50,
                    Margin = new Thickness(0, 5, 0, 5)
                };

                button.Click += (s, e) =>
                {
                    currentDifficulty = (GameDifficulty)difficulty;
                    dialog.DialogResult = true;
                    dialog.Close();
                };

                stackPanel.Children.Add(button);
            }

            dialog.Content = stackPanel;

            if (dialog.ShowDialog() == true)
            {
                StartGame();
            }
        }

        private void InitializeLabelCleanupTimer()
        {
            labelCleanupTimer = new DispatcherTimer();
            labelCleanupTimer.Interval = TimeSpan.FromSeconds(1.5);
            labelCleanupTimer.Tick += LabelCleanupTimer_Tick;
        }

        private void LabelCleanupTimer_Tick(object sender, EventArgs e)
        {
            var labelsToRemove = new List<Point>();

            foreach (var kvp in penaltyLabels)
            {
                var labelInfo = kvp.Value;
                var elapsed = (DateTime.Now - labelInfo.StartTime).TotalSeconds;
                const double animationDuration = 5;

                if (elapsed >= animationDuration)
                {
                    MapCanvas.Children.Remove(labelInfo.Label);
                    labelsToRemove.Add(kvp.Key);
                    continue;
                }

                double progress = elapsed / animationDuration;

                double easedProgress = 3 - Math.Pow(1 - progress, 3);

                labelInfo.Label.Opacity = 1 - easedProgress;

                double totalMove = 50;
                double currentY = labelInfo.InitialY - (totalMove * easedProgress);
                Canvas.SetTop(labelInfo.Label, currentY);
            }

            foreach (var point in labelsToRemove)
            {
                penaltyLabels.Remove(point);
            }

            if (penaltyLabels.Count == 0)
            {
                labelCleanupTimer.Stop();
            }
        }

        private void ShowPenaltyLabel(double x, double y, string penaltyText)
        {
            var point = new Point(x, y);

            TextBlock penaltyLabel = new TextBlock
            {
                Text = penaltyText,
                Foreground = Brushes.Red,
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Opacity = 1,
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    ShadowDepth = 2,
                    BlurRadius = 4,
                    Opacity = 0.5
                }
            };

            double initialY = y - 30;
            Canvas.SetLeft(penaltyLabel, x + 25);
            Canvas.SetTop(penaltyLabel, initialY);
            Panel.SetZIndex(penaltyLabel, 100);

            MapCanvas.Children.Add(penaltyLabel);
            penaltyLabels[point] = new PenaltyLabelInfo
            {
                Label = penaltyLabel,
                InitialY = initialY,
                StartTime = DateTime.Now
            };

            if (!labelCleanupTimer.IsEnabled)
            {
                labelCleanupTimer.Start();
            }
        }

        private void StartPOIHunt(object sender, RoutedEventArgs e)
        {
            if (!gameInProgress)
            {
                var modeWindow = new Window
                {
                    Title = "Select Game Mode",
                    Width = 300,
                    Height = 200,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this
                };

                var panel = new StackPanel { Margin = new Thickness(10) };
                var normalButton = new Button
                {
                    Content = "Normal Mode",
                    Height = 40,
                    Margin = new Thickness(0, 5, 0, 5)
                };
                var speedButton = new Button
                {
                    Content = "Speed Mode",
                    Height = 40,
                    Margin = new Thickness(0, 5, 0, 5)
                };

                normalButton.Click += (s, ev) =>
                {
                    modeWindow.Close();
                    ShowDifficultySelectionDialog();
                };

                speedButton.Click += (s, ev) =>
                {
                    modeWindow.Close();
                    StartSpeedMode();
                };

                panel.Children.Add(normalButton);
                panel.Children.Add(speedButton);
                modeWindow.Content = panel;
                modeWindow.ShowDialog();
            }
            else
            {
                if (isSpeedMode)
                {
                    EndSpeedMode();
                }
                else
                {
                    EndGame();
                }
            }
        }

        private void StartGame()
        {
            gameScore = 0;
            remainingPlaces = new List<Place>(placesList);
            Button huntButton = FindName("POIHuntButton") as Button;
            if (huntButton != null)
            {
                huntButton.Content = "End Game";
            }
            gameInProgress = true;

            targetDisplay.Visibility = Visibility.Visible;
            timeBar.Visibility = Visibility.Visible;

            timeBar.Maximum = difficultySettings[currentDifficulty].TimeLimit;

            StartNewRound();
        }


        private void StartNewRound()
        {
            if (remainingPlaces.Count == 0)
            {
                EndGame();
                return;
            }

            Random rnd = new Random();
            int index = rnd.Next(remainingPlaces.Count);
            currentTarget = remainingPlaces[index];
            remainingPlaces.RemoveAt(index);

            UpdateScoreDisplay();

            var settings = difficultySettings[currentDifficulty];
            timeBar.Value = settings.TimeLimit;
            roundStartTime = DateTime.Now;
            gameTimer.Start();
        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            TimeSpan elapsed = DateTime.Now - roundStartTime;
            double remainingTime = GAME_TIME - elapsed.TotalSeconds;
            timeBar.Value = Math.Max(0, remainingTime);

            if (remainingTime <= 0)
            {
                gameTimer.Stop();
                foreach (Place p in placesList)
                {
                    if (p.Name == currentTarget.Name)
                    {
                        Button bigRedButton = new Button
                        {
                            Width = 20,
                            Height = 20,
                            Background = Brushes.Yellow,
                            BorderBrush = Brushes.Black,
                            BorderThickness = new Thickness(1),
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Top,
                        };

                        Canvas.SetLeft(bigRedButton, p.X -5);
                        Canvas.SetTop(bigRedButton, p.Y -5);
                        MapCanvas.Children.Add(bigRedButton);
                        lastBigRedButton = bigRedButton;
                    }
                }
                MessageBox.Show($"Time's up! Too Slow! {currentTarget.Name} is marked yellow");
                MapCanvas.Children.Remove(lastBigRedButton);
                StartNewRound();
            }
        }

        private void InitializeSpeedMode()
        {
            timerDisplay = new TextBlock
            {
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 50, 0, 0),
                Visibility = Visibility.Collapsed,
            };
            Grid.SetRow(timerDisplay, 0);
            mainGrid.Children.Add(timerDisplay);

            speedTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            speedTimer.Tick += SpeedTimer_Tick;
        }

        private void StartSpeedMode()
        {
            targetDisplay.Visibility = Visibility.Visible;
            timerDisplay.Visibility = Visibility.Visible;
            isSpeedMode = true;
            speedModeStartTime = DateTime.Now;
            gameScore = 0;
            remainingPlaces = new List<Place>(placesList);
            isDoublePointsActive = false;
            isTimeFrozen = false;
            timeFreezeStart = null;
            ClearHint();

            Button huntButton = FindName("POIHuntButton") as Button;
            if (huntButton != null)
            {
                huntButton.Content = "End Game";
            }

            speedTimer.Start();
            StartNewSpeedRound();
        }

        private void SpeedTimer_Tick(object sender, EventArgs e)
        {
            if (isTimeFrozen)
            {
                if (DateTime.Now - timeFreezeStart?.AddSeconds(3) >= TimeSpan.Zero)
                {
                    isTimeFrozen = false;
                    timeFreezeStart = null;
                }
            }

            TimeSpan elapsed = DateTime.Now - speedModeStartTime;
            if (!isTimeFrozen)
            {
                timerDisplay.Text = $"Time: {elapsed.Minutes:D2}:{elapsed.Seconds:D2}.{elapsed.Milliseconds / 100:D1}";
            }

            if (random.NextDouble() < 0.05) // chance per tick
            {
                SpawnPowerUp();
            }

            CheckPowerUpExpiration();
        }

        private void SpawnPowerUp()
        {
            PowerUpType type = (PowerUpType)random.Next(Enum.GetValues(typeof(PowerUpType)).Length);

            Button powerUpButton = new Button
            {
                Width = 40,
                Height = 40,
                Background = GetPowerUpColor(type),
                Content = GetPowerUpSymbol(type),
                FontSize = 20,
                Foreground = Brushes.White,
                BorderBrush = Brushes.Gold,
                BorderThickness = new Thickness(2)
            };

            double x = random.NextDouble() * (MapCanvas.ActualWidth - powerUpButton.Width);
            double y = random.NextDouble() * (MapCanvas.ActualHeight - powerUpButton.Height);

            Canvas.SetLeft(powerUpButton, x);
            Canvas.SetTop(powerUpButton, y);
            Panel.SetZIndex(powerUpButton, 99);

            PowerUp powerUp = new PowerUp
            {
                Type = type,
                Button = powerUpButton,
                AppearTime = DateTime.Now,
                IsActive = true
            };

            powerUpButton.Click += (s, e) => CollectPowerUp(powerUp);

            MapCanvas.Children.Add(powerUpButton);
            activePowerUps.Add(powerUp);
        }

        private void CollectPowerUp(PowerUp powerUp)
        {
            switch (powerUp.Type)
            {
                case PowerUpType.DoublePoints:
                    isDoublePointsActive = true;
                    ShowPowerUpEffect("2X POINTS!", powerUp.Button);
                    break;

                case PowerUpType.TimeFreeze:
                    isTimeFrozen = true;
                    timeFreezeStart = DateTime.Now;
                    ShowPowerUpEffect("TIME FROZEN", powerUp.Button);
                    break;

                case PowerUpType.Hint:
                    ShowHint();
                    ShowPowerUpEffect("HINT!", powerUp.Button);
                    break;

                case PowerUpType.Skip:
                    StartNewSpeedRound();
                    ShowPowerUpEffect("SKIPPED!", powerUp.Button);
                    break;
            }

            MapCanvas.Children.Remove(powerUp.Button);
            activePowerUps.Remove(powerUp);
        }

        private void ShowHint()
        {
            ClearHint();

            double targetX = Canvas.GetLeft(FindButtonForPlace(currentTarget));
            double targetY = Canvas.GetTop(FindButtonForPlace(currentTarget));

            hintHighlight = new Button
            {
                Width = 100,
                Height = 100,
                Background = new SolidColorBrush(Color.FromArgb(64, 255, 255, 0)),
                BorderThickness = new Thickness(0)
            };

            Canvas.SetLeft(hintHighlight, targetX - 45);
            Canvas.SetTop(hintHighlight, targetY - 45);
            Panel.SetZIndex(hintHighlight, 1);

            MapCanvas.Children.Add(hintHighlight);
        }

        private void ClearHint()
        {
            if (hintHighlight != null)
            {
                MapCanvas.Children.Remove(hintHighlight);
                hintHighlight = null;
            }
        }

        private Button FindButtonForPlace(Place place)
        {
            foreach (var child in MapCanvas.Children)
            {
                if (child is Button button && button.Tag as Place == place)
                {
                    return button;
                }
            }
            return null;
        }

        private void ShowPowerUpEffect(string text, Button sourceButton)
        {
            TextBlock effectLabel = new TextBlock
            {
                Text = text,
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Gold,
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    ShadowDepth = 2,
                    BlurRadius = 4,
                    Opacity = 0.5
                }
            };

            double x = Canvas.GetLeft(sourceButton);
            double y = Canvas.GetTop(sourceButton);

            Canvas.SetLeft(effectLabel, x);
            Canvas.SetTop(effectLabel, y);
            Panel.SetZIndex(effectLabel, 100);

            MapCanvas.Children.Add(effectLabel);

            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(1));
            var moveUp = new DoubleAnimation(y, y - 50, TimeSpan.FromSeconds(1));

            fadeOut.Completed += (s, e) => MapCanvas.Children.Remove(effectLabel);

            effectLabel.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            Canvas.SetTop(effectLabel, y);
            effectLabel.BeginAnimation(Canvas.TopProperty, moveUp);
        }

        private void CheckPowerUpExpiration()
        {
            var expiredPowerUps = activePowerUps
                .Where(p => (DateTime.Now - p.AppearTime).TotalSeconds > 5)
                .ToList();

            foreach (var powerUp in expiredPowerUps)
            {
                MapCanvas.Children.Remove(powerUp.Button);
                activePowerUps.Remove(powerUp);
            }
        }

        private Brush GetPowerUpColor(PowerUpType type)
        {
            return type switch
            {
                PowerUpType.DoublePoints => Brushes.Purple,
                PowerUpType.TimeFreeze => Brushes.Blue,
                PowerUpType.Hint => Brushes.Green,
                PowerUpType.Skip => Brushes.Orange,
                _ => Brushes.Gray
            };
        }

        private string GetPowerUpSymbol(PowerUpType type)
        {
            return type switch
            {
                PowerUpType.DoublePoints => "2×",
                PowerUpType.TimeFreeze => "⌛",
                PowerUpType.Hint => "?",
                PowerUpType.Skip => "→",
                _ => "?"
            };
        }

        private void HandleSpeedModeClick(Place clickedPlace, Button button)
        {
            if (clickedPlace == currentTarget)
            {
                int points = CalculateSpeedPoints();
                if (isDoublePointsActive)
                {
                    points *= 2;
                    isDoublePointsActive = false;
                }

                gameScore += points;
                ShowPointsEarned(points, button);
                StartNewSpeedRound();
            }
        }

        private async void ShowPointsEarned(int points, Button button)
        {
            var originalContent = button.Content;
            var originalBackground = button.Background;

            var pointsText = $"+{points}";

            button.Content = pointsText;
            button.Background = new SolidColorBrush(Colors.Green);

            var animation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = new Duration(TimeSpan.FromSeconds(1)),
                EasingFunction = new QuadraticEase()
            };

            var scaleTransform = new ScaleTransform(1, 1);
            button.RenderTransform = scaleTransform;

            var fadeAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = new Duration(TimeSpan.FromMilliseconds(800))
            };

            button.BeginAnimation(UIElement.OpacityProperty, fadeAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animation);

            await Task.Delay(1000);

            button.Content = originalContent;
            button.Background = originalBackground;
            button.Opacity = 1.0;
            scaleTransform.ScaleX = 1.0;
            scaleTransform.ScaleY = 1.0;
        }

        private int CalculateSpeedPoints()
        {
            TimeSpan totalTime = DateTime.Now - speedModeStartTime;
            return Math.Max(10, 100 - (int)totalTime.TotalSeconds);
        }

        private void StartNewSpeedRound()
        {
            if (remainingPlaces.Count == 0)
            {
                EndSpeedMode();
                return;
            }

            ClearHint();

            Random rnd = new Random();
            int index = rnd.Next(remainingPlaces.Count);
            currentTarget = remainingPlaces[index];
            remainingPlaces.RemoveAt(index);

            targetDisplay.Text = $"Find: {currentTarget.Name}";
        }

        private void EndSpeedMode()
        {
            speedTimer.Stop();
            isSpeedMode = false;
            TimeSpan totalTime = DateTime.Now - speedModeStartTime;


            foreach (var powerUp in activePowerUps.ToList())
            {
                MapCanvas.Children.Remove(powerUp.Button);
            }
            activePowerUps.Clear();
            ClearHint();

            MessageBox.Show(
                $"Game Over!\n" +
                $"Final Score: {gameScore}\n" +
                $"Total Time: {totalTime.Minutes:D2}:{totalTime.Seconds:D2}.{totalTime.Milliseconds / 100:D1}",
                "Speed Mode Finished"
            );

            Button huntButton = FindName("POIHuntButton") as Button;
            if (huntButton != null)
            {
                huntButton.Content = "End Game";
            }

            timerDisplay.Text = "";
        }


        private int CalculatePoints(double elapsedSeconds)
        {
            return Math.Max(1, (int)Math.Ceiling((GAME_TIME - elapsedSeconds) / GAME_TIME * 10));
        }

        private void EndGame()
        {
            gameTimer.Stop();
            gameInProgress = false;

            targetDisplay.Visibility = Visibility.Collapsed;
            timeBar.Visibility = Visibility.Collapsed;
            Button huntButton = FindName("POIHuntButton") as Button;
            if (huntButton != null)
            {
                huntButton.Content = "POI Hunt";
            }

            if (currentDifficulty != GameDifficulty.Expert)
            {
                MessageBox.Show($"Congratulations!\nFinal Score: {gameScore} points", "Game Finished");
            }
            
            UpdateHighScore(gameScore);
        }

        private void ShowGameOverPenalty(double x, double y)
        {
            var point = new Point(x, y);

            TextBlock gameOverLabel = new TextBlock
            {
                Text = "GAME OVER",
                Foreground = Brushes.Red,
                FontSize = 32,
                FontWeight = FontWeights.Bold,
                Opacity = 1,
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    ShadowDepth = 2,
                    BlurRadius = 4,
                    Opacity = 0.5
                }
            };

            double initialY = y - 30;
            Canvas.SetLeft(gameOverLabel, x - 50);
            Canvas.SetTop(gameOverLabel, initialY);
            Panel.SetZIndex(gameOverLabel, 100);

            MapCanvas.Children.Add(gameOverLabel);

            var fadeOut = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = TimeSpan.FromSeconds(2)
            };

            fadeOut.Completed += (s, e) => MapCanvas.Children.Remove(gameOverLabel);
            gameOverLabel.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        private void UpdateScoreDisplay()
        {
            var settings = difficultySettings[currentDifficulty];
            string displayName = currentTarget.Name;
            string modeInfo = currentDifficulty == GameDifficulty.Expert ?
                " | ⚠️ One life" :
                $" | Score: {gameScore}";

            targetDisplay.Text = $"[{settings.Name}] Find: {displayName}{modeInfo}";
        }

        public void UpdateHighScore(int score)
        {
            if (score > previousHighScore)
            {
                previousHighScore = score;
                HighScore.Text = $"HighScore: {previousHighScore} ({currentDifficulty})";
                HighScore.Visibility = Visibility.Visible;
            }
        }

    }
}