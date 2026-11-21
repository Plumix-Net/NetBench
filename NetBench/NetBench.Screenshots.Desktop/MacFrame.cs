using Avalonia.Platform;
using SkiaSharp;

/// <summary>
/// Витринный кадр Mac App Store 2880×1800: подпись в одну строку сверху, под ней окно приложения
/// с заголовком в стиле macOS (кнопки-«светофор») и тенью. Фон и шрифт — как у мобильных кадров.
/// </summary>
internal static class MacFrame
{
    public const int Width = 2880;
    public const int Height = 1800;

    private static readonly SKColor _tile = SKColor.Parse("#11141D"); // фон логотипа Plumix
    private static readonly SKColor _glow = SKColor.Parse("#3B2370");  // фиолетовый знака Plumix, приглушённый

    private static readonly Lazy<SKTypeface> _bold = new(() => Inter("Bold"));

    private const float CaptionTop = 120;
    private const float SideMargin = 200;
    private const float TitleBar = 56; // 28 pt × 2
    private const float Corner = 24;   // скругление окна macOS, 12 pt × 2

    /// <summary>Общий размер подписи для набора: самая длинная должна влезть в одну строку.</summary>
    public static float CaptionSize(IEnumerable<string> captions)
    {
        using var font = new SKFont(_bold.Value, 112);
        foreach (var caption in captions)
        {
            while (font.MeasureText(caption) > Width - 2 * SideMargin)
                font.Size *= 0.97f;
        }

        return font.Size;
    }

    public static SKBitmap Marketing(SKBitmap app, string caption, float captionSize)
    {
        var bitmap = new SKBitmap(Width, Height);
        using var canvas = new SKCanvas(bitmap);

        using (var background = new SKPaint())
        {
            background.Shader = SKShader.CreateRadialGradient(
                new SKPoint(Width * 0.5f, Height * 0.02f), Height * 0.95f, [_glow, _tile], SKShaderTileMode.Clamp);
            canvas.DrawRect(0, 0, Width, Height, background);
        }

        using var font = new SKFont(_bold.Value, captionSize);
        using (var text = new SKPaint { Color = SKColors.White, IsAntialias = true })
            canvas.DrawText(caption, Width / 2f, CaptionTop + captionSize, SKTextAlign.Center, font, text);

        // Окно: заголовок + кадр приложения, вписано между подписью и низом кадра с полем.
        var top = CaptionTop + captionSize * 1.25f + 90;
        var available = Height - top - 90;
        var scale = Math.Min((Width - 2 * SideMargin) / app.Width, available / (app.Height + TitleBar));
        var windowWidth = app.Width * scale;
        var titleHeight = TitleBar * scale;
        var windowHeight = app.Height * scale + titleHeight;
        var window = SKRect.Create((Width - windowWidth) / 2, top, windowWidth, windowHeight);
        var radius = Corner * scale;

        using (var shadow = new SKPaint { Color = SKColors.Black.WithAlpha(170), IsAntialias = true })
        {
            shadow.ImageFilter = SKImageFilter.CreateBlur(50, 50);
            var shadowRect = window;
            shadowRect.Offset(0, 24);
            canvas.DrawRoundRect(shadowRect, radius, radius, shadow);
        }

        canvas.Save();
        canvas.ClipRoundRect(new SKRoundRect(window, radius, radius), antialias: true);

        // Заголовок — цветом фона окна (левый верхний пиксель кадра), чуть светлее/темнее для отделения.
        var windowBackground = app.GetPixel(2, 2);
        using (var bar = new SKPaint { Color = Shade(windowBackground) })
            canvas.DrawRect(window.Left, window.Top, window.Width, titleHeight, bar);
        string[] lights = ["#FF5F57", "#FEBC2E", "#28C840"];
        for (var i = 0; i < lights.Length; i++)
        {
            using var dot = new SKPaint { Color = SKColor.Parse(lights[i]), IsAntialias = true };
            canvas.DrawCircle(window.Left + (32 + i * 40) * scale, window.Top + titleHeight / 2, 12 * scale, dot);
        }

        using (var image = SKImage.FromBitmap(app))
            canvas.DrawImage(image, SKRect.Create(window.Left, window.Top + titleHeight, windowWidth, windowHeight - titleHeight),
                new SKSamplingOptions(SKCubicResampler.Mitchell));
        canvas.Restore();

        using (var rim = new SKPaint { Color = SKColors.White.WithAlpha(40), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 })
            canvas.DrawRoundRect(window, radius, radius, rim);

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

    private static SKColor Shade(SKColor color)
    {
        var light = color.Red + color.Green + color.Blue > 3 * 128;
        var delta = light ? -10 : 14;
        static byte Clamp(int value) => (byte)Math.Clamp(value, 0, 255);
        return new SKColor(Clamp(color.Red + delta), Clamp(color.Green + delta), Clamp(color.Blue + delta));
    }

    private static SKTypeface Inter(string weight)
    {
        using var stream = AssetLoader.Open(new Uri($"avares://Avalonia.Fonts.Inter/Assets/Inter-{weight}.ttf"));
        return SKTypeface.FromStream(stream);
    }
}
