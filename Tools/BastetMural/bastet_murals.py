# -*- coding: utf-8 -*-
"""
바스테트의 방 벽화 그림 생성기 (호루스 벽화 컷신과 같은 평면 이집트 벽화 화풍)

만드는 것 (Assets/Resources/Horus/Mural/  <- MuralStage가 읽는 폴더, 파일 이름만 MU_Bastet* 로 구분)
  MU_BastetS{n}_bg / _<조각>   : 벽화 n의 '이야기 연출' 장면 (배경 + 움직이는 조각)
  MU_BastetW{n}_bg / _rays / _player : 벽화 n을 '플레이어 등 뒤에서 올려다보는' 장면
그리고
  Assets/image/Bastet/Story/Bastet_Mural{n}.png : 게임 안 벽에 걸리는 작은 도트 벽화 (128x64, 32 PPU = 4x2칸)
  Assets/Scripts/Puzzles/Bastet/Story/BastetMuralArt.cs : 조각 위치표 (MuralArt와 같은 방식)

실행: python3 Tools/BastetMural/bastet_murals.py   (프로젝트 루트에서)
필요: Pillow, numpy
"""
import math
import os
import random
import shutil
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT_MURAL = os.path.join(ROOT, "Assets", "Resources", "Horus", "Mural")
OUT_PIXEL = os.path.join(ROOT, "Assets", "image", "Bastet", "Story")
OUT_CS = os.path.join(ROOT, "Assets", "Scripts", "Puzzles", "Bastet", "Story", "BastetMuralArt.cs")
PREVIEW = os.environ.get("BASTET_MURAL_PREVIEW")  # 지정하면 합친 미리보기 PNG 저장

W, H = 1280, 720
SS = 2  # 2배로 그린 뒤 줄여서 테두리를 매끈하게

# ---------------- 색 ----------------
INK = (58, 38, 28, 255)
CAT = (38, 33, 44, 255)
CAT_RIM = (92, 82, 108, 255)
GOLD = (224, 172, 62, 255)
GOLD_D = (168, 118, 40, 255)
LAPIS = (38, 62, 140, 255)
LAPIS_D = (26, 40, 96, 255)
RED = (176, 62, 42, 255)
GREEN = (70, 132, 96, 255)
GREEN_D = (46, 96, 66, 255)
SKIN = (186, 112, 64, 255)
WHITE = (238, 232, 214, 255)
SAND = (204, 160, 92, 255)
SAND_D = (120, 84, 48, 255)
BROWN = (120, 72, 40, 255)
WATER = (44, 70, 140, 255)
LINEN = (222, 206, 168, 255)
LINEN_D = (176, 152, 108, 255)

positions = {}  # "art/layer" -> (x, y) 조각 그림 가운데 (벽화 픽셀)


# ================= 그리기 도구 =================
def blank():
    return Image.new("RGBA", (W * SS, H * SS), (0, 0, 0, 0))


class Pen:
    def __init__(self, img):
        self.img = img
        self.d = ImageDraw.Draw(img)

    @staticmethod
    def q(pts):
        return [(x * SS, y * SS) for x, y in pts]

    def poly(self, pts, fill, outline=INK, w=3.0):
        p = self.q(pts)
        self.d.polygon(p, fill=fill)
        if outline is not None and w > 0:
            self.d.line(p + [p[0]], fill=outline, width=max(1, int(w * SS)), joint="curve")

    def line(self, pts, fill=INK, w=3.0):
        self.d.line(self.q(pts), fill=fill, width=max(1, int(w * SS)), joint="curve")

    def thick(self, pts, fill, w, outline=INK, ow=3.0):
        # 테두리 있는 굵은 선 (꼬리, 팔)
        if outline is not None:
            self.d.line(self.q(pts), fill=outline, width=int((w + ow * 2) * SS), joint="curve")
            for x, y in (pts[0], pts[-1]):
                r = (w / 2 + ow)
                self.d.ellipse([(x - r) * SS, (y - r) * SS, (x + r) * SS, (y + r) * SS], fill=outline)
        self.d.line(self.q(pts), fill=fill, width=int(w * SS), joint="curve")
        for x, y in (pts[0], pts[-1]):
            r = w / 2
            self.d.ellipse([(x - r) * SS, (y - r) * SS, (x + r) * SS, (y + r) * SS], fill=fill)

    def ellipse(self, cx, cy, rx, ry, fill, outline=INK, w=3.0):
        box = [(cx - rx) * SS, (cy - ry) * SS, (cx + rx) * SS, (cy + ry) * SS]
        self.d.ellipse(box, fill=fill, outline=outline if w > 0 else None, width=max(1, int(w * SS)) if outline is not None and w > 0 else 0)

    def rect(self, x0, y0, x1, y1, fill, outline=INK, w=3.0):
        self.d.rectangle([x0 * SS, y0 * SS, x1 * SS, y1 * SS], fill=fill, outline=outline if w > 0 else None, width=max(1, int(w * SS)) if outline is not None and w > 0 else 0)


def place(pts, ox, oy, s, face=1):
    """단위 좌표(발 밑 가운데 0,0, 위로 -y) -> 벽화 픽셀"""
    return [(ox + x * s * face, oy + y * s) for x, y in pts]


def smooth(pts, n=6, closed=False):
    """Catmull-Rom으로 부드러운 곡선 점"""
    out = []
    m = len(pts)
    rng = range(m) if closed else range(m - 1)
    for i in rng:
        p0 = pts[(i - 1) % m] if closed else pts[max(i - 1, 0)]
        p1 = pts[i]
        p2 = pts[(i + 1) % m] if closed else pts[min(i + 1, m - 1)]
        p3 = pts[(i + 2) % m] if closed else pts[min(i + 2, m - 1)]
        for k in range(n):
            t = k / n
            t2, t3 = t * t, t * t * t
            x = 0.5 * ((2 * p1[0]) + (-p0[0] + p2[0]) * t + (2 * p0[0] - 5 * p1[0] + 4 * p2[0] - p3[0]) * t2 + (-p0[0] + 3 * p1[0] - 3 * p2[0] + p3[0]) * t3)
            y = 0.5 * ((2 * p1[1]) + (-p0[1] + p2[1]) * t + (2 * p0[1] - 5 * p1[1] + 4 * p2[1] - p3[1]) * t2 + (-p0[1] + 3 * p1[1] - 3 * p2[1] + p3[1]) * t3)
            out.append((x, y))
    if not closed:
        out.append(pts[-1])
    return out


