using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Project
{
    public class PlaceService
    {
        static public List<Place> places = new List<Place>();

        public void AddPlace(Place place)
        {
            places.Add(place);
        }

        public List<Place> SearchPlaces(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return places;

            return places
                .Where(p => p.Name.ToLower().Contains(searchTerm.ToLower()) ||
                           p.Category.ToLower().Contains(searchTerm.ToLower()))
                .ToList();
        }

        static public List<Place> GetAllPlaces()
        {
            return places;
        }
        public List<string> GetUniqueCategories()
        {
            return places
                .Select(p => p.Category) 
                .Distinct()               
                .ToList();
        }

        static public List<Place> SortByCategory(List<Place> places)
        {
            return places
                .OrderBy(p => p.Category)
                .ThenBy(p => p.Name)
                .ToList();
        }

        static public List<Place> SortByRating(List<Place> places)
        {
            return places
                .OrderByDescending(p => p.Rating)
                .ThenBy(p => p.Name)
                .ToList();
        }


        public static double CalculateDistance(double x, double y)
        {
            double centerX = 606, centerY = 338;
            return Math.Round(Math.Sqrt(Math.Pow(x - centerX, 2) + Math.Pow(y - centerY, 2)) / 181, 2);

        }

        static public List<Place> SortByDistance(List<Place> places)
        {
            return places
                .OrderBy(p => CalculateDistance(p.X, p.Y))
                .ThenBy(p => p.Name)
                .ToList();
        }
    }
}
