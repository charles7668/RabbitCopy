using System.IO;
using RabbitCopy.Enums;

namespace RabbitCopy.RoboCopyModule;

public class RoboCopyOptions
{
    public bool IncludeSubDirs { get; set; }

    public bool ExcludeEmptyDirs { get; set; }

    public bool DryRun { get; set; }

    public bool UnbufferedIo { get; set; }

    public FileProperty FileProperties { get; set; } =
        FileProperty.DATA | FileProperty.ATTRIBUTES | FileProperty.TIME_STAMP;

    public CopyMode CopyMode { get; set; } = CopyMode.DIFF_SIZE_DATE;

    public FileAttributes IncludeFileAttributes { get; set; }

    public FileAttributes ExcludeFileAttributes { get; set; }

    public bool CreateOnly { get; set; }

    public uint ThreadNum { get; set; } = 8;

    public string IoMaxSize { get; set; } = string.Empty;

    public string IoRate { get; set; } = string.Empty;

    public string Threshold { get; set; } = string.Empty;
}