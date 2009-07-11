using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;

namespace MagiCarver.EnergyFunctions
{
    public class Sobel : EnergyFunction
    {
        #region Implementation of EnergyFunction

        /// <summary>
        /// Gets the Sobel energy of a specific pixel. Explanation here: http://en.wikipedia.org/wiki/Sobel_operator and in the project book.
        /// </summary>
        /// <param name="bitmapData"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        protected override byte GetPixelEnergy(BitmapData bitmapData, int x, int y, Size size)
        {
            byte[] pixels = new byte[9];

            pixels[0] = (byte)GetPixelValue(bitmapData, x - 1, y - 1, size);
            pixels[1] = (byte)GetPixelValue(bitmapData, x, y - 1, size);
            pixels[2] = (byte)GetPixelValue(bitmapData, x + 1, y - 1, size);
            pixels[3] = (byte)GetPixelValue(bitmapData, x - 1, y, size);
            pixels[4] = (byte)GetPixelValue(bitmapData, x, y, size);
            pixels[5] = (byte)GetPixelValue(bitmapData, x + 1, y, size);
            pixels[6] = (byte)GetPixelValue(bitmapData, x - 1, y + 1, size);
            pixels[7] = (byte)GetPixelValue(bitmapData, x, y + 1, size);
            pixels[8] = (byte)GetPixelValue(bitmapData, x + 1, y + 1, size);

            int xSobel = pixels[8] + 2 * pixels[5] + pixels[2] - pixels[0] - 2 * pixels[3] - pixels[6];
            int ySobel = pixels[8] + 2 * pixels[7] + pixels[6] - pixels[2] - 2 * pixels[1] - pixels[0];

            int sobel = Math.Abs(xSobel) + Math.Abs(ySobel);

            //int xSobel = pixels[0] + (pixels[1] + pixels[1]) + pixels[2] - pixels[6] - (pixels[7] + pixels[7]) - pixels[8];
            //int ySobel = pixels[2] + (pixels[5] + pixels[5]) + pixels[8] - pixels[0] - (pixels[3] + pixels[3]) - pixels[6];

            //int sobel = (int)Math.Sqrt((xSobel * xSobel) + (ySobel * ySobel));

            if (sobel > 255)
            {
                sobel = 255;
            }

            return (byte)sobel;
        }

        #endregion
    }
}
