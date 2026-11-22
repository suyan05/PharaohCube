# -*- coding: utf-8 -*-
"""
소베크 착시 퍼즐 그림 생성기 (임시 도트 그림, 아트 팀 그림이 오면 같은 이름으로 바꾸면 됨)

만드는 것 (Assets/Resources/Sobek/Illusion/  <- SobekIllusionBoard가 자동으로 읽는 폴더)
  Sobek_Illusion_Atlas.png   : 블록 무늬 256x32 (32x32 칸 8개)
                               0 돌 윗면 / 1 돌 옆면 / 2 통나무 뗏목 윗면 / 3 통나무 옆면 / 4 악어 등 윗면 / 5 악어 옆면 / 6 발판 새김 / 7 흰색
                               블록 색(정점 색)과 곱해지므로 돌은 밝은 회색, 나무·악어는 제 색으로 그림
  Sobek_Illusion_Water.png   : 물결 64x64 (이어 붙여도 이음새 없음, 흘러가게 씀)
  Sobek_Backdrop_{키}.png    : 퍼즐마다 배경 480x270 (S1~S4, M1~M3)
그리고 허브용 (Assets/image/Sobek/World/)
  Sobek_Obj_LogBlock.png     : 다리를 막는 통나무 더미 (32 PPU, 세로 2칸)
  Sobek_Fx_Fog.png           : 막힌 섬을 덮는 어둠 (9칸 자르기용, 가장자리가 부드러움)

실행: python3 Tools/SobekArt/sobek_illusion_art.py   (프로젝트 루트에서)
필요: Pillow, numpy
"""
import math
import os
import random
import sys

import numpy as np
from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(__file__))
from unity_meta import write_meta, write_folder_meta  # noqa: E402

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "Resources", "Sobek", "Illusion")
OUT_WORLD = os.path.join(ROOT, "Assets", "image", "Sobek", "World")
PREVIEW = os.environ.get("SOBEK_ART_PREVIEW")


def rgba(c, a=255):
    return (int(c[0]), int(c[1]), int(c[2]), a)


def gray(v, a=255):
    v = int(max(0, min(255, v)))
    return (v, v, v, a)


# ================= 블록 무늬 =================
def tile_stone_top(rng):
    t = Image.new("RGBA", (32, 32))
    px = t.load()
    for y in range(32):
        for x in range(32):
            px[x, y] = gray(246 + rng.integers(-7, 8))
    d = ImageDraw.Draw(t)
    # 판석 두 장 + 이음새
    d.line([(0, 15), (31, 15)], fill=gray(212))
    d.line([(11, 0), (11, 15)], fill=gray(212))
    d.line([(22, 16), (22, 31)], fill=gray(212))
    # 잔금, 패인 자국
    for _ in range(3):
        x, y = int(rng.integers(2, 30)), int(rng.integers(2, 30))
        pts = [(x, y)]
        for _ in range(4):
            x += int(rng.integers(-3, 4)); y += int(rng.integers(-1, 3))
            pts.append((x, y))
        d.line(pts, fill=gray(220))
    for _ in range(6):
        x, y = int(rng.integers(1, 31)), int(rng.integers(1, 31))
        px[x, y] = gray(222)
    return t


def tile_stone_side(rng):
    t = Image.new("RGBA", (32, 32))
    px = t.load()
    for y in range(32):
        for x in range(32):
            px[x, y] = gray(244 + rng.integers(-8, 9))
    d = ImageDraw.Draw(t)
    # 벽돌 줄 4개 (8px), 이음새는 엇갈리게
    for row in range(4):
        y0 = row * 8
        d.line([(0, y0), (31, y0)], fill=gray(192))
        off = 0 if row % 2 == 0 else 8
        for x in range(off, 32, 16):
            d.line([(x, y0), (x, y0 + 7)], fill=gray(192))
        d.line([(0, y0 + 1), (31, y0 + 1)], fill=gray(255))  # 벽돌 윗면 밝은 선
    # 아래쪽은 물때 (조금 어둡게)
    for y in range(26, 32):
        for x in range(32):
            r, g, b, a = px[x, y]
            px[x, y] = (int(r * 0.88), int(g * 0.92), int(b * 0.88), 255)
    return t


WOOD = np.array([150, 98, 56])
WOOD_D = np.array([98, 60, 32])
WOOD_L = np.array([188, 132, 80])
ROPE = np.array([214, 186, 120])


