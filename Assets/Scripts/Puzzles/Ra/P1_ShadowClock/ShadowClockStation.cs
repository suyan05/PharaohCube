using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// OBJ_005 그림자 다이얼: E키로 P1 오버레이 열기/닫기 + 화면 구성 (기획서 v4.0 4.1)
// 판정은 ShadowClockPuzzle이 담당하고, 이 스크립트는 화면만 담당
// - 다이얼 가운데 오벨리스크 + 지금 문제의 그림자 1개 (긴 그림자 / 발밑 짧은 그림자 / 없음)
// - 화면 방향: 오른쪽 = 동, 왼쪽 = 서 (다이얼 양옆에 표시)
// - 아래 칸 4개 = 진행 표시 (맞힌 문제는 시간 이름, 지금 문제는 ?)
// - 같은 문제 3회 오답이면 해 위치 표시
public class ShadowClockStation : MonoBehaviour, IInteractable
{
    // 기본 크기 (우클릭 -> "크기 기본값 적용"으로 씬에 저장된 값을 덮어쓸 수 있음)
    private static readonly Vector2 DefaultSymbolSize = new Vector2(130, 160);
    // [수정] 다이얼, 오벨리스크, 그림자를 크게 (그림자가 한눈에 읽히게)
    private static readonly Vector2 DefaultDialSize = new Vector2(400, 400);
    private static readonly Vector2 DefaultLongShadowSize = new Vector2(185, 56);
    private static readonly Vector2 DefaultShortShadowSize = new Vector2(130, 74);
    private static readonly Vector2 DefaultSunSize = new Vector2(70, 70);
    private static readonly Vector2 DefaultSlotSize = new Vector2(200, 70);
    private static readonly Vector2 DefaultLensSize = new Vector2(120, 120);
    private static readonly Vector2 DefaultLensPos = new Vector2(430, -20);
    private static readonly Vector2 DefaultObeliskSize = new Vector2(104, 180);

    // 예전 기본값 (씬에 이 값이 저장돼 있으면 새 기본값으로 바꿔서 씀)
    private static readonly Vector2 OldDialSize = new Vector2(300, 300);
    private static readonly Vector2 OldLongShadowSize = new Vector2(130, 26);
    private static readonly Vector2 OldShortShadowSize = new Vector2(84, 52);
    private static readonly Vector2 OldSunSize = new Vector2(56, 56);
    private static readonly Vector2 OldObeliskSize = new Vector2(64, 64);

    private const float WallY = 300f;
    private const float SlotY = -280f;
    private const float SideLabelGap = 45f;
    private static readonly Vector2 DialPos = new Vector2(0, -40);
    private const float ShadowGrowTime = 0.7f;

    [Header("연결")]
    [SerializeField] private ShadowClockPuzzle puzzle;
    [SerializeField] private GameObject overlayPanel;

    [Header("아트 - 시계판")]
    [Tooltip("끄면 코드로 정면 원판을 그림. 정면 시계판 그림이 생기면 켜기")]
    [SerializeField] private bool useClockFaceArt = false;
    [SerializeField] private Sprite clockFaceSprite;   // Clock face (정면 그림일 때만 사용)
    [SerializeField] private Sprite obeliskSprite;     // Central Obelisk

    [Header("아트 - 그림자 / 해 (비우면 코드로 그림)")]
    [Tooltip("동쪽(오른쪽)을 향하게 그린 긴 그림자. 서쪽은 좌우 뒤집어서 씀")]
    [SerializeField] private Sprite shadowLongSprite;
    [Tooltip("정오: 발밑에 짧게 깔리는 그림자")]
    [SerializeField] private Sprite shadowShortSprite;
    [Tooltip("3회 오답 힌트 때 보이는 해 그림")]
    [SerializeField] private Sprite sunSprite;

    [Header("아트 - 벽 기호 (렌즈 장착 전/후)")]
    [SerializeField] private Sprite noonSprite;
    [SerializeField] private Sprite nightSprite;
    [SerializeField] private Sprite dawnSprite;
    [SerializeField] private Sprite duskSprite;
    [SerializeField] private Sprite symbolInactiveSprite; // 렌즈 장착 전 공통 (Inactive)

