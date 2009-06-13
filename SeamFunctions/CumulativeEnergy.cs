﻿using System;
using System.Collections.Generic;
using MagiCarver.EnergyFunctions;
using System.Drawing;

namespace MagiCarver.SeamFunctions
{
    public class CumulativeEnergy
    {
        #region Data Members

        public EnergyFunction EnergyFunction { get; set; }

        public int[,] VerticalCumulativeEnergyMap { get; set; }
        public int[,] HorizontalCumulativeEnergyMap { get; set; }

        private bool[,] VerticalCumulativeEnergyMapUsed { get; set; }
        private bool[,] HorizontalCumulativeEnergyMapUsed { get; set; }

        private List<KeyValuePair<Point, double>> LowestVerticalSeamsEnergy;
        private List<KeyValuePair<Point, double>> LowestHorizontalSeamsEnergy;

        #endregion

        #region Other Methods

        public void ComputeEntireEnergyMap(Constants.Direction direction, Size size)
        {
            if (direction == Constants.Direction.VERTICAL)
            {
                VerticalCumulativeEnergyMap = new int[size.Width, size.Height];
                VerticalCumulativeEnergyMapUsed = new bool[size.Width, size.Height];
                LowestVerticalSeamsEnergy = new List<KeyValuePair<Point, double>>();
            }
            else
            {
                HorizontalCumulativeEnergyMap = new int[size.Width, size.Height];
                HorizontalCumulativeEnergyMapUsed = new bool[size.Width, size.Height];
                LowestHorizontalSeamsEnergy = new List<KeyValuePair<Point, double>>();
            }

            ComputeSubEnergyMap(direction, size);
        }

        private void ComputeSubEnergyMap(Constants.Direction direction, Size size)
        {
            if (direction == Constants.Direction.VERTICAL)
            {
                for (int i = 0; i < size.Width; ++i)
                {
                    VerticalCumulativeEnergyMap[i, 0] = EnergyFunction.EnergyMap[i, 0];
                }
            }else
            {
                for (int i = 0; i < size.Height; ++i)
                {
                    HorizontalCumulativeEnergyMap[0, i] = EnergyFunction.EnergyMap[0, i];
                }
            }

            if (direction == Constants.Direction.VERTICAL)
            {
                for (int i = 1; i < size.Height; ++i)
                {
                    for (int j = 0 ; j < size.Width; ++j)
                    {
                        int lowestEnergy;

                        int e0 = j > 0 && i > 0 ? VerticalCumulativeEnergyMap[j - 1, i - 1] : int.MaxValue;
                        int e1 = i > 0 ? VerticalCumulativeEnergyMap[j, i - 1] : int.MaxValue;
                        int e2 = i > 0 && j < size.Width - 1 ? VerticalCumulativeEnergyMap[j + 1, i - 1] : int.MaxValue;

                        if (e0 < e1)
                        {
                            lowestEnergy = e0 < e2 ? e0 : e2;
                        }
                        else
                        {
                            lowestEnergy = e1 < e2 ? e1 : e2;
                        }

                        VerticalCumulativeEnergyMap[j, i] = EnergyFunction.EnergyMap[j, i] + lowestEnergy;
                    }
                }
            }else
            {
                for (int i = 1; i < size.Width; ++i)
                {
                    for (int j = 0; j < size.Height; ++j)
                    {
                        int lowestEnergy;

                        int e0 = j > 0 && i > 0 ? HorizontalCumulativeEnergyMap[i - 1, j - 1] : int.MaxValue;
                        int e1 = i > 0 ? HorizontalCumulativeEnergyMap[i - 1, j] : int.MaxValue;
                        int e2 = i > 0 && j < size.Height - 1 ? HorizontalCumulativeEnergyMap[i - 1, j + 1] : int.MaxValue;

                        if (e0 < e1)
                        {
                            lowestEnergy = e0 < e2 ? e0 : e2;
                        }
                        else
                        {
                            lowestEnergy = e1 < e2 ? e1 : e2;
                        }

                        HorizontalCumulativeEnergyMap[i, j] = EnergyFunction.EnergyMap[i, j] + lowestEnergy;
                    }
                }
            }

            ComputeLowestSeamEnergy(direction, size);
        }

