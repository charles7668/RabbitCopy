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
        return string.Join(" ", args);
    }
}