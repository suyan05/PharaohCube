using System;
using UnityEngine;
using UnityEngine.UI;

// OBJ_007 일식 석탁: E키로 P3 오버레이 열기/닫기 + 화면 구성
public class EclipseStation : MonoBehaviour, IInteractable
{
    [Header("연결")]
    [SerializeField] private EclipsePuzzle puzzle;
    [SerializeField] private GameObject overlayPanel;

    [Header("튜닝 (기획서 6.2)")]
    [SerializeField] private float hintDelay = 30f;        // 틀린 조각 붉은 윤곽까지 시간
    [SerializeField] private float rotateAnimTime = 0.25f; // 90° 회전 애니메이션 시간

    private const float CellSize = 150f;
    private const float Spacing = 160f;
    private const float BoardY = 40f;

    // 8칸 위치 (0=위부터 시계 방향). 가운데(0,0)는 태양
    private static readonly Vector2Int[] SlotGrid =
    {
        new Vector2Int(0, 1),  new Vector2Int(1, 1),  new Vector2Int(1, 0),  new Vector2Int(1, -1),
        new Vector2Int(0, -1), new Vector2Int(-1, -1), new Vector2Int(-1, 0), new Vector2Int(-1, 1)
    };

    private readonly Image[] borders = new Image[EclipsePuzzle.SlotCount];
    private readonly Image[] inners = new Image[EclipsePuzzle.SlotCount];
    private readonly Image[] handImages = new Image[EclipsePuzzle.SlotCount];
    private readonly RectTransform[] hands = new RectTransform[EclipsePuzzle.SlotCount];
    private readonly float[] currentZ = new float[EclipsePuzzle.SlotCount];

    private Image sunImage;
    private Text statusText;
    private Font font;

    private Color darkBrown, gold, paper, sand, lapis, nightBlue, voidColor, red, dimSun;

    private bool isOpen;
    private bool hintActive;
    private float hintTimer;
    private PlayerController2D playerController;
    private Rigidbody2D playerRb;

    private void Start()
    {
        SetupColors();
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        BuildUI();
        overlayPanel.SetActive(false);

        puzzle.OnStateChanged += RefreshColors;
        puzzle.OnPigmentApplied += HandlePigmentApplied;
        puzzle.OnSuccess += HandleSuccess;
    }

    private void OnDestroy()
    {
        if (puzzle == null) return;
        puzzle.OnStateChanged -= RefreshColors;
        puzzle.OnPigmentApplied -= HandlePigmentApplied;
        puzzle.OnSuccess -= HandleSuccess;
    }

    private void Update()
    {
        if (!isOpen) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseOverlay();
            return;
        }

        // 바늘 회전 애니메이션 (90°를 rotateAnimTime초 동안)
        float speed = 90f / rotateAnimTime;
        for (int slot = 0; slot < EclipsePuzzle.SlotCount; slot++)
        {
            currentZ[slot] = Mathf.MoveTowardsAngle(currentZ[slot], TargetZ(slot), speed * Time.deltaTime);
            hands[slot].localRotation = Quaternion.Euler(0, 0, currentZ[slot]);
        }

