using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/*이슈 : 화면 메시지 표시 (빌드에서는 콘솔이 안 보임)*/
// - 하단 가운데: 상호작용 안내 (E: 비석 읽기 등)
// - 오른쪽 위: 알림 토스트 (노트, 아이템 획득, 회로 점등, 문)
// 기존 Debug.Log 중 플레이어가 봐야 하는 것만 골라서 화면에 띄움
// 퍼즐 오버레이가 열려 있는 동안 토스트는 대기열에 모아뒀다가 닫힌 뒤에 띄움 (퍼즐 화면 가림 방지)
public class GameMessageUI : MonoBehaviour
{
    [Header("연결 (둘 다 Player 오브젝트)")]
    [SerializeField] private InteractionSystem interaction;
    [SerializeField] private PlayerController2D playerController;

    [Header("표시 시간 (기획서 7: 토스트 2.5초)")]
    [SerializeField] private float toastTime = 2.5f;
    [SerializeField] private float noteTime = 6f;   // 노트는 읽을 시간이 필요해서 길게
    [SerializeField] private int maxToasts = 4;     // 한 번에 보이는 최대 개수

    [Header("퍼즐 중 대기열")]
    [SerializeField] private int maxPendingToasts = 8;   // 퍼즐 중에 모아둘 최대 개수
    [SerializeField] private float minReshowTime = 1.5f; // 퍼즐 열 때 대기열로 옮긴 알림의 최소 표시 시간

    private class Toast
    {
        public GameObject go;
        public float remain;
        public string text;
        public bool highlight;
    }

    private struct PendingToast
    {
        public string text;
        public float time;
        public bool highlight;
    }

    private readonly List<Toast> toasts = new List<Toast>();
    private readonly List<PendingToast> pending = new List<PendingToast>();
    private RectTransform toastRoot;
    private GameObject promptBox;
    private Text promptText;
    private Font font;
    private string lastPuzzleMessage;

    private Color boxColor, paper, gold;

    // 아이템 ID -> 화면에 보여줄 이름
    private static readonly Dictionary<string, string> ItemNames = new Dictionary<string, string>
    {
        { "ITEM_LENS_SUN",      "주황 렌즈 '태양의 눈'" },
        { "FRAG_1",             "거울 조각 1" },
        { "ITEM_PIGMENT_NIGHT", "청색 안료 '밤의 먹물'" },
        { "FRAG_2",             "거울 조각 2" },
        { "PRISM",              "프리즘" },
        { "ITEM_KEY_BRASS",     "황동 열쇠" },
        { "FRAG_3",             "거울 조각 3" },
        { "MIRROR_A",           "거울 A" },
        { "MIRROR_B",           "거울 B" },
        { "PLATE_RA",           "태양 형판" },
        // 호루스의 방
        { "ITEM_FEATHER",       "매의 깃털" },
        { "HO_STAR_FRAG_1",     "별 조각 1" },
        { "ITEM_SILVER_THREAD", "은빛 실" },
        { "HO_STAR_FRAG_2",     "별 조각 2" },
        { "ITEM_MOON_EYE",      "달의 눈" },
        { "ITEM_SEAL_FALCON",   "매 인장" },
        { "HO_STAR_FRAG_3",     "별 조각 3" },
        { "PLATE_HORUS",        "호루스 형판" }
    };

    // 호루스 퍼즐 로그 머리말 (이 말머리로 시작하면 아래 규칙에 따라 토스트)
    private static readonly string[] HorusTags =
    {
        "[성도] ", "[날개] ", "[우자트] ", "[별길 봉인] ", "[하늘] ", "[달의 눈] ", "[호루스 북문] "
    };

    // 위 말머리 중에서도 이 말이 들어간 것만 화면에 띄움 (조작 안내는 퍼즐 화면에 이미 있음)
    private static readonly string[] HorusKeywords =
    {
        "완성", "얻었다", "획득", "장착", "풀렸다", "열렸다", "되었다", "끼웠다", "털어냈다", "드러났다", "필요"
    };

    private void Awake()
    {
        boxColor = new Color(0.1f, 0.07f, 0.04f, 0.85f);
        ColorUtility.TryParseHtmlString("#F6F1E7", out paper);
        ColorUtility.TryParseHtmlString("#E8B23A", out gold);
        font = PuzzleUITheme.GetBodyFont();

        BuildCanvas();
    }

