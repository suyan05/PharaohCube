using System;
using UnityEngine;
using UnityEngine.UI;

// 화면 왼쪽 아래 소지품 바: 지금 갖고 있는 아이템만 아이콘 + 이름으로 표시 (씬에 1개)
// - ItemInventory.Has()로 매 프레임 확인 -> 얻으면 톡 커지며 등장, 쓰면(자동 장착 등) 사라짐
// - 퍼즐 / 단서 화면이 열려 플레이어 이동이 꺼져 있으면 숨김 (GameMessageUI 안내 문구와 같은 기준)
public class InventoryHUD : MonoBehaviour
{
    [Serializable]
    private class Entry
    {
        public string itemId = "";
        public string displayName = "";
        [Tooltip("UI_Item_* 아이콘 (비우면 이름 글자로 표시)")]
        public Sprite icon;

        [NonSerialized] public bool held;
        [NonSerialized] public RectTransform slot;
        [NonSerialized] public float popTimer;
    }

    [Header("표시할 아이템 (순서대로 왼쪽부터 놓임)")]
    [SerializeField]
    private Entry[] entries =
    {
        new Entry { itemId = "ITEM_LENS_SUN", displayName = "주황 렌즈" },
        new Entry { itemId = "ITEM_PIGMENT_NIGHT", displayName = "청색 안료" },
        new Entry { itemId = "ITEM_KEY_BRASS", displayName = "황동 열쇠" },
        new Entry { itemId = "FRAG_1", displayName = "거울 조각 1" },
        new Entry { itemId = "FRAG_2", displayName = "거울 조각 2" },
        new Entry { itemId = "FRAG_3", displayName = "거울 조각 3" },
        new Entry { itemId = "PRISM", displayName = "프리즘" },
        new Entry { itemId = "PLATE_RA", displayName = "태양 형판" }
    };

    [Header("연결 (비우면 자동으로 찾음)")]
    [SerializeField] private PlayerController2D playerController;

    [Header("배치 (1920x1080 기준, 화면 왼쪽 아래부터)")]
    [SerializeField] private Vector2 margin = new Vector2(30f, 56f);
    [SerializeField] private float slotSize = 88f;
    [SerializeField] private float spacing = 14f;
    [SerializeField] private float popTime = 0.3f;
    [SerializeField] private int sortingOrder = 50; // GameMessageUI(100)보다 아래

    private GameObject canvasGo;
    private RectTransform barRoot;
    private Text titleText;
    private Font font;
    private Color boxColor, paper, gold;
    private bool warnedNoInventory;

