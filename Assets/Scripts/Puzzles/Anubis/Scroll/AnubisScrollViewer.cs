using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 양피지 보기 창 (UI는 실행 시 코드로 생성, Managers에 부착)
// - 조각을 주우면 자동으로 열림 (주운 조각만 밝게)
// - 모두 모으면 푸는 방법 글이 나타남
// - H: 마지막 양피지 다시 보기 / Esc 또는 H: 닫기

public class AnubisScrollViewer : MonoBehaviour
{
    [Header("연결")]
    [Tooltip("비우면 같은 오브젝트 또는 씬에서 자동으로 찾음")]
    [SerializeField] private AnubisScrollCollection collection;
    [Tooltip("한글 TMP 폰트 (MonoplexKR-Regular SDF)")]
    [SerializeField] private TMP_FontAsset font;

    [Header("동작")]
    [Tooltip("조각만 주웠을 때 자동으로 닫히는 시간 (초). 완성되면 직접 닫을 때까지 유지")]
    [Range(1f, 10f)]
    [SerializeField] private float autoCloseDelay = 3f;

    private const int MaxPieces = 8;
    private static readonly Vector2 ParchmentSize = new Vector2(520, 720);

    // 그림판 스케치 기준 5조각 배치 (x, y = 중심 / 양피지 520 x 720 기준)
    private static readonly Rect[] FiveLayout =
    {
        new Rect(-140,  240, 240, 240), // 1 왼쪽 위
        new Rect( 140,  250, 240, 220), // 2 오른쪽 위
        new Rect(   0,   20, 520, 200), // 3 가운데 띠
        new Rect(-140, -220, 240, 280), // 4 왼쪽 아래
        new Rect( 140, -220, 240, 280), // 5 오른쪽 아래
    };

    private static readonly Color PieceColor = new Color(0.90f, 0.80f, 0.58f, 1f);
    private static readonly Color MissingColor = new Color(0f, 0f, 0f, 0.35f);
    private static readonly Color InkColor = new Color(0.25f, 0.17f, 0.08f, 1f);

    private GameObject root;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI countText;
    private GameObject fullTextBox;
    private TextMeshProUGUI fullText;
    private Image fullImage;
    private readonly Image[] pieceImages = new Image[MaxPieces];
    private readonly TextMeshProUGUI[] pieceLabels = new TextMeshProUGUI[MaxPieces];

    private AnubisScrollData current;
    private Coroutine autoCloseRoutine;

    private void Awake()
    {
        if (collection == null) collection = GetComponent<AnubisScrollCollection>();
        if (collection == null) collection = FindFirstObjectByType<AnubisScrollCollection>();
        BuildUI();
    }

    private void OnEnable()
    {
        if (collection == null) return;
        collection.OnPieceCollected += HandlePieceCollected;
        collection.OnScrollCompleted += HandleCompleted;
    }

