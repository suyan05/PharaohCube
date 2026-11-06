using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 소베크 판 그리기 (SobekBoard가 부름)
// - 칸마다 바닥 그림 1장 + 발판/목표 그림 + 돌 악어알
// - 모든 판 그림은 "Visual" 자식 아래 -> 거울 발판은 Visual의 좌우 크기만 뒤집으면 됨
// - 상태가 바뀔 때만 Refresh (매 프레임 갱신 없음)
// v2 연출: 섬이 실제로 90° 도는 애니메이션 / 물이 아래→위로 차오르고 위→아래로 빠지는 물결 + 수위 눈금
//          거울 축선 + 거울 발판 옆에 서면 "뒤집힌 세계의 벽" 미리보기 / 발판 글자 대신 그림 아이콘
[DisallowMultipleComponent]
public class SobekBoardView : MonoBehaviour
{
    [Header("그림 묶음 (비우면 임시 도형)")]
    [SerializeField] private SobekTheme theme;
    [Tooltip("다리·승강 발판 글자(X, Y…) 표시용 폰트 (비우면 글자 없음)")]
    [SerializeField] private TMP_FontAsset labelFont;
    [Range(1f, 8f)] [SerializeField] private float labelSize = 4f;

    [Header("발판 색 (기획서 색)")]
    [SerializeField] private Color rotateColor = new Color(0.50f, 0.47f, 0.87f); // 보라 #7F77DD
    [SerializeField] private Color liftColor = new Color(0.85f, 0.35f, 0.19f);   // 주황 #D85A30
    [SerializeField] private Color waterPlateColor = new Color(0.22f, 0.54f, 0.87f); // 파랑 #378ADD
    [SerializeField] private Color mirrorColor = new Color(0.11f, 0.62f, 0.46f); // 초록 #1D9E75

    [Header("물 연출")]
    [Tooltip("판 전체에 덮는 물빛 (수위가 높을수록 진해짐)")]
    [Range(0f, 0.5f)] [SerializeField] private float waterSheetMaxAlpha = 0.22f;
    [SerializeField] private Color gaugeOn = new Color(0.25f, 0.6f, 0.95f);
    [SerializeField] private Color gaugeOff = new Color(0.15f, 0.13f, 0.11f);

    [Header("거울 연출")]
    [SerializeField] private Color mirrorAxisColor = new Color(0.4f, 0.95f, 0.85f, 0.55f);
    [SerializeField] private Color ghostColor = new Color(0.35f, 0.95f, 0.95f, 0.5f);

    private Transform visual;
    private Transform ghostRoot;
    private SpriteRenderer[,] ground;
    private readonly Dictionary<SobekCell, SpriteRenderer> overlay = new Dictionary<SobekCell, SpriteRenderer>();
    private readonly List<SpriteRenderer> eggs = new List<SpriteRenderer>();
    private readonly List<SpriteRenderer> gauge = new List<SpriteRenderer>();
    private SpriteRenderer waterSheet;
    private SobekBoardLogic logic;
    private SobekBoardData data;
    private int width, height;

    private string Layer => theme != null ? theme.sortingLayer : "Ground";
    private int BaseOrder => theme != null ? theme.baseOrder : -20;

    public Transform Visual => visual;
    public TMP_FontAsset LabelFont => labelFont;

