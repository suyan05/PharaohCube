using System.Collections.Generic;
using UnityEngine;

// 착시 퍼즐 1판의 데이터 (블록 배치 · 시작 · 목표 · 처음 카메라 각도 · 색)
// 만들기: Tools/Sobek/착시 퍼즐 1 만들기 (직접 만들려면 Create > Sobek > Illusion Level)
// 블록 좌표: x(오른쪽) y(위) z(앞), 1칸 = 1유닛. 위가 빈 블록의 윗면이 걸을 수 있는 칸
[CreateAssetMenu(fileName = "SobekIllusion_", menuName = "Sobek/Illusion Level")]
public class SobekIllusionLevel : ScriptableObject
{
    [System.Serializable]
    public struct Segment
    {
        [Tooltip("길의 첫 블록")] public Vector3Int start;
        [Tooltip("켜면 x 방향, 끄면 z 방향으로 이어짐")] public bool alongX;
        [Tooltip("0이면 기둥만")] [Range(0, 12)] public int length;
        [Tooltip("첫 블록 아래로 물 밑까지 기둥을 세움")] public bool pillar;
    }

    // 움직이는 장치 (발판을 밟을 때마다 한 단계)
    [System.Serializable]
    public struct MechDef
    {
        public SobekIllusionModel.Kind kind;
        [Tooltip("상태 0일 때 이 장치에 속한 블록 (Water는 물 위에 뜬 뗏목)")]
        public List<Vector3Int> cells;
        [Tooltip("Rotate: 이 칸의 세로줄을 축으로 90°씩 돎")]
        public Vector3Int pivot;
        [Tooltip("Slide: 상태별 이동량 (0번 = 처음 자리)")]
        public List<Vector3Int> offsets;
        [Tooltip("상태 수 (Rotate 2~4 / Slide = 이동량 개수 / Water = 물높이 단계 수)")]
        [Range(1, 4)] public int count;
        [Range(0, 3)] public int startState;
    }

    [System.Serializable]
    public struct PlateDef
    {
        [Tooltip("발판 칸 (고정 블록 윗면)")] public Vector3Int cell;
        [Tooltip("움직일 장치 번호 (mechs 목록 순서)")] public int mech;
    }

    [Header("이름 · 설명")]
    [SerializeField] private string displayName = "착시의 수로";
    [TextArea(2, 4)] [SerializeField] private string intro = "";

    [Header("블록 (길 + 기둥)")]
    [SerializeField] private List<Segment> segments = new List<Segment>();
    [Tooltip("길 말고 따로 놓는 블록")]
    [SerializeField] private List<Vector3Int> extraBlocks = new List<Vector3Int>();

    [Header("장치 · 발판")]
    [SerializeField] private List<MechDef> mechs = new List<MechDef>();
    [SerializeField] private List<PlateDef> plates = new List<PlateDef>();
    [Tooltip("기둥 아래 끝 높이")]
    [SerializeField] private int pillarBottomY = -2;

    [Header("시작 · 목표")]
    [SerializeField] private Vector3Int start;
    [SerializeField] private Vector3Int goal;
    [Tooltip("처음 카메라 각도 0=45° 1=135° 2=225° 3=315°")]
    [Range(0, 3)] [SerializeField] private int startYaw = 2;

    [Header("화면")]
    [Range(4f, 20f)] [SerializeField] private float cameraSize = 8.5f;
    [Tooltip("물 높이 (물높이 장치가 있으면 상태 0의 높이, 한 단계마다 +1)")]
    [SerializeField] private float waterY = -1.45f;

    [Header("색")]
    [SerializeField] private Color topColor = new Color(0.93f, 0.84f, 0.62f);
    [SerializeField] private Color sideColor = new Color(0.72f, 0.55f, 0.37f);
    [SerializeField] private Color mossColor = new Color(0.33f, 0.47f, 0.36f);
    [SerializeField] private Color startColor = new Color(0.52f, 0.86f, 0.62f);
    [SerializeField] private Color goalColor = new Color(1f, 0.78f, 0.25f);
    [SerializeField] private Color waterColor = new Color(0.12f, 0.36f, 0.42f);
    [SerializeField] private Color backgroundColor = new Color(0.07f, 0.12f, 0.16f);

    public string DisplayName => displayName;
    public string Intro => intro;
    public Vector3Int Start => start;
    public Vector3Int Goal => goal;
    public int StartYaw => startYaw;
    public float CameraSize => cameraSize;
    public float WaterY => waterY;
    public Color TopColor => topColor;
    public Color SideColor => sideColor;
    public Color MossColor => mossColor;
    public Color StartColor => startColor;
    public Color GoalColor => goalColor;
    public Color WaterColor => waterColor;
    public Color BackgroundColor => backgroundColor;
    public IReadOnlyList<MechDef> Mechs => mechs;
    public IReadOnlyList<PlateDef> Plates => plates;

    // 장치 색 (발판 · 움직이는 블록 테두리): 회전 청록 / 미끄럼 주황 / 물 파랑
    public static Color MechColor(SobekIllusionModel.Kind k) => k == SobekIllusionModel.Kind.Rotate ? new Color(0.3f, 0.82f, 0.8f)
        : k == SobekIllusionModel.Kind.Slide ? new Color(0.95f, 0.5f, 0.33f) : new Color(0.38f, 0.62f, 1f);
    public static string MechName(SobekIllusionModel.Kind k) => k == SobekIllusionModel.Kind.Rotate ? "도는 섬"
        : k == SobekIllusionModel.Kind.Slide ? "악어 등 다리" : "물높이";

