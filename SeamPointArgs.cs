using System;
using System.Collections.Generic;
using System.Windows;

namespace MagiCarver
{
    public class SeamPointArgs : EventArgs
    {
        public List<Point> Points { get; set; } 

        public SeamPointArgs(IEnumerable<System.Drawing.Point> points)
        {

            Points = new List<Point>();

            foreach (System.Drawing.Point point1 in points)
            {
                Points.Add(new Point(point1.X, point1.Y));
            }
        }
    }
}