def tile_log_top(rng):
    """뗏목 윗면: 가로로 누운 통나무 4개 + 밧줄 2줄"""
    t = Image.new("RGBA", (32, 32))
    px = t.load()
    for y in range(32):
        k = y % 8
        for x in range(32):
            if k == 0:
                c = WOOD_D * 0.8
            elif k in (1, 7):
                c = WOOD_D
            elif k == 2:
                c = WOOD_L
            else:
                c = WOOD + rng.integers(-8, 9)
            # 나뭇결
            if k in (3, 5) and (x + y * 3) % 7 == 0:
                c = WOOD_D * 1.05
            px[x, y] = rgba(np.clip(c, 0, 255))
    for x0 in (6, 24):
        for y in range(32):
            for x in (x0, x0 + 1):
                c = ROPE if (y + x) % 3 else ROPE * 0.8
                px[x, y] = rgba(c)
    return t


def tile_log_side(rng):
    """뗏목 옆면: 위쪽 13px에 통나무 끝 (둥근 나이테), 아래는 물에 젖은 나무"""
    t = Image.new("RGBA", (32, 32))
    px = t.load()
    for y in range(32):
        for x in range(32):
            px[x, y] = rgba(np.clip(WOOD_D * 0.75 + rng.integers(-6, 7), 0, 255))
    d = ImageDraw.Draw(t)
    for i in range(4):
        cx = 4 + i * 8
        d.ellipse([cx - 4, 1, cx + 3, 12], fill=rgba(WOOD_L), outline=rgba(WOOD_D * 0.8))
        d.ellipse([cx - 2, 4, cx + 1, 9], outline=rgba(WOOD))
        px[cx - 1, 6] = rgba(WOOD_D)
    d.line([(0, 13), (31, 13)], fill=rgba(WOOD_D * 0.6))
    return t


CROC = np.array([96, 128, 74])
CROC_D = np.array([58, 82, 46])
CROC_L = np.array([140, 168, 98])


def tile_croc_top(rng):
    """악어 등: 가운데 줄 따라 솟은 비늘(골판) + 작은 비늘"""
    t = Image.new("RGBA", (32, 32))
    px = t.load()
    for y in range(32):
        for x in range(32):
            c = CROC + rng.integers(-6, 7)
            if x % 6 == 0 or y % 6 == 0:
                c = CROC_D
            px[x, y] = rgba(np.clip(c, 0, 255))
    d = ImageDraw.Draw(t)
    for row in range(4):
        y0 = row * 8 + 1
        for cx in (10, 21):
            d.polygon([(cx - 3, y0 + 6), (cx, y0), (cx + 3, y0 + 6)], fill=rgba(CROC_L), outline=rgba(CROC_D * 0.8))
    return t


