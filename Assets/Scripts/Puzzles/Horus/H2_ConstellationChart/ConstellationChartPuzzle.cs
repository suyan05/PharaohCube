using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 성도 한 단계의 데이터 (Inspector에서 수정 가능)
[Serializable]
public class ConstellationStage
{
    public string displayName = "";
    public int maxStrokes = 1;
    public bool requireSilver = false;
    public bool tutorial = false;
    public string clearFlag = "";
    public string clearMessage = "";
    public List<Vector2Int> needSockets = new List<Vector2Int>();
    public List<string> edges = new List<string>();
}

// H2 매의 성도: 4단계 성좌를 차례로 완성하는 허브 퍼즐 (기획서 5장)
public class ConstellationChartPuzzle : PuzzleBase
{
    [Header("연결")]
    [SerializeField] private StarLineDrawer drawer;
    [SerializeField] private StarBoardView board;
    [SerializeField] private StarGuideView guide;

    [Header("달의 눈 장착 플래그 (켜져 있으면 선이 은빛)")]
    [SerializeField] private string moonEyeFlag = "F_HO_MOON_EYE";

    [Header("선 색")]
    [SerializeField] private Color goldColor = new Color(0.91f, 0.70f, 0.23f);
    [SerializeField] private Color silverColor = new Color(0.79f, 0.83f, 0.88f);

    [Header("단계 데이터 (기획서 8.2)")]
    [SerializeField] private List<ConstellationStage> stages = DefaultStages();

    public event Action<string> OnMessage;
    public event Action<string> OnTitleChanged;
    public event Action<int> OnStageCleared;

    public int StageIndex => stageIndex;

    private readonly HashSet<Vector2Int> litSockets = new HashSet<Vector2Int>();
    private HashSet<string> answer = new HashSet<string>();
    private int stageIndex;
    private bool busy;
    private bool subscribed;

    private ConstellationStage Current => stages[Mathf.Clamp(stageIndex, 0, stages.Count - 1)];

    private void Awake()
    {
        if (stages == null || stages.Count == 0)
        {
            Debug.LogError("[성도] 단계 데이터가 비어 있음");
            return;
        }
        LoadStage(0);
    }

    private void OnEnable()
    {
        Subscribe(true);
    }

    private void OnDisable()
    {
        Subscribe(false);
    }

    private void Subscribe(bool on)
    {
        if (drawer == null || board == null)
        {
            if (on) Debug.LogError("[성도] Drawer / Board 연결 필요");
            return;
        }
        if (on == subscribed) return;
        subscribed = on;
        if (on)
        {
            drawer.OnStrokeEnded += HandleStroke;
            drawer.OnRejected += Message;
            board.OnBuilt += ApplyStageVisuals;
            return;
        }
        drawer.OnStrokeEnded -= HandleStroke;
        drawer.OnRejected -= Message;
        board.OnBuilt -= ApplyStageVisuals;
    }

    // ================= PuzzleBase =================
    public override void Open()
    {
        try
        {
            base.Open();
            if (IsCleared)
            {
                Message("모든 성좌가 하늘에 새겨져 있다.");
                return;
            }
            if (board.IsBuilt) ApplyStageVisuals();
        }
        catch (Exception e)
        {
            Debug.LogError($"[성도] Open 오류: {e}");
        }
    }

    public override void Submit()
    {
        try
        {
            if (IsCleared || busy) return;
            if (Validate())
            {
                StartCoroutine(StageClearRoutine());
                return;
            }
            bool shapeDone = answer.SetEquals(drawer.DrawnEdges);
            if (!shapeDone && drawer.StrokeCount < Current.maxStrokes)
            {
                Message($"{drawer.StrokeCount}/{Current.maxStrokes}획 사용 - 이어서 그려 보자");
                return;
            }
            Message(shapeDone ? "눈이 아직 감겨 있다 - 은빛 선이 필요하다" : "획을 모두 썼다 - 시작 별을 바꿔 보자");
            OnFailInternal();
        }
        catch (Exception e)
        {
            Debug.LogError($"[성도] Submit 오류: {e}");
        }
    }

