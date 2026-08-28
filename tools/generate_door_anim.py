#!/usr/bin/env python3
"""Generate the custom KAnim art for the Anywhere Door.

The binary layout follows the small single-frame KAnim files used by other
local mods in this workspace.  It is intentionally simple: one symbol, one
frame, several named animation states that all show the same portal frame.
"""
from __future__ import annotations

import math
import random
import struct
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "anim" / "assets" / "oni_anywhere_door"
NAME = "oni_anywhere_door"
UI_HASH = 7675188  # Hash.SDBMLower("ui")
BOUNDS = (0.0, -100.0, 200.0, 400.0)

ANIMS = (
    "idle",
    "off",
    "place",
    "ui",
    "working_pre",
    "working_loop",
    "working_pst",
)


def kstring(s: str) -> bytes:
    b = s.encode("utf-8")
    return struct.pack("<i", len(b)) + b


def write_kanim(image: Image.Image) -> None:
    ASSETS.mkdir(parents=True, exist_ok=True)
    image.save(ASSETS / f"{NAME}_0.png")

    build = bytearray()
    build += b"BILD"
    build += struct.pack("<i", 10)
    build += struct.pack("<i", 1)  # symbols
    build += struct.pack("<i", 0)
    build += kstring("ui")
    build += struct.pack("<i", UI_HASH)
    build += struct.pack("<i", UI_HASH)
    build += struct.pack("<i", 0)  # channels
    build += struct.pack("<i", 0)  # flags
    build += struct.pack("<i", 1)  # frames
    build += struct.pack("<i", 0)  # sequence
    build += struct.pack("<i", 1)  # duration
    build += struct.pack("<i", 0)  # texture image
    build += struct.pack("<4f", *BOUNDS)
    build += struct.pack("<4f", 0.0, 0.0, 1.0, 1.0)
    build += struct.pack("<i", 1)
    build += struct.pack("<i", UI_HASH)
    build += kstring("ui")
    (ASSETS / f"{NAME}_build.bytes").write_bytes(build)

    anim = bytearray()
    anim += b"ANIM"
    anim += struct.pack("<i", 5)
    anim += struct.pack("<i", 0)
    anim += struct.pack("<i", 0)
    anim += struct.pack("<i", len(ANIMS))
    for anim_name in ANIMS:
        anim += kstring(anim_name)
        anim += struct.pack("<i", UI_HASH)
        anim += struct.pack("<f", 1.0)
        anim += struct.pack("<i", 1)
        anim += struct.pack("<4f", *BOUNDS)
        anim += struct.pack("<i", 1)
        anim += struct.pack("<i", UI_HASH)
        anim += struct.pack("<i", 0)
        anim += struct.pack("<i", UI_HASH)
        anim += struct.pack("<i", 0)
        anim += struct.pack("<4f", 1.0, 1.0, 1.0, 1.0)
        anim += struct.pack("<6f", 1.0, 0.0, 0.0, 1.0, 0.0, 0.0)
        anim += struct.pack("<f", 0.0)
    anim += struct.pack("<i", 0)
    anim += struct.pack("<i", 1)
    anim += struct.pack("<i", UI_HASH)
    anim += kstring("ui")
    (ASSETS / f"{NAME}_anim.bytes").write_bytes(anim)