def tile_croc_side(rng):
    t = Image.new("RGBA", (32, 32))
    px = t.load()
    for y in range(32):
        for x in range(32):
            belly = y > 20
            base = np.array([196, 190, 140]) if belly else CROC
            c = base + rng.integers(-6, 7)
            if (x + (y // 4) * 2) % 4 == 0 or y % 4 == 0:
                c = c * 0.78
            px[x, y] = rgba(np.clip(c, 0, 255))
    return t


def tile_plate(rng):
    """발판: 밝은 돌에 둥근 홈 + 물결 새김 (장치 색과 곱해짐)"""
    t = Image.new("RGBA", (32, 32))
    px = t.load()
    for y in range(32):
        for x in range(32):
            px[x, y] = gray(250 + rng.integers(-5, 6))
    d = ImageDraw.Draw(t)
    d.ellipse([5, 5, 26, 26], outline=gray(176), width=2)
    d.ellipse([9, 9, 22, 22], outline=gray(206))
    for k in range(3):
        y = 12 + k * 3
        d.line([(11, y), (13, y - 1), (15, y), (17, y - 1), (19, y), (21, y - 1)], fill=gray(184))
    return t


def make_atlas():
    rng = np.random.default_rng(7)
    tiles = [tile_stone_top(rng), tile_stone_side(rng), tile_log_top(rng), tile_log_side(rng),
             tile_croc_top(rng), tile_croc_side(rng), tile_plate(rng), Image.new("RGBA", (32, 32), (255, 255, 255, 255))]
    atlas = Image.new("RGBA", (256, 32))
    for i, t in enumerate(tiles):
        # 그림의 아래가 UV v=0 (Unity는 아래부터) -> 위아래 그대로 두면 됨 (그림 위쪽 = 블록 위쪽)
        atlas.paste(t, (i * 32, 0))
    p = os.path.join(OUT, "Sobek_Illusion_Atlas.png")
    atlas.save(p)
    write_meta(p, "texture", point=True, repeat=False)
    return atlas


# ================= 물결 =================
def make_water():
    n = 64
    yy, xx = np.mgrid[0:n, 0:n].astype(np.float32)
    tau = math.tau
    w = (np.sin(tau * (xx / n * 2 + yy / n * 1)) * 0.5 + np.sin(tau * (xx / n * -1 + yy / n * 3) + 1.3) * 0.35
         + np.sin(tau * (xx / n * 4 + yy / n * -2) + 0.6) * 0.2)
    w = (w - w.min()) / (np.ptp(w) + 1e-6)
    levels = np.floor(w * 5) / 4.0  # 5단계로 끊어 도트 느낌
    base = 0.86 + levels * 0.14
    img = np.zeros((n, n, 4), np.uint8)
    img[:, :, 0] = np.clip(base * 232, 0, 255)
    img[:, :, 1] = np.clip(base * 248, 0, 255)
    img[:, :, 2] = np.clip(base * 255, 0, 255)
    img[:, :, 3] = 255
    # 물결 꼭대기 반짝임
    crest = (w > 0.9) & (((xx + yy * 2) % 5) < 2)
    img[crest] = (255, 255, 255, 255)
    im = Image.fromarray(img, "RGBA")
    p = os.path.join(OUT, "Sobek_Illusion_Water.png")
    im.save(p)
    write_meta(p, "texture", point=True, repeat=True)
    return im


# ================= 배경 =================
BW, BH = 480, 270


def grad(top, bot):
    y = np.linspace(0, 1, BH)[:, None, None]
    arr = np.array(top)[None, None, :] * (1 - y) + np.array(bot)[None, None, :] * y
    arr = np.repeat(arr, BW, axis=1)
    return Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8), "RGB").convert("RGBA")


def dither_bands(img, steps=10):
    """하늘 그라데이션을 띠 모양으로 끊어 도트 느낌"""
    arr = np.asarray(img).astype(np.float32)
    arr[:, :, :3] = np.round(arr[:, :, :3] / (256 / steps)) * (256 / steps)
    return Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8), "RGBA")


