using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

// 소베크 칸 퍼즐 한 판 (서브 S1~S4, 메인 구역 모두 이 컴포넌트 하나)
// - 규칙: SobekBoardLogic (순수 C#), 그림: SobekBoardView, 이동: SobekGridMover (플레이어에 자동 부착)
// - 판의 왼쪽 아래 모서리 = 이 오브젝트 위치, 1칸 = 1 유닛(32px)
// - 들어가기: SobekBoardEntrance(E) 또는 Enter() 직접 호출 (메인)
// - 풀면: 플래그 + 보상 팝업 -> Cleared 이벤트 (메인 흐름이 다음 구역으로 넘김)
// v2: Z 되돌리기 / 막힘 자동 감지(더 풀 수 없는 수를 두면 한 걸음 자동 되돌림) / 왼쪽 위 안내 패널
//     섬 회전·물 높이 애니메이션 / 알이 물에 빠지면 처음으로가 아니라 그 수만 취소
[DisallowMultipleComponent]
[RequireComponent(typeof(SobekBoardView))]
public class SobekBoard : MonoBehaviour
{
    [Header("퍼즐 데이터")]
    [SerializeField] private SobekBoardData data;

    [Header("플레이어 위치 보정")]
    [Tooltip("플레이어 그림 기준점이 발밑이면 칸 가운데보다 조금 아래로 (예: y -0.3)")]
    [SerializeField] private Vector2 playerOffset = Vector2.zero;

    [Header("연출 시간")]
    [Range(0.1f, 2f)] [SerializeField] private float rotateTime = 0.55f;
    [Range(0.2f, 3f)] [SerializeField] private float waterTime = 1.4f;
    [Range(0.05f, 2f)] [SerializeField] private float mirrorFlipTime = 0.6f;
    [Range(0.1f, 2f)] [SerializeField] private float sinkTime = 0.6f;

    [Header("막힘 처리 (난이도)")]
    [Tooltip("더 이상 풀 수 없는 상태가 되면 자동으로 한 걸음 되돌림 (쉬움용. 기본 끔)")]
    [SerializeField] private bool autoUndoDeadlock = false;
    [Range(0.3f, 3f)] [SerializeField] private float deadlockDelay = 1.2f;
    [Tooltip("막힌 뒤 이 시간(초) 동안 계속 막혀 있어야 알림이 뜸. 0이면 알림 없음 (스스로 알아채야 함)")]
    [Range(0f, 120f)] [SerializeField] private float deadlockNoticeDelay = 40f;
    [Tooltip("거울 발판 근처에서 반대편 세계를 미리 보여 줌 (쉬움용. 기본 끔)")]
    [SerializeField] private bool showMirrorGhost = false;
    [Tooltip("되돌리기 최대 기록 수")]
    [Range(20, 1000)] [SerializeField] private int historyLimit = 300;

    [Header("화면 흔들림 (비우면 Main Camera에서 찾음)")]
    [SerializeField] private CameraFollow2D cameraFollow;

    [Header("풀었을 때")]
    [Tooltip("보상 팝업을 닫으면 자동으로 입구로 나감 (서브 퍼즐). 메인은 끔")]
    [SerializeField] private bool exitAfterClear = true;
    [SerializeField] private UnityEvent onCleared;

    public event Action<SobekBoard> Cleared;   // 메인 흐름(SobekMainFlow)이 구독
    public event Action<SobekBoard> Entered;
    public event Action<SobekBoard> Exited;

    private SobekBoardLogic logic;
    private SobekBoardView view;
    private SobekGridMover mover;
    private SobekHud hud;
    private bool active;
    private bool busy;
    private bool solved;
    private int resets;
    private int steps;
    private bool hintShown;
    private float timeLeft;
    private int lastWarn;
    private int lastHudSecond = -1;
    private readonly List<SobekBoardLogic.Snap> history = new List<SobekBoardLogic.Snap>();

    // 막힘 판정 (백그라운드에서 한 번 계산)
    private Task<HashSet<string>> aliveTask;
    private HashSet<string> alive;

    public SobekBoardData Data => data;
    public SobekBoardLogic Logic => logic;
    public bool IsActive => active;
    public bool IsBusy => busy || (mover != null && mover.IsStepping);
    public bool IsSolved => solved || (data != null && !string.IsNullOrEmpty(data.ClearFlag) && HasFlag(data.ClearFlag));
    public float TimeLeft => timeLeft;