        private void ComputeLowestSeamEnergy(Constants.Direction direction, Size size)
        {
            if (direction == Constants.Direction.VERTICAL)
            {
                for (int i = 0; i < size.Width; ++i)
                {
                    LowestVerticalSeamsEnergy.Add(new KeyValuePair<Point, double>(new Point(i, size.Height - 1), VerticalCumulativeEnergyMap[i, size.Height - 1]));
                }

                LowestVerticalSeamsEnergy.Sort(new CumulativeEnergyComparePairs());
            }
            else
            {
                for (int i = 0; i < size.Height; ++i)
                {
                    LowestHorizontalSeamsEnergy.Add(new KeyValuePair<Point, double>(new Point(size.Width - 1, i), HorizontalCumulativeEnergyMap[size.Width - 1, i]));
                }

                LowestHorizontalSeamsEnergy.Sort(new CumulativeEnergyComparePairs());
            }
        }

        private void SetUsedPixel(Constants.Direction direction, int x, int y)
        {
            if (direction == Constants.Direction.VERTICAL)
            {
                VerticalCumulativeEnergyMapUsed[x, y] = true;
            }
            else
            {
                HorizontalCumulativeEnergyMapUsed[x, y] = true;
            }
        }
     
        //public void RecomputeEnergyMapRange(Size size, Size oldSize, Constants.Direction direction)
        //{
        //    int maxIndex = int.MinValue, minIndex = int.MaxValue, count = 0;

        //    if (direction == Constants.Direction.VERTICAL)
        //    {
        //        int jj = 0;

        //        while (jj < oldSize.Width && VerticalCumulativeEnergyMap[jj, 0] == -1)
        //        {
        //            jj++;
        //        }

        //        for (; jj < oldSize.Width; ++jj)
        //        {
        //            if (VerticalCumulativeEnergyMap[jj, 0] == -1)
        //            {
        //                minIndex = jj;
        //                break;
        //            }
        //        }

        //        jj = oldSize.Width - 1;

        //        while (jj >= 0 && VerticalCumulativeEnergyMap[jj, 0] == -1)
        //        {
        //            jj--;
        //        }

        //        for (; jj >= 0; --jj)
        //        {
        //            if (VerticalCumulativeEnergyMap[jj, 0] == -1)
        //            {
        //                maxIndex = jj;
        //                break;
        //            }
        //        }

        //        for (int j = minIndex; j <= maxIndex; ++j)
        //        {
        //            if (VerticalCumulativeEnergyMap[j, 0] == -1)
        //            {
        //                count++;
        //            }
        //        }

        //        //ZTODO: REQUIRES DEBUGGING!
        //        //Most of the pass is unneeded.

        //        int[,] NewVerticalCumulativeEnergyMap = new int[size.Width, size.Height];

        //        for (int i = 0; i < size.Height; ++i)
        //        {
        //            int skipCount = 0;

        //            for (int j = 0; j < size.Width; ++j)
        //            {
        //                while (j + skipCount < oldSize.Width && VerticalCumulativeEnergyMap[j + skipCount, i] == -1)
        //                {
        //                    skipCount++;
        //                }

        //                if (j + skipCount < oldSize.Width)
        //                {
        //                    NewVerticalCumulativeEnergyMap[j, i] = VerticalCumulativeEnergyMap[j + skipCount, i];
        //                }
        //            }
        //        }
        //        VerticalCumulativeEnergyMapUsed = new bool[size.Width, size.Height];

        //        VerticalCumulativeEnergyMap = NewVerticalCumulativeEnergyMap;
        //    }
        //    else
        //    {

        //        for (int j = 0; j < oldSize.Height; ++j)
        //        {
        //            if (HorizontalCumulativeEnergyMap[0, j] == -1)
        //            {
        //                if (j > maxIndex)
        //                {
        //                    maxIndex = j;
        //                }

        //                if (j < minIndex)
        //                {
        //                    minIndex = j;
        //                }

        //                count++;
        //            }
        //        }

        //        for (int i = 0; i < oldSize.Width; ++i)
        //        {
        //            for (int j = 0; j < oldSize.Height; ++j)
        //            {
        //                if (HorizontalCumulativeEnergyMap[i, j] == -1)
        //                {
        //                    if (i < oldSize.Height - 1)
        //                    {
        //                        HorizontalCumulativeEnergyMap[i, j] = HorizontalCumulativeEnergyMap[i, j + 1];
        //                        i++;
        //                    }
        //                }
        //            }
        //        }
        //        HorizontalCumulativeEnergyMapUsed = new bool[size.Width, size.Height];
        //    }

