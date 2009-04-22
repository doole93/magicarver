using System.Collections.Generic;
using System.Drawing;

namespace MagiCarver.SeamFunctions
{
    public class CumulativeEnergyComparePairs : IComparer<KeyValuePair<Point, double>>
    {
        public int Compare(KeyValuePair<Point, double> x, KeyValuePair<Point, double> y)
        {
            return x.Value < y.Value ? -1 : x.Value > y.Value ? 1 : 0;
        }
    }
}
