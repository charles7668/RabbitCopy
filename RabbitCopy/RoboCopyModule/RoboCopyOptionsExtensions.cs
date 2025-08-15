using RabbitCopy.Enums;

namespace RabbitCopy.RoboCopyModule;

public static class RoboCopyOptionsExtensions
{
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

        return string.Join(" ", args);
    }

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
}