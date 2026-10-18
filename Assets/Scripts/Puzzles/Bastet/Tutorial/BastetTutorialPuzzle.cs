using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 바스테트 튜토리얼 퍼즐 (T1~T4 공용)
// - BastetTutorialData의 종류(Mode)에 맞춰 화면 UI를 실행 중에 직접 만듦 (씬에 UI 미리 만들 필요 없음)
// - 풀면 보상 아이템 지급 -> 석문(AnubisItemGate)이 4개를 모두 확인하면 열림
// 붙이는 곳: 튜토리얼 제단 오브젝트 (BastetTutorialAltar와 같이)
public class BastetTutorialPuzzle : PuzzleBase
{
    [Header("데이터")]
    [SerializeField] private BastetTutorialData data;

    [Header("UI")]
    [Tooltip("한글 TMP 폰트 (MonoplexKR-Regular SDF)")]
    [SerializeField] private TMP_FontAsset font;
    [Tooltip("퍼즐 화면 캔버스 순서 (안내 45 < 퍼즐 50 < 토스트 55)")]
    [SerializeField] private int sortingOrder = 50;
    [Tooltip("성공 후 화면이 닫힐 때까지 (초)")]
    [Range(0.3f, 3f)] [SerializeField] private float closeDelayOnSuccess = 1.2f;
    [Tooltip("틀렸을 때 다시 보여 주기까지 (초)")]
    [Range(0.3f, 3f)] [SerializeField] private float retryDelay = 1.0f;

    [Header("플레이어 (비우면 자동 탐색)")]
    [SerializeField] private PlayerController2D playerController;

    // ===== 색 =====
    private static readonly Color DimColor = new Color(0f, 0f, 0f, 0.75f);
    private static readonly Color PanelColor = new Color(0.16f, 0.13f, 0.09f, 0.97f);
    private static readonly Color RuleColor = new Color(0.93f, 0.87f, 0.74f, 1f);
    private static readonly Color CellColor = new Color(0.36f, 0.29f, 0.2f, 1f);
    private static readonly Color LitColor = new Color(1f, 0.82f, 0.3f, 1f);
    private static readonly Color PickedColor = new Color(0.55f, 0.8f, 0.55f, 1f);
    private static readonly Color WrongColor = new Color(0.85f, 0.3f, 0.25f, 1f);
    private static readonly Color BlankColor = new Color(0.25f, 0.42f, 0.55f, 1f);
    private static readonly Color GoldText = new Color(0.95f, 0.78f, 0.35f, 1f);

    // ===== 실행 중 상태 =====
    private GameObject root;            // 캔버스 전체
    private RectTransform board;        // 퍼즐 판 (모드마다 다시 채움)
    private TextMeshProUGUI titleText, ruleText, statusText;
    private Button replayButton, submitButton;

    private Image[] cells;              // 발자국 칸 / 눈 / 목걸이 조각 / 발톱 칸
    private TextMeshProUGUI[] cellLabels;
    private RectTransform[] arrows;     // 목걸이 조각 화살표 (회전)
    private int[] rotations;            // 목걸이 현재 방향
    private int[] clawValues;           // 발톱 현재 값 (빈칸만 바뀜)
    private int inputCount;             // 발자국/눈 입력 개수
    private bool isOpen, isShowing, isLocked;
    private Coroutine routine;
    private WaitForSeconds stepWait, gapWait, retryWait, closeWait;
    private Rigidbody2D playerRb;

    private static readonly string[] ClawText = { "?", "/", "//", "///" };

    public bool IsOpen => isOpen;
    public BastetTutorialData Data => data;

