using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project
{
    public class Place
    {
        public string Name { get; set; }
        public string Info { get; set; }
        public string Address { get; set; }
        public double Rating { get; set; }
        public string[] Schedule { get; set; }
        public string Category { get; set; }
        public string ImagePath { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double DistanceFromCenter
        {
            get
            {
                double centerX = 606, centerY = 338;
                return Math.Round(Math.Sqrt(Math.Pow(X - centerX, 2) + Math.Pow(Y - centerY, 2)) / 181, 2);
            }
        }


        public Place(string name,string info, string address, double rating, string[] schedule, string category, string imagePath, double x, double y)
        {
            Name = name;
            Info = info;
            Address = address;
            Rating = rating;
            Schedule = schedule;
            Category = category;
            ImagePath = imagePath;
            X = x;
            Y = y;
        }
        public override string ToString()
        {
            return Name;
        }
    }
}
