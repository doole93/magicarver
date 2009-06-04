using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Threading;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using MagiCarver.EnergyFunctions;
using MagiCarver.SeamFunctions;
using PixelFormat=System.Drawing.Imaging.PixelFormat;
using Point=System.Drawing.Point;
using Size=System.Drawing.Size;

namespace MagiCarver
{
    public class SeamImage
    {
        private Bitmap           m_Bitmap               { get; set; }
        private Bitmap           m_EnergyMapBitmap      { get; set; }
        private BitmapData       m_BitmapData           { get; set; }

        private int[,]           m_HorizontalIndexMap   { get; set; }
        private int[,] m_VerticalIndexMap { get; set; }

        private List<Seam> m_HorizontalSeams { get; set; }
        private List<Seam> m_VerticalSeams { get; set; }

        private int              m_CurrentWidth         { get; set; }
        private int              m_CurrentHeight        { get; set; }

        private EnergyFunction   m_EnergyFunction       { get; set; }
        private CumulativeEnergy m_SeamFunction         { get; set; }

        private List<KeyValuePair<Point, Constants.EnergyType>> m_UserEnergy { get; set; }

        public event EventHandler ImageChanged;
        public event EventHandler OperationCompleted;
        public event EventHandler ColorSeam;

        public SeamImage(Bitmap bitmap, EnergyFunction energyFunction)
        {
            m_EnergyFunction = energyFunction;

            m_SeamFunction = new CumulativeEnergy {EnergyFunction = m_EnergyFunction};

            Bitmap = bitmap;

            m_HorizontalIndexMap = new int[m_CurrentWidth, m_CurrentHeight];
            m_VerticalIndexMap = new int[m_CurrentWidth, m_CurrentHeight];
        }

        public Size ImageSize
        {
            get
            {
                return new Size(m_CurrentWidth, m_CurrentHeight);  
            } 
        }

        public Bitmap Bitmap
        {
            get
            {
                return m_Bitmap;
            }
            set
            {
                m_Bitmap = value;

                m_CurrentWidth = m_Bitmap.Width;
                m_CurrentHeight = m_Bitmap.Height;

                RecomputeEntireEnergy();
            }
        }

        private void RecomputeEntireEnergy()
        {
            m_BitmapData = m_Bitmap.LockBits(new Rectangle(0, 0, m_Bitmap.Width, m_Bitmap.Height),
                                ImageLockMode.ReadWrite, m_Bitmap.PixelFormat);

            m_EnergyFunction.ComputeEnergy(m_BitmapData, ImageSize);

            m_Bitmap.UnlockBits(m_BitmapData);
        }

        public void RecomputeEntireMap()
        {
            m_SeamFunction.ComputeEntireEnergyMap(Constants.Direction.VERTICAL, ImageSize);
            m_SeamFunction.ComputeEntireEnergyMap(Constants.Direction.HORIZONTAL, ImageSize);
        }

        protected virtual void OnImageChanged()
        {
            if (ImageChanged != null)
            {
                ImageChanged(this, EventArgs.Empty);
            }
        }

        protected virtual void OnOperationComplete()
        {
            if (OperationCompleted != null)
            {
                OperationCompleted(this, EventArgs.Empty);
            }
        }

        protected virtual void OnColorSeam(IEnumerable<Point> points)
        {
            if (ColorSeam != null)
            {
                ColorSeam(this, new SeamPointArgs(points));
            }
        }

        private static void SetPixel(BitmapData bmd, int x, int y, byte red, byte green, byte blue)
        {
            unsafe
            {
                byte* row = (byte*)bmd.Scan0 + (y * bmd.Stride) + (x * 3);
                row[0] = blue;
                row[1] = green;
                row[2] = red;
            }
        }

        //private void ShiftPixels(BitmapData bmd, Constants.Direction direction, int x, int y)
        //{
        //    int dstIndex, srcIndex, maxIndex;
        //    //int offset = 0, fromIndex = 0, toIndex = 0;

        //    if (direction == Constants.Direction.VERTICAL)
        //    {
        //        unsafe
        //        {
        //            byte* row = (byte*)bmd.Scan0 + (y * bmd.Stride) + (x * 3);

        //            srcIndex = 3;
        //            maxIndex = (m_CurrentWidth - x - 1) * 3;

        //            for (dstIndex = 0; dstIndex < maxIndex; dstIndex++)
        //            {
        //                row[dstIndex] = row[srcIndex];
        //                srcIndex++;
        //            }

