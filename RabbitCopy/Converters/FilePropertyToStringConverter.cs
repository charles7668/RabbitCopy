using System.Globalization;
using System.Windows.Data;
using RabbitCopy.Enums;

namespace RabbitCopy.Converters;

public class FilePropertyToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var result = string.Empty;
        if (value is not FileProperty property)
            return result;
        if (property.HasFlag(FileProperty.DATA))
            result += "D";
        if (property.HasFlag(FileProperty.ATTRIBUTES))
            result += "A";
        if (property.HasFlag(FileProperty.TIME_STAMP))
            result += "T";
        if (property.HasFlag(FileProperty.ALT_STREAMS))
            result += "X";
        if (property.HasFlag(FileProperty.ACL))
            result += "S";
        if (property.HasFlag(FileProperty.OWNER_INFORMATION))
            result += "O";
        if (property.HasFlag(FileProperty.AUDITING_INFORMATION))
            result += "U";

        return result;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}