    // ================= 초기화 =================
    private void Awake()
    {
        view = GetComponent<SobekBoardView>();
        if (data == null)
        {
            Debug.LogError($"[SobekBoard] {name}: Board Data가 비어 있음", this);
            enabled = false;
            return;
        }
        logic = new SobekBoardLogic(data.ToSpec());
        view.Build(logic, data);

        // 막힘 판정용 상태 그래프는 무거울 수 있어 다른 스레드에서 계산 (메인 스레드 멈춤 없음)
        SobekBoardSpec spec = data.ToSpec();
        aliveTask = Task.Run(() =>
        {
            var l = new SobekBoardLogic(spec);
            return l.ComputeAliveStates(out _);
        });
    }

    private void Start()
    {
        if (cameraFollow == null && Camera.main != null) cameraFollow = Camera.main.GetComponent<CameraFollow2D>();
    }

    // ================= 좌표 =================
    // 화면에 보이는 칸(거울 반영) -> 월드 위치
    public Vector2 ViewCellToWorld(SobekCell viewCell)
    {
        int h = logic != null ? logic.Height : (data != null ? data.Height : 0);
        Vector3 local = new Vector3(viewCell.X + 0.5f, (h - 1 - viewCell.Y) + 0.5f, 0f);
        return (Vector2)transform.TransformPoint(local) + playerOffset;
    }

    public Vector2 EntranceWorld => ViewCellToWorld(new SobekCell(data.Entrance.x, data.Entrance.y));

    // ================= 들어가기 / 나가기 =================
    public bool Enter(GameObject player)
    {
        if (logic == null || player == null) return false;
        if (active) return true;
        if (IsSolved && data.AllowExit)
        {
            AnubisToast.Show($"{data.DisplayName}: 이미 풀었다.", 2f);
            return false;
        }

        mover = player.GetComponent<SobekGridMover>();
        if (mover == null) mover = player.AddComponent<SobekGridMover>();

        logic.Reset();
        history.Clear();
        steps = 0;
        view.Refresh();
        active = true;
        busy = false;
        timeLeft = data.TimeLimit;
        lastWarn = int.MaxValue;
        mover.Attach(this, ViewCellToWorld(logic.PlayerView));
        hud = SobekHud.Get(view.LabelFont);
        UpdateHud();
        UpdateGhost();

        if (!string.IsNullOrEmpty(data.Intro)) AnubisToast.Show($"{data.DisplayName}: {data.Intro}", 5f);
        Debug.Log($"[SobekBoard] {data.DisplayName} 들어감", this);
        Entered?.Invoke(this);
        return true;
    }

    public void Exit()
    {
        if (!active) return;
        active = false;
        busy = false;
        StopAllCoroutines();
        if (mover != null) mover.Detach(EntranceWorld);
        logic.Reset();
        history.Clear();
        view.Refresh();
        view.SetGhost(false);
        if (hud != null) hud.Hide();
        Debug.Log($"[SobekBoard] {data.DisplayName} 나옴", this);
        Exited?.Invoke(this);
    }

    // 메인 흐름용: 플레이어를 놓아주지 않고 판만 끔 (다음 판 Enter가 이어받음)
    public void Release()
    {
        active = false;
        busy = false;
        StopAllCoroutines();
        view.SetGhost(false);
    }

    // ================= 걷기 =================
    public void RequestStep(int dir)
    {
        if (!active || IsBusy) return;
        SobekBoardLogic.Snap before = logic.Snapshot();
        SobekBoardLogic.StepResult r = logic.TryStep(dir);
        switch (r.Outcome)
        {
            case SobekBoardLogic.Outcome.Blocked:
                return;
            case SobekBoardLogic.Outcome.Exit:
                Exit();
                return;
            case SobekBoardLogic.Outcome.EggSunk:
                StartCoroutine(EggSunkRoutine(r));
                return;
            default:
                history.Add(before);
                if (history.Count > historyLimit) history.RemoveAt(0);
                steps++;
                StartCoroutine(StepRoutine(r, before));
                return;
        }
    }

