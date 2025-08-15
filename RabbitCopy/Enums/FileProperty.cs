namespace RabbitCopy.Enums;

[Flags]
public enum FileProperty : uint
{
    DATA = 1,
    ATTRIBUTES = 1 << 1,
    TIME_STAMP = 1 << 2,
    ALT_STREAMS = 1 << 3,
    ACL = 1 << 4,
    OWNER_INFORMATION = 1 << 5,
    AUDITING_INFORMATION = 1 << 6,
}