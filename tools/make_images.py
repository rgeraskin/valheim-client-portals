# /// script
# requires-python = ">=3.11"
# dependencies = ["pillow>=11"]
# ///
"""Makes the mod page images from in-game screenshots (tools/DevHarness, showcase world):
package/icon.png (256, Thunderstore) and package/logo.png (1024): the map view with its portal badges, the list
window in front, a portal's fire around the edges; package/nexus-header.jpg (1300x372, Nexus' size): the map view behind the
title, the list window on the right; package/gallery/*.jpg.
The title is set in Cinzel (OFL), like Winter Sled's.

    uv run tools/make_images.py <shots dir> <Cinzel.ttf>
"""
import sys
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont

TITLE = "CLIENT PORTALS"
TAGLINE = "Pick from a list or the map  ·  client-side  ·  gamepad  ·  35 languages"
GOLD = (255, 214, 140)
GLOW = (255, 120, 30)


def font(path: Path, size: int, weight: str = "Bold") -> ImageFont.FreeTypeFont:
    f = ImageFont.truetype(str(path), size)
    try:
        f.set_variation_by_name(weight)
    except (OSError, ValueError):
        pass
    return f


def glow_text(img: Image.Image, xy, text: str, f: ImageFont.FreeTypeFont, anchor="mm", glow_radius=8, stroke=2):
    """Gold letters with a dark outline and a warm glow behind them, centered on xy."""
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    d.text(xy, text, font=f, anchor=anchor, fill=GLOW + (255,), stroke_width=stroke + 2, stroke_fill=GLOW + (255,))
    glow = layer.filter(ImageFilter.GaussianBlur(glow_radius))
    img.alpha_composite(glow)
    img.alpha_composite(glow)
    d = ImageDraw.Draw(img)
    d.text(xy, text, font=f, anchor=anchor, fill=GOLD + (255,), stroke_width=stroke, stroke_fill=(40, 18, 6, 255))


def bottom_shade(img: Image.Image, start: float, strength: int = 210):
    """Darkens the bottom of the picture, under the title."""
    w, h = img.size
    shade = Image.new("L", (1, h), 0)
    y0 = int(h * start)
    for y in range(y0, h):
        shade.putpixel((0, y), int(strength * (y - y0) / max(1, h - y0)))
    black = Image.new("RGBA", img.size, (0, 0, 0, 255))
    img.paste(black, (0, 0), shade.resize(img.size))