    private void Update()
    {
        // [New Input System] Esc로 닫기 (Update에서는 할당 없음)
        if (isOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
    }

    private void OnDisable()
    {
        if (isOpen) Close();
    }

    // ================= 열기 / 닫기 =================
    public override void Open()
    {
        if (data == null) { Debug.LogError($"[{name}] BastetTutorialData가 비어 있음", this); return; }
        if (!CanEnter()) { base.Open(); return; }
        if (IsCleared) { AnubisToast.Show("이미 통과한 시험이다."); return; }
        if (isOpen) return;

        base.Open();
        if (root == null) BuildUI();

        stepWait = new WaitForSeconds(data.ShowStepTime);           // 열 때 한 번만 만듦 (GC 방지)
        gapWait = new WaitForSeconds(data.ShowStepTime * 0.4f);
        retryWait = new WaitForSeconds(retryDelay);
        closeWait = new WaitForSeconds(closeDelayOnSuccess);

        root.SetActive(true);
        isOpen = true;
        LockPlayer(true);
        PuzzleOverlayFocus.Enter(this);
        BuildBoard();
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        if (routine != null) { StopCoroutine(routine); routine = null; }
        isShowing = false;
        root.SetActive(false);
        LockPlayer(false);
        PuzzleOverlayFocus.Exit(this);
    }

    private void LockPlayer(bool locked)
    {
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController2D>();
        if (playerController == null) return;
        if (playerRb == null) playerRb = playerController.GetComponent<Rigidbody2D>();
        playerController.enabled = !locked;
        if (locked && playerRb != null) playerRb.linearVelocity = Vector2.zero; // Unity 6 (2022 LTS는 velocity)
    }

    // ================= PuzzleBase =================
    public override void Submit()
    {
        if (!isOpen || isShowing || isLocked) return;
        if (Validate()) OnSuccessInternal();
        else OnFailInternal();
    }

    protected override bool Validate()
    {
        switch (data.PuzzleMode)
        {
            case BastetTutorialData.Mode.Footprint:
            case BastetTutorialData.Mode.EyeReverse:
                return inputCount == data.Sequence.Length;
            case BastetTutorialData.Mode.Necklace:
                // 좌우 거울: 0↔3, 1↔2. 거울에 비친 방향 = (4 - r) % 4 (위·아래는 그대로, 좌우는 뒤집힘)
                return rotations[3] == Mirror(rotations[0]) && rotations[2] == Mirror(rotations[1]);
            case BastetTutorialData.Mode.ClawPattern:
                foreach (int b in data.Blanks)
                    if (clawValues[b] != data.Pattern[b]) return false;
                return true;
        }
        return false;
    }

    protected override void OnSuccessInternal()
    {
        base.OnSuccessInternal();
        isLocked = true;
        SetStatus(data.SuccessMessage, GoldText);

        if (!string.IsNullOrEmpty(data.RewardItemId))
        {
            if (ItemInventory.Instance != null) ItemInventory.Instance.Grant(data.RewardItemId);
            else Debug.LogWarning($"[{name}] ItemInventory가 없어서 {data.RewardItemId} 지급 못 함", this);
        }
        AnubisToast.Show(data.SuccessMessage);
        routine = StartCoroutine(CloseLater());
    }

    protected override void OnFailInternal()
    {
        base.OnFailInternal(); // 안에서 Reset() 호출
    }

    public override void Reset()
    {
        if (!Application.isPlaying) return; // 에디터 Reset 가드
        base.Reset();
        if (!isOpen || data == null) return;

        switch (data.PuzzleMode)
        {
            case BastetTutorialData.Mode.Footprint:
            case BastetTutorialData.Mode.EyeReverse:
                SetStatus("순서가 틀렸다! 다시 보여 준다.", WrongColor);
                if (routine != null) StopCoroutine(routine);
                routine = StartCoroutine(RetryLater());
                break;
            case BastetTutorialData.Mode.Necklace:
                SetStatus("좌우가 거울처럼 맞지 않는다.", WrongColor);
                break;
            case BastetTutorialData.Mode.ClawPattern:
                SetStatus("무늬가 이어지지 않는다. 빈칸이 처음으로 돌아간다.", WrongColor);
                ResetClaw();
                break;
        }
    }

    // 실패 처벌 등에서 처음 상태로 되돌릴 때
    [ContextMenu("테스트: 클리어 상태 되돌리기")]
    public void ResetForRetry()
    {
        ResetClearedState();
        if (isOpen) BuildBoard();
    }

    // ================= 판 만들기 =================
    private void BuildBoard()
    {
        if (routine != null) { StopCoroutine(routine); routine = null; }
        isShowing = false;
        isLocked = false;
        inputCount = 0;
        for (int i = board.childCount - 1; i >= 0; i--) Destroy(board.GetChild(i).gameObject); // 열 때만 (Update 아님)

        titleText.text = data.Title;
        ruleText.text = data.RuleText;

        bool sequenceMode = data.PuzzleMode == BastetTutorialData.Mode.Footprint ||
                            data.PuzzleMode == BastetTutorialData.Mode.EyeReverse;
        replayButton.gameObject.SetActive(sequenceMode);
        submitButton.gameObject.SetActive(!sequenceMode); // 발자국/눈은 다 누르면 자동 판정

        switch (data.PuzzleMode)
        {
            case BastetTutorialData.Mode.Footprint: BuildFootprint(); break;
            case BastetTutorialData.Mode.EyeReverse: BuildEyes(); break;
            case BastetTutorialData.Mode.Necklace: BuildNecklace(); break;
            case BastetTutorialData.Mode.ClawPattern: BuildClaw(); break;
        }
    }

    // T1: N×N 발자국 판
    private void BuildFootprint()
    {
        int n = data.GridSize;
        float size = 110f, gap = 14f;
        float total = n * size + (n - 1) * gap;
        cells = new Image[n * n];
        cellLabels = new TextMeshProUGUI[n * n];
        for (int i = 0; i < n * n; i++)
        {
            int x = i % n, y = i / n;
            Vector2 pos = new Vector2(-total * 0.5f + size * 0.5f + x * (size + gap), total * 0.5f - size * 0.5f - y * (size + gap));
            int idx = i;
            cells[i] = MakeCell(board, pos, new Vector2(size, size), CellColor, () => OnSequenceClick(idx), out cellLabels[i]);
        }
        cellLabels[data.Sequence[0]].text = "시작"; // 첫 칸은 알려 줌 (튜토리얼)
        PlaySequence();
    }

    // T2: 한 줄로 놓인 고양이 눈
    private void BuildEyes()
    {
        int n = data.ButtonCount;
        float size = 120f, gap = 30f;
        float total = n * size + (n - 1) * gap;
        cells = new Image[n];
        cellLabels = new TextMeshProUGUI[n];
        for (int i = 0; i < n; i++)
        {
            Vector2 pos = new Vector2(-total * 0.5f + size * 0.5f + i * (size + gap), 0f);
            int idx = i;
            cells[i] = MakeCell(board, pos, new Vector2(size, size * 0.6f), CellColor, () => OnSequenceClick(idx), out cellLabels[i]);
            cellLabels[i].text = (i + 1).ToString();
        }
        PlaySequence();
    }

    // T3: 목걸이 조각 4개 (왼쪽 2 | 가운데 선 | 오른쪽 2)
    private void BuildNecklace()
    {
        cells = new Image[4];
        cellLabels = new TextMeshProUGUI[4];
        arrows = new RectTransform[4];
        rotations = new int[4];
        float[] xs = { -330f, -150f, 150f, 330f };
        for (int i = 0; i < 4; i++)
        {
            int idx = i;
            rotations[i] = (data.StartRotations != null && i < data.StartRotations.Length) ? ((data.StartRotations[i] % 4) + 4) % 4 : 0;
            cells[i] = MakeCell(board, new Vector2(xs[i], 0f), new Vector2(150f, 150f), CellColor, () => OnNecklaceClick(idx), out cellLabels[i]);
            cellLabels[i].text = "";
            arrows[i] = MakeArrow(cells[i].rectTransform);
            ApplyRotation(i);
        }
        // 가운데 거울 선
        MakeImage(board, "MirrorLine", Vector2.zero, new Vector2(8f, 240f), GoldText);
        Label(board, "왼쪽", new Vector2(-240f, 120f), 26, RuleColor);
        Label(board, "오른쪽", new Vector2(240f, 120f), 26, RuleColor);
        if (data.FirstPieceTurnsSecond) Label(board, "1번을 돌리면 2번도 같이 돈다", new Vector2(-240f, -120f), 22, LitColor);
        SetStatus("조각을 눌러 돌리고 [확인]", RuleColor);
    }

    // T4: 발톱 무늬 (빈칸만 눌러 바꿈)
    private void BuildClaw()
    {
        int cols = Mathf.Max(1, data.Columns);
        int count = data.Pattern.Length;
        int rows = Mathf.CeilToInt(count / (float)cols);
        float w = 120f, h = 90f, gap = 14f;
        float totalW = cols * w + (cols - 1) * gap, totalH = rows * h + (rows - 1) * gap;
        cells = new Image[count];
        cellLabels = new TextMeshProUGUI[count];
        clawValues = new int[count];
        for (int i = 0; i < count; i++)
        {
            int x = i % cols, y = i / cols, idx = i;
            Vector2 pos = new Vector2(-totalW * 0.5f + w * 0.5f + x * (w + gap), totalH * 0.5f - h * 0.5f - y * (h + gap));
            bool blank = IsBlank(i);
            cells[i] = MakeCell(board, pos, new Vector2(w, h), blank ? BlankColor : CellColor, blank ? () => OnClawClick(idx) : (UnityEngine.Events.UnityAction)null, out cellLabels[i]);
        }
        ResetClaw();
        SetStatus("파란 빈칸을 눌러 무늬를 고르고 [확인]", RuleColor);
    }

    // ================= 입력 =================
    private void OnSequenceClick(int idx)
    {
        if (isShowing || isLocked) return;
        int[] seq = data.Sequence;
        // T2는 거꾸로 입력
        int expected = data.PuzzleMode == BastetTutorialData.Mode.EyeReverse ? seq[seq.Length - 1 - inputCount] : seq[inputCount];

        if (idx != expected)
        {
            Debug.Log($"[{name}] 틀림: {inputCount + 1}번째에 {idx} (정답 {expected})");
            cells[idx].color = WrongColor;
            isLocked = true;
            OnFailInternal();
            return;
        }

        cells[idx].color = PickedColor;
        inputCount++;
        SetStatus($"{inputCount} / {seq.Length}", RuleColor);
        if (inputCount >= seq.Length) { isLocked = false; Submit(); }
    }

    private void OnNecklaceClick(int idx)
    {
        if (isLocked) return;
        RotatePiece(idx);
        if (idx == 0 && data.FirstPieceTurnsSecond) RotatePiece(1); // 연결된 조각
    }

    private void RotatePiece(int idx)
    {
        rotations[idx] = (rotations[idx] + 1) % 4;
        ApplyRotation(idx);
    }

    private void OnClawClick(int idx)
    {
        if (isLocked) return;
        clawValues[idx] = clawValues[idx] % 3 + 1; // ? -> / -> // -> /// -> / ...
        cellLabels[idx].text = ClawText[clawValues[idx]];
    }

    private void OnReplay()
    {
        if (isShowing || isLocked) return;
        PlaySequence();
    }

    private void OnRestart()
    {
        if (isShowing) return;
        BuildBoard();
    }

    // ================= 연출 =================
    private void PlaySequence()
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(ShowSequence());
    }