def finish(img):
    """2배 그림 -> 원래 크기"""
    return img.resize((W, H), Image.LANCZOS)


# ================= 인물·동물 =================
CAT_SIT = [(-0.30, 0), (-0.36, -0.12), (-0.355, -0.30), (-0.29, -0.49), (-0.18, -0.63), (-0.155, -0.73), (-0.15, -0.84),
           (-0.125, -1.0), (-0.045, -0.89), (0.035, -0.89), (0.075, -1.0), (0.105, -0.85), (0.145, -0.80), (0.19, -0.745),
           (0.17, -0.705), (0.125, -0.68), (0.09, -0.62), (0.115, -0.50), (0.13, -0.32), (0.135, -0.04), (0.20, -0.03), (0.20, 0)]


def cat_sit(pen, x, y, h, face=1, knife_paw=False):
    """앉은 검은 고양이 (이집트 청동 고양이 모양). x,y = 발 밑 가운데, h = 귀 끝까지 높이"""
    P = lambda pts: place(pts, x, y, h, face)
    # 꼬리 (몸 앞으로 감김)
    pen.thick(P(smooth([(-0.30, -0.03), (-0.12, 0.005), (0.08, 0.005), (0.20, -0.05)], 6)), CAT, h * 0.04)
    pen.poly(P(CAT_SIT), CAT, INK, 3)
    # 뒷다리 허벅지 선, 앞다리 선
    pen.line(P(smooth([(-0.31, -0.08), (-0.25, -0.27), (-0.08, -0.34), (0.02, -0.24)], 6)), CAT_RIM, 2)
    pen.line(P([(0.035, -0.36), (0.04, -0.03)]), CAT_RIM, 2)
    # 귀 안쪽
    pen.poly(P([(-0.11, -0.96), (-0.06, -0.895), (-0.12, -0.88)]), (110, 70, 80, 255), None, 0)
    # 금 귀걸이, 목걸이 + 우자트 펜던트
    pen.ellipse(*P([(0.07, -0.86)])[0], h * 0.016, h * 0.016, None, GOLD, 2.5)
    pen.thick(P(smooth([(-0.165, -0.70), (-0.02, -0.66), (0.10, -0.645)], 5)), GOLD, h * 0.02, INK, 1.5)
    px, py = P([(0.085, -0.615)])[0]
    pen.ellipse(px, py, h * 0.022, h * 0.022, GOLD, INK, 1.5)
    # 눈 (금빛 아몬드 + 세로 동공)
    ex, ey = P([(0.105, -0.795)])[0]
    pen.ellipse(ex, ey, h * 0.028, h * 0.014, GOLD, INK, 1.5)
    pen.line([(ex, ey - h * 0.012), (ex, ey + h * 0.012)], INK, 2)
    # 코·입
    pen.line(P([(0.19, -0.745), (0.17, -0.72), (0.15, -0.715)]), CAT_RIM, 1.5)


def cat_walk(pen, x, y, h, face=1):
    """걷는 고양이 (옆모습). h = 귀 끝까지 높이"""
    P = lambda pts: place(pts, x, y, h, face)
    # 먼 쪽 다리
    pen.thick(P([(0.30, -0.32), (0.34, -0.02)]), (28, 24, 32, 255), h * 0.07, INK, 2)
    pen.thick(P([(-0.40, -0.32), (-0.36, -0.02)]), (28, 24, 32, 255), h * 0.07, INK, 2)
    # 꼬리 (S자로 위로)
    pen.thick(P(smooth([(-0.78, -0.52), (-0.98, -0.58), (-1.06, -0.80), (-0.98, -0.98)], 6)), CAT, h * 0.06)
    body = [(-0.80, -0.55), (-0.40, -0.62), (0.22, -0.60), (0.38, -0.66), (0.42, -0.80), (0.44, -1.0), (0.52, -0.86), (0.58, -0.86),
            (0.63, -1.0), (0.65, -0.80), (0.74, -0.72), (0.72, -0.665), (0.63, -0.61), (0.52, -0.56), (0.47, -0.30), (0.50, 0),
            (0.41, 0), (0.39, -0.27), (0.30, -0.31), (-0.38, -0.32), (-0.47, -0.25), (-0.47, 0), (-0.56, 0), (-0.62, -0.30), (-0.76, -0.40)]
    pen.poly(P(body), CAT, INK, 3)
    pen.thick(P(smooth([(0.37, -0.66), (0.48, -0.60), (0.55, -0.565)], 4)), GOLD, h * 0.03, INK, 1.5)
    ex, ey = P([(0.62, -0.78)])[0]
    pen.ellipse(ex, ey, h * 0.035, h * 0.018, GOLD, INK, 1.5)
    pen.line([(ex, ey - h * 0.015), (ex, ey + h * 0.015)], INK, 2)
    pen.ellipse(*P([(0.55, -0.88)])[0], h * 0.02, h * 0.02, None, GOLD, 2.5)


