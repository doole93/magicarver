﻿using System;
using System.Collections.Generic;
using System.Windows;

namespace MagiCarver
{
    public class SeamPointArgs : EventArgs
    {
        #region Properties

        public List<Point> Points { get; set; }

        #endregion

        #region CTors

        public SeamPointArgs(Point[] points)
        {
            Points = new List<Point>(points);

            //foreach (System.Drawing.Point point1 in points)
            //{
            //    Points.Add(new Point(point1.X, point1.Y));
            //}
        }

        #endregion
    }
}
