using UnityEngine;

// [추가] 바스테트 스토리용 [임시] 픽셀 그림 (아트가 오기 전까지 코드로 그림)
// - 검은 고양이(앉은 모습) / 고양이 눈빛 / 화면 가장자리 눈 아이콘 / 벽화 석판
// - 메뉴(Tools/Bastet/★ 스토리·검은 고양이 세팅)가 같은 그림을 PNG로 저장해 Inspector에 넣어 줌
//   -> 아트가 오면 Assets/image/Bastet/Story 의 PNG만 덮어쓰면 됨
// - 한 번 만든 그림은 static으로 재사용 (매번 new Texture2D 하지 않음)
public static class BastetPixelArt
{
    public const float PixelsPerUnit = 16f;

    // 앉아 있는 검은 고양이 (위 줄부터). # 몸, E 눈 자리, . 빈칸
    private static readonly string[] CatMap =
    {
        "................",
        "...#.......#....",
        "...##.....##....",
        "...###...###....",
        "...#########....",
        "..###########...",
        "..##E#####E##...",
        "..###########...",
        "...#########....",
        "....#######.....",
        "...#########....",
        "..###########...",
        "..###########.#.",
        "..###########.##",
        "..###########..#",
        "...###...###.##.",
    };

    // 화면 가장자리에 띄울 두 눈
    private static readonly string[] EyesIconMap =
    {
        ".###.....###.",
        "##o##...##o##",
        "##o##...##o##",
        ".###.....###.",
    };

    private static readonly Color32 CatBody = new Color32(18, 15, 26, 255);
    private static readonly Color32 CatRim = new Color32(74, 66, 98, 255);
    private static readonly Color32 EyeColor = new Color32(214, 255, 120, 255);
    private static readonly Color32 Pupil = new Color32(30, 40, 10, 255);

    private static Sprite catSit, catEyes, eyesIcon, mural;

    public static Sprite CatSit => catSit != null ? catSit : (catSit = Make(CatTexture(false), new Vector2(0.5f, 0f)));
    public static Sprite CatEyes => catEyes != null ? catEyes : (catEyes = Make(CatTexture(true), new Vector2(0.5f, 0f)));
    public static Sprite EyesIcon => eyesIcon != null ? eyesIcon : (eyesIcon = Make(EyesIconTexture(), new Vector2(0.5f, 0.5f)));
    public static Sprite MuralSlab => mural != null ? mural : (mural = Make(MuralTexture(), new Vector2(0.5f, 0f)));

    // ================= 텍스처 (에디터 메뉴가 PNG로 저장할 때도 씀) =================
    public static Texture2D CatTexture(bool eyesOnly)
    {
        int h = CatMap.Length, w = CatMap[0].Length;
        Texture2D tex = NewTex(w, h);
        var px = new Color32[w * h]; // 기본값 = 투명
        for (int r = 0; r < h; r++)
        {
            for (int x = 0; x < w; x++)
            {
                char c = CatMap[r][x];
                int i = (h - 1 - r) * w + x; // 텍스처는 아래 줄부터
                if (eyesOnly)
                {
                    if (c == 'E') px[i] = EyeColor;
                    continue;
                }
                if (c == '.') continue;
                px[i] = IsEdge(r, x) ? CatRim : CatBody; // 테두리만 살짝 밝게 (불빛에 윤곽이 보이게)
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }

    public static Texture2D EyesIconTexture()
    {
        int h = EyesIconMap.Length, w = EyesIconMap[0].Length;
        Texture2D tex = NewTex(w, h);
        var px = new Color32[w * h];
        for (int r = 0; r < h; r++)
            for (int x = 0; x < w; x++)
            {
                char c = EyesIconMap[r][x];
                int i = (h - 1 - r) * w + x;
                if (c == '#') px[i] = EyeColor;
                else if (c == 'o') px[i] = Pupil;
            }
        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }

    // 사암 석판 + 테두리 + 상형문자 점 + 찢긴 자국 (48 x 32 = 3 x 2칸)
    public static Texture2D MuralTexture()
    {
        const int w = 48, h = 32;
        Texture2D tex = NewTex(w, h);
        var px = new Color32[w * h];
        var stone = new Color32(176, 142, 96, 255);
        var stoneDark = new Color32(120, 92, 58, 255);
        var panel = new Color32(196, 164, 112, 255);
        var ink = new Color32(92, 60, 36, 255);
        var torn = new Color32(40, 28, 20, 255);
        var rng = new System.Random(7); // 항상 같은 무늬

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool border = x < 2 || x >= w - 2 || y < 2 || y >= h - 2;
                bool inner = x >= 4 && x < w - 4 && y >= 4 && y < h - 4;
                Color32 c = border ? stoneDark : inner ? panel : stone;
                // 위아래 상형문자 띠
                if (inner && (y == h - 7 || y == 6) && x % 4 == 1) c = ink;
                // 가운데 그림 자리: 흐린 점
                if (inner && y > 8 && y < h - 9 && rng.Next(100) < 6) c = ink;
                // 오른쪽 위 찢긴 자국
                int dx = x - (w - 9), dy = y - (h - 8);
                if (dx * dx + dy * dy * 2 < 18 + rng.Next(6)) c = torn;
                px[y * w + x] = c;
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }

    // ================= 도우미 =================
    private static bool Solid(int r, int x) => r >= 0 && r < CatMap.Length && x >= 0 && x < CatMap[r].Length && CatMap[r][x] != '.';

    private static bool IsEdge(int r, int x) => !Solid(r - 1, x) || !Solid(r + 1, x) || !Solid(r, x - 1) || !Solid(r, x + 1);

    private static Texture2D NewTex(int w, int h)
    {
        return new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = "BastetPixelArt"
        };
    }

    private static Sprite Make(Texture2D tex, Vector2 pivot)
    {
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, PixelsPerUnit);
    }
}
