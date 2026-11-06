using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 화면 위 가운데 "빈 홈 3개" (기획서 7장: 신전에 들어가면 빈 홈 3개, 단서를 주우면 하나씩 빛남)
// - 수위계 기둥을 봤거나 단서를 하나라도 주우면 나타남
// - 붙이는 곳: 허브 씬 아무 곳 (Tools/Sobek 메뉴가 SobekHub에 붙임). 캔버스는 실행 중 스스로 만듦
public class SobekClueHud : MonoBehaviour
{
    [SerializeField] private Sprite[] clueIcons = new Sprite[3];
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private Color emptyColor = new Color(0.1f, 0.08f, 0.06f, 0.75f);
    [SerializeField] private Color setColor = new Color(0.55f, 0.9f, 1f, 1f);

    private Image[] slots;
    private GameObject panel;
    private TextMeshProUGUI label;
    private int lastMask = -1;

    private static SobekClueHud instance;

    private void Awake()
    {
        instance = this;
        Build();
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    // 단서를 줍거나 끼울 때 부름 (매 프레임 검사 대신 이벤트로 갱신)
    public static void Refresh()
    {
        if (instance != null) instance.Apply(true);
    }

    private void Start() => Apply(false);

    private void Apply(bool force)
    {
        int mask = 0;
        for (int i = 0; i < 3; i++)
        {
            if (SobekStory.HasFlag(SobekIds.ClueGot(i + 1))) mask |= 1 << i;
            if (SobekStory.HasFlag(SobekIds.ClueSet(i + 1))) mask |= 1 << (i + 3);
        }
        bool show = (mask != 0 || SobekStory.HasFlag(SobekIds.FlagSocketSeen)) && !SobekStory.HasFlag(SobekIds.FlagHiddenOpen);
        if (panel.activeSelf != show) panel.SetActive(show);
        if (!force && mask == lastMask) return;
        lastMask = mask;
        for (int i = 0; i < 3; i++)
        {
            bool got = (mask & (1 << i)) != 0;
            bool set = (mask & (1 << (i + 3))) != 0;
            slots[i].sprite = got || set ? clueIcons[i] : null;
            slots[i].color = set ? setColor : (got ? Color.white : emptyColor);
        }
    }

    private void Build()
    {
        var canvasGo = new GameObject("SobekClueHud_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 41;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasGo.transform, false);
        var rt = (RectTransform)panel.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f); // 위 가운데 고정
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -20f);
        rt.sizeDelta = new Vector2(330f, 130f);
        var bg = panel.GetComponent<Image>();
        bg.color = new Color(0.08f, 0.06f, 0.04f, 0.6f);
        bg.raycastTarget = false;

        slots = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            var go = new GameObject($"Slot_{i + 1}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(panel.transform, false);
            var srt = (RectTransform)go.transform;
            srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.5f);
            srt.anchoredPosition = new Vector2(-100f + i * 100f, 12f);
            srt.sizeDelta = new Vector2(78f, 78f);
            slots[i] = go.GetComponent<Image>();
            slots[i].preserveAspect = true;
            slots[i].raycastTarget = false;
        }

        var lgo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        lgo.transform.SetParent(panel.transform, false);
        var lrt = (RectTransform)lgo.transform;
        lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 0f);
        lrt.pivot = new Vector2(0.5f, 0f);
        lrt.anchoredPosition = new Vector2(0f, 6f);
        lrt.sizeDelta = new Vector2(320f, 30f);
        label = lgo.GetComponent<TextMeshProUGUI>();
        if (font != null) label.font = font;
        label.text = "수위계의 빈 눈금";
        label.fontSize = 20f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(1f, 0.9f, 0.7f);
        label.raycastTarget = false;
        panel.SetActive(false);
    }

#if UNITY_EDITOR
    public void EditorSetup(Sprite[] icons, TMP_FontAsset f) { clueIcons = icons; font = f; }
#endif
}
