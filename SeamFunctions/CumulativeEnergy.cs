using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Imaging;
using MagiCarver.EnergyFunctions;
using System.Drawing;

/*
 * 
 * ShiftPixels has to be per list of seams, and not per seams.
 * The same goes for the local updates...
 * 
 * */

namespace MagiCarver.SeamFunctions
{
    public class CumulativeEnergy
    {
        public EnergyFunction EnergyFunction { get; set; }

        private int[,] VerticalCumulativeEnergyMap { get; set; }
        private int[,] HorizontalCumulativeEnergyMap { get; set; }

        private bool[,] VerticalCumulativeEnergyMapUsed { get; set; }
        private bool[,] HorizontalCumulativeEnergyMapUsed { get; set; }

        private List<KeyValuePair<Point, double>> LowestVerticalSeamsEnergy;
        private List<KeyValuePair<Point, double>> LowestHorizontalSeamsEnergy;

        private bool VerticalMapDirty { get; set; }
        private bool HorizontalMapDirty { get; set; }

        public void ComputeEntireEnergyMap(Constants.Direction direction, Size size)
        {
            int endOffset = 0;

            if (direction == Constants.Direction.VERTICAL)
            {
                VerticalCumulativeEnergyMap = new int[size.Width, size.Height];
                VerticalCumulativeEnergyMapUsed = new bool[size.Width, size.Height];
                LowestVerticalSeamsEnergy = new List<KeyValuePair<Point, double>>();
                endOffset = size.Width - 1;
            }
            else if (direction == Constants.Direction.HORIZONTAL)
            {
                HorizontalCumulativeEnergyMap = new int[size.Width, size.Height];
                HorizontalCumulativeEnergyMapUsed = new bool[size.Width, size.Height];
                LowestHorizontalSeamsEnergy = new List<KeyValuePair<Point, double>>();
                endOffset = size.Height - 1;
            }

            ComputeSubEnergyMap(direction, 0, endOffset, size);
        }

