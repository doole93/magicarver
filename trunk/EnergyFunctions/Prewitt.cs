using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MagiCarver.EnergyFunctions
{
    public class Prewitt : EnergyFunction
    {
        protected override byte GetPixelEnergy(System.Drawing.Imaging.BitmapData bitmapData, int x, int y, System.Drawing.Size size)
        {
            byte[] pixels = new byte[8];

            pixels[0] = (byte)GetPixelValue(bitmapData, x - 1, y - 1, size);
            pixels[1] = (byte)GetPixelValue(bitmapData, x, y - 1, size);
            pixels[2] = (byte)GetPixelValue(bitmapData, x + 1, y - 1, size);
            pixels[3] = (byte)GetPixelValue(bitmapData, x - 1, y, size);
            pixels[4] = (byte)GetPixelValue(bitmapData, x + 1, y, size);
            pixels[5] = (byte)GetPixelValue(bitmapData, x - 1, y + 1, size);
            pixels[6] = (byte)GetPixelValue(bitmapData, x, y + 1, size);
            pixels[7] = (byte)GetPixelValue(bitmapData, x + 1, y + 1, size);

            int xPrewitt = pixels[2] + pixels[4] + pixels[7] - pixels[0] - pixels[3] - pixels[5];
            int yPrewitt = pixels[5] + pixels[6] + pixels[7] - pixels[0] - pixels[1] - pixels[2]; 

            int priwitt = Math.Abs(xPrewitt) + Math.Abs(yPrewitt);

            //int xSobel = pixels[0] + (pixels[1] + pixels[1]) + pixels[2] - pixels[6] - (pixels[7] + pixels[7]) - pixels[8];
            //int ySobel = pixels[2] + (pixels[5] + pixels[5]) + pixels[8] - pixels[0] - (pixels[3] + pixels[3]) - pixels[6];

            //int sobel = (int)Math.Sqrt((xSobel * xSobel) + (ySobel * ySobel));

            if (priwitt > 255)
            {
                priwitt = 255;
            }

            return (byte)priwitt;
        }
    }
}