        //    ComputeSubEnergyMap(direction, minIndex, maxIndex - count, size);
        //}

        public Seam GetKthLowestEnergySeam(Constants.Direction direction, Size size, int k, int[,] indexMap)
        {
            List<KeyValuePair<Point, double>> lowestSeamsEnergy = (direction == Constants.Direction.VERTICAL ? LowestVerticalSeamsEnergy : LowestHorizontalSeamsEnergy);

            Seam seam = BuildSeam(direction, lowestSeamsEnergy[k].Key.X, lowestSeamsEnergy[k].Key.Y, size, indexMap, k);

            seam.SeamValue = lowestSeamsEnergy[k].Value;

            return seam;
        }

        private Seam BuildSeam(Constants.Direction direction, int x, int y, Size size, int[,] indexMap, int k)
        {
            SetUsedPixel(direction, x, y);

            indexMap[x, y] = k;

            int pixelCount = direction == Constants.Direction.VERTICAL ? size.Height : size.Width;

            Seam seam = new Seam
            {
                PixelLocations = new int[pixelCount]
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
                    chosenNeighbour = straightNeighbour.Value < rightNeighbour.Value ? straightNeighbour : rightNeighbour;
                }

                seam.PixelLocations[pixelIndex] = direction == Constants.Direction.VERTICAL
                                                      ? chosenNeighbour.Key.X
                                                      : chosenNeighbour.Key.Y;

                SetUsedPixel(direction, chosenNeighbour.Key.X, chosenNeighbour.Key.Y);

                x = chosenNeighbour.Key.X;
                y = chosenNeighbour.Key.Y;

                indexMap[x, y] = k;

                pixelIndex--;
            }

            seam.Direction = direction;

            seam.StartIndex = direction == Constants.Direction.VERTICAL ? x : y;

            seam.PixelLocations[0] = seam.StartIndex;

            return seam;
        }

        private KeyValuePair<Point, int> GetNeighbour(Constants.Direction direction, int x, int y, Size size, Constants.NeighbourType type)
        {
            int currentX = x, currentY = y, currentEnergy = int.MaxValue;
            int distance, directionSetter;

            if (direction == Constants.Direction.VERTICAL)
            {
                switch (type)
                {
                    case Constants.NeighbourType.LEFT:
                        currentY--;
                        distance = currentX;
                        directionSetter = -1;
                        break;
                    case Constants.NeighbourType.RIGHT:
                        currentY--;
                        distance = size.Width - currentX - 1;
                        directionSetter = 1;
                        break;
                    case Constants.NeighbourType.STRAIGHT:
                        return new KeyValuePair<Point, int>(
                            new Point(currentX, currentY - 1), 
                            VerticalCumulativeEnergyMapUsed[currentX, currentY - 1] ? 
                            currentEnergy : VerticalCumulativeEnergyMap[currentX, currentY - 1]);
                    default:
                        throw new ArgumentOutOfRangeException("type");
                }

                for (int i = 0; i < distance; ++i){

                    currentX += directionSetter;

                    if (VerticalCumulativeEnergyMapUsed[currentX, currentY])
                    {
                        continue;
                    }

                    currentEnergy = VerticalCumulativeEnergyMap[currentX, currentY];
                    break;
                }
            }
            else
            {
                switch (type)
                {
                    case Constants.NeighbourType.LEFT:
                        currentX--;
                        distance = size.Height - currentY - 1;
                        directionSetter = 1;
                        break;
                    case Constants.NeighbourType.RIGHT:
                        currentX--;
                        distance = currentY;
                        directionSetter = -1;
                        break;
                    case Constants.NeighbourType.STRAIGHT:
                        return new KeyValuePair<Point, int>(new Point(currentX - 1, currentY),
                            HorizontalCumulativeEnergyMapUsed[currentX - 1, currentY] ?
                            currentEnergy : HorizontalCumulativeEnergyMap[currentX - 1, currentY]);
                    default:
                        throw new ArgumentOutOfRangeException("type");
                }

                for (int i = 0; i < distance; ++i)
                {

                    currentY += directionSetter;

                    if (HorizontalCumulativeEnergyMapUsed[currentX, currentY])
                    {
                        continue;
                    }

                    currentEnergy = HorizontalCumulativeEnergyMap[currentX, currentY];
                    break;
                }
            }

            return new KeyValuePair<Point, int>(new Point(currentX, currentY), currentEnergy);
        }

        #endregion
    }
}
