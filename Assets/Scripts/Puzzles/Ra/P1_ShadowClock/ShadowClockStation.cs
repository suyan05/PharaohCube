using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// OBJ_005 그림자 다이얼: E키로 P1 오버레이 열기/닫기 + 화면 구성
// 판정은 ShadowClockPuzzle이 그대로 담당하고, 이 스크립트는 화면만 담당
// 시계판은 기본으로 코드가 정면 원판을 그림 (비스듬한 Clock face 그림은 맵 오브젝트용)
// 정면 시계판 그림이 오면 Clock Face Sprite에 넣고 Use Clock Face Art 켜기
public class ShadowClockStation : MonoBehaviour, IInteractable
{
    // 기본 크기 (우클릭 -> "크기 기본값 적용"으로 씬에 저장된 값을 덮어쓸 수 있음)
    private static readonly Vector2 DefaultSymbolSize = new Vector2(130, 160);
    private static readonly Vector2 DefaultDialSize = new Vector2(300, 300);
    private static readonly Vector2 DefaultHandSize = new Vector2(18, 135);
    private static readonly Vector2 DefaultSlotSize = new Vector2(200, 70);
    private static readonly Vector2 DefaultLensSize = new Vector2(120, 120);
    private static readonly Vector2 DefaultLensPos = new Vector2(290, -20);
    private static readonly Vector2 DefaultObeliskSize = new Vector2(64, 64);

    private const float WallY = 300f;
    private const float SlotY = -280f;
    private static readonly Vector2 DialPos = new Vector2(0, -20);

    [Header("연결")]
    [SerializeField] private ShadowClockPuzzle puzzle;
    [SerializeField] private GameObject overlayPanel;

    [Header("아트 - 시계판")]
    [Tooltip("끄면 코드로 정면 원판을 그림. 정면 시계판 그림이 생기면 켜기")]
    [SerializeField] private bool useClockFaceArt = false;
    [SerializeField] private Sprite clockFaceSprite;   // Clock face (정면 그림일 때만 사용)
    [SerializeField] private Sprite obeliskSprite;     // Central Obelisk
    [SerializeField] private Sprite shadowHandSprite;  // shadow (Use Clock Face Art 켰을 때만 사용)

    [Header("아트 - 벽 기호 (렌즈 장착 전/후)")]
    [SerializeField] private Sprite noonSprite;
    [SerializeField] private Sprite nightSprite;
    [SerializeField] private Sprite dawnSprite;
    [SerializeField] private Sprite duskSprite;
    [SerializeField] private Sprite symbolInactiveSprite; // 렌즈 장착 전 공통 (Inactive)

    [Header("아트 - 렌즈")]
    [SerializeField] private Sprite lensInactiveSprite;  // [확인 필요] lens 파일이 맞는지
    [SerializeField] private Sprite lensActiveSprite;    // Lens Activation

    [Header("아트 - 입력 슬롯")]
    [SerializeField] private Sprite slotEmptySprite;     // Slot Board
    [Tooltip("끄면 빈 슬롯 그림 하나로 색만 바꿈 (상태별 그림 비율이 달라서 크기가 변하는 문제 방지)")]
    [SerializeField] private bool useSlotStateSprites = false;
    [SerializeField] private Sprite slotFilledSprite;    // Slot Input
    [SerializeField] private Sprite slotSuccessSprite;   // Slot entry successful
    [SerializeField] private Sprite slotFailSprite;      // Slot input failed

    [Header("크기 (우클릭 -> 크기 기본값 적용)")]
    [SerializeField] private Vector2 symbolSize = DefaultSymbolSize;
    [SerializeField] private Vector2 dialSize = DefaultDialSize;
    [SerializeField] private Vector2 handSize = DefaultHandSize;
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
    private RectTransform shadowHand;
    private Image lensImage;
    private Text lensLabel;
    private Text statusText;
    private Font font;

    private static Sprite circleSprite;

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

