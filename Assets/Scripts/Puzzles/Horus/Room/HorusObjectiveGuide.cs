using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 진행 단계 하나: 어떤 플래그가 켜지면 이 단계가 끝나는지, 그동안 어디로 안내할지
[Serializable]
public class HorusObjectiveStep
{
    [Tooltip("이 플래그가 켜지면 다음 단계로 넘어감")]
    public string doneFlag = "";

    [TextArea]
    public string objective = "";

    [Tooltip("밝힐 길 (Environment의 Path 오브젝트)")]
    public SpriteRenderer pathToLight;

    [Tooltip("반짝일 목적지 (퍼즐 본체)")]
    public SpriteRenderer target;
}

// 목표 HUD + 별길 가이드 + 잠김 표시를 플래그에 맞춰 한 번에 갱신 (기획서 9.2, 9.3, 9.12)
public class HorusObjectiveGuide : MonoBehaviour
{
    [Header("길 (전부 넣어두면 현재 목표 외에는 어둡게 처리)")]
    [SerializeField] private List<SpriteRenderer> allPaths = new List<SpriteRenderer>();

    [Header("퍼즐 (전부 넣어두면 잠긴 것은 어둡게 처리)")]
    [SerializeField] private List<SpriteRenderer> allTargets = new List<SpriteRenderer>();

    [Header("진행 단계 (위에서부터 순서대로)")]
    [SerializeField] private List<HorusObjectiveStep> steps = new List<HorusObjectiveStep>();

    [Header("색")]
    [SerializeField] private Color pathDim = new Color(0.12f, 0.18f, 0.31f);
    [SerializeField] private Color pathLit = new Color(0.24f, 0.48f, 0.84f);
    [SerializeField] private Color targetLocked = new Color(0.28f, 0.22f, 0.26f);

    [Header("깜빡임")]
    [SerializeField] private float pulseSpeed = 2.2f;

    [Header("HUD")]
    [SerializeField] private int fontSize = 30;
    [SerializeField] private Vector2 hudOffset = new Vector2(0f, -40f);

    private readonly Dictionary<SpriteRenderer, Color> originalColors = new Dictionary<SpriteRenderer, Color>();
    private Text objectiveText;
    private HorusObjectiveStep current;
    private bool subscribed;

    private void Start()
    {
        try
        {
            CacheColors();
            BuildHud();
            if (GameManager.Instance == null)
            {
                Debug.LogError("[목표 안내] GameManager가 없음");
                return;
            }
            GameManager.Instance.OnFlagChanged += HandleFlag;
            subscribed = true;
            Refresh();
        }
        catch (Exception e)
        {
            Debug.LogError($"[목표 안내] Start 오류: {e}");
        }
    }

    private void OnDestroy()
    {
        if (subscribed && GameManager.Instance != null) GameManager.Instance.OnFlagChanged -= HandleFlag;
    }

    private void CacheColors()
    {
        foreach (SpriteRenderer sr in allTargets)
        {
            if (sr != null && !originalColors.ContainsKey(sr)) originalColors[sr] = sr.color;
        }
    }

    private void HandleFlag(string flag)
    {
        Refresh();
    }

    // 아직 안 끝난 첫 단계를 현재 목표로 삼음
    private void Refresh()
    {
        try
        {
            current = FindCurrentStep();
            objectiveText.text = current != null ? $"목표: {current.objective}" : "모든 성좌를 되찾았다.";
            ApplyPaths();
            ApplyTargets();
        }
        catch (Exception e)
        {
            Debug.LogError($"[목표 안내] 갱신 오류: {e}");
        }
    }

    private HorusObjectiveStep FindCurrentStep()
    {
        foreach (HorusObjectiveStep step in steps)
        {
            if (!HasFlag(step.doneFlag)) return step;
        }
        return null;
    }

    private void ApplyPaths()
    {
        foreach (SpriteRenderer sr in allPaths)
        {
            if (sr != null) sr.color = pathDim;
        }
    }

    // 잠긴 퍼즐은 어둡게, 열린 퍼즐은 원래 색
    private void ApplyTargets()
    {
        foreach (SpriteRenderer sr in allTargets)
        {
            if (sr == null || !originalColors.ContainsKey(sr)) continue;
            bool isGoal = current != null && current.target == sr;
            sr.color = isGoal ? originalColors[sr] : targetLocked;
        }
    }

    // 현재 목표의 길과 퍼즐만 천천히 밝아졌다 어두워졌다 함
    private void Update()
    {
        if (current == null) return;
        float t = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed);
        if (current.pathToLight != null)
        {
            current.pathToLight.color = Color.Lerp(pathDim, pathLit, t);
        }
        if (current.target != null && originalColors.ContainsKey(current.target))
        {
            Color baseColor = originalColors[current.target];
            current.target.color = Color.Lerp(baseColor * 0.75f, baseColor, t);
        }
    }

    private bool HasFlag(string flag)
    {
        if (string.IsNullOrEmpty(flag)) return false;
        return GameManager.Instance != null && GameManager.Instance.HasFlag(flag);
    }

    // ================= HUD =================
    private void BuildHud()
    {
        var canvasGo = new GameObject("ObjectiveCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90; // 퍼즐 오버레이보다 아래, 메시지 UI보다 아래

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        RectTransform root = canvasGo.GetComponent<RectTransform>();

        var boxGo = new GameObject("ObjectiveBox", typeof(RectTransform), typeof(Image));
        var box = boxGo.GetComponent<RectTransform>();
        box.SetParent(root, false);
        box.anchorMin = box.anchorMax = new Vector2(0.5f, 1f);
        box.pivot = new Vector2(0.5f, 1f);
        box.anchoredPosition = hudOffset;
        box.sizeDelta = new Vector2(900f, 62f);
        Image boxImage = boxGo.GetComponent<Image>();
        boxImage.color = new Color(0.1f, 0.07f, 0.04f, 0.8f);
        boxImage.raycastTarget = false;

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        var rt = textGo.GetComponent<RectTransform>();
        rt.SetParent(box, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(20f, 0f);
        rt.offsetMax = new Vector2(-20f, 0f);

        objectiveText = textGo.GetComponent<Text>();
        objectiveText.font = HorusUiFactory.LegacyFont();
        objectiveText.fontSize = fontSize;
        objectiveText.color = HorusUiFactory.Hex("#E8B23A");
        objectiveText.alignment = TextAnchor.MiddleCenter;
        objectiveText.raycastTarget = false;
    }

    // ================= 개발용 =================
    [ContextMenu("현재 목표 다시 계산")]
    private void CheatRefresh() => Refresh();
}