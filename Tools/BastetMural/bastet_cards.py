# 바스테트 서브 퍼즐 유물 4장 수첩 카드 그림 (420x420 jpg) 생성
import math, random, sys
from PIL import Image, ImageDraw, ImageFilter
SRC = sys.argv[1]; OUT = sys.argv[2]
cards = {  # 아이콘 파일 -> (출력 이름, 배경 위 색, 아래 색, 광선 색)
 'Common_Icon_BastetPawSeal':      ('BASTET_PAW_RELIEF',   (92,70,48),  (40,28,20), (255,214,150)),
 'Common_Icon_BastetEyeBead':      ('BASTET_AMBER_EYE',    (34,40,70),  (14,14,28), (255,190,90)),
 'Common_Icon_BastetNecklaceBead': ('BASTET_LAPIS_USEKH',  (40,58,96),  (18,20,40), (255,220,140)),
 'Common_Icon_BastetClawSeal':     ('BASTET_CLAW_TABLET',  (96,66,44),  (36,24,18), (255,200,140)),
}
S = 420
random.seed(7)
for icon, (name, top, bot, ray) in cards.items():
    bg = Image.new('RGB', (S, S))
    d = ImageDraw.Draw(bg)
    for y in range(S):  # 세로 그라데이션
        t = y / (S - 1)
        d.line([(0, y), (S, y)], fill=tuple(int(top[i] * (1 - t) + bot[i] * t) for i in range(3)))
    # 뒤쪽 빛줄기
    rays = Image.new('L', (S, S), 0); rd = ImageDraw.Draw(rays)
    cx, cy = S / 2, S * 0.46
    for k in range(14):
        a0 = k * 2 * math.pi / 14; a1 = a0 + 0.12
        rd.polygon([(cx, cy), (cx + 600 * math.cos(a0), cy + 600 * math.sin(a0)), (cx + 600 * math.cos(a1), cy + 600 * math.sin(a1))], fill=55)
    glow = Image.new('L', (S, S), 0); ImageDraw.Draw(glow).ellipse([cx - 150, cy - 150, cx + 150, cy + 150], fill=140)
    glow = glow.filter(ImageFilter.GaussianBlur(60))
    rays = rays.filter(ImageFilter.GaussianBlur(6))
    mask = Image.blend(rays, glow, 0.5).point(lambda v: min(255, v * 2))
    bg = Image.composite(Image.new('RGB', (S, S), ray), bg, mask.point(lambda v: int(v * 0.55)))
    # 바닥 (모래 받침대)
    d = ImageDraw.Draw(bg)
    sd = Image.new('L', (S, S), 0); ImageDraw.Draw(sd).ellipse([90, 336, 330, 380], fill=150)
    sd = sd.filter(ImageFilter.GaussianBlur(10))
    bg = Image.composite(Image.new('RGB', (S, S), (10, 7, 5)), bg, sd)
    # 아이콘: 바깥 점선 테두리 지우고 확대
    ic = Image.open(f'{SRC}/{icon}.png').convert('RGBA')
    m = Image.new('L', ic.size, 0); ImageDraw.Draw(m).rectangle([15, 15, 113, 113], fill=255)  # 점선 테두리 밖은 투명
    r, g, b, a = ic.split(); ic = Image.merge('RGBA', (r, g, b, Image.composite(a, m, m)))
    ic = ic.crop((10, 10, 118, 118))
    ic = ic.resize((280, 280), Image.LANCZOS)
    sh = Image.new('RGBA', ic.size, (0, 0, 0, 0)); sh.putalpha(ic.split()[3].point(lambda v: int(v * 0.5)))
    sh = sh.filter(ImageFilter.GaussianBlur(8))
    base = bg.convert('RGBA')
    base.alpha_composite(sh, (70 + 8, 55 + 14))
    base.alpha_composite(ic, (70, 55))
    # 가장자리 어둡게 (비네트)
    vig = Image.new('L', (S, S), 0); ImageDraw.Draw(vig).ellipse([-60, -60, S + 60, S + 60], fill=255)
    vig = vig.filter(ImageFilter.GaussianBlur(70))
    out = Image.composite(base.convert('RGB'), Image.new('RGB', (S, S), (8, 6, 4)), vig)
    out.save(f'{OUT}/ACH_{name}.jpg', quality=92)
    print('saved', name)
