using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Data;
using System.Globalization;

namespace JMC_Photo_Gallery
{
    [ValueConversion(typeof(double), typeof(string))]
    public class DoublePrecisionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            int i = (int)((double)value * 100);
            double d = (double)i / 100;
            string s = d.ToString();
            if (s.LastIndexOf(".") < 0)
                s += ".00";
            while (s.LastIndexOf(".") + 2 >= s.Length)
                s += "0";
            return s;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return double.Parse((string)value);
        }
    }
}