def bastet(pen, x, y, h, face=1, item="ankh"):
    """고양이 머리 여신 바스테트 (서 있는 옆모습). h = 귀 끝까지 높이"""
    P = lambda pts: place(pts, x, y, h, face)
    # 뒤로 내린 팔 + 시스트럼
    pen.thick(P([(-0.06, -0.71), (-0.10, -0.52), (-0.085, -0.42)]), SKIN, h * 0.032)
    sx, sy = P([(-0.085, -0.40)])[0]
    pen.thick([(sx, sy), (sx, sy + h * 0.10)], GOLD, h * 0.014, INK, 1.5)
    pen.ellipse(sx, sy + h * 0.14, h * 0.03, h * 0.045, None, GOLD, 3)
    # 발
    pen.poly(P([(-0.065, -0.07), (-0.005, -0.07), (0.01, 0), (-0.07, 0)]), SKIN, INK, 2)
    pen.poly(P([(0.0, -0.07), (0.06, -0.07), (0.11, -0.01), (0.105, 0), (0.0, 0)]), SKIN, INK, 2)
    # 드레스 (구슬 그물 무늬)
    dress = [(-0.085, -0.74), (0.075, -0.74), (0.07, -0.55), (0.065, -0.065), (-0.07, -0.065), (-0.08, -0.55)]
    pen.poly(P(dress), GREEN, INK, 3)
    for k in range(1, 9):
        yy = -0.55 + k * 0.055
        pen.line(P([(-0.075, yy), (0.07, yy - 0.03)]), GREEN_D, 1.5)
        pen.line(P([(-0.075, yy - 0.03), (0.07, yy)]), GREEN_D, 1.5)
    pen.thick(P([(-0.08, -0.55), (0.07, -0.55)]), GOLD, h * 0.016, INK, 1.5)
    # 가발 뒷자락 (청금석)
    pen.poly(P([(-0.07, -0.93), (-0.01, -0.95), (0.005, -0.80), (-0.05, -0.70), (-0.10, -0.72)]), LAPIS, INK, 2.5)
    for k in range(4):
        yy = -0.90 + k * 0.045
        pen.line(P([(-0.085, yy + 0.02), (-0.01, yy)]), LAPIS_D, 1.5)
    # 목 + 넓은 목걸이 (금·청·적 띠)
    pen.poly(P([(-0.02, -0.80), (0.035, -0.80), (0.035, -0.73), (-0.02, -0.73)]), SKIN, INK, 2)
    for i, col in enumerate([RED, LAPIS, GOLD]):
        r = 0.10 - i * 0.025
        cx, cy = P([(-0.005, -0.745)])[0]
        pen.d.pieslice([(cx - r * h) * SS, (cy - r * h * 0.85) * SS, (cx + r * h) * SS, (cy + r * h * 0.85) * SS], 0, 180, fill=col, outline=INK, width=int(1.5 * SS))
    # 앞으로 내민 팔 + 앙크
    pen.thick(P([(0.05, -0.72), (0.15, -0.62), (0.235, -0.635)]), SKIN, h * 0.032)
    ax, ay = P([(0.25, -0.64)])[0]
    ankh(pen, ax + h * 0.02 * face, ay + h * 0.06, h * 0.16) if item == "ankh" else None
    # 고양이 머리
    head = [(-0.06, -0.86), (-0.045, -1.0), (0.0, -0.915), (0.03, -0.915), (0.065, -1.0), (0.08, -0.88), (0.105, -0.86),
            (0.135, -0.835), (0.12, -0.80), (0.085, -0.79), (0.05, -0.765), (0.0, -0.755), (-0.05, -0.785)]
    pen.poly(P(head), CAT, INK, 2.5)
    ex, ey = P([(0.075, -0.845)])[0]
    pen.ellipse(ex, ey, h * 0.016, h * 0.008, GOLD, INK, 1.2)
    pen.ellipse(*P([(0.03, -0.90)])[0], h * 0.01, h * 0.01, None, GOLD, 2)


def ankh(pen, x, y, h):
    """앙크. x,y = 아래 끝"""
    pen.thick([(x, y), (x, y - h * 0.62)], GOLD, h * 0.12, INK, 1.5)
    pen.thick([(x - h * 0.22, y - h * 0.62), (x + h * 0.22, y - h * 0.62)], GOLD, h * 0.11, INK, 1.5)
    pen.ellipse(x, y - h * 0.80, h * 0.13, h * 0.18, None, INK, 0)
    pen.d.ellipse([(x - h * 0.13) * SS, (y - h * 0.98) * SS, (x + h * 0.13) * SS, (y - h * 0.62) * SS], outline=INK, width=int(h * 0.14 * SS) + 2)
    pen.d.ellipse([(x - h * 0.13) * SS, (y - h * 0.98) * SS, (x + h * 0.13) * SS, (y - h * 0.62) * SS], outline=GOLD, width=int(h * 0.09 * SS))


def sun_disk(pen, cx, cy, r):
    pen.ellipse(cx, cy, r * 1.08, r * 1.08, GOLD, INK, 3)
    pen.ellipse(cx, cy, r, r, (206, 72, 40, 255), INK, 2)
    pen.ellipse(cx - r * 0.25, cy - r * 0.3, r * 0.35, r * 0.22, (226, 110, 70, 255), None, 0)
    # 우라에우스 (코브라)
    cobra = [(cx + r * 0.0, cy + r * 1.05), (cx + r * 0.12, cy + r * 0.75), (cx + r * 0.05, cy + r * 0.45), (cx + r * 0.18, cy + r * 0.22),
             (cx + r * 0.30, cy + r * 0.30), (cx + r * 0.24, cy + r * 0.52), (cx + r * 0.30, cy + r * 0.85), (cx + r * 0.16, cy + r * 1.08)]
    pen.poly(cobra, GOLD, INK, 2)


def wedjat(pen, cx, cy, w):
    """라의 눈. w = 눈 폭"""
    s = w / 2
    white = smooth([(cx - s, cy), (cx - s * 0.3, cy - s * 0.42), (cx + s * 0.45, cy - s * 0.30), (cx + s, cy + s * 0.02),
                    (cx + s * 0.4, cy + s * 0.32), (cx - s * 0.3, cy + s * 0.30)], 6, closed=True)
    pen.poly(white, WHITE, LAPIS_D, 4)
    pen.ellipse(cx + s * 0.02, cy - s * 0.02, s * 0.28, s * 0.28, LAPIS_D, None, 0)
    pen.ellipse(cx - s * 0.06, cy - s * 0.1, s * 0.07, s * 0.07, WHITE, None, 0)
    pen.thick(smooth([(cx - s * 0.95, cy - s * 0.55), (cx, cy - s * 0.78), (cx + s * 1.0, cy - s * 0.52)], 6), LAPIS_D, s * 0.13, None)
    pen.thick([(cx + s, cy + s * 0.02), (cx + s * 1.45, cy + s * 0.02)], LAPIS_D, s * 0.1, None)
    pen.thick([(cx - s * 0.05, cy + s * 0.30), (cx - s * 0.12, cy + s * 0.95)], LAPIS_D, s * 0.1, None)
    tail = smooth([(cx + s * 0.2, cy + s * 0.32), (cx + s * 0.45, cy + s * 0.9), (cx + s * 0.85, cy + s * 0.95), (cx + s * 0.95, cy + s * 0.7), (cx + s * 0.75, cy + s * 0.62)], 6)
    pen.thick(tail, LAPIS_D, s * 0.08, None)


