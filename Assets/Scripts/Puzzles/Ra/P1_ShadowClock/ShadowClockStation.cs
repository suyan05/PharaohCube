using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// OBJ_005 그림자 다이얼: E키로 P1 오버레이 열기/닫기 + 화면 구성
public class ShadowClockStation : MonoBehaviour, IInteractable
{
    [Header("연결")]
    [SerializeField] private ShadowClockPuzzle puzzle;
    [SerializeField] private GameObject overlayPanel;

    [Header("아트 - 시계판 (비우면 기존 색 네모 + 글자)")]
    [SerializeField] private Sprite clockFaceSprite;   // Clock face
    [SerializeField] private Sprite obeliskSprite;     // Central Obelisk
    [SerializeField] private Sprite shadowHandSprite;  // shadow (그림자 바늘)

    [Header("아트 - 벽 기호 (렌즈 장착 전/후)")]
    [SerializeField] private Sprite noonSprite;   // Noon-pattern carving
    [SerializeField] private Sprite nightSprite;  // Twilight Pattern Fragment
    [SerializeField] private Sprite dawnSprite;   // Dawn Pattern Fragment
    [SerializeField] private Sprite duskSprite;   // Day Pattern Fragment
    [SerializeField] private Sprite symbolInactiveSprite; // 렌즈 장착 전 공통 (Inactive)

    [Header("아트 - 렌즈")]
    [SerializeField] private Sprite lensInactiveSprite;  // Inactive
    [SerializeField] private Sprite lensActiveSprite;    // Lens Activation

    [Header("아트 - 입력 슬롯")]
    [SerializeField] private Sprite slotEmptySprite;     // Slot Board
    [SerializeField] private Sprite slotFilledSprite;    // Slot Input
    [SerializeField] private Sprite slotSuccessSprite;   // Slot entry successful
    [SerializeField] private Sprite slotFailSprite;      // Slot input failed

    [Header("크기")]
    [SerializeField] private Vector2 symbolSize = new Vector2(200, 120);
    [SerializeField] private Vector2 dialSize = new Vector2(320, 320);
    [SerializeField] private Vector2 handSize = new Vector2(14, 135);
    [SerializeField] private Vector2 slotSize = new Vector2(180, 90);
    [SerializeField] private Vector2 lensSize = new Vector2(70, 70);
    [SerializeField] private Vector2 lensPos = new Vector2(0, 420);

    [Header("글자 표시 (아트에 글자가 있으면 꺼도 됨)")]
    [SerializeField] private bool showSymbolLabels = true;

    // 기획서 11번 색상
    private Color wallColor, darkBrown, gold, paper, sand, red, glow, dimText;

    private readonly Image[] symbolImages = new Image[4];
    private readonly Text[] symbolTexts = new Text[4];
    private readonly Image[] slotImages = new Image[4];
    private readonly Text[] slotTexts = new Text[4];
    private RectTransform shadowHand;
    private Image lensImage;
    private Text statusText;
    private Font font;

    private bool isOpen;
    private bool flashingRed;
    private PlayerController2D playerController;
    private Rigidbody2D playerRb;

    private void Start()
    {
        SetupColors();
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        BuildUI();
        overlayPanel.SetActive(false);

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

        // 그림자 바늘: 시계 방향 회전 (8초에 한 바퀴)
        shadowHand.Rotate(0, 0, -360f / puzzle.ShadowPeriod * Time.deltaTime);
    }

    private void SetupColors()
    {
        wallColor = Hex("#C9A66B");
        darkBrown = Hex("#4A3423");
        gold = Hex("#E8B23A");
        paper = Hex("#F6F1E7");
        sand = Hex("#E8D9B8");
        red = Hex("#C0392B");
        glow = Hex("#FFF2B3");
        dimText = Hex("#7A6450");
    }

    // ================= IInteractable =================
    public string GetPrompt()
    {
        return isOpen ? "E: 다이얼 닫기" : "E: 그림자 다이얼 조사";
    }

    public void Interact(GameObject player)
    {
        if (isOpen) CloseOverlay();
        else OpenOverlay(player);
    }

    private void OpenOverlay(GameObject player)
    {
        playerController = player.GetComponent<PlayerController2D>();
        playerRb = player.GetComponent<Rigidbody2D>();
        if (playerController != null) playerController.enabled = false;
        if (playerRb != null) playerRb.linearVelocity = Vector2.zero;

        overlayPanel.SetActive(true);
        isOpen = true;

        puzzle.Open(); // 여기서 렌즈 자동 장착 시도

        RefreshSymbols();
        RefreshSlots();
        RefreshLens();

        if (puzzle.IsCleared) statusText.text = "이미 해독한 시계다.";
        else if (puzzle.LensMounted) statusText.text = "주황 렌즈가 벽의 기호를 비춘다.";
        else statusText.text = "기호가 너무 어둡다. 무언가로 비춰봐야 할 것 같다.";
    }

    private void CloseOverlay()
    {
        overlayPanel.SetActive(false);
        isOpen = false;
        if (playerController != null) playerController.enabled = true;
    }

