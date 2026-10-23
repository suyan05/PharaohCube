using System.Collections.Generic;
using UnityEngine;

// 상형문자 느낌의 작은 그림을 코드로 만듦 (폰트에 이집트 문자가 없어서 그림으로 대신)
// - Get(번호): 번호마다 항상 같은 모양 (16x16 도트, 한 번 만든 건 재사용)
// - 세로선, 가로선, 동그라미, 물결, 새 부리, 점, 삼각형 중 2~3개를 섞어서 만듦
// 아트가 오면 SteleDecodeUI의 Glyph Sprites 칸에 그림을 넣으면 이건 안 씀
public static class GlyphFactory
{
    private const int Size = 16;
    private static readonly Dictionary<int, Sprite> cache = new Dictionary<int, Sprite>();
    private static readonly Color32 Ink = new Color32(255, 255, 255, 255);

    public static Sprite Get(int seed)
    {
        if (cache.TryGetValue(seed, out Sprite cached) && cached != null) return cached;

        var pixels = new Color32[Size * Size];
        var rng = new System.Random(seed * 7919 + 13);
        int parts = rng.Next(2, 4);
        for (int i = 0; i < parts; i++) DrawPart(pixels, rng.Next(0, 7), rng);

        var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        tex.SetPixels32(pixels);
        tex.Apply();
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
        cache[seed] = sprite;
        return sprite;
    }

    private static void DrawPart(Color32[] p, int kind, System.Random rng)
    {
        int a = rng.Next(3, 12), b = rng.Next(3, 12);
        switch (kind)
        {
            case 0: Line(p, a, 2, a, 13); break;                    // 세로선
            case 1: Line(p, 2, b, 13, b); break;                    // 가로선
            case 2: Ring(p, a, b, rng.Next(2, 4)); break;           // 동그라미 (눈, 태양)
            case 3: Wave(p, b); break;                              // 물결 (물)
            case 4: Line(p, 3, 12, a, 4); Line(p, a, 4, 12, 7); break; // 새 부리, 꺾임
            case 5: Dot(p, a, b); Dot(p, a + 2, b); break;          // 점 두 개
            default: Line(p, 3, 12, 8, 4); Line(p, 8, 4, 13, 12); Line(p, 3, 12, 13, 12); break; // 삼각형
        }
    }

    // ================= 그리기 도우미 =================
    private static void Dot(Color32[] p, int x, int y)
    {
        if (x < 0 || y < 0 || x >= Size || y >= Size) return;
        p[y * Size + x] = Ink;
    }

    private static void Line(Color32[] p, int x0, int y0, int x1, int y1)
    {
        int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0), 1);
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            Dot(p, Mathf.RoundToInt(Mathf.Lerp(x0, x1, t)), Mathf.RoundToInt(Mathf.Lerp(y0, y1, t)));
        }
    }

    private static void Ring(Color32[] p, int cx, int cy, int r)
    {
        for (int i = 0; i < 24; i++)
        {
            float ang = i / 24f * Mathf.PI * 2f;
            Dot(p, cx + Mathf.RoundToInt(Mathf.Cos(ang) * r), cy + Mathf.RoundToInt(Mathf.Sin(ang) * r));
        }
    }

    private static void Wave(Color32[] p, int y)
    {
        for (int x = 2; x < 14; x++) Dot(p, x, y + ((x / 2) % 2 == 0 ? 0 : 1));
    }
}
