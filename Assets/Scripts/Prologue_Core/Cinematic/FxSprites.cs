using UnityEngine;

// 그림 없이 쓰는 효과용 작은 그림 (한 번 만들어 재사용)
// - Dot: 2x2 흰 점 (불똥, 먼지)
// - Soft: 가운데가 밝고 바깥이 투명한 원 (빛 덩어리, 후광)
// - Dots: CineFX.Puff / Sparkle에 바로 넣을 배열
public static class FxSprites
{
    private static Sprite dot;
    private static Sprite soft;
    private static Sprite[] dots;

    public static Sprite Dot
    {
        get
        {
            if (dot != null) return dot;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            Color32 w = new Color32(255, 255, 255, 255);
            tex.SetPixels32(new Color32[] { w, w, w, w });
            tex.Apply();
            dot = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 32f);
            return dot;
        }
    }

    public static Sprite[] Dots => dots ??= new[] { Dot };

    public static Sprite Soft
    {
        get
        {
            if (soft != null) return soft;
            const int size = 64;
            var pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                float d = Vector2.Distance(new Vector2(i % size + 0.5f, i / size + 0.5f), new Vector2(32f, 32f)) / 32f;
                float a = Mathf.Clamp01(1f - d);
                pixels[i] = new Color32(255, 255, 255, (byte)(a * a * 255f));
            }
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels32(pixels);
            tex.Apply();
            soft = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return soft;
        }
    }
}
