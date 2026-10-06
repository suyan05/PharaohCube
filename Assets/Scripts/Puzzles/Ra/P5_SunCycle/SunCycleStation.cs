using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// OBJ_010 태양 제단: E키로 P5 오버레이 열기/닫기 + 버튼 고리 화면
public class SunCycleStation : MonoBehaviour, IInteractable
{
    [Header("연결")]
    [SerializeField] private SunCyclePuzzle puzzle;
    [SerializeField] private GameObject overlayPanel;

    [Header("아트 - 고리 버튼 (비우면 기존 색 네모 + 글자)")]
    [SerializeField] private Sprite risingSprite;   // (1) 떠오르는 태양
    [SerializeField] private Sprite noonSprite;     // (2) 정오
    [SerializeField] private Sprite settingSprite;  // (3) 지는 태양
    [SerializeField] private Sprite nightSprite;    // (4) 밤
    [SerializeField] private Sprite boatSprite;     // (5) 태양의 배 [함정]
    [SerializeField] private Sprite returnSprite;   // (6) 재림

    [Header("아트 - 중앙 태양판")]
    [SerializeField] private Sprite centerPlateSprite;     // 꺼진 상태
    [SerializeField] private Sprite centerPlateLitSprite;  // 클리어 상태

    [Header("아트 - 입력 슬롯")]
    [SerializeField] private Sprite slotEmptySprite;
    [SerializeField] private Sprite slotFilledSprite;

    [Header("아트 - 배경 / 뒤로 버튼")]
    [SerializeField] private Sprite backgroundSprite;
    [SerializeField] private Sprite backButtonSprite;

    [Header("크기")]
    [SerializeField] private Vector2 buttonSize = new Vector2(190, 90);
    [SerializeField] private Vector2 slotSize = new Vector2(200, 80);
    [SerializeField] private Vector2 centerPlateSize = new Vector2(230, 120);
    [SerializeField] private Vector2 backgroundSize = new Vector2(1200, 1000);

    [Header("글자 표시 (아트에 글자가 있으면 꺼도 됨)")]
    [SerializeField] private bool showButtonLabels = true;

    private const float RingY = 120f;

    // 버튼 고리 배치 (기획서 그림 4-4: 방향이 의미를 암시)
    private static readonly SunButton[] RingButtons =
    {
        SunButton.Noon, SunButton.Return, SunButton.Rising,
        SunButton.Boat, SunButton.Night, SunButton.Setting
    };
    private static readonly Vector2[] RingPositions =
    {
        new Vector2(0, 230),      // (2) 정오 - 하늘, 12시
        new Vector2(199, 115),    // (6) 재림 - 2시
        new Vector2(199, -115),   // (1) 떠오르는 태양 - 4시
        new Vector2(0, -230),     // (5) 태양의 배 - 6시 [함정]
        new Vector2(-199, -115),  // (4) 밤 - 8시
        new Vector2(-199, 115)    // (3) 지는 태양 - 10시
    };

    private readonly List<Image> buttonImages = new List<Image>();
    private readonly Image[] slotImages = new Image[5];
    private readonly Text[] slotTexts = new Text[5];
    private Image centerPlate;
    private Text statusText;
    private Font font;

    private Color darkBrown, gold, paper, sand, red, dimButton, dimText, dimSun;

    private bool isOpen;
    private bool flashingRed;
    private PlayerController2D playerController;
    private Rigidbody2D playerRb;

    private void Start()
    {
        darkBrown = Hex("#4A3423");
        gold = Hex("#E8B23A");
        paper = Hex("#F6F1E7");
        sand = Hex("#E8D9B8");
        red = Hex("#C0392B");
        dimButton = Hex("#6B5438");
        dimText = Hex("#A89373");
        dimSun = Hex("#8A6A3A");
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        BuildUI();
        overlayPanel.SetActive(false);

        puzzle.OnInputChanged += RefreshAll;
        puzzle.OnHint += HandleHint;
        puzzle.OnSuccess += HandleSuccess;
        puzzle.OnFail += HandleFail;
    }

    private void OnDestroy()
    {
        if (puzzle == null) return;
        puzzle.OnInputChanged -= RefreshAll;
        puzzle.OnHint -= HandleHint;
        puzzle.OnSuccess -= HandleSuccess;
        puzzle.OnFail -= HandleFail;
    }

