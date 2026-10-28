using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// OBJ_010 태양 제단: E키로 P5 오버레이 열기/닫기 + 버튼 고리 화면
// - 밤: 배경, 버튼 6개, 가운데를 밤 그림으로 교체 (아트 요청서 F)
// - 밤 클리어: 잠깐 보여준 뒤 화면 닫힘 -> 클리어 컷(RaNightFinale) -> 카메라 흔들림 + 알림
//   (태양 형판은 여기서 안 줌 -> 라의 성소 마무리 대사 후 RaShrine이 지급)
public class SunCycleStation : MonoBehaviour, IInteractable
{
    private const string MessageTag = "[Door] ";

    [Header("연결")]
    [SerializeField] private SunCyclePuzzle puzzle;
    [SerializeField] private GameObject overlayPanel;

    [Header("아트 - 고리 버튼 (비우면 기존 색 네모 + 글자)")]
    [SerializeField] private Sprite risingSprite;   // (1) 떠오르는 태양
    [SerializeField] private Sprite noonSprite;     // (2) 정오
    [SerializeField] private Sprite settingSprite;  // (3) 지는 태양
    [SerializeField] private Sprite nightSprite;    // (4) 밤 [낮 함정]
    [SerializeField] private Sprite boatSprite;     // (5) 태양의 배
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

    [Header("밤 아트 (비우면 낮 그림 그대로)")]
    [SerializeField] private Sprite nightBackgroundSprite;   // P5_Altar_Night_BG
    [Tooltip("밤 배경 크기. 버튼이 고리 동그라미에 안 맞으면 여기로 맞추기")]
    [SerializeField] private Vector2 nightBackgroundSize = new Vector2(1000, 1000);
    [SerializeField] private Sprite nightRisingSprite;
    [SerializeField] private Sprite nightNoonSprite;
    [SerializeField] private Sprite nightSettingSprite;
    [SerializeField] private Sprite nightNightSprite;
    [SerializeField] private Sprite nightBoatSprite;         // P5_Btn_Boat_Glow
    [SerializeField] private Sprite nightReturnSprite;
    [Tooltip("밤 태양의 배 버튼 크기 배율 (요청서: 낮 버튼의 1.3배)")]
    [SerializeField] private float boatGlowScale = 1.3f;
    [Tooltip("밤 가운데 그림 (비우면 가운데 태양판을 숨김 -> 배경의 빈 원이 보임)")]
    [SerializeField] private Sprite centerPlateNightSprite;

    [Header("밤 클리어 - 마무리")]
    [Tooltip("정답을 맞힌 뒤 제단 화면을 보여주는 시간 (초)")]
    [SerializeField] private float clearHoldTime = 1.5f;
    [Tooltip("비우면 컷 없이 바로 흔들림 + 알림")]
    [SerializeField] private RaNightFinale finale;
    [SerializeField] private float shakeDuration = 1f;
    [SerializeField] private float shakeStrength = 0.2f;
    [SerializeField, TextArea] private string finishMessage = "신전 전체가 울린다. 라의 성소로 돌아가자.";

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
    // [수정] 원판 그림(Solar panel charging, 600 크기)의 구멍 가운데에 딱 맞춤
    private static readonly Vector2[] RingPositions =
    {
        new Vector2(0, 238),      // (2) 정오 - 하늘, 12시
        new Vector2(208, 116),    // (6) 재림 - 2시
        new Vector2(208, -120),   // (1) 떠오르는 태양 - 4시
        new Vector2(0, -242),     // (5) 태양의 배 - 6시
        new Vector2(-206, -120),  // (4) 밤 - 8시 [낮 함정]
        new Vector2(-206, 116)    // (3) 지는 태양 - 10시
    };

    // [추가] 밤 원판 그림(P5_Altar_Night_BG, 620 크기)의 구멍 자리 (낮 그림과 조금 다름)
    private static readonly Vector2[] RingPositionsNight =
    {
        new Vector2(0, 238),
        new Vector2(200, 118),
        new Vector2(200, -113),
        new Vector2(0, -232),
        new Vector2(-202, -113),
        new Vector2(-204, 118)
    };

