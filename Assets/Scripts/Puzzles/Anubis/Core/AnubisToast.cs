using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 화면 위쪽 안내 메시지 (UI는 실행 시 코드로 생성, Managers에 부착)
// 다른 스크립트는 AnubisToast.Show("문장") 한 줄로 사용 (정적 이벤트, 싱글톤 아님)
public class AnubisToast : MonoBehaviour
{
    private static event Action<string, float> OnShow;

    // 어디서든 호출 가능
    public static void Show(string message, float duration = 3f)
    {
        if (string.IsNullOrEmpty(message)) return;
        Debug.Log($"[Toast] {message}");
        OnShow?.Invoke(message, duration);
    }

    [Header("연결")]
    [Tooltip("한글 TMP 폰트 (MonoplexKR-Regular SDF)")]
    [SerializeField] private TMP_FontAsset font;

    [Header("시작 메시지 (튜토리얼 안내 등, 비워도 됨)")]
    [TextArea(2, 5)]
    [SerializeField] private string startMessage = "";
    [Range(1f, 15f)]
    [SerializeField] private float startDuration = 6f;

    private GameObject box;
    private TextMeshProUGUI text;
    private Coroutine hideRoutine;

    private void Awake() => BuildUI();
    private void OnEnable() => OnShow += Display;
    private void OnDisable() => OnShow -= Display;

    private void Start()
    {
        if (!string.IsNullOrEmpty(startMessage)) Display(startMessage, startDuration);
    }

    private void Display(string message, float duration)
    {
        text.text = message;
        box.SetActive(true);
        if (hideRoutine != null) StopCoroutine(hideRoutine);
        hideRoutine = StartCoroutine(HideAfter(duration));
    }

    private IEnumerator HideAfter(float duration)
    {
        yield return new WaitForSecondsRealtime(duration);
        box.SetActive(false);
        hideRoutine = null;
    }

    private void BuildUI()
    {
        var canvasGo = new GameObject("Toast_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasGo.layer = 5;
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 55; // 퍼즐 패널(50) 위, 양피지 창(60) 아래

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // 화면 위쪽 가운데 상자
        box = new GameObject("ToastBox", typeof(RectTransform), typeof(Image));
        box.layer = 5;
        var rt = (RectTransform)box.transform;
        rt.SetParent(canvasGo.transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0, -40);
        rt.sizeDelta = new Vector2(1200, 150);
        var bg = box.GetComponent<Image>();
        bg.color = new Color(0.10f, 0.07f, 0.04f, 0.88f);
        bg.raycastTarget = false;

        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.layer = 5;
        var trt = (RectTransform)textGo.transform;
        trt.SetParent(rt, false);
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(30, 16);
        trt.offsetMax = new Vector2(-30, -16);

        text = textGo.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.color = new Color(0.96f, 0.90f, 0.72f, 1f);
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 18;
        text.fontSizeMax = 30;
        text.raycastTarget = false;

        box.SetActive(false);
    }
}