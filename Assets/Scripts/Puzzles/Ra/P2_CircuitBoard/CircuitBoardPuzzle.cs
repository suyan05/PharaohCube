using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public enum MirrorType
{
    Slash,     // '/'  동↔북, 서↔남
    Backslash  // '\'  동↔남, 서↔북
}

public class CircuitBoardPuzzle : PuzzleBase
{
    // ================= 보드 데이터 (기획서 5.1 / 10.2) =================
    public const int MinX = 1, MaxX = 7, MinY = 4, MaxY = 12;

    private static readonly Vector2Int SourceTile = new Vector2Int(1, 4);
    private static readonly Vector2Int SourceDir = Vector2Int.up; // 북쪽
    private static readonly Vector2Int PrismSocket = new Vector2Int(4, 9);

    private static readonly HashSet<Vector2Int> Obstacles = new HashSet<Vector2Int>
    {
        new Vector2Int(1, 9), new Vector2Int(2, 9), new Vector2Int(2, 4),
        new Vector2Int(3, 8), new Vector2Int(3, 11), new Vector2Int(4, 4),
        new Vector2Int(5, 9), new Vector2Int(6, 4), new Vector2Int(7, 5),
        new Vector2Int(7, 8), new Vector2Int(7, 11)
    };

    // 거울/조각을 놓을 수 있는 자리
    public static readonly Vector2Int TileA = new Vector2Int(1, 6);
    public static readonly Vector2Int TileS2 = new Vector2Int(3, 6);
    public static readonly Vector2Int TileS3 = new Vector2Int(4, 6);
    public static readonly Vector2Int TileB = new Vector2Int(5, 6);
    public static readonly Vector2Int TileS1 = new Vector2Int(6, 6);

    private static readonly HashSet<Vector2Int> SlotTiles = new HashSet<Vector2Int>
    {
        TileA, TileS2, TileS3, TileB, TileS1
    };

    // 초기 거울 A, B도 회수 가능하게 아이템 ID로 취급
    public const string MirrorA = "MIRROR_A";
    public const string MirrorB = "MIRROR_B";

    // ================= 단계 목표 (기획서 4.2 / 5.3 / 10.2) =================
    private class StageGoal
    {
        public char edge;          // 'S' = 남변, 'N' = 북변
        public int x;              // 빛이 빠져나가는 x 좌표
        public bool needBlue;      // true면 파란빛만 인정
        public string flag;        // 점등 시 켤 플래그
        public string reward;      // 지급 아이템 (없으면 null)
        public string label;
        public Vector2Int needTile;  // 필요한 조각이 있어야 할 자리
        public string needPiece;     // 필요한 조각 ID (없으면 null)

        public StageGoal(char edge, int x, bool needBlue, string flag, string reward, string label,
                         Vector2Int needTile, string needPiece)
        {
            this.edge = edge; this.x = x; this.needBlue = needBlue;
            this.flag = flag; this.reward = reward; this.label = label;
            this.needTile = needTile; this.needPiece = needPiece;
        }
    }

    // 기획서 10.2 "need": 1단계 없음 / FRAG_1@S1 / FRAG_2@S2 / FRAG_3@S3 + 프리즘(파란빛으로 판정)
    private static readonly StageGoal[] Stages =
    {
        new StageGoal('S', 5, false, "F_RA_CIRCUIT_1", ItemIds.LensSun,      "G1", Vector2Int.zero, null),
        new StageGoal('N', 6, false, "F_RA_CIRCUIT_2", ItemIds.PigmentNight, "G2", TileS1, ItemIds.Frag1),
        new StageGoal('S', 3, false, "F_RA_CIRCUIT_3", ItemIds.KeyBrass,     "G3", TileS2, ItemIds.Frag2),
        new StageGoal('N', 4, true,  "F_RA_CIRCUIT_4", null,                 "G4", TileS3, ItemIds.Frag3)
    };

    // ================= 현재 보드 상태 =================
    private class Piece
    {
        public string id;
        public MirrorType type;
        public Piece(string id, MirrorType type) { this.id = id; this.type = type; }
    }

    private readonly Dictionary<Vector2Int, Piece> pieces = new Dictionary<Vector2Int, Piece>();
    private bool prismOn;
    private int currentStage; // 0~3, 4면 전부 완료

    // 마지막 빔 계산 결과
    private readonly List<Vector2Int> beamPath = new List<Vector2Int>();
    private bool beamExited;
    private char exitEdge;
    private int exitX;
    private bool beamBlue;

    // 보드가 바뀔 때마다 알림 (화면 다시 그리기용)
    public event Action OnBoardChanged;

    // ================= 화면(View)에서 읽는 용도 =================
    public int CurrentStage => currentStage;
    public IReadOnlyList<Vector2Int> BeamPath => beamPath;
    public bool BeamExited => beamExited;
    public char ExitEdge => exitEdge;
    public int ExitX => exitX;
    public bool BeamBlue => beamBlue;
    public bool PrismOn => prismOn;