    private void Update()
    {
        if (isOpen && Input.GetKeyDown(KeyCode.Escape)) CloseOverlay();
    }

    // ================= IInteractable =================
    public string GetPrompt() => isOpen ? "E: 제단 닫기" : "E: 태양 제단 조사";

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

        puzzle.Open();

        if (puzzle.IsCleared) statusText.text = "태양의 순환이 완성되었다.";
        else if (puzzle.IsAwake) statusText.text = "태양의 하루를 제단에 새겨라.";
        else statusText.text = "제단이 아직 잠들어 있다.";

        RefreshAll();
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

        // 배경 그림 (있으면 맨 아래에 깔림)
        if (backgroundSprite != null)
        {
            CreateSprite(root, "Background", new Vector2(0, RingY), backgroundSize, backgroundSprite);
        }

        CreateText(root, "태양의 순환", 44, new Vector2(0, 470), new Vector2(800, 70), paper);

        // 중앙 태양판
        centerPlate = CreatePiece(root, "CenterPlate", new Vector2(0, RingY), centerPlateSize,
            centerPlateSprite, dimSun);
        if (centerPlateSprite == null)
        {
            CreateText(centerPlate.rectTransform, "중앙 태양판", 28, Vector2.zero, centerPlateSize, paper);
        }

        // 버튼 6개 고리
        for (int i = 0; i < RingButtons.Length; i++)
        {
            BuildRingButton(root, i);
        }

        // 입력 슬롯 5칸
        for (int i = 0; i < 5; i++)
        {
            Vector2 pos = new Vector2(-440f + i * 220f, -250);
            slotImages[i] = CreatePiece(root, $"Slot_{i + 1}", pos, slotSize, slotEmptySprite, sand);
            slotTexts[i] = CreateText(slotImages[i].rectTransform, (i + 1).ToString(), 24,
                Vector2.zero, slotSize, darkBrown);
        }

        BuildBackButton(root);

