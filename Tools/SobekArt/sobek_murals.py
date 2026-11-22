# -*- coding: utf-8 -*-
"""
소베크의 방 전실 벽화 4장 그림 생성기 (바스테트 벽화와 같은 평면 이집트 벽화 화풍, 임시 그림)
그리기 도구는 Tools/BastetMural/bastet_murals.py 것을 그대로 빌려 씀 (그 파일은 안 고침)

만드는 것 (Assets/Resources/Horus/Mural/  <- 벽화 보기 화면이 읽는 폴더, 이름만 MU_Sobek* 로 구분)
  MU_SobekP{n}_bg                  : E를 누르면 먼저 크게 보여 주는 깨끗한 벽화 그림
  MU_SobekW{n}_bg / _rays / _player : 플레이어 등 뒤에서 낡은 벽화를 올려다보는 장면
  MU_SobekS{n}_bg / _<조각>          : 벽화 속 인물이 움직이는 이야기 장면
그리고
  Assets/image/Sobek/Story/Sobek_Mural{n}.png : 전실 벽에 걸리는 작은 도트 벽화 (128x64, 32 PPU = 4x2칸)
  Assets/Scripts/Puzzles/Sobek/Story/SobekMuralArt.cs : 조각 위치표

벽화 이야기 (스토리 기획서 B안 소베크 - 저승의 강, 멈춘 배)
  1 물의 주인    : 나일은 소베크의 땀에서 흘러나왔다
  2 태초의 알    : 태초의 물가에 알을 낳아 세상을 열었다
  3 강을 삼킨 뱀 : 아펩이 강을 삼켜 태양의 배가 모래 위에 멈췄다
  4 소베크의 입  : 마지막 범람을 신전에 가두고 문을 잠갔다 (부적 네 개)

실행: python3 Tools/SobekArt/sobek_murals.py   (프로젝트 루트에서)
"""
import importlib.util
import math
import os
import random
import sys

from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
sys.path.insert(0, HERE)
from unity_meta import write_meta, write_folder_meta  # noqa: E402

spec = importlib.util.spec_from_file_location("bastet_murals", os.path.join(ROOT, "Tools", "BastetMural", "bastet_murals.py"))
bm = importlib.util.module_from_spec(spec)
spec.loader.exec_module(bm)

W, H, SS = bm.W, bm.H, bm.SS
Pen, place, smooth, blank, finish = bm.Pen, bm.place, bm.smooth, bm.blank, bm.finish
INK, GOLD, GOLD_D, LAPIS, LAPIS_D, RED = bm.INK, bm.GOLD, bm.GOLD_D, bm.LAPIS, bm.LAPIS_D, bm.RED
GREEN, GREEN_D, SKIN, WHITE, SAND, SAND_D, BROWN, WATER = bm.GREEN, bm.GREEN_D, bm.SKIN, bm.WHITE, bm.SAND, bm.SAND_D, bm.BROWN, bm.WATER
LINEN, LINEN_D = bm.LINEN, bm.LINEN_D
CROC = (78, 112, 70, 255)
CROC_D = (52, 78, 48, 255)
CROC_L = (150, 170, 104, 255)
NILE = (54, 98, 168, 255)
NILE_L = (130, 170, 226, 255)

OUT_MURAL = bm.OUT_MURAL
OUT_PIXEL = os.path.join(ROOT, "Assets", "image", "Sobek", "Story")
OUT_CS = os.path.join(ROOT, "Assets", "Scripts", "Puzzles", "Sobek", "Story", "SobekMuralArt.cs")
PREVIEW = os.environ.get("SOBEK_MURAL_PREVIEW")

# 벽 칸 상형문자: 고양이·미라 대신 물·갈대·해 위주
bm.GLYPHS = ["sun", "ankh", "eye", "water", "reed", "bread", "snake", "star", "water", "reed"]


