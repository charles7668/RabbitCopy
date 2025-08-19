using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace RabbitCopy.Converters;

public class FileAttributesToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var result = string.Empty;
        if (value is not FileAttributes property)
            return result;
        if (property.HasFlag(FileAttributes.ReadOnly))
            result += "R";
        if (property.HasFlag(FileAttributes.Archive))
            result += "A";
        if (property.HasFlag(FileAttributes.System))
            result += "S";
        if (property.HasFlag(FileAttributes.Hidden))
            result += "H";
        if (property.HasFlag(FileAttributes.Compressed))
            result += "C";
        if (property.HasFlag(FileAttributes.NotContentIndexed))
            result += "N";
        if (property.HasFlag(FileAttributes.Encrypted))
            result += "E";
        if (property.HasFlag(FileAttributes.Temporary))
            result += "T";
        if (property.HasFlag(FileAttributes.Offline))
            result += "O";

        return result;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}