    private readonly List<Image> buttonImages = new List<Image>();
    private readonly Image[] slotImages = new Image[5];
    private readonly Text[] slotTexts = new Text[5];
    private readonly Image[] slotIcons = new Image[5];    // [추가] 고른 버튼 그림
    private readonly Text[] slotLabels = new Text[5];     // [추가] 고른 버튼 이름 (슬롯 아래)
    private readonly List<Text> orderBadges = new List<Text>(); // [추가] 고리 버튼에 몇 번째로 골랐는지
    private Image backgroundImage;
    private Image centerPlate;
    private Text statusText;
    private Font font;

    private Color darkBrown, gold, paper, sand, red, dimButton, dimText, dimSun;

    private bool isOpen;
    private bool flashingRed;
    private bool playingFinale;
    private PlayerController2D playerController;
    private Rigidbody2D playerRb;

    private void Start()
    {
        try
        {
            SetupColors();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            BuildUI();
            overlayPanel.SetActive(false);

            puzzle.OnInputChanged += RefreshAll;
            puzzle.OnHint += HandleHint;
            puzzle.OnSuccess += HandleSuccess;
            puzzle.OnFail += HandleFail;
            puzzle.OnPhaseChanged += HandlePhaseChanged;
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 제단 초기화 오류: {e}");
        }
    }

    private void SetupColors()
    {
        darkBrown = Hex("#4A3423");
        gold = Hex("#E8B23A");
        paper = Hex("#F6F1E7");
        sand = Hex("#E8D9B8");
        red = Hex("#C0392B");
        dimButton = Hex("#6B5438");
        dimText = Hex("#A89373");
        dimSun = Hex("#8A6A3A");
    }

    private void OnDestroy()
    {
        if (puzzle == null) return;
        puzzle.OnInputChanged -= RefreshAll;
        puzzle.OnHint -= HandleHint;
        puzzle.OnSuccess -= HandleSuccess;
        puzzle.OnFail -= HandleFail;
        puzzle.OnPhaseChanged -= HandlePhaseChanged;
    }

    private void Update()
    {
        if (isOpen && !playingFinale && Input.GetKeyDown(KeyCode.Escape)) CloseOverlay();
    }

    // ================= IInteractable =================
    public string GetPrompt() => isOpen ? "E: 제단 닫기" : "E: 태양 제단 조사";

    public void Interact(GameObject player)
    {
        if (playingFinale) return; // 클리어 연출 중에는 E 무시

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
        statusText.text = GetOpenMessage();

        ApplyPhaseArt();
        RefreshAll();
    }

    private string GetOpenMessage()
    {
        if (puzzle.IsCleared) return "태양의 순환이 완성되었다.";
        if (!puzzle.IsAwake) return "제단이 아직 잠들어 있다.";
        return puzzle.IsNight ? "밤의 항해를 제단에 새겨라." : "태양의 하루를 제단에 새겨라.";
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

        // 배경 (낮/밤 그림 중 하나라도 있으면 만들어 둠)
        if (backgroundSprite != null || nightBackgroundSprite != null)
        {
            backgroundImage = CreateSprite(root, "Background", new Vector2(0, RingY), backgroundSize, backgroundSprite);
        }

        CreateText(root, "태양의 순환", 44, new Vector2(0, 470), new Vector2(800, 70), paper);

        BuildCenter(root);

        // 버튼 6개 고리
        for (int i = 0; i < RingButtons.Length; i++)
        {
            BuildRingButton(root, i);
        }

        BuildSlots(root);
        BuildBackButton(root);

        statusText = CreateText(root, "", 28, new Vector2(0, -415), new Vector2(1200, 50), paper);
        CreateText(root, "버튼을 순서대로 5번 클릭  |  뒤로: 마지막 입력 취소  |  E / Esc : 닫기", 20,
            new Vector2(0, -470), new Vector2(1400, 30), sand);
    }

    // 중앙 태양판
    private void BuildCenter(RectTransform root)
    {
        centerPlate = CreatePiece(root, "CenterPlate", new Vector2(0, RingY), centerPlateSize,
            centerPlateSprite, dimSun);
        if (centerPlateSprite == null)
        {
            CreateText(centerPlate.rectTransform, "중앙 태양판", 28, Vector2.zero, centerPlateSize, paper);
        }
    }

