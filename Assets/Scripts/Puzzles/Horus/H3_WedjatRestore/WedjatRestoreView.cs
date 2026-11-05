using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// [추가] 조각 그림 하나 + 석판 그림 위에서의 자리/크기 (조각 순서 = 퍼즐 조각 순서)
[Serializable]
public class WedjatPieceArt
{
    public Sprite sprite;
    public Vector2 slotPos;
    public Vector2 size = new Vector2(100f, 100f);
}

// H3 오버레이 화면: 슬롯 윤곽과 조각을 코드로 생성 (라의 방 EclipseStation 화면 구성 방식)
public class WedjatRestoreView : MonoBehaviour
{
    [Header("연결")]
    [SerializeField] private WedjatRestorePuzzle puzzle;
    [SerializeField] private RectTransform boardRoot;

    [Header("스냅 거리 (슬롯 긴 변 대비, 기획서 0.5)")]
    [SerializeField] private float snapRatio = 0.5f;

    [Header("그림 (석판 그림 + 조각 6개가 다 있어야 씀, 아니면 색 네모 - 예전 그대로)")]
    [Tooltip("윤곽만 보이는 석판 (H3_02)")]
    [SerializeField] private Sprite tabletSprite;
    [Tooltip("완성된 눈 석판 (H3_01, 클리어 후)")]
    [SerializeField] private Sprite tabletDoneSprite;
    [SerializeField] private Vector2 tabletCenter = new Vector2(0f, 80f);
    [SerializeField] private Vector2 tabletSize = new Vector2(720f, 540f);
    [SerializeField] private List<WedjatPieceArt> pieceArt = new List<WedjatPieceArt>();
    [Tooltip("아래 받침대에 있을 때 조각 크기 비율")]
    [SerializeField] private float trayScale = 0.45f;

    private Image[] slotImages;
    private Image[] pieceBorders;
    private Image[] pieceInners;
    private RectTransform[] pieceRects;
    private Vector2[] freePos;
    private Text titleText;
    private Text statusText;
    private Font font;
    private bool built;
    private Image tabletImage;

    private Color frameColor, slotColor, paper, borderColor, gold, red, lapis, sand;

    private void Start()
    {
        try
        {
            if (puzzle == null || boardRoot == null)
            {
                Debug.LogError("[우자트 화면] Puzzle / Board Root 연결 필요");
                return;
            }
            SetupColors();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Build();
            puzzle.OnStateChanged += Refresh;
            puzzle.OnMessage += ShowStatus;
            puzzle.OnTitleChanged += ShowTitle;
            ShowTitle(puzzle.LastTitle);
            ShowStatus(puzzle.LastMessage);
            Refresh();
        }
        catch (Exception e)
        {
            Debug.LogError($"[우자트 화면] Start 오류: {e}");
        }
    }

    private void OnDestroy()
    {
        if (puzzle == null) return;
        puzzle.OnStateChanged -= Refresh;
        puzzle.OnMessage -= ShowStatus;
        puzzle.OnTitleChanged -= ShowTitle;
    }

    private void SetupColors()
    {
        frameColor = Hex("#4A3423");
        slotColor = Hex("#0E1424");
        paper = Hex("#F6F1E7");
        borderColor = Hex("#4A3423");
        gold = Hex("#E8B23A");
        red = Hex("#C0392B");
        lapis = Hex("#1E3A5F");
        sand = Hex("#E8D9B8");
    }

    // ================= 화면 만들기 =================
    private void Build()
    {
        int count = puzzle.Pieces.Count;
        slotImages = new Image[count];
        pieceBorders = new Image[count];
        pieceInners = new Image[count];
        pieceRects = new RectTransform[count];
        freePos = new Vector2[count];

        titleText = CreateText(boardRoot, "", 40, new Vector2(0, 400), new Vector2(900, 60), gold);
        BuildFrame();
        for (int i = 0; i < count; i++)
        {
            // 그림이 있으면 석판 그림이 윤곽 역할 -> 슬롯은 투명 (자리 판정용으로만 남김)
            slotImages[i] = CreateRect(boardRoot, $"Slot_{i}", SlotPos(i), SlotSize(i), HasArt ? Color.clear : slotColor);
        }
        statusText = CreateText(boardRoot, "", 26, new Vector2(0, -360), new Vector2(1400, 50), paper);
        CreateText(boardRoot, "드래그 = 조각 옮기기  |  클릭 = 90도 회전  |  E / Esc : 닫기", 20,
            new Vector2(0, -410), new Vector2(1400, 40), sand);
        for (int i = 0; i < count; i++) CreatePiece(i, puzzle.Pieces[i]);
        built = true;
    }

    private void BuildFrame()
    {
        if (!HasArt)
        {
            CreateRect(boardRoot, "EyeFrame", new Vector2(0, 40), new Vector2(560, 400), frameColor);
            return;
        }
        tabletImage = CreateRect(boardRoot, "EyeTablet", tabletCenter, tabletSize, Color.white);
        tabletImage.sprite = tabletSprite;
        tabletImage.preserveAspect = true;
    }

    private void CreatePiece(int index, WedjatPieceDef def)
    {
        Image border = CreateRect(boardRoot, $"Piece_{index}", def.trayPos, SlotSize(index), borderColor);
        border.raycastTarget = true;
        WedjatPiece handle = border.gameObject.AddComponent<WedjatPiece>();
        handle.Init(this, index);

        RectTransform rt = border.rectTransform;
        if (HasArt) DecoratePieceArt(border, index, def);
        else
        {
            pieceInners[index] = CreateRect(rt, "Inner", Vector2.zero, def.size - new Vector2(8, 8), paper);
            CreateRect(rt, "Notch", new Vector2(0, def.size.y / 2f - 8f), new Vector2(18, 10), lapis);
            CreateText(rt, def.fraction, 18, Vector2.zero, def.size, borderColor);
        }

        pieceBorders[index] = border;
        pieceRects[index] = rt;
        freePos[index] = def.trayPos;
    }

