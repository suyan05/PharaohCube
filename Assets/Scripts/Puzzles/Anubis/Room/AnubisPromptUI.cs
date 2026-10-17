using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 화면 아래 가운데 상호작용 안내 ("E: 제단 조사", "E: 비석 읽기" 등)
// - 라 방 GameMessageUI의 아래 안내만 떼어 온 아누비스 버전 (TMP)
//   (GameMessageUI를 그대로 쓰면 아이템 획득 알림이 AnubisToast와 두 번 뜨고 ID가 그대로 보임)
// - 라 방 스크립트(RaIntroCutscene 등)가 "[Door] " 로그로 보내는 알림은 AnubisToast로 띄움
public class AnubisPromptUI : MonoBehaviour
{
    private const string DoorTag = "[Door] ";

    [Header("연결 (비우면 Player에서 자동으로 찾음)")]
    [SerializeField] private InteractionSystem interaction;
    [SerializeField] private PlayerController2D playerController;

    [Header("모양")]
    [Tooltip("한글 TMP 폰트 (MonoplexKR-Regular SDF)")]
    [SerializeField] private TMP_FontAsset font;
    [Range(16, 40)] [SerializeField] private int fontSize = 28;
    [Tooltip("화면 아래 가운데 기준 위치 (1920 x 1080 기준)")]
    [SerializeField] private Vector2 offset = new Vector2(0f, 60f);
    [SerializeField] private Vector2 boxSize = new Vector2(440f, 64f);
    [SerializeField] private int sortingOrder = 45;

    [Header("라 방 알림 연결")]
    [Tooltip("켜면 \"[Door] \" 로그를 AnubisToast로 띄움 (입장 연출 안내 문구 등)")]
    [SerializeField] private bool forwardDoorLogs = true;
    [Range(1f, 10f)] [SerializeField] private float doorToastTime = 3.5f;

    private GameObject box;
    private TextMeshProUGUI text;
    private string shown; // 같은 문구면 TMP를 다시 갱신하지 않음

    private void Awake()
    {
        Build();
    }

    private void OnEnable()
    {
        if (forwardDoorLogs) Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void Update()
    {
        if (interaction == null || playerController == null) FindPlayer();

        bool canMove = playerController == null || playerController.enabled;
        string prompt = (interaction != null && canMove && !PuzzleOverlayFocus.IsActive) ? interaction.CurrentPrompt : null;

        bool show = !string.IsNullOrEmpty(prompt);
        if (box.activeSelf != show) box.SetActive(show);
        if (show && prompt != shown)
        {
            shown = prompt;
            text.text = prompt;
        }
    }

    private void FindPlayer()
    {
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController2D>();
        if (interaction == null && playerController != null) interaction = playerController.GetComponent<InteractionSystem>();
    }

    private void HandleLog(string msg, string stackTrace, LogType type)
    {
        if (type != LogType.Log || msg == null || !msg.StartsWith(DoorTag, StringComparison.Ordinal)) return;
        AnubisToast.Show(msg.Substring(DoorTag.Length), doorToastTime);
    }

    private void Build()
    {
        var canvasGo = new GameObject("PromptCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasGo.layer = 5;
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        box = new GameObject("PromptBox", typeof(RectTransform), typeof(Image));
        box.layer = 5;
        var rt = (RectTransform)box.transform;
        rt.SetParent(canvasGo.transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f); // 아래 가운데 고정
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = offset;
        rt.sizeDelta = boxSize;

        var bg = box.GetComponent<Image>();
        bg.color = new Color(0.10f, 0.07f, 0.04f, 0.85f);
        bg.raycastTarget = false;

        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.layer = 5;
        var trt = (RectTransform)textGo.transform;
        trt.SetParent(rt, false);
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(16f, 0f);
        trt.offsetMax = new Vector2(-16f, 0f);

        text = textGo.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.fontSize = fontSize;
        text.enableAutoSizing = true;
        text.fontSizeMin = 16;
        text.fontSizeMax = fontSize;
        text.color = new Color(0.96f, 0.95f, 0.91f); // #F6F1E7
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;

        box.SetActive(false);
    }
}