        private void ComputeSubEnergyMap(Constants.Direction direction, int startOffset, int endOffset, Size size)
        {
            int[,] cumulativeEnergyMap;
            int x = 0, y = 0, xInc = 0, yInc = 0, pixelCount;

            if (direction == Constants.Direction.VERTICAL)
            {
                cumulativeEnergyMap = VerticalCumulativeEnergyMap;
                x = startOffset;
                yInc = 1;
                pixelCount = size.Height;
            }
            else if (direction == Constants.Direction.HORIZONTAL)
            {
                cumulativeEnergyMap = HorizontalCumulativeEnergyMap;
                y = startOffset;
                xInc = 1;
                pixelCount = size.Width;
            }
            else
            {
                throw new InvalidEnumArgumentException();
            }

            if (pixelCount > startOffset)
            {
                while ((x < size.Width) && y < (size.Height))
                {
                    cumulativeEnergyMap[x, y] = EnergyFunction.GetEnergy(x, y, size);

                    x += yInc;
                    y += xInc;
                }
            }

            x += xInc;
            y += yInc;

            if ((direction == Constants.Direction.VERTICAL) && (x > endOffset))
            {
                startOffset--;
                if (startOffset < 0)
                {
                    startOffset = 0;
                }
                endOffset++;
                if (endOffset > size.Width - 1)
                {
                    endOffset = size.Width - 1;
                }

                x = startOffset;
            }
            else if (direction == Constants.Direction.HORIZONTAL && y >= endOffset)
            {
                startOffset--;
                if (startOffset < 0)
                {
                    startOffset = 0;
                }
                endOffset++;
                if (endOffset > size.Height - 1)
                {
                    endOffset = size.Height - 1;
                }

                y = startOffset;
            }
            while (x < size.Width && y < size.Height)
            {
                while (x < size.Width && y < size.Height)
                {
                    int e0 = 0, e1 = 0, e2 = 0, lowestEnergy;

                    if (direction == Constants.Direction.VERTICAL)
                    {
                        e0 = Utilities.InBounds(x - 1, y - 1, size) ? GetCumulativeEnergy(direction, x - 1, y - 1) : int.MaxValue;
                        e1 = Utilities.InBounds(x, y - 1, size) ? GetCumulativeEnergy(direction, x, y - 1) : int.MaxValue;
                        e2 = Utilities.InBounds(x + 1, y - 1, size) ? GetCumulativeEnergy(direction, x + 1, y - 1) : int.MaxValue;
                    }
                    else if (direction == Constants.Direction.HORIZONTAL)
                    {
                        e0 = Utilities.InBounds(x - 1, y - 1, size) ? GetCumulativeEnergy(direction, x - 1, y - 1) : int.MaxValue;
                        e1 = Utilities.InBounds(x - 1, y, size) ? GetCumulativeEnergy(direction, x - 1, y) : int.MaxValue;
                        e2 = Utilities.InBounds(x - 1, y + 1, size) ? GetCumulativeEnergy(direction, x - 1, y + 1) : int.MaxValue;
                    }

                    if (e0 < e1)
                    {
                        if (e0 < e2)
                        {
                            lowestEnergy = e0;
                        }
                        else
                        {
                            lowestEnergy = e2;
                        }
                    }
                    else
                    {
                        if (e1 < e2)
                        {
                            lowestEnergy = e1;
                        }
                        else
                        {
                            lowestEnergy = e2;
                        }
                    }

                    cumulativeEnergyMap[x, y] =
                        EnergyFunction.GetEnergy(x, y, size) + lowestEnergy;

                    x += yInc;
                    y += xInc;
                }

                x += xInc;
                y += yInc;

                if (direction == Constants.Direction.VERTICAL && x >= endOffset)
                {
                    startOffset--;
                    if (startOffset < 0)
                    {
                        startOffset = 0;
                    }
                    endOffset++;
                    if (endOffset > size.Width - 1)
                    {
                        endOffset = size.Width - 1;
                    }

                    x = startOffset;
                }
                else if (direction == Constants.Direction.HORIZONTAL && y >= endOffset)
                {
                    startOffset--;
                    if (startOffset < 0)
                    {
                        startOffset = 0;
                    }
                    endOffset++;
                    if (endOffset > size.Height - 1)
                    {
                        endOffset = size.Height - 1;
                    }

                    y = startOffset;
                }
            }

            if (direction == Constants.Direction.VERTICAL)
            {
                VerticalMapDirty = false;
            }
            else if (direction == Constants.Direction.HORIZONTAL)
            {
                HorizontalMapDirty = false;
            }

            List<KeyValuePair<Point, double>> lowestSeamsEnergy;

            if (direction == Constants.Direction.VERTICAL)
            {
                lowestSeamsEnergy = LowestVerticalSeamsEnergy;
                x = 0;
                y = size.Height - 1;
                xInc = 1;
                yInc = 0;
            }
            else if (direction == Constants.Direction.HORIZONTAL)
            {
                lowestSeamsEnergy = LowestHorizontalSeamsEnergy;
                x = size.Width - 1;
                y = 0;
                xInc = 0;
                yInc = 1;
            }else
            {
                throw new ArgumentOutOfRangeException("Bad direction.");
            }
            
            lowestSeamsEnergy.Clear();

            while ((x < size.Width) && (y < size.Height))
            {
                lowestSeamsEnergy.Add(new KeyValuePair<Point, double>(new Point(x, y), GetCumulativeEnergy(direction, x, y)));

                x += xInc;
                y += yInc;
            }

            lowestSeamsEnergy.Sort(new CumulativeEnergyComparePairs());
        }

        private void SetUsedPixel(Constants.Direction direction, int x, int y, Size size)
        {
            if (!Utilities.InBounds(x, y, size))
            {
                throw new ArgumentOutOfRangeException();
            }
            if (direction == Constants.Direction.VERTICAL)
            {
                VerticalCumulativeEnergyMapUsed[x, y] = true;
            }else
            {
                HorizontalCumulativeEnergyMapUsed[x, y] = true;
            }
        }

        private bool GetUsedPixel(Constants.Direction direction, int x, int y)
        {
            return direction == Constants.Direction.VERTICAL ? (VerticalCumulativeEnergyMapUsed[x, y]) : (HorizontalCumulativeEnergyMapUsed[x, y]);
        }

        private int GetCumulativeEnergy(Constants.Direction direction, int x, int y)
        {
            switch (direction)
            {
                case Constants.Direction.VERTICAL:

                    return VerticalCumulativeEnergyMap[x, y];
                case Constants.Direction.HORIZONTAL:

                    return HorizontalCumulativeEnergyMap[x, y];
                case Constants.Direction.OPTIMAL:
                    break;
                default:
                    throw new ArgumentOutOfRangeException("direction");
            }

            throw new Exception("Should not happen.");
        }

