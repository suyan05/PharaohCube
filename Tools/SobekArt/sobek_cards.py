# -*- coding: utf-8 -*-
"""소베크의 방 고고학 수첩 유물 카드 그림 (420x420 jpg, 바스테트 카드와 같은 방식, 임시 그림)
만드는 것: Assets/Resources/Notebook/ACH_SOBEK_*.jpg (+ .meta)
  WHIRL_AMULET / CROC_AMULET / FLOOD_AMULET / MIRROR_AMULET : 서브 퍼즐 부적 4개 (아이콘을 크게)
  TOOTH   : 히든 유물 소베크의 이빨
  MURALS  : 전실 벽화 네 폭 (벽화 그림을 잘라 씀 -> sobek_murals.py를 먼저 실행)
  MUMMY   : 페트수코스의 길 (붕대 감은 악어 미라)
실행: python3 Tools/SobekArt/sobek_cards.py   (프로젝트 루트에서)"""
import math
import os
import random
import sys

from PIL import Image, ImageDraw, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
sys.path.insert(0, HERE)
from unity_meta import write_meta  # noqa: E402

ICONS = os.path.join(ROOT, "Assets", "image", "Sobek", "UI")
MURAL = os.path.join(ROOT, "Assets", "Resources", "Horus", "Mural")
OUT = os.path.join(ROOT, "Assets", "Resources", "Notebook")
S = 420
random.seed(11)


def backdrop(top, bot, ray):
    bg = Image.new("RGB", (S, S))
    d = ImageDraw.Draw(bg)
    for y in range(S):
        t = y / (S - 1)
        d.line([(0, y), (S, y)], fill=tuple(int(top[i] * (1 - t) + bot[i] * t) for i in range(3)))
    rays = Image.new("L", (S, S), 0)
    rd = ImageDraw.Draw(rays)
    cx, cy = S / 2, S * 0.46
    for k in range(14):
        a0 = k * 2 * math.pi / 14
        a1 = a0 + 0.12
        rd.polygon([(cx, cy), (cx + 600 * math.cos(a0), cy + 600 * math.sin(a0)), (cx + 600 * math.cos(a1), cy + 600 * math.sin(a1))], fill=55)
    glow = Image.new("L", (S, S), 0)
    ImageDraw.Draw(glow).ellipse([cx - 150, cy - 150, cx + 150, cy + 150], fill=140)
    mask = Image.blend(rays.filter(ImageFilter.GaussianBlur(6)), glow.filter(ImageFilter.GaussianBlur(60)), 0.5).point(lambda v: min(255, v * 2))
    bg = Image.composite(Image.new("RGB", (S, S), ray), bg, mask.point(lambda v: int(v * 0.55)))
    sd = Image.new("L", (S, S), 0)
    ImageDraw.Draw(sd).ellipse([90, 336, 330, 380], fill=150)
    return Image.composite(Image.new("RGB", (S, S), (10, 7, 5)), bg, sd.filter(ImageFilter.GaussianBlur(10)))


def finish(base, name):
    vig = Image.new("L", (S, S), 0)
    ImageDraw.Draw(vig).ellipse([-60, -60, S + 60, S + 60], fill=255)
    out = Image.composite(base.convert("RGB"), Image.new("RGB", (S, S), (8, 6, 4)), vig.filter(ImageFilter.GaussianBlur(70)))
    p = os.path.join(OUT, f"ACH_{name}.jpg")
    out.save(p, quality=92)
    write_meta(p, "sprite", ppu=100, point=False)
    print("saved", name)


def paste_center(bg, img, size=280, y=55):
    img = img.resize((size, int(img.height * size / img.width)), Image.LANCZOS)
    sh = Image.new("RGBA", img.size, (0, 0, 0, 0))
    sh.putalpha(img.split()[3].point(lambda v: int(v * 0.5)))
    sh = sh.filter(ImageFilter.GaussianBlur(8))
    base = bg.convert("RGBA")
    x = (S - img.width) // 2
    base.alpha_composite(sh, (x + 8, y + 14))
    base.alpha_composite(img, (x, y))
    return base


