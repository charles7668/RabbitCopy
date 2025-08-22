namespace RabbitCopy.Models;

public record ConfigIdentity
{
    public string Name { get; init; } = string.Empty;
    public string Guid { get; init; } = string.Empty;
}