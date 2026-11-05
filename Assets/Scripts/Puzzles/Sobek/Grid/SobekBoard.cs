using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// 소베크 칸 퍼즐 한 판 (서브 S1~S4, 메인 A·B 모두 이 컴포넌트 하나)
// - 규칙: SobekBoardLogic (순수 C#), 그림: SobekBoardView, 이동: SobekGridMover (플레이어에 자동 부착)
// - 판의 왼쪽 아래 모서리 = 이 오브젝트 위치, 1칸 = 1 유닛(32px)
// - 들어가기: SobekBoardEntrance(E) 또는 Enter() 직접 호출 (메인)
// - 풀면: 플래그 + 보상 팝업 -> Cleared 이벤트 (메인 흐름이 다음 구역으로 넘김)
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
    [Range(0.05f, 1f)] [SerializeField] private float rotateTime = 0.25f;
    [Range(0.05f, 2f)] [SerializeField] private float waterTime = 0.5f;
    [Range(0.05f, 2f)] [SerializeField] private float mirrorFlipTime = 0.6f;
    [Range(0.1f, 2f)] [SerializeField] private float sinkTime = 0.6f;

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
    private bool active;
    private bool busy;
    private bool solved;
    private int resets;
    private bool hintShown;
    private float timeLeft;
    private int lastWarn;

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
    }

    private void Start()
    {
        if (cameraFollow == null && Camera.main != null) cameraFollow = Camera.main.GetComponent<CameraFollow2D>();
    }

    // ================= 좌표 =================
    // 화면에 보이는 칸(거울 반영) -> 월드 위치
    public Vector2 ViewCellToWorld(SobekCell view)
    {
        int h = logic != null ? logic.Height : (data != null ? data.Height : 0);
        Vector3 local = new Vector3(view.X + 0.5f, (h - 1 - view.Y) + 0.5f, 0f);
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
        view.Refresh();
        active = true;
        busy = false;
        timeLeft = data.TimeLimit;
        lastWarn = int.MaxValue;
        mover.Attach(this, ViewCellToWorld(logic.PlayerView));

        string controls = data.AllowExit ? "방향키 이동 · R 처음으로 · 판 밖으로 걸으면 나가기" : "방향키 이동 · R 처음으로";
        AnubisToast.Show(string.IsNullOrEmpty(data.Intro) ? $"{data.DisplayName}\n{controls}" : $"{data.DisplayName}: {data.Intro}\n{controls}", 4f);
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
        view.Refresh();
        Debug.Log($"[SobekBoard] {data.DisplayName} 나옴", this);
        Exited?.Invoke(this);
    }

    // 메인 흐름용: 플레이어를 놓아주지 않고 판만 끔 (다음 판 Enter가 이어받음)
    public void Release()
    {
        active = false;
        busy = false;
        StopAllCoroutines();
    }

    // ================= 걷기 =================
    public void RequestStep(int dir)
    {
        if (!active || IsBusy) return;
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
                StartCoroutine(StepRoutine(r));
                return;
        }
    }

    private IEnumerator StepRoutine(SobekBoardLogic.StepResult r)
    {
        busy = true;
        float stepTime = mover != null ? mover.StepTime : 0.16f;

        // 화면 칸은 거울을 밟아도 그대로 (세계만 뒤집힘) -> 바뀐 뒤 PlayerView로 걸어가면 됨
        if (mover != null) mover.StepTo(ViewCellToWorld(logic.PlayerView));
        if (r.PushedEgg >= 0) StartCoroutine(view.MoveEgg(r.PushedEgg, r.EggFrom, r.EggTo, stepTime));
        yield return new WaitForSeconds(stepTime);
        while (mover != null && mover.IsStepping) yield return null;

        bool mirrorStep = r.HasPlate && r.PlateKind == SobekPlateKind.Mirror;
        if (mirrorStep) yield return view.Flip(logic.Mirrored, mirrorFlipTime); // 뒤집는 동안은 예전 그림
        view.Refresh();

        if (r.HasPlate && r.PlateKind == SobekPlateKind.Rotate)
        {
            Shake(0.2f, 0.06f);
            yield return new WaitForSeconds(rotateTime);
        }
        else if (r.HasPlate && r.PlateKind == SobekPlateKind.Water)
        {
            AnubisToast.Show(WaterText(), 1.5f);
            yield return new WaitForSeconds(waterTime);
        }
        busy = false;

        if (r.Outcome == SobekBoardLogic.Outcome.Goal) yield return ClearRoutine();
    }

    private IEnumerator EggSunkRoutine(SobekBoardLogic.StepResult r)
    {
        busy = true;
        SobekCell to = r.SunkEgg == r.PushedEgg ? r.EggTo : logic.Eggs[r.SunkEgg];
        yield return view.SinkEgg(r.SunkEgg, to, sinkTime);
        AnubisToast.Show("돌 악어알이 가라앉았다... 처음으로 돌아간다.", 2f);
        yield return new WaitForSeconds(0.4f);
        ResetBoard();
        busy = false;
    }

    private string WaterText()
    {
        if (logic.Spec.LevelCount >= 3) return logic.Level == 0 ? "수위: 낮음" : (logic.Level == 1 ? "수위: 중간" : "수위: 높음");
        return logic.Level == 0 ? "물이 빠졌다 (낮음)" : "물이 차올랐다 (높음)";
    }

    // ================= 처음으로 =================
    public void ResetByPlayer()
    {
        if (!active || busy) return;
        AnubisToast.Show("처음으로 돌아간다.", 1.2f);
        ResetBoard();
    }

    private void ResetBoard()
    {
        logic.Reset();
        view.Refresh();
        if (mover != null) mover.Teleport(ViewCellToWorld(logic.PlayerView));
        timeLeft = data.TimeLimit;
        lastWarn = int.MaxValue;
        resets++;
        if (!hintShown && data.HintAfterResets > 0 && resets >= data.HintAfterResets && !string.IsNullOrEmpty(data.Hint))
        {
            hintShown = true;
            AnubisToast.Show("힌트: " + data.Hint, 5f);
        }
    }

    // ================= 제한 시간 =================
    private void Update()
    {
        if (!active || busy || data.TimeLimit <= 0f || PlayerLock.IsLocked) return;
        timeLeft -= Time.deltaTime;
        int sec = Mathf.CeilToInt(timeLeft);
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
        Debug.Log($"[SobekBoard] {data.DisplayName} 클리어", this);

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
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(CellGizmo(data.Entrance, h), 0.35f);
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