# ================= 인물·동물 =================
def sobek_god(pen, x, y, h, face=1, item="was", crown=True):
    """악어 머리 신 소베크 (서 있는 옆모습). x,y = 발 밑, h = 머리 꼭대기까지"""
    P = lambda pts: place(pts, x, y, h, face)
    # 뒤로 내린 팔 + 앙크
    pen.thick(P([(-0.05, -0.70), (-0.10, -0.52), (-0.09, -0.42)]), SKIN, h * 0.035)
    ax, ay = P([(-0.09, -0.30)])[0]
    bm.ankh(pen, ax, ay, h * 0.13)
    # 다리 (앞으로 내딛음)
    pen.thick(P([(-0.04, -0.40), (-0.06, -0.04)]), SKIN, h * 0.04)
    pen.thick(P([(0.03, -0.40), (0.10, -0.04)]), SKIN, h * 0.04)
    pen.poly(P([(-0.09, -0.04), (-0.02, -0.04), (0.0, 0), (-0.10, 0)]), SKIN, INK, 2)
    pen.poly(P([(0.07, -0.04), (0.14, -0.04), (0.18, 0), (0.07, 0)]), SKIN, INK, 2)
    # 허리치마 (흰 리넨 + 금 띠, 앞자락 세모)
    pen.poly(P([(-0.08, -0.56), (0.07, -0.56), (0.13, -0.36), (-0.09, -0.36)]), LINEN, INK, 3)
    pen.poly(P([(0.0, -0.55), (0.12, -0.37), (0.03, -0.37)]), WHITE, INK, 2)
    for k in range(4):
        yy = -0.53 + k * 0.045
        pen.line(P([(-0.07, yy), (0.0, yy)]), LINEN_D, 1.5)
    pen.thick(P([(-0.08, -0.56), (0.07, -0.56)]), GOLD, h * 0.018, INK, 1.5)
    # 몸통 (맨살)
    pen.poly(P([(-0.075, -0.76), (0.06, -0.76), (0.065, -0.56), (-0.08, -0.56)]), SKIN, INK, 3)
    # 넓은 목걸이
    for i, col in enumerate([RED, LAPIS, GOLD]):
        r = 0.10 - i * 0.025
        cx, cy = P([(-0.005, -0.765)])[0]
        pen.d.pieslice([(cx - r * h) * SS, (cy - r * h * 0.85) * SS, (cx + r * h) * SS, (cy + r * h * 0.85) * SS], 0, 180, fill=col, outline=INK, width=int(1.5 * SS))
    # 가발 뒷자락 (청금석 줄무늬)
    pen.poly(P([(-0.075, -0.93), (-0.01, -0.95), (0.0, -0.80), (-0.05, -0.70), (-0.11, -0.73)]), LAPIS, INK, 2.5)
    for k in range(4):
        yy = -0.90 + k * 0.045
        pen.line(P([(-0.09, yy + 0.02), (-0.01, yy)]), LAPIS_D, 1.5)
    # 앞으로 내민 팔 + 와스 지팡이
    pen.thick(P([(0.05, -0.73), (0.14, -0.62), (0.22, -0.64)]), SKIN, h * 0.035)
    sx, sy = P([(0.235, -0.64)])[0]
    if item == "was":
        top = P([(0.235, -0.98)])[0]
        bot = P([(0.235, 0.0)])[0]
        pen.thick([top, bot], GOLD, h * 0.016, INK, 1.5)
        pen.poly([top, (top[0] + h * 0.06 * face, top[1] - h * 0.02), (top[0] + h * 0.07 * face, top[1] + h * 0.01)], GOLD, INK, 1.5)
        pen.line([bot, (bot[0] - h * 0.025, bot[1] + h * 0.01)], GOLD_D, 2)
        pen.line([bot, (bot[0] + h * 0.025, bot[1] + h * 0.01)], GOLD_D, 2)
    pen.ellipse(sx, sy, h * 0.02, h * 0.02, SKIN, INK, 1.5)
    # 악어 머리 (긴 주둥이)
    head = [(-0.06, -0.88), (-0.03, -0.96), (0.04, -0.97), (0.10, -0.93), (0.30, -0.885), (0.315, -0.865), (0.30, -0.85),
            (0.12, -0.835), (0.05, -0.80), (-0.02, -0.78), (-0.06, -0.80)]
    pen.poly(P(head), CROC, INK, 2.5)
    pen.line(P([(0.08, -0.86), (0.30, -0.865)]), INK, 1.5)                  # 입선
    for k in range(5):
        tx = 0.11 + k * 0.04
        pen.poly(P([(tx, -0.862), (tx + 0.012, -0.848), (tx + 0.024, -0.862)]), WHITE, None, 0)  # 이빨
    ex, ey = P([(0.03, -0.915)])[0]
    pen.ellipse(ex, ey, h * 0.014, h * 0.011, GOLD, INK, 1.2)
    for k in range(3):
        bx = -0.02 + k * 0.05
        pen.line(P([(bx, -0.955), (bx + 0.02, -0.94)]), CROC_D, 1.5)
    if crown:
        # 두 깃털 + 태양 원반 (소베크-라 관)
        for dx in (-0.03, 0.0):
            pen.poly(P([(dx - 0.012, -0.96), (dx + 0.012, -0.96), (dx + 0.02, -1.18), (dx - 0.005, -1.20)]), WHITE, INK, 1.5)
        cx, cy = P([(0.02, -1.0)])[0]
        pen.ellipse(cx, cy, h * 0.045, h * 0.045, RED, INK, 2)


