#!/usr/bin/env python3
"""Генерирует feature graphic для Google Play (1024×500).

Знак Plumix — из scripts/generate_icons.py, шрифт — Inter (OFL), который приложение уже
использует: TTF берётся из пакета Avalonia.Fonts.Inter в кэше NuGet (нужен restore проекта).

Запуск из корня репозитория: python3 scripts/generate_store_graphics.py (нужен Pillow).
"""

import io
import re
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

from generate_icons import RPS_BLUE, ROOT, TILE, hex_rgb, render

INTER_DLL = Path.home() / ".nuget/packages/avalonia.fonts.inter/12.1.4/lib/net8.0/Avalonia.Fonts.Inter.dll"
OUT = ROOT / "store" / "google-play" / "feature-graphic.png"
WIDTH, HEIGHT = 1024, 500


def inter(weight):
    """TTF из !AvaloniaResources: индекс «путь → смещение, размер», затем данные подряд."""
    data = INTER_DLL.read_bytes()
    entries = {}
    for match in re.finditer(rb"/Assets/Inter-([A-Za-z]+)\.ttf", data):
        offset = int.from_bytes(data[match.end():match.end() + 4], "little")
        size = int.from_bytes(data[match.end() + 4:match.end() + 8], "little")
        entries[match.group(1).decode()] = (offset, size, match.start())
    first = min(start for _, _, start in entries.values())
    for header in range(first - 40, first):
        base = header + 4 + int.from_bytes(data[header:header + 4], "little")
        if all(data[base + offset:base + offset + 4] == b"\x00\x01\x00\x00" for offset, _, _ in entries.values()):
            offset, size, _ = entries[weight]
            return io.BytesIO(data[base + offset:base + offset + size])
    raise RuntimeError("Inter not found in " + str(INTER_DLL))


def font(weight, size):
    return ImageFont.truetype(inter(weight), size)


def main():
    scale = 2
    image = Image.new("RGBA", (WIDTH * scale, HEIGHT * scale), (*hex_rgb(TILE), 255))

    # Спарклайн RPS по нижней кромке — тот же мотив, что график на экране прогона.
    layer = Image.new("RGBA", image.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    points = [(0, 430), (120, 410), (210, 418), (300, 380), (390, 392), (480, 350), (570, 362),
              (660, 330), (750, 338), (840, 300), (930, 312), (1024, 280)]
    points = [(x * scale, y * scale) for x, y in points]
    draw.polygon([*points, (WIDTH * scale, HEIGHT * scale), (0, HEIGHT * scale)], fill=(*hex_rgb(RPS_BLUE), 40))
    draw.line(points, fill=(*hex_rgb(RPS_BLUE), 200), width=4 * scale, joint="curve")
    image = Image.alpha_composite(image, layer)

    # Знак Plumix слева — сам логотип без плитки: фон картинки и есть плитка.
    logo = render(300 * scale, tile=False)
    image.alpha_composite(logo, (40 * scale, 70 * scale))

    draw = ImageDraw.Draw(image)
    draw.text((360 * scale, 120 * scale), "NetBench", font=font("Bold", 92 * scale), fill=(255, 255, 255, 255))
    draw.text((364 * scale, 240 * scale), "HTTP load testing for your API", font=font("Medium", 34 * scale),
              fill=(0xC9, 0xC9, 0xD4, 255))
    draw.text((364 * scale, 296 * scale), "Powered by Plumix", font=font("SemiBold", 24 * scale),
              fill=(0x9B, 0x7B, 0xE6, 255))

    OUT.parent.mkdir(parents=True, exist_ok=True)
    image.resize((WIDTH, HEIGHT), Image.LANCZOS).convert("RGB").save(OUT, optimize=True)
    print("wrote", OUT.relative_to(ROOT))


if __name__ == "__main__":
    main()
