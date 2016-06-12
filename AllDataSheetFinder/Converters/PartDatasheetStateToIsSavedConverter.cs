﻿using System;
using System.Windows.Data;

namespace AllDataSheetFinder.Converters
{
    public class PartDatasheetStateToIsSavedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            PartDatasheetState state = (PartDatasheetState)value;
            return state == PartDatasheetState.Saved;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