def crocodile(pen, x, y, L, face=1, color=CROC, mouth=0.0):
    """옆모습 악어. x,y = 배 밑 가운데, L = 몸 길이 (꼬리 끝 ~ 주둥이 끝)"""
    P = lambda pts: [(x + px * L * face, y + py * L) for px, py in pts]
    # 먼 쪽 다리
    for lx in (-0.12, 0.20):
        pen.thick(P([(lx + 0.03, -0.04), (lx + 0.06, 0.0)]), CROC_D, L * 0.035, INK, 1.5)
    body = [(-0.50, -0.03), (-0.38, -0.06), (-0.20, -0.09), (0.05, -0.11), (0.25, -0.10), (0.33, -0.08), (0.36, -0.085),
            (0.50, -0.07), (0.50, -0.05 + mouth * 0.04), (0.36, -0.045), (0.30, -0.03), (0.0, -0.02), (-0.30, -0.015), (-0.50, -0.02)]
    pen.poly(P(body), color, INK, 3)
    if mouth > 0:
        pen.poly(P([(0.36, -0.06), (0.50, -0.065 - mouth * 0.05), (0.50, -0.06)]), (150, 60, 50, 255), INK, 1.5)
    # 등 비늘 혹
    for k in range(10):
        bx = -0.34 + k * 0.06
        top = -0.07 - 0.03 * math.sin((k + 2) / 12 * math.pi)
        pen.poly(P([(bx - 0.015, top + 0.01), (bx, top - 0.018), (bx + 0.015, top + 0.01)]), CROC_D, INK, 1)
    # 배 무늬
    pen.line(P(smooth([(-0.40, -0.025), (-0.1, -0.022), (0.28, -0.035)], 6)), CROC_L, 2)
    # 가까운 쪽 다리
    for lx in (-0.16, 0.16):
        pen.thick(P([(lx, -0.04), (lx + 0.04, 0.0), (lx + 0.07, 0.0)]), color, L * 0.04, INK, 2)
    ex, ey = P([(0.33, -0.105)])[0]
    pen.ellipse(ex, ey, L * 0.012, L * 0.009, GOLD, INK, 1.2)
    pen.line(P([(0.37, -0.058), (0.49, -0.06)]), INK, 1.5)