def snake_body(pen, pts, w0, w1, color=SAND, spots=True, spot_col=(92, 60, 34, 255)):
    """꼬리(w0) -> 머리 쪽(w1)으로 굵어지는 뱀 몸통"""
    c = smooth(pts, 10)
    n = len(c)
    left, right = [], []
    for i, (x, y) in enumerate(c):
        x2, y2 = c[min(i + 1, n - 1)]
        x1, y1 = c[max(i - 1, 0)]
        dx, dy = x2 - x1, y2 - y1
        L = math.hypot(dx, dy) or 1
        nx, ny = -dy / L, dx / L
        t = i / (n - 1)
        wv = (w0 + (w1 - w0) * min(1, t * 1.6)) / 2
        left.append((x + nx * wv, y + ny * wv))
        right.append((x - nx * wv, y - ny * wv))
    pen.poly(left + right[::-1], color, INK, 3)
    # 배 비늘 선
    belly = [((l[0] + r[0]) / 2 * 0.3 + r[0] * 0.7, (l[1] + r[1]) / 2 * 0.3 + r[1] * 0.7) for l, r in zip(left, right)]
    pen.line(belly[3:-3], (230, 200, 140, 255), 2)
    if spots:
        for i in range(6, n - 4, 9):
            x, y = c[i]
            t = i / (n - 1)
            r = (w0 + (w1 - w0) * min(1, t * 1.6)) * 0.22
            pen.ellipse(x, y, r * 1.3, r, spot_col, None, 0)
    return c[-1]


def snake_head(pen, x, y, s, face=1, tongue=True):
    """뱀 머리. x,y = 목 쪽 끝, s = 머리 길이"""
    P = lambda pts: [(x + px * s * face, y + py * s) for px, py in pts]
    pen.poly(P([(-0.1, -0.28), (0.45, -0.38), (0.95, -0.18), (1.0, 0.02), (0.55, 0.20), (-0.1, 0.28)]), SAND, INK, 3)
    pen.line(P([(0.25, 0.04), (0.95, -0.02)]), INK, 2)
    ex, ey = P([(0.55, -0.16)])[0]
    pen.ellipse(ex, ey, s * 0.09, s * 0.08, WHITE, INK, 2)
    pen.ellipse(ex + s * 0.02 * face, ey, s * 0.035, s * 0.06, INK, None, 0)
    if tongue:
        pen.line(P([(1.0, 0.0), (1.3, 0.02), (1.42, -0.06)]), RED, 2.5)
        pen.line(P([(1.3, 0.02), (1.42, 0.09)]), RED, 2.5)


def tree(pen, x, y, h):
    """페르세아 나무. x,y = 밑동"""
    pen.poly([(x - h * 0.05, y), (x - h * 0.035, y - h * 0.55), (x - h * 0.10, y - h * 0.68), (x - h * 0.06, y - h * 0.70),
              (x, y - h * 0.60), (x + h * 0.06, y - h * 0.72), (x + h * 0.10, y - h * 0.69), (x + h * 0.035, y - h * 0.55), (x + h * 0.05, y)],
             BROWN, INK, 3)
    rng = random.Random(4)
    blobs = [(0, -0.82, 0.30, 0.20), (-0.22, -0.74, 0.18, 0.13), (0.22, -0.74, 0.18, 0.13), (-0.12, -0.95, 0.17, 0.12), (0.13, -0.95, 0.17, 0.12)]
    for bx, by, rx, ry in blobs:
        pen.ellipse(x + bx * h, y + by * h, rx * h, ry * h, GREEN, INK, 3)
    for _ in range(26):
        a = rng.random() * math.tau
        rr = rng.random() ** 0.5
        lx, ly = x + math.cos(a) * rr * h * 0.32, y - h * 0.83 + math.sin(a) * rr * h * 0.18
        pen.ellipse(lx, ly, h * 0.022, h * 0.014, GREEN_D, None, 0)
    for _ in range(9):
        a = rng.random() * math.tau
        rr = rng.random() ** 0.5
        pen.ellipse(x + math.cos(a) * rr * h * 0.30, y - h * 0.82 + math.sin(a) * rr * h * 0.16, h * 0.017, h * 0.02, (196, 150, 60, 255), INK, 1.2)


def knife(pen, x, y, h):
    """칼 (손잡이 아래 끝 x,y, 위로 h)"""
    pen.poly([(x - h * 0.05, y), (x + h * 0.05, y), (x + h * 0.045, y - h * 0.30), (x - h * 0.045, y - h * 0.30)], BROWN, INK, 2)
    blade = smooth([(x - h * 0.06, y - h * 0.30), (x - h * 0.09, y - h * 0.65), (x - h * 0.02, y - h * 1.0), (x + h * 0.08, y - h * 0.62), (x + h * 0.06, y - h * 0.30)], 5)
    pen.poly(blade, (214, 216, 214, 255), INK, 2.5)
    pen.line([(x, y - h * 0.32), (x - h * 0.02, y - h * 0.9)], (250, 250, 248, 255), 2)


