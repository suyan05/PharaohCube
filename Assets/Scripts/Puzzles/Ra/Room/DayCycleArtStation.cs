using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

// OBJ_009 카드 벤치 (아트 버전): E키로 P4 오버레이 열기/닫기 + 카드 드래그
// 판정은 DayCyclePuzzle이 그대로 담당하고, 이 스크립트는 화면만 담당
// 퍼즐 화면은 정면에서 본 반듯한 카드 틀 4칸 (벤치 그림은 맵 오브젝트용)
// 아트팀 틀 그림이 오면 Slot Frame Sprite 칸에 넣으면 코드로 그린 틀 대신 그 그림을 씀
public class DayCycleArtStation : MonoBehaviour, IInteractable
{
    [Header("연결")]
    [SerializeField] private DayCyclePuzzle puzzle;
    [SerializeField] private GameObject overlayPanel;

    [Header("스프라이트 (컴포넌트 우클릭 -> 스프라이트 자동 연결)")]
    [Tooltip("자동 연결 때 이 폴더 안에서만 찾음 (P1에도 같은 이름 파일이 있어서)")]
    [SerializeField] private string spriteFolder = "Assets/image/Ra/P4";
    [Tooltip("비워두면 코드로 어두운 갈색 + 금테 틀을 그림")]
    [SerializeField] private Sprite slotFrameSprite;
    [SerializeField] private Sprite versoSprite;
    [SerializeField] private Sprite sealLineSprite;
    [SerializeField] private Sprite sealSprite;
    [SerializeField] private Sprite dawnSprite;
    [SerializeField] private Sprite noonSprite;
    [SerializeField] private Sprite duskSprite;
    [SerializeField] private Sprite nightSprite;

    [Header("옵션")]
    [Tooltip("벽화 아트를 벽에 넣기 전까지 오버레이 위쪽에 글자 힌트 표시")]
    [SerializeField] private bool showMuralHint = true;
    [Tooltip("카드 아래 이름표(새벽/정오/황혼/밤) 표시")]
    [SerializeField] private bool showNameLabel = true;
    [Tooltip("오버레이가 열려 있는 동안 투명하게 숨길 UI (목표 문구, 노트 팝업 등)")]
    [SerializeField] private GameObject[] hideWhileOpen;

    // 카드 / 틀 크기 (카드 원본 비율 1000x1638)
    private static readonly Vector2 CardSize = new Vector2(220f, 360f);
    private static readonly Vector2 FrameSize = new Vector2(250f, 390f);
    private const float FrameBorder = 8f;
    private const float SlotSpacing = 280f;
    private const float SlotY = -40f;
    private const float SealSize = 130f;

    private static readonly TimeSymbol[] AllSymbols =
    {
        TimeSymbol.Noon, TimeSymbol.Night, TimeSymbol.Dawn, TimeSymbol.Dusk
    };

    private readonly Dictionary<TimeSymbol, DragCard> cards = new Dictionary<TimeSymbol, DragCard>();
    private readonly Dictionary<TimeSymbol, Image> cardImages = new Dictionary<TimeSymbol, Image>();
    private readonly Dictionary<CanvasGroup, float> hiddenGroups = new Dictionary<CanvasGroup, float>();
    private readonly List<Text> nameLabels = new List<Text>();
    private GameObject sealGroup;
    private Text statusText;
    private Font font;
    private Canvas canvas;

    private Color wallColor, darkBrown, slotInner, gold, paper, sand, red, clearTint;

