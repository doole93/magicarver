using System;
using System.Collections.Generic;
using MagiCarver.EnergyFunctions;
using System.Drawing;

namespace MagiCarver.SeamFunctions
{
    public class CumulativeEnergy
    {
        #region Data Members

        public EnergyFunction EnergyFunction { get; set; }

        private int[,] VerticalCumulativeEnergyMap { get; set; }
        private int[,] HorizontalCumulativeEnergyMap { get; set; }

        private bool[,] VerticalCumulativeEnergyMapUsed { get; set; }
        private bool[,] HorizontalCumulativeEnergyMapUsed { get; set; }

        private List<KeyValuePair<Point, double>> LowestVerticalSeamsEnergy;
        private List<KeyValuePair<Point, double>> LowestHorizontalSeamsEnergy;

        //private bool VerticalMapDirty { get; set; }
        //private bool HorizontalMapDirty { get; set; }

        #endregion

        #region Other Methods

        public void ComputeEntireEnergyMap(Constants.Direction direction, Size size)
        {
            int endOffset;

            if (direction == Constants.Direction.VERTICAL)
            {
                VerticalCumulativeEnergyMap = new int[size.Width, size.Height];
                VerticalCumulativeEnergyMapUsed = new bool[size.Width, size.Height];
                LowestVerticalSeamsEnergy = new List<KeyValuePair<Point, double>>();
                endOffset = size.Width - 1;
            }
            else
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
            int[,] cumulativeEnergyMap ;
            int x = 0, y = 0, xInc = 0, yInc = 0, pixelCount;

            if (direction == Constants.Direction.VERTICAL)
            {
                cumulativeEnergyMap = VerticalCumulativeEnergyMap;
                x = startOffset;
                yInc = 1;
                pixelCount = size.Height;
            }
            else
            {
                cumulativeEnergyMap = HorizontalCumulativeEnergyMap;
                y = startOffset;
                xInc = 1;
                pixelCount = size.Width;
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

            if ((direction == Constants.Direction.VERTICAL) && (x >= endOffset))
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
                    int e0, e1, e2, lowestEnergy;

                    if (direction == Constants.Direction.VERTICAL)
                    {
                        e0 = Utilities.InBounds(x - 1, y - 1, size) ? GetCumulativeEnergy(direction, x - 1, y - 1) : int.MaxValue;
                        e1 = Utilities.InBounds(x, y - 1, size) ? GetCumulativeEnergy(direction, x, y - 1) : int.MaxValue;
                        e2 = Utilities.InBounds(x + 1, y - 1, size) ? GetCumulativeEnergy(direction, x + 1, y - 1) : int.MaxValue;
                    }
                    else
                    {
                        e0 = Utilities.InBounds(x - 1, y - 1, size) ? GetCumulativeEnergy(direction, x - 1, y - 1) : int.MaxValue;
                        e1 = Utilities.InBounds(x - 1, y, size) ? GetCumulativeEnergy(direction, x - 1, y) : int.MaxValue;
                        e2 = Utilities.InBounds(x - 1, y + 1, size) ? GetCumulativeEnergy(direction, x - 1, y + 1) : int.MaxValue;
                    }

                    if (e0 < e1)
                    {
                        lowestEnergy = e0 < e2 ? e0 : e2;
                    }
                    else
                    {
                        lowestEnergy = e1 < e2 ? e1 : e2;
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

            //if (direction == Constants.Direction.VERTICAL)
            //{
            //    VerticalMapDirty = false;
            //}
            //else
            //{
            //    HorizontalMapDirty = false;
            //}

            List<KeyValuePair<Point, double>> lowestSeamsEnergy;

            x = y = xInc = yInc = 0;

            if (direction == Constants.Direction.VERTICAL)
            {
                lowestSeamsEnergy = LowestVerticalSeamsEnergy;
                y = size.Height - 1;
                xInc = 1;
            }
            else
            {
                lowestSeamsEnergy = LowestHorizontalSeamsEnergy;
                x = size.Width - 1;
                yInc = 1;
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
            if (direction == Constants.Direction.VERTICAL)
            {
                return VerticalCumulativeEnergyMap[x, y];
            }

            if (direction == Constants.Direction.HORIZONTAL)
            {
                return HorizontalCumulativeEnergyMap[x, y];
            }

            throw new ArgumentOutOfRangeException("direction");
        }

        public void RecomputeEnergyMapRange(Seam seam, Size size)
        {
            //if (seam.Direction == Constants.Direction.VERTICAL)
            //{
            //    foreach (Point p in seam.PixelLocations(size))
            //    {
            //        Utilities.ShiftArray(VerticalCumulativeEnergyMap, seam.Direction, p.Y, p.X, size.Width, int.MaxValue);
            //    }

            //    ComputeSubEnergyMap(Constants.Direction.VERTICAL, seam.StartIndex, seam.StartIndex, size);
            //    HorizontalMapDirty = true;
            //}
            //else
            //{
            //    foreach (Point p in seam.PixelLocations(size))
            //    {
            //        Utilities.ShiftArray(HorizontalCumulativeEnergyMap, seam.Direction, p.X, p.Y, size.Height, int.MaxValue);
            //    }

            //    ComputeSubEnergyMap(Constants.Direction.HORIZONTAL, seam.StartIndex, seam.StartIndex, size);
            //    VerticalMapDirty = true;
            //}
        }

        public Seam GetKthLowestEnergySeam(Constants.Direction direction, Size size, int k, int[,] indexMap)
        {
            //if (direction == Constants.Direction.VERTICAL)
            //{
            //    // If the direction we're evaluating is "dirty," force a recompute
            //    if (VerticalMapDirty)
            //    {
            //        ComputeEntireEnergyMap(Constants.Direction.VERTICAL, size);
            //        HorizontalMapDirty = true;
            //    }
            //}
            //else
            //{
            //    // If the direction we're evaluating is "dirty," force a recompute
            //    if (HorizontalMapDirty)
            //    {
            //        ComputeEntireEnergyMap(Constants.Direction.HORIZONTAL, size);
            //        VerticalMapDirty = true;
            //    }
            //}

            List<KeyValuePair<Point, double>> lowestSeamsEnergy = (direction == Constants.Direction.VERTICAL ? LowestVerticalSeamsEnergy : LowestHorizontalSeamsEnergy);

            Seam seam = BuildSeam(direction, lowestSeamsEnergy[k].Key.X, lowestSeamsEnergy[k].Key.Y, size, indexMap, k);

            seam.SeamValue = lowestSeamsEnergy[k].Value;

            return seam;
        }

        private Seam BuildSeam(Constants.Direction direction, int x, int y, Size size, int[,] indexMap, int k)
        {

            SetUsedPixel(direction, x, y, size);

            indexMap[x, y] = k;

            int pixelCount = direction == Constants.Direction.VERTICAL ? size.Height : size.Width;

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
                KeyValuePair<Point, int> chosenNeighbour;

                leftNeighbour = GetNeighbour(direction, x, y, size, Constants.NeighbourType.LEFT);
                straightNeighbour = GetNeighbour(direction, x, y, size, Constants.NeighbourType.STRAIGHT);
                rightNeighbour = GetNeighbour(direction, x, y, size, Constants.NeighbourType.RIGHT);
                
				if (leftNeighbour.Value < straightNeighbour.Value)
				{
					chosenNeighbour = leftNeighbour.Value < rightNeighbour.Value ? leftNeighbour : rightNeighbour;
				}
				else
				{
					chosenNeighbour = !(straightNeighbour.Value < rightNeighbour.Value) ? rightNeighbour : straightNeighbour;
				}

                if (chosenNeighbour.Equals(rightNeighbour))
                {
                    seam.PixelDirections[pixelIndex] = Constants.SeamPixelDirection.RIGHT;
                }
                else if (chosenNeighbour.Equals(leftNeighbour))
                {
                    seam.PixelDirections[pixelIndex] = Constants.SeamPixelDirection.LEFT;
                }

                SetUsedPixel(direction, chosenNeighbour.Key.X, chosenNeighbour.Key.Y, size);

                x = chosenNeighbour.Key.X;
                y = chosenNeighbour.Key.Y;

                indexMap[x, y] = k;

				pixelIndex--;	
            }

            seam.Direction = direction;

            seam.StartIndex = direction == Constants.Direction.VERTICAL ? x : y;

            return seam;
        }

        private KeyValuePair<Point, int> GetNeighbour(Constants.Direction direction, int x, int y, Size size, Constants.NeighbourType type)
        {
            int currentX = x, currentY = y, currentEnergy = int.MaxValue;
            int xInc = 0, yInc = 0;

            if (direction == Constants.Direction.VERTICAL)
            {
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
                            GetUsedPixel(direction, currentX, currentY - 1) ? 
                            currentEnergy : GetCumulativeEnergy(direction, currentX, currentY - 1));
                        //yInc = -1;
                        //break;
                    default:
                        throw new ArgumentOutOfRangeException("type");
                }
            }
            else
            {
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
                            GetUsedPixel(direction, currentX - 1, currentY) ? 
                            currentEnergy : GetCumulativeEnergy(direction, currentX - 1, currentY));
                        //xInc = -1;
                        //break;
                    default:
                        throw new ArgumentOutOfRangeException("type");
                }
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

        #endregion
    }
}
