namespace RabbitCopy.RoboCopyModule;

public class RoboCopyOptionsBuilder
{
    private readonly RoboCopyOptions _robocopyOptions = new();

    public RoboCopyOptions Build()
    {
        return _robocopyOptions;
    }

    public RoboCopyOptionsBuilder WithSubDirs(bool includeEmpty)
    {
        _robocopyOptions.IncludeSubDirs = true;
        _robocopyOptions.ExcludeEmptyDirs = !includeEmpty;
        return this;
    }
}