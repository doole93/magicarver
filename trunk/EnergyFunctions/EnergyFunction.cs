using System.Drawing.Imaging;
using Size=System.Drawing.Size;

namespace MagiCarver.EnergyFunctions
{
    public abstract class EnergyFunction
    {
        public int[,] EnergyMap { get; protected set; }

        public int GetEnergy(int x, int y, Size size)
        {
            if (Utilities.InBounds(x, y, size))
            {
                return EnergyMap[x, y];
            }

            return byte.MaxValue;
        }

        public abstract void ComputeEnergy(BitmapData bitmapData, Size size);

        public abstract void ComputeLocalEnergy(BitmapData bitmapData, Size size, Constants.Direction direction);
    }
}
