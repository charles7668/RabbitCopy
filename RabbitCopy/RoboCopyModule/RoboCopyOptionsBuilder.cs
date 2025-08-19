using System.IO;
using RabbitCopy.Enums;

namespace RabbitCopy.RoboCopyModule;

public class RoboCopyOptionsBuilder
{
    private readonly RoboCopyOptions _robocopyOptions = new();

    public RoboCopyOptions Build()
    {
        return _robocopyOptions;
    }

    public RoboCopyOptionsBuilder CreateOnly()
    {
        _robocopyOptions.CreateOnly = true;
        return this;
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

    public RoboCopyOptionsBuilder SetFileAttributes(FileAttributes include, FileAttributes exclude)
    {
        _robocopyOptions.IncludeFileAttributes = include;
        _robocopyOptions.ExcludeFileAttributes = exclude;
        return this;
    }

    public RoboCopyOptionsBuilder SetFileProperty(FileProperty property)
    {
        _robocopyOptions.FileProperties = property;
        return this;
    }

    public RoboCopyOptionsBuilder WithSubDirs(bool includeEmpty)
    {
        _robocopyOptions.IncludeSubDirs = true;
        _robocopyOptions.ExcludeEmptyDirs = !includeEmpty;
        return this;
    }

    public RoboCopyOptionsBuilder SetThreadNum(uint threadNum)
    {
        if (threadNum is < 1 or > 128)
            throw new ArgumentOutOfRangeException(nameof(threadNum), @"Thread number must be between 1 and 128.");
        _robocopyOptions.ThreadNum = threadNum;
        return this;
    }
}