def pyramid(d, x, base, w, col, shade):
    d.polygon([(x - w // 2, base), (x, base - w // 2), (x + w // 2, base)], fill=col)
    d.polygon([(x, base - w // 2), (x + w // 2, base), (x + w // 8, base)], fill=shade)


def palm(d, x, base, h, col):
    d.line([(x, base), (x + 2, base - h // 2), (x + 1, base - h)], fill=col, width=2)
    tx, ty = x + 1, base - h
    for a in (-150, -120, -60, -30, -90, 180, 0):
        r = math.radians(a)
        d.line([(tx, ty), (tx + math.cos(r) * h * 0.45, ty + math.sin(r) * h * 0.30 + 3)], fill=col, width=2)


def pylon(d, x, base, w, h, col, edge):
    """신전 탑문 (사다리꼴 두 개 + 가운데 문)"""
    for sx in (-1, 1):
        cx = x + sx * w * 0.32
        d.polygon([(cx - w * 0.2, base), (cx - w * 0.16, base - h), (cx + w * 0.16, base - h), (cx + w * 0.2, base)], fill=col)
        d.line([(cx - w * 0.16, base - h), (cx + w * 0.16, base - h)], fill=edge, width=2)
    d.rectangle([x - w * 0.1, base - h * 0.62, x + w * 0.1, base], fill=col)
    d.rectangle([x - w * 0.05, base - h * 0.45, x + w * 0.05, base], fill=edge)


def reeds(d, x0, x1, base, h, col, rng):
    for _ in range(int((x1 - x0) / 2.5)):
        x = rng.uniform(x0, x1)
        hh = h * rng.uniform(0.5, 1.0)
        lean = rng.uniform(-6, 6)
        d.line([(x, base), (x + lean, base - hh)], fill=col, width=1)
        if rng.random() < 0.35:
            d.ellipse([x + lean - 2, base - hh - 4, x + lean + 2, base - hh + 2], fill=col)


def column(img, x, w, col, band, rng):
    """화면 양옆의 큰 기둥 (연꽃 머리 + 상형문자 띠) -> 연못 바깥 모서리를 채움"""
    d = ImageDraw.Draw(img)
    d.rectangle([x, 0, x + w, BH], fill=col)
    shade = tuple(int(c * 0.72) for c in col[:3]) + (255,)
    light = tuple(min(255, int(c * 1.15)) for c in col[:3]) + (255,)
    d.rectangle([x + w - 6, 0, x + w, BH], fill=shade)
    d.rectangle([x, 0, x + 3, BH], fill=light)
    # 세로 홈
    for k in range(3):
        gx = x + 8 + k * (w - 16) // 3
        d.line([(gx, 30), (gx, BH)], fill=shade)
    # 연꽃 머리
    d.polygon([(x - 8, 26), (x + w + 8, 26), (x + w, 6), (x, 6)], fill=light)
    for k in range(5):
        cx = x - 4 + k * (w + 8) / 4
        d.line([(cx, 26), (cx + 2, 8)], fill=shade)
    # 색 띠 + 상형문자 칸
    for i, yb in enumerate(range(40, BH, 46)):
        d.rectangle([x + 2, yb, x + w - 2, yb + 4], fill=band[i % len(band)])
        gy = yb + 14
        kind = rng.integers(0, 4)
        cx = x + w // 2
        if kind == 0:
            d.ellipse([cx - 5, gy, cx + 5, gy + 10], outline=shade, width=2)
        elif kind == 1:
            d.line([(cx - 6, gy + 4), (cx - 2, gy), (cx + 2, gy + 4), (cx + 6, gy)], fill=shade, width=2)
            d.line([(cx - 6, gy + 10), (cx - 2, gy + 6), (cx + 2, gy + 10), (cx + 6, gy + 6)], fill=shade, width=2)
        elif kind == 2:
            d.line([(cx, gy), (cx, gy + 14)], fill=shade, width=2)
            d.line([(cx - 5, gy + 4), (cx + 5, gy + 4)], fill=shade, width=2)
        else:
            d.polygon([(cx - 7, gy + 12), (cx, gy), (cx + 7, gy + 12)], outline=shade)


def stars(d, rng, n, y1, col):
    for _ in range(n):
        x, y = rng.integers(0, BW), rng.integers(0, y1)
        d.point((int(x), int(y)), fill=col)
        if rng.random() < 0.2:
            d.point((int(x) + 1, int(y)), fill=col)


def croc_silhouette(d, x, y, s, col, face=1):
    pts = [(-1.0, 0), (-0.6, -0.12), (-0.1, -0.16), (0.35, -0.12), (0.62, -0.08), (1.0, -0.02), (1.0, 0.02), (0.6, 0.04), (-0.2, 0.06), (-1.0, 0.02)]
    d.polygon([(x + px * s * face, y + py * s) for px, py in pts], fill=col)
    for k in range(5):
        bx = x + (-0.4 + k * 0.15) * s * face
        d.polygon([(bx - 2, y - 0.14 * s), (bx, y - 0.2 * s), (bx + 2, y - 0.14 * s)], fill=col)


def water_lines(d, y0, y1, col, rng):
    for y in range(int(y0), int(y1), 6):
        x = rng.integers(-20, 0)
        while x < BW:
            L = rng.integers(6, 18)
            d.line([(int(x), y), (int(x + L), y)], fill=col)
            x += L + rng.integers(8, 26)


def backdrop(key):
    rng = np.random.default_rng(sum(ord(ch) * 31 ** i for i, ch in enumerate(key)) % 1000)  # 실행마다 같은 그림
    prng = random.Random(key)
    hz = int(BH * 0.50)  # 수평선
    if key == "S1":   # 소용돌이 섬: 해질녘 청록 + 소용돌이 구름
        img = grad((54, 70, 118), (236, 148, 92)); sun = ((255, 214, 120), (BW * 0.5, hz - 18), 22)
        sil, water, col_c, band = (58, 44, 52), ((40, 92, 104), (16, 40, 52)), (150, 112, 82, 255), [(60, 110, 150, 255), (214, 164, 62, 255)]
    elif key == "S2":  # 악어 등 다리: 아침 초록 갈대
        img = grad((132, 176, 170), (232, 214, 150)); sun = ((255, 240, 190), (BW * 0.72, hz - 46), 16)
        sil, water, col_c, band = (62, 88, 58), ((54, 104, 90), (24, 54, 50)), (170, 140, 96, 255), [(70, 132, 96, 255), (214, 164, 62, 255)]
    elif key == "S3":  # 범람하는 나일: 한낮 금빛 + 물에 잠긴 들
        img = grad((112, 168, 214), (246, 216, 150)); sun = ((255, 246, 210), (BW * 0.3, hz - 70), 18)
        sil, water, col_c, band = (120, 96, 62), ((70, 126, 160), (30, 70, 104)), (196, 160, 104, 255), [(44, 70, 140, 255), (214, 164, 62, 255)]
    elif key == "S4":  # 두 개의 수면: 밤, 물에 비친 쌍둥이 신전
        img = grad((14, 18, 46), (56, 52, 96)); sun = ((236, 236, 214), (BW * 0.5, hz - 60), 14)
        sil, water, col_c, band = (30, 30, 60), ((30, 40, 80), (10, 14, 34)), (96, 90, 120, 255), [(38, 62, 140, 255), (200, 200, 220, 255)]
    elif key == "M1":  # 범람기: 먹구름 청록
        img = grad((44, 70, 84), (120, 150, 140)); sun = ((210, 230, 220), (BW * 0.62, hz - 50), 12)
        sil, water, col_c, band = (34, 50, 56), ((36, 84, 96), (12, 36, 44)), (130, 120, 100, 255), [(60, 110, 150, 255), (176, 62, 42, 255)]
    elif key == "M2":  # 건기의 미궁: 붉은 모래 오후
        img = grad((196, 120, 76), (240, 196, 130)); sun = ((255, 226, 160), (BW * 0.4, hz - 40), 20)
        sil, water, col_c, band = (120, 66, 40), ((120, 108, 80), (70, 60, 42)), (180, 130, 84, 255), [(176, 62, 42, 255), (214, 164, 62, 255)]
    else:              # M3 소베크-라의 탑: 새벽 + 큰 태양 원반
        img = grad((40, 36, 86), (246, 170, 96)); sun = ((255, 200, 90), (BW * 0.5, hz - 30), 34)
        sil, water, col_c, band = (50, 34, 46), ((60, 70, 110), (20, 24, 50)), (160, 120, 86, 255), [(214, 164, 62, 255), (176, 62, 42, 255)]

    img = dither_bands(img, 14)
    d = ImageDraw.Draw(img)
    if key in ("S4", "M3"):
        stars(d, rng, 60 if key == "S4" else 25, hz - 20, (240, 240, 220, 255))
    # 해/달
    (sc, (sx, sy), sr) = sun
    d.ellipse([sx - sr - 6, sy - sr - 6, sx + sr + 6, sy + sr + 6], fill=tuple(int(c * 0.9) for c in sc) + (255,))
    d.ellipse([sx - sr, sy - sr, sx + sr, sy + sr], fill=sc + (255,))
    if key == "M3":  # 소베크-라: 원반을 감싼 코브라 두 마리 + 빛줄기
        for a in range(0, 360, 20):
            r = math.radians(a)
            d.line([(sx + math.cos(r) * (sr + 10), sy + math.sin(r) * (sr + 10)), (sx + math.cos(r) * (sr + 30), sy + math.sin(r) * (sr + 30))], fill=(255, 220, 140, 255))
        d.ellipse([sx - sr + 8, sy - sr + 8, sx + sr - 8, sy + sr - 8], fill=(206, 72, 40, 255))
    if key == "S1":  # 소용돌이 구름
        for k in range(4):
            cx, cy, rr = 80 + k * 110, 40 + (k % 2) * 18, 16 + k % 3 * 4
            d.arc([cx - rr, cy - rr * 0.5, cx + rr, cy + rr * 0.5], 180, 520, fill=(250, 200, 160, 255), width=2)
            d.arc([cx - rr * 0.5, cy - rr * 0.25, cx + rr * 0.5, cy + rr * 0.25], 0, 300, fill=(250, 200, 160, 255), width=2)
    if key == "M1":  # 비구름
        for k in range(6):
            cx = 40 + k * 80
            d.ellipse([cx - 40, 10, cx + 40, 44], fill=(60, 84, 96, 255))
            for j in range(6):
                d.line([(cx - 30 + j * 12, 44), (cx - 36 + j * 12, 70)], fill=(120, 150, 150, 255))

    # 지평선 실루엣 (피라미드·탑문·야자수)
    sil_c = sil + (255,)
    sil_s = tuple(int(c * 0.7) for c in sil) + (255,)
    if key in ("S3", "M2"):
        pyramid(d, 130, hz, 90, sil_c, sil_s); pyramid(d, 200, hz, 60, sil_c, sil_s); pyramid(d, 380, hz, 70, sil_c, sil_s)
    else:
        pylon(d, BW * 0.5, hz, 120, 54, sil_c, sil_s)
    for x in (40, 70, 300, 420, 450):
        palm(d, x, hz, prng.randint(26, 40), sil_c)
    d.rectangle([0, hz - 2, BW, hz], fill=sil_c)

    # 강 (수평선 아래)
    w0, w1 = water
    arr = np.asarray(img).astype(np.float32)
    t = np.linspace(0, 1, BH - hz)[:, None]
    river = np.array(w0)[None, :] * (1 - t) + np.array(w1)[None, :] * t
    arr[hz:, :, :3] = river[:, None, :]
    img = Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8), "RGBA")
    img = dither_bands(img, 18)
    d = ImageDraw.Draw(img)
    # 물에 비친 해 / 쌍둥이 신전 (S4는 거꾸로 비친 신전)
    d.rectangle([sx - sr, hz + 6, sx + sr, hz + 8], fill=sc + (255,))
    d.rectangle([sx - sr * 0.6, hz + 14, sx + sr * 0.6, hz + 15], fill=sc + (255,))
    if key == "S4":
        refl = img.crop((0, hz - 60, BW, hz)).transpose(Image.FLIP_TOP_BOTTOM)
        refl = Image.blend(refl, Image.new("RGBA", refl.size, w0 + (255,)), 0.55)
        img.paste(refl, (0, hz))
        d = ImageDraw.Draw(img)
    water_lines(d, hz + 10, BH, tuple(min(255, int(c * 1.5) + 20) for c in w0) + (255,), rng)
    if key == "S3":  # 물에 잠긴 들 (초록 띠 + 작은 집)
        for k in range(5):
            y = hz + 22 + k * 14
            d.rectangle([20 + k * 30, y, 120 + k * 30, y + 3], fill=(90, 140, 80, 255))
            d.rectangle([BW - 150 - k * 20, y, BW - 60 - k * 20, y + 3], fill=(90, 140, 80, 255))
    if key in ("S2", "M1"):
        croc_silhouette(d, 70, BH - 40, 46, tuple(int(c * 0.7) for c in w1) + (255,), 1)
        croc_silhouette(d, BW - 80, BH - 64, 38, tuple(int(c * 0.7) for c in w1) + (255,), -1)
    # 앞 갈대 (아래 모서리)
    reeds(d, 0, 110, BH, 90 if key == "S2" else 60, tuple(int(c * 0.5) for c in sil) + (255,), prng)
    reeds(d, BW - 110, BW, BH, 90 if key == "S2" else 60, tuple(int(c * 0.5) for c in sil) + (255,), prng)
    # 양옆 큰 기둥 (신전 안에서 바깥 강을 내다보는 느낌)
    column(img, -10, 52, col_c, band, rng)
    column(img, BW - 42, 52, col_c, band, rng)
    # 위 처마 (상형문자 띠)
    d = ImageDraw.Draw(img)
    d.rectangle([0, 0, BW, 7], fill=tuple(int(c * 0.6) for c in col_c[:3]) + (255,))
    for i, x in enumerate(range(0, BW, 12)):
        d.rectangle([x + 1, 2, x + 9, 5], fill=band[i % len(band)])
    p = os.path.join(OUT, f"Sobek_Backdrop_{key}.png")
    img.convert("RGB").save(p)
    write_meta(p, "texture", point=True, repeat=False)
    return img


# ================= 허브: 통나무 막이 · 어둠 =================
def make_log_block():
    """세로 2칸 다리를 가로막는 통나무 2개 (위에서 본 모습, 32x72)"""
    W_, H_ = 40, 76
    img = Image.new("RGBA", (W_, H_), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    rng = np.random.default_rng(3)
    for i, x0 in enumerate((4, 20)):
        # 통나무 몸통
        d.rounded_rectangle([x0, 4, x0 + 15, H_ - 5], radius=6, fill=rgba(WOOD), outline=rgba(WOOD_D * 0.7))
        d.line([(x0 + 3, 8), (x0 + 3, H_ - 9)], fill=rgba(WOOD_L))
        for y in range(10, H_ - 10, 5):
            xx = x0 + 6 + int(rng.integers(0, 6))
            d.line([(xx, y), (xx, y + 3)], fill=rgba(WOOD_D))
        # 위쪽 끝 나이테
        d.ellipse([x0 + 1, 2, x0 + 14, 13], fill=rgba(WOOD_L), outline=rgba(WOOD_D * 0.7))
        d.ellipse([x0 + 4, 5, x0 + 11, 10], outline=rgba(WOOD))
        # 잔가지
        d.line([(x0 + 15, 30 + i * 12), (x0 + 19, 26 + i * 12)], fill=rgba(WOOD_D), width=2)
    # 밧줄
    for y in (22, 52):
        d.rectangle([3, y, W_ - 4, y + 2], fill=rgba(ROPE))
        d.line([(3, y + 3), (W_ - 4, y + 3)], fill=rgba(ROPE * 0.7))
    # 그림자 (아래)
    d.ellipse([2, H_ - 6, W_ - 2, H_ - 1], fill=(0, 0, 0, 90))
    p = os.path.join(OUT_WORLD, "Sobek_Obj_LogBlock.png")
    img.save(p)
    write_meta(p, "sprite", ppu=32, pivot=(0.5, 0.5), point=True)
    return img


def make_fog():
    """막힌 섬을 덮는 어둠 64x64 (가장자리 16px이 부드럽게 사라짐, 9칸 자르기 테두리 16)"""
    n, b = 64, 16
    yy, xx = np.mgrid[0:n, 0:n].astype(np.float32)
    dx = np.minimum(xx, n - 1 - xx)
    dy = np.minimum(yy, n - 1 - yy)
    edge = np.clip(np.minimum(dx, dy) / b, 0, 1)
    a = (edge ** 1.5) * 255
    img = np.zeros((n, n, 4), np.uint8)
    img[:, :, 3] = a.astype(np.uint8)
    img[:, :, 0:3] = 255
    im = Image.fromarray(img, "RGBA")
    p = os.path.join(OUT_WORLD, "Sobek_Fx_Fog.png")
    im.save(p)
    write_meta(p, "sprite", ppu=32, pivot=(0.5, 0.5), point=False, border=(b, b, b, b))
    return im


def main():
    if not os.path.isdir(os.path.join(ROOT, "Assets")):
        sys.exit("프로젝트 루트를 찾지 못함")
    for folder in (os.path.join(ROOT, "Assets", "Resources", "Sobek"), OUT):
        os.makedirs(folder, exist_ok=True)
        write_folder_meta(folder)
    os.makedirs(OUT_WORLD, exist_ok=True)
    atlas = make_atlas()
    water = make_water()
    keys = ["S1", "S2", "S3", "S4", "M1", "M2", "M3"]
    backs = [backdrop(k) for k in keys]
    log = make_log_block()
    fog = make_fog()
    print("완료: 아틀라스, 물결, 배경 %d장, 통나무, 어둠" % len(backs))
    if PREVIEW:
        sheet = Image.new("RGB", (BW * 2, BH * 4 + 80), (30, 30, 30))
        for i, b in enumerate(backs):
            sheet.paste(b.convert("RGB"), ((i % 2) * BW, (i // 2) * BH))
        sheet.paste(atlas.resize((512, 64), Image.NEAREST).convert("RGB"), (BW, BH * 3 + 10))
        sheet.paste(water.resize((128, 128), Image.NEAREST).convert("RGB"), (BW + 520, BH * 3 + 10))
        sheet.paste(log.resize((80, 152), Image.NEAREST), (BW + 660, BH * 3 + 10), log.resize((80, 152), Image.NEAREST))
        sheet.save(PREVIEW)
        print("미리보기:", PREVIEW)


if __name__ == "__main__":
    main()
