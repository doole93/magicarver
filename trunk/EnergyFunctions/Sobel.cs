using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;

namespace MagiCarver.EnergyFunctions
{
    public class Sobel : EnergyFunction
    {
        #region Other Methods

        private static double GetSobelPixel(BitmapData bitmapData, int x, int y, Size size)
        {
            if (x < 0)
            {
                x = 0;
            }
            else if (x >= size.Width)
            {
                x = size.Width - 1;
            }

            if (y < 0)
            {
                y = 0;
            }
            else if (y >= size.Height)
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
                pixels[1] = (byte)GetSobelPixel(bitmapData, x, y - 1, size);
                pixels[2] = (byte)GetSobelPixel(bitmapData, x + 1, y - 1, size);
                pixels[3] = (byte)GetSobelPixel(bitmapData, x - 1, y, size);
                pixels[4] = (byte)GetSobelPixel(bitmapData, x, y, size);
                pixels[5] = (byte)GetSobelPixel(bitmapData, x + 1, y, size);
                pixels[6] = (byte)GetSobelPixel(bitmapData, x - 1, y + 1, size);
                pixels[7] = (byte)GetSobelPixel(bitmapData, x, y + 1, size);
                pixels[8] = (byte)GetSobelPixel(bitmapData, x + 1, y + 1, size);

                int xSobel = pixels[0] + (pixels[1] + pixels[1]) + pixels[2] - pixels[6] - (pixels[7] + pixels[7]) - pixels[8];
                int ySobel = pixels[2] + (pixels[5] + pixels[5]) + pixels[8] - pixels[0] - (pixels[3] + pixels[3]) - pixels[6];

                int sobel = (int)Math.Sqrt((xSobel * xSobel) + (ySobel * ySobel));

                if (sobel > 255)
                {
                    sobel = 255;
                }

                return (byte)sobel;
            }

            return byte.MaxValue;
        }

        #endregion

        #region Implementation of EnergyFunction

        public override void ComputeEnergy(BitmapData bitmapData, Size size)
        {
            EnergyMap = new int[bitmapData.Width, bitmapData.Height];

            DateTime a = DateTime.Now;

            for (int x = 0; x < size.Width; ++x)
            {
                int index0 = x;
                Parallel.For(0, size.Height, y =>
                  {
                      EnergyMap[index0, y] = GetSobelEnergy(bitmapData, index0, y, size);
                  });
            }

            //for (int x = 0; x < size.Width; ++x)
            //{
            //    for (int y = 0; y < size.Height; ++y)
            //    {
            //        EnergyMap[x, y] = GetSobelEnergy(bitmapData, x, y, size);
            //    }
            //}

            TimeSpan b = DateTime.Now - a;

            Console.Write(b.Milliseconds);
        }

        public override void ComputeLocalEnergy(BitmapData bitmapData, Size oldSize, Size newSize, Constants.Direction direction)
        {
            if (bitmapData != null)
            {
                int[,] newEnergyMap = new int[newSize.Width, newSize.Height];

                if (direction == Constants.Direction.VERTICAL)
                {
                    Parallel.For(0, newSize.Height, delegate(int i)
                                                     {
                                                         int skipCount = 0;

                                                         for (int j = 0; j < newSize.Width; ++j)
                                                         {
                                                             if (EnergyMap[j + skipCount, i] == -1)
                                                             {
                                                                 newEnergyMap[j, i] =
                                                                    GetSobelEnergy(bitmapData, j, i,
                                                                                   new Size(bitmapData.Width,
                                                                                            bitmapData.Height));
                                                                 if (j > 0)
                                                                 {
                                                                     newEnergyMap[j - 1, i] =
                                                                         GetSobelEnergy(bitmapData, j - 1, i,
                                                                                        new Size(bitmapData.Width,
                                                                                                 bitmapData.Height));
                                                                 }

                                                                 while (EnergyMap[j + skipCount, i] == -1)
                                                                 {
                                                                     skipCount++; 
                                                                 }
                                                                 
                                                             }else
                                                             {
                                                                 newEnergyMap[j, i] = EnergyMap[j + skipCount, i];
                                                             }
                                                         }
                                                     });


                }
                else
                {
                    DateTime a = DateTime.Now;
                    for (int i = 0; i < oldSize.Width; ++i)
                    {
                        for (int j = 0; j < oldSize.Height; ++j)
                         {
                             EnergyMap[i, j] =
                                 GetSobelEnergy(bitmapData, i, j,
                                                new Size(bitmapData.Width,
                                                         bitmapData.Height));

                             if (EnergyMap[i, j] == -1)
                             {
                                 if (i > 0)
                                 {
                                     EnergyMap[i - 1, j] =
                                         GetSobelEnergy(bitmapData, i - 1, j,
                                                        new Size(bitmapData.Width,
                                                                 bitmapData.Height));
                                 }

                                 if (i < oldSize.Height - 1)
                                 {
                                     i++;
                                 }
                             }
                         }
                    }
                }
                EnergyMap = newEnergyMap;
            }
        }

        #endregion
    }
}