        //            row[dstIndex] = 0;
        //            row[dstIndex + 1] = 0;
        //            row[dstIndex + 2] = 0;
        //        }

        //        //offset = y;
        //        //fromIndex = x;
        //        //toIndex = m_CurrentWidth;
        //    }
        //    else if (direction == Constants.Direction.HORIZONTAL)
        //    {

        //        unsafe
        //        {
        //            dstIndex = y;
        //            srcIndex = dstIndex + 1;
        //            maxIndex = m_CurrentHeight - 1;
        //            byte* row;

        //            for (dstIndex = y; dstIndex < maxIndex; dstIndex++)
        //            {
        //                row = (byte*)bmd.Scan0 + (dstIndex * bmd.Stride) + (x * 3);
        //                byte* row2 = (byte*)bmd.Scan0 + (srcIndex * bmd.Stride) + (x * 3);

        //                row[0] = row2[0];
        //                row[1] = row2[1];
        //                row[2] = row2[2];
        //                srcIndex++;
        //            }

        //            row = (byte*)bmd.Scan0 + (dstIndex * bmd.Stride) + (x * 3);
        //            row[0] = 0;
        //            row[1] = 0;
        //            row[2] = 0;
        //        }

        //        //offset = x;
        //        //fromIndex = y;
        //        //toIndex = m_CurrentHeight;
        //    }

        //    //Utilities.ShiftArray(m_EnergyFunction.EnergyMap, direction, offset, fromIndex, toIndex, byte.MaxValue);

        //}

        private List<Seam> GetKBestSeams(Constants.Direction direction, int k)
        {
            if ((m_CurrentWidth < k) && (m_CurrentHeight < k))
            {
                return null;
            }

            List<Seam> seams = new List<Seam>();

            int[,] indexMap = (direction == Constants.Direction.VERTICAL ? m_VerticalIndexMap : m_HorizontalIndexMap);

            for (int i = 0; i < k; ++i)
            {
                Seam currentLowestEnergySeam;
                if (direction == Constants.Direction.VERTICAL || direction == Constants.Direction.HORIZONTAL)
                {
                    currentLowestEnergySeam = m_SeamFunction.GetKthLowestEnergySeam(direction, ImageSize, i, indexMap);								
                }
                else
                {
                    Seam isLowest1 = m_SeamFunction.GetKthLowestEnergySeam(Constants.Direction.VERTICAL, ImageSize, i, indexMap);
                    Seam isLowest2 = m_SeamFunction.GetKthLowestEnergySeam(Constants.Direction.HORIZONTAL, ImageSize, i, indexMap);

                    currentLowestEnergySeam = isLowest1.SeamValue < isLowest2.SeamValue ? isLowest1 : isLowest2;
                }

                seams.Add(currentLowestEnergySeam);
            }

            return seams;
        }

        //private void CarveSeam(Seam seam)
        //{
        //    foreach (Point p in seam.PixelLocations(ImageSize))
        //    {
        //        ShiftPixels(m_BitmapData, seam.Direction, p.X, p.Y);
        //    }

        //    if (seam.Direction == Constants.Direction.VERTICAL)
        //    {
        //        m_CurrentWidth--;
        //    }
        //    else
        //    {
        //        m_CurrentHeight--;
        //    }
        //}

        public void Carve(Constants.Direction direction, bool paintSeam, int k)
        {
            //Seam currentLowestEnergySeam;

            //List<Seam> seamsForRemoval = new List<Seam>();

            //for (int i = 0; i < k; ++i)
            //{
            //    if (direction == Constants.Direction.VERTICAL)
            //    {
            //        currentLowestEnergySeam = m_VerticalSeams[i];
            //    }
            //    else if (direction == Constants.Direction.HORIZONTAL)
            //    {
            //        currentLowestEnergySeam = m_HorizontalSeams[i];
            //        m_HorizontalSeams.RemoveAt(i);
            //    }
            //    else
            //    {
            //        currentLowestEnergySeam = m_VerticalSeams[i].SeamValue < m_HorizontalSeams[i].SeamValue ? m_VerticalSeams[i] : m_HorizontalSeams[i];
            //    }

            //    if (paintSeam)
            //    {
            //        OnColorSeam(currentLowestEnergySeam.PixelLocations(ImageSize));
            //    }

            //    seamsForRemoval.Add(currentLowestEnergySeam);
            //}

            //CarveSeams(seamsForRemoval);

            CarveSeams(direction, k);

            OnImageChanged();

            //m_EnergyFunction.ComputeLocalEnergy(m_BitmapData, ImageSize, lowestEnergySeam);

            //m_SeamFunction.RecomputeEnergyMapRange(lowestEnergySeam, ImageSize);

            OnOperationComplete();
        }