    private void BuildSlots(RectTransform root)
    {
        for (int i = 0; i < 5; i++)
        {
            Vector2 pos = new Vector2(-440f + i * 220f, -250);
            slotImages[i] = CreatePiece(root, $"Slot_{i + 1}", pos, slotSize, slotEmptySprite, sand);
            slotTexts[i] = CreateText(slotImages[i].rectTransform, (i + 1).ToString(), 24,
                Vector2.zero, slotSize, darkBrown);

            // [추가] 고른 버튼 그림이 슬롯 안에, 이름은 슬롯 아래에
            slotIcons[i] = CreateRect(slotImages[i].rectTransform, "Icon", Vector2.zero, Vector2.one * (slotSize.y * 1.05f), Color.white);
            slotIcons[i].preserveAspect = true;
            slotIcons[i].enabled = false;
            slotLabels[i] = CreateText(slotImages[i].rectTransform, "", 20, new Vector2(0, -slotSize.y * 0.5f - 16f), new Vector2(slotSize.x + 20f, 28f), paper);
        }
    }

    private void BuildRingButton(RectTransform root, int index)
    {
        SunButton b = RingButtons[index];
        Sprite sprite = SpriteOf(b, false);

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

        // [추가] 몇 번째로 골랐는지 동그라미 숫자 (오른쪽 위)
        Image badge = CreateRect(img.rectTransform, "OrderBadge", new Vector2(buttonSize.x * 0.42f, buttonSize.y * 0.42f), new Vector2(36, 36), gold);
        Text badgeText = CreateText(badge.rectTransform, "", 22, Vector2.zero, new Vector2(36, 36), darkBrown);
        badge.sprite = UIBuild.Circle();
        badge.enabled = false;
        orderBadges.Add(badgeText);
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

    // 밤이고 밤 그림이 있으면 밤 그림, 아니면 낮 그림
    private Sprite SpriteOf(SunButton b, bool night)
    {
        Sprite nightPick = night ? NightSpriteOf(b) : null;
        if (nightPick != null) return nightPick;

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

    private Sprite NightSpriteOf(SunButton b)
    {
        switch (b)
        {
            case SunButton.Rising: return nightRisingSprite;
            case SunButton.Noon: return nightNoonSprite;
            case SunButton.Setting: return nightSettingSprite;
            case SunButton.Night: return nightNightSprite;
            case SunButton.Boat: return nightBoatSprite;
            default: return nightReturnSprite;
        }
    }

    // ================= 낮 / 밤 그림 교체 =================
    private void ApplyPhaseArt()
    {
        bool night = puzzle.IsNight;
        ApplyBackground(night);

        Vector2[] ring = night && nightBackgroundSprite != null ? RingPositionsNight : RingPositions;
        for (int i = 0; i < buttonImages.Count; i++)
        {
            buttonImages[i].rectTransform.anchoredPosition = ring[i] + new Vector2(0, RingY); // [추가] 구멍 자리
            SunButton b = RingButtons[i];
            Sprite s = SpriteOf(b, night);
            if (s == null) continue;

            buttonImages[i].sprite = s;
            buttonImages[i].preserveAspect = true;

            bool glow = night && b == SunButton.Boat && nightBoatSprite != null;
            buttonImages[i].rectTransform.sizeDelta = glow ? buttonSize * boatGlowScale : buttonSize;
        }
    }

    private void ApplyBackground(bool night)
    {
        if (backgroundImage == null) return;

        bool useNight = night && nightBackgroundSprite != null;
        backgroundImage.sprite = useNight ? nightBackgroundSprite : backgroundSprite;
        backgroundImage.rectTransform.sizeDelta = useNight ? nightBackgroundSize : backgroundSize;
        backgroundImage.enabled = backgroundImage.sprite != null;
    }

    private void HandlePhaseChanged()
    {
        ApplyPhaseArt();
        RefreshAll();
        if (isOpen) statusText.text = GetOpenMessage();
    }

    // ================= 입력 / 갱신 =================
    private void OnRingClicked(SunButton b)
    {
        if (playingFinale) return;

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
            int order = PressedOrder(RingButtons[i]);
            UpdateBadge(i, order);
            if (hasSprite)
            {
                // [추가] 이미 고른 버튼은 어둡게 (몇 번째인지는 동그라미 숫자로)
                Color c = active ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.85f);
                buttonImages[i].color = order > 0 && !puzzle.IsCleared ? new Color(0.42f, 0.38f, 0.34f, 1f) : c;
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

    // [추가] 고리 버튼이 몇 번째로 눌렸는지 (안 눌렸으면 0)
    private int PressedOrder(SunButton b)
    {
        var inputs = puzzle.Inputs;
        for (int k = 0; k < inputs.Count; k++)
        {
            if (inputs[k] == b) return k + 1;
        }
        return 0;
    }

    private void UpdateBadge(int i, int order)
    {
        if (i >= orderBadges.Count) return;
        Text t = orderBadges[i];
        t.text = order > 0 ? order.ToString() : "";
        var badge = t.transform.parent.GetComponent<Image>();
        if (badge != null) badge.enabled = order > 0;
    }

    private void UpdateSlotText(int i, bool filled, IReadOnlyList<SunButton> inputs)
    {
        // [추가] 고른 버튼 그림 + 이름
        Sprite icon = filled ? SpriteOf(inputs[i], puzzle.IsNight) : null;
        slotIcons[i].sprite = icon;
        slotIcons[i].enabled = icon != null;
        slotLabels[i].text = filled ? SunCyclePuzzle.ToKorean(inputs[i]) : "";

        if (filled)
        {
            slotTexts[i].text = icon != null ? "" : SunCyclePuzzle.ToKorean(inputs[i]);
            slotTexts[i].color = darkBrown;
            return;
        }
        if (puzzle.HintShown)
        {
            // 오답 3회 힌트 (낮): 정답 5단계 실루엣 (흐리게)
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
        if (puzzle.IsNight)
        {
            RefreshNightCenter();
            return;
        }

        centerPlate.enabled = true;
        if (centerPlateSprite != null)
        {
            bool lit = puzzle.IsCleared;
            centerPlate.sprite = (lit && centerPlateLitSprite != null) ? centerPlateLitSprite : centerPlateSprite;
            centerPlate.color = lit ? Color.white : new Color(0.6f, 0.6f, 0.6f, 1f);
            return;
        }
        centerPlate.color = puzzle.IsCleared ? gold : dimSun;
    }

    // 밤: 가운데는 밤 그림 (없으면 숨김 -> 배경의 빈 원이 보임)
    private void RefreshNightCenter()
    {
        centerPlate.enabled = centerPlateNightSprite != null;
        if (centerPlateNightSprite == null) return;

        centerPlate.sprite = centerPlateNightSprite;
        centerPlate.color = Color.white;
    }

    // ================= 결과 =================
    // 낮: 성소로 안내 / 밤: 잠깐 보여주고 -> 클리어 컷 -> 흔들림 (형판은 성소에서)
    private void HandleSuccess()
    {
        if (!puzzle.IsNight)
        {
            statusText.text = "태양의 하루가 제단에 새겨졌다. 입구의 라의 성소가 빛난다.";
            RefreshAll();
            return;
        }

        statusText.text = "태양의 배가 저승의 강을 건넜다!";
        RefreshAll();
        if (isActiveAndEnabled) StartCoroutine(NightClearSequence());
    }

    private IEnumerator NightClearSequence()
    {
        playingFinale = true;
        yield return new WaitForSeconds(clearHoldTime);

        CloseOverlay();

        bool done = finale == null;
        if (!done) finale.Play(() => done = true);
        while (!done) yield return null;

        FinishWithShake();
        playingFinale = false;
    }

    private void FinishWithShake()
    {
        Shake(shakeDuration, shakeStrength);
        if (!string.IsNullOrEmpty(finishMessage)) Debug.Log(MessageTag + finishMessage);
    }

    private static void Shake(float duration, float strength)
    {
        Camera main = Camera.main;
        if (main == null) return;

        CameraFollow2D follow = main.GetComponent<CameraFollow2D>();
        if (follow != null) follow.Shake(duration, strength);
        UnlockRumble.Play(); // [추가] 쿠르릉
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