def icon_card(icon, name, top, bot, ray, pixel=True):
    ic = Image.open(os.path.join(ICONS, icon + ".png")).convert("RGBA")
    box = ic.getbbox()
    ic = ic.crop(box)
    side = max(ic.size)
    sq = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    sq.paste(ic, ((side - ic.width) // 2, (side - ic.height) // 2))
    # 도트 그림은 계단식으로 키운 뒤 살짝 부드럽게
    big = sq.resize((side * 4, side * 4), Image.NEAREST) if pixel else sq
    finish(paste_center(backdrop(top, bot, ray), big, 260, 62), name)


def mural_card():
    """전실 벽화 네 폭: 벽화 1~4를 2x2로 이어 붙인 판"""
    tiles = []
    for i in range(1, 5):
        p = os.path.join(MURAL, f"MU_SobekP{i}_bg.png")
        if not os.path.exists(p):
            print("벽화 그림 없음 (sobek_murals.py 먼저):", p)
            return
        tiles.append(Image.open(p).convert("RGB").crop((0, 20, 1280, 700)).resize((150, 80), Image.LANCZOS))
    board = Image.new("RGBA", (318, 178), (60, 40, 26, 255))
    d = ImageDraw.Draw(board)
    d.rectangle([2, 2, 315, 175], outline=(214, 164, 62, 255), width=3)
    for k, t in enumerate(tiles):
        board.paste(t, (6 + (k % 2) * 156, 6 + (k // 2) * 86))
    finish(paste_center(backdrop((70, 104, 100), (20, 30, 30), (200, 240, 220)), board, 300, 100), "SOBEK_MURALS")


def mummy_card():
    """페트수코스의 길: 붕대를 감은 악어 미라 (위에서 비스듬히 본 옆모습)"""
    S2 = 840
    img = Image.new("RGBA", (S2, S2), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    ink = (60, 42, 30, 255)
    linen = (226, 210, 172, 255)
    linen_d = (176, 150, 108, 255)
    body = [(80, 470), (170, 400), (330, 360), (500, 350), (620, 360), (700, 390), (790, 440), (790, 470), (700, 500), (560, 530), (330, 540), (160, 520)]
    d.polygon(body, fill=linen)
    # 감긴 붕대 (사선) - 몸 모양 밖으로 나가지 않게 따로 그려서 몸 모양으로 자름
    wrap = Image.new("RGBA", (S2, S2), (0, 0, 0, 0))
    wd = ImageDraw.Draw(wrap)
    for x in range(60, 800, 30):
        wd.line([(x, 545), (x + 70, 345)], fill=linen_d, width=8)
    mask = Image.new("L", (S2, S2), 0)
    ImageDraw.Draw(mask).polygon(body, fill=255)
    img.paste(wrap, (0, 0), Image.composite(wrap.split()[3], Image.new("L", (S2, S2), 0), mask))
    d = ImageDraw.Draw(img)
    d.line(body + [body[0]], fill=ink, width=8)
    # 눈 자리 (붕대 위 금 눈)
    d.ellipse([705, 405, 735, 425], fill=(214, 164, 62, 255), outline=ink, width=4)
    # 금 장식 (귀걸이·목걸이) - 페트수코스는 금으로 꾸몄다
    d.ellipse([640, 470, 680, 510], outline=(214, 164, 62, 255), width=8)
    d.line([(560, 356), (560, 530)], fill=(214, 164, 62, 255), width=12)
    # 받침
    d.rectangle([140, 540, 720, 580], fill=(120, 84, 48, 255), outline=ink, width=6)
    img = img.resize((S, S), Image.LANCZOS)
    base = backdrop((60, 52, 40), (18, 14, 10), (230, 210, 160)).convert("RGBA")
    base.alpha_composite(img, (0, -20))
    finish(base, "SOBEK_CROC_MUMMY")


def main():
    if not os.path.isdir(OUT):
        sys.exit("프로젝트 루트에서 실행하세요 (Assets/Resources/Notebook 없음)")
    icon_card("Common_Icon_SobekAmuletRotate", "SOBEK_WHIRL_AMULET", (70, 60, 120), (20, 16, 40), (200, 190, 255))
    icon_card("Common_Icon_SobekAmuletLift", "SOBEK_CROC_AMULET", (120, 70, 44), (36, 20, 14), (255, 200, 150))
    icon_card("Common_Icon_SobekAmuletWater", "SOBEK_FLOOD_AMULET", (44, 90, 140), (14, 24, 44), (170, 220, 255))
    icon_card("Common_Icon_SobekAmuletMirror", "SOBEK_MIRROR_AMULET", (40, 110, 90), (12, 32, 26), (180, 255, 220))
    icon_card("Common_Icon_SobekTooth", "SOBEK_TOOTH", (70, 60, 44), (20, 16, 12), (255, 236, 190))
    mural_card()
    mummy_card()


if __name__ == "__main__":
    main()