    public static Vector2Int Source => SourceTile;
    public static Vector2Int PrismTile => PrismSocket;
    public static bool IsObstacle(Vector2Int tile) => Obstacles.Contains(tile);
    public static bool IsSlot(Vector2Int tile) => SlotTiles.Contains(tile);

    public int GoalCount => Stages.Length;

    public void GetGoal(int index, out char edge, out int x, out bool needBlue, out string flag, out string label)
    {
        StageGoal g = Stages[index];
        edge = g.edge; x = g.x; needBlue = g.needBlue; flag = g.flag; label = g.label;
    }

    public List<(Vector2Int tile, string id, MirrorType type)> GetPieces()
    {
        var list = new List<(Vector2Int, string, MirrorType)>();
        foreach (var pair in pieces)
        {
            list.Add((pair.Key, pair.Value.id, pair.Value.type));
        }
        return list;
    }

    // ================= 초기화 =================
    private void Awake()
    {
        SetupInitialBoard();
        TraceBeam(); // 화면에 처음부터 빔이 보이도록 계산만 해둠 (클리어 판정은 Open 때)
    }

    private void SetupInitialBoard()
    {
        pieces.Clear();
        pieces[TileA] = new Piece(MirrorA, MirrorType.Slash);     // A(1,6) = '/'
        pieces[TileB] = new Piece(MirrorB, MirrorType.Backslash); // B(5,6) = '\'
        prismOn = false;
        currentStage = 0;
    }

    // ================= PuzzleBase 구현 =================
    public override void Open()
    {
        base.Open();
        Submit(); // 1단계는 초기 배치 그대로 G1에 닿는 '관찰 단계'
    }

    // 조작할 때마다 호출: 빔 다시 계산 → 목표 닿으면 단계 클리어
    public override void Submit()
    {
        TraceBeam();
        LogBeam();

        if (Validate())
        {
            CompleteStage();
        }
        else if (BeamReachesGoal() && !HasRequiredPiece())
        {
            // 빛은 닿았지만 필요한 조각이 아님 (예: 거울 B로 대신 도달)
            Debug.Log("[P2] 빛은 닿았지만 문양이 반응하지 않는다. 다른 조각이 필요한 것 같다.");
        }
        // 오답 개념 없음 (기획서 5.3) → 실패 처리/리셋 안 함

        OnBoardChanged?.Invoke();
    }

    protected override bool Validate()
    {
        return BeamReachesGoal() && HasRequiredPiece();
    }

    // 현재 단계 목표에 올바른 색의 빛이 닿았는지
    private bool BeamReachesGoal()
    {
        if (currentStage >= Stages.Length) return false;
        if (!beamExited) return false;

        StageGoal goal = Stages[currentStage];
        return exitEdge == goal.edge
            && exitX == goal.x
            && beamBlue == goal.needBlue;
    }

    // 현재 단계에 필요한 조각이 정해진 자리에 있는지 (기획서 10.2 need)
    private bool HasRequiredPiece()
    {
        if (currentStage >= Stages.Length) return false;

        StageGoal goal = Stages[currentStage];
        if (goal.needPiece == null) return true; // 1단계는 조건 없음

        return pieces.TryGetValue(goal.needTile, out Piece piece)
            && piece.id == goal.needPiece;
    }

    private void CompleteStage()
    {
        StageGoal goal = Stages[currentStage];
        Debug.Log($"[P2] ★ {goal.label} 점등! ({currentStage + 1}단계 클리어)");

        GameManager.Instance.SetFlag(goal.flag);

        if (!string.IsNullOrEmpty(goal.reward))
        {
            ItemInventory.Instance.Grant(goal.reward);
        }

        currentStage++;

        if (currentStage >= Stages.Length)
        {
            Debug.Log("[P2] 4문양 전부 점등 → P5 제단 개방");
            OnSuccessInternal();
        }
    }

    // ================= 빔 시뮬레이션 =================
    private void TraceBeam()
    {
        beamPath.Clear();
        beamExited = false;
        beamBlue = false;

        Vector2Int pos = SourceTile;
        Vector2Int dir = SourceDir;
        beamPath.Add(pos);

        var visited = new HashSet<(Vector2Int, Vector2Int)>();

        while (true)
        {
            Vector2Int next = pos + dir;

            // 보드 밖으로 나감 → 어느 변으로 나갔는지 기록
            if (next.x < MinX || next.x > MaxX || next.y < MinY || next.y > MaxY)
            {
                beamExited = true;
                exitEdge = DirToEdge(dir);
                exitX = next.x;
                return;
            }

            // 장애물 → 빛 차단
            if (Obstacles.Contains(next))
            {
                return;
            }

            pos = next;
            beamPath.Add(pos);

            // 프리즘 통과 → 파란빛으로 변환
            if (prismOn && pos == PrismSocket)
            {
                beamBlue = true;
            }

            // 거울 → 방향 전환 (빈 슬롯/빈 칸은 그냥 통과)
            if (pieces.TryGetValue(pos, out Piece piece))
            {
                dir = Reflect(dir, piece.type);
            }

            // 같은 칸을 같은 방향으로 또 지나감 → 루프 → 빛 소멸
            if (!visited.Add((pos, dir)))
            {
                return;
            }
        }
    }