    [Header("아트 - 렌즈")]
    [SerializeField] private Sprite lensInactiveSprite;  // [확인 필요] lens 파일이 맞는지
    [SerializeField] private Sprite lensActiveSprite;    // Lens Activation

    [Header("아트 - 진행 슬롯")]
    [SerializeField] private Sprite slotEmptySprite;     // Slot Board
    [Tooltip("끄면 빈 슬롯 그림 하나로 색만 바꿈 (상태별 그림 비율이 달라서 크기가 변하는 문제 방지)")]
    [SerializeField] private bool useSlotStateSprites = false;
    [SerializeField] private Sprite slotFilledSprite;    // Slot Input
    [SerializeField] private Sprite slotSuccessSprite;   // Slot entry successful
    [SerializeField] private Sprite slotFailSprite;      // Slot input failed

    [Header("크기 (우클릭 -> 크기 기본값 적용)")]
    [SerializeField] private Vector2 symbolSize = DefaultSymbolSize;
    [SerializeField] private Vector2 dialSize = DefaultDialSize;
    [SerializeField] private Vector2 longShadowSize = DefaultLongShadowSize;
    [SerializeField] private Vector2 shortShadowSize = DefaultShortShadowSize;
    [SerializeField] private Vector2 sunSize = DefaultSunSize;
    [SerializeField] private Vector2 slotSize = DefaultSlotSize;
    [SerializeField] private Vector2 lensSize = DefaultLensSize;
    [SerializeField] private Vector2 lensPos = DefaultLensPos;
    [SerializeField] private Vector2 obeliskSize = DefaultObeliskSize;

    [Header("화면")]
    [SerializeField] private bool showSymbolLabels = true;
    [SerializeField] private Color backdropColor = new Color(0f, 0f, 0f, 0.8f);
    [Tooltip("오버레이가 열려 있는 동안 투명하게 숨길 UI (목표 문구, 노트 팝업 등)")]
    [SerializeField] private GameObject[] hideWhileOpen;

    // 기획서 11번 색상
    private Color wallColor, darkBrown, gold, paper, sand, red, glow, dimText, stoneColor, shadowColor, successTint;

    private readonly Image[] symbolImages = new Image[4];
    private readonly Text[] symbolTexts = new Text[4];
    private readonly Image[] symbolPlates = new Image[4];
    private readonly Image[] slotImages = new Image[4];
    private readonly Text[] slotTexts = new Text[4];
    private readonly Dictionary<CanvasGroup, float> hiddenGroups = new Dictionary<CanvasGroup, float>();
    private Image shadowLong;
    private Image shadowShort;
    private Image sunMarker;
    private Text sunLabel;
    private Image lensImage;
    private Text lensLabel;
    private Text statusText;
    private Font font;

    private static Sprite circleSprite;
    private static Sprite wedgeSprite;
    private string lastShadowKey = "";

    private bool isOpen;
    private bool flashingRed;
    private PlayerController2D playerController;
    private Rigidbody2D playerRb;

    // ================= 초기화 =================
    private void Start()
    {
        try
        {
            if (puzzle == null || overlayPanel == null)
            {
                Debug.LogError($"[{name}] puzzle 또는 overlayPanel이 비어 있음");
                return;
            }

            SetupColors();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            UpgradeOldSizes();

            BuildUI();
            overlayPanel.SetActive(false);
            Subscribe();
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 초기화 오류: {e}");
        }
    }

    // [추가] 씬에 예전 작은 크기가 저장돼 있으면 새 크기로 (직접 바꾼 값은 그대로 둠)
    private void UpgradeOldSizes()
    {
        if (dialSize == OldDialSize) dialSize = DefaultDialSize;
        if (longShadowSize == OldLongShadowSize) longShadowSize = DefaultLongShadowSize;
        if (shortShadowSize == OldShortShadowSize) shortShadowSize = DefaultShortShadowSize;
        if (sunSize == OldSunSize) sunSize = DefaultSunSize;
        if (obeliskSize == OldObeliskSize) obeliskSize = DefaultObeliskSize;
    }