            BuildUI();
            overlayPanel.SetActive(false);
            Subscribe();
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 초기화 오류: {e}");
        }
    }

    private void Subscribe()
    {
        puzzle.OnInputChanged += RefreshSlots;
        puzzle.OnLensMounted += RefreshSymbols;
        puzzle.OnSuccess += HandleSuccess;
        puzzle.OnFail += HandleFail;
        puzzle.OnHint += HandleHint;
    }

    private void OnDestroy()
    {
        if (puzzle == null) return;
        puzzle.OnInputChanged -= RefreshSlots;
        puzzle.OnLensMounted -= RefreshSymbols;
        puzzle.OnSuccess -= HandleSuccess;
        puzzle.OnFail -= HandleFail;
        puzzle.OnHint -= HandleHint;
    }

    private void Update()
    {
        if (!isOpen) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseOverlay();
            return;
        }

        // 그림자 바늘: 시계 방향 회전 (기본 8초에 한 바퀴)
        shadowHand.Rotate(0, 0, -360f / puzzle.ShadowPeriod * Time.deltaTime);
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

        puzzle.Open(); // 여기서 렌즈 자동 장착 시도

        RefreshSymbols();
        RefreshSlots();
        statusText.text = GetOpenMessage();
    }

    private void CloseOverlay()
    {
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
        if (puzzle.IsCleared) return "이미 해독한 시계다.";
        if (puzzle.LensMounted) return "주황 렌즈가 벽의 기호를 비춘다.";
        return "기호가 너무 어둡다. 무언가로 비춰봐야 할 것 같다.";
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

        // 동쪽 벽 기호 띠 (그림 유무와 상관없이 항상)
        CreateRect(root, "WallBand", new Vector2(0, WallY), new Vector2(1100, 230), wallColor);
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
        CreateText(root, "기호를 순서대로 클릭  |  E / Esc : 닫기", 22,
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

        BuildShadowHand(root);

        if (obeliskSprite != null) CreateSprite(root, "Obelisk", DialPos, obeliskSize, obeliskSprite);
        else CreateRect(root, "Obelisk", DialPos, obeliskSize * 0.5f, gold, 45f);
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

    private void BuildShadowHand(RectTransform root)
    {
        Image hand;
        if (useClockFaceArt && shadowHandSprite != null)
        {
            hand = CreateSprite(root, "ShadowHand", DialPos, handSize, shadowHandSprite);
        }
        else
        {
            hand = CreateRect(root, "ShadowHand", DialPos, handSize, shadowColor);
        }

        shadowHand = hand.rectTransform;
        shadowHand.pivot = new Vector2(0.5f, 0f); // 바늘 아래쪽을 축으로 회전
        shadowHand.anchoredPosition = DialPos;
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

    // ================= 입력 / 화면 갱신 =================
    private void OnSymbolClicked(TimeSymbol symbol)
    {
        if (!puzzle.LensMounted)
        {
            statusText.text = "기호가 너무 어둡다. 무언가로 비춰봐야 할 것 같다.";
            return;
        }
        puzzle.PressSymbol(symbol);
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
        lensLabel.color = mounted ? gold : dimText;

        if (lensInactiveSprite == null && lensActiveSprite == null)
        {
            lensImage.color = mounted ? gold : dimText;
            return;
        }

        Sprite sprite = mounted ? lensActiveSprite : lensInactiveSprite;
        if (sprite != null) lensImage.sprite = sprite;
        lensImage.color = Color.white;
    }

    private void RefreshSlots()
    {
        var inputs = puzzle.Inputs;
        for (int i = 0; i < 4; i++)
        {
            bool filled = i < inputs.Count;
            slotTexts[i].text = filled ? ShadowClockPuzzle.ToKorean(inputs[i]) : (i + 1).ToString();

            if (!flashingRed)
            {
                UpdateSlotVisual(i, filled);
            }
        }
    }

    private void UpdateSlotVisual(int i, bool filled)
    {
        // 그림이 없을 때: 밝은 네모 + 진한 글자
        if (slotEmptySprite == null)
        {
            slotImages[i].color = puzzle.IsCleared ? gold : (filled ? paper : sand);
            slotTexts[i].color = darkBrown;
            return;
        }

        slotTexts[i].color = filled ? paper : dimText;

        if (useSlotStateSprites)
        {
            UpdateSlotWithStateSprites(i, filled);
            return;
        }

        // 빈 슬롯 그림 하나로 색만 바꿈
        slotImages[i].sprite = slotEmptySprite;
        if (puzzle.IsCleared) slotImages[i].color = successTint;
        else slotImages[i].color = filled ? Color.white : new Color(0.75f, 0.75f, 0.75f, 1f);
    }

    private void UpdateSlotWithStateSprites(int i, bool filled)
    {
        Sprite sprite = slotEmptySprite;
        if (puzzle.IsCleared && slotSuccessSprite != null) sprite = slotSuccessSprite;
        else if (filled && slotFilledSprite != null) sprite = slotFilledSprite;

        slotImages[i].sprite = sprite;
        slotImages[i].color = Color.white;
    }

    private void HandleSuccess()
    {
        statusText.text = "그림자가 멈췄다. 거울 조각 1 획득!";
        RefreshSlots();
    }

    private void HandleFail()
    {
        statusText.text = "순서가 맞지 않는다...";
        if (isActiveAndEnabled) StartCoroutine(FlashSlotsRed());
    }

    private void HandleHint()
    {
        statusText.text = "'새벽' 기호가 희미하게 빛난다...";
        if (isActiveAndEnabled) StartCoroutine(HintGlow());
    }

    // 오답: 붉은 플래시 0.3초 (기획서 10.3)
    private IEnumerator FlashSlotsRed()
    {
        flashingRed = true;
        foreach (Image img in slotImages)
        {
            bool useFailArt = useSlotStateSprites && slotFailSprite != null;
            if (useFailArt) { img.sprite = slotFailSprite; img.color = Color.white; }
            else img.color = red;
        }

        yield return new WaitForSeconds(0.3f);

        flashingRed = false;
        RefreshSlots();
    }

    // 힌트: '새벽' 기호 5초 발광 (기획서 6.1 튜닝)
    private IEnumerator HintGlow()
    {
        int index = Array.IndexOf(ShadowClockPuzzle.WallOrder, TimeSymbol.Dawn);
        bool hasSprite = symbolImages[index].sprite != null;
        float t = 0f;
        bool on = false;

        while (t < 5f)
        {
            on = !on;
            if (hasSprite)
            {
                symbolImages[index].color = on ? Color.white : new Color(1f, 0.95f, 0.7f, 1f);
            }
            else
            {
                symbolImages[index].color = on ? glow : gold;
            }
            yield return new WaitForSeconds(0.25f);
            t += 0.25f;
        }

        RefreshSymbols();
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

    // 코드로 만드는 원 스프라이트 (정면 원판용, 한 번만 만들어서 재사용)
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
            handSize = DefaultHandSize;
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