using System;
using UnityEngine;
using UnityEngine.UI;

// [추가] 신 일러스트를 "신성하게" 보이게 하는 화면 보정 (UI 그림만 씀, 후처리 없음)
// - 뒤: 금빛 후광(크게 숨 쉬는 빛) + 그림 테두리 오라(같은 그림을 살짝 크게, 금색으로)
// - 앞: 위에서 비스듬히 내려오는 빛기둥 3개(천천히 흔들림) + 가장자리를 어둡게(비네트) + 아주 옅은 따뜻한 색
// - 그림이 천천히 숨 쉬듯 커졌다 작아짐
// - 그림의 투명도(페이드)를 그대로 따라감
// 쓰는 곳: RaFinaleCut(라 엔딩), GodIllustration(다른 신 일러스트)
public class HolyFx : MonoBehaviour
{
    private const int ShaftCount = 3;
    private static Sprite vignette, shaft;

    private Image art, aura, halo, vignetteImg, warm;
    private readonly Image[] shafts = new Image[ShaftCount];
    private Color tint;
    private float clock;

    public static HolyFx Attach(RectTransform parent, Image artImage, Color tintColor)
    {
        if (parent == null || artImage == null) return null;
        try
        {
            HolyFx fx = parent.gameObject.AddComponent<HolyFx>();
            fx.Build(parent, artImage, tintColor);
            return fx;
        }
        catch (Exception e)
        {
            Debug.LogError($"[HolyFx] 만들기 오류: {e}");
            return null;
        }
    }

    private void Build(RectTransform parent, Image artImage, Color tintColor)
    {
        art = artImage;
        tint = tintColor;
        RectTransform artRt = art.rectTransform;
        int artIndex = artRt.GetSiblingIndex();
        halo = UIBuild.Image(parent, "HolyHalo", artRt.anchoredPosition + new Vector2(0f, 160f), new Vector2(1500f, 1500f), FxSprites.Soft, Color.white);
        bool stretched = artRt.anchorMin != artRt.anchorMax; // 화면 가득 늘린 그림 (GodIllustration)
        aura = stretched ? Full(parent, "HolyAura", art.sprite)
                         : UIBuild.Image(parent, "HolyAura", artRt.anchoredPosition, artRt.sizeDelta * 1.025f, art.sprite, Color.white);
        aura.preserveAspect = art.preserveAspect;
        halo.rectTransform.SetSiblingIndex(artIndex);
        aura.rectTransform.SetSiblingIndex(artIndex + 1);
        for (int i = 0; i < ShaftCount; i++) shafts[i] = MakeShaft(parent, i);
        vignetteImg = Full(parent, "HolyVignette", Vignette());
        warm = Full(parent, "HolyWarm", null);
        foreach (Image img in new[] { halo, aura, vignetteImg, warm }) img.raycastTarget = false;
    }

    private Image MakeShaft(RectTransform parent, int i)
    {
        Vector2 pos = new Vector2(-380f + i * 380f, 260f);
        Image img = UIBuild.Image(parent, "HolyShaft" + i, pos, new Vector2(240f, 1500f), Shaft(), Color.white);
        img.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -18f + i * 6f);
        img.raycastTarget = false;
        return img;
    }

    private static Image Full(RectTransform parent, string name, Sprite sprite)
    {
        Image img = UIBuild.Stretch(parent, name).gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = Color.clear;
        return img;
    }

    private void Update()
    {
        if (art == null) return;
        clock += Time.unscaledDeltaTime;
        float a = art.color.a;
        float breathe = 1f + Mathf.Sin(clock * 0.9f) * 0.012f;
        art.rectTransform.localScale = Vector3.one * breathe;
        aura.rectTransform.localScale = Vector3.one * (breathe * (1f + Mathf.Sin(clock * 1.7f) * 0.006f));
        aura.color = new Color(tint.r, tint.g, tint.b, a * (0.32f + Mathf.Sin(clock * 1.7f) * 0.1f));
        halo.color = new Color(tint.r, tint.g * 0.95f, tint.b * 0.8f, a * (0.42f + Mathf.Sin(clock * 0.8f) * 0.08f));
        halo.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(clock * 0.8f) * 0.04f);
        for (int i = 0; i < ShaftCount; i++) UpdateShaft(i, a);
        vignetteImg.color = new Color(0f, 0f, 0f, a * 0.75f);
        warm.color = new Color(1f, 0.82f, 0.45f, a * 0.06f);
    }

    private void UpdateShaft(int i, float a)
    {
        float k = Mathf.Sin(clock * (0.35f + i * 0.13f) + i * 2.1f);
        shafts[i].color = new Color(1f, 0.93f, 0.7f, a * (0.13f + k * 0.07f));
        shafts[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, -18f + i * 6f + k * 2.5f);
    }

    // ================= 코드로 만드는 그림 =================
    // 가운데는 투명, 가장자리로 갈수록 검게 (비네트)
    private static Sprite Vignette()
    {
        if (vignette != null) return vignette;
        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx * 0.8f + dy * dy * 1.1f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, (d - 0.55f) / 0.6f)));
            }
        }
        tex.Apply();
        vignette = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return vignette;
    }

    // 위는 밝고 아래로 갈수록 사라지는 부드러운 빛기둥
    private static Sprite Shaft()
    {
        if (shaft != null) return shaft;
        const int w = 32, h = 128;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float across = 1f - Mathf.Abs((x + 0.5f) / w * 2f - 1f);
                float along = (float)y / (h - 1);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, across) * along * along));
            }
        }
        tex.Apply();
        shaft = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        return shaft;
    }
}
