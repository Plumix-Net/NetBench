using Avalonia.Platform;
using SkiaSharp;

namespace NetBench.Screenshots;

/// <summary>
/// Из кадра приложения — картинки для сторов: экран телефона со статус-баром и home-индикатором,
/// затем витринный кадр нужного размера — подпись сверху, телефон с рамкой ниже.
/// Шрифт — Inter, тот же, что в приложении (из пакета Avalonia.Fonts.Inter).
/// </summary>
internal static class StoreFrame
{
    private const float Pt = (float)ShotSession.Scale;

    private static readonly SKColor _tile = SKColor.Parse("#11141D"); // фон логотипа Plumix
    private static readonly SKColor _glow = SKColor.Parse("#3B2370");  // фиолетовый знака Plumix, приглушённый
    private static readonly SKColor _caption = SKColors.White;

    private static readonly Lazy<SKTypeface> _bold = new(() => Inter("Bold"));
    private static readonly Lazy<SKTypeface> _semiBold = new(() => Inter("SemiBold"));

    /// <summary>Полный экран телефона 1320×2868: кадр приложения между статус-баром и home-индикатором.</summary>
    public static SKBitmap Phone(SKBitmap app, bool dark)
    {
        var width = app.Width;
        var top = (int)(ShotSession.StatusBarHeight * Pt);
        var height = top + app.Height + (int)(ShotSession.HomeIndicatorHeight * Pt);
        var ink = dark ? SKColors.White : SKColors.Black;

        var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);

        // Полосы — цветом фона у края экрана (в углу: по центру снизу бывает кнопка), приложение рисует edge-to-edge.
        using (var paint = new SKPaint { Color = app.GetPixel(2, 1) })
            canvas.DrawRect(0, 0, width, top, paint);
        using (var paint = new SKPaint { Color = app.GetPixel(2, app.Height - 2) })
            canvas.DrawRect(0, top + app.Height, width, height - top - app.Height, paint);
        canvas.DrawBitmap(app, 0, top);

        using var fill = new SKPaint { Color = ink, IsAntialias = true };

        // Время в левом «ухе», как на iPhone; справа — сеть, Wi-Fi и батарея.
        using (var font = new SKFont(_semiBold.Value, 17 * Pt))
            canvas.DrawText("9:41", 62 * Pt, 39 * Pt, SKTextAlign.Center, font, fill);

        var x = 318 * Pt;
        var baseline = 35 * Pt;
        for (var bar = 0; bar < 4; bar++)
        {
            var barHeight = (4 + bar * 2.6f) * Pt;
            canvas.DrawRoundRect(x + bar * 4.6f * Pt, baseline - barHeight, 3 * Pt, barHeight, 0.8f * Pt, 0.8f * Pt, fill);
        }

        x += 25 * Pt;
        using (var path = new SKPath())
        {
            var center = new SKPoint(x + 8 * Pt, baseline);
            path.MoveTo(center);
            path.ArcTo(new SKRect(center.X - 8.5f * Pt, center.Y - 11.5f * Pt, center.X + 8.5f * Pt, center.Y + 5.5f * Pt),
                -135, 90, false);
            path.Close();
            canvas.DrawPath(path, fill);
        }

        x += 24 * Pt;
        using (var outline = new SKPaint { Color = ink.WithAlpha(110), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1 * Pt })
            canvas.DrawRoundRect(new SKRect(x, baseline - 12 * Pt, x + 25 * Pt, baseline), 4 * Pt, 4 * Pt, outline);
        canvas.DrawRoundRect(new SKRect(x + 2 * Pt, baseline - 10 * Pt, x + 20 * Pt, baseline - 2 * Pt), 2.5f * Pt, 2.5f * Pt, fill);
        using (var cap = new SKPaint { Color = ink.WithAlpha(110), IsAntialias = true })
            canvas.DrawRoundRect(new SKRect(x + 26.5f * Pt, baseline - 8 * Pt, x + 28 * Pt, baseline - 4 * Pt), 1 * Pt, 1 * Pt, cap);

        // Dynamic Island и home-индикатор.
        using (var island = new SKPaint { Color = SKColors.Black, IsAntialias = true })
            canvas.DrawRoundRect(new SKRect(width / 2f - 63 * Pt, 11 * Pt, width / 2f + 63 * Pt, 48 * Pt), 18.5f * Pt, 18.5f * Pt, island);
        canvas.DrawRoundRect(new SKRect(width / 2f - 72 * Pt, height - 13 * Pt, width / 2f + 72 * Pt, height - 8 * Pt),
            2.5f * Pt, 2.5f * Pt, fill);

