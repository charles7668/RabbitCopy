using System.IO;
using RabbitCopy.Enums;

namespace RabbitCopy.Models;

public class Config
{
    public bool CreateOnly { get; set; }

    public bool EnableFilterFileAttributes { get; set; }

    public bool EnableFilterName { get; set; }

    public bool EnableThrottling { get; set; }

    public FileAttributes ExcFileAttributes { get; set; }

    public bool ExcludeEmptyDirsOption { get; set; }

    public FileProperty FileProperty { get; set; }

    public FileAttributes FilterFileAttributes { get; set; }

    public string FilterName { get; set; } = string.Empty;

    public FileAttributes IncFileAttributes { get; set; }

    public CopyModeItem SelectedCopyMode { get; set; } = new();

    public string SelectedIoMaxSizeThrottlingUnit { get; set; } = string.Empty;

    public string SelectedIoRateThrottlingUnit { get; set; } = string.Empty;

    public string SelectedThresholdThrottlingUnit { get; set; } = string.Empty;

    public uint ThreadNum { get; set; }

    public uint ThrottlingIoMaxSize { get; set; }

    public uint ThrottlingIoRate { get; set; }

    public uint ThrottlingThreshold { get; set; }

    public bool UnbufferedIo { get; set; }
}