def mummy_cat(pen, x, y, h, seed=0):
    """고양이 미라 (서 있음). x,y = 발 밑 가운데"""
    rng = random.Random(seed)
    w = h * 0.30
    body = smooth([(x - w * 0.42, y), (x - w * 0.55, y - h * 0.35), (x - w * 0.50, y - h * 0.62), (x - w * 0.33, y - h * 0.70),
                   (x + w * 0.33, y - h * 0.70), (x + w * 0.50, y - h * 0.62), (x + w * 0.55, y - h * 0.35), (x + w * 0.42, y)], 5, closed=True)
    pen.poly(body, LINEN, INK, 3)
    # 리넨 띠 (사선 무늬)
    for k in range(9):
        yy = y - h * 0.05 - k * h * 0.07
        pen.line([(x - w * 0.48, yy), (x + w * 0.48, yy - h * 0.05)], LINEN_D, 2)
    pen.thick([(x - w * 0.5, y - h * 0.40), (x + w * 0.5, y - h * 0.40)], (150, 70, 50, 255), h * 0.03, INK, 1)
    # 머리 + 귀 + 칠한 얼굴
    hx, hy, hr = x, y - h * 0.80, w * 0.42
    pen.poly([(hx - hr * 0.9, hy - hr * 0.2), (hx - hr * 0.75, hy - hr * 1.6), (hx - hr * 0.2, hy - hr * 0.85),
              (hx + hr * 0.2, hy - hr * 0.85), (hx + hr * 0.75, hy - hr * 1.6), (hx + hr * 0.9, hy - hr * 0.2)], (98, 72, 48, 255), INK, 2.5)
    pen.ellipse(hx, hy, hr, hr * 0.92, (120, 88, 58, 255), INK, 2.5)
    for sx in (-1, 1):
        pen.ellipse(hx + sx * hr * 0.42, hy - hr * 0.1, hr * 0.22, hr * 0.13, WHITE, INK, 1.5)
        pen.ellipse(hx + sx * hr * 0.42, hy - hr * 0.1, hr * 0.08, hr * 0.11, INK, None, 0)
    pen.line([(hx - hr * 0.12, hy + hr * 0.3), (hx, hy + hr * 0.42), (hx + hr * 0.12, hy + hr * 0.3)], INK, 2)


def human_sleep(pen, x, y, s):
    """침대에 누워 자는 사람 (사자 다리 침대). x,y = 침대 가운데 바닥"""
    # 침대
    pen.poly([(x - s * 1.1, y - s * 0.42), (x + s * 1.0, y - s * 0.50), (x + s * 1.0, y - s * 0.40), (x - s * 1.1, y - s * 0.32)], BROWN, INK, 2.5)
    for lx in (-0.95, 0.85):
        pen.poly([(x + lx * s - s * 0.06, y - s * 0.33), (x + lx * s + s * 0.06, y - s * 0.33), (x + lx * s + s * 0.08, y), (x + lx * s - s * 0.10, y)], BROWN, INK, 2)
    # 머리 받침, 이불, 머리
    pen.poly([(x - s * 1.0, y - s * 0.42), (x - s * 0.82, y - s * 0.44), (x - s * 0.84, y - s * 0.62), (x - s * 0.98, y - s * 0.60)], GOLD, INK, 2)
    pen.poly(smooth([(x - s * 0.80, y - s * 0.48), (x - s * 0.5, y - s * 0.66), (x + s * 0.2, y - s * 0.62), (x + s * 0.9, y - s * 0.56), (x + s * 0.92, y - s * 0.46)], 5)
             + [(x - s * 0.80, y - s * 0.44)], WHITE, INK, 2.5)
    pen.ellipse(x - s * 0.90, y - s * 0.70, s * 0.13, s * 0.11, SKIN, INK, 2)
    pen.poly([(x - s * 1.02, y - s * 0.80), (x - s * 0.80, y - s * 0.80), (x - s * 0.78, y - s * 0.62), (x - s * 1.02, y - s * 0.60)], (30, 30, 40, 255), INK, 2)


def child_sit(pen, x, y, s):
    """앉아 있는 아이 (옆머리 땋은 머리). x,y = 엉덩이 밑"""
    pen.poly([(x - s * 0.15, y), (x + s * 0.35, y), (x + s * 0.38, y - s * 0.12), (x + s * 0.10, y - s * 0.12), (x + s * 0.08, y - s * 0.45),
              (x - s * 0.14, y - s * 0.45)], SKIN, INK, 2.5)
    pen.thick([(x + s * 0.02, y - s * 0.40), (x + s * 0.25, y - s * 0.30)], SKIN, s * 0.07)
    pen.ellipse(x - s * 0.02, y - s * 0.58, s * 0.14, s * 0.14, SKIN, INK, 2.5)
    pen.thick(smooth([(x - s * 0.14, y - s * 0.62), (x - s * 0.22, y - s * 0.52), (x - s * 0.18, y - s * 0.40)], 4), (30, 30, 40, 255), s * 0.05, INK, 1)


def house(pen, x, y, s):
    """곡식 창고(돔 2개) + 항아리 3개. x,y = 왼쪽 아래"""
    for i in range(2):
        cx = x + s * (0.35 + i * 0.75)
        dome = smooth([(cx - s * 0.32, y), (cx - s * 0.33, y - s * 0.55), (cx, y - s * 0.95), (cx + s * 0.33, y - s * 0.55), (cx + s * 0.32, y)], 6)
        pen.poly(dome, (214, 182, 128, 255), INK, 3)
        pen.rect(cx - s * 0.07, y - s * 0.80, cx + s * 0.07, y - s * 0.68, (60, 40, 30, 255), INK, 2)
        for k in range(4):
            pen.ellipse(cx + (k - 1.5) * s * 0.11, y - s * 0.22, s * 0.04, s * 0.06, (230, 196, 90, 255), INK, 1.2)
    for i in range(3):
        jx = x + s * (1.70 + i * 0.30)
        pen.poly(smooth([(jx - s * 0.06, y), (jx - s * 0.13, y - s * 0.18), (jx - s * 0.08, y - s * 0.34), (jx - s * 0.05, y - s * 0.40),
                         (jx + s * 0.05, y - s * 0.40), (jx + s * 0.08, y - s * 0.34), (jx + s * 0.13, y - s * 0.18), (jx + s * 0.06, y)], 4, closed=True),
                 (RED if i % 2 == 0 else (196, 132, 70, 255)), INK, 2.5)
        pen.line([(jx - s * 0.10, y - s * 0.24), (jx + s * 0.10, y - s * 0.24)], (240, 220, 180, 255), 1.5)


def lamp(pen, x, y, s):
    """기름 등잔 (받침). x,y = 바닥"""
    pen.poly([(x - s * 0.04, y), (x + s * 0.04, y), (x + s * 0.03, y - s * 0.45), (x - s * 0.03, y - s * 0.45)], GOLD_D, INK, 2)
    pen.poly([(x - s * 0.16, y - s * 0.45), (x + s * 0.16, y - s * 0.45), (x + s * 0.10, y - s * 0.55), (x - s * 0.10, y - s * 0.55)], GOLD, INK, 2)


