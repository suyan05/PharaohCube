using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// H3 오버레이 화면: 슬롯 윤곽과 조각을 코드로 생성 (라의 방 EclipseStation 화면 구성 방식)
public class WedjatRestoreView : MonoBehaviour
{
    [Header("연결")]
    [SerializeField] private WedjatRestorePuzzle puzzle;
    [SerializeField] private RectTransform boardRoot;

    [Header("스냅 거리 (슬롯 긴 변 대비, 기획서 0.5)")]
    [SerializeField] private float snapRatio = 0.5f;

    private Image[] slotImages;
    private Image[] pieceBorders;
    private Image[] pieceInners;
    private RectTransform[] pieceRects;
    private Vector2[] freePos;
    private Text titleText;
    private Text statusText;
    private Font font;
    private bool built;

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
        CreateRect(boardRoot, "EyeFrame", new Vector2(0, 40), new Vector2(560, 400), frameColor);
        for (int i = 0; i < count; i++)
        {
            WedjatPieceDef def = puzzle.Pieces[i];
            slotImages[i] = CreateRect(boardRoot, $"Slot_{i}", def.slotPos, def.size, slotColor);
        }
        statusText = CreateText(boardRoot, "", 26, new Vector2(0, -360), new Vector2(1400, 50), paper);
        CreateText(boardRoot, "드래그 = 조각 옮기기  |  클릭 = 90도 회전  |  E / Esc : 닫기", 20,
            new Vector2(0, -410), new Vector2(1400, 40), sand);
        for (int i = 0; i < count; i++) CreatePiece(i, puzzle.Pieces[i]);
        built = true;
    }

    private void CreatePiece(int index, WedjatPieceDef def)
    {
        Image border = CreateRect(boardRoot, $"Piece_{index}", def.trayPos, def.size, borderColor);
        border.raycastTarget = true;
        WedjatPiece handle = border.gameObject.AddComponent<WedjatPiece>();
        handle.Init(this, index);

        RectTransform rt = border.rectTransform;
        pieceInners[index] = CreateRect(rt, "Inner", Vector2.zero, def.size - new Vector2(8, 8), paper);
        CreateRect(rt, "Notch", new Vector2(0, def.size.y / 2f - 8f), new Vector2(18, 10), lapis);
        CreateText(rt, def.fraction, 18, Vector2.zero, def.size, borderColor);

        pieceBorders[index] = border;
        pieceRects[index] = rt;
        freePos[index] = def.trayPos;
    }

    // ================= 화면 갱신 =================
    private void Refresh()
    {
        if (!built) return;
        bool unlocked = puzzle.IsUnlocked;
        for (int i = 0; i < slotImages.Length; i++) slotImages[i].enabled = unlocked;
        for (int i = 0; i < pieceRects.Length; i++) RefreshPiece(i);
    }

    private void RefreshPiece(int i)
    {
        int slot = puzzle.SlotOf(i);
        pieceRects[i].anchoredPosition = slot >= 0 ? puzzle.Pieces[slot].slotPos : freePos[i];
        pieceRects[i].localRotation = Quaternion.Euler(0, 0, -90f * puzzle.RotationOf(i));
        bool correct = puzzle.IsCorrect(i);
        pieceInners[i].color = correct ? gold : paper;
        pieceBorders[i].color = (puzzle.HintActive && !correct) ? red : borderColor;
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
            WedjatPieceDef def = puzzle.Pieces[i];
            float radius = Mathf.Max(def.size.x, def.size.y) * snapRatio;
            float d = Vector2.Distance(p, def.slotPos);
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