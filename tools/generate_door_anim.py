#!/usr/bin/env python3
"""Generate the custom KAnim art for the Anywhere Door.

The art is painted procedurally at 4x resolution and downsampled, so the final
sprite keeps crisp metal edges while still carrying fine surface grain.  The
design language is deliberately "physical": a basalt plinth, brushed brass
inlay, polished steel caps, and a deep-space portal framed by a lit metal lip.
"""
from __future__ import annotations

import math
import struct
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "anim" / "assets" / "oni_anywhere_door"
NAME = "oni_anywhere_door"
UI_HASH = 7675188  # Hash.SDBMLower("ui")
DOOR_HASH = 1673617230  # Hash.SDBMLower("door")
UI_ART_SCALE = 0.72
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

# --------------------------------------------------------------------------
# render setup
# --------------------------------------------------------------------------

SS = 4
OUT_W, OUT_H = 256, 512
W, H = OUT_W * SS, OUT_H * SS

# Outer silhouette (native 4x pixels).
X0, X1 = 20, 1004
Y0, Y1 = 12, 2034
R_OUT = 32

# Portal arch.
PX0, PX1 = 244, 780
PTOP, PBOT = 340, 1742

# Palette (0..1 floats, gamma-encoded sRGB).
INK = np.array([0.040, 0.050, 0.080], np.float32)
TEAL = np.array([0.235, 0.960, 0.930], np.float32)
TEAL_DEEP = np.array([0.055, 0.360, 0.390], np.float32)
GOLD = np.array([1.000, 0.855, 0.565], np.float32)
COPPER = np.array([0.900, 0.560, 0.340], np.float32)


# --------------------------------------------------------------------------
# small helpers
# --------------------------------------------------------------------------

def _mask(draw_fn) -> np.ndarray:
    im = Image.new("L", (W, H), 0)
    draw_fn(ImageDraw.Draw(im))
    return np.asarray(im, np.float32) / 255.0


def rect(box) -> np.ndarray:
    return _mask(lambda d: d.rectangle(box, fill=255))


def rrect(box, radius) -> np.ndarray:
    return _mask(lambda d: d.rounded_rectangle(box, radius=radius, fill=255))


def arch(grow: float = 0.0) -> np.ndarray:
    x0, x1 = PX0 - grow, PX1 + grow
    top, bot = PTOP - grow, PBOT + grow
    r = (x1 - x0) * 0.5
    im = Image.new("L", (W, H), 0)
    d = ImageDraw.Draw(im)
    d.ellipse([x0, top, x1, top + 2.0 * r], fill=255)
    d.rounded_rectangle([x0, top + r - 1.0, x1, bot],
                        radius=max(2.0, 26.0 + grow), fill=255)
    return np.asarray(im, np.float32) / 255.0


def blur_mask(mask: np.ndarray, radius: float) -> np.ndarray:
    if radius <= 0:
        return mask
    im = Image.fromarray((np.clip(mask, 0, 1) * 255).astype(np.uint8))
    return np.asarray(im.filter(ImageFilter.GaussianBlur(radius)),
                      np.float32) / 255.0


def band(outer: np.ndarray, inner: np.ndarray) -> np.ndarray:
    return np.clip(outer - inner, 0.0, 1.0)


def union(*masks: np.ndarray) -> np.ndarray:
    out = masks[0].copy()
    for m in masks[1:]:
        np.maximum(out, m, out=out)
    return out


def to_img(rgb: np.ndarray, alpha: np.ndarray) -> Image.Image:
    arr = np.empty((H, W, 4), np.uint8)
    arr[..., :3] = (np.clip(rgb, 0, 1) * 255.0).astype(np.uint8)
    arr[..., 3] = (np.clip(alpha, 0, 1) * 255.0).astype(np.uint8)
    return Image.fromarray(arr)


def lerp3(t: np.ndarray, c0, c1, c2) -> np.ndarray:
    t = np.clip(t, 0.0, 1.0)[..., None]
    a = np.clip(t / 0.55, 0.0, 1.0)
    b = np.clip((t - 0.55) / 0.45, 0.0, 1.0)
    return np.where(t < 0.55, c0 + (c1 - c0) * a, c1 + (c2 - c1) * b)


# --------------------------------------------------------------------------
# procedural texture fields
# --------------------------------------------------------------------------

def _noise(h: int, w: int, cells: int, rng) -> np.ndarray:
    ch = max(2, int(cells * h / float(max(h, w))))
    cw = max(2, int(cells))
    small = rng.random((ch, cw)).astype(np.float32) * 255.0
    im = Image.fromarray(small.astype(np.uint8)).resize((w, h), Image.BICUBIC)
    return np.asarray(im, np.float32) / 255.0


def fbm(h: int, w: int, rng, base: int = 4, octaves: int = 5) -> np.ndarray:
    total = np.zeros((h, w), np.float32)
    amp = 1.0
    norm = 0.0
    for i in range(octaves):
        total += amp * _noise(h, w, base * (2 ** i), rng)
        norm += amp
        amp *= 0.5
    return total / norm