    // ================= 화면 만들기 =================
    private void BuildUI()
    {
        RectTransform root = overlayPanel.GetComponent<RectTransform>();

        CreateText(root, "태양 그림자 시계", 44, new Vector2(0, 430), new Vector2(800, 70), paper);

        // 동쪽 벽 기호 밴드 (배경 그림이 없을 때만 띠를 그림)
        if (clockFaceSprite == null)
        {
            CreateRect(root, "WallBand", new Vector2(0, 280), new Vector2(1060, 160), wallColor);
        }

        for (int i = 0; i < 4; i++)
        {
            BuildSymbol(root, i);
        }

        BuildDial(root);
        BuildLens(root);

        // 입력 슬롯 4칸
        for (int i = 0; i < 4; i++)
        {
            float x = -375f + i * 250f;
            slotImages[i] = CreatePiece(root, $"Slot_{i + 1}", new Vector2(x, -260), slotSize, slotEmptySprite, sand);
            slotTexts[i] = CreateText(slotImages[i].rectTransform, (i + 1).ToString(), 30,
                Vector2.zero, slotSize, darkBrown);
        }

        statusText = CreateText(root, "", 28, new Vector2(0, -380), new Vector2(1200, 60), paper);
        CreateText(root, "기호를 순서대로 클릭  |  E / Esc : 닫기", 22,
            new Vector2(0, -450), new Vector2(1200, 40), wallColor);
    }

    private void BuildSymbol(RectTransform root, int index)
    {
        TimeSymbol symbol = ShadowClockPuzzle.WallOrder[index];
        float x = -375f + index * 250f;
        Sprite sprite = LitSprite(symbol); // 처음엔 꺼진 모습으로 시작, RefreshSymbols가 바로 맞춰줌

        Image img = CreatePiece(root, $"Symbol_{symbol}", new Vector2(x, 280), symbolSize, sprite, darkBrown);
        img.raycastTarget = true;

        Button btn = img.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => OnSymbolClicked(symbol));

        symbolImages[index] = img;

        if (showSymbolLabels || sprite == null)
        {
            Color labelColor = sprite != null ? paper : dimText;
            Vector2 labelPos = sprite != null ? new Vector2(0, -symbolSize.y * 0.6f) : Vector2.zero;
            symbolTexts[index] = CreateText(img.rectTransform, ShadowClockPuzzle.ToKorean(symbol), 30,
                labelPos, symbolSize, labelColor);
        }
    }

    private void BuildDial(RectTransform root)
    {
        if (clockFaceSprite != null)
        {
            CreateSprite(root, "Dial", Vector2.zero, dialSize, clockFaceSprite);
        }
        else
        {
            CreateRect(root, "DialBorder", Vector2.zero, dialSize + new Vector2(16, 16), darkBrown);
            CreateRect(root, "Dial", Vector2.zero, dialSize, sand);
        }

        shadowHand = CreatePiece(root, "ShadowHand", Vector2.zero, handSize, shadowHandSprite, darkBrown).rectTransform;
        shadowHand.pivot = new Vector2(0.5f, 0f);   // 바늘 아래쪽을 축으로 회전
        shadowHand.anchoredPosition = Vector2.zero;

        CreatePiece(root, "Obelisk", Vector2.zero, new Vector2(30, 30), obeliskSprite, darkBrown, 45f);
    }

    private void BuildLens(RectTransform root)
    {
        lensImage = CreatePiece(root, "Lens", lensPos, lensSize, lensInactiveSprite, Color.clear);
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

            if (symbolTexts[i] != null)
            {
                symbolTexts[i].color = puzzle.LensMounted ? darkBrown : dimText;
            }
        }
        RefreshLens();
    }

    private void RefreshLens()
    {
        if (lensInactiveSprite == null && lensActiveSprite == null) return;
        bool mounted = puzzle.LensMounted;
        lensImage.sprite = mounted ? lensActiveSprite : lensInactiveSprite;
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
        if (slotEmptySprite == null)
        {
            slotImages[i].color = puzzle.IsCleared ? gold : (filled ? paper : sand);
            return;
        }

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
        StartCoroutine(FlashSlotsRed());
    }

    private void HandleHint()
    {
        statusText.text = "'새벽' 기호가 희미하게 빛난다...";
        StartCoroutine(HintGlow());
    }

    // 오답: 붉은 플래시 0.3초 (기획서 10.3)
    private IEnumerator FlashSlotsRed()
    {
        flashingRed = true;
        foreach (var img in slotImages)
        {
            if (slotFailSprite != null) { img.sprite = slotFailSprite; img.color = Color.white; }
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
                symbolImages[index].color = on ? new Color(1f, 1f, 1f, 1f) : new Color(1f, 0.95f, 0.7f, 1f);
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

    // 그림이 있으면 그림으로, 없으면 색 네모로
    private Image CreatePiece(RectTransform parent, string name, Vector2 pos, Vector2 size,
        Sprite sprite, Color fallbackColor, float rotZ = 0f)
    {
        if (sprite != null) return CreateSprite(parent, name, pos, size, sprite, rotZ);
        return CreateRect(parent, name, pos, size, fallbackColor, rotZ);
    }

    private Image CreateSprite(RectTransform parent, string name, Vector2 pos, Vector2 size, Sprite sprite, float rotZ = 0f)
    {
        Image img = CreateRect(parent, name, pos, size, Color.white, rotZ);
        img.sprite = sprite;
        img.preserveAspect = true;
        return img;
    }

    private Image CreateRect(RectTransform parent, string name, Vector2 pos, Vector2 size, Color color, float rotZ = 0f)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
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
}