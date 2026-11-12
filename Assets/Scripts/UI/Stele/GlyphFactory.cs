using System.Collections.Generic;
using UnityEngine;

// 상형문자 그림을 코드로 만듦 (폰트에 이집트 문자가 없어서 그림으로 대신)
// - Get(번호): 번호마다 항상 같은 모양 (16x16 도트, 한 번 만든 건 재사용)
// [수정] 예전: 가는 1px 선 몇 개를 아무렇게나 섞음 -> 너무 얇고 낙서 같았음
//        지금: 실제 상형문자를 본뜬 16가지(앙크, 우자트 눈, 물, 태양, 갈대, 입, 새, 빵, 바구니, 제드 기둥,
//              뿔뱀, 깃털, 팔, 별, 풍뎅이, 집)를 2px 굵은 선으로 그림. 번호에 따라 좌우가 뒤집히기도 함
// 아트가 오면 SteleDecodeUI의 Glyph Sprites 칸에 그림을 넣으면 이건 안 씀
public static class GlyphFactory
{
    private const int Size = 16;
    private const int Kinds = 16;
    private static readonly Dictionary<int, Sprite> cache = new Dictionary<int, Sprite>();
    private static readonly Color32 Ink = new Color32(255, 255, 255, 255);

    public static Sprite Get(int seed)
    {
        if (cache.TryGetValue(seed, out Sprite cached) && cached != null) return cached;

        var pixels = new Color32[Size * Size];
        var rng = new System.Random(seed * 7919 + 13);
        int kind = rng.Next(0, Kinds);
        if (kind < 8) DrawA(pixels, kind);
        else DrawB(pixels, kind);
        if (rng.Next(2) == 0) Mirror(pixels);

        var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        tex.SetPixels32(pixels);
        tex.Apply();
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
        cache[seed] = sprite;
        return sprite;
    }

    // y는 아래가 0. 선은 전부 2px 굵기
    private static void DrawA(Color32[] p, int kind)
    {
        switch (kind)
        {
            case 0: Ring(p, 8, 11, 3, 2); Line(p, 8, 1, 8, 8); Line(p, 4, 7, 11, 7); break;                              // 앙크 (생명)
            case 1: Ring(p, 8, 9, 5, 3); Fill(p, 7, 8, 9, 10); Line(p, 2, 13, 13, 13); Line(p, 8, 6, 6, 2); Line(p, 10, 6, 13, 2); break; // 우자트 눈
            case 2: Zig(p, 11); Zig(p, 7); Zig(p, 3); break;                                                             // 물
            case 3: Ring(p, 8, 8, 6, 6); Fill(p, 7, 7, 9, 9); break;                                                     // 태양
            case 4: Line(p, 5, 1, 5, 14); Line(p, 5, 14, 10, 10); Line(p, 10, 10, 10, 4); Line(p, 10, 4, 5, 1); break;   // 갈대 잎
            case 5: Ring(p, 8, 8, 6, 3); break;                                                                          // 입
            case 6: Ring(p, 7, 7, 4, 3); Fill(p, 10, 10, 13, 13); Line(p, 13, 11, 15, 11); Line(p, 6, 4, 6, 1); Line(p, 9, 4, 9, 1); Line(p, 3, 8, 1, 6); break; // 새
            default: Line(p, 2, 3, 13, 3); Line(p, 2, 3, 2, 6); Line(p, 13, 3, 13, 6); Line(p, 2, 6, 5, 10); Line(p, 5, 10, 10, 10); Line(p, 10, 10, 13, 6); break; // 빵
        }
    }

    private static void DrawB(Color32[] p, int kind)
    {
        switch (kind)
        {
            case 8: Line(p, 2, 9, 4, 4); Line(p, 4, 4, 11, 4); Line(p, 11, 4, 13, 9); Line(p, 2, 9, 13, 9); Line(p, 6, 9, 9, 13); break; // 바구니
            case 9: Line(p, 8, 1, 8, 13); Line(p, 4, 13, 11, 13); Line(p, 4, 10, 11, 10); Line(p, 4, 7, 11, 7); Line(p, 5, 1, 10, 1); break; // 제드 기둥
            case 10: Line(p, 1, 5, 5, 8); Line(p, 5, 8, 9, 5); Line(p, 9, 5, 13, 8); Fill(p, 12, 8, 14, 10); Line(p, 12, 11, 11, 14); Line(p, 14, 11, 14, 14); break; // 뿔뱀
            case 11: Line(p, 5, 1, 5, 9); Line(p, 5, 9, 8, 14); Line(p, 8, 14, 11, 10); Line(p, 11, 10, 9, 5); Line(p, 9, 5, 5, 3); break; // 깃털 (마아트)
            case 12: Line(p, 1, 6, 11, 6); Line(p, 11, 6, 14, 9); Line(p, 11, 6, 14, 6); Line(p, 11, 6, 13, 3); Line(p, 1, 6, 1, 3); break; // 팔
            case 13: Line(p, 8, 1, 8, 14); Line(p, 2, 8, 13, 8); Line(p, 3, 3, 12, 12); Line(p, 3, 12, 12, 3); break;      // 별
            case 14: Ring(p, 8, 7, 4, 5); Line(p, 8, 2, 8, 11); Line(p, 3, 9, 1, 12); Line(p, 12, 9, 14, 12); Line(p, 3, 5, 1, 2); Line(p, 12, 5, 14, 2); break; // 풍뎅이
            default: Line(p, 2, 2, 2, 12); Line(p, 2, 12, 13, 12); Line(p, 13, 12, 13, 2); Line(p, 2, 2, 6, 2); Line(p, 9, 2, 13, 2); break; // 집
        }
    }

    // ================= 그리기 도우미 =================
    // 점 하나 = 2x2 (굵은 선)
    private static void Dot(Color32[] p, int x, int y)
    {
        for (int dy = 0; dy < 2; dy++)
        {
            for (int dx = 0; dx < 2; dx++)
            {
                int px = x + dx, py = y + dy;
                if (px < 0 || py < 0 || px >= Size || py >= Size) continue;
                p[py * Size + px] = Ink;
            }
        }
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

    // 타원 테두리 (가로 반지름 rx, 세로 반지름 ry)
    private static void Ring(Color32[] p, int cx, int cy, int rx, int ry)
    {
        for (int i = 0; i < 40; i++)
        {
            float ang = i / 40f * Mathf.PI * 2f;
            Dot(p, cx + Mathf.RoundToInt(Mathf.Cos(ang) * rx) - 1, cy + Mathf.RoundToInt(Mathf.Sin(ang) * ry) - 1);
        }
    }

    private static void Fill(Color32[] p, int x0, int y0, int x1, int y1)
    {
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++) Dot(p, x - 1, y - 1);
    }

    // 지그재그 물결 한 줄
    private static void Zig(Color32[] p, int y)
    {
        for (int x = 1; x < 13; x += 4)
        {
            Line(p, x, y, x + 2, y + 2);
            Line(p, x + 2, y + 2, x + 4, y);
        }
    }

    private static void Mirror(Color32[] p)
    {
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size / 2; x++)
            {
                Color32 a = p[y * Size + x];
                p[y * Size + x] = p[y * Size + Size - 1 - x];
                p[y * Size + Size - 1 - x] = a;
            }
        }
    }
}
