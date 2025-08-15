using System.Globalization;
using System.Windows.Data;
using RabbitCopy.Enums;

namespace RabbitCopy.Converters;

public class CopyModeEnumToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is CopyMode copyMode)
        {
            return copyMode switch
            {
                CopyMode.DIFF_NO_OVERWRITE => "Diff (No Overwrite)",
                CopyMode.DIFF_SIZE_DATE => "Diff (Size & Date)",
                CopyMode.DIFF_NEWER => "Diff (Newer Only)",
                CopyMode.COPY_OVERWRITE => "Copy (Overwrite)",
                CopyMode.SYNC_SIZE_DATE => "Sync (Size & Date)",
                CopyMode.MOVE_OVERWRITE => "Move (Overwrite)",
                CopyMode.MOVE_NO_OVERWRITE => "Move (No Overwrite)",
                _ => "Unknown Mode"
            };
        }

        return "Unknown Mode";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}