    private static Vector2Int Reflect(Vector2Int dir, MirrorType type)
    {
        if (type == MirrorType.Slash)
            return new Vector2Int(dir.y, dir.x);   // 동→북, 북→동, 서→남, 남→서
        else
            return new Vector2Int(-dir.y, -dir.x); // 동→남, 남→동, 서→북, 북→서
    }

    private static char DirToEdge(Vector2Int dir)
    {
        if (dir == Vector2Int.up) return 'N';
        if (dir == Vector2Int.down) return 'S';
        if (dir == Vector2Int.right) return 'E';
        return 'W';
    }

    private void LogBeam()
    {
        var sb = new StringBuilder("[P2] 빔 경로: ");
        foreach (var p in beamPath) sb.Append($"({p.x},{p.y}) ");

        if (beamExited)
            sb.Append($"→ {exitEdge}변 x={exitX}로 나감, 색={(beamBlue ? "파랑" : "주황")}");
        else
            sb.Append("→ 차단 또는 소멸");

        Debug.Log(sb.ToString());
    }

    // ================= 플레이어 조작 (클릭 UI에서 호출) =================
    public void RotatePiece(Vector2Int tile)
    {
        if (!pieces.TryGetValue(tile, out Piece piece))
        {
            Debug.Log($"[P2] ({tile.x},{tile.y})에 거울 없음");
            return;
        }

        piece.type = piece.type == MirrorType.Slash ? MirrorType.Backslash : MirrorType.Slash;
        Debug.Log($"[P2] ({tile.x},{tile.y}) 회전 → {(piece.type == MirrorType.Slash ? "/" : "\\")}");
        Submit();
    }

    public void RemovePiece(Vector2Int tile)
    {
        if (!pieces.TryGetValue(tile, out Piece piece))
        {
            Debug.Log($"[P2] ({tile.x},{tile.y})에 회수할 거울 없음");
            return;
        }

        pieces.Remove(tile);
        ItemInventory.Instance.Grant(piece.id);
        Debug.Log($"[P2] ({tile.x},{tile.y})에서 {piece.id} 회수");
        Submit();
    }

    public void InsertPiece(Vector2Int tile, string pieceId, MirrorType type)
    {
        if (!SlotTiles.Contains(tile))
        {
            Debug.Log($"[P2] ({tile.x},{tile.y})는 슬롯이 아님");
            return;
        }
        if (pieces.ContainsKey(tile))
        {
            Debug.Log($"[P2] ({tile.x},{tile.y})에 이미 거울 있음");
            return;
        }
        if (!ItemInventory.Instance.Consume(pieceId))
        {
            Debug.Log($"[P2] {pieceId}를 가지고 있지 않음");
            return;
        }

        pieces[tile] = new Piece(pieceId, type);
        Debug.Log($"[P2] ({tile.x},{tile.y})에 {pieceId} 삽입");
        Submit();
    }

    public void InsertPrism()
    {
        if (prismOn) return;
        if (!ItemInventory.Instance.Consume(ItemIds.Prism))
        {
            Debug.Log("[P2] 프리즘 없음");
            return;
        }

        prismOn = true;
        Debug.Log("[P2] 프리즘 장착 (4,9)");
        Submit();
    }

    public void RemovePrism()
    {
        if (!prismOn) return;

        prismOn = false;
        ItemInventory.Instance.Grant(ItemIds.Prism);
        Debug.Log("[P2] 프리즘 회수");
        Submit();
    }

    // ================= 테스트용 (기획서 5.3 정답 순서) =================
    [ContextMenu("테스트 1) Open - 1단계 관찰")]
    void TestStage1() => Open();

    [ContextMenu("테스트 2) B 회수 + 조각①을 S1에 '/'")]
    void TestStage2()
    {
        RemovePiece(TileB);
        InsertPiece(TileS1, ItemIds.Frag1, MirrorType.Slash);
    }

    [ContextMenu("테스트 3) 조각②를 S2에 '\\'")]
    void TestStage3()
    {
        InsertPiece(TileS2, ItemIds.Frag2, MirrorType.Backslash);
    }

    [ContextMenu("테스트 4) ② 회수 + 조각③을 S3에 '/' + 프리즘")]
    void TestStage4()
    {
        RemovePiece(TileS2);
        InsertPiece(TileS3, ItemIds.Frag3, MirrorType.Slash);
        InsertPrism();
    }
}