def flame(pen, x, y, s):
    pen.poly(smooth([(x - s * 0.06, y), (x - s * 0.07, y - s * 0.08), (x, y - s * 0.22), (x + s * 0.07, y - s * 0.08), (x + s * 0.06, y)], 5, closed=True), (246, 170, 60, 255), (200, 90, 40, 255), 2)
    pen.poly(smooth([(x - s * 0.03, y), (x, y - s * 0.12), (x + s * 0.03, y)], 4, closed=True), (255, 236, 160, 255), None, 0)


def offering_table(pen, x, y, s):
    pen.poly([(x - s * 0.05, y), (x + s * 0.05, y), (x + s * 0.04, y - s * 0.45), (x - s * 0.04, y - s * 0.45)], (196, 150, 90, 255), INK, 2.5)
    pen.rect(x - s * 0.30, y - s * 0.50, x + s * 0.30, y - s * 0.44, (196, 150, 90, 255), INK, 2.5)
    pen.ellipse(x - s * 0.14, y - s * 0.57, s * 0.12, s * 0.07, (214, 160, 80, 255), INK, 2)
    pen.ellipse(x + s * 0.12, y - s * 0.56, s * 0.10, s * 0.06, (232, 190, 110, 255), INK, 2)
    # 연꽃
    pen.thick(smooth([(x + s * 0.02, y - s * 0.62), (x + s * 0.06, y - s * 0.80), (x + s * 0.0, y - s * 0.95)], 4), GREEN, s * 0.025, INK, 1)
    pen.poly([(x - s * 0.06, y - s * 0.95), (x, y - s * 1.08), (x + s * 0.06, y - s * 0.95), (x, y - s * 0.92)], (120, 150, 220, 255), INK, 1.5)


# ================= 벽 바탕 =================
def wall_base(seed, night=False):
    rng = np.random.default_rng(seed)
    y = np.linspace(0, 1, H)[:, None]
    top = np.array([216, 193, 152]) if not night else np.array([206, 180, 140])
    bot = np.array([196, 168, 124]) if not night else np.array([180, 150, 108])
    base = top + (bot - top) * y
    base = np.repeat(base[:, None, :], W, axis=1).reshape(H, W, 3)
    low = rng.normal(0, 1, (H // 24 + 2, W // 24 + 2))
    low = np.array(Image.fromarray(((low - low.min()) / (np.ptp(low) + 1e-6) * 255).astype(np.uint8)).resize((W, H), Image.BICUBIC)) / 255.0 - 0.5
    grain = rng.normal(0, 1, (H, W)) * 3.0
    arr = base + low[:, :, None] * 22 + grain[:, :, None]
    img = Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8)).convert("RGBA")
    return img.resize((W * SS, H * SS), Image.BICUBIC)


def cracks(pen, seed, count=7):
    rng = random.Random(seed)
    for _ in range(count):
        x, y = rng.uniform(40, W - 40), rng.uniform(60, H - 120)
        pts = [(x, y)]
        a = rng.uniform(0, math.tau)
        for _ in range(rng.randint(5, 10)):
            a += rng.uniform(-0.7, 0.7)
            x += math.cos(a) * rng.uniform(14, 34)
            y += math.sin(a) * rng.uniform(10, 26)
            pts.append((x, y))
        pen.line(pts, (128, 98, 70, 200), 1.6)


BAND = [LAPIS, GOLD, RED, GOLD, GREEN, GOLD]


def borders(pen):
    for y0 in (0, H - 20):
        pen.rect(0, y0, W, y0 + 20, (60, 40, 30, 255), None, 0)
        x, i = 2, 0
        while x < W:
            pen.rect(x, y0 + 3, x + 20, y0 + 17, BAND[i % len(BAND)], None, 0)
            x += 24
            i += 1


def ground(pen, y):
    pen.thick([(0, y), (W, y)], (88, 62, 40, 255), 4, None)
    pen.thick([(0, y + 12), (W, y + 12)], (150, 110, 70, 255), 3, None)


GLYPHS = ["sun", "cat", "ankh", "eye", "water", "reed", "bread", "snake", "knife", "house", "star", "mummy"]


def glyph(pen, kind, x, y, s):
    if kind == "sun":
        pen.ellipse(x, y, s * 0.32, s * 0.32, RED, INK, 2)
        pen.ellipse(x, y, s * 0.10, s * 0.10, GOLD, None, 0)
    elif kind == "cat":
        pen.poly(place(CAT_SIT, x, y + s * 0.42, s * 0.8), LAPIS_D, None, 0)
    elif kind == "ankh":
        ankh(pen, x, y + s * 0.42, s * 0.85)
    elif kind == "eye":
        pen.ellipse(x, y, s * 0.36, s * 0.16, WHITE, LAPIS_D, 2)
        pen.ellipse(x, y, s * 0.12, s * 0.12, LAPIS_D, None, 0)
        pen.line([(x - s * 0.05, y + s * 0.15), (x - s * 0.08, y + s * 0.40)], LAPIS_D, 2)
    elif kind == "water":
        for k in range(3):
            yy = y - s * 0.2 + k * s * 0.2
            pen.line([(x - s * 0.36 + j * s * 0.12, yy + (s * 0.06 if j % 2 else -s * 0.06)) for j in range(7)], LAPIS, 2)
    elif kind == "reed":
        pen.poly([(x - s * 0.05, y + s * 0.4), (x + s * 0.05, y + s * 0.4), (x + s * 0.12, y - s * 0.2), (x, y - s * 0.42), (x - s * 0.08, y - s * 0.15)], GREEN, INK, 1.5)
    elif kind == "bread":
        pen.d.pieslice([(x - s * 0.3) * SS, (y - s * 0.25) * SS, (x + s * 0.3) * SS, (y + s * 0.35) * SS], 180, 360, fill=GOLD, outline=INK, width=2 * SS)
    elif kind == "snake":
        pen.line(smooth([(x - s * 0.35, y + s * 0.2), (x - s * 0.15, y - s * 0.05), (x + s * 0.05, y + s * 0.2), (x + s * 0.25, y - s * 0.05), (x + s * 0.32, y - s * 0.25)], 4), SAND_D, 3)
    elif kind == "knife":
        pen.line([(x - s * 0.25, y + s * 0.25), (x + s * 0.25, y - s * 0.3)], (110, 110, 120, 255), 4)
    elif kind == "house":
        pen.rect(x - s * 0.3, y - s * 0.2, x + s * 0.3, y + s * 0.3, None, RED, 2.5)
    elif kind == "star":
        for a in range(5):
            ang = a * math.tau / 5 - math.pi / 2
            pen.line([(x, y), (x + math.cos(ang) * s * 0.32, y + math.sin(ang) * s * 0.32)], LAPIS, 2.5)
    elif kind == "mummy":
        pen.ellipse(x, y, s * 0.14, s * 0.38, LINEN_D, INK, 1.5)