    private void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
        PuzzleOverlayFocus.OnFocusChanged += HandleFocusChanged;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
        PuzzleOverlayFocus.OnFocusChanged -= HandleFocusChanged;
    }

    private void Update()
    {
        // 1) 상호작용 안내: 오버레이가 열려 있으면(이동이 꺼져 있으면) 숨김
        bool canMove = playerController == null || playerController.enabled;
        string prompt = (interaction != null && canMove) ? interaction.CurrentPrompt : null;

        bool show = !string.IsNullOrEmpty(prompt);
        promptBox.SetActive(show);
        if (show) promptText.text = prompt;

        // 2) 토스트 시간 줄이기, 끝난 건 제거
        for (int i = toasts.Count - 1; i >= 0; i--)
        {
            toasts[i].remain -= Time.deltaTime;
            if (toasts[i].remain <= 0f)
            {
                Destroy(toasts[i].go);
                toasts.RemoveAt(i);
            }
        }
        LayoutToasts();
    }

    // ================= 퍼즐 열림/닫힘 =================
    private void HandleFocusChanged(bool puzzleOpen)
    {
        try
        {
            if (puzzleOpen) MoveShownToastsToPending();
            else FlushPending();
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameMessageUI] 퍼즐 열림/닫힘 처리 오류: {e}");
        }
    }

    // 퍼즐을 여는 순간 떠 있던 알림은 닫은 뒤에 다시 보여줌
    private void MoveShownToastsToPending()
    {
        foreach (Toast t in toasts)
        {
            QueueToast(t.text, Mathf.Max(t.remain, minReshowTime), t.highlight);
            Destroy(t.go);
        }
        toasts.Clear();
    }

    private void FlushPending()
    {
        var copy = new List<PendingToast>(pending);
        pending.Clear();

        foreach (PendingToast p in copy)
        {
            ShowToast(p.text, p.time, p.highlight);
        }
    }

    private void QueueToast(string text, float time, bool highlight)
    {
        if (pending.Count >= maxPendingToasts) pending.RemoveAt(0); // 너무 많으면 오래된 것부터 버림
        pending.Add(new PendingToast { text = text, time = time, highlight = highlight });
    }

    // ================= 로그 -> 화면 메시지 =================
    private void HandleLog(string msg, string stackTrace, LogType type)
    {
        if (type != LogType.Log) return;

        // 노트 1~7
        if (msg.StartsWith("[노트"))
        {
            AddToast(msg, noteTime, true);
            return;
        }

        // 아이템 획득
        if (msg.StartsWith("[Item] 획득: "))
        {
            string id = msg.Substring("[Item] 획득: ".Length).Trim();
            string name = ItemNames.ContainsKey(id) ? ItemNames[id] : id;
            AddToast($"획득: {name}", toastTime, false);
            return;
        }

        // 북문 (라의 방)
        if (msg.StartsWith("[Door] "))
        {
            AddToast(msg.Substring("[Door] ".Length), toastTime, false);
            return;
        }

        // 회로판 점등 / 안내 (라의 방)
        if (msg.StartsWith("[P2]"))
        {
            HandleCircuitLog(msg);
            return;
        }

        HandleHorusLog(msg);
    }

    private void HandleCircuitLog(string msg)
    {
        if (msg.Contains("점등!"))
        {
            int g = msg.IndexOf("G");
            if (g >= 0) AddToast(msg.Substring(g), toastTime, true);
        }
        else if (msg.Contains("빛은 닿았지만"))
        {
            AddToast("빛은 닿았지만 문양이 반응하지 않는다. 다른 조각이 필요한 것 같다.", toastTime, false);
        }
        else if (msg.Contains("4문양 전부 점등"))
        {
            AddToast("4문양 전부 점등 - 태양 제단이 깨어났다", toastTime, true);
        }
    }

    // 호루스 퍼즐 메시지: 중요한 것만 골라서 토스트
    private void HandleHorusLog(string msg)
    {
        string body = StripHorusTag(msg);
        if (body == null || !ContainsKeyword(body)) return;
        if (body == lastPuzzleMessage) return; // 같은 안내가 연달아 뜨는 것 방지
        lastPuzzleMessage = body;
        bool highlight = body.Contains("완성") || body.Contains("얻었다") || body.Contains("되었다");
        AddToast(body, toastTime, highlight);
    }

    private string StripHorusTag(string msg)
    {
        foreach (string tag in HorusTags)
        {
            if (msg.StartsWith(tag)) return msg.Substring(tag.Length).Trim();
        }
        return null;
    }

    private bool ContainsKeyword(string body)
    {
        foreach (string word in HorusKeywords)
        {
            if (body.Contains(word)) return true;
        }
        return false;
    }

    // 퍼즐이 열려 있으면 대기열로, 아니면 바로 표시
    private void AddToast(string text, float time, bool highlight)
    {
        if (PuzzleOverlayFocus.IsActive)
        {
            QueueToast(text, time, highlight);
            return;
        }
        ShowToast(text, time, highlight);
    }

    private void ShowToast(string text, float time, bool highlight)
    {
        // 너무 많으면 가장 오래된 것부터 제거
        if (toasts.Count >= maxToasts)
        {
            Destroy(toasts[0].go);
            toasts.RemoveAt(0);
        }

        GameObject box = CreateToastBox(text, highlight);
        toasts.Add(new Toast { go = box, remain = time, text = text, highlight = highlight });
        LayoutToasts();
    }

    private GameObject CreateToastBox(string text, bool highlight)
    {
        var box = new GameObject("Toast", typeof(RectTransform), typeof(Image));
        var rt = box.GetComponent<RectTransform>();
        rt.SetParent(toastRoot, false);
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(480, 90);
        box.GetComponent<Image>().color = boxColor;
        box.GetComponent<Image>().raycastTarget = false;

        Text t = CreateText(rt, text, 22, highlight ? gold : paper);
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.alignment = TextAnchor.MiddleLeft;
        var trt = t.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(16, 6);
        trt.offsetMax = new Vector2(-16, -6);
        return box;
    }

    // 최신 토스트가 맨 위
    private void LayoutToasts()
    {
        for (int i = 0; i < toasts.Count; i++)
        {
            int order = toasts.Count - 1 - i;
            var rt = (RectTransform)toasts[i].go.transform;
            rt.anchoredPosition = new Vector2(0, -order * 100f);
        }
    }

    // ================= 화면 만들기 =================
    private void BuildCanvas()
    {
        // 다른 오버레이보다 항상 위에 그려지는 전용 Canvas
        var canvasGo = new GameObject("GameMessageCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        RectTransform root = canvasGo.GetComponent<RectTransform>();
        BuildToastRoot(root);
        BuildPromptBox(root);
    }

    // 토스트 영역 (오른쪽 위)
    private void BuildToastRoot(RectTransform root)
    {
        var toastGo = new GameObject("ToastRoot", typeof(RectTransform));
        toastRoot = toastGo.GetComponent<RectTransform>();
        toastRoot.SetParent(root, false);
        toastRoot.anchorMin = toastRoot.anchorMax = new Vector2(1f, 1f);
        toastRoot.pivot = new Vector2(1f, 1f);
        toastRoot.anchoredPosition = new Vector2(-30, -90);
        toastRoot.sizeDelta = new Vector2(480, 500);
    }

    // 상호작용 안내 (하단 가운데)
    private void BuildPromptBox(RectTransform root)
    {
        promptBox = new GameObject("PromptBox", typeof(RectTransform), typeof(Image));
        var prt = promptBox.GetComponent<RectTransform>();
        prt.SetParent(root, false);
        prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0f);
        prt.pivot = new Vector2(0.5f, 0f);
        prt.anchoredPosition = new Vector2(0, 40);
        prt.sizeDelta = new Vector2(440, 64);
        promptBox.GetComponent<Image>().color = boxColor;
        promptBox.GetComponent<Image>().raycastTarget = false;

        promptText = CreateText(prt, "", 28, paper);
        var trt = promptText.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        promptBox.SetActive(false);
    }

    private Text CreateText(RectTransform parent, string text, int fontSize, Color color)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        go.GetComponent<RectTransform>().SetParent(parent, false);

        var t = go.GetComponent<Text>();
        t.font = font;
        t.text = text;
        t.fontSize = fontSize;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false; // 클릭을 막지 않도록
        return t;
    }
}