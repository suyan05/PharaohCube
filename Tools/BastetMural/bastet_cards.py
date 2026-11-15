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

# ---- [추가] 히든 유물: 설화석고 향유 그릇 (아이콘이 없어 직접 그림) ----
def vessel_card():
    S2 = 840  # 2배로 그린 뒤 줄여 부드럽게
    bg = Image.new('RGB', (S2, S2)); d = ImageDraw.Draw(bg)
    top, bot = (70, 52, 40), (22, 15, 11)
    for y in range(S2):
        t = y / (S2 - 1)
        d.line([(0, y), (S2, y)], fill=tuple(int(top[i] * (1 - t) + bot[i] * t) for i in range(3)))
    glow = Image.new('L', (S2, S2), 0); ImageDraw.Draw(glow).ellipse([170, 80, 670, 580], fill=170)
    glow = glow.filter(ImageFilter.GaussianBlur(110))
    bg = Image.composite(Image.new('RGB', (S2, S2), (255, 196, 120)), bg, glow.point(lambda v: int(v * 0.6)))
    d = ImageDraw.Draw(bg)
    sd = Image.new('L', (S2, S2), 0); ImageDraw.Draw(sd).ellipse([220, 690, 620, 760], fill=170)
    bg = Image.composite(Image.new('RGB', (S2, S2), (10, 7, 5)), bg, sd.filter(ImageFilter.GaussianBlur(18)))
    d = ImageDraw.Draw(bg)
    ink = (60, 40, 28); body = (236, 224, 198); shade = (200, 182, 150)
    # 받침
    d.rectangle([330, 650, 510, 700], fill=shade, outline=ink, width=6)
    # 몸통
    d.ellipse([230, 360, 610, 680], fill=body, outline=ink, width=8)
    d.chord([230, 360, 610, 680], 20, 160, fill=shade)
    d.arc([230, 360, 610, 680], 0, 360, fill=ink, width=8)
    # 목, 입구
    d.rectangle([340, 300, 500, 380], fill=body, outline=ink, width=8)
    d.ellipse([310, 270, 530, 320], fill=(170, 150, 120), outline=ink, width=8)
    # 고양이 귀 손잡이
    d.polygon([(250, 430), (230, 300), (330, 380)], fill=body, outline=ink)
    d.polygon([(590, 430), (610, 300), (510, 380)], fill=body, outline=ink)
    d.line([(250, 430), (230, 300), (330, 380)], fill=ink, width=8)
    d.line([(590, 430), (610, 300), (510, 380)], fill=ink, width=8)
    # 금 띠와 눈 무늬
    d.line([(255, 470), (585, 470)], fill=(212, 160, 60), width=14)
    d.ellipse([385, 520, 455, 570], fill=(40, 90, 140), outline=ink, width=6)
    d.ellipse([410, 530, 430, 560], fill=ink)
    # 불꽃
    fl = Image.new('L', (S2, S2), 0); fd = ImageDraw.Draw(fl)
    fd.polygon([(420, 120), (370, 230), (400, 285), (440, 285), (470, 230)], fill=255)
    bg = Image.composite(Image.new('RGB', (S2, S2), (255, 170, 60)), bg, fl.filter(ImageFilter.GaussianBlur(4)))
    fl2 = Image.new('L', (S2, S2), 0); ImageDraw.Draw(fl2).polygon([(420, 175), (395, 245), (420, 280), (445, 245)], fill=255)
    bg = Image.composite(Image.new('RGB', (S2, S2), (255, 238, 170)), bg, fl2.filter(ImageFilter.GaussianBlur(3)))
    out = bg.resize((S, S), Image.LANCZOS)
    vig = Image.new('L', (S, S), 0); ImageDraw.Draw(vig).ellipse([-60, -60, S + 60, S + 60], fill=255)
    out = Image.composite(out, Image.new('RGB', (S, S), (8, 6, 4)), vig.filter(ImageFilter.GaussianBlur(70)))
    out.save(f'{OUT}/ACH_BASTET_ALABASTER_VESSEL.jpg', quality=92)
    print('saved BASTET_ALABASTER_VESSEL')

vessel_card()
