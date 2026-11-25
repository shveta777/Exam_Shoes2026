using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ShoeStore.Helpers;

public sealed class PhotoConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        PhotoFiles.Load(value as string);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public static class PhotoFiles
{
    private static readonly string DirectoryPath = Path.Combine(AppContext.BaseDirectory, "Photos");
    private const string ResourceRoot = "pack://application:,,,/ShoeStore;component/Resources/";

    public static BitmapSource Load(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return Placeholder();
        }

        try
        {
            var path = LocalPath(fileName);

            if (File.Exists(path))
            {
                return Read(path);
            }

            var resourceUri = new Uri(ResourceRoot + "Catalog/" + Uri.EscapeDataString(fileName));
            using var stream = Application.GetResourceStream(resourceUri)?.Stream;
            if (stream is not null)
            {
                return Decode(stream);
            }
        }
        catch (Exception error) when (error is IOException or ArgumentException or FormatException
            or NotSupportedException or UnauthorizedAccessException)
        {
            return Placeholder();
        }

        return Placeholder();
    }

    public static BitmapSource Read(string fileName)
    {
        using var stream = File.OpenRead(fileName);
        return Decode(stream);
    }

    public static string Save(BitmapSource source)
    {
        var scale = Math.Min(1d, Math.Min(300d / source.PixelWidth, 200d / source.PixelHeight));
        if (scale < 1)
        {
            source = new TransformedBitmap(source, new ScaleTransform(scale, scale));
        }

        var fileName = Guid.NewGuid().ToString("N") + ".jpg";
        Directory.CreateDirectory(DirectoryPath);

        var encoder = new JpegBitmapEncoder { QualityLevel = 90 };
        encoder.Frames.Add(BitmapFrame.Create(source));
        try
        {
            using var stream = File.Create(LocalPath(fileName));
            encoder.Save(stream);
        }
        catch
        {
            Delete(fileName);
            throw;
        }

        return fileName;
    }

    public static void Delete(string fileName)
    {
        var path = LocalPath(fileName);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static string LocalPath(string fileName)
    {
        if (fileName != Path.GetFileName(fileName) || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException("Недопустимое имя файла фотографии.", nameof(fileName));
        }

        return Path.Combine(DirectoryPath, fileName);
    }

    private static BitmapSource Decode(Stream stream)
    {
        var bitmap = BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        bitmap.Freeze();
        return bitmap;
    }

    private static BitmapImage Placeholder() => new(new Uri(ResourceRoot + "picture.png"));
}

