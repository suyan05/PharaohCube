using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// [추가] 메뉴 UI를 코드로 만드는 도우미 (시작 화면 / 설정 창 / 일시 정지 / 확인 창이 같이 씀)
// 모양·색·글꼴은 MenuTheme(Resources/MenuTheme.asset)에서 가져옴
public static class MenuUI
{
    public static MenuTheme Theme => MenuTheme.Current;

    // ================= 캔버스 =================
    public static Canvas NewCanvas(string name, int sortingOrder, bool dontDestroy = false)
    {
        EnsureEventSystem();
        var go = new GameObject(name, typeof(RectTransform));
        if (dontDestroy && Application.isPlaying) UnityEngine.Object.DontDestroyOnLoad(go);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 해상도가 바뀌어도 같은 비율
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    // New Input System용 EventSystem (없으면 만듦)
    public static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        if (Application.isPlaying) UnityEngine.Object.DontDestroyOnLoad(es); // [수정] 에디터에서 만들 때는 씬에 저장
    }

    public static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    // 화면 전체를 덮는 판 (앵커 0~1 스트레치)
    public static Image FullScreen(string name, Transform parent, Color color, bool blockClicks = true)
    {
        RectTransform rt = Rect(name, parent);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = blockClicks;
        return img;
    }

    // 가운데 판 (앵커 가운데, 크기 고정)
    public static Image Panel(string name, Transform parent, Vector2 size)
    {
        RectTransform rt = Rect(name, parent);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = Theme.panelSprite;
        img.type = Theme.panelSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        img.color = Theme.panelColor;
        // 금색 테두리
        var outline = rt.gameObject.AddComponent<Outline>();
        outline.effectColor = Theme.accentColor * new Color(1f, 1f, 1f, 0.8f);
        outline.effectDistance = new Vector2(3f, -3f);
        return img;
    }

    public static VerticalLayoutGroup Column(Transform target, float spacing, RectOffset padding, TextAnchor align = TextAnchor.UpperCenter)
    {
        var v = target.gameObject.AddComponent<VerticalLayoutGroup>();
        v.spacing = spacing;
        v.padding = padding;
        v.childAlignment = align;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        return v;
    }

    public static void Size(Component c, float width, float height)
    {
        var le = c.GetComponent<LayoutElement>();
        if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
        if (width > 0f) { le.preferredWidth = width; le.minWidth = width; }
        if (height > 0f) { le.preferredHeight = height; le.minHeight = height; }
    }

    // ================= 글자 =================
    // locKey가 있으면 언어에 따라 바뀜 (LocalizedText), 없으면 raw 글자
    public static TextMeshProUGUI Text(string name, Transform parent, string locKey, float size, Color color,
        TextAlignmentOptions align = TextAlignmentOptions.Center, string raw = null)
    {
        RectTransform rt = Rect(name, parent);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = Theme.font;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.overflowMode = TextOverflowModes.Overflow;
        if (!string.IsNullOrEmpty(locKey)) rt.gameObject.AddComponent<LocalizedText>().SetKey(locKey);
        else t.text = raw ?? "";
        return t;
    }

    // ================= 버튼 =================
    public static Button Button(string name, Transform parent, string locKey, float fontSize, Action onClick)
    {
        RectTransform rt = Rect(name, parent);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = Theme.buttonSprite;
        img.type = Theme.buttonSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        img.color = Color.white;
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        b.colors = Colors();
        b.onClick.AddListener(() => { Click(); onClick?.Invoke(); });
        rt.gameObject.AddComponent<MenuSelectSound>();

        TextMeshProUGUI label = Text("Label", rt, locKey, fontSize, Theme.textColor);
        Stretch(label.rectTransform, 12f);
        return b;
    }

    public static ColorBlock Colors()
    {
        ColorBlock c = ColorBlock.defaultColorBlock;
        c.normalColor = Theme.buttonColor;
        c.highlightedColor = Theme.buttonHighlight;
        c.selectedColor = Theme.buttonHighlight; // 키보드로 고른 버튼도 밝게
        c.pressedColor = Theme.buttonPressed;
        c.disabledColor = Theme.buttonColor * new Color(1f, 1f, 1f, 0.5f);
        c.fadeDuration = 0.08f;
        return c;
    }

