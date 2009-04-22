using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace MagiCarver.EnergyFunctions
{
    public class Sobel : EnergyFunction
    {
        public override void ComputeEnergy(BitmapData bitmapData, Size size)
        {

            EnergyMap = new byte[bitmapData.Width, bitmapData.Height];

            for (int x = 0; x < size.Width; ++x)
            {
                for (int y = 0; y < size.Height; ++y)
                {
                    EnergyMap[x, y] = GetSobelEnergy(bitmapData, x, y, size);
                }
            }
        }

        public override void ComputeLocalEnergy(BitmapData bitmapData, Size size, Seam seam)
        {
            if (bitmapData != null)
            {
                foreach (Point p in seam.PixelLocations(size))
                {
                    if (seam.Direction == Constants.Direction.VERTICAL)
                    {
                        if (p.X > 0)
                        {
                            EnergyMap[p.X - 1, p.Y] = GetSobelEnergy(bitmapData, p.X - 1, p.Y, size);
                        }
                        if (p.X < size.Width)
                        {
                            EnergyMap[p.X, p.Y] = GetSobelEnergy(bitmapData, p.X, p.Y, size);
                        }
                    }
                    else
                    {
                        if (p.Y > 0)
                        {
                            EnergyMap[p.X, p.Y - 1] = GetSobelEnergy(bitmapData, p.X, p.Y - 1, size);
                        }
                        if (p.Y < size.Height)
                        {
                            EnergyMap[p.X, p.Y] = GetSobelEnergy(bitmapData, p.X, p.Y, size);
                        }
                    }
                }
            }
        }

        private static double GetSobelPixel(BitmapData bitmapData, int x, int y, Size size)
        {
            if (x < 0)
            {
                x = 0;
            }else if (x >= size.Width)
            {
                x = size.Width - 1;
            }

            if (y < 0)
            {
                y = 0;
            }else if (y >= size.Height)
            {
                y = size.Height - 1;
            }

            return Utilities.GetPixel(bitmapData, size, x, y);
        }

        private static byte GetSobelEnergy(BitmapData bitmapData, int x, int y, Size size)
        {
            if (Utilities.InBounds(x, y, size))
            {
                byte[] pixels = new byte[9];

                pixels[0] = (byte)GetSobelPixel(bitmapData, x - 1, y - 1, size);
                pixels[1] = (byte)GetSobelPixel(bitmapData, x    , y - 1, size);
                pixels[2] = (byte)GetSobelPixel(bitmapData, x + 1, y - 1, size);
                pixels[3] = (byte)GetSobelPixel(bitmapData, x - 1, y    , size);
                pixels[4] = (byte)GetSobelPixel(bitmapData, x    , y    , size);
                pixels[5] = (byte)GetSobelPixel(bitmapData, x + 1, y    , size);
                pixels[6] = (byte)GetSobelPixel(bitmapData, x - 1, y + 1, size);
                pixels[7] = (byte)GetSobelPixel(bitmapData, x    , y + 1, size);
                pixels[8] = (byte)GetSobelPixel(bitmapData, x + 1, y + 1, size);

                int xSobel = pixels[0] + (pixels[1] + pixels[1]) + pixels[2] - pixels[6] - (pixels[7] + pixels[7]) - pixels[8];
                int ySobel = pixels[2] + (pixels[5] + pixels[5]) + pixels[8] - pixels[0] - (pixels[3] + pixels[3]) - pixels[6];

                int sobel = (int) Math.Sqrt((xSobel*xSobel) + (ySobel*ySobel));

                if (sobel > 255)
                {
                    sobel = 255;
                }

                return (byte) sobel;
            }

            return byte.MaxValue;
        }
    }
}
