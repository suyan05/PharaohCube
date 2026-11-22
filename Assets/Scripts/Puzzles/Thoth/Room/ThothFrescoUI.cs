using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 벽화 되살리기 (Fresco 같은 직소 퍼즐) - 토트의 방 핵심 놀이
// - 왼쪽: 부서진 벽화 판 (조각 자리 금만 보임) / 오른쪽: 가진 조각 보관함 (작게)
// - 끌어서 옮기기, 오른쪽 클릭 = 조각 뒤집기(180도). 똑바로 된 조각이 제자리 40px 안이면 착 붙음
// - 판 위에 놓으면 그 자리에 남고, 판 밖에 놓으면 보관함으로 돌아감
// - 9조각 모두 맞추면 완성 -> 닫히고 onSolved (벽화 연출은 ThothFrescoWall이 틂)
// - 맞춘 조각은 플래그로 기억 (나갔다 와도 그대로)
public class ThothFrescoUI : ThothPanel
{
    private const float SnapDistance = 40f;
    private const float TrayScale = 0.55f;
    private static readonly Vector2 BoardPos = new Vector2(-250f, 30f);
    private static readonly Vector2 TrayPos = new Vector2(610f, 30f);
    private static ThothFrescoUI instance;

    private RectTransform board, pieceRoot;
    private Image boardImg;
    private Text title, count;
    private string fresco;
    private Action onSolved;
    private bool solving;
    private readonly List<ThothFrescoPiece> pieces = new List<ThothFrescoPiece>();

    protected override bool CanClose => !solving;

    public static void Open(string fresco, Action onSolved)
    {
        if (instance == null) instance = FindFirstObjectByType<ThothFrescoUI>();
        if (instance == null) instance = new GameObject("ThothFrescoUI").AddComponent<ThothFrescoUI>();
        instance.fresco = fresco;
        instance.onSolved = onSolved;
        instance.OpenPanel();
    }

    protected override void Build()
    {
        title = ThothUI.Label(root, "Title", font, 46, new Vector2(0f, 470f), new Vector2(1600f, 70f), ThothUI.Gold, "");
        UIBuild.Image(root, "BoardFrame", BoardPos, new Vector2(ThothFrescoData.BoardW + 24f, ThothFrescoData.BoardH + 24f), null, ThothUI.Gold);
        boardImg = ThothUI.Pic(root, "Board", "", BoardPos, new Vector2(ThothFrescoData.BoardW, ThothFrescoData.BoardH));
        board = boardImg.rectTransform;
        UIBuild.Image(root, "Tray", TrayPos, new Vector2(560f, 900f), null, new Color(0.08f, 0.1f, 0.12f, 0.85f));
        count = ThothUI.Label(root, "Count", font, 30, new Vector2(-250f, -285f), new Vector2(960f, 50f), Color.white, "");
        ThothUI.Label(root, "Help", font, 28, new Vector2(0f, -470f), new Vector2(1800f, 50f), new Color(1f, 1f, 1f, 0.8f),
            "끌어서 옮기기   /   오른쪽 클릭: 조각 뒤집기   /   E: 닫기");
        pieceRoot = UIBuild.Stretch(root, "Pieces");
    }

    protected override void OnOpened()
    {
        solving = false;
        title.text = $"벽화 '{ThothFrescoData.Title(fresco)}' 되살리기";
        boardImg.sprite = ThothState.Art($"Fresco/{ThothFrescoData.Art(fresco)}_board");
        boardImg.color = boardImg.sprite != null ? Color.white : new Color(0.2f, 0.15f, 0.1f, 1f);
        RebuildPieces();
        RefreshCount();
    }

    private void RebuildPieces()
    {
        foreach (ThothFrescoPiece p in pieces) if (p != null) Destroy(p.gameObject);
        pieces.Clear();
        int slot = 0;
        for (int i = 0; i < ThothFrescoData.PieceCount; i++)
        {
            if (!ThothState.OwnsPiece(fresco, i)) continue;
            bool placed = ThothState.Has(ThothIds.Placed(fresco, i));
            pieces.Add(CreatePiece(i, placed ? -1 : slot));
            if (!placed) slot++;
        }
    }

    private ThothFrescoPiece CreatePiece(int index, int slot)
    {
        Vector4 d = ThothFrescoData.Piece(fresco, index);
        Image img = ThothUI.Pic(pieceRoot, "Piece" + index, $"Fresco/{ThothFrescoData.Art(fresco)}_p{index}", Vector2.zero, new Vector2(d.z, d.w));
        img.raycastTarget = true;
        ThothFrescoPiece p = img.gameObject.AddComponent<ThothFrescoPiece>();
        p.Setup(this, index, BoardPos + new Vector2(d.x, d.y), TraySlot(slot));
        if (slot < 0) p.Lock();
        else p.SetRotated(index % 3 == 1); // 몇 조각은 뒤집힌 채로 시작
        return p;
    }

    // 보관함 칸: 2열 x 5줄
    private static Vector2 TraySlot(int slot)
    {
        if (slot < 0) return Vector2.zero;
        int col = slot % 2, row = slot / 2;
        return TrayPos + new Vector2(col == 0 ? -135f : 135f, 340f - row * 165f);
    }

