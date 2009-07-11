using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MagiCarver.EnergyFunctions
{
    public class Roberts : EnergyFunction
    {
        protected override byte GetPixelEnergy(System.Drawing.Imaging.BitmapData bitmapData, int x, int y, System.Drawing.Size size)
        {
            byte[] pixels = new byte[4];

            pixels[0] = (byte)GetPixelValue(bitmapData, x, y, size); 
            pixels[1] = (byte)GetPixelValue(bitmapData, x + 1, y, size); 
            pixels[2] = (byte)GetPixelValue(bitmapData, x, y + 1, size); 
            pixels[3] = (byte)GetPixelValue(bitmapData, x + 1, y + 1, size); 

            int xRoberts = pixels[0] - pixels[3];
            int yRoberts = pixels[1] - pixels[2];

            int roberts = Math.Abs(xRoberts) + Math.Abs(yRoberts);

            if (roberts > 255)
            {
                roberts = 255;
            }

            return (byte)roberts;
        }
    }
}