        private void CarveSeams(Constants.Direction direction, int k)
        {
            int newWidth = direction == Constants.Direction.VERTICAL ? m_CurrentWidth - k : m_CurrentWidth;
            int newHeight = direction == Constants.Direction.HORIZONTAL ? m_CurrentHeight - k : m_CurrentHeight;

            Bitmap newBitmap = new Bitmap(newWidth, newHeight, m_Bitmap.PixelFormat);

            BitmapData newBmd = newBitmap.LockBits(new Rectangle(0, 0, newBitmap.Width, newBitmap.Height),
                                                   ImageLockMode.WriteOnly, newBitmap.PixelFormat);
            BitmapData oldBmd = m_Bitmap.LockBits(new Rectangle(0, 0, m_Bitmap.Width, m_Bitmap.Height),
                                                   ImageLockMode.ReadOnly, m_Bitmap.PixelFormat);

            int[,] indexMap = (direction == Constants.Direction.VERTICAL ? m_VerticalIndexMap : m_HorizontalIndexMap);

            Dictionary<int, List<Point>> removedPixels = new Dictionary<int, List<Point>>();

            if (direction == Constants.Direction.VERTICAL)
            {
                unsafe
                {
                    byte* dest = (byte*)newBmd.Scan0;
                    byte* src = (byte*)oldBmd.Scan0;

                    for (int i = 0; i < m_CurrentHeight; ++i)
                    {
                        for (int j = 0; j < m_CurrentWidth; ++j)
                        {
                            if (indexMap[j, i] < k)
                            {
                                if (!removedPixels.ContainsKey(indexMap[j, i]))
                                {
                                    removedPixels.Add(indexMap[j, i], new List<Point>());
                                }

                                removedPixels[indexMap[j, i]].Add(new Point(j, i));
                                src += 3;
                                continue;
                            }

                            dest[0] = src[0];
                            dest[1] = src[1];
                            dest[2] = src[2];
                            src += 3;
                            dest += 3;
                        }
                    }
                }
            }else
            {
                unsafe
                {
                    for (int i = 0; i < m_CurrentWidth; ++i)
                    {
                        byte* dest = (byte*)newBmd.Scan0 + i * 3;
                        byte* src = (byte*)oldBmd.Scan0 + i * 3;

                        for (int j = 0; j < m_CurrentHeight; ++j)
                        {
                            if (indexMap[i, j] < k)
                            {

                                if (!removedPixels.ContainsKey(indexMap[i, j]))
                                {
                                    removedPixels.Add(indexMap[i, j], new List<Point>());
                                }

                                removedPixels[indexMap[i, j]].Add(new Point(i, j));
                                src += oldBmd.Stride;
                                continue;
                            }

                            dest[0] = src[0];
                            dest[1] = src[1];
                            dest[2] = src[2];
                            src += oldBmd.Stride;
                            dest += newBmd.Stride;
                        }
                    }
                } 
            }

            //foreach (List<Point> pointList in removedPixels.Values)
            //{
            //    OnColorSeam(pointList);  
            //}

            
            newBitmap.UnlockBits(newBmd);
            m_Bitmap.UnlockBits(oldBmd);

            m_Bitmap = newBitmap;
            m_BitmapData = newBmd;

            m_CurrentWidth = newWidth;
            m_CurrentHeight = newHeight;
        }

        private void GenerateEnergyMapBitmap()
        {
            m_EnergyMapBitmap = new Bitmap(m_Bitmap.Width, m_Bitmap.Height, PixelFormat.Format24bppRgb);

            BitmapData energyMapBmd = m_EnergyMapBitmap.LockBits(new Rectangle(0, 0, m_EnergyMapBitmap.Width, m_EnergyMapBitmap.Height),
                ImageLockMode.ReadWrite, m_EnergyMapBitmap.PixelFormat);

            for (int y = 0; y < energyMapBmd.Height; y++)
            {
                for (int x = 0; x < energyMapBmd.Width; x++)
                {
                    // ZTODO: Need to fix this.
                    byte grayValue = (byte) m_EnergyFunction.EnergyMap[x, y];
                    SetPixel(energyMapBmd, x, y, grayValue, grayValue, grayValue);
                }
            }

            m_EnergyMapBitmap.UnlockBits(energyMapBmd);
        }