def glyph_panel(pen, x0, y0, cols, rows, cell, seed):
    rng = random.Random(seed)
    for c in range(cols + 1):
        pen.line([(x0 + c * cell, y0), (x0 + c * cell, y0 + rows * cell)], (88, 62, 40, 255), 1.5)
    for c in range(cols):
        for r in range(rows):
            glyph(pen, rng.choice(GLYPHS), x0 + c * cell + cell / 2, y0 + r * cell + cell / 2, cell * 0.8)


def stars(pen, seed, y0, y1, n=18):
    rng = random.Random(seed)
    for _ in range(n):
        glyph(pen, "star", rng.uniform(60, W - 60), rng.uniform(y0, y1), 18)


# ================= 낡은 벽 효과 (올려다보는 장면용) =================
def aged(img, seed, spots):
    """찢긴 자국 + 가장자리 어둡게 + 그을음"""
    rng = random.Random(seed)
    pen = Pen(img)
    for cx, cy in spots:
        r = rng.uniform(40, 62)
        pts = []
        for k in range(16):
            a = k / 16 * math.tau
            rr = r * rng.uniform(0.6, 1.25)
            pts.append((cx + math.cos(a) * rr * 1.4, cy + math.sin(a) * rr))
        pen.poly(pts, (160, 132, 98, 255), (228, 208, 172, 255), 3)
        for _ in range(30):
            pen.ellipse(cx + rng.uniform(-r, r), cy + rng.uniform(-r * 0.6, r * 0.6), 2.5, 2.5, (140, 112, 84, 255), None, 0)
    cracks(pen, seed + 9, 6)
    small = finish(img)
    arr = np.asarray(small).astype(np.float32)
    yy, xx = np.mgrid[0:H, 0:W]
    d = np.sqrt(((xx - W / 2) / (W / 2)) ** 2 + ((yy - H / 2) / (H / 2)) ** 2)
    shade = np.clip(1.0 - (d - 0.55) * 0.55, 0.55, 1.0)
    soot = np.exp(-(((xx - rng.uniform(300, 900)) / 260.0) ** 2 + ((yy - 200) / 160.0) ** 2)) * 0.22
    arr[:, :, :3] *= (shade - soot)[:, :, None]
    return Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8))


# ================= 저장 =================
def save_layer(img_ss, art, layer):
    small = finish(img_ss)
    box = small.getbbox()
    if box is None:
        print("빈 조각:", art, layer)
        return
    crop = small.crop(box)
    crop.save(os.path.join(OUT_MURAL, f"MU_{art}_{layer}.png"))
    positions[f"{art}/{layer}"] = ((box[0] + box[2]) / 2.0, (box[1] + box[3]) / 2.0)


def save_bg(img, art):
    out = finish(img) if img.size[0] == W * SS else img
    out.convert("RGB").save(os.path.join(OUT_MURAL, f"MU_{art}_bg.png"))
    return out


def composite(bg_ss, layers_ss):
    out = bg_ss.copy()
    for l in layers_ss:
        out.alpha_composite(l)
    return out


# ================= 벽화 4장 =================
def scene1():
    """라의 눈: 태양 원반 아래 라의 눈에서 딸(바스테트)이 태어난다"""
    bg = wall_base(11)
    pen = Pen(bg)
    cracks(pen, 1)
    borders(pen)
    ground(pen, 600)
    glyph_panel(pen, 52, 64, 3, 5, 54, 1)
    glyph_panel(pen, W - 52 - 3 * 54, 64, 3, 4, 54, 2)

    sun = blank(); sun_disk(Pen(sun), 640, 150, 78)
    eye = blank(); wedjat(Pen(eye), 600, 320, 300)
    beam = blank()
    bp = Pen(beam)
    bp.poly([(818, 312), (868, 262), (1000, 596), (830, 596)], (255, 226, 150, 95), None, 0)
    goddess = blank(); bastet(Pen(goddess), 900, 598, 300)
    cat = blank(); cat_sit(Pen(cat), 1040, 598, 150)
    layers = {"sun": sun, "eye": eye, "beam": beam, "goddess": goddess, "cat": cat}
    return bg, layers


def scene2():
    """나무 아래의 칼: 페르세아 나무 아래 고양이가 칼로 아펩을 벤다"""
    bg = wall_base(22, night=True)
    pen = Pen(bg)
    cracks(pen, 2)
    borders(pen)
    stars(pen, 3, 40, 170)
    ground(pen, 600)
    tree(pen, 470, 598, 420)
    glyph_panel(pen, 52, 64, 2, 6, 54, 3)

    apep = blank()
    ap = Pen(apep)
    body = [(1250, 560), (1180, 520), (1110, 572), (1040, 520), (970, 566), (900, 520), (850, 470), (830, 420)]
    snake_body(ap, body, 14, 46)
    head = blank(); snake_head(Pen(head), 832, 420, 70, face=-1)
    cat = blank(); cat_sit(Pen(cat), 640, 598, 250)
    kn = blank(); knife(Pen(kn), 720, 470, 150)
    layers = {"apep": apep, "head": head, "cat": cat, "knife": kn}
    return bg, layers


