using System;
using System.Collections.Generic;
using UnityEngine;

// 소베크 칸 퍼즐 한 판의 데이터 (방마다 에셋 1개)
// 만들기: Project 우클릭 -> Create -> PharaohCube -> Sobek -> Board Data (보통은 Tools/Sobek 메뉴가 자동으로 만듦)
// 좌표 규칙: x = 왼쪽부터 칸 번호, y = 위쪽부터 줄 번호 (지형 글자와 같은 방향, 0부터)
[CreateAssetMenu(fileName = "SobekBoard_", menuName = "PharaohCube/Sobek/Board Data")]
public class SobekBoardData : ScriptableObject
{
    public enum GoalKind { Goal, Gate, Altar }

    [Serializable]
    public class IslandDef
    {
        [Tooltip("섬 가운데 칸 (회전 발판이 보통 여기)")]
        public Vector2Int center;
        [Tooltip("방향별 팔 길이 (0 = 팔 없음, 최대 3칸)")]
        [Range(0, 3)] public int north, east, south, west;
    }

    [Serializable]
    public class SpinDef
    {
        [Tooltip("돌릴 섬 번호 (Islands 목록 순서, 0부터)")]
        public int island;
        [Tooltip("켜면 시계 방향, 끄면 반시계 방향")]
        public bool clockwise = true;
    }

    [Serializable]
    public class PlateDef
    {
        public Vector2Int cell;
        public SobekPlateKind kind;
        [Tooltip("승강 발판: 올릴 다리 글자 (Bridges의 id와 같게)")]
        public string bridgeId = "";
        [Tooltip("회전 발판: 밟을 때 돌릴 섬 목록")]
        public List<SpinDef> spins = new List<SpinDef>();
    }

    [Serializable]
    public class BridgeDef
    {
        public Vector2Int cell;
        [Tooltip("같은 글자의 승강 발판이 눌려 있는 동안만 떠 있음")]
        public string id = "X";
    }

    [Header("이름 / 안내")]
    [SerializeField] private string displayName = "소베크의 방";
    [TextArea(1, 3)]
    [Tooltip("들어갈 때 아래 알림으로 보여 줄 한 줄 (규칙 힌트)")]
    [SerializeField] private string intro = "";

    [Header("지형 (한 줄 = 한 행, 위에서 아래로)")]
    [TextArea(5, 15)]
    [Tooltip("# 벽  . 땅  0~2 높이 있는 땅  ~ 물(건기는 구덩이)  s 디딤돌  a 계단0-1  b 계단1-2  F 뗏목")]
    [SerializeField] private string map = "";
    [Tooltip("켜면 물(~)을 마른 구덩이 그림으로 그림 (메인 B 건기)")]
    [SerializeField] private bool dryPits;

    [Header("시작 / 목표")]
    [Tooltip("퍼즐에 들어오면 서는 칸")]
    [SerializeField] private Vector2Int start;
    [Tooltip("퍼즐 밖에서 기다리는 칸 (판 바깥 좌표 가능, 예: -1). 들어가기 돌이 여기 놓이고, 나가면 여기로 돌아옴")]
    [SerializeField] private Vector2Int entrance = new Vector2Int(-1, 0);
    [SerializeField] private Vector2Int goal;
    [SerializeField] private GoalKind goalKind = GoalKind.Goal;
    [Tooltip("판 밖으로 걸어 나가면 퍼즐에서 나감 (메인은 끔)")]
    [SerializeField] private bool allowExit = true;

    [Header("수위")]
    [Range(1, 3)]
    [Tooltip("1 = 수위 없음, 2 = 높음/낮음 (메인), 3 = 낮음/중간/높음 (S3)")]
    [SerializeField] private int levelCount = 1;
    [Tooltip("시작 수위 (0 = 가장 낮음)")]
    [SerializeField] private int startLevel;
    [Tooltip("켜면 높이가 다른 땅은 계단으로만 오감 (S3)")]
    [SerializeField] private bool useTerraces;

    [Header("장치")]
    [SerializeField] private List<Vector2Int> eggs = new List<Vector2Int>();
    [SerializeField] private List<IslandDef> islands = new List<IslandDef>();
    [SerializeField] private List<PlateDef> plates = new List<PlateDef>();
    [SerializeField] private List<BridgeDef> bridges = new List<BridgeDef>();

    [Header("보상 / 기록")]
    [Tooltip("풀면 줄 아이템 (비우면 안 줌)")]
    [SerializeField] private string rewardItemId = "";
    [Tooltip("풀면 켤 플래그 (비우면 안 켬)")]
    [SerializeField] private string clearFlag = "";

