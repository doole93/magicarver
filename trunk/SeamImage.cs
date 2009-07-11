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

        // The bitmaps
        private Bitmap _bitmap;
        private Bitmap _energyMapBitmap;

        #endregion

        #region Properties

        // The bitmap data
        private BitmapData       BitData              { get; set; }

        // The bitmap which is coherent with the caches
        private Bitmap           OldBitmap            { get; set; }
        private Size            OldSize                 { get; set; }

        // Index maps
        private int[,]           HorizontalIndexMap   { get; set; }
        private int[,]           VerticalIndexMap     { get; set; }

        // Calculated seams
        private List<Seam>       HorizontalSeams      { get; set; }
        private List<Seam>       VerticalSeams        { get; set; }

        // Current dimensions
        private int              CurrentWidth         { get; set; }
        private int              CurrentHeight        { get; set; }

        // Functions
        private EnergyFunction   EnergyFunction       { get; set; }
        private CumulativeEnergy SeamFunction         { get; set; }

        // Number of removed / added seams
        private int RemovedSeams { get; set; }
        private int AddedSeams { get; set; }

        public int CacheLimit { get; private set; }
        
        // Indicates whether operation has been taken after the caches were refreshed
        private bool Dirty {
            get
            {
                return RemovedSeams + AddedSeams > 0;
            } 
        }

        private readonly Object lockObject = new Object();
        
        // History of operations for cache refresh
        private Constants.ActionType LastAction { get; set; }
        private Constants.Direction LastDirection { get; set; }

        // User input
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

                OldSize = ImageSize;
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

            LastDirection = Constants.Direction.NONE;
            LastAction = Constants.ActionType.NONE;

            RecomputeEntireEnergy();

            CacheLimit = Math.Min(Constants.DEFAULT_CACHELIMIT, Math.Min(CurrentHeight, CurrentWidth));
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

        protected virtual void OnColorSeam(System.Windows.Point[] points)
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
                }

                seams.Add(currentLowestEnergySeam);
            }

            return seams;
        }

        /// <summary>
        /// Carves a seam
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="paintSeam"></param>
        /// <param name="k"></param>
        public void Carve(Constants.Direction direction, bool paintSeam, int k)
        {
            int factor = k > CacheLimit ? k : CacheLimit;

            // Refresh the cache if last operation was enlargement
            if (LastAction == Constants.ActionType.ENLARGE)
            {
                InvalidateAndRefresh(direction, factor);
            }

            // If we changed direction, refresh the cache
            if ((direction == Constants.Direction.VERTICAL && LastDirection == Constants.Direction.HORIZONTAL) || (direction == Constants.Direction.HORIZONTAL && LastDirection == Constants.Direction.VERTICAL))
            {
                InvalidateAndRefresh(direction, factor);
            }

            // If we ran out of image or cache, refresh it
            if (direction == Constants.Direction.VERTICAL)
            {
                if (k >= ImageSize.Width)
                {
                    k = ImageSize.Width - 1;
                }
                if (VerticalSeams.Count < k)
                {
                    InvalidateAndRefresh(direction, factor);
                }
            }
            else
            {
                if (k >= ImageSize.Height)
                {
                    k = ImageSize.Height - 1;
                }
                if (HorizontalSeams.Count < k)
                {
                    InvalidateAndRefresh(direction, factor);
                }
            }

            List<Seam> seams = direction == Constants.Direction.VERTICAL ? VerticalSeams : HorizontalSeams;

            if (paintSeam)
            {
                for (int i = 0; i < factor; ++i)
                {
                    OnColorSeam(seams[i].PixelLocations);
                }
            }

            CarveSeams(direction, k - 1);

            LastAction = Constants.ActionType.SHIRNK;
            LastDirection = direction;

            RemovedSeams += k;

            if (direction == Constants.Direction.VERTICAL)
            {
                VerticalSeams.RemoveRange(0, k);
            }
            else
            {
                HorizontalSeams.RemoveRange(0, k);
            }

            OnImageChanged();

           // OnOperationComplete();
        }

        /// <summary>
        /// Recomputes the energy. Needed to allow user to add input as he wishes
        /// </summary>
        public void RecomputeBase()
        {
            if (Dirty)
            {
                RecomputeEntireEnergy();

                LastAction = Constants.ActionType.NONE;
                LastDirection = Constants.Direction.NONE;
            }
        }

        /// <summary>
        /// Adds seams. Exactly as Carve(). Need to merge the two
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="paintSeam"></param>
        /// <param name="k"></param>
        public void Add(Constants.Direction direction, bool paintSeam, int k)
        {
            lock (lockObject)
            {
                int factor = k > CacheLimit ? k : CacheLimit;

                if (direction == Constants.Direction.VERTICAL && factor > ImageSize.Width)
                {
                    factor = ImageSize.Width;
                }
                else if (direction == Constants.Direction.HORIZONTAL && factor > ImageSize.Height){
                    factor = ImageSize.Height;
                }

                if (LastAction == Constants.ActionType.SHIRNK)
                {
                    InvalidateAndRefresh(direction, factor);
                }

                if ((direction == Constants.Direction.VERTICAL && LastDirection == Constants.Direction.HORIZONTAL) || (direction == Constants.Direction.HORIZONTAL && LastDirection == Constants.Direction.VERTICAL))
                {
                    InvalidateAndRefresh(direction, factor);
                }

                if (direction == Constants.Direction.VERTICAL)
                {
                    if (VerticalSeams.Count < k)
                    {
                        InvalidateAndRefresh(direction, factor);
                    }
                }
                else
                {
                    if (HorizontalSeams.Count < k)
                    {
                        InvalidateAndRefresh(direction, factor);
                    }
                }

                List<Seam> seams = direction == Constants.Direction.VERTICAL ? VerticalSeams : HorizontalSeams;

                if (paintSeam)
                {
                    for (int i = 0; i < factor; ++i)
                    {
                        OnColorSeam(seams[i].PixelLocations);
                    }
                }

                AddSeams(direction, k - 1);

                LastAction = Constants.ActionType.ENLARGE;
                LastDirection = direction;

                AddedSeams += k;

                if (direction == Constants.Direction.VERTICAL)
                {
                    VerticalSeams.RemoveRange(0, k);
                }
                else
                {
                    HorizontalSeams.RemoveRange(0, k);
                }

                OnImageChanged();

                //  OnOperationComplete();
            }
        }

        /// <summary>
        /// Actually carves the seams
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="k"></param>
        private void CarveSeams(Constants.Direction direction, int k)
        {
            // Finding the new dimensions
            int newWidth = direction == Constants.Direction.VERTICAL ? CurrentWidth - k - 1 : CurrentWidth;
            int newHeight = direction == Constants.Direction.HORIZONTAL ? CurrentHeight - k - 1 : CurrentHeight;

            // Creating the new bitmap
            Bitmap newBitmap = new Bitmap(newWidth, newHeight, _bitmap.PixelFormat);

            BitmapData newBmd = newBitmap.LockBits(new Rectangle(0, 0, newBitmap.Width, newBitmap.Height),
                                                   ImageLockMode.WriteOnly, newBitmap.PixelFormat);

            BitmapData oldBmd;

            // Checking for old bitmap to work with
            if (OldBitmap == null)
            {
                oldBmd = _bitmap.LockBits(new Rectangle(0, 0, _bitmap.Width, _bitmap.Height),
                                                   ImageLockMode.ReadOnly, _bitmap.PixelFormat);
            }
            else
            {
                oldBmd = OldBitmap.LockBits(new Rectangle(0, 0, OldBitmap.Width, OldBitmap.Height),
                                                   ImageLockMode.ReadOnly, OldBitmap.PixelFormat);
            }

            int[,] indexMap = (direction == Constants.Direction.VERTICAL ? VerticalIndexMap : HorizontalIndexMap);

            //     Dictionary<int, List<Point>> removedPixels = new Dictionary<int, List<Point>>();

            if (direction == Constants.Direction.VERTICAL)
            {
                unsafe
                {
                    Parallel.For(0, OldSize.Height, i =>
                    {
                        // Setting starting lines
                        byte* dest = (byte*)newBmd.Scan0 + i * newBmd.Stride;
                        byte* src = (byte*)oldBmd.Scan0 + i * oldBmd.Stride;

                        for (int j = 0; j < OldSize.Width; ++j)
                        {
                            // If this pixel was removed sometime after the last cache refresh
                            // we skip it and mark it for energy correction
                            if (indexMap[j, i] <= k + RemovedSeams)
                            {
                                EnergyFunction.EnergyMap[j, i] = -1;
                                src += 3;
                                continue;
                            }

                            // Copy the pixel
                            dest[0] = src[0];
                            dest[1] = src[1];
                            dest[2] = src[2];

                            src += 3;
                            dest += 3;
                        }
                    });
                }
            }
            else
            {
                unsafe
                {
                    // Same for horizontal
                    for (int i = 0; i < OldSize.Width; ++i)
                    {
                        byte* dest = (byte*)newBmd.Scan0 + i * 3;
                        byte* src = (byte*)oldBmd.Scan0 + i * 3;

                        for (int j = 0; j < OldSize.Height; ++j)
                        {
                            if (indexMap[i, j] <= k + RemovedSeams)
                            {
                                EnergyFunction.EnergyMap[i, j] = -1;
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

            newBitmap.UnlockBits(newBmd);

            // Save the old bitmap if it does not exist
            if (OldBitmap == null)
            {
                _bitmap.UnlockBits(oldBmd);

                OldBitmap = _bitmap;
            }
            else
            {
                OldBitmap.UnlockBits(oldBmd);
            }


            // Set the new bitmap
            _bitmap = newBitmap;
            BitData = newBmd;

            CurrentWidth = newWidth;
            CurrentHeight = newHeight;
        }

        /// <summary>
        /// Actual addition of seams. Adding the seams with lowest energy
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="k"></param>
        private void AddSeams(Constants.Direction direction, int k)
        {
            int newWidth = direction == Constants.Direction.VERTICAL ? CurrentWidth + k + 1 : CurrentWidth;
            int newHeight = direction == Constants.Direction.HORIZONTAL ? CurrentHeight + k + 1 : CurrentHeight;

            Bitmap newBitmap = new Bitmap(newWidth, newHeight, _bitmap.PixelFormat);

            BitmapData newBmd = newBitmap.LockBits(new Rectangle(0, 0, newBitmap.Width, newBitmap.Height),
                                                   ImageLockMode.WriteOnly, newBitmap.PixelFormat);

            BitmapData oldBmd;

            if (OldBitmap == null)
            {
               oldBmd = _bitmap.LockBits(new Rectangle(0, 0, _bitmap.Width, _bitmap.Height),
                                                   ImageLockMode.ReadOnly, _bitmap.PixelFormat);
            }
            else
            {
                oldBmd = OldBitmap.LockBits(new Rectangle(0, 0, OldBitmap.Width, OldBitmap.Height),
                                                   ImageLockMode.ReadOnly, OldBitmap.PixelFormat);
            }

            int[,] indexMap = (direction == Constants.Direction.VERTICAL ? VerticalIndexMap : HorizontalIndexMap);

            if (direction == Constants.Direction.VERTICAL)
            {
                unsafe
                {
                    Parallel.For(0, OldSize.Height, i =>
                    {
                        byte* dest = (byte*)newBmd.Scan0 + i * newBmd.Stride;
                        byte* src = (byte*)oldBmd.Scan0 + i * oldBmd.Stride;

                        for (int j = 0; j < OldSize.Width; ++j)
                        {
                            // If this pixel is to be added
                            if (indexMap[j, i] <= k + AddedSeams)
                            {

                                // Mark it for cache refresh
                                EnergyFunction.EnergyMap[j, i] = -1;

                                // We firstly copy the pixel
                                dest[0] = src[0];
                                dest[1] = src[1];
                                dest[2] = src[2];

                                dest += 3;

                                // We then add the average of the pixel and its neighbours
                                if (j > 0 && j < OldSize.Width - 1)
                                {
                                    dest[0] = (byte)((src[0] + src[3] + src[-3]) / 3);
                                    dest[1] = (byte)((src[1] + src[4] + src[-2]) / 3);
                                    dest[2] = (byte)((src[2] + src[5] + src[-1]) / 3); 
                                }
                                else if (j < OldSize.Width - 1)
                                {
                                    dest[0] = (byte)((src[0] + src[3]) / 2);
                                    dest[1] = (byte)((src[1] + src[4]) / 2);
                                    dest[2] = (byte)((src[2] + src[5]) / 2);
                                }
                                else if (j > 0)
                                {
                                    dest[0] = (byte)((src[0] + src[-3]) / 2);
                                    dest[1] = (byte)((src[1] + src[-2]) / 2);
                                    dest[2] = (byte)((src[2] + src[-1]) / 2);
                                }
                                else
                                {
                                    dest[0] = src[0];
                                    dest[1] = src[1];
                                    dest[2] = src[2];
                                }

                                src += 3;
                                dest += 3;
                                continue;
                            }

                            dest[0] = src[0];
                            dest[1] = src[1];
                            dest[2] = src[2];

                            src += 3;
                            dest += 3;
                        }
                    });
                }
            }
            else
            {
                unsafe
                {
                    for (int i = 0; i < OldSize.Width; ++i)
                    {
                        byte* dest = (byte*)newBmd.Scan0 + i * 3;
                        byte* src = (byte*)oldBmd.Scan0 + i * 3;

                        for (int j = 0; j < OldSize.Height; ++j)
                        {
                            if (indexMap[i, j] <= k + AddedSeams)
                            {
                                EnergyFunction.EnergyMap[i, j] = -1;
                                
                                dest[0] = src[0];
                                dest[1] = src[1];
                                dest[2] = src[2];

                                dest += newBmd.Stride;

                                if (j > 0 && j < OldSize.Height - 1)
                                {
                                    dest[0] = (byte)((src[0] + src[oldBmd.Stride] + src[-oldBmd.Stride]) / 3);
                                    dest[1] = (byte)((src[1] + src[oldBmd.Stride + 1] + src[-oldBmd.Stride + 1]) / 3);
                                    dest[2] = (byte)((src[2] + src[oldBmd.Stride + 2] + src[-oldBmd.Stride + 2]) / 3);
                                }
                                else if (j < OldSize.Height - 1)
                                {
                                    dest[0] = (byte)((src[0] + src[oldBmd.Stride]) / 2);
                                    dest[1] = (byte)((src[1] + src[oldBmd.Stride + 1]) / 2);
                                    dest[2] = (byte)((src[2] + src[oldBmd.Stride + 2]) / 2);
                                }
                                else if (j > 0)
                                {
                                    dest[0] = (byte)((src[0] + src[-oldBmd.Stride]) / 2);
                                    dest[1] = (byte)((src[1] + src[-oldBmd.Stride + 1]) / 2);
                                    dest[2] = (byte)((src[2] + src[-oldBmd.Stride + 2]) / 2);
                                }
                                else
                                {
                                    dest[0] = src[0];
                                    dest[1] = src[1];
                                    dest[2] = src[2];
                                }

                                src += oldBmd.Stride;
                                dest += newBmd.Stride;
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

            newBitmap.UnlockBits(newBmd);

            if (OldBitmap == null)
            {
                _bitmap.UnlockBits(oldBmd);

                OldBitmap = _bitmap;
            }
            else
            {
                OldBitmap.UnlockBits(oldBmd);
            }


            _bitmap = newBitmap;
            BitData = newBmd;

            CurrentWidth = newWidth;
            CurrentHeight = newHeight;
        }

        /// <summary>
        /// Resposible for cache rebuild / refresh
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="validationFactor"></param>
        private void InvalidateAndRefresh(Constants.Direction direction, int validationFactor)
        {

            BitData = _bitmap.LockBits(new Rectangle(0, 0, _bitmap.Width, _bitmap.Height),
                                                   ImageLockMode.ReadOnly, _bitmap.PixelFormat);


            DateTime a = DateTime.Now;

            // Refreshes the pixel's energy
            EnergyFunction.ComputeLocalEnergy(BitData, OldSize, ImageSize, LastDirection);

            TimeSpan b = DateTime.Now - a;

            Console.WriteLine("Refreshing the energy took " + b.Milliseconds);

            _bitmap.UnlockBits(BitData);

            a = DateTime.Now;

            // Recomputes the cumulativer energy map
            if (direction == Constants.Direction.BOTH || direction == Constants.Direction.VERTICAL)
            {
                SeamFunction.ComputeEntireEnergyMap(Constants.Direction.VERTICAL, ImageSize);
            }

            if (direction == Constants.Direction.BOTH || direction == Constants.Direction.HORIZONTAL)
            {
                SeamFunction.ComputeEntireEnergyMap(Constants.Direction.HORIZONTAL, ImageSize);
            }

            b = DateTime.Now - a;

            Console.WriteLine("Recalculating the cumulative energy took " + b.Milliseconds);

            a = DateTime.Now;

            // Recalculates the index maps and rebuilds the seams
            CalculateIndexMaps(direction, validationFactor);

            b = DateTime.Now - a;

            Console.WriteLine("Recalculating the index maps and rebuild of seams took " + b.Milliseconds);

            OldSize = ImageSize;

            OldBitmap = null;

            RemovedSeams = AddedSeams = 0;

            LastAction = Constants.ActionType.NONE;
            LastDirection = Constants.Direction.NONE;
        }

        /// <summary>
        /// Generates the energy map bitmap
        /// </summary>
        private void GenerateEnergyMapBitmap()
        {
            if (Dirty)
            {
                InvalidateAndRefresh(Constants.Direction.BOTH, 0);
            }

            _energyMapBitmap = new Bitmap(_bitmap.Width, _bitmap.Height, PixelFormat.Format24bppRgb);

            BitmapData energyMapBmd = _energyMapBitmap.LockBits(new Rectangle(0, 0, _energyMapBitmap.Width, _energyMapBitmap.Height),
                ImageLockMode.ReadOnly, _energyMapBitmap.PixelFormat);

            Parallel.For(0, energyMapBmd.Height, delegate(int y)
            {
                for (int x = 0; x < energyMapBmd.Width; x++)
                {
                    byte grayValue = (byte)(EnergyFunction.EnergyMap[x, y] < 0 ? 0 : EnergyFunction.EnergyMap[x, y] > 254 ? 254 : EnergyFunction.EnergyMap[x, y]);
                    SetPixel(energyMapBmd, x, y, grayValue, grayValue, grayValue);
                }
            });

            _energyMapBitmap.UnlockBits(energyMapBmd);
        }

        /// <summary>
        /// Sets the user input energy
        /// </summary>
        /// <param name="strokes"></param>
        public void SetEnergy(StrokeCollection strokes)
        {
            UserEnergy = new List<KeyValuePair<Point, Constants.EnergyType>>();

            Constants.EnergyType[,] userEnergy = new Constants.EnergyType[CurrentWidth, CurrentHeight];
            
            Parallel.ForEach(strokes, delegate(Stroke stroke)
            {
                bool isHigh = stroke.DrawingAttributes.Color == Colors.Yellow ? true : false;

                foreach (StylusPoint point in stroke.StylusPoints)
                {
                                     for (int i = 0; i < stroke.DrawingAttributes.Width; ++i)
                    {
                        for (int j = 0; j < stroke.DrawingAttributes.Height; ++j)
                        {
                            int x = (int)(point.X - stroke.DrawingAttributes.Width / 2 + i);
                            int y = (int)(point.Y - stroke.DrawingAttributes.Height / 2 + j);

                            if (Utilities.InBounds(x, y, ImageSize))
                            {
                                userEnergy[x, y] = isHigh ? Constants.EnergyType.MAX : Constants.EnergyType.MIN;
                            }
                        }
                    }   
                }

            });

            RefineEnergy(userEnergy);
        }
        /// <summary>
        /// Helper method to set user input
        /// </summary>
        private void RefineEnergy(Constants.EnergyType[,] userEnergy)
        {
            Parallel.For(0, CurrentWidth, delegate(int i)
            {
                for (int j = 0; j < CurrentHeight; ++j)
                {
                    if (userEnergy[i, j] != 0)
                    {
                        // ZTODO: Fix this. It should be something defined and not '10000'...
                        EnergyFunction.EnergyMap[i, j] =
                            (userEnergy[i, j] == Constants.EnergyType.MAX ? Constants.MAX_ENERGY : Constants.MIN_ENERGY);
                    }
                }
            });
        }

        /// <summary>
        /// Calculates the index maps
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="validationFactor"></param>
        public void CalculateIndexMaps(Constants.Direction direction, int validationFactor)
        {
            int factor = validationFactor <= 0 ? CacheLimit : validationFactor;

            BitData = _bitmap.LockBits(new Rectangle(0, 0, _bitmap.Width, _bitmap.Height),
                                                    ImageLockMode.ReadOnly, _bitmap.PixelFormat);            

            HorizontalIndexMap = new int[CurrentWidth, CurrentHeight];
            VerticalIndexMap = new int[CurrentWidth, CurrentHeight];

            if (direction == Constants.Direction.BOTH)
            {
                Parallel.For(0, CurrentWidth, i =>
                {
                    for (int j = 0; j < CurrentHeight; ++j)
                    {
                        HorizontalIndexMap[i, j] = VerticalIndexMap[i, j] = int.MaxValue;
                    }
                });
            }else if (direction == Constants.Direction.VERTICAL)
            {
                Parallel.For(0, CurrentWidth, i =>
                {
                    for (int j = 0; j < CurrentHeight; ++j)
                    {
                        VerticalIndexMap[i, j] = int.MaxValue;
                    }
                });
            }else
            {
                Parallel.For(0, CurrentWidth, i =>
                {
                    for (int j = 0; j < CurrentHeight; ++j)
                    {
                        HorizontalIndexMap[i, j] = int.MaxValue;
                    }
                });
            }

            Thread tHorizontal = new Thread(delegate()
                                                {
                                                    HorizontalSeams = GetKBestSeams(Constants.Direction.HORIZONTAL, Math.Min(factor, ImageSize.Height));
                                                });

            Thread tVertical = new Thread(delegate()
                                              {
                                                  VerticalSeams = GetKBestSeams(Constants.Direction.VERTICAL, Math.Min(factor, ImageSize.Width));
                                              });

            if (direction != Constants.Direction.VERTICAL)
            {
                tHorizontal.Start();
            }
            
            if (direction != Constants.Direction.HORIZONTAL)
            {
                tVertical.Start(); 
            }

            if (direction != Constants.Direction.VERTICAL)
            {
                tHorizontal.Join();
            }

            if (direction != Constants.Direction.HORIZONTAL)
            {
                tVertical.Join();
            }

            _bitmap.UnlockBits(BitData);
        }

        //        private void CarveSeamsNew(Constants.Direction direction, List<Seam> seams, int k)
        //        {
        //            int newWidth = direction == Constants.Direction.VERTICAL ? CurrentWidth - k : CurrentWidth;
        //            int newHeight = direction == Constants.Direction.HORIZONTAL ? CurrentHeight - k : CurrentHeight;

        //            BitmapData oldBmd;

        //            //if (OldBitmap == null)
        //            //{
        //                oldBmd = _bitmap.LockBits(new Rectangle(0, 0, _bitmap.Width, _bitmap.Height),
        //                                                   ImageLockMode.ReadOnly, _bitmap.PixelFormat);
        //            //}
        //            //else
        //            //{
        //            //    oldBmd = OldBitmap.LockBits(new Rectangle(0, 0, OldBitmap.Width, OldBitmap.Height),
        //            //                                       ImageLockMode.ReadOnly, OldBitmap.PixelFormat);
        //            //}

        //            Bitmap newBitmap = new Bitmap(newWidth, newHeight, _bitmap.PixelFormat);

        //            BitmapData newBmd = newBitmap.LockBits(new Rectangle(0, 0, newBitmap.Width, newBitmap.Height),
        //                                                   ImageLockMode.WriteOnly, newBitmap.PixelFormat);

        //            byte[] oldBitmapBytes = new byte[oldBmd.Stride * oldBmd.Height];
        //            byte[] newBitmapBytes = new byte[newBmd.Stride * newBmd.Height];

        ////            DateTime a = DateTime.Now;

        //            //Buffer.BlockCopy(oldBmd.Scan0, 0, tst, 0, 0);
        //            Marshal.Copy(oldBmd.Scan0, oldBitmapBytes, 0, oldBmd.Stride * oldBmd.Height);

        //      //      TimeSpan b = DateTime.Now - a;

        // //           Console.WriteLine(b.Milliseconds);

        //            List<int> pixelsLocations = new List<int>();

        //            unsafe
        //            {

        //                byte* a = (byte*) newBmd.Scan0;

        //                for (int j = 0; j < ImageSize.Height; ++j)
        //                {
        //                    for (int i = 0; i < k; ++i)
        //                    {
        //                        pixelsLocations.Add(seams[i].PixelLocations[j]);
        //                    }

        //                    pixelsLocations.Sort(new IntSort());

        //                    int offset = j * oldBmd.Stride;
        //                    int cumLength = j * newBmd.Stride;

        //                    for (int m = 0; m < pixelsLocations.Count; ++m)
        //                    {
        //                        int length = m > 0
        //                                         ? (pixelsLocations[m] - pixelsLocations[m - 1] - 1) * 3
        //                                         : pixelsLocations[m] *3 ;
        //                        //Marshal.Copy(oldBitmap, newBmd.Scan0, offset, length);
        //                        //Marshal.Copy(oldBitmapBytes, offset, newBitmapBytes, length);
        //                        Buffer.BlockCopy(oldBitmapBytes, offset, newBitmapBytes, cumLength, length);


        //                        cumLength += length;

        //                        offset += length + 3;
        //                    }

        //                    Buffer.BlockCopy(oldBitmapBytes, offset, newBitmapBytes, cumLength, (oldBmd.Width - pixelsLocations[pixelsLocations.Count - 1] - 1) * 3);


        //                    pixelsLocations.Clear();
        //                }

        //            }

        //            Marshal.Copy(newBitmapBytes, 0, newBmd.Scan0, newBmd.Stride * newBmd.Height);

        //            newBitmap.UnlockBits(newBmd);

        //            //if (OldBitmap == null)
        //            //{
        //                _bitmap.UnlockBits(oldBmd);

        //            //    OldBitmap = _bitmap;
        //            //}
        //            //else
        //            //{
        //            //    OldBitmap.UnlockBits(oldBmd);
        //            //}


        //            _bitmap = newBitmap;
        //            BitData = newBmd;

        //            CurrentWidth = newWidth;
        //            CurrentHeight = newHeight;
        //        }

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

        public void SetCacheLimit(int newLimit)
        {
            CacheLimit = newLimit;
        }
    }
}
