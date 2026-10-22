using System.Collections;
using UnityEngine;

// B1 고양이 발자국 (걸어서 푸는 버전)
// 1) 큰 고양이 석상 앞에서 E -> 석상이 앞발을 휘두름
// 2) 석상 앞 발자국들이 순서대로 잠깐 빛났다 꺼짐
// 3) 플레이어가 같은 순서로 발자국을 밟음 (밟을 때 금색으로 남음)
// 4) 틀린 발자국을 밟으면 빨갛게 깜빡 + 횃불이 꺼짐 + 처음부터 (석상을 다시 쳐서 다시 보기)
// 붙이는 곳: Walk_B1_Footprint 루트 (Tools/Bastet/걷는 퍼즐 메뉴가 자동 생성)
public class BastetFootprintWalk : BastetWalkPuzzleBase
{
    private enum State { Idle, Showing, Input, Cleared }

    [Header("발자국 판 (배열 번호 = 발자국 번호)")]
    [SerializeField] private BastetFootprintPlate[] plates = new BastetFootprintPlate[0];

    [Header("난이도")]
    [Tooltip("밟아야 할 발자국 번호 순서. 길수록 어려움 (튜토리얼은 4개)")]
    [SerializeField] private int[] sequence = { 4, 1, 2, 5, 3 };
    [Range(0.2f, 1.5f)] [SerializeField] private float showOnTime = 0.6f;
    [Range(0.05f, 1f)] [SerializeField] private float showGapTime = 0.25f;
    [Range(0f, 2f)] [SerializeField] private float beforeShowDelay = 0.5f;
    [Range(0.2f, 2f)] [SerializeField] private float failFlashTime = 0.6f;

    [Header("색")]
    [SerializeField] private Color showColor = new Color(0.75f, 0.45f, 1f);  // 보라 빛 (보여 줄 때)
    [SerializeField] private Color doneColor = new Color(1f, 0.85f, 0.25f);  // 금 (맞게 밟음)
    [SerializeField] private Color wrongColor = new Color(0.95f, 0.25f, 0.2f);

    [Header("문구")]
    [SerializeField] private string showMessage = "고양이 석상이 앞발을 내리친다. 발자국을 잘 봐 두자.";
    [SerializeField] private string inputMessage = "빛났던 순서대로 발자국을 밟자.";
    [SerializeField] private string wrongMessage = "발자국이 흩어졌다! 석상이 횃불을 꺼뜨렸다.";
    [SerializeField] private string clearMessage = "발자국을 끝까지 따라갔다!";

    private State state = State.Idle;
    private int step;
    private BastetFootprintPlate lastPlate;
    private WaitForSeconds waitOn, waitGap, waitBefore, waitFail;

    public override string StarterPrompt
    {
        get
        {
            if (state == State.Cleared) return "고양이 석상 (통과)";
            if (state == State.Showing) return "";
            if (state == State.Input) return "E: 발자국 다시 보기";
            return "E: 고양이 석상의 앞발 깨우기";
        }
    }

    private void Awake()
    {
        waitOn = new WaitForSeconds(showOnTime);
        waitGap = new WaitForSeconds(showGapTime);
        waitBefore = new WaitForSeconds(beforeShowDelay);
        waitFail = new WaitForSeconds(failFlashTime);

        for (int i = 0; i < plates.Length; i++)
        {
            if (plates[i] == null) { Debug.LogError($"[{name}] 발자국 {i}번이 비어 있음", this); continue; }
            plates[i].Index = i;
            plates[i].OnStepped += HandleStep;
        }
        AllOff();
    }

    private void OnDestroy()
    {
        foreach (var p in plates) if (p != null) p.OnStepped -= HandleStep;
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

        foreach (int i in sequence)
        {
            if (!Valid(i)) continue;
            plates[i].SetGlow(true, showColor);
            yield return waitOn;
            plates[i].SetGlow(false, showColor);
            yield return waitGap;
        }

        step = 0;
        lastPlate = null;
        state = State.Input;
        Toast(inputMessage);
        Debug.Log($"[BastetB1] 입력 시작. 정답 = {string.Join(" > ", sequence)}", this);
    }

    private void HandleStep(BastetFootprintPlate plate)
    {
        if (state != State.Input || plate == lastPlate) return; // 같은 발자국 위에서 다시 들어온 것은 무시
        lastPlate = plate;

        int expected = sequence[step];
        if (plate.Index != expected)
        {
            Debug.Log($"[BastetB1] 틀림: {step + 1}번째에 {plate.Index} (정답 {expected})", this);
            StartCoroutine(FailRoutine(plate));
            return;
        }

        plate.SetGlow(true, doneColor);
        step++;
        if (step >= sequence.Length) Submit();
    }

    private IEnumerator FailRoutine(BastetFootprintPlate wrong)
    {
        state = State.Showing; // 깜빡이는 동안 입력 막음
        wrong.SetGlow(true, wrongColor);
        PunishTorch(wrongMessage);
        yield return waitFail;
        AllOff();
        state = State.Idle;
        SetSolving(false); // [추가]
        Toast("석상을 다시 깨워 발자국을 보자.");
    }

    // ================= PuzzleBase =================
    public override void Submit()
    {
        if (Validate()) OnSuccessInternal();
    }

    protected override bool Validate() => step >= sequence.Length;

    protected override void OnSuccessInternal()
    {
        if (IsCleared) return;
        base.OnSuccessInternal(); // 클리어 플래그 + OnSuccess (BastetRoomProgress가 보상 지급)
        SetSolving(false); // [추가]
        state = State.Cleared;
        foreach (int i in sequence) if (Valid(i)) plates[i].SetGlow(true, doneColor);
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
        foreach (var p in plates) if (p != null) p.SetGlow(false, showColor);
    }

    private bool Valid(int i) => i >= 0 && i < plates.Length && plates[i] != null;

    // ================= 테스트 =================
    [ContextMenu("테스트: 바로 클리어")]
    private void TestClear()
    {
        if (!Application.isPlaying) return;
        StopAllCoroutines();
        step = sequence.Length;
        Submit();
    }

    // Scene 뷰: 정답 순서를 선으로 (뒤로 갈수록 큰 원)
    private void OnDrawGizmosSelected()
    {
        if (plates == null || sequence == null) return;
        Gizmos.color = new Color(0.75f, 0.45f, 1f);
        for (int k = 0; k < sequence.Length; k++)
        {
            if (!Valid(sequence[k])) continue;
            Vector3 p = plates[sequence[k]].transform.position;
            Gizmos.DrawWireSphere(p, 0.25f + k * 0.08f);
            if (k > 0 && Valid(sequence[k - 1])) Gizmos.DrawLine(plates[sequence[k - 1]].transform.position, p);
        }
    }

#if UNITY_EDITOR
    public void EditorSetPlates(BastetFootprintPlate[] p) => plates = p;
#endif
}
