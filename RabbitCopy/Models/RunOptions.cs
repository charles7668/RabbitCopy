namespace RabbitCopy.Models;

public class RunOptions
{
    public string? DestPath { get; set; }

    public string[]? SrcPaths { get; set; }

    public bool OpenUI { get; set; }

    public string? Guid { get; set; }
}