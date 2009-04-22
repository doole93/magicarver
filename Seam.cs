using System.Collections.Generic;
using System.Drawing;

namespace MagiCarver
{
    public class Seam
    {
        public Constants.SeamPixelDirection[] PixelDirections { get; set; }

        public Constants.Direction Direction { get; set; }

        public int StartIndex { get; set; }

        public double SeamValue { get; set; }

        public IEnumerable<Point> PixelLocations(Size size)
        {
            int x = 0, y = 0, xInc = 0, yInc = 0, currentIndex = 0;

            if (Direction == Constants.Direction.VERTICAL)
            {
                x = StartIndex;
                yInc = 1;
            }
            else
            {
                y = StartIndex;
                xInc = 1;
            }

            while ((x < size.Width) && (y < size.Height))
            {
                Constants.SeamPixelDirection seamPixelDirection = PixelDirections[currentIndex];
                currentIndex++;

                if (seamPixelDirection == Constants.SeamPixelDirection.LEFT)
                {
                    if (Direction == Constants.Direction.VERTICAL)
                    {
                        x++;
                    }
                    else
                    {
                        y--;
                    }
                }
                else if (seamPixelDirection == Constants.SeamPixelDirection.RIGHT)
                {
                    if (Direction == Constants.Direction.VERTICAL)
                    {
                        x--;
                    }
                    else
                    {
                        y++;
                    }
                }

                yield return new Point(x, y);

                x += xInc;
                y += yInc;
            }
        }
    }
}
