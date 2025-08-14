using System.IO;
using System.Windows.Media.Imaging;

namespace RabbitCopy.Helper;

public static class ImageHelper
{
    public static BitmapImage? ByteArrayToBitmapImage(byte[] byteArray)
    {
        if (byteArray.Length == 0)
            return null;

        using var stream = new MemoryStream(byteArray);
        var bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.StreamSource = stream;
        bitmapImage.EndInit();
        return bitmapImage;
    }
}