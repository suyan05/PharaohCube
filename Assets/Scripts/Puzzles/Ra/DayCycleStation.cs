using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// OBJ_009 카드 벤치: E키로 P4 오버레이 열기/닫기 + 카드 드래그 화면
public class DayCycleStation : MonoBehaviour, IInteractable
{
    [Header("연결")]
    [SerializeField] private DayCyclePuzzle puzzle;
    [SerializeField] private GameObject overlayPanel;

    private const float SlotSpacing = 250f;
    private const float SlotY = -100f;

    private static readonly TimeSymbol[] AllSymbols =
    {
        TimeSymbol.Noon, TimeSymbol.Night, TimeSymbol.Dawn, TimeSymbol.Dusk
    };

    private readonly Dictionary<TimeSymbol, DragCard> cards = new Dictionary<TimeSymbol, DragCard>();
    private readonly Dictionary<TimeSymbol, Image> cardImages = new Dictionary<TimeSymbol, Image>();
    private GameObject sealBand;
    private Text statusText;
    private Font font;
    private Canvas canvas;

    private Color wallColor, darkBrown, gold, paper, sand, red, dimCard, sealColor;

    private bool isOpen;
    private bool dragAllowed;
    private bool flashingRed;
    private PlayerController2D playerController;
    private Rigidbody2D playerRb;

    private void Start()
    {
        SetupColors();
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        canvas = overlayPanel.GetComponentInParent<Canvas>(); // 드래그 이동량 보정용

        BuildUI();
        overlayPanel.SetActive(false);

        puzzle.OnArrangementChanged += LayoutCards;
        puzzle.OnUnsealed += HandleUnsealed;
        puzzle.OnSuccess += HandleSuccess;
        puzzle.OnFail += HandleFail;
    }

    private void OnDestroy()
    {
        if (puzzle == null) return;
        puzzle.OnArrangementChanged -= LayoutCards;
        puzzle.OnUnsealed -= HandleUnsealed;
        puzzle.OnSuccess -= HandleSuccess;
        puzzle.OnFail -= HandleFail;
    }

    private void Update()
    {
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
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
        red = Hex("#C0392B");
        dimCard = Hex("#B9A98A");
        sealColor = Hex("#8B2E1F");
    }

    private Vector2 SlotPos(int index) => new Vector2(-375f + index * SlotSpacing, SlotY);

    // ================= IInteractable =================
    public string GetPrompt()
    {
        return isOpen ? "E: 벤치 닫기" : "E: 카드 벤치 조사";
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

        if (puzzle.IsCleared) statusText.text = "이미 해결한 벤치다.";
        else if (puzzle.Unsealed) statusText.text = "벽화를 보고 카드를 동쪽(왼쪽)부터 배열하자.";
        else statusText.text = "봉인줄이 감겨 있다. 열쇠가 필요할 것 같다.";

        puzzle.Open(); // 여기서 황동 열쇠 자동 사용 시도

        LayoutCards();
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

        CreateText(root, "동쪽에서 서쪽으로", 44, new Vector2(0, 440), new Vector2(800, 70), paper);

        // 남쪽 벽화 단서 (항상 올바른 순서)
        CreateText(root, "남쪽 벽화 '하루의 순서'", 22, new Vector2(0, 375), new Vector2(600, 30), sand);
        CreateRect(root, "MuralBand", new Vector2(0, 300), new Vector2(1060, 120), wallColor);
        for (int i = 0; i < 4; i++)
        {
            Image panel = CreateRect(root, $"Mural_{i + 1}", new Vector2(-375f + i * SlotSpacing, 300),
                new Vector2(200, 90), paper);
            CreateText(panel.rectTransform, $"{i + 1}. {ShadowClockPuzzle.ToKorean(DayCyclePuzzle.AnswerAt(i))}",
                30, Vector2.zero, new Vector2(200, 90), darkBrown);
        }
        CreateText(root, "← 동 (왼쪽)                                        서 (오른쪽) →", 22,
            new Vector2(0, 222), new Vector2(1060, 30), sand);

        // 슬롯 4칸
        for (int i = 0; i < 4; i++)
        {
            CreateRect(root, $"Slot_{i + 1}", SlotPos(i), new Vector2(200, 150), sand);
            CreateText(root, $"슬롯 {i + 1}", 20, SlotPos(i) + new Vector2(0, -95), new Vector2(200, 30), sand);
        }

        // 카드 4장 (드래그 가능)
        foreach (TimeSymbol symbol in AllSymbols)
        {
            Image img = CreateRect(root, $"Card_{symbol}", SlotPos(puzzle.IndexOf(symbol)),
                new Vector2(180, 130), paper);
            img.raycastTarget = true;

            DragCard card = img.gameObject.AddComponent<DragCard>();
            card.symbol = symbol;
            card.onBeginDrag = OnCardBeginDrag;
            card.onDrag = OnCardDrag;
            card.onEndDrag = OnCardEndDrag;

            CreateText(img.rectTransform, ShadowClockPuzzle.ToKorean(symbol), 36,
                Vector2.zero, new Vector2(180, 130), darkBrown);

            cards[symbol] = card;
            cardImages[symbol] = img;
        }

        // 봉인줄 (열쇠 쓰기 전까지 카드 위에 표시)
        Image seal = CreateRect(root, "SealBand", new Vector2(0, SlotY), new Vector2(1060, 40), sealColor);
        CreateText(seal.rectTransform, "봉인줄", 24, Vector2.zero, new Vector2(1060, 40), paper);
        sealBand = seal.gameObject;

        // 확정 / 취소 버튼
        CreateButton(root, "확정", new Vector2(-130, -280), gold, OnConfirmClicked);
        CreateButton(root, "취소(초기화)", new Vector2(130, -280), paper, OnCancelClicked);

        statusText = CreateText(root, "", 28, new Vector2(0, -370), new Vector2(1200, 60), paper);
        CreateText(root, "카드를 끌어서 자리 바꾸기  |  확정: 판정  |  취소: 다시 섞기  |  E / Esc : 닫기", 20,
            new Vector2(0, -430), new Vector2(1400, 30), sand);
    }

