using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// [추가] 유물 카드 등급별 틀 (업적 카드처럼 등급마다 모양이 다름). 그림 파일 없이 코드로 그린 도트 카드 (100x150px)
// - 파피루스: 엮은 갈대 종이 결 + 갈색 테두리 + 붉은 도장 점
// - 청동: 금속 그러데이션 + 입체 테두리 + 네 귀퉁이 리벳 + 청록 녹 + 어두운 이름판
// - 황금: 밝은 금 그러데이션 + 입체 테두리 + 청금석 장식 + 위쪽 홍옥 + 청금석 이름판 (+ 빛이 지나가는 반짝임)
// - 뒷면(아직 못 얻음): 가죽 + 바느질 선 + 앙크 무늬
// 카드 위 칸 위치는 Window/Plaque/Band (카드 크기 비율)로 맞춤
public static class NotebookCardFrame
{
    private const int W = 100, H = 150;
    private static readonly RectInt WindowPx = new RectInt(10, 52, 80, 80);
    private static readonly RectInt PlaquePx = new RectInt(10, 12, 80, 32);
    private static readonly Dictionary<int, Sprite> cache = new Dictionary<int, Sprite>();
    private static Sprite back, shine;

    // ================= 밖에서 쓰는 것 =================
    public static Sprite For(AchGrade g)
    {
        int key = (int)g;
        if (cache.TryGetValue(key, out Sprite s) && s != null) return s;
        Color32[] px = g == AchGrade.Gold ? Gold() : (g == AchGrade.Bronze ? Bronze() : Papyrus());
        s = ToSprite(px);
        cache[key] = s;
        return s;
    }

    public static Sprite Back()
    {
        if (back == null) back = ToSprite(Leather());
        return back;
    }

    // 카드 크기에 맞춘 칸 위치 (카드 가운데 기준)
    public static void Window(Vector2 card, out Vector2 center, out Vector2 size) => Slot(WindowPx, card, out center, out size);
    public static void Plaque(Vector2 card, out Vector2 center, out Vector2 size) => Slot(PlaquePx, card, out center, out size);
    public static Vector2 Band(Vector2 card) => new Vector2(0f, (139.5f / H - 0.5f) * card.y);

    public static string Label(AchGrade g) => g == AchGrade.Gold ? "황금" : (g == AchGrade.Bronze ? "청동" : "파피루스");

    public static Color LabelColor(AchGrade g)
    {
        if (g == AchGrade.Gold) return Hex(0xF2D06B);
        return g == AchGrade.Bronze ? Hex(0xF0C890) : Hex(0x6B4A2B);
    }

    public static Color NameColor(AchGrade g)
    {
        if (g == AchGrade.Gold) return Hex(0xF2D06B);
        return g == AchGrade.Bronze ? Hex(0xF3DDB0) : Hex(0x4A3423);
    }

    public static string HexOf(AchGrade g) => g == AchGrade.Gold ? "#C8962A" : (g == AchGrade.Bronze ? "#A0622D" : "#7A5A30");

