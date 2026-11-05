using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 소베크 판 그리기 (SobekBoard가 부름)
// - 칸마다 바닥 그림 1장 + 필요한 칸만 발판/목표 그림 + 돌 악어알
// - 모든 그림은 "Visual" 자식 아래 -> 거울 발판은 Visual의 좌우 크기만 뒤집으면 됨
// - 상태가 바뀔 때만 Refresh (매 프레임 갱신 없음)
[DisallowMultipleComponent]
public class SobekBoardView : MonoBehaviour
{
    [Header("그림 묶음 (비우면 임시 도형)")]
    [SerializeField] private SobekTheme theme;
    [Tooltip("발판 글자(회·올·물·거)와 다리 글자 표시용 폰트 (비우면 글자 없음)")]
    [SerializeField] private TMP_FontAsset labelFont;
    [Range(1f, 6f)] [SerializeField] private float labelSize = 3f;

    [Header("발판 색 (기획서 색)")]
    [SerializeField] private Color rotateColor = new Color(0.50f, 0.47f, 0.87f); // 보라 #7F77DD
    [SerializeField] private Color liftColor = new Color(0.85f, 0.35f, 0.19f);   // 주황 #D85A30
    [SerializeField] private Color waterColor = new Color(0.22f, 0.54f, 0.87f);  // 파랑 #378ADD
    [SerializeField] private Color mirrorColor = new Color(0.11f, 0.62f, 0.46f); // 초록 #1D9E75

    private Transform visual;
    private SpriteRenderer[,] ground;
    private readonly Dictionary<SobekCell, SpriteRenderer> overlay = new Dictionary<SobekCell, SpriteRenderer>();
    private readonly List<SpriteRenderer> eggs = new List<SpriteRenderer>();
    private SobekBoardLogic logic;
    private SobekBoardData data;
    private int width, height;

    public Transform Visual => visual;

    // ================= 만들기 =================
    public void Build(SobekBoardLogic boardLogic, SobekBoardData boardData)
    {
        logic = boardLogic;
        data = boardData;
        width = logic.Width;
        height = logic.Height;

        Transform old = transform.Find("Visual");
        if (old != null) Destroy(old.gameObject);
        visual = new GameObject("Visual").transform;
        visual.SetParent(transform, false);
        visual.localPosition = new Vector3(width * 0.5f, height * 0.5f, 0f); // 가운데 기준으로 뒤집기

        ground = new SpriteRenderer[width, height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                ground[x, y] = MakeRenderer($"Cell_{x}_{y}", new SobekCell(x, y), 0);

        overlay.Clear();
        foreach (var p in logic.Spec.Plates)
        {
            SpriteRenderer sr = MakeRenderer($"Plate_{p.Cell.X}_{p.Cell.Y}", p.Cell, 1);
            sr.sprite = PlateSprite(p.Kind);
            overlay[p.Cell] = sr;
            MakeLabel(p.Cell, PlateLabel(p));
        }
        foreach (var b in logic.Spec.Bridges) MakeLabel(b.Cell, b.Id, 0.6f);

        SpriteRenderer goalSr = MakeRenderer("Goal", logic.Spec.Goal, 1);
        goalSr.sprite = GoalSprite();
        goalSr.color = data != null && data.Goal == SobekBoardData.GoalKind.Gate ? new Color(0.6f, 0.5f, 0.9f) : new Color(1f, 0.82f, 0.35f);

        eggs.Clear();
        for (int i = 0; i < logic.Eggs.Count; i++)
        {
            SpriteRenderer e = MakeRenderer($"Egg_{i}", logic.Eggs[i], 3);
            e.sprite = Pick(theme != null ? theme.egg : null, SobekPlaceholderSprites.Shape.Circle);
            e.color = new Color(0.96f, 0.94f, 0.88f);
            eggs.Add(e);
        }
        Refresh();
    }

    private SpriteRenderer MakeRenderer(string objName, SobekCell cell, int orderOffset)
    {
        var go = new GameObject(objName);
        go.transform.SetParent(visual, false);
        go.transform.localPosition = CellToVisual(cell);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = theme != null ? theme.sortingLayer : "Ground";
        sr.sortingOrder = (theme != null ? theme.baseOrder : -20) + orderOffset;
        return sr;
    }

    private void MakeLabel(SobekCell cell, string text, float yOffset = 0f)
    {
        if (labelFont == null || string.IsNullOrEmpty(text)) return;
        var go = new GameObject("Label_" + text);
        go.transform.SetParent(visual, false);
        go.transform.localPosition = CellToVisual(cell) + new Vector3(0f, yOffset * 0.5f, 0f);
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.font = labelFont;
        tmp.text = text;
        tmp.fontSize = labelSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.rectTransform.sizeDelta = new Vector2(1f, 1f);
        tmp.sortingLayerID = SortingLayer.NameToID(theme != null ? theme.sortingLayer : "Ground");
        tmp.sortingOrder = (theme != null ? theme.baseOrder : -20) + 2;
    }

    // 실제 칸 -> Visual 안의 위치 (Visual이 판 가운데에 있음)
    public Vector3 CellToVisual(SobekCell c) => new Vector3(c.X + 0.5f - width * 0.5f, (height - 1 - c.Y) + 0.5f - height * 0.5f, 0f);

    // ================= 갱신 =================
    public void Refresh()
    {
        if (logic == null || ground == null) return;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var c = new SobekCell(x, y);
                SpriteRenderer sr = ground[x, y];
                sr.sprite = GroundSprite(c, out Color col);
                sr.color = col;
            }
        }

