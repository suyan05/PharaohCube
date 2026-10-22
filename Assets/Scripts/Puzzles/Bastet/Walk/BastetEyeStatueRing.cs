using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// B2 고양이의 눈 (걸어서 푸는 버전)
// 1) 큰 고양이 석상 앞에서 E -> 작은 고양이 석상들의 눈이 순서대로 빛남
// 2) 플레이어가 석상 앞으로 걸어가 E. 마지막에 빛난 석상부터 거꾸로
// 3) 틀리면 그 석상이 횃불을 먹음 (흔들림 + 빨간 눈) -> 횃불 꺼짐 + 처음부터
// 같은 묶음(동시 점멸)의 석상은 아무 순서로 눌러도 됨
public class BastetEyeStatueRing : BastetWalkPuzzleBase, IBastetStatueOwner
{
    [System.Serializable]
    public class EyeGroup
    {
        [Tooltip("같이 빛나는 석상 번호. 2개 이상이면 동시 점멸")]
        public int[] statues = { 0 };
        public EyeGroup() { }
        public EyeGroup(params int[] s) { statues = s; }
    }

    private enum State { Idle, Showing, Input, Cleared }

    [Header("작은 고양이 석상 (배열 번호 = 석상 번호)")]
    [SerializeField] private BastetEyeStatue[] statues = new BastetEyeStatue[0];

    [Header("난이도")]
    [Tooltip("눈이 빛나는 순서 (위에서부터). 정답은 이 순서의 역순")]
    [SerializeField] private EyeGroup[] forwardSteps = { new EyeGroup(0), new EyeGroup(3), new EyeGroup(1), new EyeGroup(4) };
    [Range(0.2f, 1.5f)] [SerializeField] private float showOnTime = 0.6f;
    [Range(0.05f, 1f)] [SerializeField] private float showGapTime = 0.25f;
    [Range(0f, 2f)] [SerializeField] private float beforeShowDelay = 0.5f;
    [Range(0.2f, 2f)] [SerializeField] private float eatTime = 0.8f;

    [Header("색")]
    [SerializeField] private Color showColor = new Color(0.35f, 0.9f, 1f);   // 청록 눈빛
    [SerializeField] private Color doneColor = new Color(1f, 0.85f, 0.25f);
    [SerializeField] private Color wrongColor = new Color(0.95f, 0.25f, 0.2f);

    [Header("문구")]
    [SerializeField] private string showMessage = "고양이 석상들의 눈이 차례로 빛난다. 잘 봐 두자.";
    [SerializeField] private string inputMessage = "고양이는 뒤를 돌아본다. 마지막에 빛난 석상부터 거꾸로 눌러라.";
    [SerializeField] private string wrongMessage = "고양이 석상이 횃불을 삼켰다!";
    [SerializeField] private string idleMessage = "석상의 눈이 감겨 있다. 큰 고양이 석상을 먼저 깨우자.";
    [SerializeField] private string clearMessage = "고양이들이 고개를 끄덕였다!";

    private State state = State.Idle;
    private int step;                                   // 지금 입력할 묶음 (역순 기준)
    private readonly HashSet<int> pending = new HashSet<int>(); // 이번 묶음에서 아직 안 누른 석상
    private bool[] lit;                                  // 이번 도전에서 금색으로 켜진 석상
    private WaitForSeconds waitOn, waitGap, waitBefore, waitEat;

    private int StepCount => forwardSteps != null ? forwardSteps.Length : 0;
    private int[] ReverseGroup(int k) => forwardSteps[StepCount - 1 - k].statues;

    public override string StarterPrompt
    {
        get
        {
            if (state == State.Cleared) return "큰 고양이 석상 (통과)";
            if (state == State.Showing) return "";
            if (state == State.Input) return "E: 눈빛 다시 보기";
            return "E: 큰 고양이 석상 깨우기";
        }
    }

    private void Awake()
    {
        waitOn = new WaitForSeconds(showOnTime);
        waitGap = new WaitForSeconds(showGapTime);
        waitBefore = new WaitForSeconds(beforeShowDelay);
        waitEat = new WaitForSeconds(eatTime);
        lit = new bool[statues.Length];
        for (int i = 0; i < statues.Length; i++)
        {
            if (statues[i] != null) statues[i].Init(this, i);
            else Debug.LogError($"[{name}] 석상 {i}번이 비어 있음", this);
        }
        AllOff();
    }

