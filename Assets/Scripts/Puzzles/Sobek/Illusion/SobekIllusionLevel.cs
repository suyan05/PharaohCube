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
        [Range(1, 12)] public int length;
        [Tooltip("첫 블록 아래로 물(y=-1)까지 기둥을 세움")] public bool pillar;
    }

    [Header("이름 · 설명")]
    [SerializeField] private string displayName = "착시의 수로";
    [TextArea(2, 4)] [SerializeField] private string intro = "";

    [Header("블록 (길 + 기둥)")]
    [SerializeField] private List<Segment> segments = new List<Segment>();
    [Tooltip("길 말고 따로 놓는 블록")]
    [SerializeField] private List<Vector3Int> extraBlocks = new List<Vector3Int>();

    [Header("시작 · 목표")]
    [SerializeField] private Vector3Int start;
    [SerializeField] private Vector3Int goal;
    [Tooltip("처음 카메라 각도 0=45° 1=135° 2=225° 3=315°")]
    [Range(0, 3)] [SerializeField] private int startYaw = 2;

    [Header("화면")]
    [Range(4f, 20f)] [SerializeField] private float cameraSize = 8.5f;
    [Tooltip("물 높이 (블록 y=-1의 아랫면 근처)")]
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

    // 길·기둥을 블록 좌표 목록으로 펼침
    public List<Vector3Int> BuildBlocks()
    {
        var set = new HashSet<Vector3Int>();
        foreach (Segment s in segments)
        {
            Vector3Int step = s.alongX ? Vector3Int.right : new Vector3Int(0, 0, 1);
            for (int i = 0; i < Mathf.Max(1, s.length); i++) set.Add(s.start + step * i);
            if (s.pillar)
                for (int y = -1; y < s.start.y; y++) set.Add(new Vector3Int(s.start.x, y, s.start.z));
        }
        foreach (Vector3Int b in extraBlocks) set.Add(b);
        return new List<Vector3Int>(set);
    }

    public SobekIllusionGraph BuildGraph() => new SobekIllusionGraph(BuildBlocks());

    // 블록 범위의 가운데 (카메라 회전 중심)
    public Vector3 Center()
    {
        List<Vector3Int> b = BuildBlocks();
        if (b.Count == 0) return Vector3.zero;
        Vector3 min = b[0], max = b[0];
        foreach (Vector3Int c in b) { min = Vector3.Min(min, c); max = Vector3.Max(max, c); }
        return (min + max) * 0.5f;
    }

#if UNITY_EDITOR
    public void EditorSet(string name, string introText, List<Segment> segs, Vector3Int startCell, Vector3Int goalCell, int yaw, float camSize)
    {
        displayName = name; intro = introText; segments = segs; extraBlocks = new List<Vector3Int>();
        start = startCell; goal = goalCell; startYaw = yaw; cameraSize = camSize;
    }
#endif
}
