using System.Drawing.Imaging;
using Size=System.Drawing.Size;
using System.Threading;

namespace MagiCarver.EnergyFunctions
{
    public abstract class EnergyFunction
    {
        #region Properties

        public int[,] EnergyMap { get; protected set; }

        #endregion

        #region Abstract Methods

        protected abstract byte GetPixelEnergy(BitmapData bitmapData, int x, int y, Size size);

        #endregion

        #region Other Methods

        /// <summary>
        /// Computes the entire image energy.
        /// </summary>
        /// <param name="bitmapData"></param>
        /// <param name="size"></param>
        public void ComputeEnergy(BitmapData bitmapData, Size size)
        {
            EnergyMap = new int[bitmapData.Width, bitmapData.Height];

            Parallel.For(0, size.Width, x =>
            {
                for (int y = 0; y < size.Height; ++y)
                {
                    EnergyMap[x, y] = GetPixelEnergy(bitmapData, x, y, size);
                }
            });
        }

        //TODO: Vertical shrink has changed for better. Need to reflect on horizontal.
        /// <summary>
        /// Refreshes the pixel's energy
        /// </summary>
        /// <param name="bitmapData"></param>
        /// <param name="oldSize"></param>
        /// <param name="newSize"></param>
        /// <param name="direction"></param>
        public void ComputeLocalEnergy(BitmapData bitmapData, Size oldSize, Size newSize, Constants.Direction direction)
        {
            int[,] newEnergyMap = new int[newSize.Width, newSize.Height];

            if (newSize.Width < oldSize.Width || newSize.Height < oldSize.Height)
            {
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
                                   GetPixelEnergy(bitmapData, j, i,
                                                  new Size(bitmapData.Width,
                                                           bitmapData.Height));
                                if (j > 0)
                                {
                                    if (EnergyMap[j - 1 + skipCount, i] != Constants.MIN_ENERGY && EnergyMap[j - 1 + skipCount, i] != Constants.MAX_ENERGY)
                                    {
                                        newEnergyMap[j - 1, i] =
                                            GetPixelEnergy(bitmapData, j - 1, i,
                                            new Size(bitmapData.Width,
                                                bitmapData.Height));  
                                    }else
                                    {
                                        newEnergyMap[j - 1, i] = EnergyMap[j - 1 + skipCount, i];
                                    }
                                }

                                while (j + skipCount < oldSize.Width && EnergyMap[j + skipCount, i] == -1)
                                {
                                    skipCount++;
                                }

                                //if (j + 1 < newSize.Width)
                                //{
                                //    if (EnergyMap[j + skipCount, i] != Constants.MIN_ENERGY && EnergyMap[j + skipCount, i] != Constants.MAX_ENERGY)
                                //    {
                                //        newEnergyMap[j + 1, i] = GetPixelEnergy(bitmapData, j + 1, i,
                                //            new Size(bitmapData.Width,
                                //                bitmapData.Height)); 
                                //    }else
                                //    {
                                //        newEnergyMap[j + 1, i] = EnergyMap[j + skipCount, i];
                                //    }
                                //}
                            }
                            else
                            {
                                if (EnergyMap[j + skipCount, i] != Constants.MIN_ENERGY && EnergyMap[j + skipCount, i] != Constants.MAX_ENERGY)
                                {
                                    newEnergyMap[j, i] = GetPixelEnergy(bitmapData, j, i,
                                            new Size(bitmapData.Width,
                                                bitmapData.Height)); 
                                }else
                                {
                                    newEnergyMap[j, i] = EnergyMap[j + skipCount, i];
                                }
                                
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
                                   GetPixelEnergy(bitmapData, i, j,
                                                  new Size(bitmapData.Width,
                                                           bitmapData.Height));
                                if (j > 0)
                                {
                                    if (EnergyMap[i, j - 1 + skipCount] != Constants.MIN_ENERGY && EnergyMap[i, j - 1 + skipCount] != Constants.MAX_ENERGY)
                                    {
                                        newEnergyMap[i, j - 1] =
                                            GetPixelEnergy(bitmapData, i, j - 1,
                                            new Size(bitmapData.Width,
                                                bitmapData.Height));
                                    }
                                    else
                                    {
                                        newEnergyMap[i, j - 1] = EnergyMap[i, j - 1 + skipCount];
                                    }
                                }

                                while (j + skipCount < oldSize.Height && EnergyMap[i, j + skipCount] == -1)
                                {
                                    skipCount++;
                                }

                            }
                            else
                            {
                                if (EnergyMap[i, j + skipCount] != Constants.MIN_ENERGY && EnergyMap[i, j + skipCount] != Constants.MAX_ENERGY)
                                {
                                    newEnergyMap[i, j] = GetPixelEnergy(bitmapData, i, j,
                                            new Size(bitmapData.Width,
                                                bitmapData.Height));
                                }
                                else
                                {
                                    newEnergyMap[i, j] = EnergyMap[i, j + skipCount];
                                }
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
                                        GetPixelEnergy(bitmapData, j - 1, i,
                                                       new Size(bitmapData.Width,
                                                                bitmapData.Height));
                                }

                                do
                                {
                                    newEnergyMap[j + skipCount, i] =
                                        GetPixelEnergy(bitmapData, j, i,
                                                       new Size(bitmapData.Width,
                                                                bitmapData.Height));
                                    skipCount++;

                                    newEnergyMap[j + skipCount, i] =
                                        GetPixelEnergy(bitmapData, j, i,
                                                       new Size(bitmapData.Width,
                                                                bitmapData.Height));

                                } while (++j < oldSize.Width && EnergyMap[j, i] == -1);

                                if (j >= oldSize.Width)
                                {
                                    continue;
                                }

                                newEnergyMap[j + skipCount, i] =
                                    GetPixelEnergy(bitmapData, j, i,
                                                   new Size(bitmapData.Width,
                                                            bitmapData.Height));
                            }
                            else
                            {
                                newEnergyMap[j + skipCount, i] = EnergyMap[j, i];
                            }
                        }
                    });
                }
                else
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
                                        GetPixelEnergy(bitmapData, i, j - 1,
                                                       new Size(bitmapData.Width,
                                                                bitmapData.Height));
                                }

                                do
                                {
                                    newEnergyMap[i, j + skipCount] =
                                        GetPixelEnergy(bitmapData, i, j,
                                                       new Size(bitmapData.Width,
                                                                bitmapData.Height));
                                    skipCount++;

                                    newEnergyMap[i, j + skipCount] =
                                        GetPixelEnergy(bitmapData, i, j,
                                                       new Size(bitmapData.Width,
                                                                bitmapData.Height));

                                } while (++j < oldSize.Height && EnergyMap[i, j] == -1);

                                if (j >= oldSize.Height)
                                {
                                    continue;
                                }

                                newEnergyMap[i, j + skipCount] =
                                    GetPixelEnergy(bitmapData, i, j,
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

        /// <summary>
        /// Gets a pixel. Pixels out of the array are put back inside in a linear matter.
        /// </summary>
        /// <param name="bitmapData"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        protected double GetPixelValue(BitmapData bitmapData, int x, int y, Size size)
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

        #endregion
    }
}
