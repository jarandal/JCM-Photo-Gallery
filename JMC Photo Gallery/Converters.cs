using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;

namespace JMC_Photo_Gallery
{

    [ValueConversion(typeof(Brush), typeof(Brush))]
    public class BrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
               object parameter, System.Globalization.CultureInfo culture)
        {

            Boolean Selected = (Boolean)value;

            if (Selected)
                return Brushes.Red;
            else
                return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType,
               object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
