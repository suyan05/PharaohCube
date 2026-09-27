using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/*이슈 : 화면 메시지 표시 (빌드에서는 콘솔이 안 보임)*/
// - 하단 가운데: 상호작용 안내 (E: 비석 읽기 등)
// - 오른쪽 위: 알림 토스트 (노트, 아이템 획득, 회로 점등, 문)
// 기존 Debug.Log 중 플레이어가 봐야 하는 것만 골라서 화면에 띄움
public class GameMessageUI : MonoBehaviour
{
    [Header("연결 (둘 다 Player 오브젝트)")]
    [SerializeField] private InteractionSystem interaction;
    [SerializeField] private PlayerController2D playerController;

    [Header("표시 시간 (기획서 7: 토스트 2.5초)")]
    [SerializeField] private float toastTime = 2.5f;
    [SerializeField] private float noteTime = 6f;   // 노트는 읽을 시간이 필요해서 길게
    [SerializeField] private int maxToasts = 4;     // 한 번에 보이는 최대 개수

    private class Toast
    {
        public GameObject go;
        public float remain;
    }

    private readonly List<Toast> toasts = new List<Toast>();
    private RectTransform toastRoot;
    private GameObject promptBox;
    private Text promptText;
    private Font font;

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
        { "PLATE_RA",           "태양 형판" }
    };

    private void Awake()
    {
        boxColor = new Color(0.1f, 0.07f, 0.04f, 0.85f);
        ColorUtility.TryParseHtmlString("#F6F1E7", out paper);
        ColorUtility.TryParseHtmlString("#E8B23A", out gold);
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        BuildCanvas();
    }

    private void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
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

    // ================= 로그 -> 화면 메시지 =================
    private void HandleLog(string msg, string stackTrace, LogType type)
    {
        if (type != LogType.Log) return;

        // 노트 1~6
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

        // 북문
        if (msg.StartsWith("[Door] "))
        {
            AddToast(msg.Substring("[Door] ".Length), toastTime, false);
            return;
        }

        // 회로판 점등 / 안내
        if (msg.StartsWith("[P2]"))
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
    }

    private void AddToast(string text, float time, bool highlight)
    {
        // 너무 많으면 가장 오래된 것부터 제거
        if (toasts.Count >= maxToasts)
        {
            Destroy(toasts[0].go);
            toasts.RemoveAt(0);
        }

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

        toasts.Add(new Toast { go = box, remain = time });
        LayoutToasts();
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

        // 토스트 영역 (오른쪽 위)
        var toastGo = new GameObject("ToastRoot", typeof(RectTransform));
        toastRoot = toastGo.GetComponent<RectTransform>();
        toastRoot.SetParent(root, false);
        toastRoot.anchorMin = toastRoot.anchorMax = new Vector2(1f, 1f);
        toastRoot.pivot = new Vector2(1f, 1f);
        toastRoot.anchoredPosition = new Vector2(-30, -90);
        toastRoot.sizeDelta = new Vector2(480, 500);

        // 상호작용 안내 (하단 가운데)
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