    public float CanvasScale
    {
        get
        {
            Canvas c = root != null ? root.GetComponent<Canvas>() : null;
            return c != null && c.scaleFactor > 0f ? c.scaleFactor : 1f;
        }
    }

    public float TrayScaleValue => TrayScale;
    public bool Solving => solving;

    // 조각을 놓았을 때: 제자리면 붙이고, 판 위면 그대로, 판 밖이면 보관함으로
    public void Drop(ThothFrescoPiece p)
    {
        try
        {
            if (!p.Rotated && Vector2.Distance(p.Pos, p.Target) <= SnapDistance)
            {
                p.Lock();
                ThothState.Set(ThothIds.Placed(fresco, p.Index));
                GameAudio.Play("sfx_th_snap", 0.9f, UnityEngine.Random.Range(0.95f, 1.08f));
                RefreshCount();
                CheckSolved();
                return;
            }
            if (OnBoard(p.Pos)) p.StayLoose();
            else p.ReturnHome();
        }
        catch (Exception e)
        {
            Debug.LogError($"[벽화 맞추기] 조각 놓기 오류: {e}");
        }
    }

    private static bool OnBoard(Vector2 pos)
    {
        Vector2 d = pos - BoardPos;
        return Mathf.Abs(d.x) < ThothFrescoData.BoardW * 0.5f && Mathf.Abs(d.y) < ThothFrescoData.BoardH * 0.5f;
    }

    private int PlacedCount()
    {
        int n = 0;
        for (int i = 0; i < ThothFrescoData.PieceCount; i++) if (ThothState.Has(ThothIds.Placed(fresco, i))) n++;
        return n;
    }

    private void RefreshCount()
    {
        int owned = ThothState.OwnedCount(fresco), placed = PlacedCount();
        count.text = owned < ThothFrescoData.PieceCount
            ? $"맞춘 조각 {placed}/9   (가진 조각 {owned}/9 - 나머지는 서고 어딘가에)"
            : $"맞춘 조각 {placed}/9";
    }

    private void CheckSolved()
    {
        if (solving || PlacedCount() < ThothFrescoData.PieceCount) return;
        StartCoroutine(Solve());
    }

    private IEnumerator Solve()
    {
        solving = true;
        PuzzleSolvedFx.Play();
        GameAudio.Play("sfx_th_chime", 0.8f);
        yield return new WaitForSecondsRealtime(PuzzleSolvedFx.HoldSeconds);
        solving = false;
        ClosePanel();
        Action done = onSolved;
        onSolved = null;
        ThothUI.Safe(done, "벽화 완성");
    }
}

// 벽화 조각 하나 (끌기, 오른쪽 클릭 뒤집기)
public class ThothFrescoPiece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    private ThothFrescoUI panel;
    private RectTransform rt;
    private Vector2 home;
    private bool locked, rotated;

    public int Index { get; private set; }
    public Vector2 Target { get; private set; }
    public Vector2 Pos => rt.anchoredPosition;
    public bool Rotated => rotated;

    public void Setup(ThothFrescoUI owner, int index, Vector2 target, Vector2 homePos)
    {
        panel = owner;
        rt = (RectTransform)transform;
        Index = index;
        Target = target;
        home = homePos;
        ReturnHome();
    }

    public void SetRotated(bool on)
    {
        rotated = on;
        rt.localEulerAngles = new Vector3(0f, 0f, on ? 180f : 0f);
    }

    public void Lock()
    {
        locked = true;
        SetRotated(false);
        rt.anchoredPosition = Target;
        rt.localScale = Vector3.one;
        rt.SetAsFirstSibling(); // 맞춘 조각은 아래로 (끄는 조각이 위로 보이게)
        GetComponent<Image>().raycastTarget = false;
    }

    public void ReturnHome()
    {
        rt.anchoredPosition = home;
        rt.localScale = Vector3.one * panel.TrayScaleValue;
    }

    public void StayLoose() => rt.localScale = Vector3.one;

    public void OnBeginDrag(PointerEventData e)
    {
        if (locked || panel.Solving || e.button != PointerEventData.InputButton.Left) return;
        rt.localScale = Vector3.one;
        rt.SetAsLastSibling();
        GameAudio.Play("sfx_th_paper", 0.35f, 1.2f);
    }

    public void OnDrag(PointerEventData e)
    {
        if (locked || panel.Solving || e.button != PointerEventData.InputButton.Left) return;
        rt.anchoredPosition += e.delta / panel.CanvasScale;
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (locked || panel.Solving || e.button != PointerEventData.InputButton.Left) return;
        panel.Drop(this);
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (locked || panel.Solving || e.button != PointerEventData.InputButton.Right) return;
        SetRotated(!rotated);
        GameAudio.Play("sfx_th_paper", 0.4f, 0.9f);
        if (!rotated) panel.Drop(this); // 똑바로 돌렸을 때 제자리 근처면 바로 붙음
    }
}