    private IEnumerator StepRoutine(SobekBoardLogic.StepResult r, SobekBoardLogic.Snap before)
    {
        busy = true;
        float stepTime = mover != null ? mover.StepTime : 0.16f;

        // 화면 칸은 거울을 밟아도 그대로 (세계만 뒤집힘) -> 바뀐 뒤 PlayerView로 걸어가면 됨
        if (mover != null) mover.StepTo(ViewCellToWorld(logic.PlayerView));
        if (r.PushedEgg >= 0) StartCoroutine(view.MoveEgg(r.PushedEgg, r.EggFrom, r.EggTo, stepTime));
        yield return new WaitForSeconds(stepTime);
        while (mover != null && mover.IsStepping) yield return null;

        if (r.HasPlate && r.PlateKind == SobekPlateKind.Rotate && logic.TryGetPlate(r.To, out SobekBoardSpec.Plate rp))
        {
            Shake(rotateTime, 0.05f);
            yield return view.RotateIslands(rp.Spins, before.Rots, before.Eggs, rotateTime);
        }
        else if (r.HasPlate && r.PlateKind == SobekPlateKind.Water)
        {
            bool up = logic.Level > r.LevelBefore;
            AnubisToast.Show(up ? $"물이 차오른다 ▲  ({LevelName(logic.Level)})" : $"물이 빠진다 ▼  ({LevelName(logic.Level)})", 2f);
            Shake(waterTime, 0.04f);
            yield return view.AnimateWater(r.LevelBefore, logic.Level, waterTime);
        }
        else if (r.HasPlate && r.PlateKind == SobekPlateKind.Mirror)
        {
            yield return view.Flip(logic.Mirrored, mirrorFlipTime); // 뒤집는 동안은 예전 그림
            view.Refresh();
        }
        else view.Refresh();

        UpdateGhost();
        UpdateHud();
        busy = false;

        if (r.Outcome == SobekBoardLogic.Outcome.Goal) { yield return ClearRoutine(); yield break; }
        if (IsDeadNow()) yield return DeadlockRoutine();
    }

    private IEnumerator EggSunkRoutine(SobekBoardLogic.StepResult r)
    {
        busy = true;
        yield return view.SinkEgg(r.SunkEgg, r.SunkAt, sinkTime);
        AnubisToast.Show("알이 물에 빠질 뻔했다! 그 수는 취소된다.", 2f);
        yield return new WaitForSeconds(0.25f);
        view.Refresh(); // 상태는 그대로 -> 알이 제자리로
        busy = false;
    }

    private bool IsDeadNow()
    {
        if (alive == null && aliveTask != null && aliveTask.IsCompleted)
        {
            if (aliveTask.Status == TaskStatus.RanToCompletion) alive = aliveTask.Result;
            aliveTask = null;
        }
        return alive != null && !alive.Contains(logic.StateKey());
    }

    private IEnumerator DeadlockRoutine()
    {
        busy = true;
        if (autoUndoDeadlock)
        {
            AnubisToast.Show("이렇게 하면 더 이상 풀 수 없다... 한 걸음 되돌린다.", deadlockDelay + 1f);
            yield return new WaitForSeconds(deadlockDelay);
            busy = false;
            Undo(false);
        }
        else
        {
            busy = false;
            // 바로 알려 주지 않음: 오래 막혀 있을 때만 조용히 알림
            if (deadlockNoticeDelay > 0f) StartCoroutine(LateDeadlockNotice(logic.StateKey()));
        }
        Debug.Log($"[SobekBoard] {data.DisplayName}: 막힌 상태 감지", this);
    }

    private IEnumerator LateDeadlockNotice(string key)
    {
        yield return new WaitForSeconds(deadlockNoticeDelay);
        // 그사이 움직였거나 되돌렸으면 알리지 않음
        if (!active || logic.StateKey() != key) yield break;
        AnubisToast.Show("강물이 멈춘 것 같다... Z로 되돌리거나 R로 처음부터.", 3f);
    }

    private string LevelName(int lv)
    {
        int n = logic.Spec.LevelCount;
        if (n >= 3) return lv == 0 ? "낮음" : (lv == 1 ? "중간" : "높음");
        return lv == 0 ? "낮음" : "높음";
    }

    // ================= 되돌리기 / 처음으로 =================
    public void UndoByPlayer()
    {
        if (!active || IsBusy) return;
        if (history.Count == 0) { AnubisToast.Show("더 되돌릴 수 없다.", 1f); return; }
        Undo(true);
    }