def draw_portal() -> Image.Image:
    W, H = 1024, 2048
    image = Image.new("RGBA", (W, H), (0, 0, 0, 0))

    teal = (86, 244, 232)
    teal_soft = (72, 190, 210)
    gold = (242, 205, 120)
    ink = (15, 21, 34)

    # --- soft outer glow -------------------------------------------------
    glow = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    gd.rounded_rectangle([100, 70, W - 100, H - 90], radius=220,
                         fill=(*teal, 70))
    gd.ellipse([W // 2 - 360, 280, W // 2 + 360, H - 360],
               fill=(*teal_soft, 90))
    glow = glow.filter(ImageFilter.GaussianBlur(42))
    image = Image.alpha_composite(image, glow)
    d = ImageDraw.Draw(image)

    # --- stone frame -----------------------------------------------------
    d.rounded_rectangle([105, 55, W - 105, H - 55], radius=225,
                        fill=(29, 37, 53, 255), outline=(*teal, 245), width=11)
    d.rounded_rectangle([135, 85, W - 135, H - 85], radius=195,
                        outline=(*gold, 160), width=4)
    d.rounded_rectangle([158, 108, W - 158, H - 108], radius=175,
                        outline=(*teal_soft, 80), width=3)

    # --- top/bottom metal caps ------------------------------------------
    d.rounded_rectangle([135, 105, W - 135, 235], radius=52,
                        fill=(43, 57, 78, 255), outline=(*teal, 170), width=4)
    d.rounded_rectangle([135, H - 235, W - 135, H - 105], radius=52,
                        fill=(43, 57, 78, 255), outline=(*teal, 170), width=4)

    # network glyph on the top cap: three connected nodes
    cx = W // 2
    for dx, dy in ((0, -38), (-78, 24), (78, 24)):
        d.line([(cx, 170), (cx + dx, 170 + dy)], fill=(*teal_soft, 150), width=5)
    for dx, dy in ((0, -38), (-78, 24), (78, 24)):
        d.ellipse([cx + dx - 14, 170 + dy - 14, cx + dx + 14, 170 + dy + 14],
                  fill=(*teal, 235), outline=(235, 255, 255, 220), width=3)

    # --- inner portal recess --------------------------------------------
    portal = [205, 300, W - 205, H - 300]
    d.rounded_rectangle(portal, radius=190, fill=ink, outline=(220, 250, 255, 220), width=9)
    d.rounded_rectangle([230, 325, W - 230, H - 325], radius=170,
                        outline=(*teal, 120), width=4)

    # deep-space gradient inside the portal
    inner = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    idraw = ImageDraw.Draw(inner)
    px0, py0, px1, py1 = 238, 340, W - 238, H - 340
    for i in range(80):
        t = i / 79.0
        r = int(10 + 10 * t)
        g = int(18 + 28 * t)
        b = int(40 + 55 * t)
        margin = int(4 + 10 * (1.0 - t))
        idraw.ellipse([px0 + margin, py0 + margin, px1 - margin, py1 - margin],
                      fill=(r, g, b, 235))
    mask = Image.new("L", (W, H), 0)
    md = ImageDraw.Draw(mask)
    md.rounded_rectangle([238, 340, W - 238, H - 340], radius=160, fill=255)
    inner.putalpha(Image.composite(inner.getchannel("A"), Image.new("L", (W, H), 0), mask))
    image = Image.alpha_composite(image, inner)
    d = ImageDraw.Draw(image)

    # --- star field ------------------------------------------------------
    rng = random.Random(20261104)
    for _ in range(190):
        x = rng.randint(270, W - 270)
        y = rng.randint(375, H - 375)
        # reject corners outside the rounded portal
        dx = max(abs(x - W // 2) - (W // 2 - 300), 0)
        dy = max(abs(y - H // 2) - (H // 2 - 300), 0)
        if dx * dx + dy * dy > 150 * 150:
            continue
        rr = rng.choice((1, 1, 1, 2, 2, 3))
        alpha = rng.randint(90, 235)
        tint = rng.choice(((220, 250, 255), (155, 225, 255), (240, 255, 250)))
        d.ellipse([x - rr, y - rr, x + rr, y + rr], fill=(*tint, alpha))

    # --- slow spiral galaxy ---------------------------------------------
    ccx, ccy = W // 2, H // 2 + 5
    for arm in range(3):
        phase = arm * (2.0 * math.pi / 3.0)
        points = []
        for step in range(120):
            t = step / 119.0
            angle = phase + t * 2.7 * math.pi
            radius = 55 + t * 255
            wobble = 10 * math.sin(t * 11.0 + arm)
            points.append((ccx + math.cos(angle) * (radius + wobble),
                           ccy + math.sin(angle) * (radius * 0.78 + wobble)))
        for i in range(len(points) - 1):
            t = i / (len(points) - 1)
            colour = (
                int(90 + 120 * t),
                int(210 + 35 * t),
                int(235 + 15 * t),
                int(40 + 135 * t),
            )
            d.line([points[i], points[i + 1]], fill=colour, width=3 if t < 0.6 else 2)

    # bright core
    for radius, colour in ((85, (180, 250, 255, 90)),
                           (55, (220, 255, 255, 160)),
                           (26, (255, 255, 255, 235))):
        d.ellipse([ccx - radius, ccy - radius, ccx + radius, ccy + radius],
                  fill=colour)

    # --- inner rim glow --------------------------------------------------
    rim = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    rd = ImageDraw.Draw(rim)
    rd.rounded_rectangle(portal, radius=190, outline=(*teal, 210), width=12)
    rim = rim.filter(ImageFilter.GaussianBlur(14))
    image = Image.alpha_composite(image, rim)
    d = ImageDraw.Draw(image)

    # --- corner plates / rivets -----------------------------------------
    for x, y in ((172, 290), (W - 172, 290), (172, H - 290), (W - 172, H - 290)):
        d.ellipse([x - 21, y - 21, x + 21, y + 21],
                  fill=(60, 78, 102, 255), outline=(*gold, 210), width=3)

    # two small vertical status lights on each side
    for i, y in enumerate((760, 930, 1100, 1270)):
        alpha = 255 if i % 2 == 0 else 170
        d.rounded_rectangle([150, y, 182, y + 54], radius=14,
                            fill=(*teal, alpha), outline=(230, 255, 255, 180), width=2)
        d.rounded_rectangle([W - 182, y, W - 150, y + 54], radius=14,
                            fill=(*teal, alpha), outline=(230, 255, 255, 180), width=2)

    # bottom nameplate
    d.rounded_rectangle([W // 2 - 190, H - 170, W // 2 + 190, H - 118],
                        radius=24, fill=(20, 28, 42, 240), outline=(*gold, 170), width=3)
    d.line([(W // 2 - 120, H - 144), (W // 2 + 120, H - 144)],
           fill=(*teal, 160), width=3)

    return image.resize((256, 512), Image.LANCZOS)


def main() -> None:
    write_kanim(draw_portal())
    print(f"wrote {ASSETS}")


if __name__ == "__main__":
    main()