def brushed(rng, lines: float = 0.45) -> np.ndarray:
    """Anisotropic streaks, read as brushed metal at small sizes."""
    cols = rng.random((H, max(2, W // 28))).astype(np.float32) * 255.0
    im = Image.fromarray(cols.astype(np.uint8)).resize((W, H), Image.BICUBIC)
    streaks = np.asarray(im, np.float32) / 255.0
    rows = np.repeat(rng.random((H, 1)).astype(np.float32), W, axis=1)
    rows = np.asarray(
        Image.fromarray((rows * 255).astype(np.uint8)).filter(
            ImageFilter.GaussianBlur(0.8)),
        np.float32) / 255.0
    return (1.0 - lines) * streaks + lines * rows


def stone_rgb(rng) -> np.ndarray:
    n = fbm(H, W, rng, base=5, octaves=5)
    fine = fbm(H, W, rng, base=64, octaves=4)
    speck = fbm(H, W, rng, base=240, octaves=2)
    t = np.clip(0.36 * n + 0.64 * fine, 0, 1)
    c0 = np.array([0.052, 0.058, 0.078], np.float32)
    c1 = np.array([0.088, 0.097, 0.124], np.float32)
    c2 = np.array([0.142, 0.152, 0.186], np.float32)
    rgb = lerp3(t, c0, c1, c2)
    # fine mineral specks and micro pores (kept small so the stone reads dense)
    specks = np.clip((speck - 0.74) * 8.0, 0, 1)
    rgb += specks[..., None] * np.array([0.10, 0.11, 0.13], np.float32)
    pores = np.clip((0.34 - speck) * 9.0, 0, 1)
    rgb -= pores[..., None] * 0.040
    veins = np.clip((fine - 0.58) * 3.2, 0, 1)
    rgb += veins[..., None] * np.array([0.018, 0.021, 0.026], np.float32)
    return np.clip(rgb, 0, 1)


def steel_rgb(rng) -> np.ndarray:
    br = brushed(rng, 0.55)
    n = fbm(H, W, rng, base=9, octaves=4)
    t = np.clip(0.62 * br + 0.38 * n, 0, 1)
    c0 = np.array([0.050, 0.062, 0.088], np.float32)
    c1 = np.array([0.185, 0.215, 0.268], np.float32)
    c2 = np.array([0.420, 0.460, 0.520], np.float32)
    return lerp3(t, c0, c1, c2)


def brass_rgb(rng, bright: bool = False) -> np.ndarray:
    br = brushed(rng, 0.30)
    n = fbm(H, W, rng, base=10, octaves=4)
    t = np.clip(0.58 * br + 0.42 * n, 0, 1)
    if bright:
        c0 = np.array([0.400, 0.255, 0.105], np.float32)
        c1 = np.array([0.885, 0.690, 0.355], np.float32)
        c2 = np.array([1.000, 0.945, 0.740], np.float32)
    else:
        c0 = np.array([0.235, 0.150, 0.060], np.float32)
        c1 = np.array([0.610, 0.430, 0.185], np.float32)
        c2 = np.array([0.905, 0.755, 0.470], np.float32)
    return lerp3(t, c0, c1, c2)


# --------------------------------------------------------------------------
# lighting / material painting
# --------------------------------------------------------------------------

def paint_region(mask: np.ndarray, base_rgb: np.ndarray, *, bevel: float = 24.0,
                 ambient: float = 0.44, diffuse: float = 0.92,
                 spec: float = 0.65, spec_power: float = 24.0,
                 spec_tint=(1.0, 1.0, 1.0), light=(-0.5, -0.72),
                 vertical: float = 0.10) -> tuple[np.ndarray, np.ndarray]:
    ys, xs = np.nonzero(mask > 0.002)
    if len(ys) == 0:
        return base_rgb, mask

    pad = int(bevel * 3.0) + 8
    y0 = max(0, int(ys.min()) - pad)
    y1 = min(H, int(ys.max()) + pad + 1)
    x0 = max(0, int(xs.min()) - pad)
    x1 = min(W, int(xs.max()) + pad + 1)

    m = mask[y0:y1, x0:x1]
    height = blur_mask(m, bevel)
    gy, gx = np.gradient(height)

    k = bevel * 1.15
    nx = -gx * k
    ny = -gy * k
    nz = np.full_like(nx, 0.62)
    inv = 1.0 / np.sqrt(nx * nx + ny * ny + nz * nz)
    nx *= inv
    ny *= inv
    nz *= inv

    lx, ly = light
    lv = np.array([lx, ly, 0.68], np.float32)
    lv /= np.linalg.norm(lv)
    lam = np.clip(nx * lv[0] + ny * lv[1] + nz * lv[2], 0.0, 1.0)

    hv = np.array([lv[0], lv[1], 1.0 + lv[2]], np.float32)
    hv /= np.linalg.norm(hv)
    dot = np.clip(nx * hv[0] + ny * hv[1] + nz * hv[2], 0.0, 1.0)
    spec_v = dot ** spec_power

    rgb = base_rgb[y0:y1, x0:x1] * (ambient + diffuse * lam[..., None])
    rgb += (np.array(spec_tint, np.float32)[None, None, :]
            * spec * spec_v[..., None])

    if vertical:
        yy = np.linspace(y0 / H, y1 / H, y1 - y0, dtype=np.float32)
        rgb *= (1.0 + vertical * (0.5 - yy))[:, None, None]

    out_rgb = np.zeros((H, W, 3), np.float32)
    out_a = np.zeros((H, W), np.float32)
    out_rgb[y0:y1, x0:x1] = np.clip(rgb, 0, 1)
    out_a[y0:y1, x0:x1] = m
    return out_rgb, out_a


def comp(canvas: Image.Image, rgb: np.ndarray, alpha: np.ndarray) -> Image.Image:
    return Image.alpha_composite(canvas, to_img(rgb, alpha))


def glow(canvas: Image.Image, mask: np.ndarray, color, strength: float,
         radius: float) -> Image.Image:
    m = blur_mask(mask, radius)
    rgb = np.broadcast_to(np.array(color, np.float32), (H, W, 3))
    return comp(canvas, rgb, m * strength)


# --------------------------------------------------------------------------
# cosmic interior
# --------------------------------------------------------------------------

def _radial(xx, yy, cx, cy, rx, ry, power=2.0) -> np.ndarray:
    d = ((xx - cx) / rx) ** 2 + ((yy - cy) / ry) ** 2
    return np.exp(-np.power(d, power * 0.5))


def cosmic(portal: np.ndarray, rng) -> np.ndarray:
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)

    t = np.clip((yy - PTOP) / float(PBOT - PTOP), 0.0, 1.0)
    top = np.array([0.020, 0.026, 0.062], np.float32)
    bot = np.array([0.035, 0.075, 0.150], np.float32)
    rgb = top + (bot - top) * (t ** 0.85)[..., None]

    # central nebular pool
    pool = _radial(xx, yy, 512, 1000, 360, 520, 1.6)
    rgb += pool[..., None] * np.array([0.030, 0.130, 0.150], np.float32)

    # multi-scale nebula, generated small and upsampled for soft clouds
    h, w = H // 4, W // 4
    f1 = fbm(h, w, rng, base=4, octaves=5)
    f2 = fbm(h, w, rng, base=7, octaves=5)
    f3 = fbm(h, w, rng, base=14, octaves=4)

    def up(a):
        im = Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8))
        return np.asarray(im.resize((W, H), Image.BICUBIC),
                          np.float32) / 255.0

    f1, f2, f3 = up(f1), up(f2), up(f3)

    c1 = np.clip((f1 - 0.46) / 0.34, 0, 1) ** 1.45
    c2 = np.clip((f2 - 0.52) / 0.30, 0, 1) ** 1.8
    c3 = np.clip((f3 - 0.56) / 0.28, 0, 1) ** 2.2

    place1 = _radial(xx, yy, 330, 690, 520, 760, 1.4)
    place2 = _radial(xx, yy, 760, 1330, 430, 560, 1.5)
    place3 = _radial(xx, yy, 500, 1010, 420, 420, 1.5)

    rgb += (c1 * place1)[..., None] * np.array([0.050, 0.360, 0.400], np.float32)
    rgb += (c2 * place2)[..., None] * np.array([0.300, 0.115, 0.420], np.float32)
    rgb += (c3 * place3)[..., None] * np.array([0.110, 0.300, 0.330], np.float32)

    # faint dark dust lanes
    dust = np.clip((f3 - 0.48) / 0.30, 0, 1) * place3
    rgb -= dust[..., None] * np.array([0.030, 0.040, 0.050], np.float32)

    # depth vignette inside the arch
    edge = blur_mask(portal, 70)
    rgb *= (0.38 + 0.62 * edge)[..., None]

    # base interior as an image so stars can be painted on top
    img = to_img(np.clip(rgb, 0, 1), np.ones((H, W), np.float32))

    # ---------------- galaxy -------------------------------------------
    cx, cy = 512.0, 1005.0
    arms = 2
    r_max = 268.0
    y_scale = 0.66
    wind = 4.2 * math.pi

    def arm_point(arm: int, tt: float):
        ang = arm * math.pi + tt * wind
        r = 24.0 + r_max * (tt ** 0.88)
        wobble = 11.0 * math.sin(tt * 9.0 + arm * 2.0)
        return (cx + math.cos(ang) * (r + wobble),
                cy + math.sin(ang) * (r * y_scale + wobble * 0.5))

    glow_layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow_layer)

    # broad luminous core
    for radius, alpha, col in (
        (250, 18, (90, 240, 230)),
        (175, 30, (150, 250, 242)),
        (112, 50, (215, 250, 250)),
        (66, 78, (245, 252, 250)),
        (30, 120, (255, 255, 252)),
        (13, 190, (255, 255, 255)),
    ):
        gd.ellipse([cx - radius * 1.2, cy - radius * 0.66,
                    cx + radius * 1.2, cy + radius * 0.66],
                   fill=(*col, alpha))

    # soft dust glow along the arms
    for arm in range(arms):
        for i in range(900):
            tt = i / 899.0
            px, py = arm_point(arm, tt)
            jitter = rng.normal(0.0, 2.0 + 6.0 * tt, 2)
            px += jitter[0]
            py += jitter[1] * 0.6
            c = int(150 + 105 * (1.0 - tt))
            rr = int(4 + 7 * (1.0 - tt))
            gd.ellipse([px - rr, py - rr * 0.65, px + rr, py + rr * 0.65],
                       fill=(c, 245, 240, int(14 + 22 * (1.0 - tt))))

    glow_layer = glow_layer.filter(ImageFilter.GaussianBlur(2.6))
    img = Image.alpha_composite(img, glow_layer)

    # event-horizon rings give the portal a sense of depth
    ring_layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    rd = ImageDraw.Draw(ring_layer)
    for rx, ry, a0, a1, alpha in (
        (292, 186, 168, 372, 46),
        (258, 164, 24, 208, 38),
        (224, 142, 186, 354, 32),
    ):
        rd.arc([cx - rx, cy - ry, cx + rx, cy + ry], a0, a1,
               fill=(120, 250, 240, alpha), width=4)
    ring_layer = ring_layer.filter(ImageFilter.GaussianBlur(3.0))
    img = Image.alpha_composite(img, ring_layer)

    # crisp particles tracing the arms
    arm_layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    ad = ImageDraw.Draw(arm_layer)
    for arm in range(arms):
        for i in range(900):
            tt = i / 899.0
            px, py = arm_point(arm, tt)
            spread = 4.0 + 9.0 * tt
            px += rng.normal(0.0, spread)
            py += rng.normal(0.0, spread * 0.6)
            rr = rng.uniform(0.5, 1.7)
            if tt > 0.55:
                col = (150, 198, 255)
            else:
                col = (232, 252, 248)
            ad.ellipse([px - rr, py - rr, px + rr, py + rr],
                       fill=(*col, int(rng.uniform(55, 165))))
    img = Image.alpha_composite(img, arm_layer)

    # ---------------- star field ---------------------------------------
    star = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    sd = ImageDraw.Draw(star)
    pmask = portal

    def sample(margin=26):
        while True:
            x = rng.integers(PX0 + margin, PX1 - margin)
            y = rng.integers(PTOP + margin, PBOT - margin)
            if pmask[y, x] > 0.5:
                return float(x), float(y)

    haze = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    hd = ImageDraw.Draw(haze)
    spikes = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    kd = ImageDraw.Draw(spikes)

    for _ in range(950):
        x, y = sample()
        rr = rng.uniform(0.4, 1.35)
        tint = rng.choice((
            (225, 245, 255), (180, 225, 255), (255, 248, 232),
            (200, 255, 250),
        ))
        a = int(rng.uniform(40, 135))
        sd.ellipse([x - rr, y - rr, x + rr, y + rr], fill=(*tint, a))

    for _ in range(170):
        x, y = sample()
        rr = rng.uniform(1.3, 2.5)
        tint = rng.choice(((235, 250, 255), (190, 235, 255), (255, 250, 238)))
        a = int(rng.uniform(120, 215))
        sd.ellipse([x - rr, y - rr, x + rr, y + rr], fill=(*tint, a))
        hd.ellipse([x - rr * 2.4, y - rr * 2.4, x + rr * 2.4, y + rr * 2.4],
                   fill=(*tint, 40))

    # bright feature stars with fine diffraction spikes
    for _ in range(22):
        x, y = sample(60)
        rr = rng.uniform(2.4, 4.2)
        tint = rng.choice(((255, 255, 252), (220, 255, 255), (255, 238, 215)))
        sd.ellipse([x - rr, y - rr, x + rr, y + rr], fill=(*tint, 250))
        sd.ellipse([x - rr * 0.55, y - rr * 0.55, x + rr * 0.55, y + rr * 0.55],
                   fill=(255, 255, 255, 255))
        hd.ellipse([x - rr * 5.0, y - rr * 5.0, x + rr * 5.0, y + rr * 5.0],
                   fill=(*tint, 24))
        length = rr * rng.uniform(8, 14)
        for dx, dy, alpha in ((1, 0, 110), (0, 1, 110),
                              (0.7, 0.7, 55), (-0.7, 0.7, 55)):
            kd.line([(x - dx * length, y - dy * length),
                     (x + dx * length, y + dy * length)],
                    fill=(*tint, alpha), width=2 if alpha > 90 else 1)
        kd.line([(x - length * 0.32, y), (x + length * 0.32, y)],
                fill=(255, 255, 255, 150), width=1)
        kd.line([(x, y - length * 0.32), (x, y + length * 0.32)],
                fill=(255, 255, 255, 150), width=1)

    haze = haze.filter(ImageFilter.GaussianBlur(1.6))
    spikes = spikes.filter(ImageFilter.GaussianBlur(0.5))
    img = Image.alpha_composite(img, haze)
    img = Image.alpha_composite(img, spikes)
    img = Image.alpha_composite(img, star)

    # clip everything to the portal opening
    arr = np.asarray(img, np.float32) / 255.0
    arr[..., 3] *= portal
    arr[..., :3] *= portal[..., None]
    return arr


