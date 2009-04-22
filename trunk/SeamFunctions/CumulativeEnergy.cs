using System;
using System.Collections.Generic;
using System.ComponentModel;
using MagiCarver.EnergyFunctions;
using System.Drawing;

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
                        e0 = GetCumulativeEnergy(direction, x - 1, y - 1, size);
                        e1 = GetCumulativeEnergy(direction, x, y - 1, size);
                        e2 = GetCumulativeEnergy(direction, x + 1, y - 1, size);
                    }
                    else if (direction == Constants.Direction.HORIZONTAL)
                    {
                        e0 = GetCumulativeEnergy(direction, x - 1, y - 1, size);
                        e1 = GetCumulativeEnergy(direction, x - 1, y, size);
                        e2 = GetCumulativeEnergy(direction, x - 1, y + 1, size);
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
                lowestSeamsEnergy.Add(new KeyValuePair<Point, double>(new Point(x, y), GetCumulativeEnergy(direction, x, y, size)));

                x += xInc;
                y += yInc;
            }

            lowestSeamsEnergy.Sort(new CumulativeEnergyComparePairs());
        }

        private void SetUsedPixel(Constants.Direction direction, int x, int y, Size size)
        {
            if (!Utilities.InBounds(x, y, size))
            {
                return;
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

        private int GetCumulativeEnergy(Constants.Direction direction, int x, int y, Size size)
        {
            if ((Utilities.InBounds(x, y, size) && (!GetUsedPixel(direction, x, y))))
            {   
                return direction == Constants.Direction.VERTICAL ? VerticalCumulativeEnergyMap[x, y] : HorizontalCumulativeEnergyMap[x, y];
            }

            return int.MaxValue;
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

        public Seam GetKthLowestEnergySeam(Constants.Direction direction, Size size, int k)
        {
            if ((k >= size.Width) && (k > size.Height))
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

            Seam seam = BuildSeam(direction, lowestSeamsEnergy[k].Key.X, lowestSeamsEnergy[k].Key.Y, size);

            seam.SeamValue = lowestSeamsEnergy[k].Value;

            return seam;
        }

        private Seam BuildSeam(Constants.Direction direction, int x, int y, Size size)
        {
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
                int e0 = 0, e1 = 0, e2 = 0;
                Point e0p = new Point(), e1p = new Point(), e2p = new Point();

                if (direction == Constants.Direction.VERTICAL)
                {
                    e0 = GetCumulativeEnergy(direction, x - 1, y - 1, size);
                    e0p = new Point(x - 1, y - 1);
                    e1 = GetCumulativeEnergy(direction, x, y - 1, size);
                    e1p = new Point(x, y - 1);
                    e2 = GetCumulativeEnergy(direction, x + 1, y - 1, size);
                    e2p = new Point(x + 1, y - 1);
                }
                else if (direction == Constants.Direction.HORIZONTAL)
                {
                    e0 = GetCumulativeEnergy(direction, x - 1, y - 1, size);
                    e0p = new Point(x - 1, y - 1);
                    e1 = GetCumulativeEnergy(direction, x - 1, y, size);
                    e1p = new Point(x - 1, y);
                    e2 = GetCumulativeEnergy(direction, x - 1, y + 1, size);
                    e2p = new Point(x - 1, y + 1);
                }

                if (e0 < e1)
                {
                    if (e0 < e2)
                    {
                        if (direction == Constants.Direction.VERTICAL)
                        {
                            x--;
                            if (x < 0)
                            {
                                x = 0;
                            }
                            else
                            {
                                seam.PixelDirections[pixelIndex] = Constants.SeamPixelDirection.LEFT;
                            }
                        }
                        else if (direction == Constants.Direction.HORIZONTAL)
                        {
                            y--;
                            if (y < 0)
                            {
                                y = 0;
                            }
                            else
                            {
                                seam.PixelDirections[pixelIndex] = Constants.SeamPixelDirection.RIGHT;
                            }
                        }

                        SetUsedPixel(direction, e0p.X, e0p.Y, size);
                    }
                    else
                    {
                        if (direction == Constants.Direction.VERTICAL)
                        {
                            x++;
                            if (x > size.Width - 1)
                            {
                                x = size.Width - 1;
                            }
                            else
                            {
                                seam.PixelDirections[pixelIndex] = Constants.SeamPixelDirection.RIGHT;
                            }
                        }
                        else if (direction == Constants.Direction.HORIZONTAL)
                        {
                            y++;
                            if (y > size.Height - 1)
                            {
                                y = size.Height - 1;
                            }
                            else
                            {
                                seam.PixelDirections[pixelIndex] = Constants.SeamPixelDirection.LEFT;
                            }
                        }
                        SetUsedPixel(direction, e2p.X, e2p.Y, size);
                    }
                }
                else
                {
                    if (!(e1 < e2))
                    {
                        if (direction == Constants.Direction.VERTICAL)
                        {
                            x++;
                            if (x > size.Width - 1)
                            {
                                x = size.Width - 1;
                            }
                            else
                            {
                                seam.PixelDirections[pixelIndex] = Constants.SeamPixelDirection.RIGHT;
                            }
                        }
                        else if (direction == Constants.Direction.HORIZONTAL)
                        {
                            y++;
                            if (y > size.Height - 1)
                            {
                                y = size.Height - 1;
                            }
                            else
                            {
                                seam.PixelDirections[pixelIndex] = Constants.SeamPixelDirection.LEFT;
                            }
                        }
                        SetUsedPixel(direction, e2p.X, e2p.Y, size);
                    }else
                    {
                        SetUsedPixel(direction, e1p.X, e1p.Y, size);
                    }
                }

                x -= xInc;
                y -= yInc;

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
    }
}
