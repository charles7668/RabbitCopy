namespace RabbitCopy.RoboCopyModule;

public class RoboCopyOptions
{
    public bool IncludeSubDirs { get; set; }

    public bool ExcludeEmptyDirs { get; set; }

    public bool DryRun { get; set; }
}