        // 30초 경과 시 틀린 조각 붉은 윤곽 (기획서 6.2 튜닝)
        if (!hintActive && puzzle.PigmentApplied && !puzzle.IsCleared)
        {
            hintTimer += Time.deltaTime;
            if (hintTimer >= hintDelay)
            {
                hintActive = true;
                statusText.text = "어긋난 조각의 윤곽이 붉게 빛난다...";
                RefreshColors();
            }
        }
    }

    private void SetupColors()
    {
        darkBrown = Hex("#4A3423");
        gold = Hex("#E8B23A");
        paper = Hex("#F6F1E7");
        sand = Hex("#E8D9B8");
        lapis = Hex("#1E3A5F");
        nightBlue = Hex("#3E7BD6");
        voidColor = Hex("#2A2A2A");
        red = Hex("#C0392B");
        dimSun = Hex("#8A6A3A");
    }

    // ================= IInteractable =================
    public string GetPrompt()
    {
        return isOpen ? "E: 석탁 닫기" : "E: 일식 석탁 조사";
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

        if (puzzle.IsCleared) statusText.text = "이미 복원한 원판이다.";
        else if (puzzle.PigmentApplied) statusText.text = "회전 조각을 돌려 문양을 태양에 이어보자.";
        else statusText.text = "밤 문양 조각이 빠져 있다. 무언가로 채워야 할 것 같다.";

        puzzle.Open(); // 여기서 청색 안료 자동 사용 시도 (성공하면 안내 문구가 바뀜)

        // 열 때는 애니메이션 없이 현재 방향으로 바로 맞춤
        for (int slot = 0; slot < EclipsePuzzle.SlotCount; slot++)
        {
            currentZ[slot] = TargetZ(slot);
            hands[slot].localRotation = Quaternion.Euler(0, 0, currentZ[slot]);
        }

        RefreshColors();
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

        CreateText(root, "일식 타일 복원", 44, new Vector2(0, 430), new Vector2(800, 70), paper);

        // 원판 배경
        CreateRect(root, "DiskBg", new Vector2(0, BoardY),
            new Vector2(Spacing * 3 + 20, Spacing * 3 + 20), darkBrown);

        for (int slot = 0; slot < EclipsePuzzle.SlotCount; slot++)
        {
            Vector2 pos = new Vector2(SlotGrid[slot].x * Spacing, SlotGrid[slot].y * Spacing + BoardY);

            borders[slot] = CreateRect(root, $"Border_{slot}", pos, new Vector2(CellSize, CellSize), darkBrown);
            inners[slot] = CreateRect(root, $"Cell_{slot}", pos, new Vector2(CellSize - 14, CellSize - 14), sand);

            // 회전 조각이면 클릭 가능 + A/B/C 표시
            int r = RotIndexOf(slot);
            if (r >= 0)
            {
                inners[slot].raycastTarget = true;
                Button btn = inners[slot].gameObject.AddComponent<Button>();
                btn.targetGraphic = inners[slot];
                int captured = r;
                btn.onClick.AddListener(() => OnPieceClicked(captured));

                CreateText(inners[slot].rectTransform, EclipsePuzzle.RotatingLabels[r], 26,
                    new Vector2(-46, 46), new Vector2(40, 40), darkBrown);
            }

            // 문양 선(바늘): 아래쪽을 축으로 회전
            Image hand = CreateRect(root, $"Hand_{slot}", pos, new Vector2(14, 62), gold);
            hand.rectTransform.pivot = new Vector2(0.5f, 0f);
            hand.rectTransform.anchoredPosition = pos;

            hands[slot] = hand.rectTransform;
            handImages[slot] = hand;
            currentZ[slot] = TargetZ(slot);
            hands[slot].localRotation = Quaternion.Euler(0, 0, currentZ[slot]);
        }

        // 가운데 태양
        sunImage = CreateRect(root, "Sun", new Vector2(0, BoardY), new Vector2(CellSize - 14, CellSize - 14), dimSun);
        CreateText(sunImage.rectTransform, "태양", 30, Vector2.zero, new Vector2(136, 136), paper);

        statusText = CreateText(root, "", 28, new Vector2(0, -300), new Vector2(1200, 60), paper);
        CreateText(root, "회전 조각(A·B·C) 클릭 = 90° 시계 회전  |  E / Esc : 닫기", 22,
            new Vector2(0, -360), new Vector2(1200, 40), sand);
    }

    // ================= 입력 / 화면 갱신 =================
    private void OnPieceClicked(int rotIndex)
    {
        if (!puzzle.PigmentApplied)
        {
            statusText.text = "밤 문양 조각이 빠져 있다. 무언가로 채워야 할 것 같다.";
            return;
        }
        puzzle.RotatePiece(rotIndex);
    }

    private void RefreshColors()
    {
        for (int slot = 0; slot < EclipsePuzzle.SlotCount; slot++)
        {
            int r = RotIndexOf(slot);
            bool isNight = slot == EclipsePuzzle.NightSlot;

            // 안료 적용 전: 밤 문양 칸은 비어 있음
            if (isNight && !puzzle.PigmentApplied)
            {
                inners[slot].color = voidColor;
                handImages[slot].enabled = false;
                borders[slot].color = darkBrown;
                continue;
            }

            handImages[slot].enabled = true;

            if (isNight)
            {
                inners[slot].color = lapis;
                handImages[slot].color = nightBlue;
            }
            else if (r < 0)
            {
                inners[slot].color = sand;       // 고정 조각
                handImages[slot].color = gold;
            }
            else
            {
                inners[slot].color = paper;      // 회전 조각 (조금 밝게)
                handImages[slot].color = puzzle.IsCorrect(r) ? gold : darkBrown; // 연결되면 금빛
            }

            bool showRed = hintActive && r >= 0 && !puzzle.IsCorrect(r);
            borders[slot].color = showRed ? red : darkBrown;
        }

        sunImage.color = puzzle.IsCleared ? gold : dimSun;
    }

    private void HandlePigmentApplied()
    {
        statusText.text = "청색 안료로 밤 문양을 채웠다. 원판이 움직인다!";
        RefreshColors();
    }

    private void HandleSuccess()
    {
        statusText.text = "태양 문양이 완성됐다! 거울 조각 2 + 프리즘 획득";
        RefreshColors();
    }

    // ================= 도우미 함수 =================
    private int RotIndexOf(int slot) => Array.IndexOf(EclipsePuzzle.RotatingSlots, slot);

    // 바늘 방향: 태양을 가리키는 방향에서, 어긋난 칸 수만큼 반시계로 틀어짐
    private float TargetZ(int slot)
    {
        Vector2 toCenter = -(Vector2)SlotGrid[slot];
        float angle = Mathf.Atan2(toCenter.y, toCenter.x) * Mathf.Rad2Deg;

        int r = RotIndexOf(slot);
        if (r >= 0) angle -= puzzle.GetOffset(r) * 90f;

        return angle - 90f; // 바늘 기본 방향이 위쪽이라 보정
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
/*부채꼴은 코드로 까다로워서 3x3 칸에 가운데를 뺀 8칸을 고리처럼 둠.
 아트가 나오면 원형으로 바꿀 에정.*/

/*각 회저 조각의 상태를 누적 회전수 0~3으로 저장하고, 클릭할 때 마다
 (값+1)%4로 계산. 세 조각이 정답 값과 모두 같음녀 즉시 성공, 시작 값은 무작위지만
정답과 다른 값만 나오게 셋팅*/