    // ================= 만들기 =================
    public void Build(SobekBoardLogic boardLogic, SobekBoardData boardData)
    {
        logic = boardLogic;
        data = boardData;
        width = logic.Width;
        height = logic.Height;

        foreach (string n in new[] { "Visual", "Ghost", "Gauge" })
        {
            Transform old = transform.Find(n);
            if (old != null) Destroy(old.gameObject);
        }
        visual = new GameObject("Visual").transform;
        visual.SetParent(transform, false);
        visual.localPosition = new Vector3(width * 0.5f, height * 0.5f, 0f); // 가운데 기준으로 뒤집기

        ground = new SpriteRenderer[width, height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                ground[x, y] = MakeRenderer(visual, $"Cell_{x}_{y}", CellToVisual(new SobekCell(x, y)), 0);

        overlay.Clear();
        foreach (var p in logic.Spec.Plates)
        {
            SpriteRenderer sr = MakeRenderer(visual, $"Plate_{p.Cell.X}_{p.Cell.Y}", CellToVisual(p.Cell), 2);
            sr.sprite = PlateSprite(p.Kind);
            overlay[p.Cell] = sr;
            if (p.Kind == SobekPlateKind.Lift) MakeLabel(p.Cell, p.BridgeId, 0f);
        }
        foreach (var b in logic.Spec.Bridges) MakeLabel(b.Cell, b.Id, 0.55f);

        SpriteRenderer goalSr = MakeRenderer(visual, "Goal", CellToVisual(logic.Spec.Goal), 2);
        goalSr.sprite = GoalSprite();
        goalSr.color = data != null && data.Goal == SobekBoardData.GoalKind.Gate ? new Color(0.6f, 0.5f, 0.9f) : new Color(1f, 0.82f, 0.35f);

        eggs.Clear();
        for (int i = 0; i < logic.Eggs.Count; i++)
        {
            SpriteRenderer e = MakeRenderer(visual, $"Egg_{i}", CellToVisual(logic.Eggs[i]), 4);
            e.sprite = Pick(theme != null ? theme.egg : null, SobekPlaceholderSprites.Shape.Circle);
            e.color = EggColor;
            eggs.Add(e);
        }

        BuildWater();
        BuildMirror();
        Refresh();
    }

    private static readonly Color EggColor = new Color(0.96f, 0.94f, 0.88f);

    private SpriteRenderer MakeRenderer(Transform parent, string objName, Vector3 localPos, int orderOffset)
    {
        var go = new GameObject(objName);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = Layer;
        sr.sortingOrder = BaseOrder + orderOffset;
        return sr;
    }

    private void MakeLabel(SobekCell cell, string text, float yOffset)
    {
        if (labelFont == null || string.IsNullOrEmpty(text)) return;
        var go = new GameObject("Label_" + text);
        go.transform.SetParent(visual, false);
        go.transform.localPosition = CellToVisual(cell) + new Vector3(0f, yOffset * 0.5f, 0f);
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.font = labelFont;
        tmp.text = text;
        tmp.fontSize = labelSize;
        tmp.enableWordWrapping = false;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.outlineWidth = 0.25f;
        tmp.outlineColor = new Color32(40, 25, 10, 255);
        tmp.rectTransform.sizeDelta = new Vector2(1f, 1f);
        tmp.sortingLayerID = SortingLayer.NameToID(Layer);
        tmp.sortingOrder = BaseOrder + 3;
    }

    // 수위 눈금 (판 오른쪽) + 판 전체 물빛
    private void BuildWater()
    {
        gauge.Clear();
        waterSheet = null;
        if (logic.Spec.LevelCount <= 1) return;

        var root = new GameObject("Gauge").transform;
        root.SetParent(transform, false);
        root.localPosition = new Vector3(width + 0.7f, 0.5f, 0f);
        for (int i = 0; i < logic.Spec.LevelCount; i++)
        {
            SpriteRenderer seg = MakeRenderer(root, $"Level_{i}", new Vector3(0f, i * 0.9f, 0f), 6);
            seg.sprite = SobekPlaceholderSprites.Get(SobekPlaceholderSprites.Shape.Square);
            seg.transform.localScale = new Vector3(0.6f, 0.8f, 1f);
            gauge.Add(seg);
        }

        waterSheet = MakeRenderer(visual, "WaterSheet", Vector3.zero, 1);
        waterSheet.sprite = SobekPlaceholderSprites.Get(SobekPlaceholderSprites.Shape.Square);
        waterSheet.transform.localScale = new Vector3(width, height, 1f);
        waterSheet.color = new Color(0.3f, 0.55f, 0.9f, 0f);
    }

    // 거울 축선 + 뒤집힌 세계의 벽 미리보기
    private void BuildMirror()
    {
        ghostRoot = null;
        if (!logic.HasMirror) return;

        SpriteRenderer axis = MakeRenderer(transform, "MirrorAxis", new Vector3(width * 0.5f, height * 0.5f, 0f), 7);
        axis.transform.SetParent(visual, true);
        axis.sprite = SobekPlaceholderSprites.Get(SobekPlaceholderSprites.Shape.Square);
        axis.transform.localScale = new Vector3(0.08f, height, 1f);
        axis.color = mirrorAxisColor;

        ghostRoot = new GameObject("Ghost").transform;
        ghostRoot.SetParent(transform, false);
        ghostRoot.localPosition = visual.localPosition;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var c = new SobekCell(x, y);
                char t = logic.TerrainAt(c);
                bool block = t == '#' || (t == '~' && !logic.TryGetBridge(c, out _) && logic.IslandAt(c) < 0);
                bool isGoal = logic.IsGoal(c);
                if (!block && !isGoal) continue;
                SpriteRenderer g = MakeRenderer(ghostRoot, $"Ghost_{x}_{y}", CellToVisual(c), 8);
                g.sprite = SobekPlaceholderSprites.Get(isGoal ? SobekPlaceholderSprites.Shape.Diamond : SobekPlaceholderSprites.Shape.Frame);
                g.color = isGoal ? new Color(1f, 0.85f, 0.3f, 0.8f) : ghostColor;
            }
        }
        ghostRoot.gameObject.SetActive(false);
    }

    // 거울 발판 옆에 서면 켬: 지금 세계 위에 "뒤집으면 보일 벽"을 겹쳐 보여 줌
    public void SetGhost(bool on)
    {
        if (ghostRoot == null) return;
        ghostRoot.localScale = new Vector3(logic.Mirrored ? 1f : -1f, 1f, 1f); // 지금 세계의 반대
        if (ghostRoot.gameObject.activeSelf != on) ghostRoot.gameObject.SetActive(on);
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
                sr.sprite = GroundSprite(c, logic.Level, out Color col);
                sr.color = col;
            }
        }
        RefreshPlates(logic.Level);

        for (int i = 0; i < eggs.Count && i < logic.Eggs.Count; i++)
        {
            Transform t = eggs[i].transform;
            if (t.parent != visual) t.SetParent(visual, false);
            t.localPosition = CellToVisual(logic.Eggs[i]);
            t.localScale = Vector3.one;
            t.localRotation = Quaternion.identity;
            eggs[i].enabled = true;
            eggs[i].color = EggColor;
        }

        SetGauge(logic.Level);
        SetMirrorInstant(logic.Mirrored);
    }

    private void RefreshPlates(int level)
    {
        foreach (var p in logic.Spec.Plates)
        {
            if (!overlay.TryGetValue(p.Cell, out SpriteRenderer sr)) continue;
            Color baseCol = PlateColor(p.Kind);
            bool pressed = p.Kind == SobekPlateKind.Lift && (logic.PlayerReal == p.Cell || logic.EggIndexAt(p.Cell) >= 0);
            float k = pressed ? (theme != null ? theme.pressedBrightness : 1.35f) : 1f;
            Color col = new Color(Mathf.Min(1f, baseCol.r * k), Mathf.Min(1f, baseCol.g * k), Mathf.Min(1f, baseCol.b * k), 1f);
            if (logic.IsSubmergedAt(p.Cell, level)) col.a = 0.35f; // 물에 잠긴 발판은 흐리게
            sr.color = col;
        }
    }

    private void SetGauge(float level)
    {
        for (int i = 0; i < gauge.Count; i++)
        {
            float fill = Mathf.Clamp01(level - i + 1f); // i번 칸: 수위가 i 이상이면 켜짐 (0번은 항상)
            gauge[i].color = Color.Lerp(gaugeOff, gaugeOn, fill);
        }
        if (waterSheet != null)
        {
            float top = Mathf.Max(1, logic.Spec.LevelCount - 1);
            Color c = waterSheet.color;
            c.a = waterSheetMaxAlpha * Mathf.Clamp01(level / top);
            waterSheet.color = c;
        }
    }

    private Sprite GroundSprite(SobekCell c, int level, out Color col)
    {
        col = Color.white;
        if (logic.IslandAt(c) >= 0) return Pick(theme != null ? theme.island : null, SobekPlaceholderSprites.Shape.Square, ref col, new Color(0.80f, 0.65f, 0.40f));
        return TerrainSprite(c, level, out col);
    }

    // 섬을 뺀 바닥 그림 (섬 회전 연출 중에 섬 밑을 그릴 때도 씀)
    private Sprite TerrainSprite(SobekCell c, int level, out Color col)
    {
        col = Color.white;
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
        bool sub = logic.IsSubmergedAt(c, level);
        if (t == 's')
        {
            Sprite s = Pick(theme != null ? theme.stone : null, SobekPlaceholderSprites.Shape.Square, ref col, new Color(0.62f, 0.58f, 0.50f));
            if (sub) col = Color.Lerp(col, waterTint, 0.8f);
            return s;
        }
        if (t == 'F')
        {
            Sprite s = Pick(theme != null ? theme.raft : null, SobekPlaceholderSprites.Shape.Square, ref col, new Color(0.55f, 0.42f, 0.24f));
            float lift = logic.TopHeight > 0 ? (float)level / logic.TopHeight : 1f;
            col = Color.Lerp(col * 0.7f, col, lift); // 물이 높을수록 밝게 (떠오름)
            col.a = 1f;
            return s;
        }

        Sprite sprite;
        if (t == 'a') sprite = Pick(theme != null ? theme.stairLow : null, SobekPlaceholderSprites.Shape.Frame, ref col, new Color(0.70f, 0.55f, 0.32f));
        else if (t == 'b') sprite = Pick(theme != null ? theme.stairHigh : null, SobekPlaceholderSprites.Shape.Frame, ref col, new Color(0.80f, 0.64f, 0.38f));
        else
        {
            Color top = theme != null ? theme.floorTop : new Color(0.92f, 0.84f, 0.66f);
            Color low = theme != null ? theme.floorLow : new Color(0.62f, 0.50f, 0.32f);
            int h = Mathf.Max(0, logic.HeightOf(t));
            float k = logic.TopHeight > 0 ? (float)h / logic.TopHeight : 1f;
            sprite = theme != null && theme.floor != null ? theme.floor : SobekPlaceholderSprites.Get(SobekPlaceholderSprites.Shape.Square);
            col = Color.Lerp(low, top, k);
        }
        if (sub) col = Color.Lerp(col, waterTint, 0.8f);
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
            case SobekPlateKind.Water: return waterPlateColor;
            default: return mirrorColor;
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

    // 알이 물에 빠짐: 칸으로 밀려가며 작아지고 사라짐 (Refresh하면 제자리로 돌아옴)
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
        sr.enabled = false;
    }

    // 섬 회전: 회전 전 섬 모양을 임시 그림으로 만들어 가운데를 축으로 90° 돌림 (위의 알도 같이)
    // rotsBefore: 회전 전 섬별 회전 값, eggsBefore: 회전 전 알 위치
    public IEnumerator RotateIslands(List<SobekBoardSpec.Spin> spins, int[] rotsBefore, SobekCell[] eggsBefore, float time)
    {
        var pivots = new List<Transform>();
        var angles = new List<float>();
        var hidden = new List<SobekCell>();
        var carried = new List<int>();

        foreach (var s in spins)
        {
            var ctr = logic.Spec.Islands[s.Island].Center;
            var pivot = new GameObject("IslandSpin").transform;
            pivot.SetParent(visual, false);
            pivot.localPosition = CellToVisual(ctr);
            foreach (var c in logic.IslandCells(s.Island, rotsBefore[s.Island]))
            {
                SpriteRenderer sr = MakeRenderer(pivot, "Piece", CellToVisual(c) - CellToVisual(ctr), 1);
                sr.sprite = Pick(theme != null ? theme.island : null, SobekPlaceholderSprites.Shape.Square, ref tmpColor, new Color(0.80f, 0.65f, 0.40f));
                sr.color = tmpColor;
                hidden.Add(c);
                for (int e = 0; e < eggsBefore.Length && e < eggs.Count; e++)
                {
                    if (eggsBefore[e] != c) continue;
                    eggs[e].transform.SetParent(pivot, true);
                    carried.Add(e);
                }
            }
            // 플레이어는 가운데(발판)에 서 있으므로 가운데 칸 그림은 그대로 두고 팔만 돌아 보임
            pivots.Add(pivot);
            angles.Add(-90f * s.Dir); // 실제 좌표 시계 방향 = 화면 시계 방향 (z 음수)
        }

        // 회전 후 섬 칸 + 회전 전 섬 칸 바닥은 섬 없이 그림 (임시 그림이 그 위를 돎)
        foreach (var s in spins) hidden.AddRange(logic.IslandCells(s.Island, logic.IslandRotation(s.Island)));
        foreach (var c in hidden)
        {
            if (!logic.InBounds(c)) continue;
            ground[c.X, c.Y].sprite = TerrainSprite(c, logic.Level, out Color col);
            ground[c.X, c.Y].color = col;
        }

        for (float k = 0f; k < 1f; k += Time.deltaTime / Mathf.Max(0.01f, time))
        {
            float e = Mathf.SmoothStep(0f, 1f, k);
            for (int i = 0; i < pivots.Count; i++) pivots[i].localRotation = Quaternion.Euler(0f, 0f, angles[i] * e);
            foreach (int egg in carried) eggs[egg].transform.rotation = Quaternion.identity; // 알 그림은 똑바로
            yield return null;
        }

        foreach (int egg in carried) eggs[egg].transform.SetParent(visual, true);
        foreach (var p in pivots) Destroy(p.gameObject);
        Refresh();
    }

    private Color tmpColor;

    // 물 높이 변화: 차오르면 아래 줄부터, 빠지면 위 줄부터 물결이 지나가며 색이 바뀜 + 눈금·물빛
    public IEnumerator AnimateWater(int from, int to, float time)
    {
        var colA = new Color[width, height];
        var colB = new Color[width, height];
        var spr = new Sprite[width, height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var c = new SobekCell(x, y);
                GroundSprite(c, from, out colA[x, y]);
                spr[x, y] = GroundSprite(c, to, out colB[x, y]);
                ground[x, y].sprite = spr[x, y];
            }
        }

        bool rising = to > from;
        const float band = 3f; // 물결 두께 (줄 수)
        for (float k = 0f; k < 1f; k += Time.deltaTime / Mathf.Max(0.01f, time))
        {
            float front = k * (height + band);
            for (int y = 0; y < height; y++)
            {
                int fromBottom = height - 1 - y;
                float rowPos = rising ? fromBottom : y; // 차오름: 아래부터 / 빠짐: 위부터
                float t = Mathf.Clamp01((front - rowPos) / band);
                for (int x = 0; x < width; x++)
                {
                    Color c = Color.Lerp(colA[x, y], colB[x, y], t);
                    // 물결 앞머리를 밝게 반짝
                    float crest = 1f - Mathf.Abs(t - 0.5f) * 2f;
                    if (colA[x, y] != colB[x, y]) c = Color.Lerp(c, Color.white, crest * 0.35f);
                    ground[x, y].color = c;
                }
            }
            SetGauge(Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, k)));
            RefreshPlates(k < 0.5f ? from : to);
            yield return null;
        }
        Refresh();
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