        statusText = CreateText(root, "", 28, new Vector2(0, -415), new Vector2(1200, 50), paper);
        CreateText(root, "버튼을 순서대로 5번 클릭  |  뒤로: 마지막 입력 취소  |  E / Esc : 닫기", 20,
            new Vector2(0, -470), new Vector2(1400, 30), sand);
    }

    private void BuildRingButton(RectTransform root, int index)
    {
        SunButton b = RingButtons[index];
        Sprite sprite = SpriteOf(b);

        Image img = CreatePiece(root, $"Btn_{b}", RingPositions[index] + new Vector2(0, RingY),
            buttonSize, sprite, dimButton);
        img.raycastTarget = true;

        Button btn = img.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => OnRingClicked(b));

        // 아트에 글자가 들어 있으면 Inspector에서 꺼둘 수 있음
        if (showButtonLabels || sprite == null)
        {
            Color labelColor = sprite != null ? paper : darkBrown;
            Vector2 labelPos = sprite != null ? new Vector2(0, -buttonSize.y * 0.62f) : Vector2.zero;
            CreateText(img.rectTransform, SunCyclePuzzle.ToKorean(b), 24, labelPos, buttonSize, labelColor);
        }

        buttonImages.Add(img);
    }

    private void BuildBackButton(RectTransform root)
    {
        Image back = CreatePiece(root, "Btn_Back", new Vector2(0, -345), new Vector2(180, 60),
            backButtonSprite, paper);
        back.raycastTarget = true;
        Button backBtn = back.gameObject.AddComponent<Button>();
        backBtn.targetGraphic = back;
        backBtn.onClick.AddListener(() => puzzle.Undo());
        CreateText(back.rectTransform, "뒤로", 26, Vector2.zero, new Vector2(180, 60), darkBrown);
    }

    private Sprite SpriteOf(SunButton b)
    {
        switch (b)
        {
            case SunButton.Rising: return risingSprite;
            case SunButton.Noon: return noonSprite;
            case SunButton.Setting: return settingSprite;
            case SunButton.Night: return nightSprite;
            case SunButton.Boat: return boatSprite;
            default: return returnSprite;
        }
    }

    // ================= 입력 / 갱신 =================
    private void OnRingClicked(SunButton b)
    {
        if (!puzzle.IsAwake)
        {
            statusText.text = "제단이 아직 잠들어 있다.";
            return;
        }
        puzzle.Press(b);
    }

    private void RefreshAll()
    {
        RefreshButtons();
        RefreshSlots();
        RefreshCenterPlate();
    }

    // 그림이 있으면 밝기만 조절, 없으면 기존처럼 색을 바꿈
    private void RefreshButtons()
    {
        bool active = puzzle.IsAwake;
        for (int i = 0; i < buttonImages.Count; i++)
        {
            bool hasSprite = buttonImages[i].sprite != null;
            if (hasSprite)
            {
                buttonImages[i].color = active ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.85f);
                continue;
            }
            buttonImages[i].color = puzzle.IsCleared ? gold : (active ? paper : dimButton);
        }
    }

    private void RefreshSlots()
    {
        var inputs = puzzle.Inputs;
        for (int i = 0; i < 5; i++)
        {
            bool filled = i < inputs.Count;
            UpdateSlotText(i, filled, inputs);
            if (flashingRed) continue;
            UpdateSlotVisual(i, filled);
        }
    }

    private void UpdateSlotText(int i, bool filled, IReadOnlyList<SunButton> inputs)
    {
        if (filled)
        {
            slotTexts[i].text = SunCyclePuzzle.ToKorean(inputs[i]);
            slotTexts[i].color = darkBrown;
            return;
        }
        if (puzzle.HintShown)
        {
            // 오답 3회 힌트: 정답 5단계 실루엣 (흐리게)
            slotTexts[i].text = SunCyclePuzzle.ToKorean(SunCyclePuzzle.AnswerAt(i));
            slotTexts[i].color = dimText;
            return;
        }
        slotTexts[i].text = (i + 1).ToString();
        slotTexts[i].color = darkBrown;
    }

    private void UpdateSlotVisual(int i, bool filled)
    {
        if (slotEmptySprite != null)
        {
            slotImages[i].sprite = (filled && slotFilledSprite != null) ? slotFilledSprite : slotEmptySprite;
            slotImages[i].color = Color.white;
            return;
        }
        slotImages[i].color = puzzle.IsCleared ? gold : (filled ? paper : sand);
    }

    private void RefreshCenterPlate()
    {
        if (centerPlateSprite != null)
        {
            bool lit = puzzle.IsCleared;
            centerPlate.sprite = (lit && centerPlateLitSprite != null) ? centerPlateLitSprite : centerPlateSprite;
            centerPlate.color = lit ? Color.white : new Color(0.6f, 0.6f, 0.6f, 1f);
            return;
        }
        centerPlate.color = puzzle.IsCleared ? gold : dimSun;
    }

    private void HandleSuccess()
    {
        statusText.text = "태양이 다시 떠오른다! 태양 형판 획득 - 북쪽 문이 열린다";
        RefreshAll();
    }

    private void HandleFail()
    {
        statusText.text = "제단이 반응하지 않는다...";
        StartCoroutine(FlashRed());
    }

    private void HandleHint()
    {
        statusText.text = "제단 벽면에 희미한 실루엣이 떠오른다...";
        RefreshAll();
    }

    // 오답: 붉은 플래시 0.3초 (기획서 10.3)
    private IEnumerator FlashRed()
    {
        flashingRed = true;
        foreach (var img in slotImages) img.color = red;
        yield return new WaitForSeconds(0.3f);
        flashingRed = false;
        RefreshAll();
    }

    // ================= 도우미 함수 =================
    // 그림이 있으면 그림으로, 없으면 색 네모로
    private Image CreatePiece(RectTransform parent, string name, Vector2 pos, Vector2 size,
        Sprite sprite, Color fallbackColor)
    {
        if (sprite != null) return CreateSprite(parent, name, pos, size, sprite);
        return CreateRect(parent, name, pos, size, fallbackColor);
    }

    private Image CreateSprite(RectTransform parent, string name, Vector2 pos, Vector2 size, Sprite sprite)
    {
        Image img = CreateRect(parent, name, pos, size, Color.white);
        img.sprite = sprite;
        img.preserveAspect = true;
        return img;
    }

    private Image CreateRect(RectTransform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
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