#!/usr/bin/env python3
"""Генерирует иконки и сплэш NetBench для Android/iOS, иконку для Google Play и иконки десктопа.

Знак — логотип Plumix (два шеврона), векторизованный по landing/src/assets/plumix-logo.jpg
(1920×1920). Геометрия задана один раз в координатах адаптивной иконки Android (108×108):
исходный квадрат логотипа занимает её видимую часть 72×72 (18..90), и все вершины лежат
в безопасном круге радиусом 33 — поэтому кадр одинаков на iOS, в Play и в лаунчерах Android.

Запуск из корня репозитория: python3 scripts/generate_icons.py (нужен Pillow).
"""

from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parent.parent
ANDROID_RES = ROOT / "NetBench" / "NetBench.Android" / "Resources"
IOS_ASSETS = ROOT / "NetBench" / "NetBench.iOS" / "Assets.xcassets"
APP_ASSETS = ROOT / "NetBench" / "NetBench" / "Assets"
DESKTOP_PACKAGING = ROOT / "NetBench" / "NetBench.Desktop" / "Packaging"
PLAY_METADATA = ROOT / "fastlane" / "metadata" / "android"
PLAY_LANGUAGES = ("en-US", "ru-RU")

TILE = "#11141D"  # фон логотипа Plumix
RPS_BLUE = "#1E90FF"  # NetBenchPalette.Rps — акцент для витринной графики
SPLASH_LIGHT = "#F5F6F8"  # NetBenchPalette.Light.BgBase
SPLASH_DARK = "#101014"  # NetBenchPalette.Dark.BgBase


def logo(points):
    """Вершины в пикселях исходника 1920×1920 → координаты 108×108."""
    return [(round(18 + x * 72 / 1920, 2), round(18 + y * 72 / 1920, 2)) for x, y in points]


# Порядок важен: нижнее плечо правого шеврона рисуется поверх верхнего (линия сгиба).
SHAPES = [
    ("#751497", logo([(481.8, 300), (739.8, 300), (1134.6, 848.3),
                      (603.3, 1617), (371.4, 1617), (883.6, 859.8)])),
    ("#653ABF", logo([(852.0, 300), (1108.4, 300), (1505.0, 823.8), (1243.5, 843.7)])),
    ("#834CDC", logo([(1378.7, 653.2), (1505.0, 823.8), (1195.8, 1266), (946.8, 1266)])),
]

# Плитка на iOS и в Play — видимая часть адаптивной иконки (x, y, сторона в координатах 108).
VISIBLE = (18, 18, 72)


def hex_rgb(value):
    value = value.lstrip("#")
    return tuple(int(value[i:i + 2], 16) for i in (0, 2, 4))


def render(size, viewport=VISIBLE, tile=True):
    """Рисует знак в квадрат size×size; viewport = (x, y, сторона) в координатах 108."""
    scale_up = 4
    canvas = size * scale_up
    vx, vy, vside = viewport
    k = canvas / vside

    background = (*hex_rgb(TILE), 255) if tile else (0, 0, 0, 0)
    image = Image.new("RGBA", (canvas, canvas), background)
    draw = ImageDraw.Draw(image)
    for color, points in SHAPES:
        draw.polygon([((x - vx) * k, (y - vy) * k) for x, y in points], fill=(*hex_rgb(color), 255))

    return image.resize((size, size), Image.LANCZOS)


def save_png(image, path, opaque=False):
    path.parent.mkdir(parents=True, exist_ok=True)
    if opaque:
        image = image.convert("RGB")
    image.save(path, optimize=True)
    print("wrote", path.relative_to(ROOT))


def save_text(path, text):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")
    print("wrote", path.relative_to(ROOT))


def path_data(points):
    head, *tail = points
    return f"M{head[0]},{head[1]}" + "".join(f" L{x},{y}" for x, y in tail) + " Z"


def vector_drawable(mono):
    paths = "\n".join(
        f'  <path android:fillColor="{"#FF000000" if mono else "#FF" + color.lstrip("#")}"\n'
        f'      android:pathData="{path_data(points)}" />'
        for color, points in SHAPES)
    return f"""<?xml version="1.0" encoding="utf-8"?>
<!-- Сгенерировано scripts/generate_icons.py — правьте скрипт, а не файл. -->
<vector xmlns:android="http://schemas.android.com/apk/res/android"
    android:width="108dp" android:height="108dp"
    android:viewportWidth="108" android:viewportHeight="108">
{paths}
</vector>
"""


ADAPTIVE_ICON = """<?xml version="1.0" encoding="utf-8"?>
<!-- Сгенерировано scripts/generate_icons.py — правьте скрипт, а не файл. -->
<adaptive-icon xmlns:android="http://schemas.android.com/apk/res/android">
  <background android:drawable="@color/ic_launcher_background" />
  <foreground android:drawable="@drawable/ic_launcher_foreground" />
  <monochrome android:drawable="@drawable/ic_launcher_monochrome" />
</adaptive-icon>
"""

ANDROID_DENSITIES = {"mdpi": 48, "hdpi": 72, "xhdpi": 96, "xxhdpi": 144, "xxxhdpi": 192}


