using System.Drawing;
using System.Drawing.Imaging;

namespace MagiCarver.EnergyFunctions
{
    public abstract class EnergyFunction
    {
        public byte[,] EnergyMap { get; protected set; }

        public byte GetEnergy(int x, int y, Size size)
        {
            if (Utilities.InBounds(x, y, size))
            {
                return EnergyMap[x, y];
            }

            return byte.MaxValue;
        }

        public abstract void ComputeEnergy(BitmapData bitmapData, Size size);

        public abstract void ComputeLocalEnergy(BitmapData bitmapData, Size size, Seam seam);
    }
}