    private IEnumerator ShowSequence()
    {
        isShowing = true;
        inputCount = 0;
        ClearCellColors();
        SetStatus("잘 봐라...", LitColor);
        yield return gapWait;

        foreach (int i in data.Sequence)
        {
            if (i < 0 || i >= cells.Length) { Debug.LogWarning($"[{name}] sequence 칸 번호 {i}가 판 밖", this); continue; }
            cells[i].color = LitColor;
            yield return stepWait;
            cells[i].color = CellColor;
            yield return gapWait;
        }

        isShowing = false;
        routine = null;
        SetStatus(data.PuzzleMode == BastetTutorialData.Mode.EyeReverse ? "이제 거꾸로 눌러라" : "이제 같은 길을 밟아라", RuleColor);
    }

    private IEnumerator RetryLater()
    {
        yield return retryWait;
        isLocked = false;
        routine = null;
        PlaySequence();
    }

    private IEnumerator CloseLater()
    {
        yield return closeWait;
        routine = null;
        Close();
    }

    private void ClearCellColors()
    {
        for (int i = 0; i < cells.Length; i++) cells[i].color = CellColor;
    }

    private void ResetClaw()
    {
        for (int i = 0; i < clawValues.Length; i++)
        {
            bool blank = IsBlank(i);
            clawValues[i] = blank ? 0 : data.Pattern[i];
            cellLabels[i].text = ClawText[Mathf.Clamp(clawValues[i], 0, 3)];
        }
    }