    private void Undo(bool fromPlayer)
    {
        if (history.Count == 0) return;
        SobekBoardLogic.Snap s = history[history.Count - 1];
        history.RemoveAt(history.Count - 1);
        logic.Restore(s);
        steps = Mathf.Max(0, steps - 1);
        view.Refresh();
        if (mover != null) mover.Teleport(ViewCellToWorld(logic.PlayerView));
        UpdateGhost();
        UpdateHud();
        if (fromPlayer) Debug.Log($"[SobekBoard] 되돌리기 (남은 기록 {history.Count})", this);
    }

    public void ResetByPlayer()
    {
        if (!active || IsBusy) return;
        AnubisToast.Show("처음으로 돌아간다.", 1.2f);
        ResetBoard();
    }

    private void ResetBoard()
    {
        logic.Reset();
        history.Clear();
        steps = 0;
        view.Refresh();
        if (mover != null) mover.Teleport(ViewCellToWorld(logic.PlayerView));
        timeLeft = data.TimeLimit;
        lastWarn = int.MaxValue;
        resets++;
        UpdateGhost();
        UpdateHud();
        if (!hintShown && data.HintAfterResets > 0 && resets >= data.HintAfterResets && !string.IsNullOrEmpty(data.Hint))
        {
            hintShown = true;
            AnubisToast.Show("힌트: " + data.Hint, 6f);
        }
    }

    // ================= 안내 =================
    private void UpdateGhost()
    {
        if (!logic.HasMirror) return;
        if (!showMirrorGhost) { view.SetGhost(false); return; }
        bool near = false;
        foreach (var p in logic.Spec.Plates)
        {
            if (p.Kind != SobekPlateKind.Mirror) continue;
            int d = Mathf.Abs(p.Cell.X - logic.PlayerReal.X) + Mathf.Abs(p.Cell.Y - logic.PlayerReal.Y);
            if (d <= 1) { near = true; break; }
        }
        view.SetGhost(near);
    }

    private void UpdateHud()
    {
        if (hud == null || !active) return;
        var sb = new System.Text.StringBuilder();
        sb.Append("<b>").Append(data.DisplayName).Append("</b>\n");
        sb.Append("<size=80%>방향키 이동 · <color=#FFD27A>Z</color> 되돌리기 · <color=#FFD27A>R</color> 처음으로");
        if (data.AllowExit) sb.Append(" · 판 밖으로 걸으면 나가기");
        sb.Append("</size>\n<size=85%>걸음 ").Append(steps);
        if (logic.Spec.LevelCount > 1)
        {
            sb.Append("   수위 ");
            for (int i = 0; i < logic.Spec.LevelCount; i++) sb.Append(i <= logic.Level ? "■" : "□");
            sb.Append(' ').Append(LevelName(logic.Level));
        }
        if (logic.HasMirror) sb.Append(logic.Mirrored ? "   <color=#7FF2E0>거울 세계</color>" : "   진짜 세계");
        if (data.TimeLimit > 0f)
        {
            int s = Mathf.Max(0, Mathf.CeilToInt(timeLeft));
            sb.Append("   횃불 ").Append(s / 60).Append(':').Append((s % 60).ToString("00"));
        }
        sb.Append("</size>");
        hud.Show(sb.ToString());
    }

    // ================= 제한 시간 =================
    private void Update()
    {
        if (!active || busy || data.TimeLimit <= 0f || PlayerLock.IsLocked) return;
        timeLeft -= Time.deltaTime;
        int sec = Mathf.CeilToInt(timeLeft);
        if (sec != lastHudSecond) { lastHudSecond = sec; UpdateHud(); }
        if ((sec == 60 || sec == 30 || sec == 10) && sec < lastWarn)
        {
            lastWarn = sec;
            AnubisToast.Show($"횃불이 {sec}초 남았다.", 2f);
        }
        if (timeLeft <= 0f)
        {
            AnubisToast.Show("횃불이 꺼졌다... 이 구역을 처음부터.", 2.5f);
            ResetBoard();
        }
    }

