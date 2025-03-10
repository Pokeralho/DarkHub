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
                if (status == ResourceManagerHelper.Instance.InjectionSuccessStatus)
                    return new SolidColorBrush(Colors.Green);
                if (status == ResourceManagerHelper.Instance.InjectingStatus)
                    return new SolidColorBrush(Colors.Blue);
                if (status == ResourceManagerHelper.Instance.ProcessNotFoundStatus ||
                    status.Contains(ResourceManagerHelper.Instance.InjectionFailedStatus) ||
                    status.Contains(ResourceManagerHelper.Instance.ErrorStatus.Split(':')[0]))
                    return new SolidColorBrush(Colors.Red);
            }
            return new SolidColorBrush(Colors.White);
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