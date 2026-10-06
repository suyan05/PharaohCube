using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 플래그 하나당 목표 문구 하나. 가장 먼저 안 끝난 단계를 화면 위에 표시 (기획서 9.12 A안)
[Serializable]
public class RaObjectiveStep
{
    [Tooltip("이 플래그가 켜지면 다음 단계로 넘어감")]
    public string doneFlag = "";

    [TextArea]
    public string objective = "";
}

public class RaObjectiveGuide : MonoBehaviour
{
    [Header("진행 단계 (위에서부터 순서대로)")]
    [SerializeField] private List<RaObjectiveStep> steps = DefaultSteps();

    [Header("HUD")]
    [SerializeField] private int fontSize = 30;
    [SerializeField] private Vector2 hudOffset = new Vector2(0f, -40f);

    private Text objectiveText;
    private bool subscribed;

    private void Start()
    {
        try
        {
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

    private void HandleFlag(string flag)
    {
        Refresh();
    }

    // 아직 안 끝난 첫 단계를 현재 목표로 삼음
    private void Refresh()
    {
        try
        {
            RaObjectiveStep current = FindCurrentStep();
            objectiveText.text = current != null ? $"목표: {current.objective}" : "";
        }
        catch (Exception e)
        {
            Debug.LogError($"[목표 안내] 갱신 오류: {e}");
        }
    }

    private RaObjectiveStep FindCurrentStep()
    {
        foreach (RaObjectiveStep step in steps)
        {
            if (!HasFlag(step.doneFlag)) return step;
        }
        return null;
    }

    private bool HasFlag(string flag)
    {
        if (string.IsNullOrEmpty(flag)) return false;
        return GameManager.Instance != null && GameManager.Instance.HasFlag(flag);
    }

    // ================= HUD =================
    private void BuildHud()
    {
        var canvasGo = new GameObject("RaObjectiveCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
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
        objectiveText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        objectiveText.fontSize = fontSize;
        objectiveText.color = Hex("#E8B23A");
        objectiveText.alignment = TextAnchor.MiddleCenter;
        objectiveText.raycastTarget = false;
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }

    [ContextMenu("현재 목표 다시 계산")]
    private void CheatRefresh() => Refresh();

    // ================= 기본 데이터 (기획서 4.2 플래그 순서) =================
    private static List<RaObjectiveStep> DefaultSteps()
    {
        return new List<RaObjectiveStep>
        {
            Make("F_RA_CIRCUIT_1", "서쪽 벽의 빛 회로를 조사하자"),
            Make("F_RA_P1_DONE", "주황 렌즈를 들고 동쪽 그림자 다이얼로 가자"),
            Make("F_RA_CIRCUIT_2", "거울 조각 1을 들고 회로판 빈 슬롯에 끼우자"),
            Make("F_RA_P3_DONE", "청색 안료를 들고 남동쪽 일식 석탁으로 가자"),
            Make("F_RA_CIRCUIT_3", "거울 조각 2와 프리즘을 들고 회로판으로 돌아가자"),
            Make("F_RA_P4_DONE", "황동 열쇠를 들고 남서쪽 카드 벤치로 가자"),
            Make("F_RA_CIRCUIT_4", "거울 조각 3을 들고 회로판 마지막 슬롯에 끼우자"),
            Make("F_RA_P5_DONE", "북쪽 태양 제단에서 하루의 순환을 완성하자")
        };
    }

    private static RaObjectiveStep Make(string flag, string objective)
    {
        return new RaObjectiveStep { doneFlag = flag, objective = objective };
    }
}