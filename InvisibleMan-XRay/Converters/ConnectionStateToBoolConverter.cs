using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace InvisibleManXRay.Converters
{
    using ViewModels;

    /// <summary>
    /// Конвертер состояний Hero-зоны (Шаг 4, MVVM).
    /// Value = Visibility, Parameter = требуемое ConnectionState ("Disconnected"/"Connecting"/"Connected").
    /// Видим только тогда, когда текущее состояние VM совпадает с запрошенным.
    /// Использование:
    ///   Visibility="{Binding State, Converter={StaticResource ConnectionStateToVisibility}, ConverterParameter=Connected}"
    /// </summary>
    public class ConnectionStateToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not MainViewModel.ConnectionState state)
                return Visibility.Collapsed;

            if (parameter is not string requested)
                return Visibility.Collapsed;

            bool matches = Enum.TryParse(requested, ignoreCase: true, out MainViewModel.ConnectionState parsed)
                && state == parsed;

            return matches ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