def masked(image, shape):
    scale = 4
    mask = Image.new("L", (image.width * scale, image.height * scale), 0)
    box = (0, 0, mask.width - 1, mask.height - 1)
    if shape == "circle":
        ImageDraw.Draw(mask).ellipse(box, fill=255)
    else:
        ImageDraw.Draw(mask).rounded_rectangle(box, mask.width * shape, fill=255)
    mask = mask.resize(image.size, Image.LANCZOS)
    result = Image.new("RGBA", image.size, (0, 0, 0, 0))
    result.paste(image, (0, 0), mask)
    return result


def android():
    save_text(ANDROID_RES / "drawable" / "ic_launcher_foreground.xml", vector_drawable(mono=False))
    save_text(ANDROID_RES / "drawable" / "ic_launcher_monochrome.xml", vector_drawable(mono=True))
    save_text(ANDROID_RES / "values" / "ic_launcher_background.xml", f"""<?xml version="1.0" encoding="utf-8"?>
<!-- Сгенерировано scripts/generate_icons.py — правьте скрипт, а не файл. -->
<resources>
  <color name="ic_launcher_background">{TILE}</color>
</resources>
""")
    for name in ("ic_launcher", "ic_launcher_round"):
        save_text(ANDROID_RES / "mipmap-anydpi-v26" / f"{name}.xml", ADAPTIVE_ICON)

    # Растровые иконки для Android 6–7.1 (до адаптивных): скруглённый квадрат и круг.
    for density, size in ANDROID_DENSITIES.items():
        tile = render(size)
        save_png(masked(tile, 0.18), ANDROID_RES / f"mipmap-{density}" / "ic_launcher.png")
        save_png(masked(tile, "circle"), ANDROID_RES / f"mipmap-{density}" / "ic_launcher_round.png")


def ios():
    save_text(IOS_ASSETS / "Contents.json", '{\n  "info" : { "author" : "xcode", "version" : 1 }\n}\n')

    # Одна иконка 1024×1024 без альфы — остальные размеры actool выводит сам (Xcode 14+).
    save_png(render(1024), IOS_ASSETS / "AppIcon.appiconset" / "AppIcon-1024.png", opaque=True)
    save_text(IOS_ASSETS / "AppIcon.appiconset" / "Contents.json", """{
  "images" : [
    { "filename" : "AppIcon-1024.png", "idiom" : "universal", "platform" : "ios", "size" : "1024x1024" }
  ],
  "info" : { "author" : "xcode", "version" : 1 }
}
""")

    # Знак сплэша — скруглённая плитка 120 pt по центру LaunchScreen.
    images = []
    for scale in (1, 2, 3):
        name = f"LaunchLogo@{scale}x.png"
        save_png(masked(render(120 * scale), 0.22), IOS_ASSETS / "LaunchLogo.imageset" / name)
        images.append(f'    {{ "filename" : "{name}", "idiom" : "universal", "scale" : "{scale}x" }}')
    save_text(IOS_ASSETS / "LaunchLogo.imageset" / "Contents.json",
              '{\n  "images" : [\n' + ",\n".join(images) + '\n  ],\n'
              '  "info" : { "author" : "xcode", "version" : 1 }\n}\n')

    def color(value):
        r, g, b = hex_rgb(value)
        return (f'"color" : {{ "color-space" : "srgb", "components" : {{ "alpha" : "1.000", '
                f'"red" : "0x{r:02X}", "green" : "0x{g:02X}", "blue" : "0x{b:02X}" }} }}')

    save_text(IOS_ASSETS / "LaunchBackground.colorset" / "Contents.json", f"""{{
  "colors" : [
    {{ {color(SPLASH_LIGHT)}, "idiom" : "universal" }},
    {{
      "appearances" : [ {{ "appearance" : "luminosity", "value" : "dark" }} ],
      {color(SPLASH_DARK)}, "idiom" : "universal"
    }}
  ],
  "info" : {{ "author" : "xcode", "version" : 1 }}
}}
""")


def store():
    # Google Play: 512×512, 32-bit PNG без прозрачности; маску скругления Play накладывает сам.
    # supply берёт графику из папки каждого языка листинга.
    for language in PLAY_LANGUAGES:
        save_png(render(512), PLAY_METADATA / language / "images" / "icon.png", opaque=True)


def desktop():
    # Windows/Linux: скруглённая плитка во весь кадр. .ico — иконка окна (Avalonia-ресурс) и exe,
    # её же и png берёт vpk для установщика и AppImage.
    sizes = [16, 24, 32, 48, 64, 128, 256]
    tile = masked(render(1024), 0.18)
    APP_ASSETS.mkdir(parents=True, exist_ok=True)
    tile.save(APP_ASSETS / "netbench.ico", sizes=[(s, s) for s in sizes])
    print("wrote", (APP_ASSETS / "netbench.ico").relative_to(ROOT))
    save_png(tile.resize((512, 512), Image.LANCZOS), DESKTOP_PACKAGING / "netbench.png")

    # macOS: сетка Big Sur — плитка 824 в кадре 1024 со скруглением ~22.5% и прозрачными полями.
    mac = Image.new("RGBA", (1024, 1024), (0, 0, 0, 0))
    mac.paste(masked(render(824), 0.225), (100, 100))
    DESKTOP_PACKAGING.mkdir(parents=True, exist_ok=True)
    mac.save(DESKTOP_PACKAGING / "netbench.icns")
    print("wrote", (DESKTOP_PACKAGING / "netbench.icns").relative_to(ROOT))


if __name__ == "__main__":
    android()
    ios()
    store()
    desktop()
