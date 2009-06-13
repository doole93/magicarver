using System.Collections.Generic;
using System.Drawing;

namespace MagiCarver.SeamFunctions
{
    public class CompareSeam : IComparer<Point>
    {
        public int Compare(Point x, Point y)
        {
            return x.X < y.X ? -1 : x.X > y.X ? 1 : 0;
        }
    }
}
