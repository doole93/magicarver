using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;

namespace MagiCarver.EnergyFunctions
{
    public class Sobel : EnergyFunction
    {
        #region Other Methods

        /// <summary>
        /// Gets a pixel in the Sobel way. Pixels out of the array are put back inside in a linear matter.
        /// </summary>
        /// <param name="bitmapData"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="size"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Gets the Sobel energy of a specific pixel. Explanation here: http://en.wikipedia.org/wiki/Sobel_operator and in the project book.
        /// </summary>
        /// <param name="bitmapData"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        private static byte GetSobelEnergy(BitmapData bitmapData, int x, int y, Size size)
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

        #endregion

        #region Implementation of EnergyFunction

        /// <summary>
        /// Computes the entire image energy.
        /// </summary>
        /// <param name="bitmapData"></param>
        /// <param name="size"></param>
        public override void ComputeEnergy(BitmapData bitmapData, Size size)
        {
            EnergyMap = new int[bitmapData.Width, bitmapData.Height];

            Parallel.For(0, size.Width, x =>
            {
                for (int y = 0; y < size.Height; ++y)
                {
                    EnergyMap[x, y] = GetSobelEnergy(bitmapData, x, y, size);
                }
            });
        }

        /// <summary>
        /// Refreshes the pixel's energy
        /// </summary>
        /// <param name="bitmapData"></param>
        /// <param name="oldSize"></param>
        /// <param name="newSize"></param>
        /// <param name="direction"></param>
        public override void ComputeLocalEnergy(BitmapData bitmapData, Size oldSize, Size newSize, Constants.Direction direction)
        {
            int[,] newEnergyMap = new int[newSize.Width, newSize.Height];

            if (newSize.Width < oldSize.Width || newSize.Height < oldSize.Height)
                {


                    if (direction == Constants.Direction.VERTICAL)
                    {
                        Parallel.For(0, newSize.Height, delegate(int i)
                        {
                            int skipCount = 0;
                            //
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

                                    while (j + skipCount < oldSize.Width && EnergyMap[j + skipCount, i] == -1)
                                    {
                                        skipCount++;
                                    }

                                }
                                else
                                {
                                    newEnergyMap[j, i] = EnergyMap[j + skipCount, i];
                                }
                            }
                        });
                    }
                    else
                    {
                        Parallel.For(0, newSize.Width, delegate(int i)
                        {
                            int skipCount = 0;

                            for (int j = 0; j < newSize.Height; ++j)
                            {
                                if (EnergyMap[i, j + skipCount] == -1)
                                {
                                    newEnergyMap[i, j] =
                                       GetSobelEnergy(bitmapData, i, j,
                                                      new Size(bitmapData.Width,
                                                               bitmapData.Height));
                                    if (j > 0)
                                    {
                                        newEnergyMap[i, j - 1] =
                                            GetSobelEnergy(bitmapData, i, j - 1,
                                                           new Size(bitmapData.Width,
                                                                    bitmapData.Height));
                                    }

                                    while (j + skipCount < oldSize.Height && EnergyMap[i, j + skipCount] == -1)
                                    {
                                        skipCount++;
                                    }

                                }
                                else
                                {
                                    newEnergyMap[i, j] = EnergyMap[i, j + skipCount];
                                }
                            }
                        });
                    }
                }
                else
                {
                    if (direction == Constants.Direction.VERTICAL)
                    {
                        Parallel.For(0, oldSize.Height, delegate(int i)
                        {
                             int skipCount = 0;
                            
                             for (int j = 0; j < oldSize.Width; ++j)
                            {
                                if (EnergyMap[j, i] == -1)
                                {
                                    if (j > 0)
                                    {
                                        newEnergyMap[j - 1 + skipCount, i] =
                                            GetSobelEnergy(bitmapData, j - 1, i,
                                                           new Size(bitmapData.Width,
                                                                    bitmapData.Height));
                                    }

                                    do
                                    {
                                        newEnergyMap[j + skipCount, i] =
                                            GetSobelEnergy(bitmapData, j, i,
                                                           new Size(bitmapData.Width,
                                                                    bitmapData.Height));
                                        skipCount++;

                                        newEnergyMap[j + skipCount, i] =
                                            GetSobelEnergy(bitmapData, j, i,
                                                           new Size(bitmapData.Width,
                                                                    bitmapData.Height));

                                    } while (++j < oldSize.Width && EnergyMap[j, i] == -1);

                                    if (j >= oldSize.Width){
                                        continue;
                                    }

                                    newEnergyMap[j + skipCount, i] =
                                        GetSobelEnergy(bitmapData, j, i,
                                                       new Size(bitmapData.Width,
                                                                bitmapData.Height));
                                }
                                else
                                {
                                    newEnergyMap[j + skipCount, i] = EnergyMap[j, i];
                                }
                            }
                        });
                    }else
                    {
                        Parallel.For(0, oldSize.Width, delegate(int i)
                        {
                            int skipCount = 0;

                            for (int j = 0; j < oldSize.Height; ++j)
                            {
                                if (EnergyMap[i, j] == -1)
                                {
                                    if (j > 0)
                                    {
                                        newEnergyMap[i, j - 1 + skipCount] =
                                            GetSobelEnergy(bitmapData, i, j - 1,
                                                           new Size(bitmapData.Width,
                                                                    bitmapData.Height));
                                    }

                                    do
                                    {
                                        newEnergyMap[i, j + skipCount] =
                                            GetSobelEnergy(bitmapData, i, j,
                                                           new Size(bitmapData.Width,
                                                                    bitmapData.Height));
                                        skipCount++;

                                        newEnergyMap[i, j + skipCount] =
                                            GetSobelEnergy(bitmapData, i, j,
                                                           new Size(bitmapData.Width,
                                                                    bitmapData.Height));

                                    } while (++j < oldSize.Height && EnergyMap[i, j] == -1);

                                    if (j >= oldSize.Height)
                                    {
                                        continue;
                                    }

                                    newEnergyMap[i, j + skipCount] =
                                        GetSobelEnergy(bitmapData, i, j,
                                                       new Size(bitmapData.Width,
                                                                bitmapData.Height));
                                }
                                else
                                {
                                    newEnergyMap[i, j + skipCount] = EnergyMap[i, j];
                                }
                            }
                        }); 
                    }
                }
                EnergyMap = newEnergyMap;
            
        }

        #endregion
    }
}