def square_logo(hero: Image.Image, size: int, cinzel: Path) -> Image.Image:
    # the portal fills the square; the title sits over a darkened bottom band
    w, h = hero.size
    side = min(h, int(w * 0.56))
    cx, cy = w // 2, int(h * 0.53)
    left = max(0, min(w - side, cx - side // 2))
    top = max(0, min(h - side, cy - side // 2))
    img = hero.crop((left, top, left + side, top + side)).resize((size, size), Image.LANCZOS).convert("RGBA")
    bottom_shade(img, 0.62)
    title_font = font(cinzel, int(size * 0.112))
    glow_text(img, (size / 2, size * 0.885), TITLE, title_font, glow_radius=max(2, size // 90), stroke=max(1, size // 160))
    return img


def map_logo(map_shot: Image.Image, list_path: Path, portal_shot: Image.Image, size: int, cinzel: Path) -> Image.Image:
    """The map view (badges, names, the hovered portal's label) with the list window in front, framed by the portal's
    fire like a vignette, the title below."""
    # the map around the crosshair, below the hint bar
    cx, cy, side = 880, 640, 600
    img = map_shot.crop((cx - side // 2, cy - side // 2, cx + side // 2, cy + side // 2)).resize((size, size), Image.LANCZOS).convert("RGBA")
    img.alpha_composite(Image.new("RGBA", img.size, (12, 8, 4, 60)))
    # the list window against the left edge (the map keeps the rest), cut from the full-size screenshot and put on
    # top of everything but the title, so it stays sharp
    panel = shot_crop(list_path, (653, 153, 1267, 927))
    ph = int(size * 0.58)
    panel = panel.resize((ph * panel.width // panel.height, ph), Image.LANCZOS).filter(ImageFilter.UnsharpMask(1, 60, 2)).convert("RGBA")
    x, y = 0, int(size * 0.17)
    # the border: the portal's ring of fire, fitted to the square, shows only toward the edges (bright parts glow,
    # dark parts let the picture through), over a dark vignette in the corners
    w, h = portal_shot.size
    # below the portal's sign: its letters would show as a dark hole in the glow
    fs, fy = int(h * 0.82), int(h * 0.59)
    fire = portal_shot.crop((w // 2 - fs // 2, fy - fs // 2, w // 2 + fs // 2, fy + fs // 2)).resize((size, size), Image.LANCZOS)
    edge = Image.new("L", (size, size))
    dark = Image.new("L", (size, size))
    c = (size - 1) / 2
    for yy in range(size):
        for xx in range(size):
            r = max(abs(xx - c), abs(yy - c)) / c * 0.6 + ((xx - c) ** 2 + (yy - c) ** 2) ** 0.5 / c * 0.4
            edge.putpixel((xx, yy), int(255 * min(1, max(0, (r - 0.62) / 0.33))))
            dark.putpixel((xx, yy), int(190 * min(1, max(0, (r - 0.7) / 0.45))))
    img.paste(Image.new("RGBA", img.size, (8, 3, 0, 255)), (0, 0), dark)
    light = fire.convert("L").point(lambda v: min(255, max(0, (v - 40) * 2)))
    glow = Image.composite(fire, Image.new("RGB", fire.size), ImageChops.multiply(light, edge))
    img = ImageChops.screen(img.convert("RGB"), glow).convert("RGBA")
    bottom_shade(img, 0.68, 190)
    shadow = Image.new("L", img.size, 0)
    ImageDraw.Draw(shadow).rectangle((x, y + size * 0.02, x + panel.width + size * 0.015, y + ph + size * 0.02), fill=200)
    img.paste(Image.new("RGBA", img.size, (0, 0, 0, 255)), (0, 0), shadow.filter(ImageFilter.GaussianBlur(size * 0.02)))
    img.alpha_composite(panel, (x, y))
    title_font = font(cinzel, int(size * 0.112))
    glow_text(img, (size / 2, size * 0.885), TITLE, title_font, glow_radius=max(2, size // 90), stroke=max(1, size // 160))
    return img


def header(map_path: Path, list_path: Path, cinzel: Path) -> Image.Image:
    """The map view with its portals as the background, darker behind the title on the left; the list window on the
    right. 1300x372, the size Nexus recommends (other sizes are cropped)."""
    W, H = 1300, 372
    k = H / 480
    # a band of the map below the hint bar, inside the map's frame (left of its pin buttons)
    bg = shot_crop(map_path, (130, 380, 1700, 829)).resize((W, H), Image.LANCZOS).convert("RGBA")
    bg.alpha_composite(Image.new("RGBA", (W, H), (6, 3, 0, 90)))
    # the list window, full resolution, on a soft shadow
    panel = shot_crop(list_path, (653, 153, 1267, 927))
    m = round(20 * k)
    ph = H - 2 * m
    panel = panel.resize((ph * panel.width // panel.height, ph), Image.LANCZOS).filter(ImageFilter.UnsharpMask(1, 60, 2)).convert("RGBA")
    x, y = W - panel.width - round(60 * k), m
    shadow = Image.new("L", (W, H), 0)
    ImageDraw.Draw(shadow).rectangle((x - 8, y + 5, x + panel.width + 11, y + ph + 9), fill=220)
    bg.paste(Image.new("RGBA", (W, H), (0, 0, 0, 255)), (0, 0), shadow.filter(ImageFilter.GaussianBlur(12)))
    bg.alpha_composite(panel, (x, y))
    cx = x // 2
    # title and tagline in the upper half: Nexus lays its breadcrumb, mod name and buttons over the lower ~40%
    ty, gy = round(135 * k), round(215 * k)
    # a soft dark patch behind the title and the tagline: the portals around it stay in view
    plate = Image.new("L", (W, H), 0)
    ImageDraw.Draw(plate).rounded_rectangle((cx - 440, ty - 75 * k, cx + 440, gy + 45 * k), radius=round(110 * k), fill=175)
    bg.paste(Image.new("RGBA", (W, H), (6, 3, 0, 255)), (0, 0), plate.filter(ImageFilter.GaussianBlur(35)))
    glow_text(bg, (cx, ty), TITLE, font(cinzel, 80), glow_radius=8, stroke=2)
    d = ImageDraw.Draw(bg)
    d.text((cx, gy), TAGLINE, font=font(cinzel, 21, "Regular"), anchor="mm", fill=(235, 220, 195, 255),
           stroke_width=2, stroke_fill=(20, 10, 4, 255))
    return bg


def shot(path: Path) -> Image.Image:
    """A screenshot at 1920x1080: a window of another shape (a Mac's, 2880x1558) is cut to 16:9 around its center."""
    img = Image.open(path).convert("RGB")
    w, h = img.size
    cw, ch = min(w, h * 16 // 9), min(h, w * 9 // 16)
    img = img.crop(((w - cw) // 2, (h - ch) // 2, (w - cw) // 2 + cw, (h - ch) // 2 + ch))
    return img if img.size == (1920, 1080) else img.resize((1920, 1080), Image.LANCZOS)


def shot_crop(path: Path, box) -> Image.Image:
    """A box given in the 1920x1080 picture of shot(), cut from the screenshot at its own, full resolution."""
    img = Image.open(path).convert("RGB")
    w, h = img.size
    cw, ch = min(w, h * 16 // 9), min(h, w * 9 // 16)
    k = cw / 1920
    left, top = (w - cw) / 2, (h - ch) / 2
    return img.crop(tuple(int(round(v * k + (left if i % 2 == 0 else top))) for i, v in enumerate(box)))


def main(shots: Path, cinzel: Path, out: Path):
    hero = shot(shots / "p_hero_front.png")
    room = shot(shots / "p_room_dusk.png")
    map_shot = shot(shots / "p_map_gamepad.png")
    out.mkdir(parents=True, exist_ok=True)
    logo = map_logo(map_shot, shots / "p_list_gamepad.png", hero, 1024, cinzel).convert("RGB")
    logo.save(out / "logo.png")
    logo.resize((256, 256), Image.LANCZOS).save(out / "icon.png")
    header(shots / "p_map_gamepad.png", shots / "p_list_gamepad.png", cinzel).convert("RGB").save(out / "nexus-header.jpg", quality=90)
    gallery = out / "gallery"
    gallery.mkdir(exist_ok=True)
    for old in gallery.glob("*.jpg"):
        old.unlink()
    for name, source in [
        ("1-list-gamepad", "p_list_gamepad"),
        ("2-map-view", "p_map_keyboard"),
        ("3-portal-names", "p_room_dusk"),
        ("4-list-keyboard", "p_list_keyboard"),
        ("5-map-search", "p_map_search"),
        ("6-hidden-portals", "p_hidden_tab"),
        ("7-rename-both-ends", "p_rename"),
        ("8-japanese", "p_list_japanese"),
        ("9-map-gamepad", "p_map_gamepad"),
    ]:
        src = shots / f"{source}.png"
        if src.exists():
            shot(src).save(gallery / f"{name}.jpg", quality=88)
    print("images in", out)


if __name__ == "__main__":
    main(Path(sys.argv[1]), Path(sys.argv[2]), Path(sys.argv[3]) if len(sys.argv) > 3 else Path("package"))
