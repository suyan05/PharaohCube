using System.Collections.Generic;
using UnityEngine;

// 테마 그림이 비어 있을 때 쓰는 임시 그림 (실행 중 한 번만 만들어 재사용)
// 모양: 0 사각형, 1 원, 2 마름모, 3 테두리 사각형
public static class SobekPlaceholderSprites
{
    public enum Shape { Square, Circle, Diamond, Frame }

    private const int Size = 32;
    private static readonly Dictionary<Shape, Sprite> cache = new Dictionary<Shape, Sprite>();

    public static Sprite Get(Shape shape)
    {
        if (cache.TryGetValue(shape, out Sprite s) && s != null) return s;
        s = Sprite.Create(MakeTexture(shape), new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
        s.name = "SobekPlaceholder_" + shape;
        cache[shape] = s;
        return s;
    }

    // 에디터 PNG 생성(SobekArtSetup)과 같은 모양을 씀
    public static Texture2D MakeTexture(Shape shape, Color? fill = null, Color? edge = null)
    {
        Color f = fill ?? Color.white;
        Color e = edge ?? new Color(0.75f, 0.75f, 0.75f);
        var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var px = new Color[Size * Size];
        float c = (Size - 1) * 0.5f;
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                Color col = Color.clear;
                switch (shape)
                {
                    case Shape.Square:
                        col = (x == 0 || y == 0 || x == Size - 1 || y == Size - 1) ? e : f;
                        break;
                    case Shape.Frame:
                        col = (x < 3 || y < 3 || x > Size - 4 || y > Size - 4) ? e : Color.clear;
                        break;
                    case Shape.Circle:
                    {
                        float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                        col = d < 11f ? f : (d < 13f ? e : Color.clear);
                        break;
                    }
                    case Shape.Diamond:
                    {
                        float d = Mathf.Abs(x - c) + Mathf.Abs(y - c);
                        col = d < 11f ? f : (d < 13f ? e : Color.clear);
                        break;
                    }
                }
                px[y * Size + x] = col;
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }
}
