using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 플래그 하나당 목표 문구 하나. 가장 먼저 안 끝난 단계를 화면 위에 표시 (기획서 9.12 A안)
[Serializable]
public class RaObjectiveStep
{
    [Tooltip("이 플래그가 켜지면 다음 단계로 넘어감 (비우면 끝나지 않는 마지막 단계)")]
    public string doneFlag = "";

    [TextArea]
    public string objective = "";
}

// 퍼즐 오버레이가 열려 있는 동안은 목표 문구를 숨김 (퍼즐 제목 가림 방지)
// 단계 목록은 Inspector(씬)에 저장됨 -> 코드 기본값을 바꿨으면 우클릭 -> "기본 단계로 다시 채우기"
public class RaObjectiveGuide : MonoBehaviour
{
    [Header("진행 단계 (위에서부터 순서대로)")]
    [SerializeField] private List<RaObjectiveStep> steps = DefaultSteps();

    [Header("HUD")]
    [SerializeField] private int fontSize = 30;
    [SerializeField] private Vector2 hudOffset = new Vector2(0f, -40f);

    private GameObject objectiveBox;
    private Text objectiveText;
    private bool subscribed;
    private bool focusSubscribed;

    private void Start()
    {
        try
        {
            BuildHud();
            PuzzleOverlayFocus.OnFocusChanged += HandleFocus;
            focusSubscribed = true;

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
        if (focusSubscribed) PuzzleOverlayFocus.OnFocusChanged -= HandleFocus;
    }

    private void HandleFlag(string flag)
    {
        Refresh();
    }

    private void HandleFocus(bool puzzleOpen)
    {
        UpdateBoxVisible();
    }

    // 아직 안 끝난 첫 단계를 현재 목표로 삼음
    private void Refresh()
    {
        try
        {
            RaObjectiveStep current = FindCurrentStep();
            objectiveText.text = current != null ? $"목표: {current.objective}" : "";
            UpdateBoxVisible();
        }
        catch (Exception e)
        {
            Debug.LogError($"[목표 안내] 갱신 오류: {e}");
        }
    }

    // 퍼즐이 열려 있거나 문구가 비어 있으면 띠째로 숨김
    private void UpdateBoxVisible()
    {
        if (objectiveBox == null || objectiveText == null) return;

        bool hasText = !string.IsNullOrEmpty(objectiveText.text);
        objectiveBox.SetActive(hasText && !PuzzleOverlayFocus.IsActive);
    }

    private RaObjectiveStep FindCurrentStep()
    {
        if (steps == null) return null;

        foreach (RaObjectiveStep step in steps)
        {
            if (step != null && !HasFlag(step.doneFlag)) return step;
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
        RectTransform root = BuildCanvas();
        RectTransform box = BuildBox(root);
        objectiveText = BuildText(box);
    }

    private RectTransform BuildCanvas()
    {
        var canvasGo = new GameObject("RaObjectiveCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90; // 퍼즐 오버레이보다 아래, 메시지 UI보다 아래

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        return canvasGo.GetComponent<RectTransform>();
    }

    private RectTransform BuildBox(RectTransform root)
    {
        objectiveBox = new GameObject("ObjectiveBox", typeof(RectTransform), typeof(Image));
        var box = objectiveBox.GetComponent<RectTransform>();
        box.SetParent(root, false);
        box.anchorMin = box.anchorMax = new Vector2(0.5f, 1f);
        box.pivot = new Vector2(0.5f, 1f);
        box.anchoredPosition = hudOffset;
        box.sizeDelta = new Vector2(900f, 62f);

        Image boxImage = objectiveBox.GetComponent<Image>();
        boxImage.color = new Color(0.1f, 0.07f, 0.04f, 0.8f);
        boxImage.raycastTarget = false;
        return box;
    }

    private Text BuildText(RectTransform box)
    {
        var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        var rt = textGo.GetComponent<RectTransform>();
        rt.SetParent(box, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(20f, 0f);
        rt.offsetMax = new Vector2(-20f, 0f);

        Text t = textGo.GetComponent<Text>();
        t.font = PuzzleUITheme.GetBodyFont();
        t.fontSize = fontSize;
        t.color = Hex("#E8B23A");
        t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false;
        return t;
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }

    [ContextMenu("현재 목표 다시 계산")]
    private void CheatRefresh() => Refresh();

    // 씬에 저장된 단계 목록을 아래 기본값(낮 + 밤)으로 덮어씀
    [ContextMenu("기본 단계로 다시 채우기 (낮 + 밤)")]
    private void ResetToDefaultSteps()
    {
        try
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(this, "목표 단계 다시 채우기");
#endif
            steps = DefaultSteps();
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
            if (Application.isPlaying && objectiveText != null) Refresh();
            Debug.Log($"[목표 안내] 기본 단계 {steps.Count}개로 다시 채움 (씬 저장 Ctrl+S 잊지 말기)");
        }
        catch (Exception e)
        {
            Debug.LogError($"[목표 안내] 기본 단계 채우기 오류: {e}");
        }
    }

    // ================= 기본 데이터 (기획서 4.2 플래그 순서 + 밤) =================
    private static List<RaObjectiveStep> DefaultSteps()
    {
        var list = new List<RaObjectiveStep>();
        list.AddRange(DaySteps());
        list.AddRange(NightSteps());
        return list;
    }

    private static List<RaObjectiveStep> DaySteps()
    {
        return new List<RaObjectiveStep>
        {
            Make("F_RA_SHRINE_FIRST_TALKED", "중앙의 라의 성소를 조사하자"),
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

    // 밤: 성소 -> P1 -> P3 -> P4 -> P5 -> 북쪽 문 (밤 퍼즐은 이 순서로만 열림)
    private static List<RaObjectiveStep> NightSteps()
    {
        return new List<RaObjectiveStep>
        {
            Make("F_RA_NIGHT_OPEN", "라의 성소로 돌아가 라와 이야기하자"),
            Make("F_RA_SHRINE_NIGHT_TALKED", "밤이 내려앉았다. 라의 성소에 다시 말을 걸어 보자"),
            Make("F_RA_NIGHT_P1_DONE", "푸른 불이 켜진 동쪽 방, 밤의 그림자를 읽자"),
            Make("F_RA_NIGHT_P3_DONE", "남동쪽 일식 석탁에서 개기 일식을 완성하자"),
            Make("F_RA_NIGHT_P4_DONE", "남서쪽 카드 벤치에 죽은 왕의 하루를 새기자"),
            Make("F_RA_NIGHT_P5_DONE", "북쪽 밤의 제단에서 태양의 항해를 완성하자"),
            Make("", "태양 형판을 들고 북쪽 문으로 나가자")
        };
    }

    private static RaObjectiveStep Make(string flag, string objective)
    {
        return new RaObjectiveStep { doneFlag = flag, objective = objective };
    }
}