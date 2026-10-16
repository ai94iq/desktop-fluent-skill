using Avalonia.Data.Converters;

namespace __Product__.Desktop.Common;

// IsVisible="{Binding State, Converter={x:Static common:LoadStateIs.Instance}, ConverterParameter=Empty}"
public sealed class LoadStateIs : IValueConverter
{
    public static LoadStateIs Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is LoadState state && Enum.TryParse<LoadState>(parameter as string, out var wanted) && state == wanted;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
