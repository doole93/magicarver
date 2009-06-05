using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Image=System.Windows.Controls.Image;

namespace MagiCarver
{
    public static class Utilities
    {
        #region Other Methods

        public static void ExportToPng(Uri path, Image image)
        {
            System.Windows.Size size = new System.Windows.Size(image.RenderSize.Width, image.RenderSize.Height);

            RenderTargetBitmap renderBitmap = 
                new RenderTargetBitmap((int)size.Width, (int)size.Height, 96d, 96d, PixelFormats.Pbgra32);
            
            renderBitmap.Render(image);

            using (FileStream outStream = new FileStream(path.LocalPath, FileMode.Create))
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();

                encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

                encoder.Save(outStream);
            }
        }

        public static byte GetPixel(BitmapData bitmapData, Size size, int x, int y)
        {
            if (InBounds(x, y, size))
            {
                unsafe
                {
                    byte* row = (byte*)bitmapData.Scan0 + (y * bitmapData.Stride) + (x * 3);
                    return (byte)((0.2126 * row[2]) + (0.7152 * row[1]) + (0.0722 * row[0]));
                }
            }
            
            return byte.MaxValue;
        }

        public static bool InBounds(int x, int y, Size size)
        {
            return (x >= 0) && (x < size.Width) && (y >= 0) && (y < size.Height);
        }

        #endregion

        // toIndex is exclusive.
        public static void ShiftArray<T>(T[,] array, Constants.Direction direction, int OppositeDirectionOffset, int fromIndex, int toIndex, object defaultValue)
        {
            int x = 0, y = 0, xMax = int.MaxValue, yMax = int.MaxValue, xInc = 0, yInc = 0;

            if (direction == Constants.Direction.VERTICAL)
            {
                x = fromIndex;
                y = OppositeDirectionOffset;
                xMax = toIndex - 1;
                xInc = 1;
            }
            else if (direction == Constants.Direction.HORIZONTAL)
            {
                x = OppositeDirectionOffset;
                y = fromIndex;
                yMax = toIndex - 1;
                yInc = 1;
            }

            while ((x < xMax) && (y < yMax))
            {
                array[x, y] = array[x + xInc, y + yInc];

                x += xInc;
                y += yInc;
            }

            array[x, y] = (T)defaultValue;
        }

        // toIndex is inclusive.
        //public static void ShiftAddArray<T>(T[,] array, Constants.Direction direction, int OppositeDirectionOffset, int fromIndex, int toIndex, object defaultValue)
        //{
        //    int x = 0, y = 0, xMin = int.MaxValue, yMin = int.MaxValue, xInc = 0, yInc = 0;

        //    if (direction == Constants.Direction.VERTICAL)
        //    {
        //        x = toIndex;
        //        y = OppositeDirectionOffset;
        //        xMin = fromIndex;
        //        xInc = -1;
        //    }
        //    else if (direction == Constants.Direction.HORIZONTAL)
        //    {
        //        x = OppositeDirectionOffset;
        //        y = fromIndex;
        //        yMin = toIndex - 1;
        //        yInc = -1;
        //    }

        //    while ((x > xMin) && (y > yMin))
        //    {
        //        array[x, y] = array[x + xInc, y + yInc];

        //        x += xInc;
        //        y += yInc;
        //    }

        //  //  array[x, y] = (T)defaultValue;
        //}
    }
}
