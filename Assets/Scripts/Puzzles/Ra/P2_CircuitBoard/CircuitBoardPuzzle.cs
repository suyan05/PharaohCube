using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public enum MirrorType
{
    Slash,     // '/'  동<->북, 서<->남
    Backslash  // '\'  동<->남, 서<->북
}

// P2 태양의 빛 회로 (허브) - "거울로 빛의 길 바꾸기"
// 낮: 4단계 체인 / 밤: 데이터의 startCompleted로 완성 상태(파란 빛)에서 시작하고 조작 잠금
// 조각 자동 장착: 지금 단계에 필요한 조각을 갖고 있으면 정답 칸에 알아서 끼워짐
// - 일부러 틀린 방향으로 끼워서 플레이어가 한 번은 돌려야 함
// - 그 단계에 방해되는 조각(2단계 거울 B, 4단계 조각 2)은 자동으로 빠짐
public class CircuitBoardPuzzle : RaPhasedPuzzle
{
    // ================= 보드 데이터 (기획서 4.2) =================
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

    private static readonly Vector2Int[] NoClear = new Vector2Int[0];

    // ================= 단계 목표 (기획서 3장 / 4.2) =================
    private class StageGoal
    {
        public char edge;          // 'S' = 남변, 'N' = 북변
        public int x;              // 빛이 빠져나가는 x 좌표
        public bool needBlue;      // true면 파란빛만 인정 (프리즘 필요)
        public string flag;        // 점등 시 켤 플래그
        public string reward;      // 지급 아이템 (없으면 null)
        public string label;
        public Vector2Int needTile;  // 필요한 조각이 있어야 할 자리
        public string needPiece;     // 필요한 조각 ID (없으면 null)
        public Vector2Int[] clearTiles; // 자동 장착 때 치울 자리 (빛을 가로막는 조각)
        public string sourceHint;       // 조각이 없을 때 화면에 띄울 안내

        public StageGoal(char edge, int x, bool needBlue, string flag, string reward, string label,
                         Vector2Int needTile, string needPiece, Vector2Int[] clearTiles, string sourceHint)
        {
            this.edge = edge; this.x = x; this.needBlue = needBlue;
            this.flag = flag; this.reward = reward; this.label = label;
            this.needTile = needTile; this.needPiece = needPiece;
            this.clearTiles = clearTiles; this.sourceHint = sourceHint;
        }
    }

    // 1단계 조건 없음 / FRAG_1@S1 / FRAG_2@S2 / FRAG_3@S3 + 프리즘(파란빛으로 판정)
    private static readonly StageGoal[] Stages =
    {
        new StageGoal('S', 5, false, "F_RA_CIRCUIT_1", ItemIds.LensSun, "G1",
            Vector2Int.zero, null, NoClear, ""),
        new StageGoal('N', 6, false, "F_RA_CIRCUIT_2", ItemIds.PigmentNight, "G2",
            TileS1, ItemIds.Frag1, new[] { TileB },
            "동쪽 방 그림자 시계에서 거울 조각 1을 찾아오자."),
        new StageGoal('S', 3, false, "F_RA_CIRCUIT_3", ItemIds.KeyBrass, "G3",
            TileS2, ItemIds.Frag2, NoClear,
            "북서쪽 방 일식 석탁에서 거울 조각 2를 찾아오자."),
        new StageGoal('N', 4, true, "F_RA_CIRCUIT_4", null, "G4",
            TileS3, ItemIds.Frag3, new[] { TileS2 },
            "남서쪽 방 카드 벤치에서 거울 조각 3을 찾아오자.")
    };

    // ================= 낮/밤 데이터 =================
    [Header("낮/밤 데이터 (밤 데이터가 비면 낮 데이터 사용)")]
    [SerializeField] private CircuitBoardPuzzleData dayData;
    [SerializeField] private CircuitBoardPuzzleData nightData;
    private CircuitBoardPuzzleData fallbackData;

    private CircuitBoardPuzzleData Data => PickData(dayData, nightData, ref fallbackData);
    protected override RaPuzzleData CurrentData => Data;

    // 완성 상태로 시작한 회로판은 조작 잠금 (밤)
    public bool IsLocked => Data.StartCompleted;

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
    public bool IsComplete => currentStage >= Stages.Length;
    public IReadOnlyList<Vector2Int> BeamPath => beamPath;
    public bool BeamExited => beamExited;
    public char ExitEdge => exitEdge;
    public int ExitX => exitX;
    public bool BeamBlue => beamBlue;
    public bool PrismOn => prismOn;

    // 방금 조각이 자동으로 끼워졌는지 (거울을 돌리면 false)
    public bool PieceJustPlaced { get; private set; }

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