    // ================= 드래그 =================
    private bool CanArrange => puzzle.Unsealed && !puzzle.IsCleared;

    private void OnCardBeginDrag(DragCard card)
    {
        if (!CanArrange)
        {
            dragAllowed = false;
            if (!puzzle.Unsealed) statusText.text = "봉인줄이 감겨 있다. 열쇠가 필요할 것 같다.";
            return;
        }

        dragAllowed = true;
        card.transform.SetAsLastSibling(); // 끄는 카드를 맨 앞으로
    }

    private void OnCardDrag(DragCard card, Vector2 delta)
    {
        if (!dragAllowed) return;

        // 화면 해상도 배율만큼 나눠서 마우스를 정확히 따라가게
        var rt = (RectTransform)card.transform;
        rt.anchoredPosition += delta / canvas.scaleFactor;
    }

    private void OnCardEndDrag(DragCard card)
    {
        if (!dragAllowed) return;
        dragAllowed = false;

        var rt = (RectTransform)card.transform;
        int from = puzzle.IndexOf(card.symbol);

        // 가장 가까운 슬롯 찾기
        int nearest = -1;
        float nearestDist = float.MaxValue;
        for (int i = 0; i < puzzle.SlotCount; i++)
        {
            float d = Vector2.Distance(rt.anchoredPosition, SlotPos(i));
            if (d < nearestDist)
            {
                nearestDist = d;
                nearest = i;
            }
        }

        // 0.5칸 이내면 그 자리 카드와 교환, 아니면 원위치 (기획서 6.3: 스냅 0.5 슬롯)
        if (nearestDist <= SlotSpacing * 0.5f && nearest != from)
        {
            puzzle.SwapSlots(from, nearest); // → OnArrangementChanged → LayoutCards
        }
        else
        {
            LayoutCards();
        }
    }

    // ================= 버튼 =================
    private void OnConfirmClicked()
    {
        if (!puzzle.Unsealed)
        {
            statusText.text = "봉인줄이 감겨 있다. 열쇠가 필요할 것 같다.";
            return;
        }
        puzzle.Submit();
    }

    private void OnCancelClicked()
    {
        if (!CanArrange) return;
        puzzle.ShuffleCards();
        statusText.text = "카드를 다시 섞었다.";
    }

    // ================= 화면 갱신 =================
    private void LayoutCards()
    {
        foreach (TimeSymbol symbol in AllSymbols)
        {
            var rt = (RectTransform)cards[symbol].transform;
            rt.anchoredPosition = SlotPos(puzzle.IndexOf(symbol));
        }
        RefreshColors();
    }

    private void RefreshColors()
    {
        if (!flashingRed)
        {
            foreach (TimeSymbol symbol in AllSymbols)
            {
                if (puzzle.IsCleared) cardImages[symbol].color = gold;
                else if (puzzle.Unsealed) cardImages[symbol].color = paper;
                else cardImages[symbol].color = dimCard;
            }
        }

        sealBand.SetActive(!puzzle.Unsealed);
    }

    private void HandleUnsealed()
    {
        statusText.text = "황동 열쇠로 봉인줄을 풀었다! 카드를 움직일 수 있다.";
        RefreshColors();
    }

    private void HandleSuccess()
    {
        statusText.text = "벽화와 같은 순서다! 거울 조각 ③ 획득";
        RefreshColors();
    }

    private void HandleFail()
    {
        statusText.text = "순서가 맞지 않는다... (배치는 그대로)";
        StartCoroutine(FlashCardsRed());
    }

    // 오답: 붉은 플래시 0.3초 (기획서 10.3)
    private IEnumerator FlashCardsRed()
    {
        flashingRed = true;
        foreach (var img in cardImages.Values) img.color = red;

        yield return new WaitForSeconds(0.3f);

        flashingRed = false;
        RefreshColors();
    }

    // ================= 도우미 함수 =================
    private void CreateButton(RectTransform parent, string label, Vector2 pos, Color color,
                              UnityEngine.Events.UnityAction onClick)
    {
        Image img = CreateRect(parent, $"Btn_{label}", pos, new Vector2(200, 70), color);
        img.raycastTarget = true;

        Button btn = img.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        CreateText(img.rectTransform, label, 28, Vector2.zero, new Vector2(200, 70), darkBrown);
    }

    private Image CreateRect(RectTransform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

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