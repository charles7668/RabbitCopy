using RabbitCopy.Enums;

namespace RabbitCopy.RoboCopyModule;

public class RoboCopyOptions
{
    public bool IncludeSubDirs { get; set; }

    public bool ExcludeEmptyDirs { get; set; }

    public bool DryRun { get; set; }

    public CopyMode CopyMode { get; set; } = CopyMode.DIFF_SIZE_DATE;
}