def waves(pen, x0, x1, y0, y1, color=NILE, line=NILE_L, step=26, rows=None):
    pen.rect(x0, y0, x1, y1, color, None, 0)
    rows = rows or max(1, int((y1 - y0) / 22))
    for k in range(rows):
        yy = y0 + 12 + k * (y1 - y0 - 16) / max(1, rows)
        pts = [(x, yy + (6 if ((x - x0) // step) % 2 else -6)) for x in range(int(x0), int(x1) + step, step)]
        pen.line(pts, line, 2.5)


def wheat(pen, x, y, h, col=GOLD):
    pen.line([(x, y), (x + h * 0.05, y - h)], GREEN_D, 2.5)
    for k in range(4):
        yy = y - h * (0.62 + k * 0.1)
        xx = x + h * 0.05 * (0.62 + k * 0.1)
        pen.ellipse(xx - h * 0.035, yy, h * 0.03, h * 0.05, col, INK, 1)
        pen.ellipse(xx + h * 0.035, yy, h * 0.03, h * 0.05, col, INK, 1)


def papyrus(pen, x, y, h):
    pen.line([(x, y), (x, y - h)], GREEN_D, 3)
    pen.poly([(x - h * 0.16, y - h * 1.12), (x, y - h * 0.96), (x + h * 0.16, y - h * 1.12), (x, y - h * 1.2)], GREEN, INK, 1.5)


def egg(pen, cx, cy, r, cracked=False):
    pen.ellipse(cx, cy, r * 0.78, r, WHITE, INK, 3)
    pen.ellipse(cx - r * 0.25, cy - r * 0.35, r * 0.2, r * 0.25, (250, 246, 232, 255), None, 0)
    if cracked:
        pen.line([(cx - r * 0.7, cy - r * 0.1), (cx - r * 0.35, cy - r * 0.3), (cx - r * 0.1, cy), (cx + r * 0.2, cy - r * 0.32), (cx + r * 0.45, cy - r * 0.05), (cx + r * 0.72, cy - r * 0.2)], INK, 3)


def rays(img, cx, cy, r0, r1, n, col):
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    for k in range(n):
        a = k / n * math.tau
        a2 = a + math.tau / n * 0.35
        d.polygon([((cx + math.cos(a) * r0) * SS, (cy + math.sin(a) * r0) * SS), ((cx + math.cos(a) * r1) * SS, (cy + math.sin(a) * r1) * SS),
                   ((cx + math.cos(a2) * r1) * SS, (cy + math.sin(a2) * r1) * SS)], fill=col)
    img.alpha_composite(layer)


def croc_gate(pen, cx, base, w, h, open_=False):
    """악어 입 모양 큰 문 (위턱·아래턱 + 이빨 + 부적 홈 4개)"""
    # 문틀 (탑문)
    pen.poly([(cx - w * 0.62, base), (cx - w * 0.55, base - h), (cx + w * 0.55, base - h), (cx + w * 0.62, base)], (196, 160, 104, 255), INK, 3)
    pen.rect(cx - w * 0.58, base - h - 14, cx + w * 0.58, base - h, (150, 110, 70, 255), INK, 2.5)
    for i, x in enumerate(range(int(cx - w * 0.56), int(cx + w * 0.56), 22)):
        pen.rect(x + 2, base - h - 11, x + 18, base - h - 3, bm.BAND[i % len(bm.BAND)], None, 0)
    # 입 (어두운 문)
    gap = 0.34 if open_ else 0.08
    pen.rect(cx - w * 0.36, base - h * 0.78, cx + w * 0.36, base, (40, 30, 26, 255), INK, 3)
    # 위턱
    up = [(cx - w * 0.40, base - h * 0.80), (cx + w * 0.40, base - h * 0.80), (cx + w * 0.40, base - h * (0.42 + gap)), (cx - w * 0.40, base - h * (0.42 + gap))]
    pen.poly(up, CROC, INK, 3)
    lo = [(cx - w * 0.40, base - h * (0.40 - gap)), (cx + w * 0.40, base - h * (0.40 - gap)), (cx + w * 0.40, base), (cx - w * 0.40, base)]
    pen.poly(lo, CROC_D, INK, 3)
    n = 9
    for k in range(n):
        tx = cx - w * 0.36 + k * w * 0.72 / (n - 1)
        pen.poly([(tx - 8, base - h * (0.42 + gap)), (tx, base - h * (0.42 + gap) + 20), (tx + 8, base - h * (0.42 + gap))], WHITE, INK, 1.5)
        pen.poly([(tx - 8, base - h * (0.40 - gap)), (tx, base - h * (0.40 - gap) - 20), (tx + 8, base - h * (0.40 - gap))], WHITE, INK, 1.5)
    # 눈 두 개
    for sx in (-1, 1):
        pen.ellipse(cx + sx * w * 0.24, base - h * 0.70, w * 0.05, h * 0.05, GOLD, INK, 2)
        pen.ellipse(cx + sx * w * 0.24, base - h * 0.70, w * 0.012, h * 0.04, INK, None, 0)


def sockets(pen, cx, y, w, lit):
    for k in range(4):
        x = cx - w * 0.3 + k * w * 0.2
        pen.ellipse(x, y, 16, 16, GOLD if lit else (90, 70, 50, 255), INK, 2.5)
        pen.ellipse(x, y, 7, 7, (240, 220, 150, 255) if lit else (60, 46, 34, 255), None, 0)


# ================= 벽화 4장 =================
def scene1():
    """물의 주인: 소베크의 몸에서 강이 흘러나오고, 물이 닿는 곳마다 곡식이 자람"""
    bg = bm.wall_base(61)
    pen = Pen(bg)
    bm.cracks(pen, 61)
    bm.borders(pen)
    bm.ground(pen, 600)
    bm.glyph_panel(pen, 52, 64, 2, 5, 54, 61)

    god = blank(); sobek_god(Pen(god), 300, 598, 400)
    river = blank(); rp = Pen(river)
    rp.poly(smooth([(360, 470), (520, 520), (760, 548), (1240, 556), (1240, 600), (700, 600), (430, 560), (360, 500)], 6, closed=True), NILE, INK, 3)
    for k in range(3):
        yy = 520 + k * 22
        rp.line([(x, yy + 10 * (k / 3) + (5 if (x // 26) % 2 else -5)) for x in range(520 + k * 40, 1230, 26)], NILE_L, 2.5)
    grain = blank(); gp = Pen(grain)
    for i, x in enumerate(range(640, 1220, 34)):
        if i % 3:
            wheat(gp, x, 530 - (i % 2) * 4, 110 + (i % 4) * 12)
        else:
            papyrus(gp, x, 532, 120)
    sun = blank(); bm.sun_disk(Pen(sun), 900, 160, 64)
    return bg, {"sun": sun, "grain": grain, "river": river, "god": god}


def scene2():
    """태초의 알: 태초의 물 -> 물가의 악어가 알을 낳음 -> 알이 깨지며 땅과 해가 떠오름"""
    bg = bm.wall_base(62)
    pen = Pen(bg)
    bm.cracks(pen, 62)
    bm.borders(pen)
    bm.glyph_panel(pen, W - 52 - 2 * 54, 64, 2, 4, 54, 62)

    water = blank(); waves(Pen(water), 22, W - 22, 560, 700, NILE, NILE_L, 28, 5)
    mound = blank(); mp = Pen(mound)
    mp.poly(smooth([(560, 600), (640, 520), (780, 488), (920, 520), (1000, 600)], 8) + [(1000, 610), (560, 610)], (176, 132, 80, 255), INK, 3)
    croc = blank(); crocodile(Pen(croc), 300, 560, 460, 1)
    light = blank(); rays(light, 780, 430, 70, 230, 14, (255, 222, 140, 150))
    eg = blank(); egg(Pen(eg), 780, 436, 52, cracked=True)
    sun = blank(); bm.sun_disk(Pen(sun), 780, 170, 56)
    return bg, {"water": water, "light": light, "mound": mound, "egg": eg, "croc": croc, "sun": sun}


def scene3():
    """강을 삼킨 뱀: 밤의 강 위 태양의 배 -> 아펩이 솟아 강을 들이마심 -> 배가 모래 위에 멈춤"""
    bg = bm.wall_base(63, night=True)
    pen = Pen(bg)
    bm.cracks(pen, 63)
    bm.borders(pen)
    bm.stars(pen, 63, 40, 200, 16)
    # 마른 강바닥 (모래 + 갈라진 금)
    pen.rect(22, 560, W - 22, 700, (196, 156, 98, 255), None, 0)
    rng = random.Random(63)
    for _ in range(26):
        x, y = rng.uniform(40, W - 40), rng.uniform(580, 690)
        pen.line([(x, y), (x + rng.uniform(-30, 30), y + rng.uniform(-8, 8)), (x + rng.uniform(-50, 50), y + rng.uniform(-6, 10))], SAND_D, 2)
    bm.ground(pen, 560)

    river = blank(); waves(Pen(river), 22, W - 22, 540, 640, NILE, NILE_L, 26, 4)
    boat = blank(); bm.solar_boat(Pen(boat), 380, 560, 380)
    sun = blank(); bm.sun_disk(Pen(sun), 380, 420, 44)
    apep = blank(); ap = Pen(apep)
    bm.snake_body(ap, [(1270, 300), (1200, 250), (1130, 320), (1060, 260), (990, 340), (930, 440), (880, 524)], 18, 60)
    head = blank(); bm.snake_head(Pen(head), 880, 524, 96, face=-1, tongue=False)
    return bg, {"river": river, "boat": boat, "sun": sun, "apep": apep, "head": head}


def scene4():
    """소베크의 입: 소베크가 마지막 범람을 신전에 몰아넣고 악어 입 문을 잠금 -> 부적 네 개가 빛나면 열림"""
    bg = bm.wall_base(64)
    pen = Pen(bg)
    bm.cracks(pen, 64)
    bm.borders(pen)
    bm.ground(pen, 600)
    bm.glyph_panel(pen, 52, 64, 2, 4, 54, 64)

    gate = blank(); croc_gate(Pen(gate), 820, 598, 420, 400)
    flood = blank(); fp = Pen(flood)
    fp.poly(smooth([(380, 598), (430, 500), (520, 440), (610, 470), (660, 540), (700, 598)], 8), NILE, INK, 3)
    for k in range(3):
        yy = 500 + k * 30
        fp.line([(x, yy + (6 if (x // 22) % 2 else -6)) for x in range(450 + k * 10, 660 - k * 10, 22)], NILE_L, 2.5)
    god = blank(); sobek_god(Pen(god), 250, 598, 380)
    sock = blank(); sockets(Pen(sock), 820, 250, 420, True)
    return bg, {"gate": gate, "flood": flood, "god": god, "sockets": sock}


SCENES = [scene1, scene2, scene3, scene4]
PLAYER_X = [1120, 1130, 130, 1170]   # 올려다보는 장면에서 플레이어가 설 자리 (핵심 그림을 가리지 않게)
PATCHES = [[(1100, 120), (180, 560), (760, 200)], [(150, 520), (1120, 140), (560, 200)],
           [(200, 160), (700, 230), (1150, 620)], [(1120, 140), (420, 200), (150, 540)]]


def save_layer(img_ss, art, layer, positions):
    small = finish(img_ss)
    box = small.getbbox()
    if box is None:
        print("빈 조각:", art, layer)
        return
    crop = small.crop(box)
    p = os.path.join(OUT_MURAL, f"MU_{art}_{layer}.png")
    crop.save(p)
    write_meta(p, "sprite", ppu=100, point=False)
    positions[f"{art}/{layer}"] = ((box[0] + box[2]) / 2.0, (box[1] + box[3]) / 2.0)


def save_bg(img, art):
    out = finish(img) if img.size[0] == W * SS else img
    p = os.path.join(OUT_MURAL, f"MU_{art}_bg.png")
    out.convert("RGB").save(p)
    write_meta(p, "sprite", ppu=100, point=False)
    return out


def pixel_mural(full, path):
    """전실 벽에 걸 작은 도트 벽화 128x64 (청록·금 테두리)"""
    crop = full.crop((0, 40, W, 680)).convert("RGB")
    inner = crop.resize((120, 56), Image.LANCZOS).quantize(colors=40, method=Image.MEDIANCUT, dither=Image.NONE).convert("RGBA")
    out = Image.new("RGBA", (128, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(out)
    d.rectangle([0, 0, 127, 63], fill=(30, 40, 44, 255))
    d.rectangle([1, 1, 126, 62], fill=(70, 132, 128, 255))
    d.rectangle([3, 3, 124, 60], fill=(120, 84, 40, 255))
    out.paste(inner, (4, 4))
    out.save(path)
    write_meta(path, "sprite", ppu=32, pivot=(0.5, 0.0), point=True)


def write_cs(positions):
    lines = [
        "using System.Collections.Generic;",
        "using UnityEngine;",
        "",
        "// [자동 생성] Tools/SobekArt/sobek_murals.py 가 만든 소베크 벽화 조각 위치표 (직접 고치지 말 것)",
        "// 벽화 1280x720 픽셀, 왼쪽 위 0,0 / 조각 그림의 가운데. 그림을 다시 뽑으면 이 파일도 같이 바뀜",
        "public static class SobekMuralArt",
        "{",
        "    private static readonly Dictionary<string, Vector2> Table = new Dictionary<string, Vector2>",
        "    {",
    ]
    for k in sorted(positions):
        x, y = positions[k]
        lines.append(f'        {{ "{k}", new Vector2({x:.1f}f, {y:.1f}f) }},')
    lines += [
        "    };",
        "",
        "    public static Vector2 Pos(string art, string layer)",
        "    {",
        "        if (Table.TryGetValue($\"{art}/{layer}\", out Vector2 p)) return p;",
        "        Debug.LogWarning($\"[소베크 벽화] 위치표에 없음: {art}/{layer}\");",
        "        return new Vector2(640f, 360f);",
        "    }",
        "}",
        "",
    ]
    os.makedirs(os.path.dirname(OUT_CS), exist_ok=True)
    with open(OUT_CS, "w", encoding="utf-8") as f:
        f.write("\n".join(lines))


def main():
    if not os.path.isdir(OUT_MURAL):
        sys.exit("프로젝트 루트에서 실행하세요 (Assets/Resources/Horus/Mural 없음)")
    os.makedirs(OUT_PIXEL, exist_ok=True)
    write_folder_meta(OUT_PIXEL)
    rays_img = bm.horus_layer("rays")
    player = bm.horus_layer("player")
    positions = {}
    previews = []
    for i, fn in enumerate(SCENES, start=1):
        bg, layers = fn()
        s_art, w_art = f"SobekS{i}", f"SobekW{i}"
        save_bg(bg, s_art)
        for name, img in layers.items():
            save_layer(img, s_art, name, positions)
        full_ss = bm.composite(bg, list(layers.values()))
        clean = finish(full_ss)
        p = os.path.join(OUT_MURAL, f"MU_SobekP{i}_bg.png")
        clean.convert("RGB").save(p)
        write_meta(p, "sprite", ppu=100, point=False)
        old = bm.aged(full_ss, 200 + i, PATCHES[i - 1])
        save_bg(old, w_art)
        for nm, im in (("rays", rays_img), ("player", player)):
            p = os.path.join(OUT_MURAL, f"MU_{w_art}_{nm}.png")
            im.save(p)
            write_meta(p, "sprite", ppu=100, point=False)
        positions[f"{w_art}/rays"] = (807.0, 360.0)
        positions[f"{w_art}/player"] = (float(PLAYER_X[i - 1]), 474.5)
        pixel_mural(old, os.path.join(OUT_PIXEL, f"Sobek_Mural{i}.png"))
        if PREVIEW:
            pv = old.copy()
            pv.alpha_composite(rays_img, (int(807 - rays_img.width / 2), int(360 - rays_img.height / 2)))
            pv.alpha_composite(player, (int(PLAYER_X[i - 1] - player.width / 2), int(474.5 - player.height / 2)))
            previews.append((clean, pv))
        print(f"벽화 {i}: 조각 {len(layers)}개")
    write_cs(positions)
    print("위치표:", OUT_CS)
    if PREVIEW and previews:
        sheet = Image.new("RGB", (W * 2, H * len(previews)))
        for r, (a, b) in enumerate(previews):
            sheet.paste(a.convert("RGB"), (0, r * H))
            sheet.paste(b.convert("RGB"), (W, r * H))
        sheet.resize((W, H * len(previews) // 2)).save(PREVIEW)
        px = Image.new("RGBA", (128 * 4 + 30, 64), (40, 40, 40, 255))
        for i in range(4):
            px.alpha_composite(Image.open(os.path.join(OUT_PIXEL, f"Sobek_Mural{i + 1}.png")), (i * 138, 0))
        px.resize((px.width * 3, px.height * 3), Image.NEAREST).save(PREVIEW.replace(".png", "_wall.png"))
        print("미리보기:", PREVIEW)


if __name__ == "__main__":
    main()