    // ================= 클리어 =================
    private IEnumerator ClearRoutine()
    {
        busy = true;
        solved = true;
        if (!string.IsNullOrEmpty(data.ClearFlag) && GameManager.Instance != null) GameManager.Instance.SetFlag(data.ClearFlag);
        Shake(0.4f, 0.1f);
        Debug.Log($"[SobekBoard] {data.DisplayName} 클리어 ({steps}걸음)", this);

        if (!string.IsNullOrEmpty(data.RewardItemId))
        {
            bool closed = false;
            ItemPopup.Grant(data.RewardItemId, () => closed = true);
            while (!closed) yield return null;
        }

        busy = false;
        onCleared?.Invoke();
        Cleared?.Invoke(this);
        if (exitAfterClear && data.AllowExit) Exit();
    }

    private void Shake(float duration, float strength)
    {
        if (cameraFollow != null) cameraFollow.Shake(duration, strength);
    }

    private static bool HasFlag(string f) => GameManager.Instance != null && GameManager.Instance.HasFlag(f);

    // ================= 테스트 =================
    [ContextMenu("정답 최단 걸음 확인 (BFS)")]
    private void CheckSolution()
    {
        if (data == null) return;
        var l = new SobekBoardLogic(data.ToSpec());
        List<int> path = l.Solve();
        if (path == null) { Debug.LogWarning($"[SobekBoard] {data.DisplayName}: 정답 없음! 지형/장치를 확인", this); return; }
        var sb = new System.Text.StringBuilder();
        foreach (int d in path) sb.Append("↑→↓←"[d]);
        Debug.Log($"[SobekBoard] {data.DisplayName}: 최단 {path.Count}걸음\n{sb}", this);
    }

    [ContextMenu("정답까지 자동 진행 (Play 중, 판 안에서)")]
    private void AutoSolve()
    {
        if (!Application.isPlaying || !active) { Debug.Log("[SobekBoard] Play 중에 판에 들어간 뒤 눌러 주세요", this); return; }
        List<int> path = logic.Solve();
        if (path == null) { Debug.LogWarning("[SobekBoard] 지금 상태에서 정답 없음 -> R로 처음부터", this); return; }
        StartCoroutine(AutoRoutine(path));
    }

    private IEnumerator AutoRoutine(List<int> path)
    {
        foreach (int d in path)
        {
            while (IsBusy || PlayerLock.IsLocked) yield return null;
            if (!active) yield break;
            RequestStep(d);
            yield return null;
        }
    }

    [ContextMenu("퍼즐 리셋")]
    private void TestReset()
    {
        if (Application.isPlaying && active) ResetBoard();
    }

    // ================= Scene 뷰 표시 =================
    private void OnDrawGizmos()
    {
        if (data == null) return;
        string[] rows = data.Rows;
        int h = rows.Length;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < rows[y].Length; x++)
            {
                Gizmos.color = GizmoColor(rows[y][x]);
                Vector3 c = transform.TransformPoint(new Vector3(x + 0.5f, h - 1 - y + 0.5f, 0f));
                Gizmos.DrawCube(c, new Vector3(0.9f, 0.9f, 0f));
            }
        }
        Gizmos.color = new Color(1f, 0.8f, 0.2f);
        Gizmos.DrawWireCube(CellGizmo(data.Entrance, h), Vector3.one * 0.8f);

        // 승강 발판 -> 같은 글자 다리 연결선
        Gizmos.color = new Color(0.85f, 0.35f, 0.19f);
        foreach (var p in data.Plates)
        {
            if (p.kind != SobekPlateKind.Lift) continue;
            foreach (var b in data.Bridges)
                if (b.id == p.bridgeId) Gizmos.DrawLine(CellGizmo(p.cell, h), CellGizmo(b.cell, h));
        }
    }

    private Vector3 CellGizmo(Vector2Int c, int h) => transform.TransformPoint(new Vector3(c.x + 0.5f, h - 1 - c.y + 0.5f, 0f));

    private static Color GizmoColor(char t)
    {
        switch (t)
        {
            case '#': return new Color(0.3f, 0.3f, 0.3f, 0.35f);
            case '~': return new Color(0.3f, 0.6f, 0.9f, 0.35f);
            case 's': return new Color(0.6f, 0.6f, 0.55f, 0.35f);
            case 'F': return new Color(0.5f, 0.35f, 0.2f, 0.35f);
            default: return new Color(0.9f, 0.8f, 0.6f, 0.25f);
        }
    }

#if UNITY_EDITOR
    public void EditorSetup(SobekBoardData boardData, bool exitWhenCleared)
    {
        data = boardData;
        exitAfterClear = exitWhenCleared;
    }
#endif
}
