using UnityEngine;
using UnityEngine.UI;

// 코드로 UI를 만들 때 쓰는 공용 도우미 (새 공통 UI들이 같이 씀)
// 좌표는 1920x1080 기준, 부모 가운데가 (0,0)
public static class UIBuild
{
    // 화면 전체를 덮는 Screen Space Overlay 캔버스
    public static RectTransform CreateCanvas(Transform parent, string name, int sortingOrder, out GameObject canvasGo)
    {
        canvasGo = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(parent, false);

        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        return canvasGo.GetComponent<RectTransform>();
    }

    // 부모를 꽉 채우는 빈 RectTransform
    public static RectTransform Stretch(RectTransform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    // 부모 가운데 기준 빈 RectTransform
    public static RectTransform Rect(RectTransform parent, string name, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    // 그림이 있으면 그림, 없으면 fallback 색 네모
    public static Image Image(RectTransform parent, string name, Vector2 pos, Vector2 size, Sprite sprite, Color fallback)
    {
        RectTransform rt = Rect(parent, name, pos, size);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = sprite != null ? Color.white : fallback;
        img.preserveAspect = sprite != null;
        img.raycastTarget = false;
        return img;
    }

    public static Text Text(RectTransform parent, string name, Font font, int size, Vector2 pos, Vector2 box, Color color)
    {
        RectTransform rt = Rect(parent, name, pos, box);
        var t = rt.gameObject.AddComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    // 원 모양 스프라이트 (아트가 없을 때 링/동그라미 대신 씀, 한 번만 만들어 재사용)
    private static Sprite circle;

    public static Sprite Circle()
    {
        if (circle != null) return circle;

        const int size = 128;
        float r = size * 0.5f;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(r - d) * 255f));
            }
        }

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        tex.SetPixels32(pixels);
        tex.Apply();
        circle = Sprite.Create(tex, new UnityEngine.Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return circle;
    }
}