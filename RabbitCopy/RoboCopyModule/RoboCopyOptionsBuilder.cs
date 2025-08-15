using RabbitCopy.Enums;

namespace RabbitCopy.RoboCopyModule;

public class RoboCopyOptionsBuilder
{
    private readonly RoboCopyOptions _robocopyOptions = new();

    public RoboCopyOptions Build()
    {
        return _robocopyOptions;
    }

    public RoboCopyOptionsBuilder DryRun()
    {
        _robocopyOptions.DryRun = true;
        return this;
    }

    public RoboCopyOptionsBuilder EnableUnbufferedIo()
    {
        _robocopyOptions.UnbufferedIo = true;
        return this;
    }

    public RoboCopyOptionsBuilder SetCopyMode(CopyMode mode)
    {
        _robocopyOptions.CopyMode = mode;
        return this;
    }

    public RoboCopyOptionsBuilder WithSubDirs(bool includeEmpty)
    {
        _robocopyOptions.IncludeSubDirs = true;
        _robocopyOptions.ExcludeEmptyDirs = !includeEmpty;
        return this;
    }
}