    // 길·기둥을 블록 좌표 목록으로 펼침
    public List<Vector3Int> BuildBlocks()
    {
        var set = new HashSet<Vector3Int>();
        foreach (Segment s in segments)
        {
            Vector3Int step = s.alongX ? Vector3Int.right : new Vector3Int(0, 0, 1);
            for (int i = 0; i < s.length; i++) set.Add(s.start + step * i);
            if (s.pillar)
                for (int y = pillarBottomY; y < s.start.y; y++) set.Add(new Vector3Int(s.start.x, y, s.start.z));
        }
        foreach (Vector3Int b in extraBlocks) set.Add(b);
        return new List<Vector3Int>(set);
    }

    // 규칙 모델 (고정 블록 + 장치 + 발판)
    public SobekIllusionModel BuildModel()
    {
        var ms = new List<SobekIllusionModel.Mech>();
        foreach (MechDef d in mechs)
            ms.Add(new SobekIllusionModel.Mech
            {
                kind = d.kind,
                cells = d.cells != null ? new List<Vector3Int>(d.cells) : new List<Vector3Int>(),
                pivot = d.pivot,
                offsets = d.offsets != null ? new List<Vector3Int>(d.offsets) : new List<Vector3Int>(),
                count = d.kind == SobekIllusionModel.Kind.Slide && d.offsets != null && d.offsets.Count > 0 ? d.offsets.Count : Mathf.Max(1, d.count),
                startState = d.startState,
            });
        var ps = new List<SobekIllusionModel.Plate>();
        foreach (PlateDef p in plates) ps.Add(new SobekIllusionModel.Plate { cell = p.cell, mech = p.mech });
        return new SobekIllusionModel(BuildBlocks(), ms, ps, start, goal, waterY);
    }

    // 장치를 처음 상태로 둔 착시 길 (기즈모 · 미리보기용)
    public SobekIllusionGraph BuildGraph()
    {
        SobekIllusionModel m = BuildModel();
        return m.GraphAt(m.StartStates());
    }

    // 블록 범위의 가운데 (카메라 회전 중심, 장치가 움직일 수 있는 범위 포함)
    public Vector3 Center()
    {
        var b = new List<Vector3Int>(BuildBlocks());
        foreach (MechDef d in mechs) if (d.cells != null) b.AddRange(d.cells);
        if (b.Count == 0) return Vector3.zero;
        Vector3 min = b[0], max = b[0];
        foreach (Vector3Int c in b) { if (c.y < 0) continue; min = Vector3.Min(min, c); max = Vector3.Max(max, c); }
        min.y = Mathf.Min(min.y, 0f);
        return (min + max) * 0.5f;
    }

#if UNITY_EDITOR
    // 설계 도구가 뽑은 글(seg / mech / plate / start / goal / yaw / water 줄)을 그대로 읽어 채움
    public void EditorParse(string name, string introText, string design, float camSize)
    {
        var segs = new List<Segment>();
        var ms = new List<MechDef>();
        var ps = new List<PlateDef>();
        Vector3Int st = default, gl = default;
        int yaw = 0;
        float water = -1.45f;
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        foreach (string raw in design.Split('\n'))
        {
            string[] t = raw.Trim().Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (t.Length == 0 || t[0].StartsWith("#")) continue;
            switch (t[0])
            {
                case "seg":
                    segs.Add(new Segment { start = V(t[1], t[2], t[3]), alongX = t[4] == "1", length = int.Parse(t[5]), pillar = t[6] == "1" });
                    break;
                case "mech":
                {
                    var d = new MechDef
                    {
                        kind = (SobekIllusionModel.Kind)System.Enum.Parse(typeof(SobekIllusionModel.Kind), t[1]),
                        count = int.Parse(t[2]), startState = int.Parse(t[3]),
                        pivot = V(t[5], t[6], t[7]),
                        cells = new List<Vector3Int>(), offsets = new List<Vector3Int>(),
                    };
                    int i = System.Array.IndexOf(t, "cells") + 1, j = System.Array.IndexOf(t, "offs");
                    for (; i < j; i++) d.cells.Add(V(t[i].Split(',')));
                    for (j++; j < t.Length; j++) d.offsets.Add(V(t[j].Split(',')));
                    ms.Add(d);
                    break;
                }
                case "plate": ps.Add(new PlateDef { cell = V(t[1], t[2], t[3]), mech = int.Parse(t[4]) }); break;
                case "start": st = V(t[1], t[2], t[3]); break;
                case "goal": gl = V(t[1], t[2], t[3]); break;
                case "yaw": yaw = int.Parse(t[1]); break;
                case "water": water = float.Parse(t[1], inv); break;
            }
        }
        EditorSet(name, introText, segs, ms, ps, st, gl, yaw, camSize, water);
    }

    private static Vector3Int V(params string[] p) => new Vector3Int(int.Parse(p[0]), int.Parse(p[1]), int.Parse(p[2]));

    public void EditorSet(string name, string introText, List<Segment> segs, List<MechDef> mechDefs, List<PlateDef> plateDefs,
        Vector3Int startCell, Vector3Int goalCell, int yaw, float camSize, float water)
    {
        displayName = name; intro = introText; segments = segs; extraBlocks = new List<Vector3Int>();
        mechs = mechDefs ?? new List<MechDef>(); plates = plateDefs ?? new List<PlateDef>();
        start = startCell; goal = goalCell; startYaw = yaw; cameraSize = camSize; waterY = water; pillarBottomY = -2;
    }
#endif
}