        return bitmap;
    }

    /// <summary>
    /// Кегль подписей набора: самый крупный, при котором каждая укладывается в две строки, —
    /// один на все кадры, чтобы телефоны стояли на одной высоте.
    /// </summary>
    public static float CaptionSize(IEnumerable<string> captions, int width)
    {
        var unit = width / 1320f;
        using var font = new SKFont(_bold.Value, 104 * unit);
        foreach (var caption in captions)
        {
            while (Wrap(caption, font, width - 140 * unit).Count > 2)
                font.Size *= 0.96f;
        }

        return font.Size;
    }

    /// <summary>Витринный кадр width×height: подпись в 1–2 строки сверху, телефон в рамке под ней.</summary>
    public static SKBitmap Marketing(SKBitmap phone, string caption, float captionSize, int width, int height)
    {
        var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        var unit = width / 1320f;

        using (var background = new SKPaint())
        {
            background.Shader = SKShader.CreateRadialGradient(
                new SKPoint(width * 0.5f, height * 0.05f), height * 0.75f, [_glow, _tile], SKShaderTileMode.Clamp);
            canvas.DrawRect(0, 0, width, height, background);
        }

        // Подпись переносится по обычным пробелам — неразрывные держат предлог со словом.
        var size = captionSize;
        using var font = new SKFont(_bold.Value, size);
        var lines = Wrap(caption, font, width - 140 * unit);

        using var text = new SKPaint { Color = _caption, IsAntialias = true };
        var lineHeight = size * 1.18f;
        var y = 150 * unit + size;
        foreach (var line in lines)
        {
            canvas.DrawText(line, width / 2f, y, SKTextAlign.Center, font, text);
            y += lineHeight;
        }

        // Телефон: от места под две строки подписи до низа кадра с полем, пропорции экрана сохраняются.
        var top = 150 * unit + lineHeight * 2 + 90 * unit;
        var bottom = height - 90 * unit;
        var screenHeight = bottom - top;
        var screenWidth = screenHeight * phone.Width / phone.Height;
        var screen = SKRect.Create((width - screenWidth) / 2, top, screenWidth, screenHeight);
        var radius = 55 * screenWidth / (float)ShotSession.ScreenWidth; // скругление экрана iPhone, 55 pt
        var bezel = 16 * screenWidth / 1000;

        var body = screen;
        body.Inflate(bezel, bezel);
        using (var shadow = new SKPaint { Color = SKColors.Black.WithAlpha(150), IsAntialias = true })
        {
            shadow.ImageFilter = SKImageFilter.CreateBlur(40 * unit, 40 * unit);
            canvas.DrawRoundRect(body, radius + bezel, radius + bezel, shadow);
        }

        using (var frame = new SKPaint { Color = SKColor.Parse("#05060A"), IsAntialias = true })
            canvas.DrawRoundRect(body, radius + bezel, radius + bezel, frame);
        using (var rim = new SKPaint { Color = SKColor.Parse("#3A3F52"), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 3 * unit })
            canvas.DrawRoundRect(body, radius + bezel, radius + bezel, rim);

        canvas.Save();
        canvas.ClipRoundRect(new SKRoundRect(screen, radius, radius), antialias: true);
        using (var image = SKImage.FromBitmap(phone))
            canvas.DrawImage(image, screen, new SKSamplingOptions(SKCubicResampler.Mitchell));
        canvas.Restore();

        return bitmap;
    }

    public static void SavePng(SKBitmap bitmap, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        // Сторы не принимают прозрачность в скриншотах — кодируем без альфы.
        using var opaque = new SKBitmap(new SKImageInfo(bitmap.Width, bitmap.Height, SKColorType.Rgb888x, SKAlphaType.Opaque));
        using (var canvas = new SKCanvas(opaque))
            canvas.DrawBitmap(bitmap, 0, 0);
        using var data = opaque.Encode(SKEncodedImageFormat.Png, 100);
        using var file = File.Create(path);
        data.SaveTo(file);
    }

    private static List<string> Wrap(string caption, SKFont font, float maxWidth)
    {
        var lines = new List<string>();
        foreach (var paragraph in caption.Split('\n'))
        {
            var line = "";
            foreach (var word in paragraph.Split(' '))
            {
                var candidate = line.Length == 0 ? word : line + " " + word;
                if (line.Length > 0 && font.MeasureText(candidate) > maxWidth)
                {
                    lines.Add(line);
                    line = word;
                }
                else
                {
                    line = candidate;
                }
            }

            lines.Add(line);
        }

        return lines;
    }

    private static SKTypeface Inter(string weight)
    {
        using var stream = AssetLoader.Open(new Uri($"avares://Avalonia.Fonts.Inter/Assets/Inter-{weight}.ttf"));
        return SKTypeface.FromStream(stream);
    }
}
