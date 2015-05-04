﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace AllDataSheetFinder.Converters
{
    public class PartDatasheetStateToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            PartDatasheetState state = (PartDatasheetState)value;

            switch (state)
            {
                case PartDatasheetState.Downloading:
                case PartDatasheetState.Saved: return Color.FromRgb(0xFF, 0xE8, 0x9C);

                case PartDatasheetState.DownloadingAndOpening:
                case PartDatasheetState.Cached: return Color.FromRgb(0x85, 0xC6, 0xFF);
                default: return Colors.White;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