    // 조각 그림 + 분수 글자 (그림 아래쪽에 작게)
    private void DecoratePieceArt(Image piece, int index, WedjatPieceDef def)
    {
        piece.sprite = pieceArt[index].sprite;
        piece.preserveAspect = true;
        piece.color = Color.white;
        Vector2 size = SlotSize(index);
        CreateText(piece.rectTransform, def.fraction, 22, new Vector2(0, -size.y / 2f - 14f), new Vector2(160, 30), paper);
    }

    // ================= 그림 배치 =================
    private bool HasArt => tabletSprite != null && pieceArt != null && pieceArt.Count == puzzle.Pieces.Count && AllPieceSprites();

    private bool AllPieceSprites()
    {
        foreach (WedjatPieceArt a in pieceArt) if (a == null || a.sprite == null) return false;
        return true;
    }

    private Vector2 SlotPos(int i) => HasArt ? pieceArt[i].slotPos : puzzle.Pieces[i].slotPos;

    private Vector2 SlotSize(int i) => HasArt ? pieceArt[i].size : puzzle.Pieces[i].size;

    // ================= 화면 갱신 =================
    private void Refresh()
    {
        if (!built) return;
        bool unlocked = puzzle.IsUnlocked;
        for (int i = 0; i < slotImages.Length; i++) slotImages[i].enabled = unlocked;
        RefreshTablet(unlocked);
        for (int i = 0; i < pieceRects.Length; i++) RefreshPiece(i);
    }

    // 은빛 실 전: 윤곽이 흐릿 / 후: 또렷 / 클리어: 완성된 눈 그림 + 조각 숨김
    private void RefreshTablet(bool unlocked)
    {
        if (tabletImage == null) return;
        bool done = puzzle.IsCleared && tabletDoneSprite != null;
        tabletImage.sprite = done ? tabletDoneSprite : tabletSprite;
        tabletImage.color = unlocked || done ? Color.white : new Color(1f, 1f, 1f, 0.35f);
        for (int i = 0; i < pieceRects.Length; i++) pieceRects[i].gameObject.SetActive(!done);
    }

    private void RefreshPiece(int i)
    {
        int slot = puzzle.SlotOf(i);
        pieceRects[i].anchoredPosition = slot >= 0 ? SlotPos(slot) : freePos[i];
        pieceRects[i].localRotation = Quaternion.Euler(0, 0, -90f * puzzle.RotationOf(i));
        bool correct = puzzle.IsCorrect(i);
        bool wrongHint = puzzle.HintActive && !correct;
        if (HasArt)
        {
            float s = slot >= 0 ? 1f : trayScale;
            pieceRects[i].localScale = new Vector3(s, s, 1f);
            pieceBorders[i].color = correct ? new Color(1f, 0.88f, 0.55f) : wrongHint ? new Color(1f, 0.45f, 0.45f) : Color.white;
            return;
        }
        pieceInners[i].color = correct ? gold : paper;
        pieceBorders[i].color = wrongHint ? red : borderColor;
    }

    private void ShowStatus(string text)
    {
        if (statusText != null) statusText.text = text;
    }

    private void ShowTitle(string text)
    {
        if (titleText != null) titleText.text = text;
    }

    // ================= WedjatPiece에서 호출 =================
    public bool CanDrag(int index)
    {
        return built && puzzle.CanEdit(index);
    }

    public void BeginDrag(int index)
    {
        pieceRects[index].SetAsLastSibling();
    }

    public void DragTo(int index, PointerEventData e)
    {
        if (TryLocal(e, out Vector2 p)) pieceRects[index].anchoredPosition = p;
    }

    public void Drop(int index, PointerEventData e)
    {
        try
        {
            if (!TryLocal(e, out Vector2 p))
            {
                Refresh();
                return;
            }
            int slot = NearestSlot(p);
            if (slot >= 0) PlaceInSlot(index, slot);
            else
            {
                freePos[index] = ClampToBoard(p);
                puzzle.TryPlace(index, -1);
            }
            Refresh();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[우자트 화면] Drop 오류: {ex}");
        }
    }

    public void Click(int index)
    {
        if (built) puzzle.Rotate(index);
    }

    private void PlaceInSlot(int index, int slot)
    {
        int occupant = puzzle.PieceInSlot(slot);
        bool placed = puzzle.TryPlace(index, slot);
        if (placed && occupant >= 0 && occupant != index)
        {
            freePos[occupant] = puzzle.Pieces[occupant].trayPos;
        }
    }

    private int NearestSlot(Vector2 p)
    {
        int best = -1;
        float bestDist = float.MaxValue;
        for (int i = 0; i < puzzle.Pieces.Count; i++)
        {
            Vector2 size = SlotSize(i);
            float radius = Mathf.Max(size.x, size.y) * snapRatio;
            float d = Vector2.Distance(p, SlotPos(i));
            if (d <= radius && d < bestDist)
            {
                bestDist = d;
                best = i;
            }
        }
        return best;
    }

    private bool TryLocal(PointerEventData e, out Vector2 local)
    {
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            boardRoot, e.position, e.pressEventCamera, out local);
    }

    private Vector2 ClampToBoard(Vector2 p)
    {
        Vector2 half = boardRoot.rect.size / 2f - new Vector2(80f, 80f);
        return new Vector2(Mathf.Clamp(p.x, -half.x, half.x), Mathf.Clamp(p.y, -half.y, half.y));
    }

    // ================= 도우미 =================
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
}