        public Bitmap EnergyMapBitmap
        {
            get
            {
                GenerateEnergyMapBitmap();

                return m_EnergyMapBitmap;
            }
        }

        public void AddSeam(Constants.Direction direction, Size minimumSize, bool paintSeam)
        {
            Seam lowestEnergySeam;

            while ((m_CurrentWidth < minimumSize.Width) && (m_CurrentHeight < minimumSize.Height))
            {

                if (direction == Constants.Direction.VERTICAL || direction == Constants.Direction.HORIZONTAL)
                {
                	lowestEnergySeam = m_SeamFunction.GetKthLowestEnergySeam(direction, ImageSize, 1, null);
                }
                else
                {
                    Seam isLowest1 = m_SeamFunction.GetKthLowestEnergySeam(Constants.Direction.VERTICAL, ImageSize, 1, null);
                    Seam isLowest2 = m_SeamFunction.GetKthLowestEnergySeam(Constants.Direction.HORIZONTAL, ImageSize, 1, null);

                    lowestEnergySeam = isLowest1.SeamValue < isLowest2.SeamValue ? isLowest1 : isLowest2;
                }

                if (lowestEnergySeam.Direction == Constants.Direction.VERTICAL)
                {
                    m_CurrentWidth++;
                }else
                {
                    m_CurrentHeight++;
                }

                Bitmap newBitmap = new Bitmap(m_CurrentWidth, m_CurrentHeight, PixelFormat.Format24bppRgb);

                m_BitmapData = m_Bitmap.LockBits(new Rectangle(0, 0, m_Bitmap.Width, m_Bitmap.Height),
                ImageLockMode.ReadWrite, m_Bitmap.PixelFormat);

                BitmapData newBitmapData = newBitmap.LockBits(new Rectangle(0, 0, newBitmap.Width, newBitmap.Height),
                ImageLockMode.ReadWrite, newBitmap.PixelFormat);

                CopyBitmap(m_BitmapData, newBitmapData);

                m_Bitmap.UnlockBits(m_BitmapData);

                m_Bitmap = newBitmap;
                m_BitmapData = newBitmapData;

                AddSeamToBitmap(lowestEnergySeam);

                m_Bitmap.UnlockBits(m_BitmapData);

                OnImageChanged();

                if (paintSeam)
                {
                    OnColorSeam(lowestEnergySeam.PixelLocations(ImageSize));
                }

                m_EnergyFunction.ComputeEnergy(m_BitmapData, ImageSize);
                m_SeamFunction.ComputeEntireEnergyMap(Constants.Direction.VERTICAL, ImageSize);
                m_SeamFunction.ComputeEntireEnergyMap(Constants.Direction.HORIZONTAL, ImageSize);

                OnOperationComplete();

    //            m_EnergyFunction.ComputeLocalEnergy(m_BitmapData, ImageSize, lowestEnergySeam);

             //   m_SeamFunction.RecomputeEnergyMapRange(lowestEnergySeam, ImageSize);
            }
        }

        private static void CopyBitmap(BitmapData src, BitmapData dest)
        {
            unsafe
            {
                byte* srcBits = (byte*)src.Scan0;
                byte* destBits = (byte*)dest.Scan0;

                int yLimit = Math.Min(src.Height, dest.Height);
                int xLimit = Math.Min(dest.Stride, src.Stride);

                for (int y = 0; y < yLimit; ++y)
                {
                    for (int x = 0; x < xLimit; ++x)
                    {
                        destBits[x + y*dest.Stride] = srcBits[x + y*src.Stride];
                    }
                }
            }
        }

        private void AddSeamToBitmap(Seam seam)
        {
            foreach (Point p in seam.PixelLocations(ImageSize))
            {
                ShiftAddPixels(m_BitmapData, seam.Direction, p.X, p.Y);
            }
        }