    private bool IsBlank(int i)
    {
        foreach (int b in data.Blanks) if (b == i) return true;
        return false;
    }

    private void ApplyRotation(int idx)
    {
        arrows[idx].localEulerAngles = new Vector3(0f, 0f, -90f * rotations[idx]); // 0 위, 1 오른쪽, 2 아래, 3 왼쪽
    }

    private static int Mirror(int r) => (4 - r) % 4;

    private void SetStatus(string msg, Color c)
    {
        statusText.text = msg;
        statusText.color = c;
    }

    // ================= UI 만들기 (처음 열 때 한 번) =================
    private void BuildUI()
    {
        root = new GameObject($"{name}_TutorialUI", typeof(RectTransform));
        root.transform.SetParent(transform, false);
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 해상도 대응
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        root.AddComponent<GraphicRaycaster>();

        var dim = MakeImage(root.transform, "Dim", Vector2.zero, Vector2.zero, DimColor);
        Stretch(dim.rectTransform);

        var panel = MakeImage(root.transform, "Panel", Vector2.zero, new Vector2(1300f, 900f), PanelColor).rectTransform;
        var outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = GoldText;
        outline.effectDistance = new Vector2(3f, -3f);

        titleText = Label(panel, "", new Vector2(0f, 390f), 46, GoldText);
        titleText.rectTransform.sizeDelta = new Vector2(1200f, 70f);

        var ruleBox = MakeImage(panel, "RuleBox", new Vector2(0f, 270f), new Vector2(1180f, 150f), RuleColor).rectTransform;
        ruleText = Label(ruleBox, "", Vector2.zero, 26, new Color(0.17f, 0.13f, 0.09f, 1f));
        ruleText.rectTransform.sizeDelta = new Vector2(1120f, 140f);
        ruleText.alignment = TextAlignmentOptions.Left;

        board = new GameObject("Board", typeof(RectTransform)).GetComponent<RectTransform>();
        board.SetParent(panel, false);
        board.anchoredPosition = new Vector2(0f, -10f);
        board.sizeDelta = new Vector2(1100f, 380f);

        statusText = Label(panel, "", new Vector2(0f, -260f), 30, RuleColor);
        statusText.rectTransform.sizeDelta = new Vector2(1180f, 50f);

        replayButton = MakeButton(panel, "다시 보기", new Vector2(-420f, -370f), OnReplay);
        submitButton = MakeButton(panel, "확인", new Vector2(-140f, -370f), Submit);
        MakeButton(panel, "처음부터", new Vector2(140f, -370f), OnRestart);
        MakeButton(panel, "닫기 (Esc)", new Vector2(420f, -370f), Close);

        root.SetActive(false);
    }