    protected override bool Validate()
    {
        if (!answer.SetEquals(drawer.DrawnEdges)) return false;
        return !Current.requireSilver || IsMoonEyeMounted();
    }

    // 실패 시 0.3초 뒤 내가 그린 선만 지움 (단계 진행도는 유지)
    public override void Reset()
    {
        base.Reset();
        if (isActiveAndEnabled) StartCoroutine(ClearAfterFlash());
        else drawer.ClearAll();
    }

    // ================= 단계 진행 =================
    private void LoadStage(int index)
    {
        stageIndex = index;
        answer = StarGraph.BuildEdgeSet(Current.edges);
        int min = StarGraph.MinStrokes(answer);
        if (min > Current.maxStrokes)
        {
            Debug.LogWarning($"[성도] {Current.displayName}: 최소 {min}획인데 제한이 {Current.maxStrokes}획 - 풀 수 없음");
        }
    }

    private IEnumerator StageClearRoutine()
    {
        busy = true;
        drawer.SetInputEnabled(false);
        ConstellationStage done = Current;
        if (!string.IsNullOrEmpty(done.clearFlag)) GameManager.Instance.SetFlag(done.clearFlag);
        Message(done.clearMessage);
        OnStageCleared?.Invoke(stageIndex);
        yield return new WaitForSeconds(1.5f);
        drawer.ClearAll();
        if (stageIndex >= stages.Count - 1)
        {
            busy = false;
            if (guide != null) guide.HideAll();
            OnSuccessInternal();
            yield break;
        }
        LoadStage(stageIndex + 1);
        drawer.SetInputEnabled(true);
        busy = false;
        ApplyStageVisuals();
    }

    private IEnumerator ClearAfterFlash()
    {
        busy = true;
        drawer.SetInputEnabled(false);
        yield return new WaitForSeconds(0.3f);
        drawer.ClearAll();
        drawer.SetInputEnabled(true);
        busy = false;
    }

    private void ApplyStageVisuals()
    {
        try
        {
            if (!board.IsBuilt || IsCleared) return;
            foreach (Vector2Int c in litSockets) board.SetStarLit(c);
            drawer.SetLineColor(IsMoonEyeMounted() ? silverColor : goldColor);
            if (guide != null)
            {
                guide.ShowGuide(answer);
                if (Current.tutorial) guide.ShowStartMarkers(StarGraph.OddDegreeStars(answer));
                else guide.HideStartMarkers();
            }
            OnTitleChanged?.Invoke($"성도 {stageIndex + 1}단계 - {Current.displayName} (최대 {Current.maxStrokes}획)");
            Message(StartMessage());
        }
        catch (Exception e)
        {
            Debug.LogError($"[성도] 화면 갱신 오류: {e}");
        }
    }

    private string StartMessage()
    {
        if (MissingSocketCount() > 0) return "꺼진 별이 있다 - 별 조각을 끼워야 그릴 수 있다";
        if (Current.requireSilver && !IsMoonEyeMounted()) return "은빛 선이 필요하다 - 달의 눈을 장착하자";
        if (Current.tutorial) return "빛나는 별에서 시작해서, 흐린 선을 손 떼지 말고 한 번에 따라 그려 보세요.";
        return "흐린 선을 따라 성좌를 그려 보자. 어디서 시작할지가 중요하다.";
    }

    private int MissingSocketCount()
    {
        int missing = 0;
        foreach (Vector2Int c in Current.needSockets) if (!litSockets.Contains(c)) missing++;
        return missing;
    }

    // ================= 외부 호출 =================
    private void HandleStroke(List<string> stroke)
    {
        Submit();
    }

    public void Undo()
    {
        if (busy || IsCleared) return;
        drawer.UndoLastStroke();
    }

    // 별 조각을 꺼진 별에 끼움 (나중에 아이템 시스템과 연결)
    public bool InsertFragment(Vector2Int coord)
    {
        if (litSockets.Contains(coord)) return false;
        litSockets.Add(coord);
        if (board != null && board.IsBuilt) board.SetStarLit(coord);
        Message($"별 조각을 끼웠다 - ({coord.x},{coord.y}) 별에 빛이 들어왔다");
        return true;
    }

