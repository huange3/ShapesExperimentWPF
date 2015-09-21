using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.IO;

namespace ShapesExperimentWPF
{
    public class Shape
    {
        public int ShapeID { get; set; } = 0;
        public bool IsBucket { get; set; } = false;
        public Uri ImagePath { get; set; }
        public Point Location { get; set; }

        public Shape(int id, string imagePath, bool isBucket = false)
        {
            this.ShapeID = id;
            this.IsBucket = isBucket;
            this.ImagePath = new Uri(Constants.ImageRoot + imagePath);
        }
    }
}