    // 지금 단계에 필요한 조각(또는 프리즘)이 아직 없으면 안내 문구, 다 있으면 null
    public string MissingHint()
    {
        if (IsLocked || IsComplete) return null;

        StageGoal g = Stages[currentStage];
        if (g.needPiece == null) return null;

        bool pieceOk = IsPiecePlaced(g);
        bool prismOk = !g.needBlue || prismOn;
        if (pieceOk && prismOk) return null;

        return pieceOk ? "일식 석탁에서 받은 프리즘이 필요하다." : g.sourceHint;
    }

    // ================= 초기화 =================
    private void Awake()
    {
        SetupBoardForPhase();
        TraceBeam(); // 화면에 처음부터 빔이 보이도록 계산만 해둠 (클리어 판정은 Open 때)
    }

    private void SetupBoardForPhase()
    {
        if (Data.StartCompleted) SetupCompletedBoard();
        else SetupInitialBoard();
    }

    private void SetupInitialBoard()
    {
        pieces.Clear();
        pieces[TileA] = new Piece(MirrorA, MirrorType.Slash);     // A(1,6) = '/'
        pieces[TileB] = new Piece(MirrorB, MirrorType.Backslash); // B(5,6) = '\'
        prismOn = false;
        currentStage = 0;
        PieceJustPlaced = false;
    }

    // 4단계 정답 배치 그대로 (A '/', (1) S1 '/', (3) S3 '/', 프리즘) -> 북변 x4로 파란 빛
    private void SetupCompletedBoard()
    {
        pieces.Clear();
        pieces[TileA] = new Piece(MirrorA, MirrorType.Slash);
        pieces[TileS1] = new Piece(ItemIds.Frag1, MirrorType.Slash);
        pieces[TileS3] = new Piece(ItemIds.Frag3, MirrorType.Slash);
        prismOn = true;
        currentStage = Stages.Length;
        PieceJustPlaced = false;
    }

    // ================= 튜토리얼 =================
    // 튜토리얼(거울 돌려보기)을 한 번 봤는지 저장하는 플래그
    private const string TutorialSeenFlag = "F_RA_P2_TUTORIAL_SEEN";
    public event Action<int> OnTutorialProgress; // 지금까지 누른 횟수 (0~4) 전달

    public bool TutorialDone => IsLocked || IsFlagOn(TutorialSeenFlag);
    private int tutorialClicks;

    // ================= PuzzleBase 구현 =================
    public override void Open()
    {
        base.Open();
        if (!CanEnter()) return;

        if (IsLocked)
        {
            // 밤: 이미 완성된 회로판을 보여주기만 함
            TraceBeam();
            OnBoardChanged?.Invoke();
            return;
        }

        if (TutorialDone)
        {
            Submit();       // 이미 조작법을 배웠으면 기존처럼 즉시 1단계 관찰 클리어
            TryAutoPlace(); // 가져온 조각이 있으면 자동 장착
        }
        else
        {
            Debug.Log("[P2] 거울을 클릭해서 돌려 보자. 한 바퀴 돌리면 원래 모양으로 돌아온다.");
            OnTutorialProgress?.Invoke(0);
        }
    }