    private void OnDisable()
    {
        if (collection == null) return;
        collection.OnPieceCollected -= HandlePieceCollected;
        collection.OnScrollCompleted -= HandleCompleted;
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.hKey.wasPressedThisFrame)
        {
            if (root.activeSelf) Close();
            else if (collection != null && collection.LastScroll != null) Show(collection.LastScroll, false);
            else Debug.Log("[Scroll] 아직 주운 양피지가 없습니다.");
        }
        else if (root.activeSelf && kb.escapeKey.wasPressedThisFrame)
        {
            Close();
        }
    }

    private void HandlePieceCollected(AnubisScrollData scroll, int index, int count) => Show(scroll, true);
    private void HandleCompleted(AnubisScrollData scroll) => Show(scroll, false);

    public void Show(AnubisScrollData scroll, bool allowAutoClose)
    {
        if (scroll == null || collection == null) return;
        current = scroll;
        root.SetActive(true);
        Refresh();

        if (autoCloseRoutine != null)
        {
            StopCoroutine(autoCloseRoutine);
            autoCloseRoutine = null;
        }
        if (allowAutoClose && !collection.IsComplete(scroll))
            autoCloseRoutine = StartCoroutine(CloseAfter(autoCloseDelay));
    }

    public void Close()
    {
        root.SetActive(false);
        if (autoCloseRoutine != null)
        {
            StopCoroutine(autoCloseRoutine);
            autoCloseRoutine = null;
        }
    }

    private IEnumerator CloseAfter(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        autoCloseRoutine = null;
        root.SetActive(false);
    }

    private void Refresh()
    {
        int count = Mathf.Min(current.PieceCount, MaxPieces);
        int got = collection.GetCount(current);
        bool complete = collection.IsComplete(current);

        titleText.text = current.Title;
        countText.text = complete
            ? "양피지 완성!   [H] 다시 보기 · [Esc] 닫기"
            : $"조각 {got} / {current.PieceCount}   [H] 다시 보기 · [Esc] 닫기";

        for (int i = 0; i < MaxPieces; i++)
        {
            bool used = i < count;
            pieceImages[i].gameObject.SetActive(used);
            if (!used) continue;

            Rect r = (count == 5) ? FiveLayout[i] : StripLayout(i, count);
            var rt = pieceImages[i].rectTransform;
            rt.anchoredPosition = new Vector2(r.x, r.y);
            rt.sizeDelta = new Vector2(r.width, r.height);

            bool has = collection.Has(current, i);
            Sprite sprite = current.GetPieceSprite(i);
            pieceImages[i].sprite = sprite;
            pieceImages[i].preserveAspect = sprite != null;
            pieceImages[i].color = has ? (sprite != null ? Color.white : PieceColor) : MissingColor;
            pieceLabels[i].text = has ? (sprite != null ? "" : $"{i + 1}") : "?";
        }

        fullTextBox.SetActive(complete);
        if (complete) fullText.text = current.FullText;

        bool showFullImage = complete && current.FullSprite != null;
        fullImage.gameObject.SetActive(showFullImage);
        if (showFullImage) fullImage.sprite = current.FullSprite;
    }

    private static Rect StripLayout(int i, int count)
    {
        float h = ParchmentSize.y / count;
        float y = ParchmentSize.y * 0.5f - h * (i + 0.5f);
        return new Rect(0, y, ParchmentSize.x, h - 6);
    }

    private void BuildUI()
    {
        var canvasGo = new GameObject("ScrollViewer_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.layer = 5;
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60; // 퍼즐 패널(50) 위

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // 어두운 배경 (전체 화면)
        var dim = NewImage("Root", canvasGo.transform, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.6f), true);
        var dimRt = dim.rectTransform;
        dimRt.anchorMin = Vector2.zero;
        dimRt.anchorMax = Vector2.one;
        dimRt.offsetMin = Vector2.zero;
        dimRt.offsetMax = Vector2.zero;
        root = dim.gameObject;

        titleText = NewText("Title", root.transform, new Vector2(0, 425), new Vector2(900, 60), 40, new Color(0.91f, 0.70f, 0.23f), TextAlignmentOptions.Center);

        // 양피지 틀
        var frame = NewImage("Frame", root.transform, new Vector2(0, -5), ParchmentSize + new Vector2(30, 30), new Color(0.30f, 0.22f, 0.12f, 1f), false);
        var area = NewImage("Area", frame.transform, Vector2.zero, ParchmentSize, new Color(0.12f, 0.09f, 0.06f, 1f), false);

        // 조각 자리 (최대 8개)
        for (int i = 0; i < MaxPieces; i++)
        {
            pieceImages[i] = NewImage($"Piece{i + 1}", area.transform, Vector2.zero, new Vector2(100, 100), MissingColor, false);
            pieceLabels[i] = NewText("Label", pieceImages[i].transform, Vector2.zero, new Vector2(100, 60), 40, InkColor, TextAlignmentOptions.Center);
            var lrt = pieceLabels[i].rectTransform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
        }

        // 완성 그림
        fullImage = NewImage("FullImage", area.transform, Vector2.zero, ParchmentSize, Color.white, false);
        fullImage.preserveAspect = true;
        fullImage.gameObject.SetActive(false);

        // 완성 글
        var textBg = NewImage("FullTextBox", area.transform, Vector2.zero, ParchmentSize - new Vector2(20, 20), new Color(0.93f, 0.86f, 0.68f, 0.93f), false);
        fullTextBox = textBg.gameObject;
        fullText = NewText("Text", textBg.transform, Vector2.zero, ParchmentSize - new Vector2(70, 70), 26, InkColor, TextAlignmentOptions.TopLeft);
        fullText.enableAutoSizing = true;
        fullText.fontSizeMin = 16;
        fullText.fontSizeMax = 26;
        fullTextBox.SetActive(false);

        countText = NewText("Count", root.transform, new Vector2(0, -420), new Vector2(900, 40), 24, new Color(0.96f, 0.93f, 0.86f), TextAlignmentOptions.Center);

        // 닫기 버튼
        var closeImg = NewImage("Btn_Close", root.transform, new Vector2(380, 425), new Vector2(150, 54), new Color(0.35f, 0.25f, 0.15f, 1f), true);
        var closeBtn = closeImg.gameObject.AddComponent<Button>();
        closeBtn.targetGraphic = closeImg;
        closeBtn.onClick.AddListener(Close);
        NewText("Label", closeImg.transform, Vector2.zero, new Vector2(140, 50), 22, new Color(0.96f, 0.93f, 0.86f), TextAlignmentOptions.Center).text = "닫기";

        root.SetActive(false);
    }

    private Image NewImage(string n, Transform parent, Vector2 pos, Vector2 size, Color color, bool raycast)
    {
        var go = new GameObject(n, typeof(RectTransform), typeof(Image));
        go.layer = 5;
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = raycast;
        return img;
    }

    private TextMeshProUGUI NewText(string n, Transform parent, Vector2 pos, Vector2 size, float fontSize, Color color, TextAlignmentOptions align)
    {
        var go = new GameObject(n, typeof(RectTransform));
        go.layer = 5;
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.fontSize = fontSize;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
        return t;
    }
}