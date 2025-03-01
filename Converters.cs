using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DarkHub
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                if (status.Contains("sucesso"))
                    return new SolidColorBrush(Colors.Green);
                if (status.Contains("Erro") || status.Contains("Falha"))
                    return new SolidColorBrush(Colors.Red);
                if (status.Contains("Injetando"))
                    return new SolidColorBrush(Colors.Blue);
            }
            return new SolidColorBrush(Colors.Black);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;
            return value;
        }
    }
} 