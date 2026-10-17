using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 아누비스 방 목표 한 단계 (보물 아이템 또는 플래그로 완료 판단)
[Serializable]
public class AnubisObjectiveStep
{
    [Tooltip("이 아이템을 갖고 있으면 완료 (서브 퍼즐 보상). 비우면 아이템으로는 판단 안 함")]
    public string doneItemId = "";

    [Tooltip("이 플래그가 켜져 있으면 완료. 둘 다 비우면 끝나지 않는 마지막 단계")]
    public string doneFlag = "";

    [TextArea] public string objective = "";
}

// 화면 위 "목표: …" 띠 (라 방 RaObjectiveGuide의 아누비스 버전)
// - 라 방은 플래그로만 진행하지만, 아누비스는 서브 퍼즐 보상(아이템)으로 진행해서 아이템도 함께 봄
// - "마지막으로 끝낸 단계의 다음 단계"를 목표로 삼음
//   -> 메인 저울 실패 처벌로 보물이 회수되면 자동으로 A1 목표로 돌아감
// - 퍼즐 화면, 대화, 비석 화면이 열려 플레이어 이동이 꺼져 있으면 숨김
public class AnubisObjectiveGuide : MonoBehaviour
{
    [Header("진행 단계 (위에서부터 순서대로)")]
    [SerializeField] private List<AnubisObjectiveStep> steps = DefaultSteps();

    [Header("HUD")]
    [Tooltip("한글 TMP 폰트 (MonoplexKR-Regular SDF)")]
    [SerializeField] private TMP_FontAsset font;
    [Range(16, 48)] [SerializeField] private int fontSize = 30;
    [Tooltip("화면 위 가운데 기준 위치 (1920 x 1080 기준)")]
    [SerializeField] private Vector2 hudOffset = new Vector2(0f, -12f);
    [SerializeField] private Vector2 boxSize = new Vector2(980f, 60f);
    [Tooltip("퍼즐 패널(50), 안내 토스트(55)보다 아래")]
    [SerializeField] private int sortingOrder = 45;

    [Header("목표가 바뀔 때")]
    [Tooltip("켜면 목표가 바뀔 때 띠가 잠깐 반짝임")]
    [SerializeField] private bool flashOnChange = true;

    private GameObject boxGo;
    private Image boxImage;
    private TextMeshProUGUI text;
    private PlayerController2D player;
    private int currentIndex = -2;
    private float flashTimer;
    private bool gmSubscribed, invSubscribed;

    private static readonly Color BoxColor = new Color(0.10f, 0.07f, 0.04f, 0.82f);
    private static readonly Color FlashColor = new Color(0.55f, 0.40f, 0.10f, 0.95f);

    // ================= 초기화 =================
    private void Awake()
    {
        BuildHud();
    }

    private void OnEnable()
    {
        PuzzleOverlayFocus.OnFocusChanged += HandleFocus;
    }

    private void Start()
    {
        // GameManager, ItemInventory는 다른 오브젝트 Awake에서 만들어지므로 Start에서 구독
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnFlagChanged += HandleFlag;
            gmSubscribed = true;
        }
        else Debug.LogWarning($"[{name}] GameManager가 없음 -> 플래그 단계(신상 대화, 메인 클리어)는 넘어가지 않음");

        if (ItemInventory.Instance != null)
        {
            ItemInventory.Instance.OnItemGranted += HandleItem;
            ItemInventory.Instance.OnItemConsumed += HandleItem;
            invSubscribed = true;
        }
        else Debug.LogWarning($"[{name}] ItemInventory가 없음 -> 보물 단계가 넘어가지 않음");

