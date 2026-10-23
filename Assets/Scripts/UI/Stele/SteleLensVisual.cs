using UnityEngine;
using UnityEngine.UI;

// 비석 화면의 볼록렌즈 모양 (금테 고리 + 연한 유리 + 손잡이). 가운데가 비어 있어서 아래 글자가 보임
// Build(부모, 크기) -> 움직일 RectTransform을 돌려줌
public static class SteleLensVisual
{
    private static Sprite ringSprite;

    public static RectTransform Build(RectTransform parent, float size)
    {
        RectTransform lens = UIBuild.Rect(parent, "Lens", Vector2.zero, new Vector2(size, size));

        Image handle = UIBuild.Image(lens, "Handle", new Vector2(size * 0.48f, -size * 0.48f), new Vector2(34f, size * 0.55f), null, Hex("#8A5A2B"));
        handle.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        Image glass = UIBuild.Image(lens, "Glass", Vector2.zero, new Vector2(size, size), UIBuild.Circle(), Color.white);
        glass.color = new Color(1f, 0.95f, 0.75f, 0.18f); // 비추는 빛

        Image ring = UIBuild.Image(lens, "Rim", Vector2.zero, new Vector2(size + 16f, size + 16f), Ring(), Color.white);
        ring.color = Hex("#E8B23A");

        Image shine = UIBuild.Image(lens, "Shine", new Vector2(-size * 0.22f, size * 0.24f), new Vector2(size * 0.22f, size * 0.1f), UIBuild.Circle(), Color.white);
        shine.color = new Color(1f, 1f, 1f, 0.35f);
        shine.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 35f);
        return lens;
    }

    // 바깥 8%만 칠해진 고리 (한 번만 만들어 재사용)
    private static Sprite Ring()
    {
        if (ringSprite != null) return ringSprite;

        const int size = 128;
        var pixels = new Color32[size * size];
        float r = size * 0.5f;
        for (int i = 0; i < pixels.Length; i++)
        {
            float d = Vector2.Distance(new Vector2(i % size + 0.5f, i / size + 0.5f), new Vector2(r, r)) / r;
            float a = Mathf.Clamp01((d - 0.9f) * 20f) * Mathf.Clamp01((1f - d) * 20f);
            pixels[i] = new Color32(255, 255, 255, (byte)(a * 255f));
        }

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        tex.SetPixels32(pixels);
        tex.Apply();
        ringSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return ringSprite;
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }
}
