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

    private const float RingY = 110f;

    // 버튼 고리 배치 (기획서 그림 4-4: 방향이 의미를 암시)
    private static readonly SunButton[] RingButtons =
    {
        SunButton.Noon, SunButton.Return, SunButton.Rising,
        SunButton.Boat, SunButton.Night, SunButton.Setting
    };
    private static readonly Vector2[] RingPositions =
    {
        new Vector2(0, 240),     // (2) 정오 - 하늘, 위
        new Vector2(210, 130),   // (6) 재림 - 오른쪽 위
        new Vector2(260, -10),   // (1) 떠오르는 태양 - 동쪽, 오른쪽
        new Vector2(210, -150),  // (5) 태양의 배 - 오른쪽 아래 [함정]
        new Vector2(0, -250),    // (4) 밤 - 저승, 아래
        new Vector2(-260, -10)   // (3) 지는 태양 - 서쪽, 왼쪽
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

        CreateText(root, "태양의 순환", 44, new Vector2(0, 470), new Vector2(800, 70), paper);

        // 중앙 태양판
        centerPlate = CreateRect(root, "CenterPlate", new Vector2(0, RingY), new Vector2(230, 120), dimSun);
        CreateText(centerPlate.rectTransform, "중앙 태양판", 28, Vector2.zero, new Vector2(230, 120), paper);

        // 버튼 6개 고리
        for (int i = 0; i < RingButtons.Length; i++)
        {
            SunButton b = RingButtons[i];
            Image img = CreateRect(root, $"Btn_{b}", RingPositions[i] + new Vector2(0, RingY),
                new Vector2(190, 90), dimButton);
            img.raycastTarget = true;

            Button btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => OnRingClicked(b));

            CreateText(img.rectTransform, SunCyclePuzzle.ToKorean(b), 26,
                Vector2.zero, new Vector2(190, 90), darkBrown);
            buttonImages.Add(img);
        }

        // 입력 슬롯 5칸
        for (int i = 0; i < 5; i++)
        {
            Vector2 pos = new Vector2(-440f + i * 220f, -250);
            slotImages[i] = CreateRect(root, $"Slot_{i + 1}", pos, new Vector2(200, 80), sand);
            slotTexts[i] = CreateText(slotImages[i].rectTransform, (i + 1).ToString(), 24,
                Vector2.zero, new Vector2(200, 80), darkBrown);
        }

        // 뒤로 버튼
        Image back = CreateRect(root, "Btn_Back", new Vector2(0, -345), new Vector2(180, 60), paper);
        back.raycastTarget = true;
        Button backBtn = back.gameObject.AddComponent<Button>();
        backBtn.targetGraphic = back;
        backBtn.onClick.AddListener(() => puzzle.Undo());
        CreateText(back.rectTransform, "뒤로", 26, Vector2.zero, new Vector2(180, 60), darkBrown);

        statusText = CreateText(root, "", 28, new Vector2(0, -415), new Vector2(1200, 50), paper);
        CreateText(root, "버튼을 순서대로 5번 클릭  |  뒤로: 마지막 입력 취소  |  E / Esc : 닫기", 20,
            new Vector2(0, -470), new Vector2(1400, 30), sand);
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
        bool active = puzzle.IsAwake;
        foreach (var img in buttonImages)
        {
            img.color = puzzle.IsCleared ? gold : (active ? paper : dimButton);
        }

        var inputs = puzzle.Inputs;
        for (int i = 0; i < 5; i++)
        {
            bool filled = i < inputs.Count;

            if (filled)
            {
                slotTexts[i].text = SunCyclePuzzle.ToKorean(inputs[i]);
                slotTexts[i].color = darkBrown;
            }
            else if (puzzle.HintShown)
            {
                // 오답 3회 힌트: 정답 5단계 실루엣 (흐리게)
                slotTexts[i].text = SunCyclePuzzle.ToKorean(SunCyclePuzzle.AnswerAt(i));
                slotTexts[i].color = dimText;
            }
            else
            {
                slotTexts[i].text = (i + 1).ToString();
                slotTexts[i].color = darkBrown;
            }

            if (!flashingRed)
            {
                slotImages[i].color = puzzle.IsCleared ? gold : (filled ? paper : sand);
            }
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