        public void RecomputeEnergyMapRange(Seam seam, Size size)
        {
            if (seam.Direction == Constants.Direction.VERTICAL)
            {
                foreach (Point p in seam.PixelLocations(size))
                {
                    Utilities.ShiftArray(VerticalCumulativeEnergyMap, seam.Direction, p.Y, p.X, size.Width, int.MaxValue);
                }

                ComputeSubEnergyMap(Constants.Direction.VERTICAL, seam.StartIndex, seam.StartIndex, size);
                HorizontalMapDirty = true;
            }
            else if (seam.Direction == Constants.Direction.HORIZONTAL)
            {
                foreach (Point p in seam.PixelLocations(size))
                {
                    Utilities.ShiftArray(HorizontalCumulativeEnergyMap, seam.Direction, p.X, p.Y, size.Height, int.MaxValue);
                }

                ComputeSubEnergyMap(Constants.Direction.HORIZONTAL, seam.StartIndex, seam.StartIndex, size);
                VerticalMapDirty = true;
            }
        }

        public Seam GetKthLowestEnergySeam(Constants.Direction direction, Size size, int k, int[,] indexMap)
        {
            if ((k >= size.Width) && (k >= size.Height))
            {
                throw new ArgumentOutOfRangeException("k too big");
            }

            // Set initial values
            if (direction == Constants.Direction.VERTICAL)
            {
                // If the direction we're evaluating is "dirty," force a recompute
                if (VerticalMapDirty)
                {
                    ComputeEntireEnergyMap(Constants.Direction.VERTICAL, size);
                    HorizontalMapDirty = true;
                }
            }
            else if (direction == Constants.Direction.HORIZONTAL)
            {
                // If the direction we're evaluating is "dirty," force a recompute
                if (HorizontalMapDirty)
                {
                    ComputeEntireEnergyMap(Constants.Direction.HORIZONTAL, size);
                    VerticalMapDirty = true;
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException("bad direction");
            }

            List<KeyValuePair<Point, double>> lowestSeamsEnergy = (direction == Constants.Direction.VERTICAL ? LowestVerticalSeamsEnergy : LowestHorizontalSeamsEnergy);

            Seam seam = BuildSeam(direction, lowestSeamsEnergy[k].Key.X, lowestSeamsEnergy[k].Key.Y, size, indexMap, k);

            seam.SeamValue = lowestSeamsEnergy[k].Value;

            return seam;
        }

        private Seam BuildSeam(Constants.Direction direction, int x, int y, Size size, int[,] indexMap, int k)
        {

            SetUsedPixel(direction, x, y, size);

            indexMap[x, y] = k;

            int pixelCount = 0, xInc = 0, yInc = 0;                           

            if (direction == Constants.Direction.VERTICAL)
            {
                yInc = 1;
                pixelCount = size.Height;
            }
            else if (direction == Constants.Direction.HORIZONTAL)
            {
                xInc = 1;
                pixelCount = size.Width;
            }

            Seam seam = new Seam
                            {
                                PixelDirections = new Constants.SeamPixelDirection[pixelCount]
                            };

            int pixelIndex = pixelCount - 1;



            while (pixelIndex > 0)
            {           
                KeyValuePair<Point, int> leftNeighbour;
                KeyValuePair<Point, int> straightNeighbour;
                KeyValuePair<Point, int> rightNeighbour;

                leftNeighbour = GetNeighbour(direction, x, y, size, Constants.NeighbourType.LEFT);
                straightNeighbour = GetNeighbour(direction, x, y, size, Constants.NeighbourType.STRAIGHT);
                rightNeighbour = GetNeighbour(direction, x, y, size, Constants.NeighbourType.RIGHT);
                
								if (leftNeighbour.Value < straightNeighbour.Value)
								{
									if (leftNeighbour.Value < rightNeighbour.Value)
									{
										if (direction == Constants.Direction.VERTICAL)
										{
											seam.PixelDirections[pixelIndex] = Constants.SeamPixelDirection.LEFT;
										}
										else if (direction == Constants.Direction.HORIZONTAL)
										{
										    seam.PixelDirections[pixelIndex] = Constants.SeamPixelDirection.LEFT;
										}
                                        SetUsedPixel(direction, leftNeighbour.Key.X, leftNeighbour.Key.Y, size);

									    x = leftNeighbour.Key.X;
									    y = leftNeighbour.Key.Y;

                                        indexMap[x, y] = k;
									}
									else
									{
										if (direction == Constants.Direction.VERTICAL)
										{
											seam.PixelDirections[pixelIndex] = Constants.SeamPixelDirection.RIGHT;
										}
										else if (direction == Constants.Direction.HORIZONTAL)
										{
										    seam.PixelDirections[pixelIndex] = Constants.SeamPixelDirection.RIGHT;
										}
										SetUsedPixel(direction, rightNeighbour.Key.X, rightNeighbour.Key.Y, size);

									    x = rightNeighbour.Key.X;
									    y = rightNeighbour.Key.Y;

                                        indexMap[x, y] = k;
									}
								}
								else
								{
									if (!(straightNeighbour.Value < rightNeighbour.Value))
									{
										if (direction == Constants.Direction.VERTICAL)
										{
											seam.PixelDirections[pixelIndex] = Constants.SeamPixelDirection.RIGHT;
										}
										else if (direction == Constants.Direction.HORIZONTAL)
										{
                                            seam.PixelDirections[pixelIndex] = Constants.SeamPixelDirection.RIGHT;
										}
										SetUsedPixel(direction, rightNeighbour.Key.X, rightNeighbour.Key.Y, size);

                                        x = rightNeighbour.Key.X;
                                        y = rightNeighbour.Key.Y;

                                        indexMap[x, y] = k;
									}
									else
									{
										SetUsedPixel(direction, straightNeighbour.Key.X, straightNeighbour.Key.Y, size);

									    x = straightNeighbour.Key.X;
									    y = straightNeighbour.Key.Y;

                                        indexMap[x, y] = k;
									}
								}

								pixelIndex--;
							
            }

            seam.Direction = direction;
            if (direction == Constants.Direction.VERTICAL)
            {
                seam.StartIndex = x;
            }
            else if (direction == Constants.Direction.HORIZONTAL)
            {
                seam.StartIndex = y;
            }

            return seam;
        }

        private KeyValuePair<Point, int> GetNeighbour(Constants.Direction direction, int x, int y, Size size, Constants.NeighbourType type)
        {
            int currentX = x, currentY = y, currentEnergy = int.MaxValue;
            int xInc = 0, yInc = 0;

            switch (direction)
            {
                case Constants.Direction.VERTICAL:

                    switch (type)
                    {
                        case Constants.NeighbourType.LEFT:
                            currentY--;
                            xInc = -1;
                            break;
                        case Constants.NeighbourType.RIGHT:
                            currentY--;
                            xInc = 1;
                            break;
                        case Constants.NeighbourType.STRAIGHT:
                            return new KeyValuePair<Point, int>(new Point(currentX, currentY - 1),
                                                                GetUsedPixel(direction, currentX, currentY - 1)
                                                                    ? currentEnergy
                                                                    : GetCumulativeEnergy(direction, currentX,
                                                                                          currentY - 1));
                            yInc = -1;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException("type");
                    }
                    break;
                case Constants.Direction.HORIZONTAL:


                    switch (type)
                    {
                        case Constants.NeighbourType.LEFT:
                            currentX--;
                            yInc = 1;
                            break;
                        case Constants.NeighbourType.RIGHT:
                            currentX--;
                            yInc = -1;
                            break;
                        case Constants.NeighbourType.STRAIGHT:
                            return new KeyValuePair<Point, int>(new Point(currentX - 1, currentY),
                                                                GetUsedPixel(direction, currentX - 1, currentY)
                                                                    ? currentEnergy
                                                                    : GetCumulativeEnergy(direction, currentX - 1,
                                                                                          currentY));
                            xInc = -1;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException("type");
                    }
                    break;
                case Constants.Direction.OPTIMAL:
                    break;
                default:
                    throw new ArgumentOutOfRangeException("direction");
            }

            while (Utilities.InBounds(currentX += xInc, currentY += yInc, size))
            {
                if (GetUsedPixel(direction, currentX, currentY))
                {
                    continue;
                }

                currentEnergy = GetCumulativeEnergy(direction, currentX, currentY);
                break;
            }

            return new KeyValuePair<Point, int>(new Point(currentX, currentY), currentEnergy);
        }
    }
}