    private Image MakeImage(Transform parent, string objName, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(objName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        img.rectTransform.anchoredPosition = pos;
        img.rectTransform.sizeDelta = size;
        return img;
    }

    private TextMeshProUGUI Label(Transform parent, string msg, Vector2 pos, int fontSize, Color color)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = msg;
        t.fontSize = fontSize;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        t.rectTransform.anchoredPosition = pos;
        t.rectTransform.sizeDelta = new Vector2(500f, 60f);
        return t;
    }

    private Image MakeCell(Transform parent, Vector2 pos, Vector2 size, Color color, UnityEngine.Events.UnityAction onClick, out TextMeshProUGUI label)
    {
        var img = MakeImage(parent, "Cell", pos, size, color);
        if (onClick != null)
        {
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);
        }
        label = Label(img.transform, "", Vector2.zero, 34, RuleColor);
        label.rectTransform.sizeDelta = size;
        return img;
    }

    private Button MakeButton(Transform parent, string text, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        var img = MakeImage(parent, text, pos, new Vector2(240f, 72f), new Color(0.48f, 0.36f, 0.16f, 1f));
        var btn = img.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);
        var t = Label(img.transform, text, Vector2.zero, 28, Color.white);
        t.rectTransform.sizeDelta = new Vector2(240f, 72f);
        return btn;
    }

    // 화살표 = 막대 + 마름모 머리 (폰트에 ▲ 글자가 없어도 보이게 이미지로 만듦)
    private RectTransform MakeArrow(RectTransform parent)
    {
        var holder = new GameObject("Arrow", typeof(RectTransform)).GetComponent<RectTransform>();
        holder.SetParent(parent, false);
        holder.sizeDelta = new Vector2(100f, 100f);
        var shaft = MakeImage(holder, "Shaft", new Vector2(0f, -12f), new Vector2(18f, 70f), LitColor);
        shaft.raycastTarget = false;
        var head = MakeImage(holder, "Head", new Vector2(0f, 26f), new Vector2(42f, 42f), LitColor);
        head.raycastTarget = false;
        head.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
        return holder;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

#if UNITY_EDITOR
    public void EditorSetup(BastetTutorialData d, TMP_FontAsset f)
    {
        data = d;
        if (f != null) font = f;
    }

    [ContextMenu("테스트: 바로 풀기 (보상 지급)")]
    private void EditorForceSolve()
    {
        if (!Application.isPlaying) { Debug.Log("Play 중에만 동작"); return; }
        if (!isOpen) Open();
        if (isOpen && !IsCleared) OnSuccessInternal();
    }
#endif
}
