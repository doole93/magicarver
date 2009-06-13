using System.Collections.Generic;
using System.Drawing;

namespace MagiCarver.SeamFunctions
{
    public class CompareSeam2 : IComparer<Seam>
    {
        public int Compare(Seam x, Seam y)
        {
            return x.StartIndex < y.StartIndex ? -1 : x.StartIndex > y.StartIndex ? 1 : 0;
        }
    }
}