# --------------------------------------------------------------------------
# assembly
# --------------------------------------------------------------------------

def draw_portal() -> Image.Image:
    rng = np.random.default_rng(20261108)

    stone = stone_rgb(rng)
    steel = steel_rgb(rng)
    brass = brass_rgb(rng, bright=False)
    brass_hi = brass_rgb(rng, bright=True)

    outer = rect((X0, Y0, X1, Y1))
    portal = arch(0.0)

    canvas = Image.new("RGBA", (W, H), (0, 0, 0, 0))

    # --- ambient halo behind the door ---------------------------------
    halo = blur_mask(rrect((X0 - 10, Y0 - 8, X1 + 10, Y1 + 6), R_OUT), 70)
    canvas = comp(canvas, np.broadcast_to(
        np.array([0.10, 0.55, 0.62], np.float32), (H, W, 3)), halo * 0.20)

    # --- basalt frame --------------------------------------------------
    rgb, a = paint_region(outer, stone, bevel=30, ambient=0.40,
                          diffuse=0.95, spec=0.22, spec_power=18,
                          vertical=0.22)
    canvas = comp(canvas, rgb, a)

    # brass inlay following the silhouette
    in1 = rrect((X0 + 26, Y0 + 26, X1 - 26, Y1 - 26), R_OUT - 18)
    in2 = rrect((X0 + 46, Y0 + 46, X1 - 46, Y1 - 46), max(2.0, R_OUT - 34))
    rgb, a = paint_region(band(in1, in2), brass,
                          bevel=9, ambient=0.42, diffuse=1.0,
                          spec=1.0, spec_power=32, vertical=0.05)
    canvas = comp(canvas, rgb, a)

    # recessed shadow just inside the inlay
    lip = band(in2, rrect((X0 + 62, Y0 + 62, X1 - 62, Y1 - 62),
                          max(2.0, R_OUT - 46)))
    canvas = comp(canvas, np.broadcast_to(INK, (H, W, 3)), lip * 0.55)

    # --- top lintel ----------------------------------------------------
    lintel = rect((X0, Y0, X1, 334))
    rgb, a = paint_region(lintel, steel, bevel=20, ambient=0.38,
                          diffuse=1.0, spec=0.75, spec_power=22,
                          vertical=0.14)
    canvas = comp(canvas, rgb, a)

    # lintel trim
    trim = rrect((X0 + 2, 266, X1 - 2, 300), 15)
    rgb, a = paint_region(trim, brass, bevel=8, ambient=0.45,
                          diffuse=1.0, spec=1.0, spec_power=34, vertical=0.0)
    canvas = comp(canvas, rgb, a)

    # engraved groove above the trim
    canvas = comp(canvas, np.broadcast_to(INK, (H, W, 3)),
                  rect((X0 + 26, 244, X1 - 26, 250)) * 0.85)
    canvas = comp(canvas, np.broadcast_to(
        np.array([0.55, 0.62, 0.72], np.float32), (H, W, 3)),
        rect((X0 + 26, 251, X1 - 26, 255)) * 0.35)

    # --- bottom plinth -------------------------------------------------
    plinth = union(
        rrect((X0 - 8, 1760, X1 + 8, 2036), 46),
        rect((X0 - 8, 1760, X1 + 8, 1880)),
    )
    rgb, a = paint_region(plinth, stone, bevel=22, ambient=0.38,
                          diffuse=0.98, spec=0.30, spec_power=20,
                          vertical=0.12)
    canvas = comp(canvas, rgb, a)

    # polished stone catches a soft horizontal highlight
    stone_sheen = _mask(lambda d: d.ellipse(
        [X0 - 60, 1840, X0 + 470, 2090]))
    canvas = glow(canvas, stone_sheen * plinth, (0.55, 0.66, 0.80), 0.075, 55)

    molding = rrect((X0 - 4, 1764, X1 + 4, 1804), 17)
    rgb, a = paint_region(molding, brass, bevel=8, ambient=0.42,
                          diffuse=1.0, spec=0.95, spec_power=30, vertical=0.0)
    canvas = comp(canvas, rgb, a)

    foot = rrect((X0, 1998, X1, 2026), 12)
    rgb, a = paint_region(foot, brass, bevel=6, ambient=0.40,
                          diffuse=1.0, spec=0.8, spec_power=26, vertical=0.0)
    canvas = comp(canvas, rgb, a)

    # recessed plate set into the plinth
    plate = rrect((338, 1822, 686, 1948), 24)
    rgb, a = paint_region(plate, steel, bevel=11, ambient=0.26,
                          diffuse=0.85, spec=1.1, spec_power=40,
                          vertical=0.0)
    canvas = comp(canvas, rgb, a)
    plate_rim = band(plate, rrect((346, 1830, 678, 1940), 20))
    rgb, a = paint_region(plate_rim, brass_hi, bevel=4, ambient=0.48,
                          diffuse=1.0, spec=1.0, spec_power=34, vertical=0.0)
    canvas = comp(canvas, rgb, a)

    # soft studio sheen across the top plate
    sheen = _mask(lambda d: d.ellipse([X0 + 20, Y0 - 40, X1 - 20, 330]))
    canvas = glow(canvas, sheen * lintel, (0.62, 0.80, 0.88), 0.14, 55)

    # --- side pilasters ------------------------------------------------
    for left in (True, False):
        x0, x1 = (86, 202) if left else (822, 938)
        body = rrect((x0, 356, x1, 1712), 30)
        rgb, a = paint_region(body, steel, bevel=14, ambient=0.42,
                              diffuse=1.0, spec=0.85, spec_power=26,
                              vertical=0.10)
        canvas = comp(canvas, rgb, a)

        # fluting
        for fx in (x0 + 24, x0 + 58, x0 + 92):
            canvas = comp(canvas, np.broadcast_to(INK, (H, W, 3)),
                          rect((fx - 3, 404, fx + 3, 1660)) * 0.55)
            canvas = comp(canvas, np.broadcast_to(
                np.array([0.52, 0.58, 0.68], np.float32), (H, W, 3)),
                rect((fx + 4, 404, fx + 6, 1660)) * 0.22)

        # brass edging
        for ex in (x0 + 2, x1 - 8):
            edge = rrect((ex, 368, ex + 6, 1700), 3)
            rgb, a = paint_region(edge, brass_hi, bevel=3, ambient=0.5,
                                  diffuse=1.0, spec=1.0, spec_power=30,
                                  vertical=0.0)
            canvas = comp(canvas, rgb, a)

        # stepped column capitals / bases
        for cy0 in (340, 1664):
            upper = rrect((x0 - 9, cy0, x1 + 9, cy0 + 26), 8)
            rgb, a = paint_region(upper, brass, bevel=5, ambient=0.44,
                                  diffuse=1.0, spec=1.0, spec_power=30,
                                  vertical=0.0)
            canvas = comp(canvas, rgb, a)
            lower = rrect((x0 - 2, cy0 + 26, x1 + 2, cy0 + 48), 6)
            rgb, a = paint_region(lower, brass_hi, bevel=4, ambient=0.44,
                                  diffuse=1.0, spec=1.0, spec_power=32,
                                  vertical=0.0)
            canvas = comp(canvas, rgb, a)

    # --- portal rim ----------------------------------------------------
    rim_outer = arch(46)
    rim = band(rim_outer, arch(0))
    rgb, a = paint_region(rim, steel, bevel=18, ambient=0.36,
                          diffuse=1.0, spec=0.9, spec_power=24, vertical=0.06)
    canvas = comp(canvas, rgb, a)

    rim_brass = band(rim_outer, arch(32))
    rgb, a = paint_region(rim_brass, brass, bevel=7, ambient=0.44,
                          diffuse=1.0, spec=1.0, spec_power=34, vertical=0.0)
    canvas = comp(canvas, rgb, a)

    rim_inner = band(arch(16), arch(0))
    rgb, a = paint_region(rim_inner, brass_hi, bevel=6, ambient=0.42,
                          diffuse=1.0, spec=1.05, spec_power=36, vertical=0.0)
    canvas = comp(canvas, rgb, a)

    # dark throat just inside the metal lip
    throat = band(arch(18), arch(0))
    canvas = comp(canvas, np.broadcast_to(INK, (H, W, 3)), throat * 0.35)

    # --- cosmic interior ----------------------------------------------
    interior = cosmic(portal, rng)
    canvas = comp(canvas, interior[..., :3], interior[..., 3])

    # --- threshold -----------------------------------------------------
    sill = rrect((PX0 - 16, 1672, PX1 + 16, 1760), 18)
    rgb, a = paint_region(sill, brass, bevel=10, ambient=0.42,
                          diffuse=1.0, spec=1.0, spec_power=30, vertical=0.0)
    canvas = comp(canvas, rgb, a)
    canvas = comp(canvas, np.broadcast_to(INK, (H, W, 3)),
                  rect((PX0 + 10, 1706, PX1 - 10, 1714)) * 0.70)
    canvas = comp(canvas, np.broadcast_to(TEAL, (H, W, 3)),
                  rect((PX0 + 10, 1710, PX1 - 10, 1713)) * 0.65)

    # --- emissive portal lip ------------------------------------------
    lip_glow = band(arch(10), arch(-8))
    canvas = glow(canvas, lip_glow, TEAL, 0.55, 12)
    canvas = comp(canvas, np.broadcast_to(
        np.array([0.72, 1.0, 0.98], np.float32), (H, W, 3)),
        band(arch(4), arch(-2)) * 0.60)

    # --- network emblem on the lintel ---------------------------------
    canvas = draw_emblem(canvas)

    # --- status gems ---------------------------------------------------
    for left in (True, False):
        cx = 144 if left else 880
        for gy in (700, 960, 1220):
            canvas = draw_gem(canvas, cx, gy)

    # --- rim studs -----------------------------------------------------
    canvas = draw_studs(canvas)

    # --- plinth plate inlay -------------------------------------------
    canvas = draw_plinth_details(canvas)

    # --- outer edge / rim light ---------------------------------------
    canvas = comp(canvas, np.broadcast_to(INK, (H, W, 3)),
                  band(outer, rect((X0 + 12, Y0 + 12, X1 - 12, Y1 - 12))) * 0.75)
    rim_light = band(outer, rect((X0 + 4, Y0 + 4, X1 - 4, Y1 - 4)))
    light_grad = np.clip(0.55 - (np.linspace(0, 1, W, dtype=np.float32)[None, :]
                                 * 0.35
                                 + np.linspace(0, 1, H, dtype=np.float32)[:, None]
                                 * 0.42), 0, 1)
    canvas = comp(canvas, np.broadcast_to(
        np.array([0.66, 0.96, 0.98], np.float32), (H, W, 3)),
        rim_light * light_grad * 0.75)

    # subtle teal spill onto the frame around the portal
    spill = band(arch(92), arch(44))
    canvas = glow(canvas, spill, TEAL_DEEP, 0.35, 20)

    # --- finishing -----------------------------------------------------
    arr = np.asarray(canvas, np.float32) / 255.0
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    # gentle top-left key light on the whole object
    key = np.clip(1.0 + 0.055 * (0.5 - xx / W) + 0.075 * (0.5 - yy / H), 0.8, 1.2)
    arr[..., :3] *= key[..., None]
    # fine film grain, only on visible pixels
    grain = rng.normal(0.0, 1.0, (H, W, 1)).astype(np.float32)
    arr[..., :3] = np.clip(arr[..., :3] + grain * 0.012 * (arr[..., 3:] > 0), 0, 1)
    # fade any glow before it reaches the texture border, so the sprite never
    # shows a clipped rectangular halo in game
    border = np.minimum(np.minimum(xx, (W - 1) - xx),
                        np.minimum(yy, (H - 1) - yy))
    arr[..., 3] *= np.clip(border / 12.0, 0.0, 1.0)

    out = Image.fromarray((np.clip(arr, 0, 1) * 255).astype(np.uint8))
    out = out.resize((OUT_W, OUT_H), Image.LANCZOS)
    out = out.filter(ImageFilter.UnsharpMask(radius=1.1, percent=55, threshold=2))
    return out


