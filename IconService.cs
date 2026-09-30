using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ChatGPT
{
    public sealed class IconService
    {
        private static readonly Lazy<IconService> _instance = new(() => new IconService());
        public static IconService Instance => _instance.Value;

        private readonly object _lock = new();
        private readonly Dictionary<string, Bitmap> _cache = new(StringComparer.OrdinalIgnoreCase);

        public Bitmap LoadIcon(string resourceName, int targetSize, bool crop = false, int feather = 10)
        {
            string key = $"{resourceName}_{targetSize}_{crop}_{feather}";
            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var bmp) && bmp != null)
                    return bmp;
            }

            try
            {
                string? actualResource = FindResourceName(resourceName);
                if (actualResource == null)
                    return CreateEmptyBitmap(targetSize);

                using Stream? stream = Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream(actualResource);
                if (stream == null)
                    return CreateEmptyBitmap(targetSize);

                using var icon = new Icon(stream, new Size(targetSize, targetSize));
                using Bitmap src = icon.ToBitmap();
                Bitmap result = crop
                    ? CropToCircleCached(src, feather, targetSize)
                    : ScaleBitmap(src, targetSize);

                lock (_lock)
                {
                    if (_cache.TryGetValue(key, out var already) && already != null)
                    {
                        result.Dispose();
                        return already;
                    }
                    _cache[key] = result;
                }
                return result;
            }
            catch
            {
                return CreateEmptyBitmap(targetSize);
            }
        }

        public Task WarmupIconCacheAsync()
        {
            var jobs = new List<Task>();

            // Сервисы на оверлее (большие кнопки 128x128, crop в круг)
            jobs.Add(Task.Run(() => Preload("ChatGPT.Resources.ChatGPT.ico", 128, true, 10)));
            jobs.Add(Task.Run(() => Preload("ChatGPT.Resources.Claude.ico", 128, true, 10)));
            jobs.Add(Task.Run(() => Preload("ChatGPT.Resources.Gemini.ico", 128, true, 10)));
            jobs.Add(Task.Run(() => Preload("ChatGPT.Resources.DeepSeek.ico", 128, true, 10)));
            jobs.Add(Task.Run(() => Preload("ChatGPT.Resources.Grok.ico", 128, true, 10)));
            jobs.Add(Task.Run(() => Preload("ChatGPT.Resources.Copilot.ico", 128, true, 10)));

            // Тулбар (24x24)
            jobs.Add(Task.Run(() => Preload("ChatGPT.Resources.Menu.ico", 24, false, 10)));
            jobs.Add(Task.Run(() => Preload("ChatGPT.Resources.Fullscreen.ico", 24, false, 10)));
            jobs.Add(Task.Run(() => Preload("ChatGPT.Resources.Browser.ico", 24, false, 10)));
            jobs.Add(Task.Run(() => Preload("ChatGPT.Resources.Adressbar.ico", 24, false, 10)));
            jobs.Add(Task.Run(() => Preload("ChatGPT.Resources.Settings.ico", 24, false, 10)));

            // Оверлейные кнопки (48x48, без обрезки для ровных гладких краев)
            jobs.Add(Task.Run(() => Preload("ChatGPT.Resources.on.ico", 48, false, 10)));
            jobs.Add(Task.Run(() => Preload("ChatGPT.Resources.off.ico", 48, false, 10)));
            jobs.Add(Task.Run(() => Preload("ChatGPT.Resources.no.ico", 48, false, 10)));
            jobs.Add(Task.Run(() => Preload("ChatGPT.Resources.Close.ico", 48, false, 10)));
            jobs.Add(Task.Run(() => Preload("ChatGPT.Resources.Settings.ico", 48, false, 10)));

            return Task.WhenAll(jobs);
        }

        private void Preload(string resourceName, int size, bool crop, int feather)
        {
            _ = LoadIcon(resourceName, size, crop, feather);
        }

        private static string? FindResourceName(string resourceName)
        {
            var names = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            return names.FirstOrDefault(n => string.Equals(n, resourceName, StringComparison.OrdinalIgnoreCase))
                ?? names.FirstOrDefault(n => n.EndsWith("." + resourceName, StringComparison.OrdinalIgnoreCase)
                                          || n.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase));
        }

        private static Bitmap ScaleBitmap(Bitmap src, int targetSize)
        {
            var result = new Bitmap(targetSize, targetSize);
            using (var g = Graphics.FromImage(result))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.Clear(Color.Transparent);

                g.DrawImage(src, 0, 0, targetSize, targetSize);
            }
            return result;
        }

        private static Bitmap CropToCircleCached(Bitmap src, int feather, int targetSize)
        {
            int size = Math.Min(src.Width, src.Height);
            using Bitmap croppedSrc = new Bitmap(size, size);
            using (var g = Graphics.FromImage(croppedSrc))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.Clear(Color.Transparent);

                using (var path = new GraphicsPath())
                {
                    path.AddEllipse(feather, feather, size - 2 * feather, size - 2 * feather);
                    g.SetClip(path);
                    g.DrawImage(src, new Rectangle(feather, feather, size - 2 * feather, size - 2 * feather),
                                new Rectangle(0, 0, src.Width, src.Height), GraphicsUnit.Pixel);
                }
            }

            var result = new Bitmap(targetSize, targetSize);
            using (var g = Graphics.FromImage(result))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.Clear(Color.Transparent);

                g.DrawImage(croppedSrc, 0, 0, targetSize, targetSize);
            }

            return result;
        }

        private static Bitmap CreateEmptyBitmap(int size)
        {
            var bmp = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bmp))
            {
                using var br = new SolidBrush(Color.Transparent);
                g.FillRectangle(br, 0, 0, size, size);
            }
            return bmp;
        }
    }
}
