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

    // 기획서 11번 색상
    private Color wallColor, darkBrown, gold, paper, sand, red, glow, dimText;

    private readonly Image[] symbolImages = new Image[4];
    private readonly Text[] symbolTexts = new Text[4];
    private readonly Image[] slotImages = new Image[4];
    private readonly Text[] slotTexts = new Text[4];
    private RectTransform shadowHand;
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

        // 동쪽 벽 기호 밴드
        CreateRect(root, "WallBand", new Vector2(0, 280), new Vector2(1060, 160), wallColor);

        for (int i = 0; i < 4; i++)
        {
            TimeSymbol symbol = ShadowClockPuzzle.WallOrder[i];
            float x = -375f + i * 250f;

            Image img = CreateRect(root, $"Symbol_{symbol}", new Vector2(x, 280), new Vector2(200, 120), darkBrown);
            img.raycastTarget = true;

            Button btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => OnSymbolClicked(symbol));

            symbolImages[i] = img;
            symbolTexts[i] = CreateText(img.rectTransform, ShadowClockPuzzle.ToKorean(symbol), 36,
                Vector2.zero, new Vector2(200, 120), dimText);
        }

        // 다이얼 + 그림자 바늘
        CreateRect(root, "DialBorder", Vector2.zero, new Vector2(316, 316), darkBrown);
        CreateRect(root, "Dial", Vector2.zero, new Vector2(300, 300), sand);

        shadowHand = CreateRect(root, "ShadowHand", Vector2.zero, new Vector2(14, 135), darkBrown).rectTransform;
        shadowHand.pivot = new Vector2(0.5f, 0f);   // 바늘 아래쪽을 축으로 회전
        shadowHand.anchoredPosition = Vector2.zero;

        CreateRect(root, "Obelisk", Vector2.zero, new Vector2(30, 30), darkBrown, 45f);

        // 입력 슬롯 4칸
        for (int i = 0; i < 4; i++)
        {
            float x = -375f + i * 250f;
            slotImages[i] = CreateRect(root, $"Slot_{i + 1}", new Vector2(x, -260), new Vector2(180, 90), sand);
            slotTexts[i] = CreateText(slotImages[i].rectTransform, (i + 1).ToString(), 30,
                Vector2.zero, new Vector2(180, 90), darkBrown);
        }

        statusText = CreateText(root, "", 28, new Vector2(0, -380), new Vector2(1200, 60), paper);
        CreateText(root, "기호를 순서대로 클릭  |  E / Esc : 닫기", 22,
            new Vector2(0, -450), new Vector2(1200, 40), wallColor);
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
            symbolImages[i].color = puzzle.LensMounted ? gold : darkBrown;
            symbolTexts[i].color = puzzle.LensMounted ? darkBrown : dimText;
        }
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
                slotImages[i].color = puzzle.IsCleared ? gold : (filled ? paper : sand);
            }
        }
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
        foreach (var img in slotImages) img.color = red;

        yield return new WaitForSeconds(0.3f);

        flashingRed = false;
        RefreshSlots();
    }

    // 힌트: '새벽' 기호 5초 발광 (기획서 6.1 튜닝)
    private IEnumerator HintGlow()
    {
        int index = Array.IndexOf(ShadowClockPuzzle.WallOrder, TimeSymbol.Dawn);
        float t = 0f;
        bool on = false;

        while (t < 5f)
        {
            on = !on;
            symbolImages[index].color = on ? glow : gold;
            yield return new WaitForSeconds(0.25f);
            t += 0.25f;
        }

        RefreshSymbols();
    }

    // ================= 도우미 함수 =================
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