    // ================= 초기화 =================
    private void Awake()
    {
        try
        {
            boxColor = new Color(0.1f, 0.07f, 0.04f, 0.85f);
            paper = Hex("#F6F1E7");
            gold = Hex("#E8B23A");
            font = PuzzleUITheme.GetBodyFont();
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            BuildCanvas();
            foreach (Entry e in entries) BuildSlot(e);
            Layout();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{name}] 소지품 바 초기화 오류: {ex}");
            enabled = false;
        }
    }

    private void Update()
    {
        try
        {
            RefreshVisibility();
            if (!canvasGo.activeSelf) return;

            RefreshHeld();
            AnimatePops();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{name}] 소지품 바 갱신 오류: {ex}");
            enabled = false; // 매 프레임 같은 오류 도배 방지
        }
    }

    // ================= 갱신 =================
    private void RefreshVisibility()
    {
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController2D>();

        bool show = playerController == null || playerController.enabled;
        if (canvasGo.activeSelf != show) canvasGo.SetActive(show);
    }

    private void RefreshHeld()
    {
        ItemInventory inventory = ItemInventory.Instance;
        if (inventory == null)
        {
            if (!warnedNoInventory) Debug.LogWarning($"[{name}] ItemInventory가 없어서 소지품을 못 읽음");
            warnedNoInventory = true;
            return;
        }

        bool changed = false;
        foreach (Entry e in entries)
        {
            if (e == null || e.slot == null) continue;

            bool now = inventory.Has(e.itemId);
            if (now == e.held) continue;

            e.held = now;
            e.popTimer = now ? popTime : 0f;
            changed = true;
        }
        if (changed) Layout();
    }

    // 갖고 있는 것만 왼쪽부터 빈틈없이 줄 세움
    private void Layout()
    {
        int index = 0;
        foreach (Entry e in entries)
        {
            if (e == null || e.slot == null) continue;

            e.slot.gameObject.SetActive(e.held);
            if (!e.held) continue;

            float x = index * (slotSize + spacing) + slotSize * 0.5f;
            e.slot.anchoredPosition = new Vector2(x, slotSize * 0.5f);
            index++;
        }
        titleText.enabled = index > 0;
    }

    // 새로 얻은 아이템: 1.4배에서 원래 크기로
    private void AnimatePops()
    {
        foreach (Entry e in entries)
        {
            if (e == null || e.slot == null || e.popTimer <= 0f) continue;

            e.popTimer = Mathf.Max(0f, e.popTimer - Time.deltaTime);
            float t = popTime > 0f ? e.popTimer / popTime : 0f;
            e.slot.localScale = Vector3.one * (1f + 0.4f * t);
        }
    }

    // ================= 화면 만들기 =================
    private void BuildCanvas()
    {
        canvasGo = new GameObject("InventoryCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform, false);

        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var barGo = new GameObject("Bar", typeof(RectTransform));
        barRoot = barGo.GetComponent<RectTransform>();
        barRoot.SetParent(canvasGo.transform, false);
        barRoot.anchorMin = barRoot.anchorMax = Vector2.zero;
        barRoot.pivot = Vector2.zero;
        barRoot.anchoredPosition = margin;
        barRoot.sizeDelta = new Vector2(1000f, slotSize);

        titleText = CreateText(barRoot, "소지품", 20, new Vector2(40f, slotSize + 16f), new Vector2(80f, 28f), gold);
    }

    private void BuildSlot(Entry e)
    {
        if (e == null || string.IsNullOrEmpty(e.itemId)) return;

        Image box = CreateImage(barRoot, $"Slot_{e.itemId}", new Vector2(slotSize, slotSize), boxColor);
        e.slot = box.rectTransform;

        if (e.icon != null)
        {
            Image icon = CreateImage(e.slot, "Icon", new Vector2(slotSize * 0.8f, slotSize * 0.8f), Color.white);
            icon.sprite = e.icon;
            icon.preserveAspect = true;
            icon.rectTransform.anchoredPosition = Vector2.zero;
        }
        else
        {
            CreateText(e.slot, e.displayName, 16, Vector2.zero, new Vector2(slotSize - 8f, slotSize - 8f), paper)
                .horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        Vector2 labelPos = new Vector2(0f, -slotSize * 0.5f - 14f);
        CreateText(e.slot, e.displayName, 16, labelPos, new Vector2(slotSize + spacing + 20f, 24f), paper);
        e.slot.gameObject.SetActive(false);
    }

    // ================= 도우미 함수 =================
    // 바 위의 칸은 바의 왼쪽 아래 기준, 칸 안의 아이콘은 칸 가운데 기준
    // 피벗은 항상 가운데 (톡 커지는 연출이 가운데서 일어나게)
    private Image CreateImage(RectTransform parent, string objName, Vector2 size, Color color)
    {
        var go = new GameObject(objName, typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = parent == barRoot ? Vector2.zero : new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;

        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private Text CreateText(RectTransform parent, string text, int fontSize, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        bool onBar = parent == barRoot;
        rt.anchorMin = rt.anchorMax = onBar ? Vector2.zero : new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var t = go.GetComponent<Text>();
        t.font = font;
        t.text = text;
        t.fontSize = fontSize;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }
}