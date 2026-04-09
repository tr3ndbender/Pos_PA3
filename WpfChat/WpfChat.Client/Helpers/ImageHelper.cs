using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace WpfChat.Client.Helpers
{
    public static class ImageHelper
    {
        public static BitmapImage? LoadAndResize(string filePath, int pixelSize = 50)
        {
            try
            {
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.DecodePixelWidth = pixelSize;
                bi.DecodePixelHeight = pixelSize;
                bi.UriSource = new Uri(filePath, UriKind.Absolute);
                bi.EndInit();
                bi.Freeze();
                return bi;
            }
            catch { return null; }
        }

        public static BitmapImage? FromBase64(string base64, int pixelSize = 50)
        {
            try
            {
                if (string.IsNullOrEmpty(base64)) return null;
                var bytes = Convert.FromBase64String(base64);
                return FromStream(new MemoryStream(bytes), pixelSize);
            }
            catch { return null; }
        }

        public static BitmapImage? FromStream(Stream stream, int pixelSize = 50)
        {
            try
            {
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.DecodePixelWidth = pixelSize;
                bi.DecodePixelHeight = pixelSize;
                bi.StreamSource = stream;
                bi.EndInit();
                bi.Freeze();
                stream.Close();
                return bi;
            }
            catch { return null; }
        }

        public static string? ToBase64(BitmapImage? image)
        {
            if (image == null) return null;
            try
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                using var ms = new MemoryStream();
                encoder.Save(ms);
                return Convert.ToBase64String(ms.ToArray());
            }
            catch { return null; }
        }
    }
}
