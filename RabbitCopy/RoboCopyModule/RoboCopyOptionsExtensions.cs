using System.Globalization;
using System.IO;
using RabbitCopy.Converters;
using RabbitCopy.Enums;

namespace RabbitCopy.RoboCopyModule;

public static class RoboCopyOptionsExtensions
{
    private static string CopyModeArgs(CopyMode mode)
    {
        return mode switch
        {
            CopyMode.DIFF_NO_OVERWRITE => "/xo /xn /xc",
            CopyMode.DIFF_SIZE_DATE => "",
            CopyMode.DIFF_NEWER => "/xo /xc",
            CopyMode.COPY_OVERWRITE => "/is",
            CopyMode.SYNC_SIZE_DATE => "/mir",
            CopyMode.MOVE_OVERWRITE => "/move /is",
            CopyMode.MOVE_NO_OVERWRITE => "/move /xo /xn /xc",
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }

    private static string FileAttributesArgs(FileAttributes attr)
    {
        var converter = new FileAttributesToStringConverter();
        var converted = (string?)converter.Convert(attr, typeof(string), null, CultureInfo.CurrentCulture);
        return converted ?? "";
    }

    private static string FilePropertiesArgs(FileProperty fileProperty)
    {
        if (fileProperty == (FileProperty.DATA | FileProperty.ATTRIBUTES | FileProperty.TIME_STAMP))
            return "";

        var converter = new FilePropertyToStringConverter();
        var converted = (string?)converter.Convert(fileProperty, typeof(string), null, CultureInfo.CurrentCulture);
        if (!string.IsNullOrEmpty(converted))
            return "/copy:" + converted;
        return "/nocopy";
    }

    public static string ToArgsString(this RoboCopyOptions options)
    {
        List<string> args = [];
        if (options.IncludeSubDirs)
            args.Add("/e");
        if (options.ExcludeEmptyDirs)
            args.Add("/s");
        if (options.DryRun)
            args.Add("/l");
        if (options.UnbufferedIo)
            args.Add("/j");
        args.Add(CopyModeArgs(options.CopyMode));
        args.Add(FilePropertiesArgs(options.FileProperties));
        if (options.IncludeFileAttributes != FileAttributes.None)
            args.Add($"/a+:{FileAttributesArgs(options.IncludeFileAttributes)}");
        if (options.ExcludeFileAttributes != FileAttributes.None)
            args.Add($"/a-:{FileAttributesArgs(options.ExcludeFileAttributes)}");
        if (options.CreateOnly)
            args.Add("/create");
        if (options.ThreadNum != 8)
            args.Add($"/mt:{options.ThreadNum}");

        return string.Join(" ", args);
    }
}