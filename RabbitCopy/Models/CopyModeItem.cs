using System.Globalization;
using RabbitCopy.Converters;
using RabbitCopy.Enums;

namespace RabbitCopy.Models;

public record CopyModeItem
{
    public CopyMode Mode { get; set; }

    public string Description { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public override string ToString()
    {
        var converter = new CopyModeEnumToStringConverter();
        var modeString = (string?)converter.Convert(Mode, typeof(string), null, CultureInfo.CurrentCulture);
        return modeString ?? "";
    }
}