def scene3():
    """집을 지키는 고양이: 곡식 창고, 잠든 사람, 아이 - 숨어드는 뱀을 고양이가 쫓는다"""
    bg = wall_base(33)
    pen = Pen(bg)
    cracks(pen, 3)
    borders(pen)
    ground(pen, 600)
    house(pen, 40, 598, 190)
    lamp(pen, 520, 598, 160)
    human_sleep(pen, 700, 598, 140)
    child_sit(pen, 870, 598, 150)
    glyph_panel(pen, W - 52 - 3 * 54, 64, 3, 3, 54, 4)

    fl = blank(); flame(Pen(fl), 520, 598 - 160 * 0.55, 160)
    cat = blank(); cat_walk(Pen(cat), 1070, 598, 130)
    snake = blank()
    sp = Pen(snake)
    snake_body(sp, [(1272, 596), (1252, 584), (1236, 596), (1222, 580), (1212, 562)], 6, 18, color=(170, 150, 80, 255))
    snake_head(sp, 1212, 562, 30, face=-1)
    layers = {"flame": fl, "cat": cat, "snake": snake}
    return bg, layers


def scene4():
    """잠든 고양이들: 여신 곁에 나란히 선 고양이 미라"""
    bg = wall_base(44)
    pen = Pen(bg)
    cracks(pen, 4)
    borders(pen)
    stars(pen, 5, 40, 150, 10)
    ground(pen, 600)
    offering_table(pen, 470, 598, 230)
    pen.rect(580, 586, 1210, 600, (150, 110, 70, 255), INK, 2.5)  # 미라 받침
    glyph_panel(pen, 52, 64, 2, 4, 54, 5)

    goddess = blank(); bastet(Pen(goddess), 300, 598, 330)
    layers = {"goddess": goddess}
    for i in range(5):
        m = blank()
        mummy_cat(Pen(m), 650 + i * 128, 586, 180 + (i % 2) * 14, seed=i)
        layers[f"mummy{i + 1}"] = m
    return bg, layers


SCENES = [scene1, scene2, scene3, scene4]
PLAYER_X = [250, 200, 250, 1185]
PATCHES = [[(1010, 110), (150, 500), (700, 530)], [(1100, 120), (190, 560), (760, 170)],
           [(450, 150), (800, 300), (1150, 250)], [(560, 170), (1000, 230), (110, 540)]]  # 올려다보는 장면에서 플레이어가 설 자리 (핵심 그림을 가리지 않게)


def horus_layer(name):
    return Image.open(os.path.join(OUT_MURAL, f"MU_Wall1_{name}.png")).convert("RGBA")


def pixel_mural(full, path):
    """게임 벽에 걸 작은 도트 벽화 128x64 (금 테두리)"""
    crop = full.crop((0, 40, W, 680)).convert("RGB")
    inner = crop.resize((120, 56), Image.LANCZOS).quantize(colors=40, method=Image.MEDIANCUT, dither=Image.NONE).convert("RGBA")
    out = Image.new("RGBA", (128, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(out)
    d.rectangle([0, 0, 127, 63], fill=(52, 36, 26, 255))
    d.rectangle([1, 1, 126, 62], fill=(214, 164, 62, 255))
    d.rectangle([3, 3, 124, 60], fill=(120, 84, 40, 255))
    out.paste(inner, (4, 4))
    out.save(path)


def main():
    if not os.path.isdir(OUT_MURAL):
        sys.exit("프로젝트 루트에서 실행하세요 (Assets/Resources/Horus/Mural 없음)")
    os.makedirs(OUT_PIXEL, exist_ok=True)
    rays, player = horus_layer("rays"), horus_layer("player")
    previews = []
    for i, fn in enumerate(SCENES, start=1):
        bg, layers = fn()
        s_art, w_art = f"BastetS{i}", f"BastetW{i}"
        # 이야기 장면: 배경 + 조각
        save_bg(bg, s_art)
        for name, img in layers.items():
            save_layer(img, s_art, name)
        # 올려다보는 장면: 모든 조각을 붙인 낡은 벽화 + 빛줄기 + 플레이어 뒷모습
        full_ss = composite(bg, list(layers.values()))
        clean = finish(full_ss)
        old = aged(full_ss, 100 + i, PATCHES[i - 1])
        save_bg(old, w_art)
        rays.save(os.path.join(OUT_MURAL, f"MU_{w_art}_rays.png"))
        player.save(os.path.join(OUT_MURAL, f"MU_{w_art}_player.png"))
        positions[f"{w_art}/rays"] = (807.0, 360.0)
        positions[f"{w_art}/player"] = (float(PLAYER_X[i - 1]), 474.5)
        pixel_mural(old, os.path.join(OUT_PIXEL, f"Bastet_Mural{i}.png"))
        if PREVIEW:
            pv = old.copy()
            pv.alpha_composite(rays, (int(807 - rays.width / 2), int(360 - rays.height / 2)))
            pv.alpha_composite(player, (int(PLAYER_X[i - 1] - player.width / 2), int(474.5 - player.height / 2)))
            previews.append((clean, pv))
        print(f"벽화 {i}: 조각 {len(layers)}개")
    write_cs()
    if PREVIEW and previews:
        sheet = Image.new("RGB", (W * 2, H * len(previews)))
        for r, (a, b) in enumerate(previews):
            sheet.paste(a.convert("RGB"), (0, r * H))
            sheet.paste(b.convert("RGB"), (W, r * H))
        sheet.resize((W, H * len(previews) // 2)).save(PREVIEW)
        print("미리보기:", PREVIEW)


def write_cs():
    lines = [
        "using System.Collections.Generic;",
        "using UnityEngine;",
        "",
        "// [자동 생성] Tools/BastetMural/bastet_murals.py 가 만든 바스테트 벽화 조각 위치표 (직접 고치지 말 것)",
        "// 벽화 1280x720 픽셀, 왼쪽 위 0,0 / 조각 그림의 가운데. 그림을 다시 뽑으면 이 파일도 같이 바뀜",
        "public static class BastetMuralArt",
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
        "        Debug.LogWarning($\"[바스테트 벽화] 위치표에 없음: {art}/{layer}\");",
        "        return new Vector2(640f, 360f);",
        "    }",
        "}",
        "",
    ]
    with open(OUT_CS, "w", encoding="utf-8") as f:
        f.write("\n".join(lines))
    print("위치표:", OUT_CS)


if __name__ == "__main__":
    main()