    // 황금 카드: 대각선 빛이 3초마다 지나감 (카드 밖으로 안 나가게 RectMask2D)
    public static void AddShine(Image frame)
    {
        if (frame.GetComponent<RectMask2D>() == null) frame.gameObject.AddComponent<RectMask2D>();
        Vector2 size = frame.rectTransform.sizeDelta;
        Image s = UIBuild.Image(frame.rectTransform, "Shine", Vector2.zero, new Vector2(size.x * 0.35f, size.y * 1.6f), Shine(), Color.white);
        s.raycastTarget = false;
        s.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -25f);
        s.gameObject.AddComponent<NotebookCardShine>();
    }

    private static void Slot(RectInt r, Vector2 card, out Vector2 center, out Vector2 size)
    {
        center = new Vector2((r.x + r.width * 0.5f) / W - 0.5f, (r.y + r.height * 0.5f) / H - 0.5f);
        center = new Vector2(center.x * card.x, center.y * card.y);
        size = new Vector2((float)r.width / W * card.x, (float)r.height / H * card.y);
    }

    // ================= 파피루스 =================
    private static Color32[] Papyrus()
    {
        var px = new Color32[W * H];
        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                int strip = ((x / 6) + (y / 6)) % 2 == 0 ? 6 : -4;       // 가로세로로 엮은 갈대 띠
                int fiber = (Noise(x, y / 3) % 9) - 4;                   // 결
                px[y * W + x] = Shade(Hex32(0xE8D5A4), strip + fiber);
            }
        }
        Border(px, 0, 2, Hex32(0x6B4A2B));
        Border(px, 2, 1, Hex32(0xF6EBCB));
        Frame(px, WindowPx, 2, Hex32(0x6B4A2B), Hex32(0x2A1E14));
        Frame(px, PlaquePx, 1, Hex32(0x6B4A2B), Hex32(0xD8C08A));
        foreach (Vector2Int c in Corners(6)) Dot(px, c, 2, Hex32(0xA33A26));
        return px;
    }

    // ================= 청동 =================
    private static Color32[] Bronze()
    {
        var px = Gradient(Hex32(0x7E5129), Hex32(0xBC8A50));
        for (int i = 0; i < px.Length; i++)
        {
            if (Noise(i % W, i / W) % 37 == 0) px[i] = Hex32(0x5F9C8A); // 청록 녹
        }
        Bevel(px, Hex32(0x3E2614), Hex32(0xE6B878), Hex32(0x5A3A1E));
        Frame(px, WindowPx, 3, Hex32(0x5A3A1E), Hex32(0x22170F));
        Frame(px, PlaquePx, 2, Hex32(0xC89458), Hex32(0x6E4524));
        foreach (Vector2Int c in Corners(6)) Rivet(px, c, Hex32(0xF0C890), Hex32(0x3E2614));
        return px;
    }

    // ================= 황금 =================
    private static Color32[] Gold()
    {
        var px = Gradient(Hex32(0xB07C22), Hex32(0xF4D46E));
        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++) if ((x + y) % 11 == 0) px[y * W + x] = Shade(px[y * W + x], 14); // 비스듬한 광택 줄
        }
        Bevel(px, Hex32(0x6A4508), Hex32(0xFFF2B8), Hex32(0x8A5E12));
        Frame(px, WindowPx, 3, Hex32(0x7A5410), Hex32(0x1A1208));
        Frame(px, PlaquePx, 2, Hex32(0xF2D06B), Hex32(0x1E2F5C));
        foreach (Vector2Int c in Corners(7)) Diamond(px, c, 3, Hex32(0x2A4C9C), Hex32(0x7A9CEC));
        Dot(px, new Vector2Int(W / 2, 140), 4, Hex32(0xB8321E));
        Dot(px, new Vector2Int(W / 2 - 1, 141), 1, Hex32(0xFF9A7A));
        return px;
    }

    // ================= 뒷면 =================
    private static Color32[] Leather()
    {
        var px = new Color32[W * H];
        for (int i = 0; i < px.Length; i++) px[i] = Shade(Hex32(0x4A3220), (Noise(i % W, i / W) % 11) - 5);
        Border(px, 0, 2, Hex32(0x2E1E12));
        for (int x = 6; x < W - 6; x += 4) { Set(px, x, 5, Hex32(0xC9A66B)); Set(px, x, H - 6, Hex32(0xC9A66B)); }
        for (int y = 6; y < H - 6; y += 4) { Set(px, 5, y, Hex32(0xC9A66B)); Set(px, W - 6, y, Hex32(0xC9A66B)); }
        Ankh(px, new Vector2Int(W / 2, 80), Hex32(0x6E5232));
        return px;
    }

    private static void Ankh(Color32[] px, Vector2Int c, Color32 col)
    {
        Fill(px, new RectInt(c.x - 2, c.y - 34, 5, 34), col);       // 세로 줄
        Fill(px, new RectInt(c.x - 14, c.y - 4, 29, 5), col);       // 가로 팔
        for (int a = 0; a < 360; a += 3)                             // 위 고리
        {
            float r = a * Mathf.Deg2Rad;
            for (int t = 0; t < 3; t++) Set(px, c.x + Mathf.RoundToInt(Mathf.Cos(r) * (9 + t)), c.y + 13 + Mathf.RoundToInt(Mathf.Sin(r) * (12 + t)), col);
        }
    }

    // ================= 그리기 도우미 =================
    private static Color32[] Gradient(Color32 bottom, Color32 top)
    {
        var px = new Color32[W * H];
        for (int y = 0; y < H; y++)
        {
            Color32 c = Color32.Lerp(bottom, top, (float)y / (H - 1));
            for (int x = 0; x < W; x++) px[y * W + x] = Shade(c, (Noise(x, y) % 7) - 3);
        }
        return px;
    }

    // 바깥 1px 어둡게 + 위/왼쪽 밝게 + 아래/오른쪽 어둡게 (입체 테두리)
    private static void Bevel(Color32[] px, Color32 edge, Color32 light, Color32 dark)
    {
        Border(px, 0, 1, edge);
        for (int i = 1; i < 5; i++)
        {
            Fill(px, new RectInt(i, H - 1 - i, W - 2 * i, 1), i < 3 ? light : Shade(light, -30));
            Fill(px, new RectInt(i, i, 1, H - 2 * i), i < 3 ? light : Shade(light, -30));
            Fill(px, new RectInt(i, i, W - 2 * i, 1), dark);
            Fill(px, new RectInt(W - 1 - i, i, 1, H - 2 * i), dark);
        }
    }

    private static void Frame(Color32[] px, RectInt inner, int t, Color32 line, Color32 fill)
    {
        Fill(px, new RectInt(inner.x - t, inner.y - t, inner.width + 2 * t, inner.height + 2 * t), line);
        Fill(px, inner, fill);
    }

    private static void Border(Color32[] px, int inset, int t, Color32 c)
    {
        Fill(px, new RectInt(inset, inset, W - 2 * inset, t), c);
        Fill(px, new RectInt(inset, H - inset - t, W - 2 * inset, t), c);
        Fill(px, new RectInt(inset, inset, t, H - 2 * inset), c);
        Fill(px, new RectInt(W - inset - t, inset, t, H - 2 * inset), c);
    }

    private static Vector2Int[] Corners(int inset)
    {
        return new[] { new Vector2Int(inset, inset), new Vector2Int(W - 1 - inset, inset), new Vector2Int(inset, H - 1 - inset), new Vector2Int(W - 1 - inset, H - 1 - inset) };
    }

    private static void Rivet(Color32[] px, Vector2Int c, Color32 light, Color32 dark)
    {
        Dot(px, c, 2, dark);
        Dot(px, c, 1, light);
        Set(px, c.x - 1, c.y + 1, new Color32(255, 250, 230, 255));
    }

    private static void Diamond(Color32[] px, Vector2Int c, int r, Color32 col, Color32 hi)
    {
        for (int dy = -r; dy <= r; dy++)
        {
            for (int dx = -r; dx <= r; dx++) if (Mathf.Abs(dx) + Mathf.Abs(dy) <= r) Set(px, c.x + dx, c.y + dy, col);
        }
        Set(px, c.x - 1, c.y + 1, hi);
    }

    private static void Dot(Color32[] px, Vector2Int c, int r, Color32 col)
    {
        for (int dy = -r; dy <= r; dy++)
        {
            for (int dx = -r; dx <= r; dx++) if (dx * dx + dy * dy <= r * r + 1) Set(px, c.x + dx, c.y + dy, col);
        }
    }

    private static void Fill(Color32[] px, RectInt r, Color32 c)
    {
        for (int y = r.y; y < r.y + r.height; y++)
        {
            for (int x = r.x; x < r.x + r.width; x++) Set(px, x, y, c);
        }
    }

    private static void Set(Color32[] px, int x, int y, Color32 c)
    {
        if (x >= 0 && x < W && y >= 0 && y < H) px[y * W + x] = c;
    }

    private static int Noise(int x, int y)
    {
        unchecked
        {
            int h = x * 374761393 + y * 668265263;
            h = (h ^ (h >> 13)) * 1274126177;
            return (h ^ (h >> 16)) & 0x7FFFFFFF;
        }
    }

    private static Color32 Shade(Color32 c, int d)
    {
        return new Color32((byte)Mathf.Clamp(c.r + d, 0, 255), (byte)Mathf.Clamp(c.g + d, 0, 255), (byte)Mathf.Clamp(c.b + d, 0, 255), c.a);
    }

    private static Color32 Hex32(int rgb) => new Color32((byte)(rgb >> 16), (byte)((rgb >> 8) & 0xFF), (byte)(rgb & 0xFF), 255);
    private static Color Hex(int rgb) => Hex32(rgb);

    private static Sprite ToSprite(Color32[] px)
    {
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 100f);
    }

    // 가운데가 밝은 세로 띠 (반짝임)
    private static Sprite Shine()
    {
        if (shine != null) return shine;
        var tex = new Texture2D(16, 4, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int x = 0; x < 16; x++)
        {
            float a = Mathf.Sin((x + 0.5f) / 16f * Mathf.PI);
            for (int y = 0; y < 4; y++) tex.SetPixel(x, y, new Color(1f, 0.97f, 0.85f, a * a * 0.55f));
        }
        tex.Apply();
        shine = Sprite.Create(tex, new Rect(0, 0, 16, 4), new Vector2(0.5f, 0.5f));
        return shine;
    }
}