    // ================= 진행 =================
    public override void Begin()
    {
        if (state == State.Cleared) { Toast("이미 통과한 시험이다."); return; }
        if (state == State.Showing) return;
        StopAllCoroutines();
        SetSolving(true); // [추가] 횃불 타는 시간 멈춤
        StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        state = State.Showing;
        AllOff();
        Toast(showMessage);
        yield return waitBefore;

        for (int s = 0; s < StepCount; s++)
        {
            foreach (int i in forwardSteps[s].statues) if (Valid(i)) statues[i].SetEyes(true, showColor);
            yield return waitOn;
            foreach (int i in forwardSteps[s].statues) if (Valid(i)) statues[i].SetEyes(false, showColor);
            yield return waitGap;
        }

        step = 0;
        LoadPending();
        state = State.Input;
        Toast(inputMessage);
    }

    private void LoadPending()
    {
        pending.Clear();
        if (step < StepCount) foreach (int i in ReverseGroup(step)) pending.Add(i);
    }

    // ================= IBastetStatueOwner =================
    public string PromptFor(int index)
    {
        if (state == State.Input) return lit[index] ? "" : "E: 고양이 석상의 눈 누르기";
        if (state == State.Idle) return "E: 고양이 석상 살펴보기";
        return "";
    }

    public void OnStatuePressed(int index)
    {
        if (state == State.Idle) { Toast(idleMessage); return; }
        if (state != State.Input || lit[index]) return; // 이미 금색으로 켠 석상은 무시

        if (!pending.Contains(index))
        {
            Debug.Log($"[BastetB2] 틀림: {step + 1}번째 묶음에 {index} 누름", this);
            StartCoroutine(FailRoutine(index));
            return;
        }

        pending.Remove(index);
        lit[index] = true;
        statues[index].SetEyes(true, doneColor);

        if (pending.Count > 0) return;
        step++;
        if (step >= StepCount) { Submit(); return; }
        LoadPending();
    }

    private IEnumerator FailRoutine(int index)
    {
        state = State.Showing;
        statues[index].EatTorch(wrongColor, eatTime);
        PunishTorch(wrongMessage);
        yield return waitEat;
        AllOff();
        state = State.Idle;
        SetSolving(false); // [추가]
    }

    // ================= PuzzleBase =================
    public override void Submit()
    {
        if (Validate()) OnSuccessInternal();
    }

    protected override bool Validate() => step >= StepCount;

    protected override void OnSuccessInternal()
    {
        if (IsCleared) return;
        base.OnSuccessInternal();
        SetSolving(false); // [추가]
        state = State.Cleared;
        for (int i = 0; i < statues.Length; i++) if (statues[i] != null) statues[i].SetEyes(true, doneColor);
        Toast(clearMessage);
    }

    public override void Reset()
    {
        if (!Application.isPlaying) return; // 에디터 Reset 가드
        base.Reset();
        if (IsCleared) return;
        StopAllCoroutines();
        state = State.Idle;
        SetSolving(false); // [추가]
        AllOff();
    }

    private void AllOff()
    {
        if (lit != null) for (int i = 0; i < lit.Length; i++) lit[i] = false;
        foreach (var s in statues) if (s != null) s.SetEyes(false, showColor);
    }

    private bool Valid(int i) => i >= 0 && i < statues.Length && statues[i] != null;

    [ContextMenu("테스트: 바로 클리어")]
    private void TestClear()
    {
        if (!Application.isPlaying) return;
        StopAllCoroutines();
        step = StepCount;
        Submit();
    }

    // Scene 뷰: 누를 순서(역순)를 선으로
    private void OnDrawGizmosSelected()
    {
        if (statues == null || forwardSteps == null) return;
        Gizmos.color = new Color(0.35f, 0.9f, 1f);
        Vector3? prev = null;
        for (int k = 0; k < StepCount; k++)
        {
            int[] g = ReverseGroup(k);
            if (g == null || g.Length == 0 || !Valid(g[0])) continue;
            Vector3 p = statues[g[0]].transform.position;
            Gizmos.DrawWireSphere(p, 0.3f + k * 0.08f);
            if (prev.HasValue) Gizmos.DrawLine(prev.Value, p);
            prev = p;
        }
    }

#if UNITY_EDITOR
    public void EditorSetStatues(BastetEyeStatue[] s) => statues = s;
#endif
}