        Refresh();
    }

    private void OnDisable()
    {
        PuzzleOverlayFocus.OnFocusChanged -= HandleFocus;
    }

    private void OnDestroy()
    {
        if (gmSubscribed && GameManager.Instance != null) GameManager.Instance.OnFlagChanged -= HandleFlag;
        if (invSubscribed && ItemInventory.Instance != null)
        {
            ItemInventory.Instance.OnItemGranted -= HandleItem;
            ItemInventory.Instance.OnItemConsumed -= HandleItem;
        }
    }

    private void HandleFlag(string flag) => Refresh();
    private void HandleItem(string itemId) => Refresh();
    private void HandleFocus(bool open) => UpdateVisible();

    // 이동이 꺼지는 순간(대화, 비석, 입장 연출)은 이벤트가 없어서 매 프레임 bool 하나만 확인
    private void Update()
    {
        UpdateVisible();

        if (flashTimer > 0f)
        {
            flashTimer -= Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(flashTimer / 0.8f);
            boxImage.color = Color.Lerp(BoxColor, FlashColor, Mathf.Sin(t * Mathf.PI));
        }
    }

    // ================= 단계 계산 =================
    private void Refresh()
    {
        int index = FindCurrentIndex();
        if (index == currentIndex) return;

        bool first = currentIndex == -2;
        currentIndex = index;

        AnubisObjectiveStep step = (index >= 0 && index < steps.Count) ? steps[index] : null;
        text.text = step != null && !string.IsNullOrEmpty(step.objective) ? $"목표: {step.objective}" : "";
        Debug.Log($"[목표 안내] {index + 1}단계 -> {text.text}", this);

        if (!first && flashOnChange) flashTimer = 0.8f;
        UpdateVisible();
    }

    // 끝난 단계 중 가장 뒤 단계의 다음 단계
    private int FindCurrentIndex()
    {
        if (steps == null || steps.Count == 0) return -1;

        int lastDone = -1;
        for (int i = 0; i < steps.Count; i++)
        {
            if (IsDone(steps[i])) lastDone = i;
        }
        return Mathf.Min(lastDone + 1, steps.Count - 1);
    }

    private static bool IsDone(AnubisObjectiveStep step)
    {
        if (step == null) return false;

        bool byItem = !string.IsNullOrEmpty(step.doneItemId)
                      && ItemInventory.Instance != null && ItemInventory.Instance.Has(step.doneItemId);
        bool byFlag = !string.IsNullOrEmpty(step.doneFlag)
                      && GameManager.Instance != null && GameManager.Instance.HasFlag(step.doneFlag);
        return byItem || byFlag;
    }

    private void UpdateVisible()
    {
        if (boxGo == null) return;

        if (player == null) player = FindAnyObjectByType<PlayerController2D>();
        bool canMove = player == null || player.enabled;
        bool show = canMove && !PuzzleOverlayFocus.IsActive && !string.IsNullOrEmpty(text.text);
        if (boxGo.activeSelf != show) boxGo.SetActive(show);
    }

    // ================= HUD 만들기 =================
    private void BuildHud()
    {
        var canvasGo = new GameObject("ObjectiveCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasGo.layer = 5; // UI
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        boxGo = new GameObject("ObjectiveBox", typeof(RectTransform), typeof(Image));
        boxGo.layer = 5;
        var box = (RectTransform)boxGo.transform;
        box.SetParent(canvasGo.transform, false);
        box.anchorMin = box.anchorMax = new Vector2(0.5f, 1f); // 위 가운데 고정 (해상도가 바뀌어도 위에 붙음)
        box.pivot = new Vector2(0.5f, 1f);
        box.anchoredPosition = hudOffset;
        box.sizeDelta = boxSize;

        boxImage = boxGo.GetComponent<Image>();
        boxImage.color = BoxColor;
        boxImage.raycastTarget = false;

        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.layer = 5;
        var trt = (RectTransform)textGo.transform;
        trt.SetParent(box, false);
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(20f, 0f);
        trt.offsetMax = new Vector2(-20f, 0f);

        text = textGo.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.fontSize = fontSize;
        text.enableAutoSizing = true; // 긴 목표는 줄여서 한 줄에
        text.fontSizeMin = 18;
        text.fontSizeMax = fontSize;
        text.color = new Color(0.91f, 0.70f, 0.23f); // #E8B23A (라 방 목표 색과 같음)
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.text = "";

        boxGo.SetActive(false);
    }

    // ================= 테스트 =================
    [ContextMenu("현재 목표 다시 계산")]
    private void CheatRefresh()
    {
        currentIndex = -3;
        if (Application.isPlaying) Refresh();
    }

    [ContextMenu("기본 단계로 다시 채우기")]
    private void ResetToDefaultSteps()
    {
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "아누비스 목표 단계 다시 채우기");
#endif
        steps = DefaultSteps();
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
        Debug.Log($"[목표 안내] 기본 단계 {steps.Count}개로 다시 채움 (씬 저장 Ctrl+S)");
    }

    // ================= 기본 데이터 =================
    public const string FlagShrineFirstTalked = "F_ANUBIS_SHRINE_FIRST_TALKED";
    public const string FlagMainDone = "F_ANUBIS_MAIN_DONE";

    private static List<AnubisObjectiveStep> DefaultSteps()
    {
        return new List<AnubisObjectiveStep>
        {
            // [수정] 첫 단계 = 입구 비석 읽기 (ClueStele가 읽으면 F_CLUE_<id>_READ를 켬)
            Make("", FlagHallSteleRead, "입구의 비석을 읽어 보자"),
            Make(AnubisItemIds.GoldJar, "", "양피지 조각을 모아 A1 모래 제단의 시험을 풀자"),
            Make(AnubisItemIds.ClayPot, "", "열린 A2 톱니 구역에서 도르래 제단을 풀자"),
            Make(AnubisItemIds.Obsidian, "", "A3 레버 구역의 지렛대 제단을 풀자"),
            Make(AnubisItemIds.BronzeBowl, "", "A4 구역에서 42인의 심판관 마방진을 풀자"),
            Make("", FlagMainDone, "심판의 저울(A5)에 깃털과 같은 무게의 보물을 올리자"),
            // [수정] 신상 대화 단계 제거 (신상 오브젝트가 아직 없음)
            Make("", "", "심판을 통과했다. 아누비스의 시련을 모두 마쳤다")
        };
    }

    // [추가] 입구 비석(Clue_ANUBIS_HALL) 읽음 플래그
    public const string FlagHallSteleRead = "F_CLUE_ANUBIS_HALL_READ";

#if UNITY_EDITOR
    // [추가] 에디터 세팅 메뉴에서 씬에 저장된 옛 단계를 새 기본값으로 바꿀 때 사용
    public void EditorResetSteps() => steps = DefaultSteps();
#endif

    public const string FlagShrineEndTalked = "F_ANUBIS_SHRINE_END_TALKED";

    private static AnubisObjectiveStep Make(string item, string flag, string objective)
    {
        return new AnubisObjectiveStep { doneItemId = item, doneFlag = flag, objective = objective };
    }
}
