using System.Drawing.Imaging;
using Size=System.Drawing.Size;

namespace MagiCarver.EnergyFunctions
{
    public abstract class EnergyFunction
    {
        public int[,] EnergyMap { get; protected set; }

        public abstract void ComputeEnergy(BitmapData bitmapData, Size size);

        public abstract void ComputeLocalEnergy(BitmapData bitmapData, Size oldSize, Size newSize, Constants.Direction direction);
    }
}
