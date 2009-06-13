using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MagiCarver
{
    public class SeamStep
    {
        public KeyValuePair<Constants.SeamPixelDirection, int> Step;

        public SeamStep(KeyValuePair<Constants.SeamPixelDirection, int> step)
        {
            Step = step;
        }
    }
}
