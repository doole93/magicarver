using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Drawing;
using System.Windows.Media;
using System.Windows;

namespace MagiCarver.Converters
{
    class BooleanCanOperateToColorConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool canOperate = (bool)value;
            BrushConverter colorConv = new BrushConverter();

            if (canOperate)
            {
                return (colorConv.ConvertFromString("Yellow") as Freezable).Clone();
            }
            else
            {
                return (colorConv.ConvertFromString("Crimson") as Freezable).Clone();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