    [Header("난이도 보조")]
    [Tooltip("제한 시간 (초, 0 = 없음). 끝나면 이 판만 처음으로")]
    [Min(0f)] [SerializeField] private float timeLimit;
    [Tooltip("이만큼 처음으로 돌아가면 힌트를 보여 줌 (0 = 안 보여 줌)")]
    [Min(0)] [SerializeField] private int hintAfterResets = 3;
    [TextArea(1, 3)]
    [SerializeField] private string hint = "";

    public string DisplayName => displayName;
    public string Intro => intro;
    public bool DryPits => dryPits;
    public Vector2Int Entrance => entrance;
    public GoalKind Goal => goalKind;
    public bool AllowExit => allowExit;
    public string RewardItemId => rewardItemId;
    public string ClearFlag => clearFlag;
    public float TimeLimit => timeLimit;
    public int HintAfterResets => hintAfterResets;
    public string Hint => hint;
    public IReadOnlyList<PlateDef> Plates => plates;
    public IReadOnlyList<BridgeDef> Bridges => bridges;

    public string[] Rows
    {
        get
        {
            string[] rows = (map ?? "").Replace("\r", "").Split('\n');
            // 맨 끝 빈 줄 제거
            int n = rows.Length;
            while (n > 0 && string.IsNullOrWhiteSpace(rows[n - 1])) n--;
            if (n == rows.Length) return rows;
            var trimmed = new string[n];
            Array.Copy(rows, trimmed, n);
            return trimmed;
        }
    }

    public int Width
    {
        get { int w = 0; foreach (string r in Rows) w = Mathf.Max(w, r.Length); return w; }
    }

    public int Height => Rows.Length;

    // 규칙 클래스(SobekBoardLogic)가 쓰는 형태로 변환
    public SobekBoardSpec ToSpec()
    {
        var spec = new SobekBoardSpec
        {
            Rows = Rows,
            Start = C(start),
            Goal = C(goal),
            LevelCount = levelCount,
            StartLevel = startLevel,
            UseTerraces = useTerraces,
            AllowExit = allowExit,
        };
        foreach (Vector2Int e in eggs) spec.Eggs.Add(C(e));
        foreach (IslandDef i in islands)
        {
            spec.Islands.Add(new SobekBoardSpec.Island { Center = C(i.center), Arm = new[] { i.north, i.east, i.south, i.west } });
        }
        foreach (PlateDef p in plates)
        {
            var sp = new SobekBoardSpec.Plate { Cell = C(p.cell), Kind = p.kind, BridgeId = p.bridgeId ?? "" };
            foreach (SpinDef s in p.spins) sp.Spins.Add(new SobekBoardSpec.Spin(s.island, s.clockwise ? 1 : -1));
            spec.Plates.Add(sp);
        }
        foreach (BridgeDef b in bridges) spec.Bridges.Add(new SobekBoardSpec.Bridge { Cell = C(b.cell), Id = b.id ?? "" });
        return spec;
    }

    private static SobekCell C(Vector2Int v) => new SobekCell(v.x, v.y);

#if UNITY_EDITOR
    // 에디터 자동 세팅용 (Tools/Sobek 메뉴)
    public void EditorSet(string name, string introText, string[] rows, Vector2Int startCell, Vector2Int entranceCell,
        Vector2Int goalCell, GoalKind kind, bool exit, int levels, int level0, bool terraces, bool dry,
        List<Vector2Int> eggList, List<IslandDef> islandList, List<PlateDef> plateList, List<BridgeDef> bridgeList,
        string reward, string flag, float limit, int hintResets, string hintText)
    {
        displayName = name; intro = introText; map = string.Join("\n", rows);
        start = startCell; entrance = entranceCell; goal = goalCell; goalKind = kind; allowExit = exit;
        levelCount = levels; startLevel = level0; useTerraces = terraces; dryPits = dry;
        eggs = eggList; islands = islandList; plates = plateList; bridges = bridgeList;
        rewardItemId = reward; clearFlag = flag; timeLimit = limit; hintAfterResets = hintResets; hint = hintText;
    }
#endif

    private void OnValidate()
    {
        string[] rows = Rows;
        for (int y = 1; y < rows.Length; y++)
        {
            if (rows[y].Length != rows[0].Length)
            {
                Debug.LogWarning($"[SobekBoardData] {name}: {y}번 줄 길이가 다름 ({rows[y].Length} / {rows[0].Length}). 모자란 칸은 벽으로 처리", this);
                break;
            }
        }
    }
}