    private bool isOpen;
    private bool dragAllowed;
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
            canvas = overlayPanel.GetComponentInParent<Canvas>(); // 드래그 이동량 보정용

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
        slotInner = Hex("#2E1C14");
        gold = Hex("#E8B23A");
        paper = Hex("#F6F1E7");
        sand = Hex("#E8D9B8");
        red = Hex("#E06050");
        clearTint = Hex("#FFF1C2");
    }

    // ================= IInteractable =================
    public string GetPrompt()
    {
        return isOpen ? "E: 벤치 닫기" : "E: 카드 벤치 조사";
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

        statusText.text = GetOpenMessage();
        puzzle.Open(); // 여기서 황동 열쇠 자동 사용 시도

        LayoutCards();
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
        if (puzzle.IsCleared) return "이미 해결한 벤치다.";
        if (puzzle.Unsealed) return "벽화를 보고 카드를 동쪽(왼쪽)부터 배열하자.";
        return "봉인줄이 감겨 있다. 열쇠가 필요할 것 같다.";
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

        CreateText(root, "동쪽에서 서쪽으로", 44, new Vector2(0, 470), new Vector2(800, 70), paper);
        if (showMuralHint) BuildMuralHint(root);

        for (int i = 0; i < puzzle.SlotCount; i++)
        {
            CreateSlotFrame(root, i);
        }
        foreach (TimeSymbol symbol in AllSymbols)
        {
            CreateCard(root, symbol);
        }
        BuildSeal(root);

        CreateButton(root, "확정", new Vector2(-115, -330), gold, OnConfirmClicked);
        CreateButton(root, "취소(초기화)", new Vector2(115, -330), paper, OnCancelClicked);

        statusText = CreateText(root, "", 28, new Vector2(0, -435), new Vector2(1200, 60), paper);
        CreateText(root, "카드를 끌어서 자리 바꾸기  |  확정: 판정  |  취소: 다시 섞기  |  E / Esc : 닫기", 20,
            new Vector2(0, -485), new Vector2(1400, 30), sand);
    }

    // 남쪽 벽화 단서 (벽에 벽화 오브젝트를 넣으면 showMuralHint 끄기)
    private void BuildMuralHint(RectTransform root)
    {
        CreateText(root, "남쪽 벽화 '하루의 순서'", 22, new Vector2(0, 415), new Vector2(600, 30), sand);
        CreateRect(root, "MuralBand", new Vector2(0, 345), new Vector2(1060, 100), wallColor);

        for (int i = 0; i < puzzle.SlotCount; i++)
        {
            Image panel = CreateRect(root, $"Mural_{i + 1}", new Vector2(-375f + i * 250f, 345),
                new Vector2(200, 80), paper);
            CreateText(panel.rectTransform, $"{i + 1}. {ShadowClockPuzzle.ToKorean(DayCyclePuzzle.AnswerAt(i))}",
                30, Vector2.zero, new Vector2(200, 80), darkBrown);
        }

        CreateText(root, "<- 동 (왼쪽)                                        서 (오른쪽) ->", 22,
            new Vector2(0, 278), new Vector2(1060, 30), sand);
    }

    // 슬롯 틀: 아트 그림이 있으면 그림, 없으면 금테 + 어두운 갈색 안쪽
    private void CreateSlotFrame(RectTransform root, int index)
    {
        Vector2 pos = SlotPos(index);

        if (slotFrameSprite != null)
        {
            Image frame = CreateImage(root, $"Slot_{index + 1}", slotFrameSprite, FrameSize, pos);
            frame.preserveAspect = true;
            return;
        }

        CreateRect(root, $"Slot_{index + 1}_Gold", pos, FrameSize, gold);
        Vector2 inner = FrameSize - Vector2.one * (FrameBorder * 2f);
        CreateRect(root, $"Slot_{index + 1}_Inner", pos, inner, slotInner);
    }

    private void CreateCard(RectTransform root, TimeSymbol symbol)
    {
        Image img = CreateImage(root, $"Card_{symbol}", versoSprite, CardSize, SlotPos(puzzle.IndexOf(symbol)));
        img.preserveAspect = true;
        img.raycastTarget = true;

        DragCard card = img.gameObject.AddComponent<DragCard>();
        card.symbol = symbol;
        card.onBeginDrag = OnCardBeginDrag;
        card.onDrag = OnCardDrag;
        card.onEndDrag = OnCardEndDrag;

        if (showNameLabel)
        {
            Text label = CreateText(img.rectTransform, ShadowClockPuzzle.ToKorean(symbol), 26,
                new Vector2(0, -CardSize.y * 0.5f - 22f), new Vector2(CardSize.x, 34), paper);
            nameLabels.Add(label);
        }

        cards[symbol] = card;
        cardImages[symbol] = img;
    }

    // 봉인줄 + 가운데 봉인 (열쇠 쓰기 전까지 카드 위에 표시)
    private void BuildSeal(RectTransform root)
    {
        sealGroup = new GameObject("SealGroup", typeof(RectTransform));
        var groupRt = sealGroup.GetComponent<RectTransform>();
        groupRt.SetParent(root, false);
        groupRt.anchoredPosition = new Vector2(0, SlotY);
        groupRt.sizeDelta = Vector2.zero;

        float lineWidth = SlotSpacing * (puzzle.SlotCount - 1) + FrameSize.x + 60f;
        float lineAspect = 213f / 3000f;
        if (sealLineSprite != null) lineAspect = sealLineSprite.rect.height / sealLineSprite.rect.width;

        CreateImage(groupRt, "SealLine", sealLineSprite, new Vector2(lineWidth, lineWidth * lineAspect), Vector2.zero);

        Image seal = CreateImage(groupRt, "Seal", sealSprite, new Vector2(SealSize, SealSize), Vector2.zero);
        seal.preserveAspect = true;
    }

    private Vector2 SlotPos(int index)
    {
        float startX = -SlotSpacing * (puzzle.SlotCount - 1) * 0.5f;
        return new Vector2(startX + index * SlotSpacing, SlotY);
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
        float scale = canvas != null ? canvas.scaleFactor : 1f;
        var rt = (RectTransform)card.transform;
        rt.anchoredPosition += delta / scale;
    }

    private void OnCardEndDrag(DragCard card)
    {
        if (!dragAllowed) return;
        dragAllowed = false;

        var rt = (RectTransform)card.transform;
        int from = puzzle.IndexOf(card.symbol);
        int nearest = FindNearestSlot(rt.anchoredPosition, out float nearestDist);

        // 0.5칸 이내면 그 자리 카드와 교환, 아니면 원위치 (기획서 6.3: 스냅 0.5 슬롯)
        if (nearestDist <= SlotSpacing * 0.5f && nearest != from)
        {
            puzzle.SwapSlots(from, nearest); // -> OnArrangementChanged -> LayoutCards
        }
        else
        {
            LayoutCards();
        }
    }

    private int FindNearestSlot(Vector2 pos, out float nearestDist)
    {
        int nearest = -1;
        nearestDist = float.MaxValue;

        for (int i = 0; i < puzzle.SlotCount; i++)
        {
            float d = Vector2.Distance(pos, SlotPos(i));
            if (d < nearestDist)
            {
                nearestDist = d;
                nearest = i;
            }
        }
        return nearest;
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
        RefreshCards();
    }

    private void RefreshCards()
    {
        foreach (TimeSymbol symbol in AllSymbols)
        {
            Image img = cardImages[symbol];
            img.sprite = puzzle.Unsealed ? GetFaceSprite(symbol) : versoSprite; // 봉인 중엔 뒷면

            if (flashingRed) continue;
            img.color = puzzle.IsCleared ? clearTint : Color.white;
        }

        // 뒷면일 땐 이름표도 숨김 (봉인 풀기 전엔 뭔지 모르게)
        foreach (Text label in nameLabels) label.enabled = puzzle.Unsealed;

        sealGroup.SetActive(!puzzle.Unsealed);
        sealGroup.transform.SetAsLastSibling(); // 봉인은 항상 카드 위
    }

    private Sprite GetFaceSprite(TimeSymbol symbol)
    {
        switch (symbol)
        {
            case TimeSymbol.Dawn: return dawnSprite;
            case TimeSymbol.Noon: return noonSprite;
            case TimeSymbol.Dusk: return duskSprite;
            case TimeSymbol.Night: return nightSprite;
            default:
                Debug.LogWarning($"[{name}] {symbol} 카드 그림이 정해지지 않음");
                return versoSprite;
        }
    }

    private void HandleUnsealed()
    {
        statusText.text = "황동 열쇠로 봉인줄을 풀었다! 카드를 움직일 수 있다.";
        RefreshCards();
    }

    private void HandleSuccess()
    {
        statusText.text = "벽화와 같은 순서다! 거울 조각 (3) 획득";
        RefreshCards();
    }

    private void HandleFail()
    {
        statusText.text = "순서가 맞지 않는다... (배치는 그대로)";
        if (isActiveAndEnabled) StartCoroutine(FlashCardsRed());
    }

    // 오답: 붉은 플래시 0.3초 (기획서 10.3)
    private IEnumerator FlashCardsRed()
    {
        flashingRed = true;
        foreach (Image img in cardImages.Values) img.color = red;

        yield return new WaitForSeconds(0.3f);

        flashingRed = false;
        RefreshCards();
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

    private Image CreateImage(RectTransform parent, string objName, Sprite sprite, Vector2 size, Vector2 pos)
    {
        Image img = CreateRect(parent, objName, pos, size, Color.white);
        img.sprite = sprite;

        if (sprite == null)
        {
            Debug.LogWarning($"[{name}] {objName} 스프라이트가 비어 있음 (우클릭 -> 스프라이트 자동 연결)");
        }
        return img;
    }

    private Image CreateRect(RectTransform parent, string objName, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(objName, typeof(RectTransform), typeof(Image));
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

    // ================= 에디터 전용: 스프라이트 자동 연결 =================
#if UNITY_EDITOR
    [ContextMenu("스프라이트 자동 연결")]
    private void AutoAssignSprites()
    {
        try
        {
            if (!AssetDatabase.IsValidFolder(spriteFolder))
            {
                Debug.LogError($"[{name}] 폴더 '{spriteFolder}'가 없음 -> Sprite Folder 칸 경로 확인 (Project 창에서 P4 폴더 우클릭 -> Copy Path)");
                return;
            }

            Undo.RecordObject(this, "P4 스프라이트 자동 연결");
            AssignAllSprites();
            EditorUtility.SetDirty(this);
            Debug.Log($"[{name}] P4 스프라이트 연결 끝! ({spriteFolder}) 빈 칸이 있으면 Console 경고 확인");
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 스프라이트 자동 연결 실패: {e}");
        }
    }

    private void AssignAllSprites()
    {
        slotFrameSprite = LoadSprite("P4_Slot_Frame", false); // 아트팀 틀 그림 (없으면 코드 틀)
        versoSprite = LoadSprite("verso", true);
        sealLineSprite = LoadSprite("seal line", true);
        sealSprite = LoadSprite("seal", true);
        dawnSprite = LoadSprite("dawn", true);
        noonSprite = LoadSprite("noon", true);
        duskSprite = LoadSprite("dusk", true);
        nightSprite = LoadSprite("night", true);
    }

    // spriteFolder 안에서만, 파일 이름이 정확히 같은 텍스처를 찾음 ("noon"과 "noon 1"은 구분됨)
    private Sprite LoadSprite(string fileName, bool required)
    {
        string[] folders = { spriteFolder };
        foreach (string guid in AssetDatabase.FindAssets($"{fileName} t:Texture2D", folders))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) != fileName) continue;

            Sprite s = PickBestSprite(path, fileName);
            if (s != null) return s;
        }

        if (required)
        {
            Debug.LogWarning($"[{name}] '{spriteFolder}' 안에서 '{fileName}' 스프라이트를 못 찾음 -> 파일 이름이랑 Texture Type(Sprite) 확인");
        }
        return null;
    }

    // Multiple로 잘려 있어도: 이름이 같은 조각 -> 없으면 제일 큰 조각
    private static Sprite PickBestSprite(string path, string fileName)
    {
        Sprite biggest = null;
        float biggestArea = 0f;

        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (!(asset is Sprite s)) continue;
            if (s.name == fileName) return s;

            float area = s.rect.width * s.rect.height;
            if (area > biggestArea)
            {
                biggestArea = area;
                biggest = s;
            }
        }
        return biggest;
    }
#endif
}