        // 승강 발판: 누가 올라가 있으면 밝게
        foreach (var p in logic.Spec.Plates)
        {
            if (!overlay.TryGetValue(p.Cell, out SpriteRenderer sr)) continue;
            Color baseCol = PlateColor(p.Kind);
            bool pressed = p.Kind == SobekPlateKind.Lift && (logic.PlayerReal == p.Cell || logic.EggIndexAt(p.Cell) >= 0);
            float k = pressed ? (theme != null ? theme.pressedBrightness : 1.35f) : 1f;
            sr.color = new Color(Mathf.Min(1f, baseCol.r * k), Mathf.Min(1f, baseCol.g * k), Mathf.Min(1f, baseCol.b * k), 1f);
            // 물에 잠긴 발판은 흐리게
            if (logic.IsSubmerged(p.Cell)) sr.color *= new Color(1f, 1f, 1f, 0.45f);
        }

        for (int i = 0; i < eggs.Count && i < logic.Eggs.Count; i++)
        {
            eggs[i].transform.localPosition = CellToVisual(logic.Eggs[i]);
            eggs[i].enabled = true;
            eggs[i].color = new Color(0.96f, 0.94f, 0.88f, 1f);
        }

        SetMirrorInstant(logic.Mirrored);
    }

    private Sprite GroundSprite(SobekCell c, out Color col)
    {
        col = Color.white;
        if (logic.IslandAt(c) >= 0) return Pick(theme != null ? theme.island : null, SobekPlaceholderSprites.Shape.Square, ref col, new Color(0.80f, 0.65f, 0.40f));

        char t = logic.TerrainAt(c);
        if (t == '#') return Pick(theme != null ? theme.wall : null, SobekPlaceholderSprites.Shape.Square, ref col, new Color(0.45f, 0.42f, 0.38f));

        if (logic.TryGetBridge(c, out string id))
        {
            bool up = logic.IsBridgeRaised(id);
            return up ? Pick(theme != null ? theme.bridgeUp : null, SobekPlaceholderSprites.Shape.Square, ref col, new Color(0.44f, 0.62f, 0.27f))
                      : Pick(theme != null ? theme.bridgeDown : null, SobekPlaceholderSprites.Shape.Frame, ref col, new Color(0.33f, 0.48f, 0.22f));
        }

        if (t == '~') return WaterOrPit(ref col);

        Color waterTint = theme != null ? theme.submergedTint : new Color(0.55f, 0.72f, 0.88f);
        if (t == 's')
        {
            Sprite s = Pick(theme != null ? theme.stone : null, SobekPlaceholderSprites.Shape.Square, ref col, new Color(0.62f, 0.58f, 0.50f));
            if (logic.IsSubmerged(c)) col = Color.Lerp(col, waterTint, 0.75f);
            return s;
        }
        if (t == 'F')
        {
            Sprite s = Pick(theme != null ? theme.raft : null, SobekPlaceholderSprites.Shape.Square, ref col, new Color(0.55f, 0.42f, 0.24f));
            float lift = logic.TopHeight > 0 ? (float)logic.Level / logic.TopHeight : 1f;
            col = Color.Lerp(col * 0.8f, col, lift); // 물이 높을수록 밝게 (떠오름)
            col.a = 1f;
            return s;
        }

        Sprite sprite;
        if (t == 'a') sprite = Pick(theme != null ? theme.stairLow : null, SobekPlaceholderSprites.Shape.Frame, ref col, new Color(0.70f, 0.55f, 0.32f));
        else if (t == 'b') sprite = Pick(theme != null ? theme.stairHigh : null, SobekPlaceholderSprites.Shape.Frame, ref col, new Color(0.80f, 0.64f, 0.38f));
        else
        {
            Color top = theme != null ? theme.floorTop : new Color(0.92f, 0.84f, 0.66f);
            Color low = theme != null ? theme.floorLow : new Color(0.78f, 0.66f, 0.45f);
            int h = Mathf.Max(0, logic.HeightOf(t));
            float k = logic.TopHeight > 0 ? (float)h / logic.TopHeight : 1f;
            sprite = theme != null && theme.floor != null ? theme.floor : SobekPlaceholderSprites.Get(SobekPlaceholderSprites.Shape.Square);
            col = Color.Lerp(low, top, k);
        }
        if (logic.IsSubmerged(c)) col = Color.Lerp(col, waterTint, 0.75f);
        return sprite;
    }

    private Sprite WaterOrPit(ref Color col)
    {
        if (data != null && data.DryPits)
            return Pick(theme != null ? theme.pit : null, SobekPlaceholderSprites.Shape.Square, ref col, new Color(0.20f, 0.15f, 0.10f));
        return Pick(theme != null ? theme.water : null, SobekPlaceholderSprites.Shape.Square, ref col,
            theme != null ? theme.waterColor : new Color(0.45f, 0.65f, 0.80f));
    }

    // 테마 그림이 있으면 그대로(흰색), 없으면 임시 도형 + 색
    private static Sprite Pick(Sprite themed, SobekPlaceholderSprites.Shape shape, ref Color col, Color fallbackColor)
    {
        if (themed != null) { col = Color.white; return themed; }
        col = fallbackColor;
        return SobekPlaceholderSprites.Get(shape);
    }

    private static Sprite Pick(Sprite themed, SobekPlaceholderSprites.Shape shape)
    {
        return themed != null ? themed : SobekPlaceholderSprites.Get(shape);
    }

    private Sprite PlateSprite(SobekPlateKind k)
    {
        Sprite s = null;
        if (theme != null)
        {
            switch (k)
            {
                case SobekPlateKind.Rotate: s = theme.plateRotate; break;
                case SobekPlateKind.Lift: s = theme.plateLift; break;
                case SobekPlateKind.Water: s = theme.plateWater; break;
                case SobekPlateKind.Mirror: s = theme.plateMirror; break;
            }
        }
        return s != null ? s : SobekPlaceholderSprites.Get(SobekPlaceholderSprites.Shape.Circle);
    }

    private Color PlateColor(SobekPlateKind k)
    {
        switch (k)
        {
            case SobekPlateKind.Rotate: return rotateColor;
            case SobekPlateKind.Lift: return liftColor;
            case SobekPlateKind.Water: return waterColor;
            default: return mirrorColor;
        }
    }

    private static string PlateLabel(SobekBoardSpec.Plate p)
    {
        switch (p.Kind)
        {
            case SobekPlateKind.Rotate: return "회";
            case SobekPlateKind.Lift: return string.IsNullOrEmpty(p.BridgeId) ? "올" : p.BridgeId;
            case SobekPlateKind.Water: return "물";
            default: return "거";
        }
    }

    private Sprite GoalSprite()
    {
        Sprite s = null;
        if (theme != null && data != null)
        {
            switch (data.Goal)
            {
                case SobekBoardData.GoalKind.Gate: s = theme.gate; break;
                case SobekBoardData.GoalKind.Altar: s = theme.altar; break;
                default: s = theme.goal; break;
            }
        }
        return s != null ? s : SobekPlaceholderSprites.Get(SobekPlaceholderSprites.Shape.Diamond);
    }

    // ================= 연출 =================
    public Transform EggTransform(int i) => i >= 0 && i < eggs.Count ? eggs[i].transform : null;

    public IEnumerator MoveEgg(int i, SobekCell from, SobekCell to, float time)
    {
        Transform t = EggTransform(i);
        if (t == null) yield break;
        Vector3 a = CellToVisual(from), b = CellToVisual(to);
        for (float k = 0f; k < 1f; k += Time.deltaTime / Mathf.Max(0.01f, time))
        {
            t.localPosition = Vector3.Lerp(a, b, k);
            yield return null;
        }
        t.localPosition = b;
    }

    // 알이 물에 빠짐: 칸으로 밀려가며 작아지고 사라짐
    public IEnumerator SinkEgg(int i, SobekCell to, float time)
    {
        Transform t = EggTransform(i);
        if (t == null) yield break;
        SpriteRenderer sr = eggs[i];
        Vector3 a = t.localPosition, b = CellToVisual(to);
        for (float k = 0f; k < 1f; k += Time.deltaTime / Mathf.Max(0.01f, time))
        {
            t.localPosition = Vector3.Lerp(a, b, Mathf.Min(1f, k * 2f));
            t.localScale = Vector3.one * Mathf.Lerp(1f, 0.3f, k);
            Color c = sr.color; c.a = 1f - k; sr.color = c;
            yield return null;
        }
        t.localScale = Vector3.one;
        sr.enabled = false;
    }

    public IEnumerator Flip(bool mirrored, float time)
    {
        if (visual == null) yield break;
        float from = visual.localScale.x, to = mirrored ? -1f : 1f;
        for (float k = 0f; k < 1f; k += Time.deltaTime / Mathf.Max(0.01f, time))
        {
            float s = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, k));
            visual.localScale = new Vector3(s, 1f, 1f);
            yield return null;
        }
        visual.localScale = new Vector3(to, 1f, 1f);
    }

    private void SetMirrorInstant(bool mirrored)
    {
        if (visual != null) visual.localScale = new Vector3(mirrored ? -1f : 1f, 1f, 1f);
    }
}