    public static void Stretch(RectTransform rt, float pad = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(pad, 0f);
        rt.offsetMax = new Vector2(-pad, 0f);
    }

    // ================= 슬라이더 (0~1) =================
    public static Slider Slider(string name, Transform parent, float value, Action<float> onChanged, out TextMeshProUGUI valueLabel)
    {
        RectTransform root = Rect(name, parent);
        var h = root.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 18f;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = true; h.childControlHeight = true;
        h.childForceExpandWidth = false; h.childForceExpandHeight = false;

        RectTransform sRt = Rect("Slider", root);
        Size(sRt, 380f, 36f);
        var slider = sRt.gameObject.AddComponent<Slider>();

        Image bg = FullScreen("Background", sRt, Theme.buttonColor, true);
        bg.rectTransform.anchorMin = new Vector2(0f, 0.35f);
        bg.rectTransform.anchorMax = new Vector2(1f, 0.65f);

        RectTransform fillArea = Rect("Fill Area", sRt);
        fillArea.anchorMin = new Vector2(0f, 0.35f);
        fillArea.anchorMax = new Vector2(1f, 0.65f);
        fillArea.offsetMin = new Vector2(4f, 0f);
        fillArea.offsetMax = new Vector2(-14f, 0f);
        Image fill = FullScreen("Fill", fillArea, Theme.accentColor, false);
        fill.rectTransform.offsetMax = new Vector2(10f, 0f);

        RectTransform handleArea = Rect("Handle Slide Area", sRt);
        Stretch(handleArea, 14f);
        RectTransform handle = Rect("Handle", handleArea);
        handle.sizeDelta = new Vector2(28f, 0f);
        handle.anchorMin = new Vector2(0f, 0f);
        handle.anchorMax = new Vector2(0f, 1f);
        var hImg = handle.gameObject.AddComponent<Image>();
        hImg.sprite = Theme.knobSprite;
        hImg.color = Color.white;

        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle;
        slider.targetGraphic = hImg;
        slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        ColorBlock cb = ColorBlock.defaultColorBlock;
        cb.normalColor = Theme.textColor;
        cb.highlightedColor = Color.white;
        cb.selectedColor = Theme.accentColor;
        cb.pressedColor = Theme.accentColor;
        slider.colors = cb;
        slider.SetValueWithoutNotify(value);
        sRt.gameObject.AddComponent<MenuSelectSound>();

        TextMeshProUGUI label = Text("Value", root, null, 30f, Theme.textColor, TextAlignmentOptions.Left, Percent(value));
        Size(label, 110f, 36f);
        valueLabel = label;
        slider.onValueChanged.AddListener(v => { label.text = Percent(v); onChanged?.Invoke(v); });
        return slider;
    }

    public static string Percent(float v) => $"{Mathf.RoundToInt(v * 100f)}%";

    // ================= 한 줄 (왼쪽 이름 + 오른쪽 조절) =================
    public static RectTransform Row(string name, Transform parent, string labelKey, float height = 60f)
    {
        RectTransform row = Rect(name, parent);
        Size(row, 0f, height);
        var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 24f;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = true; h.childControlHeight = true;
        h.childForceExpandWidth = false; h.childForceExpandHeight = false;
        TextMeshProUGUI label = Text("Label", row, labelKey, 32f, Theme.textColor, TextAlignmentOptions.Left);
        Size(label, 330f, height);
        return row;
    }

    public static TextMeshProUGUI Header(Transform parent, string key)
    {
        TextMeshProUGUI t = Text("Header_" + key, parent, key, 30f, Theme.accentColor, TextAlignmentOptions.Left);
        Size(t, 0f, 48f);
        return t;
    }

    // ================= 소리 =================
    public static void Click() => SoundManager.PlaySfx(Theme.clickSfx, 0.8f);
    public static void Move() => SoundManager.PlaySfx(Theme.moveSfx, 0.5f);

    public static void Select(Selectable s)
    {
        if (s == null || EventSystem.current == null) return;
        EventSystem.current.SetSelectedGameObject(s.gameObject);
    }
}