# --------------------------------------------------------------------------
# vector details
# --------------------------------------------------------------------------

def draw_gem(canvas: Image.Image, cx: int, cy: int) -> Image.Image:
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    w, h = 42, 86
    # bezel shadow
    d.rounded_rectangle([cx - w // 2 + 2, cy - h // 2 + 4,
                         cx + w // 2 + 2, cy + h // 2 + 4],
                        radius=18, fill=(4, 7, 12, 120))
    d.rounded_rectangle([cx - w // 2, cy - h // 2,
                         cx + w // 2, cy + h // 2],
                        radius=18, fill=(120, 86, 38, 255))
    d.rounded_rectangle([cx - w // 2 + 3, cy - h // 2 + 3,
                         cx + w // 2 - 3, cy + h // 2 - 3],
                        radius=15, fill=(10, 16, 24, 255))
    glow_layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow_layer)
    gd.rounded_rectangle([cx - 12, cy - h // 2 + 13,
                          cx + 12, cy + h // 2 - 13],
                        radius=11, fill=(90, 240, 232, 190))
    gd.ellipse([cx - 10, cy - 34, cx + 10, cy + 34], fill=(190, 255, 250, 90))
    glow_layer = glow_layer.filter(ImageFilter.GaussianBlur(5))
    layer = Image.alpha_composite(layer, glow_layer)

    # glass capsule with a real vertical gradient instead of a flat fill
    gw, gh = 24, h - 26
    top_col = np.array([214, 255, 252], np.float32)
    bot_col = np.array([26, 146, 156], np.float32)
    grad = Image.new("RGBA", (gw, gh), (0, 0, 0, 0))
    gd2 = ImageDraw.Draw(grad)
    for yy in range(gh):
        t = (yy / max(1, gh - 1)) ** 0.85
        col = top_col + (bot_col - top_col) * t
        gd2.line([(0, yy), (gw, yy)],
                 fill=(int(col[0]), int(col[1]), int(col[2]), 255))
    gmask = Image.new("L", (gw, gh), 0)
    ImageDraw.Draw(gmask).rounded_rectangle(
        [0, 0, gw - 1, gh - 1], radius=10, fill=255)
    layer.paste(grad, (cx - gw // 2, cy - gh // 2), gmask)

    d = ImageDraw.Draw(layer)
    d.ellipse([cx - 7, cy - gh // 2 + 5, cx + 5, cy - gh // 2 + 22],
              fill=(255, 255, 255, 190))
    d.line([(cx - 8, cy - gh // 2 + 9), (cx - 8, cy + gh // 2 - 12)],
           fill=(255, 255, 255, 150), width=2)
    d.line([(cx + 7, cy - gh // 2 + 12), (cx + 7, cy + gh // 2 - 18)],
           fill=(255, 255, 255, 70), width=1)
    # bezel highlight
    d.arc([cx - w // 2 + 1, cy - h // 2 + 1,
           cx + w // 2 - 1, cy + h // 2 - 1], 195, 330,
          fill=(255, 236, 190, 200), width=3)
    return Image.alpha_composite(canvas, layer)


def draw_emblem(canvas: Image.Image) -> Image.Image:
    cx, cy = 512, 158
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)

    # small brass bolts frame the emblem
    draw_bolt(d, cx - 146, cy, 14)
    draw_bolt(d, cx + 146, cy, 14)

    nodes = ((cx, cy - 32), (cx - 82, cy + 54), (cx + 82, cy + 54))

    # etched channels: dark shadow then polished brass then highlight
    for nx, ny in nodes:
        d.line([(cx, cy), (nx, ny)], fill=(6, 9, 15, 200), width=13)
        d.line([(cx, cy), (nx, ny)], fill=(150, 112, 54, 255), width=8)
        d.line([(cx, cy - 2), (nx, ny - 2)], fill=(255, 224, 162, 210), width=2)

    glow_layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow_layer)
    for i, (nx, ny) in enumerate(nodes):
        rr = 30 if i == 0 else 23
        gd.ellipse([nx - rr * 2, ny - rr * 2, nx + rr * 2, ny + rr * 2],
                   fill=(90, 240, 232, 48))

    for i, (nx, ny) in enumerate(nodes):
        rr = 30 if i == 0 else 23
        d.ellipse([nx - rr - 7, ny - rr - 7, nx + rr + 7, ny + rr + 7],
                  fill=(4, 7, 12, 130))
        d.ellipse([nx - rr, ny - rr, nx + rr, ny + rr],
                  fill=(118, 84, 36, 255))
        d.ellipse([nx - rr + 4, ny - rr + 4, nx + rr - 4, ny + rr - 4],
                  fill=(255, 214, 140, 255))
        d.ellipse([nx - rr + 8, ny - rr + 8, nx + rr - 8, ny + rr - 8],
                  fill=(16, 24, 34, 255))
        core = rr - 12
        d.ellipse([nx - core, ny - core, nx + core, ny + core],
                  fill=(92, 244, 236, 255))
        d.ellipse([nx - core + 3, ny - core + 3, nx + core - 3, ny + core - 3],
                  fill=(226, 255, 252, 255))
        d.ellipse([nx - 4, ny - 4, nx + 4, ny + 4], fill=(255, 255, 255, 255))
        d.arc([nx - rr, ny - rr, nx + rr, ny + rr], 200, 320,
              fill=(255, 246, 220, 210), width=3)

    glow_layer = glow_layer.filter(ImageFilter.GaussianBlur(9))
    layer = Image.alpha_composite(layer, glow_layer)
    return Image.alpha_composite(canvas, layer)


def draw_bolt(d: ImageDraw.ImageDraw, cx: float, cy: float, r: float,
              alpha: int = 255) -> None:
    d.ellipse([cx - r + 3, cy - r + 5, cx + r + 3, cy + r + 5],
              fill=(4, 7, 12, 120))
    d.ellipse([cx - r, cy - r, cx + r, cy + r],
              fill=(96, 68, 30, 255))
    d.ellipse([cx - r + 2, cy - r + 2, cx + r - 2, cy + r - 2],
              fill=(226, 186, 116, 255))
    d.ellipse([cx - r + 5, cy - r + 5, cx + r - 5, cy + r - 5],
              fill=(44, 34, 22, 255))
    d.arc([cx - r, cy - r, cx + r, cy + r], 200, 330,
          fill=(255, 245, 218, 210), width=2)
    d.line([(cx - r * 0.45, cy), (cx + r * 0.45, cy)],
           fill=(20, 14, 8, 235), width=max(1, int(r * 0.28)))


def draw_plinth_details(canvas: Image.Image) -> Image.Image:
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)

    # twin bolts anchor the plate on the stone plinth
    draw_bolt(d, 248, 1886, 17)
    draw_bolt(d, 776, 1886, 17)

    # three teal pips: the network mark, mirroring the lintel emblem
    glow_layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow_layer)
    for px in (452, 512, 572):
        gd.ellipse([px - 34, 1886 - 34, px + 34, 1886 + 34],
                   fill=(80, 240, 232, 62))
    glow_layer = glow_layer.filter(ImageFilter.GaussianBlur(9))

    for px in (452, 512, 572):
        d.ellipse([px - 12, 1886 - 12, px + 12, 1886 + 12],
                  fill=(16, 24, 34, 255))
        d.ellipse([px - 8, 1886 - 8, px + 8, 1886 + 8],
                  fill=(110, 246, 238, 255))
        d.ellipse([px - 4, 1886 - 4, px + 4, 1886 + 4],
                  fill=(240, 255, 253, 255))

    # engraved hairline under the pips
    d.line([(398, 1918), (626, 1918)], fill=(6, 10, 16, 200), width=3)
    d.line([(398, 1915), (626, 1915)], fill=(255, 226, 176, 120), width=1)
    d.arc([346, 1830, 678, 1940], 200, 340,
          fill=(255, 240, 204, 90), width=2)

    layer = Image.alpha_composite(layer, glow_layer)
    return Image.alpha_composite(canvas, layer)


def draw_studs(canvas: Image.Image) -> Image.Image:
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)

    points: list[tuple[float, float]] = []
    cx, cy = (PX0 + PX1) * 0.5, PTOP + (PX1 - PX0) * 0.5
    for k in range(11):
        ang = math.pi + (k / 10.0) * math.pi
        rr = (PX1 - PX0) * 0.5 + 39
        points.append((cx + math.cos(ang) * rr, cy + math.sin(ang) * rr))
    for y in (760, 900, 1040, 1180, 1320, 1460, 1600):
        points.append((PX0 - 39, y))
        points.append((PX1 + 39, y))

    for x, y in points:
        d.ellipse([x - 8, y - 8, x + 8, y + 8], fill=(5, 8, 14, 130))
        d.ellipse([x - 7, y - 7, x + 7, y + 7], fill=(108, 76, 34, 255))
        d.ellipse([x - 5, y - 5, x + 5, y + 5], fill=(230, 190, 118, 255))
        d.ellipse([x - 5, y - 5, x + 1, y + 1], fill=(255, 244, 214, 230))
        d.ellipse([x - 2, y - 2, x + 2, y + 2], fill=(58, 42, 20, 255))
    return Image.alpha_composite(canvas, layer)


# --------------------------------------------------------------------------
# kanim writer (unchanged binary layout)
# --------------------------------------------------------------------------

def kstring(s: str) -> bytes:
    b = s.encode("utf-8")
    return struct.pack("<i", len(b)) + b


def _ui_art(image: Image.Image) -> Image.Image:
    """Return a padded copy used only for UI/build-menu icons."""
    w, h = image.size
    scaled = image.resize((max(1, int(w * UI_ART_SCALE)),
                           max(1, int(h * UI_ART_SCALE))), Image.LANCZOS)
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    out.paste(scaled, ((w - scaled.width) // 2, (h - scaled.height) // 2), scaled)
    return out


def write_kanim(image: Image.Image) -> None:
    ASSETS.mkdir(parents=True, exist_ok=True)

    # One texture, two regions: the top half is the full-size in-world art;
    # the bottom half is a padded copy used for the build-menu icon and portrait.
    ui_art = _ui_art(image)
    atlas = Image.new("RGBA", (image.width, image.height * 2), (0, 0, 0, 0))
    atlas.paste(image, (0, 0))
    atlas.paste(ui_art, (0, image.height))
    atlas.save(ASSETS / f"{NAME}_0.png")

    build = bytearray()
    build += b"BILD"
    build += struct.pack("<i", 10)
    build += struct.pack("<i", 2)  # symbols
    build += struct.pack("<i", 0)
    build += kstring("oni_anywhere_door")
    for symbol_hash, uv_y0, uv_y1 in (
        (DOOR_HASH, 0.0, 0.5),  # full-size world art (top half)
        (UI_HASH, 0.5, 1.0),    # padded UI art (bottom half)
    ):
        build += struct.pack("<i", symbol_hash)
        build += struct.pack("<i", symbol_hash)
        build += struct.pack("<i", 0)  # channels
        build += struct.pack("<i", 0)  # flags
        build += struct.pack("<i", 1)  # frames
        build += struct.pack("<i", 0)  # sequence
        build += struct.pack("<i", 1)  # duration
        build += struct.pack("<i", 0)  # texture image
        build += struct.pack("<4f", *BOUNDS)
        build += struct.pack("<4f", 0.0, uv_y0, 1.0, uv_y1)
    build += struct.pack("<i", 2)  # hash table entries
    build += struct.pack("<i", DOOR_HASH)
    build += kstring("door")
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
        # The build menu reads the "ui" symbol; in-world anims use the full art.
        symbol_hash = UI_HASH if anim_name == "ui" else DOOR_HASH
        anim += kstring(anim_name)
        anim += struct.pack("<i", symbol_hash)
        anim += struct.pack("<f", 1.0)
        anim += struct.pack("<i", 1)
        anim += struct.pack("<4f", *BOUNDS)
        anim += struct.pack("<i", 1)
        anim += struct.pack("<i", symbol_hash)
        anim += struct.pack("<i", 0)
        anim += struct.pack("<i", symbol_hash)
        anim += struct.pack("<i", 0)
        anim += struct.pack("<4f", 1.0, 1.0, 1.0, 1.0)
        anim += struct.pack("<6f", 1.0, 0.0, 0.0, 1.0, 0.0, 0.0)
        anim += struct.pack("<f", 0.0)
    anim += struct.pack("<i", 0)
    anim += struct.pack("<i", 2)
    anim += struct.pack("<i", DOOR_HASH)
    anim += kstring("door")
    anim += struct.pack("<i", UI_HASH)
    anim += kstring("ui")
    (ASSETS / f"{NAME}_anim.bytes").write_bytes(anim)


def main() -> None:
    write_kanim(draw_portal())
    print(f"wrote {ASSETS}")


if __name__ == "__main__":
    main()
