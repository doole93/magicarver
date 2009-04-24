using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Threading;
using MagiCarver.EnergyFunctions;
using MagiCarver.SeamFunctions;

namespace MagiCarver
{
    public class SeamImage
    {
        private Bitmap           m_Bitmap          { get; set; }
        private Bitmap           m_EnergyMapBitmap { get; set; }
        private BitmapData       m_BitmapData      { get; set; }

        private int              m_CurrentWidth    { get; set; }
        private int              m_CurrentHeight   { get; set; }

        private EnergyFunction   m_EnergyFunction  { get; set; }
        private CumulativeEnergy m_SeamFunction    { get; set; }

        public event EventHandler ImageChanged;
        public event EventHandler OperationCompleted;
        public event EventHandler ColorSeam;

        public SeamImage(Bitmap bitmap, EnergyFunction energyFunction)
        {
            m_EnergyFunction = energyFunction;

            m_SeamFunction = new CumulativeEnergy {EnergyFunction = m_EnergyFunction};

            Bitmap = bitmap;
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

                RecomputeEntireImage();
            }
        }

        private void RecomputeEntireImage()
        {
            m_BitmapData = m_Bitmap.LockBits(new Rectangle(0, 0, m_Bitmap.Width, m_Bitmap.Height),
                ImageLockMode.ReadWrite, m_Bitmap.PixelFormat);

            m_EnergyFunction.ComputeEnergy(m_BitmapData, ImageSize);

            m_Bitmap.UnlockBits(m_BitmapData);

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

        private void ShiftPixels(BitmapData bmd, Constants.Direction direction, int x, int y)
        {
            int dstIndex, srcIndex, maxIndex, offset = 0, fromIndex = 0, toIndex = 0;

            if (direction == Constants.Direction.VERTICAL)
            {
                unsafe
                {
                    byte* row = (byte*)bmd.Scan0 + (y * bmd.Stride) + (x * 3);

                    srcIndex = 3;
                    maxIndex = (m_CurrentWidth - x - 1) * 3;

                    for (dstIndex = 0; dstIndex < maxIndex; dstIndex++)
                    {
                        row[dstIndex] = row[srcIndex];
                        srcIndex++;
                    }

                    row[dstIndex] = 0;
                    row[dstIndex + 1] = 0;
                    row[dstIndex + 2] = 0;
                }

                offset = y;
                fromIndex = x;
                toIndex = m_CurrentWidth;
            }
            else if (direction == Constants.Direction.HORIZONTAL)
            {

                unsafe
                {
                    dstIndex = y;
                    srcIndex = dstIndex + 1;
                    maxIndex = m_CurrentHeight - 1;
                    byte* row;

                    for (dstIndex = y; dstIndex < maxIndex; dstIndex++)
                    {
                        row = (byte*)bmd.Scan0 + (dstIndex * bmd.Stride) + (x * 3);
                        byte* row2 = (byte*)bmd.Scan0 + (srcIndex * bmd.Stride) + (x * 3);

                        row[0] = row2[0];
                        row[1] = row2[1];
                        row[2] = row2[2];
                        srcIndex++;
                    }

                    row = (byte*)bmd.Scan0 + (dstIndex * bmd.Stride) + (x * 3);
                    row[0] = 0;
                    row[1] = 0;
                    row[2] = 0;
                }

                offset = x;
                fromIndex = y;
                toIndex = m_CurrentHeight;
            }

            //Utilities.ShiftArray(m_EnergyFunction.EnergyMap, direction, offset, fromIndex, toIndex, byte.MaxValue);

        }

        private List<Seam> GetKBestSeams(Constants.Direction direction, int k)
        {
            if ((m_CurrentWidth < k) && (m_CurrentHeight < k))
            {
                return null;
            }

            m_BitmapData = m_Bitmap.LockBits(new Rectangle(0, 0, m_Bitmap.Width, m_Bitmap.Height),
            ImageLockMode.ReadWrite, m_Bitmap.PixelFormat);

            List<Seam> seams = new List<Seam>();

            for (int i = 0; i < k; ++i)
            {
                Seam currentLowestEnergySeam;
                if (direction == Constants.Direction.VERTICAL || direction == Constants.Direction.HORIZONTAL)
                {
                	int oldi = i;
                    currentLowestEnergySeam = m_SeamFunction.GetKthLowestEnergySeam(direction, ImageSize, ref i);

                	k += (i - oldi);

										
                }
                else
                {
                    Seam isLowest1 = m_SeamFunction.GetKthLowestEnergySeam(Constants.Direction.VERTICAL, ImageSize, ref i);
                    Seam isLowest2 = m_SeamFunction.GetKthLowestEnergySeam(Constants.Direction.HORIZONTAL, ImageSize, ref i);

                    currentLowestEnergySeam = isLowest1.SeamValue < isLowest2.SeamValue ? isLowest1 : isLowest2;
                }

                seams.Add(currentLowestEnergySeam);
            }

            m_Bitmap.UnlockBits(m_BitmapData);

            return seams;
        }

        private void CarveSeam(Seam seam)
        {
            foreach (Point p in seam.PixelLocations(ImageSize))
            {
                ShiftPixels(m_BitmapData, seam.Direction, p.X, p.Y);
            }

            if (seam.Direction == Constants.Direction.VERTICAL)
            {
                m_CurrentWidth--;
            }
            else
            {
                m_CurrentHeight--;
            }
        }

        public void Carve(Constants.Direction direction, bool paintSeam, int k)
        {
            List<Seam> lowestVerticalEnergySeams = null;
            List<Seam> lowestHorizontalEnergySeams = null;

            Seam currentLowestEnergySeam;

            if (direction == Constants.Direction.VERTICAL)
            {
                lowestVerticalEnergySeams = GetKBestSeams(direction, k);
            }
            else if (direction == Constants.Direction.HORIZONTAL)
            {
                lowestHorizontalEnergySeams = GetKBestSeams(direction, k);
            }
            else
            {
                lowestVerticalEnergySeams = GetKBestSeams(Constants.Direction.VERTICAL, k);
                lowestHorizontalEnergySeams = GetKBestSeams(Constants.Direction.HORIZONTAL, k);
            }

            List<Seam> seamsForRemoval = new List<Seam>();

            for (int i = 0; i < k; ++i)
            {
                if (direction == Constants.Direction.VERTICAL)
                {
                    currentLowestEnergySeam = lowestVerticalEnergySeams[i];
                }
                else if (direction == Constants.Direction.HORIZONTAL)
                {
                    currentLowestEnergySeam = lowestHorizontalEnergySeams[i];
                }
                else
                {
                    currentLowestEnergySeam = lowestVerticalEnergySeams[i].SeamValue < lowestHorizontalEnergySeams[i].SeamValue ? lowestVerticalEnergySeams[i] : lowestHorizontalEnergySeams[i];
                }

                if (paintSeam)
                {
                    OnColorSeam(currentLowestEnergySeam.PixelLocations(ImageSize));
                }

                seamsForRemoval.Add(currentLowestEnergySeam);
            }

					Thread.Sleep(3000);

        	int x = 0;

						//foreach (Seam seam in seamsForRemoval)
						//{
						//  seam.StartIndex -= x++;
						//    CarveSeam(seam);
						//}

            OnImageChanged();

            //m_EnergyFunction.ComputeLocalEnergy(m_BitmapData, ImageSize, lowestEnergySeam);

            //m_SeamFunction.RecomputeEnergyMapRange(lowestEnergySeam, ImageSize);

            OnOperationComplete();
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
                    byte grayValue = m_EnergyFunction.EnergyMap[x, y];
                    SetPixel(energyMapBmd, x, y, grayValue, grayValue, grayValue);
                }
            }

            m_EnergyMapBitmap.UnlockBits(energyMapBmd);
        }

        private Bitmap ResizeBitmap(List<Seam> seam)
        {
            return null;
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
                	int i = 1;
                	lowestEnergySeam = m_SeamFunction.GetKthLowestEnergySeam(direction, ImageSize, ref i);
                }
                else
                {
                	int i = 1;
                	Seam isLowest1 = m_SeamFunction.GetKthLowestEnergySeam(Constants.Direction.VERTICAL, ImageSize, ref i);
                    Seam isLowest2 = m_SeamFunction.GetKthLowestEnergySeam(Constants.Direction.HORIZONTAL, ImageSize, ref i);

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
            int dstIndex, srcIndex, maxIndex, offset = 0, fromIndex = 0, toIndex = 0;

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

                offset = y;
                fromIndex = x;
                toIndex = m_CurrentWidth - 1;
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

                offset = x;
                fromIndex = y;
                toIndex = m_CurrentHeight;
            }

         //   Utilities.ShiftAddArray(m_EnergyFunction.EnergyMap, direction, offset, fromIndex, toIndex, byte.MaxValue);
        }
    }
}