    private void Subscribe()
    {
        puzzle.OnQuestionChanged += RefreshAll;
        puzzle.OnLensMounted += RefreshAll;
        puzzle.OnCorrect += HandleCorrect;
        puzzle.OnSuccess += HandleSuccess;
        puzzle.OnFail += HandleFail;
        puzzle.OnHint += HandleHint;
        puzzle.OnPhaseChanged += HandlePhaseChanged;
    }

    private void OnDestroy()
    {
        if (puzzle == null) return;
        puzzle.OnQuestionChanged -= RefreshAll;
        puzzle.OnLensMounted -= RefreshAll;
        puzzle.OnCorrect -= HandleCorrect;
        puzzle.OnSuccess -= HandleSuccess;
        puzzle.OnFail -= HandleFail;
        puzzle.OnHint -= HandleHint;
        puzzle.OnPhaseChanged -= HandlePhaseChanged;
    }

    private void Update()
    {
        if (!isOpen) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseOverlay();
        }
    }

    private void SetupColors()
    {
        wallColor = Hex("#C9A66B");
        darkBrown = Hex("#4A3423");
        gold = Hex("#E8B23A");
        paper = Hex("#F6F1E7");
        sand = Hex("#E8D9B8");
        red = Hex("#E06050");
        glow = Hex("#FFF2B3");
        dimText = Hex("#7A6450");
        stoneColor = Hex("#CDB48A");
        shadowColor = new Color(0.1f, 0.06f, 0.03f, 0.7f);
        successTint = Hex("#CDEFB0");
    }

    // ================= IInteractable =================
    public string GetPrompt()
    {
        return isOpen ? "E: 다이얼 닫기" : "E: 그림자 다이얼 조사";
    }

    public void Interact(GameObject player)
    {
        try
        {
            if (isOpen) CloseOverlay();
            else OpenOverlay(player);
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 상호작용 오류: {e}");
        }
    }

    // ================= 열기 / 닫기 =================
    private void OpenOverlay(GameObject player)
    {
        LockPlayer(player);
        SetOtherUIHidden(true);
        overlayPanel.SetActive(true);
        isOpen = true;

        puzzle.Open(); // 여기서 출제 + 렌즈 자동 장착 시도

        RefreshAll();
        statusText.text = GetOpenMessage();
    }

    private void CloseOverlay()
    {
        StopAllCoroutines();
        flashingRed = false;
        lastShadowKey = ""; // 다시 열면 그림자가 다시 드리워짐

        overlayPanel.SetActive(false);
        isOpen = false;
        SetOtherUIHidden(false);
        if (playerController != null) playerController.enabled = true;
    }

    private void LockPlayer(GameObject player)
    {
        playerController = player.GetComponent<PlayerController2D>();
        playerRb = player.GetComponent<Rigidbody2D>();
        if (playerController != null) playerController.enabled = false;
        if (playerRb != null) playerRb.linearVelocity = Vector2.zero;
    }

    private string GetOpenMessage()
    {
        if (!puzzle.CanEnter()) return "다이얼이 아직 반응하지 않는다.";
        if (puzzle.IsCleared) return "이미 해독한 시계다.";
        if (!puzzle.LensMounted) return "너무 어두워 아무것도 보이지 않는다.";
        return "그림자를 보고, 지금이 언제인지 벽의 기호를 골라라.";
    }

    // 다른 UI는 끄지 않고 투명하게만 (그쪽 스크립트가 멈추지 않게)
    private void SetOtherUIHidden(bool hide)
    {
        if (hideWhileOpen == null) return;

        foreach (GameObject go in hideWhileOpen)
        {
            if (go == null) continue;

            CanvasGroup group = go.GetComponent<CanvasGroup>();
            if (group == null) group = go.AddComponent<CanvasGroup>();

            if (hide)
            {
                hiddenGroups[group] = group.alpha;
                group.alpha = 0f;
            }
            else if (hiddenGroups.TryGetValue(group, out float prev))
            {
                group.alpha = prev;
            }
        }
        if (!hide) hiddenGroups.Clear();
    }

    // ================= 화면 만들기 =================
    private void BuildUI()
    {
        RectTransform root = overlayPanel.GetComponent<RectTransform>();

        BuildBackdrop(root);
        CreateText(root, "태양 그림자 시계", 44, new Vector2(0, 470), new Vector2(800, 70), paper);

        // [수정] 기호 뒤 황토색 벽 띠는 지움 (UI와 안 어울림) -> 기호만
        for (int i = 0; i < 4; i++)
        {
            BuildSymbol(root, i);
        }

        BuildDial(root);
        BuildLens(root);
        for (int i = 0; i < 4; i++)
        {
            BuildSlot(root, i);
        }

        statusText = CreateText(root, "", 28, new Vector2(0, -390), new Vector2(1200, 60), paper);
        CreateText(root, "그림자를 보고 지금 시간의 기호를 클릭  |  E / Esc : 닫기", 22,
            new Vector2(0, -450), new Vector2(1200, 40), wallColor);
    }

    // 화면 전체를 덮는 어두운 배경 (뒤의 맵이 비치지 않게)
    private void BuildBackdrop(RectTransform root)
    {
        Image bg = CreateRect(root, "Backdrop", Vector2.zero, Vector2.zero, backdropColor);
        RectTransform rt = bg.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        bg.raycastTarget = true; // 뒤쪽 클릭 막기
        rt.SetAsFirstSibling();
    }

    private void BuildSymbol(RectTransform root, int index)
    {
        TimeSymbol symbol = ShadowClockPuzzle.WallOrder[index];
        Vector2 pos = new Vector2(-375f + index * 250f, WallY + 15f);
        Sprite sprite = LitSprite(symbol);

        Image img = CreatePiece(root, $"Symbol_{symbol}", pos, symbolSize, sprite, darkBrown);
        img.raycastTarget = true;

        Button btn = img.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => OnSymbolClicked(symbol));

        symbolImages[index] = img;

        if (sprite == null)
        {
            // 그림이 없으면 네모 안에 글자
            symbolTexts[index] = CreateText(img.rectTransform, ShadowClockPuzzle.ToKorean(symbol), 30,
                Vector2.zero, symbolSize, dimText);
        }
        else if (showSymbolLabels)
        {
            BuildSymbolLabel(img.rectTransform, index, symbol);
        }
    }

    // 메달 아래 이름표: 어두운 바탕판 + 밝은 글자 (렌즈 장착 후에만 보임)
    private void BuildSymbolLabel(RectTransform parent, int index, TimeSymbol symbol)
    {
        Vector2 pos = new Vector2(0, -symbolSize.y * 0.5f - 20f);
        Vector2 size = new Vector2(symbolSize.x + 10f, 34f);

        symbolPlates[index] = CreateRect(parent, "LabelPlate", pos, size, new Color(0f, 0f, 0f, 0.55f));
        symbolTexts[index] = CreateText(parent, ShadowClockPuzzle.ToKorean(symbol), 26, pos, size, paper);
    }

    // 원판 -> 동/서 표시 -> 그림자 -> 오벨리스크 -> 해 힌트 순서로 쌓음 (뒤에 만든 게 위에 그려짐)
    private void BuildDial(RectTransform root)
    {
        if (useClockFaceArt && clockFaceSprite != null)
        {
            CreateSprite(root, "Dial", DialPos, dialSize, clockFaceSprite);
        }
        else
        {
            BuildCodeDial(root);
        }

        BuildSideLabels(root);
        BuildShadows(root);

        // [수정] 오벨리스크를 크게, 밑동이 다이얼 가운데(그림자가 시작하는 곳)에 서게
        if (obeliskSprite != null)
        {
            Image obelisk = CreateSprite(root, "Obelisk", DialPos, obeliskSize, obeliskSprite);
            obelisk.rectTransform.pivot = new Vector2(0.5f, 0.06f);
            obelisk.rectTransform.anchoredPosition = DialPos;
        }
        else CreateRect(root, "Obelisk", DialPos, obeliskSize * 0.5f, gold, 45f);

        BuildSunMarker(root);
    }

    // 정면 원판: 금테 -> 돌판 -> 눈금 4개 (위/오른쪽/아래/왼쪽)
    private void BuildCodeDial(RectTransform root)
    {
        Image ring = CreateSprite(root, "DialRing", DialPos, dialSize + new Vector2(20, 20), GetCircleSprite());
        ring.color = gold;

        Image face = CreateSprite(root, "DialFace", DialPos, dialSize, GetCircleSprite());
        face.color = stoneColor;

        float r = dialSize.x * 0.5f - 22f;
        Vector2[] dirs = { Vector2.up, Vector2.right, Vector2.down, Vector2.left };
        for (int i = 0; i < dirs.Length; i++)
        {
            CreateRect(root, $"Tick_{i}", DialPos + dirs[i] * r, new Vector2(8, 26), darkBrown, -90f * i);
        }
    }

    // 다이얼 양옆 동/서 표시 (기획서 4.1: 탑다운에서 방향 헷갈림 방지)
    private void BuildSideLabels(RectTransform root)
    {
        float x = dialSize.x * 0.5f + SideLabelGap;
        BuildSideLabel(root, "동", DialPos + new Vector2(x, 0));
        BuildSideLabel(root, "서", DialPos + new Vector2(-x, 0));
    }

    private void BuildSideLabel(RectTransform root, string text, Vector2 pos)
    {
        Vector2 size = new Vector2(56, 44);
        CreateRect(root, $"Side_{text}", pos, size, darkBrown);
        CreateText(root, text, 30, pos, size, gold);
    }

    private void BuildShadows(RectTransform root)
    {
        // 긴 그림자: 오벨리스크 쪽 끝을 축으로 동쪽을 향함 (서쪽은 좌우 뒤집기)
        Sprite longSprite = shadowLongSprite != null ? shadowLongSprite : GetWedgeSprite();
        shadowLong = CreateSprite(root, "ShadowLong", DialPos, longShadowSize, longSprite);
        shadowLong.color = shadowLongSprite != null ? Color.white : shadowColor;
        shadowLong.preserveAspect = false;
        shadowLong.rectTransform.pivot = new Vector2(0f, 0.5f);
        shadowLong.rectTransform.anchoredPosition = DialPos;

        // 짧은 그림자: 발밑에 깔리는 타원
        Sprite shortSprite = shadowShortSprite != null ? shadowShortSprite : GetCircleSprite();
        shadowShort = CreateSprite(root, "ShadowShort", DialPos, shortShadowSize, shortSprite);
        shadowShort.preserveAspect = false;
        shadowShort.color = shadowShortSprite != null ? Color.white : shadowColor;

        shadowLong.enabled = false;
        shadowShort.enabled = false;
    }

    private void BuildSunMarker(RectTransform root)
    {
        Sprite sprite = sunSprite != null ? sunSprite : GetCircleSprite();
        sunMarker = CreateSprite(root, "SunHint", DialPos, sunSize, sprite);
        sunMarker.color = sunSprite != null ? Color.white : gold;

        sunLabel = CreateText(root, "해", 24, DialPos, new Vector2(200, 30), glow);

        sunMarker.enabled = false;
        sunLabel.enabled = false;
    }

    private void BuildLens(RectTransform root)
    {
        lensImage = CreatePiece(root, "Lens", lensPos, lensSize, lensInactiveSprite, dimText);
        lensLabel = CreateText(root, "주황 렌즈", 22, lensPos + new Vector2(0, -lensSize.y * 0.5f - 18f),
            new Vector2(200, 30), dimText);
    }

    private void BuildSlot(RectTransform root, int index)
    {
        Vector2 pos = new Vector2(-375f + index * 250f, SlotY);

        slotImages[index] = CreatePiece(root, $"Slot_{index + 1}", pos, slotSize, slotEmptySprite, sand);
        slotImages[index].preserveAspect = false; // 상태가 바뀌어도 크기 고정

        slotTexts[index] = CreateText(slotImages[index].rectTransform, (index + 1).ToString(), 30,
            Vector2.zero, slotSize, paper);
    }

    // ================= 입력 =================
    private void OnSymbolClicked(TimeSymbol symbol)
    {
        try
        {
            if (puzzle.IsCleared) return;

            if (!puzzle.LensMounted)
            {
                statusText.text = "너무 어두워 아무것도 보이지 않는다.";
                return;
            }
            puzzle.PressSymbol(symbol);
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 기호 클릭 오류: {e}");
        }
    }

    // ================= 화면 갱신 =================
    private void RefreshAll()
    {
        try
        {
            RefreshSymbols();
            RefreshShadow();
            RefreshSun();
            RefreshSlots();
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 화면 갱신 오류: {e}");
        }
    }

    private void RefreshSymbols()
    {
        for (int i = 0; i < 4; i++)
        {
            TimeSymbol symbol = ShadowClockPuzzle.WallOrder[i];
            Sprite sprite = puzzle.LensMounted ? LitSprite(symbol) : symbolInactiveSprite;

            if (sprite != null)
            {
                symbolImages[i].sprite = sprite;
                symbolImages[i].color = Color.white;
            }
            else
            {
                symbolImages[i].color = puzzle.LensMounted ? gold : darkBrown;
            }

            RefreshSymbolLabel(i);
        }
        RefreshLens();
    }

    private void RefreshSymbolLabel(int i)
    {
        if (symbolTexts[i] == null) return;

        if (symbolPlates[i] != null)
        {
            // 그림 아래 이름표: 렌즈 장착 전에는 숨김 (어두워서 못 읽는 연출)
            symbolPlates[i].enabled = puzzle.LensMounted;
            symbolTexts[i].enabled = puzzle.LensMounted;
            return;
        }

        symbolTexts[i].color = puzzle.LensMounted ? darkBrown : dimText;
    }

    private void RefreshLens()
    {
        bool mounted = puzzle.LensMounted;
        lensLabel.color = mounted ? Hex("#C2661A") : dimText; // [수정] 두루마리 위에서도 읽히는 주황

        if (lensInactiveSprite == null && lensActiveSprite == null)
        {
            lensImage.color = mounted ? gold : dimText;
            return;
        }

        Sprite sprite = mounted ? lensActiveSprite : lensInactiveSprite;
        if (sprite != null) lensImage.sprite = sprite;
        lensImage.color = Color.white;
    }

    // 지금 문제의 그림자 1개 (렌즈 없거나 클리어했으면 숨김)
    private void RefreshShadow()
    {
        ShadowRule rule = puzzle.CurrentRule;
        bool show = puzzle.LensMounted && !puzzle.IsCleared && rule != null;
        bool sideways = show && (rule.shadowSide == DialSide.East || rule.shadowSide == DialSide.West);
        bool isLong = sideways && rule.shadowLength == ShadowLength.Long;
        bool isShort = show && rule.shadowLength == ShadowLength.Short;

        shadowLong.enabled = isLong;
        shadowShort.enabled = isShort;

        float flip = isLong && rule.shadowSide == DialSide.West ? -1f : 1f;
        string key = show ? $"{puzzle.IsNight}_{puzzle.SolvedCount}_{rule.time}" : "";
        if (key == lastShadowKey) return;

        lastShadowKey = key;
        if (show && isActiveAndEnabled) StartCoroutine(GrowShadow(flip, isLong));
        else shadowLong.rectTransform.localScale = new Vector3(flip, 1f, 1f);
    }

    // [추가] 새 문제마다 그림자가 오벨리스크 밑에서부터 쭉 드리워짐 (짧은 그림자는 퍼지듯)
    private IEnumerator GrowShadow(float flip, bool isLong)
    {
        RectTransform target = isLong ? shadowLong.rectTransform : shadowShort.rectTransform;
        float t = 0f;
        while (t < ShadowGrowTime)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / ShadowGrowTime));
            target.localScale = isLong ? new Vector3(flip * k, 1f, 1f) : new Vector3(k, k, 1f);
            yield return null;
        }
        target.localScale = isLong ? new Vector3(flip, 1f, 1f) : Vector3.one;
    }

    // 해 위치 힌트 (같은 문제 3회 오답 후에만)
    private void RefreshSun()
    {
        ShadowRule rule = puzzle.CurrentRule;
        bool show = puzzle.HintActive && puzzle.LensMounted && !puzzle.IsCleared && rule != null;

        sunMarker.enabled = show && rule.sunSide != DialSide.None;
        sunLabel.enabled = show;
        if (!show) return;

        Vector2 pos = SunPosition(rule.sunSide);
        sunMarker.rectTransform.anchoredPosition = pos;
        sunLabel.rectTransform.anchoredPosition = pos + new Vector2(0, sunSize.y * 0.5f + 16f);
        sunLabel.text = rule.sunSide == DialSide.None ? "해가 없다" : "해";
    }

    // 동/서는 방향 표시 위쪽, 머리 위(또는 없음)는 원판 안쪽 위
    private Vector2 SunPosition(DialSide side)
    {
        float x = dialSize.x * 0.5f + SideLabelGap;
        switch (side)
        {
            case DialSide.East: return DialPos + new Vector2(x, 60f);
            case DialSide.West: return DialPos + new Vector2(-x, 60f);
            default: return DialPos + new Vector2(0, dialSize.y * 0.3f);
        }
    }

    private void RefreshSlots()
    {
        int solved = puzzle.SolvedCount;
        for (int i = 0; i < slotImages.Length; i++)
        {
            bool isSolved = i < solved;
            bool isCurrent = !puzzle.IsCleared && i == solved;
            slotTexts[i].text = SlotText(i, isSolved, isCurrent);

            if (!flashingRed)
            {
                UpdateSlotVisual(i, isSolved, isCurrent);
            }
        }
    }

    private string SlotText(int i, bool solved, bool current)
    {
        if (solved) return ShadowClockPuzzle.ToKorean(puzzle.QuestionAt(i));
        return current ? "?" : (i + 1).ToString();
    }

    private void UpdateSlotVisual(int i, bool solved, bool current)
    {
        // 그림이 없을 때: 밝은 네모 + 진한 글자
        if (slotEmptySprite == null)
        {
            slotImages[i].color = puzzle.IsCleared ? gold : (solved ? paper : (current ? glow : sand));
            slotTexts[i].color = darkBrown;
            return;
        }

        slotTexts[i].color = solved || current ? paper : dimText;

        if (useSlotStateSprites)
        {
            UpdateSlotWithStateSprites(i, solved);
            return;
        }

        // 빈 슬롯 그림 하나로 색만 바꿈
        slotImages[i].sprite = slotEmptySprite;
        if (puzzle.IsCleared) slotImages[i].color = successTint;
        else if (solved) slotImages[i].color = Color.white;
        else if (current) slotImages[i].color = new Color(1f, 0.92f, 0.7f, 1f);
        else slotImages[i].color = new Color(0.75f, 0.75f, 0.75f, 1f);
    }

    private void UpdateSlotWithStateSprites(int i, bool solved)
    {
        Sprite sprite = slotEmptySprite;
        if (puzzle.IsCleared && slotSuccessSprite != null) sprite = slotSuccessSprite;
        else if (solved && slotFilledSprite != null) sprite = slotFilledSprite;

        slotImages[i].sprite = sprite;
        slotImages[i].color = Color.white;
    }

    // ================= 퍼즐 이벤트 =================
    private void HandleCorrect(TimeSymbol solved)
    {
        statusText.text = $"맞았다. {ShadowClockPuzzle.ToKorean(solved)}의 그림자였다.";
    }

    private void HandleSuccess()
    {
        statusText.text = puzzle.IsNight
            ? "그림자가 멈췄다. 저승의 시계가 맞춰졌다."
            : "그림자가 멈췄다. 거울 조각 1 획득!";
        RefreshAll();
    }

    private void HandleFail()
    {
        statusText.text = "그 시간이 아니다... 그림자를 다시 보자.";
        int index = Mathf.Clamp(puzzle.SolvedCount, 0, slotImages.Length - 1);
        if (isActiveAndEnabled) StartCoroutine(FlashSlotRed(index));
    }

    private void HandleHint()
    {
        statusText.text = "벽 틈으로 해의 위치가 어렴풋이 보인다...";
        RefreshAll();
    }

    private void HandlePhaseChanged()
    {
        if (!isOpen) return;
        RefreshAll();
        statusText.text = GetOpenMessage();
    }

    // 오답: 지금 칸만 붉은 플래시 0.3초 (기획서 10.3)
    private IEnumerator FlashSlotRed(int index)
    {
        flashingRed = true;
        Image img = slotImages[index];

        if (useSlotStateSprites && slotFailSprite != null)
        {
            img.sprite = slotFailSprite;
            img.color = Color.white;
        }
        else
        {
            img.color = red;
        }

        yield return new WaitForSeconds(0.3f);

        flashingRed = false;
        RefreshSlots();
    }

    // ================= 도우미 함수 =================
    private Sprite LitSprite(TimeSymbol symbol)
    {
        switch (symbol)
        {
            case TimeSymbol.Noon: return noonSprite;
            case TimeSymbol.Night: return nightSprite;
            case TimeSymbol.Dawn: return dawnSprite;
            default: return duskSprite;
        }
    }

    // 코드로 만드는 원 스프라이트 (원판, 짧은 그림자, 해 힌트용, 한 번만 만들어서 재사용)
    private static Sprite GetCircleSprite()
    {
        if (circleSprite != null) return circleSprite;

        const int size = 256;
        float r = size * 0.5f;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                byte a = (byte)(Mathf.Clamp01(r - d) * 255f); // 가장자리 1픽셀 부드럽게
                pixels[y * size + x] = new Color32(255, 255, 255, a);
            }
        }

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.SetPixels32(pixels);
        tex.Apply();

        circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return circleSprite;
    }

    // [추가] 긴 그림자: 왼쪽(오벨리스크 쪽)이 굵고 진하고, 오른쪽 끝으로 갈수록 가늘고 흐려짐
    private static Sprite GetWedgeSprite()
    {
        if (wedgeSprite != null) return wedgeSprite;

        const int w = 256, h = 64;
        var pixels = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                pixels[y * w + x] = new Color32(255, 255, 255, (byte)(WedgeAlpha(x / (float)w, y / (float)(h - 1)) * 255f));
            }
        }

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        tex.SetPixels32(pixels);
        tex.Apply();
        wedgeSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0f, 0.5f), 100f);
        return wedgeSprite;
    }

    private static float WedgeAlpha(float u, float v)
    {
        float half = Mathf.Lerp(0.5f, 0.16f, u);             // 끝으로 갈수록 가늘게
        float d = Mathf.Abs(v - 0.5f);
        float edge = Mathf.Clamp01((half - d) / 0.05f);      // 옆 가장자리 부드럽게
        float fade = Mathf.Lerp(1f, 0.45f, u);               // 끝으로 갈수록 흐리게
        float tip = Mathf.Clamp01((1f - u) / 0.12f);         // 맨 끝 둥글게 사라짐
        return edge * fade * tip;
    }

    // 그림이 있으면 그림으로, 없으면 색 네모로
    private Image CreatePiece(RectTransform parent, string objName, Vector2 pos, Vector2 size,
        Sprite sprite, Color fallbackColor, float rotZ = 0f)
    {
        if (sprite != null) return CreateSprite(parent, objName, pos, size, sprite, rotZ);
        return CreateRect(parent, objName, pos, size, fallbackColor, rotZ);
    }

    private Image CreateSprite(RectTransform parent, string objName, Vector2 pos, Vector2 size, Sprite sprite, float rotZ = 0f)
    {
        Image img = CreateRect(parent, objName, pos, size, Color.white, rotZ);
        img.sprite = sprite;
        img.preserveAspect = true;
        return img;
    }

    private Image CreateRect(RectTransform parent, string objName, Vector2 pos, Vector2 size, Color color, float rotZ = 0f)
    {
        var go = new GameObject(objName, typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        rt.localRotation = Quaternion.Euler(0, 0, rotZ);

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
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
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

    // ================= 에디터 전용: 크기 기본값 적용 =================
#if UNITY_EDITOR
    [ContextMenu("크기 기본값 적용")]
    private void ApplyDefaultSizes()
    {
        try
        {
            UnityEditor.Undo.RecordObject(this, "P1 크기 기본값 적용");

            symbolSize = DefaultSymbolSize;
            dialSize = DefaultDialSize;
            longShadowSize = DefaultLongShadowSize;
            shortShadowSize = DefaultShortShadowSize;
            sunSize = DefaultSunSize;
            slotSize = DefaultSlotSize;
            lensSize = DefaultLensSize;
            lensPos = DefaultLensPos;
            obeliskSize = DefaultObeliskSize;

            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[{name}] P1 크기 기본값 적용 완료 (그림 연결은 그대로)");
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 크기 기본값 적용 실패: {e}");
        }
    }
#endif
}