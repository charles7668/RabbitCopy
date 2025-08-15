namespace RabbitCopy.Enums;

public enum CopyMode
{
    DIFF_NO_OVERWRITE = 0,
    DIFF_SIZE_DATE = 1,
    DIFF_NEWER = 2,
    COPY_OVERWRITE = 3,
    SYNC_SIZE_DATE = 4,
    MOVE_OVERWRITE = 5,
    MOVE_NO_OVERWRITE = 6
}