        private void ShiftAddPixels(BitmapData bmd, Constants.Direction direction, int x, int y)
        {
            int dstIndex;
            //int offset = 0, fromIndex = 0, toIndex = 0;

            if (direction == Constants.Direction.VERTICAL)
            {
                unsafe
                {
                    byte* row = (byte*)bmd.Scan0 + (y * bmd.Stride) + (x * 3);

                    //Inclusive

                    for (dstIndex = (m_CurrentWidth - x - 1) * 3; dstIndex > 6; dstIndex -= 3)
                    {
                        row[dstIndex] = row[dstIndex - 3];
                        row[dstIndex + 1] = row[dstIndex - 2];
                        row[dstIndex + 2] = row[dstIndex - 1];
                    }

                    row[dstIndex] = (byte) ((row[dstIndex - 3] + row[dstIndex + 3]) / 2);
                    row[dstIndex + 1] = (byte)((row[dstIndex - 2] + row[dstIndex + 4]) / 2);
                    row[dstIndex + 2] = (byte)((row[dstIndex - 1] + row[dstIndex + 5]) / 2);
                }

                //offset = y;
                //fromIndex = x;
                //toIndex = m_CurrentWidth - 1;
            }
            else if (direction == Constants.Direction.HORIZONTAL)
            {

                unsafe
                {
                    byte* row, row2;

                    for (dstIndex = bmd.Height - 1; dstIndex > y + 1; --dstIndex)
                    {
                        row = (byte*)bmd.Scan0 + (dstIndex * bmd.Stride) + (x * 3);
                        row2 = (byte*)bmd.Scan0 + ((dstIndex - 1) * bmd.Stride) + (x * 3);

                        row[0] = row2[0];
                        row[1] = row2[1];
                        row[2] = row2[2];
                    }

                    row = (byte*)bmd.Scan0 + (dstIndex * bmd.Stride) + (x * 3);
                    row2 = (byte*)bmd.Scan0 + ((dstIndex - 1) * bmd.Stride) + (x * 3);
                    byte* row3 = (byte*)bmd.Scan0 + ((dstIndex + 1) * bmd.Stride) + (x * 3);

                    row[0] = (byte)((row2[0] + row3[0]) / 2);
                    row[1] = (byte)((row2[1] + row3[1]) / 2);
                    row[2] = (byte)((row2[2] + row3[2]) / 2);
                }

                //offset = x;
                //fromIndex = y;
                //toIndex = m_CurrentHeight;
            }

         //   Utilities.ShiftAddArray(m_EnergyFunction.EnergyMap, direction, offset, fromIndex, toIndex, byte.MaxValue);
        }

        public void SetEnergy(StrokeCollection strokes)
        {
            m_UserEnergy = new List<KeyValuePair<Point, Constants.EnergyType>>();

            foreach (Stroke stroke in strokes)
            {
                bool isHigh = stroke.DrawingAttributes.Color == Colors.Yellow ? true : false;

                foreach (StylusPoint point in stroke.StylusPoints)
                {
                    m_UserEnergy.Add(new KeyValuePair<Point, Constants.EnergyType>(new Point((int)point.X, (int)point.Y), isHigh ? Constants.EnergyType.MAX : Constants.EnergyType.MIN));
                }
            }

            RefineEnergy();
        }

        private void RefineEnergy()
        {
            foreach (KeyValuePair<Point, Constants.EnergyType> userEnergy in m_UserEnergy)
            {
                if (userEnergy.Key.X < 0 || userEnergy.Key.Y < 0 || userEnergy.Key.X >= m_CurrentWidth || userEnergy.Key.Y >= m_CurrentHeight)
                {
                    continue;
                }
                m_EnergyFunction.EnergyMap[userEnergy.Key.X, userEnergy.Key.Y] = (userEnergy.Value ==
                                                                                         Constants.EnergyType.MAX
                                                                                             ? 50000
                                                                                             : -50000);
            }
        }

        public void CalculateIndexMaps()
        {

            m_BitmapData = m_Bitmap.LockBits(new Rectangle(0, 0, m_Bitmap.Width, m_Bitmap.Height),
                                                    ImageLockMode.ReadWrite, m_Bitmap.PixelFormat);

            Thread tHorizontal = new Thread(delegate()
                                                {
                                                    m_HorizontalSeams = GetKBestSeams(Constants.Direction.HORIZONTAL, m_CurrentHeight); ;
                                                });

            Thread tVertical = new Thread(delegate()
                                              {
                                                  m_VerticalSeams = GetKBestSeams(Constants.Direction.VERTICAL, m_CurrentWidth);
                                              });

            tHorizontal.Start();
            tVertical.Start();

            tHorizontal.Join();
            tVertical.Join();

            m_Bitmap.UnlockBits(m_BitmapData);
        }
    }
}
