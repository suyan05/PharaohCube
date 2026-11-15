# -*- coding: utf-8 -*-
"""
바스테트 숨은 방(향실) 벽화 컷 그림 만들기 - 기존 바스테트 벽화 조각을 다시 써서 조합
만드는 것 (Assets/Resources/Horus/Mural/):
  MU_BastetV1_bg      : 향과 기름 그릇이 놓인 벽 (S3 배경을 어둡게 + 가운데 빛)
  MU_BastetV1_rays    : 빛줄기 (W1과 같음)
  MU_BastetV1_player  : 등 뒤에서 본 플레이어 (W1과 같음)
  MU_BastetV1_goddess : 손을 내민 바스테트 (E2와 같음)
  MU_BastetV1_cat     : 검은 고양이 (E2와 같음)
  MU_BastetV1_vessel  : 고양이 귀 향유 그릇 (새로 그림)
  MU_BastetV1_glow    : 그릇 뒤 둥근 빛
위치는 BastetVaultScenes.cs 의 Pos 값과 맞춰야 함
실행: python3 Tools/BastetVault/bastet_vault_art.py  (프로젝트 루트에서)
"""
import os
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
M = os.path.join(ROOT, "Assets", "Resources", "Horus", "Mural")
W, H = 1280, 720
INK = (58, 38, 28, 255)

def load(n): return Image.open(os.path.join(M, n + ".png")).convert("RGBA")
def save(im, n): im.save(os.path.join(M, "MU_BastetV1_" + n + ".png")); print("saved", n, im.size)

# ---- 배경: S3 배경을 어둡게, 가운데만 따뜻하게 ----
bg = load("MU_BastetS3_bg")
dark = Image.new("RGBA", (W, H), (20, 12, 8, 255))
mask = Image.new("L", (W, H), 0)
ImageDraw.Draw(mask).ellipse([420, 60, 1060, 700], fill=255)
mask = mask.filter(ImageFilter.GaussianBlur(120))
bg = Image.composite(bg, Image.blend(bg, dark, 0.72), mask)
warm = Image.new("RGBA", (W, H), (255, 190, 110, 255))
wm = Image.new("L", (W, H), 0); ImageDraw.Draw(wm).ellipse([520, 180, 1000, 560], fill=70)
bg = Image.composite(warm, bg, wm.filter(ImageFilter.GaussianBlur(90)))
# 바닥에서 피어오르는 향 연기 몇 줄
smoke = Image.new("RGBA", (W, H), (0, 0, 0, 0)); sd = ImageDraw.Draw(smoke)
for i, x0 in enumerate([600, 700, 820]):
    pts = [(x0 + 18 * ((k % 4) - 1.5), 600 - k * 40) for k in range(12)]
    sd.line(pts, fill=(235, 225, 210, 70), width=10, joint="curve")
bg.alpha_composite(smoke.filter(ImageFilter.GaussianBlur(6)))
save(bg, "bg")

# ---- 다시 쓰는 조각 ----
save(load("MU_BastetW1_rays"), "rays")
save(load("MU_BastetW1_player"), "player")
save(load("MU_BastetE2_goddess"), "goddess")
save(load("MU_BastetE2_cat"), "cat")

# ---- 새 조각: 그릇 (2배로 그린 뒤 줄임) ----
S = 2
v = Image.new("RGBA", (150 * S, 170 * S), (0, 0, 0, 0)); d = ImageDraw.Draw(v)
body, shade, gold, lapis = (238, 228, 204, 255), (204, 186, 152, 255), (224, 172, 62, 255), (38, 62, 140, 255)
def P(*xy): return [c * S for c in xy]
d.polygon(P(28, 70, 20, 22, 58, 52), fill=body, outline=INK)          # 왼쪽 귀
d.polygon(P(122, 70, 130, 22, 92, 52), fill=body, outline=INK)        # 오른쪽 귀
d.line(P(28, 70, 20, 22, 58, 52), fill=INK, width=3 * S)
d.line(P(122, 70, 130, 22, 92, 52), fill=INK, width=3 * S)
d.rectangle(P(55, 34, 95, 60), fill=body, outline=INK, width=3 * S)   # 목
d.ellipse(P(25, 50, 125, 150), fill=body, outline=INK, width=3 * S)   # 몸통
d.chord(P(28, 53, 122, 147), 20, 160, fill=shade)
d.arc(P(25, 50, 125, 150), 0, 360, fill=INK, width=3 * S)
d.line(P(30, 85, 120, 85), fill=gold, width=5 * S)                    # 금 띠
d.ellipse(P(63, 100, 87, 118), fill=lapis, outline=INK, width=2 * S)  # 눈 무늬
d.ellipse(P(72, 103, 78, 115), fill=INK)
d.rectangle(P(55, 150, 95, 166), fill=shade, outline=INK, width=3 * S)  # 받침
d.ellipse(P(48, 26, 102, 42), fill=(170, 150, 120, 255), outline=INK, width=3 * S)  # 입구
fl = Image.new("RGBA", v.size, (0, 0, 0, 0)); fd = ImageDraw.Draw(fl)  # 불꽃
fd.polygon(P(75, 0, 62, 26, 75, 36, 88, 26), fill=(255, 170, 60, 255))
fd.polygon(P(75, 12, 68, 28, 75, 34, 82, 28), fill=(255, 238, 170, 255))
v.alpha_composite(fl.filter(ImageFilter.GaussianBlur(1)))
save(v.resize((150, 170), Image.LANCZOS), "vessel")

# ---- 그릇 뒤 빛 ----
gm = Image.new("L", (360, 360), 0); ImageDraw.Draw(gm).ellipse([100, 100, 260, 260], fill=210)  # 가장자리까지 번지지 않게 여백
g = Image.new("RGBA", (360, 360), (255, 214, 140, 255)); g.putalpha(gm.filter(ImageFilter.GaussianBlur(45)))
save(g, "glow")

# ---- 아이템 아이콘 (획득 카드용 128x128): Assets/image/Bastet/Bastet_UI/Common_Icon_BastetVessel.png ----
icon = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
vi = Image.open(os.path.join(M, "MU_BastetV1_vessel.png")).convert("RGBA")
vi.thumbnail((104, 118), Image.LANCZOS)
icon.alpha_composite(vi, ((128 - vi.width) // 2, (128 - vi.height) // 2))
icon_path = os.path.join(ROOT, "Assets", "image", "Bastet", "Bastet_UI", "Common_Icon_BastetVessel.png")
icon.save(icon_path); print("saved icon", icon_path)
