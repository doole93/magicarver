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
        #region Data Members

        private Bitmap _bitmap;
        private Bitmap _energyMapBitmap;

        #endregion

        #region Properties

        private BitmapData       BitData              { get; set; }

        private int[,]           HorizontalIndexMap   { get; set; }
        private int[,]           VerticalIndexMap     { get; set; }

        private List<Seam>       HorizontalSeams      { get; set; }
        private List<Seam>       VerticalSeams        { get; set; }

        private int              CurrentWidth         { get; set; }
        private int              CurrentHeight        { get; set; }

        private EnergyFunction   EnergyFunction       { get; set; }
        private CumulativeEnergy SeamFunction         { get; set; }

        private List<KeyValuePair<Point, Constants.EnergyType>> UserEnergy { get; set; }

        public Size ImageSize
        {
            get
            {
                return new Size(CurrentWidth, CurrentHeight);
            }
        }

        public Bitmap EnergyMapBitmap
        {
            get
            {
                GenerateEnergyMapBitmap();

                return _energyMapBitmap;
            }
        }

        public Bitmap Bitmap
        {
            get
            {
                return _bitmap;
            }
            set
            {
                _bitmap = value;

                CurrentWidth = _bitmap.Width;
                CurrentHeight = _bitmap.Height; 
            }
        }

        #endregion

        #region Event Handlers

        public event EventHandler ImageChanged;
        public event EventHandler OperationCompleted;
        public event EventHandler ColorSeam;

        #endregion

        #region CTors

        public SeamImage(Bitmap bitmap, EnergyFunction energyFunction)
        {
            EnergyFunction = energyFunction;

            Bitmap = bitmap;

            SeamFunction = new CumulativeEnergy {EnergyFunction = EnergyFunction};

            RecomputeEntireEnergy();

            HorizontalIndexMap = new int[CurrentWidth, CurrentHeight];
            VerticalIndexMap = new int[CurrentWidth, CurrentHeight];

            for (int i = 0; i < CurrentWidth; ++i)
            {
                for (int j = 0; j < CurrentHeight; ++j)
                {
                    HorizontalIndexMap[i, j] = VerticalIndexMap[i, j] = int.MaxValue;
                }
            }
        }

        #endregion

        #region Events

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

        #endregion

        #region Other Methods

        private void RecomputeEntireEnergy()
        {
            BitData = _bitmap.LockBits(new Rectangle(0, 0, _bitmap.Width, _bitmap.Height),
                                ImageLockMode.ReadOnly, _bitmap.PixelFormat);

            EnergyFunction.ComputeEnergy(BitData, ImageSize);

            _bitmap.UnlockBits(BitData);
        }

        public void RecomputeEntireMap()
        {
            Thread t1 = new Thread(() => SeamFunction.ComputeEntireEnergyMap(Constants.Direction.VERTICAL, ImageSize));
            Thread t2 = new Thread(() => SeamFunction.ComputeEntireEnergyMap(Constants.Direction.HORIZONTAL, ImageSize));

            t1.Start();
            t2.Start();

            t1.Join();
            t2.Join();
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

        private List<Seam> GetKBestSeams(Constants.Direction direction, int k)
        {
            if ((CurrentWidth < k) && (CurrentHeight < k))
            {
                return null;
            }

            List<Seam> seams = new List<Seam>();

            int[,] indexMap = (direction == Constants.Direction.VERTICAL ? VerticalIndexMap : HorizontalIndexMap);

            for (int i = 0; i < k; ++i)
            {
                Seam currentLowestEnergySeam;
                if (direction == Constants.Direction.VERTICAL || direction == Constants.Direction.HORIZONTAL)
                {
                    currentLowestEnergySeam = SeamFunction.GetKthLowestEnergySeam(direction, ImageSize, i, indexMap);								
                }
                else
                {
                    throw new InvalidOperationException();
                    //Seam isLowest1 = SeamFunction.GetKthLowestEnergySeam(Constants.Direction.VERTICAL, ImageSize, i, indexMap);
                    //Seam isLowest2 = SeamFunction.GetKthLowestEnergySeam(Constants.Direction.HORIZONTAL, ImageSize, i, indexMap);

                    //currentLowestEnergySeam = isLowest1.SeamValue < isLowest2.SeamValue ? isLowest1 : isLowest2;
                }

                seams.Add(currentLowestEnergySeam);
            }

            return seams;
        }

        public void Carve(Constants.Direction direction, bool paintSeam, int k)
        {
            //Seam currentLowestEnergySeam;

            //List<Seam> seamsForRemoval = new List<Seam>();

            //for (int i = 0; i < k; ++i)
            //{
            //    if (direction == Constants.Direction.VERTICAL)
            //    {
            //        currentLowestEnergySeam = VerticalSeams[i];
            //    }
            //    else if (direction == Constants.Direction.HORIZONTAL)
            //    {
            //        currentLowestEnergySeam = HorizontalSeams[i];
            //        HorizontalSeams.RemoveAt(i);
            //    }
            //    else
            //    {
            //        currentLowestEnergySeam = VerticalSeams[i].SeamValue < HorizontalSeams[i].SeamValue ? VerticalSeams[i] : HorizontalSeams[i];
            //    }

            //    if (paintSeam)
            //    {
            //        OnColorSeam(currentLowestEnergySeam.PixelLocations(ImageSize));
            //    }

            //    seamsForRemoval.Add(currentLowestEnergySeam);
            //}

            //CarveSeams(seamsForRemoval);

            CarveSeams(direction, k);

            //for (int i = 0; i < k; ++i)
            //{
            //    SeamFunction.RecomputeEnergyMapRange(removedSeams[i], ImageSize);
            //}

            OnImageChanged();

            //EnergyFunction.ComputeLocalEnergy(BitData, ImageSize, lowestEnergySeam);

            //SeamFunction.RecomputeEnergyMapRange(lowestEnergySeam, ImageSize);

            OnOperationComplete();
        }

        private void CarveSeams(Constants.Direction direction, int k)
        {
            int newWidth = direction == Constants.Direction.VERTICAL ? CurrentWidth - k : CurrentWidth;
            int newHeight = direction == Constants.Direction.HORIZONTAL ? CurrentHeight - k : CurrentHeight;

            Bitmap newBitmap = new Bitmap(newWidth, newHeight, _bitmap.PixelFormat);

            BitmapData newBmd = newBitmap.LockBits(new Rectangle(0, 0, newBitmap.Width, newBitmap.Height),
                                                   ImageLockMode.WriteOnly, newBitmap.PixelFormat);
            BitmapData oldBmd = _bitmap.LockBits(new Rectangle(0, 0, _bitmap.Width, _bitmap.Height),
                                                   ImageLockMode.ReadOnly, _bitmap.PixelFormat);

            int[,] indexMap = (direction == Constants.Direction.VERTICAL ? VerticalIndexMap : HorizontalIndexMap);

            Dictionary<int, List<Point>> removedPixels = new Dictionary<int, List<Point>>();

            if (direction == Constants.Direction.VERTICAL)
            {
                unsafe
                {
                    byte* dest = (byte*)newBmd.Scan0;
                    byte* src = (byte*)oldBmd.Scan0;

                    for (int i = 0; i < CurrentHeight; ++i)
                    {
                        for (int j = 0; j < CurrentWidth; ++j)
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

                        dest += newBmd.Stride - newBmd.Width * 3;
                        src += oldBmd.Stride - oldBmd.Width * 3;
                    }
                }
            }else
            {
                unsafe
                {
                    for (int i = 0; i < CurrentWidth; ++i)
                    {
                        byte* dest = (byte*)newBmd.Scan0 + i * 3;
                        byte* src = (byte*)oldBmd.Scan0 + i * 3;

                        for (int j = 0; j < CurrentHeight; ++j)
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

            foreach (List<Point> pointList in removedPixels.Values)
            {
                OnColorSeam(pointList);  
            }
            
            newBitmap.UnlockBits(newBmd);
            _bitmap.UnlockBits(oldBmd);

            _bitmap = newBitmap;
            BitData = newBmd;

            CurrentWidth = newWidth;
            CurrentHeight = newHeight;
        }

        private void GenerateEnergyMapBitmap()
        {
            _energyMapBitmap = new Bitmap(_bitmap.Width, _bitmap.Height, PixelFormat.Format24bppRgb);

            BitmapData energyMapBmd = _energyMapBitmap.LockBits(new Rectangle(0, 0, _energyMapBitmap.Width, _energyMapBitmap.Height),
                ImageLockMode.ReadOnly, _energyMapBitmap.PixelFormat);

            for (int y = 0; y < energyMapBmd.Height; y++)
            {
                for (int x = 0; x < energyMapBmd.Width; x++)
                {
                    byte grayValue = (byte)(EnergyFunction.EnergyMap[x, y] < 0 ? 0 : EnergyFunction.EnergyMap[x, y] > 254 ? 254 : EnergyFunction.EnergyMap[x, y]);
                    SetPixel(energyMapBmd, x, y, grayValue, grayValue, grayValue);
                }
            }

            _energyMapBitmap.UnlockBits(energyMapBmd);
        }

        public void SetEnergy(StrokeCollection strokes)
        {
            UserEnergy = new List<KeyValuePair<Point, Constants.EnergyType>>();

            foreach (Stroke stroke in strokes)
            {
                bool isHigh = stroke.DrawingAttributes.Color == Colors.Yellow ? true : false;

                foreach (StylusPoint point in stroke.StylusPoints)
                {
                    for (int i = 0; i < stroke.DrawingAttributes.Width; ++i)
                    {
                        for (int j = 0; j < stroke.DrawingAttributes.Height; ++j)
                        {
                            UserEnergy.Add(new KeyValuePair<Point, Constants.EnergyType>(new Point((int) (point.X - stroke.DrawingAttributes.Width / 2 + i), (int) (point.Y - stroke.DrawingAttributes.Height / 2 + j)), isHigh ? Constants.EnergyType.MAX : Constants.EnergyType.MIN));
                        }
                    }  
                }
            }

            RefineEnergy();
        }

        private void RefineEnergy()
        {
            foreach (KeyValuePair<Point, Constants.EnergyType> userEnergy in UserEnergy)
            {
                if (!Utilities.InBounds(userEnergy.Key.X, userEnergy.Key.Y, ImageSize))
                {
                    continue;
                }

                EnergyFunction.EnergyMap[userEnergy.Key.X, userEnergy.Key.Y] = 
                    (userEnergy.Value == Constants.EnergyType.MAX ? int.MaxValue : int.MinValue);
            }
        }

        public void CalculateIndexMaps()
        {
            BitData = _bitmap.LockBits(new Rectangle(0, 0, _bitmap.Width, _bitmap.Height),
                                                    ImageLockMode.ReadOnly, _bitmap.PixelFormat);

            Thread tHorizontal = new Thread(delegate()
                                                {
                                                    HorizontalSeams = GetKBestSeams(Constants.Direction.HORIZONTAL, 1);
                                                });

            Thread tVertical = new Thread(delegate()
                                              {
                                                  VerticalSeams = GetKBestSeams(Constants.Direction.VERTICAL, 1);
                                              });

            tHorizontal.Start();
            tVertical.Start();

            tHorizontal.Join();
            tVertical.Join();

            _bitmap.UnlockBits(BitData);
        }

        //private void CarveSeam(Seam seam)
        //{
        //    foreach (Point p in seam.PixelLocations(ImageSize))
        //    {
        //        ShiftPixels(BitData, seam.Direction, p.X, p.Y);
        //    }

        //    if (seam.Direction == Constants.Direction.VERTICAL)
        //    {
        //        CurrentWidth--;
        //    }
        //    else
        //    {
        //        CurrentHeight--;
        //    }
        //}

        //    public void AddSeam(Constants.Direction direction, Size minimumSize, bool paintSeam)
        //    {
        //        Seam lowestEnergySeam;

        //        while ((CurrentWidth < minimumSize.Width) && (CurrentHeight < minimumSize.Height))
        //        {

        //            if (direction == Constants.Direction.VERTICAL || direction == Constants.Direction.HORIZONTAL)
        //            {
        //                lowestEnergySeam = SeamFunction.GetKthLowestEnergySeam(direction, ImageSize, 1, null);
        //            }
        //            else
        //            {
        //                Seam isLowest1 = SeamFunction.GetKthLowestEnergySeam(Constants.Direction.VERTICAL, ImageSize, 1, null);
        //                Seam isLowest2 = SeamFunction.GetKthLowestEnergySeam(Constants.Direction.HORIZONTAL, ImageSize, 1, null);

        //                lowestEnergySeam = isLowest1.SeamValue < isLowest2.SeamValue ? isLowest1 : isLowest2;
        //            }

        //            if (lowestEnergySeam.Direction == Constants.Direction.VERTICAL)
        //            {
        //                CurrentWidth++;
        //            }else
        //            {
        //                CurrentHeight++;
        //            }

        //            Bitmap newBitmap = new Bitmap(CurrentWidth, CurrentHeight, PixelFormat.Format24bppRgb);

        //            BitData = _bitmap.LockBits(new Rectangle(0, 0, _bitmap.Width, _bitmap.Height),
        //            ImageLockMode.ReadWrite, _bitmap.PixelFormat);

        //            BitmapData newBitmapData = newBitmap.LockBits(new Rectangle(0, 0, newBitmap.Width, newBitmap.Height),
        //            ImageLockMode.ReadWrite, newBitmap.PixelFormat);

        //            CopyBitmap(BitData, newBitmapData);

        //            _bitmap.UnlockBits(BitData);

        //            _bitmap = newBitmap;
        //            BitData = newBitmapData;

        //            AddSeamToBitmap(lowestEnergySeam);

        //            _bitmap.UnlockBits(BitData);

        //            OnImageChanged();

        //            if (paintSeam)
        //            {
        //                OnColorSeam(lowestEnergySeam.PixelLocations(ImageSize));
        //            }

        //            EnergyFunction.ComputeEnergy(BitData, ImageSize);
        //            SeamFunction.ComputeEntireEnergyMap(Constants.Direction.VERTICAL, ImageSize);
        //            SeamFunction.ComputeEntireEnergyMap(Constants.Direction.HORIZONTAL, ImageSize);

        //            OnOperationComplete();

        ////            EnergyFunction.ComputeLocalEnergy(BitData, ImageSize, lowestEnergySeam);

        //         //   SeamFunction.RecomputeEnergyMapRange(lowestEnergySeam, ImageSize);
        //        }
        //    }

        //private static void CopyBitmap(BitmapData src, BitmapData dest)
        //{
        //    unsafe
        //    {
        //        byte* srcBits = (byte*)src.Scan0;
        //        byte* destBits = (byte*)dest.Scan0;

        //        int yLimit = Math.Min(src.Height, dest.Height);
        //        int xLimit = Math.Min(dest.Stride, src.Stride);

        //        for (int y = 0; y < yLimit; ++y)
        //        {
        //            for (int x = 0; x < xLimit; ++x)
        //            {
        //                destBits[x + y*dest.Stride] = srcBits[x + y*src.Stride];
        //            }
        //        }
        //    }
        //}

        //private void AddSeamToBitmap(Seam seam)
        //{
        //    foreach (Point p in seam.PixelLocations(ImageSize))
        //    {
        //        ShiftAddPixels(BitData, seam.Direction, p.X, p.Y);
        //    }
        //}

        //private void ShiftAddPixels(BitmapData bmd, Constants.Direction direction, int x, int y)
        //{
        //    int dstIndex;
        //    //int offset = 0, fromIndex = 0, toIndex = 0;

        //    if (direction == Constants.Direction.VERTICAL)
        //    {
        //        unsafe
        //        {
        //            byte* row = (byte*)bmd.Scan0 + (y * bmd.Stride) + (x * 3);

        //            //Inclusive

        //            for (dstIndex = (CurrentWidth - x - 1) * 3; dstIndex > 6; dstIndex -= 3)
        //            {
        //                row[dstIndex] = row[dstIndex - 3];
        //                row[dstIndex + 1] = row[dstIndex - 2];
        //                row[dstIndex + 2] = row[dstIndex - 1];
        //            }

        //            row[dstIndex] = (byte) ((row[dstIndex - 3] + row[dstIndex + 3]) / 2);
        //            row[dstIndex + 1] = (byte)((row[dstIndex - 2] + row[dstIndex + 4]) / 2);
        //            row[dstIndex + 2] = (byte)((row[dstIndex - 1] + row[dstIndex + 5]) / 2);
        //        }

        //        //offset = y;
        //        //fromIndex = x;
        //        //toIndex = CurrentWidth - 1;
        //    }
        //    else if (direction == Constants.Direction.HORIZONTAL)
        //    {

        //        unsafe
        //        {
        //            byte* row, row2;

        //            for (dstIndex = bmd.Height - 1; dstIndex > y + 1; --dstIndex)
        //            {
        //                row = (byte*)bmd.Scan0 + (dstIndex * bmd.Stride) + (x * 3);
        //                row2 = (byte*)bmd.Scan0 + ((dstIndex - 1) * bmd.Stride) + (x * 3);

        //                row[0] = row2[0];
        //                row[1] = row2[1];
        //                row[2] = row2[2];
        //            }

        //            row = (byte*)bmd.Scan0 + (dstIndex * bmd.Stride) + (x * 3);
        //            row2 = (byte*)bmd.Scan0 + ((dstIndex - 1) * bmd.Stride) + (x * 3);
        //            byte* row3 = (byte*)bmd.Scan0 + ((dstIndex + 1) * bmd.Stride) + (x * 3);

        //            row[0] = (byte)((row2[0] + row3[0]) / 2);
        //            row[1] = (byte)((row2[1] + row3[1]) / 2);
        //            row[2] = (byte)((row2[2] + row3[2]) / 2);
        //        }

        //        //offset = x;
        //        //fromIndex = y;
        //        //toIndex = CurrentHeight;
        //    }

        // //   Utilities.ShiftAddArray(EnergyFunction.EnergyMap, direction, offset, fromIndex, toIndex, byte.MaxValue);
        //}

        //private void CarveSeam(Seam seam)
        //{
        //    foreach (Point p in seam.PixelLocations(ImageSize))
        //    {
        //        ShiftPixels(BitData, seam.Direction, p.X, p.Y);
        //    }

        //    if (seam.Direction == Constants.Direction.VERTICAL)
        //    {
        //        CurrentWidth--;
        //    }
        //    else
        //    {
        //        CurrentHeight--;
        //    }
        //}

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
        //            maxIndex = (CurrentWidth - x - 1) * 3;

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
        //        //toIndex = CurrentWidth;
        //    }
        //    else if (direction == Constants.Direction.HORIZONTAL)
        //    {

        //        unsafe
        //        {
        //            dstIndex = y;
        //            srcIndex = dstIndex + 1;
        //            maxIndex = CurrentHeight - 1;
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
        //        //toIndex = CurrentHeight;
        //    }

        //    //Utilities.ShiftArray(EnergyFunction.EnergyMap, direction, offset, fromIndex, toIndex, byte.MaxValue);

        //}

        #endregion
    }
}
