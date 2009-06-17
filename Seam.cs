using System.Windows;

namespace MagiCarver
{
    public class Seam
    {
        #region Properties

        public Point[] PixelLocations { get; set; }

        public Constants.Direction Direction { get; set; }

        public int StartIndex { get; set; }

        public double SeamValue { get; set; }

        #endregion
    }
}