    public void ShowStartHint()
    {
        if (guide != null) guide.ShowStartMarkers(StarGraph.OddDegreeStars(answer));
    }

    private bool IsMoonEyeMounted()
    {
        return GameManager.Instance != null && GameManager.Instance.HasFlag(moonEyeFlag);
    }

    private void Message(string text)
    {
        Debug.Log($"[성도] {text}");
        OnMessage?.Invoke(text);
    }

    // ================= 개발용 치트 (Inspector 우클릭) =================
    [ContextMenu("치트: 별 조각 1 삽입 (3,5)")]
    private void CheatFragment1() => InsertFragment(new Vector2Int(3, 5));

    [ContextMenu("치트: 별 조각 2 삽입 (3,3)")]
    private void CheatFragment2() => InsertFragment(new Vector2Int(3, 3));

    [ContextMenu("치트: 별 조각 3 삽입 (5,1)")]
    private void CheatFragment3() => InsertFragment(new Vector2Int(5, 1));

    [ContextMenu("치트: 달의 눈 장착")]
    private void CheatMoonEye()
    {
        GameManager.Instance.SetFlag(moonEyeFlag);
        if (board != null && board.IsBuilt) ApplyStageVisuals();
    }

    [ContextMenu("힌트: 시작 별 표시")]
    private void CheatStartHint() => ShowStartHint();

    // ================= 기본 단계 데이터 (기획서 5.2) =================
    private static List<ConstellationStage> DefaultStages()
    {
        return new List<ConstellationStage>
        {
            MakeStage("메스케티우", 1, false, true, "F_HO_CHART_1", "메스케티우 완성! 매의 깃털을 얻었다",
                new Vector2Int[0],
                new[] { "1,5-1,4", "1,4-2,4", "2,4-2,5", "2,5-1,5", "2,5-3,4", "3,4-4,4", "4,4-5,3" }),
            MakeStage("사흐", 1, false, false, "F_HO_CHART_2", "사흐 완성! 은빛 실을 얻었다",
                new[] { new Vector2Int(3, 5) },
                new[] { "2,5-3,5", "3,5-4,5", "2,5-2,4", "4,5-4,4", "2,4-3,4", "3,4-4,4", "2,4-2,3", "2,3-3,2", "3,2-4,3", "4,3-4,4" }),
            MakeStage("소프데트", 1, false, false, "F_HO_CHART_3", "소프데트 완성! 매 인장을 얻었다",
                new[] { new Vector2Int(3, 5), new Vector2Int(3, 3) },
                new[] { "3,5-4,4", "4,4-5,3", "5,3-4,2", "4,2-3,1", "3,1-2,2", "2,2-1,3", "1,3-2,4", "2,4-3,5", "3,5-3,4", "3,4-3,3", "3,3-3,2", "3,2-3,1" }),
            MakeStage("우자트", 3, true, false, "F_HO_CHART_4", "우자트 완성! 성좌 4개가 모두 빛난다",
                new[] { new Vector2Int(3, 3), new Vector2Int(5, 1) },
                new[] { "1,3-2,4", "2,4-3,4", "3,4-4,4", "4,4-5,3", "5,3-4,2", "4,2-3,2", "3,2-2,2", "2,2-1,3", "3,4-3,3", "3,3-3,2", "2,2-2,1", "4,2-5,1" })
        };
    }

    private static ConstellationStage MakeStage(string name, int strokes, bool silver, bool tutorial,
        string flag, string message, Vector2Int[] sockets, string[] edges)
    {
        return new ConstellationStage
        {
            displayName = name,
            maxStrokes = strokes,
            requireSilver = silver,
            tutorial = tutorial,
            clearFlag = flag,
            clearMessage = message,
            needSockets = new List<Vector2Int>(sockets),
            edges = new List<string>(edges)
        };
    }
}