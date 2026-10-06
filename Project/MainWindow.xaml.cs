using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Project
{

    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public SortingMode CurrentSortingMode
        {
            get => currentSortingMode;
            set
            {
                currentSortingMode = value;
                OnPropertyChanged(nameof(CurrentSortingMode));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public PlaceService placeService = new PlaceService();
        private ObservableCollection<Place> places { get; set; }
        public enum SortingMode
        {
            Category,
            Rating,
            Closest
        }
        private SortingMode currentSortingMode = SortingMode.Category;

        private bool isRatingFilterEnabled = false;
        private bool isDistanceFilterEnabled = false;
        private bool isOpenFilterEnabled = false;
        private void Search_Click(object sender, RoutedEventArgs e)
        {
            string searchTerm = SearchTextBox.Text;
            PlacesListBox.ItemsSource = placeService.SearchPlaces(searchTerm);
            ApplyFiltersAndSorting();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchTerm = SearchTextBox.Text;
            PlacesListBox.ItemsSource = placeService.SearchPlaces(searchTerm);
            ApplyFiltersAndSorting();
        }

        private void placeListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (PlacesListBox.SelectedItem != null)
            {
                SelectedPlace = (Place)PlacesListBox.SelectedItem;
                panel.Visibility = Visibility.Visible;
                panel.DataContext = SelectedPlace;
                this.DataContext = this;
            }
        }
        public ObservableCollection<string> Places { get; set; }
        public Place SelectedPlace { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            placeService = new PlaceService();
            PlacesData.Initialize(placeService);
            Places = new ObservableCollection<string>();
            places = new ObservableCollection<Place>(PlaceService.GetAllPlaces());
            LoadCategories();
            PlacesListBox.ItemsSource = PlaceService.GetAllPlaces();
            categoryComboBox.SelectionChanged += categoryComboBox_SelectionChanged;
            UpdateSortingButtonContent();
            panel.Visibility = Visibility.Hidden;
            RatingCheckBox.IsChecked = false;
            DistanceCheckBox.IsChecked = false;
            Open247CheckBox.IsChecked = false;
            this.DataContext = this;
        }
        private Map mapWindow;
        public void RestoreMainWindow()
        {
            panel.Visibility = Visibility.Hidden;
            this.Show();
            this.DataContext = this;
            ApplyFiltersAndSorting();
        }
        private void ToMapView_Click(object sender, RoutedEventArgs e)
        {
            if (mapWindow == null || !mapWindow.IsLoaded)
            {
                mapWindow = new Map(this);
                mapWindow.Show();
            }
            else
            {
                mapWindow.Activate();
            }
            this.Hide();
        }

        private void LoadCategories()
        {
            var categories = new List<string> { "Viss" };
            categories.AddRange(PlaceService.GetAllPlaces().Select(p => p.Category).Distinct());

            categoryComboBox.ItemsSource = categories;
        }
        public void categoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFiltersAndSorting();
            string selectedCategory = categoryComboBox.SelectedItem.ToString();

            List<Place> filteredPlaces = new List<Place>();

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
            switch (selectedCategory)
            {
                case "Viss":
                    filteredPlaces = PlaceService.GetAllPlaces();
                    break;
                case "Elektronika":
                    filteredPlaces = PlaceService.GetAllPlaces()
                                    .Where(p => p.Category == selectedCategory)
                                    .ToList();
                    break;
                case "Ediens":
                    filteredPlaces = PlaceService.GetAllPlaces()
                                    .Where(p => p.Category == selectedCategory)
                                    .ToList();
                    break;
                case "Clubi/Bari":
                    filteredPlaces = PlaceService.GetAllPlaces()
                                    .Where(p => p.Category == selectedCategory)
                                    .ToList();
                    break;
                case "Parki":
                    filteredPlaces = PlaceService.GetAllPlaces()
                                    .Where(p => p.Category == selectedCategory)
                                    .ToList();
                    break;
                case "Veikali":
                    filteredPlaces = PlaceService.GetAllPlaces()
                                    .Where(p => p.Category == selectedCategory)
                                    .ToList();
                    break;
                case "Skaistumi":
                    filteredPlaces = PlaceService.GetAllPlaces()
                                    .Where(p => p.Category == selectedCategory)
                                    .ToList();
                    break;
                default:
                    break;
            }

            if (PlacesListBox != null)
            {
                PlacesListBox.ItemsSource = filteredPlaces;
            }
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

            PlacesListBox.ItemsSource = filteredPlaces;
        }

        private void UpdateSortingButtonContent()
        {
            switch (currentSortingMode)
            {
                case SortingMode.Category:
                    SortingButton.Content = "Sort: Category";
                    break;
                case SortingMode.Rating:
                    SortingButton.Content = "Sort: Rating";
                    break;
                case SortingMode.Closest:
                    SortingButton.Content = "Sort: Closest";
                    break;
            }
        }

        private void SortByCategoryAscending_Click(object sender, RoutedEventArgs e)
        {
            var sortedPlaces = places.OrderBy(p => p.Rating).ToList();
            places.Clear();
            foreach (var place in sortedPlaces)
            {
                places.Add(place);
            }
        }

        private void SortByCategoryDescending_Click(object sender, RoutedEventArgs e)
        {
            var sortedPlaces = places.OrderByDescending(p => p.Rating).ToList();
            places.Clear();
            foreach (var place in sortedPlaces)
            {
                places.Add(place);
            }
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
            UpdateSortingButtonContent();
        }

        public void UpdatePlaceDetails()
        {
            panel.DataContext = SelectedPlace;
        }
    }
}




