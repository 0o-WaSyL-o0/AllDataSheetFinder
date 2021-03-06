﻿using System;
using System.Windows.Data;

namespace AllDataSheetFinder.Converters
{
    class IntegerGreaterThanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int x = (int)value;
            int cutoff = (int)parameter;

            return x > cutoff;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