    // 조작할 때마다 호출: 빔 다시 계산 -> 목표 닿으면 단계 클리어
    public override void Submit()
    {
        try
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
            // 오답 개념 없음 (기획서 4.2) -> 실패 처리/리셋 안 함
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] P2 판정 오류: {e}");
        }

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

    // 현재 단계에 필요한 조각이 정해진 자리에 있는지
    private bool HasRequiredPiece()
    {
        if (currentStage >= Stages.Length) return false;

        StageGoal goal = Stages[currentStage];
        if (goal.needPiece == null) return true; // 1단계는 조건 없음

        return IsPiecePlaced(goal);
    }

    private bool IsPiecePlaced(StageGoal goal)
    {
        return pieces.TryGetValue(goal.needTile, out Piece piece) && piece.id == goal.needPiece;
    }

    private void CompleteStage()
    {
        StageGoal goal = Stages[currentStage];
        Debug.Log($"[P2] {goal.label} 점등! ({currentStage + 1}단계 클리어)");

        if (GameManager.Instance != null) GameManager.Instance.SetFlag(goal.flag);

        if (GrantRewards && !string.IsNullOrEmpty(goal.reward) && ItemInventory.Instance != null)
        {
            ItemInventory.Instance.Grant(goal.reward);
        }

        currentStage++;
        PieceJustPlaced = false;

        if (currentStage >= Stages.Length)
        {
            Debug.Log("[P2] 4문양 전부 점등 -> P5 제단 개방");
            OnSuccessInternal();
            return;
        }

        TryAutoPlace(); // 다음 단계 조각을 이미 갖고 있으면 바로 장착
    }

    // 낮 -> 밤 전환: 데이터대로 보드를 다시 세팅 (밤이면 완성 상태)
    protected override void ApplyPhase()
    {
        tutorialClicks = 0;
        SetupBoardForPhase();
        TraceBeam();
        OnBoardChanged?.Invoke();
    }

    // ================= 조각 자동 장착 =================
    // 지금 단계 조각(+프리즘)을 갖고 있으면 정답 칸에 끼움. 바뀐 게 있으면 true
    private bool TryAutoPlace()
    {
        if (IsLocked || IsComplete) return false;

        try
        {
            StageGoal g = Stages[currentStage];
            bool changed = PlaceStagePiece(g);
            changed |= PlaceStagePrism(g);
            if (!changed) return false;

            MakeUnsolved(g);
            TraceBeam();
            PieceJustPlaced = true;
            OnBoardChanged?.Invoke();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 조각 자동 장착 오류: {e}");
            return false;
        }
    }

    private bool PlaceStagePiece(StageGoal g)
    {
        if (g.needPiece == null || IsPiecePlaced(g)) return false;

        ItemInventory inventory = ItemInventory.Instance;
        if (inventory == null || !inventory.Has(g.needPiece)) return false;

        ClearBlockers(g);
        pieces.Remove(g.needTile); // 자리에 다른 조각이 있으면 치움
        inventory.Consume(g.needPiece);

        pieces[g.needTile] = new Piece(g.needPiece, MirrorType.Backslash);
        Debug.Log($"[P2] {g.needPiece} 자동 장착 ({g.needTile.x},{g.needTile.y})");
        return true;
    }

    private bool PlaceStagePrism(StageGoal g)
    {
        if (!g.needBlue || prismOn) return false;

        ItemInventory inventory = ItemInventory.Instance;
        if (inventory == null || !inventory.Has(ItemIds.Prism)) return false;

        inventory.Consume(ItemIds.Prism);
        prismOn = true;
        Debug.Log("[P2] 프리즘 자동 장착 (4,9)");
        return true;
    }

    // 빛을 가로막는 조각은 빼서 버림 (인벤토리로 돌려주지 않음 -> 회수 알림이 안 뜸)
    private void ClearBlockers(StageGoal g)
    {
        foreach (Vector2Int tile in g.clearTiles)
        {
            if (pieces.Remove(tile))
            {
                Debug.Log($"[P2] ({tile.x},{tile.y}) 조각이 빠짐");
            }
        }
    }

    // 끼우자마자 풀려 버리면 플레이어가 할 게 없으니 반대 방향으로 돌려둠
    private void MakeUnsolved(StageGoal g)
    {
        TraceBeam();
        if (!Validate()) return;

        if (pieces.TryGetValue(g.needTile, out Piece piece))
        {
            piece.type = Flip(piece.type);
        }
    }

    private static MirrorType Flip(MirrorType type)
    {
        return type == MirrorType.Slash ? MirrorType.Backslash : MirrorType.Slash;
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

        // 한 칸씩 전진, 멈춰야 하면 false
        while (StepBeam(ref pos, ref dir, visited)) { }
    }

    private bool StepBeam(ref Vector2Int pos, ref Vector2Int dir, HashSet<(Vector2Int, Vector2Int)> visited)
    {
        Vector2Int next = pos + dir;

        // 보드 밖으로 나감 -> 어느 변으로 나갔는지 기록
        if (next.x < MinX || next.x > MaxX || next.y < MinY || next.y > MaxY)
        {
            beamExited = true;
            exitEdge = DirToEdge(dir);
            exitX = next.x;
            return false;
        }

        // 장애물 -> 빛 차단
        if (Obstacles.Contains(next)) return false;

        pos = next;
        beamPath.Add(pos);

        // 프리즘 통과 -> 파란빛으로 변환
        if (prismOn && pos == PrismSocket) beamBlue = true;

        // 거울 -> 방향 전환 (빈 슬롯/빈 칸은 그냥 통과)
        if (pieces.TryGetValue(pos, out Piece piece)) dir = Reflect(dir, piece.type);

        // 같은 칸을 같은 방향으로 또 지나감 -> 루프 -> 빛 소멸
        return visited.Add((pos, dir));
    }

    private static Vector2Int Reflect(Vector2Int dir, MirrorType type)
    {
        if (type == MirrorType.Slash)
            return new Vector2Int(dir.y, dir.x);   // 동->북, 북->동, 서->남, 남->서
        else
            return new Vector2Int(-dir.y, -dir.x); // 동->남, 남->동, 서->북, 북->서
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
            sb.Append($"-> {exitEdge}변 x={exitX}로 나감, 색={(beamBlue ? "파랑" : "주황")}");
        else
            sb.Append("-> 차단 또는 소멸");

        Debug.Log(sb.ToString());
    }

    // ================= 플레이어 조작 (클릭 UI에서 호출) =================
    // 밤(완성 상태)에는 조작 막기
    private bool BlockIfLocked()
    {
        if (!IsLocked) return false;
        Debug.Log("[P2] 회로판은 이미 파란 빛으로 완성되어 있다.");
        return true;
    }

    public void RotatePiece(Vector2Int tile)
    {
        if (BlockIfLocked()) return;
        if (!pieces.TryGetValue(tile, out Piece piece))
        {
            Debug.Log($"[P2] ({tile.x},{tile.y})에 거울 없음");
            return;
        }

        piece.type = Flip(piece.type);
        PieceJustPlaced = false;
        Debug.Log($"[P2] ({tile.x},{tile.y}) 회전 -> {(piece.type == MirrorType.Slash ? "/" : "\\")}");

        if (!TutorialDone && !AdvanceTutorial()) return;

        Submit();
    }

    // 튜토리얼 클릭 처리. 아직 진행 중이면 false (클리어 판정 안 함)
    private bool AdvanceTutorial()
    {
        tutorialClicks++;
        OnTutorialProgress?.Invoke(tutorialClicks);

        if (tutorialClicks < 4)
        {
            OnBoardChanged?.Invoke(); // 빛 경로만 갱신, 아직 클리어 판정은 안 함
            return false;
        }

        // 4번째 클릭으로 원위치 = 튜토리얼 완료
        if (GameManager.Instance != null) GameManager.Instance.SetFlag(TutorialSeenFlag);
        Debug.Log("[P2] 빛이 다시 새벽 문양에 닿았다! 거울을 돌려 길을 바꾸는 방법을 익혔다.");
        return true;
    }

    // 아래 함수들은 자동 장착으로 바뀐 뒤 화면에서는 안 쓰지만, 테스트 메뉴용으로 유지
    public void RemovePiece(Vector2Int tile)
    {
        if (BlockIfLocked()) return;
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
        if (BlockIfLocked()) return;
        if (!CanInsertAt(tile)) return;

        if (!ItemInventory.Instance.Consume(pieceId))
        {
            Debug.Log($"[P2] {pieceId}를 가지고 있지 않음");
            return;
        }

        pieces[tile] = new Piece(pieceId, type);
        Debug.Log($"[P2] ({tile.x},{tile.y})에 {pieceId} 삽입");
        Submit();
    }

    private bool CanInsertAt(Vector2Int tile)
    {
        if (!SlotTiles.Contains(tile))
        {
            Debug.Log($"[P2] ({tile.x},{tile.y})는 슬롯이 아님");
            return false;
        }
        if (pieces.ContainsKey(tile))
        {
            Debug.Log($"[P2] ({tile.x},{tile.y})에 이미 거울 있음");
            return false;
        }
        return true;
    }

    public void InsertPrism()
    {
        if (BlockIfLocked()) return;
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
        if (BlockIfLocked()) return;
        if (!prismOn) return;

        prismOn = false;
        ItemInventory.Instance.Grant(ItemIds.Prism);
        Debug.Log("[P2] 프리즘 회수");
        Submit();
    }

    // ================= 테스트용 (기획서 4.2 정답 순서) =================
    [ContextMenu("테스트 1) Open - 1단계 관찰")]
    void TestStage1() => Open();

    [ContextMenu("테스트 2) B 회수 + 조각(1)을 S1에 '/'")]
    void TestStage2()
    {
        RemovePiece(TileB);
        InsertPiece(TileS1, ItemIds.Frag1, MirrorType.Slash);
    }

    [ContextMenu("테스트 3) 조각(2)를 S2에 '\\'")]
    void TestStage3()
    {
        InsertPiece(TileS2, ItemIds.Frag2, MirrorType.Backslash);
    }

    [ContextMenu("테스트 4) (2) 회수 + 조각(3)을 S3에 '/' + 프리즘")]
    void TestStage4()
    {
        RemovePiece(TileS2);
        InsertPiece(TileS3, ItemIds.Frag3, MirrorType.Slash);
        InsertPrism();
    }

    [ContextMenu("테스트: 지금 단계 조각 받고 자동 장착")]
    void TestGrantAndAutoPlace()
    {
        if (IsComplete || ItemInventory.Instance == null) return;

        StageGoal g = Stages[currentStage];
        if (g.needPiece != null) ItemInventory.Instance.Grant(g.needPiece);
        if (g.needBlue) ItemInventory.Instance.Grant(ItemIds.Prism);
        TryAutoPlace();
    }
}