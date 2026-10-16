using System;
using UnityEngine;

// P3 일식 타일 복원 - "회전으로 그림 맞추기"
// 정답 횟수, 회전 방향은 낮/밤 데이터(EclipsePuzzleData)에서 읽음
// - 낮: 30초가 지나면 화면(EclipseArtStation)이 어긋난 원판을 붉게 표시 (자동 힌트)
// - 밤: 자동 힌트 없음 -> 힌트 버튼을 누르면 "지금" 어긋난 원판이 붉게 빛남 (원판을 돌리면 꺼짐)
public class EclipsePuzzle : RaPhasedPuzzle, IPuzzleHintProvider
{
    // 원판 8칸 (0=위, 시계 방향으로 1=오른쪽 위 ~ 7=왼쪽 위)
    public const int SlotCount = 8;
    public const int NightSlot = 4; // 밤 문양 칸 (아래) - 청색 안료로 채워짐

    // 회전 조각 A, B, C가 있는 칸 번호
    public static readonly int[] RotatingSlots = { 6, 7, 1 }; // A=왼쪽, B=왼쪽 위, C=오른쪽 위
    public static readonly string[] RotatingLabels = { "A", "B", "C" };

    [Header("낮/밤 데이터 (밤 데이터가 비면 낮 데이터 사용)")]
    [SerializeField] private EclipsePuzzleData dayData;
    [SerializeField] private EclipsePuzzleData nightData;
    private EclipsePuzzleData fallbackData;

    // 현재 상태: 시계 방향 기준 누적 회전수 (0~3)
    private readonly int[] states = new int[EclipsePuzzleData.PieceCount];
    private bool pigmentApplied;
    private bool hintActive;

    public event Action OnStateChanged;
    public event Action OnPigmentApplied;
    public event Action OnHint; // 힌트 버튼으로 붉은 윤곽이 켜짐

    public bool PigmentApplied => pigmentApplied;
    public bool HintActive => hintActive;

    private EclipsePuzzleData Data => PickData(dayData, nightData, ref fallbackData);
    protected override RaPuzzleData CurrentData => Data;

    private void Awake()
    {
        RandomizeStates();
    }

    // 초기 무작위 회전, 단 정답 값으로는 시작 안 함 (기획서 4.3)
    private void RandomizeStates()
    {
        for (int i = 0; i < states.Length; i++)
        {
            int target = TargetState(i);
            do { states[i] = UnityEngine.Random.Range(0, 4); }
            while (states[i] == target);
        }
    }

    // 데이터의 정답 횟수를 시계 방향 기준 상태값으로 바꿈 (반시계 3회 = 시계 1회)
    private int TargetState(int index)
    {
        int t = Data.TargetAt(index);
        return Data.RotateClockwise ? t : (4 - t) % 4;
    }

    // 정답까지 시계 방향으로 몇 칸 어긋났는지 (0이면 정답) 화면에서 원판 각도 계산용
    public int GetOffset(int index) => (states[index] - TargetState(index) + 4) % 4;
    public bool IsCorrect(int index) => states[index] == TargetState(index);

    // 화면에서 회전 방향 안내용 (밤 = 반시계)
    public bool RotateClockwise => Data.RotateClockwise;

    // ================= 진입 + 안료 게이트 =================
    public override void Open()
    {
        base.Open();
        if (!CanEnter()) return;

        TryApplyPigment();
        if (!pigmentApplied)
        {
            Debug.Log("[P3] 밤 문양 조각이 빠져 있다. 원판이 움직이지 않는다.");
        }
    }

    // 낮: 청색 안료를 갖고 있으면 자동 사용 (기획서 3장) / 밤: 아이템 없이 바로 가동
    private void TryApplyPigment()
    {
        if (pigmentApplied) return;

        if (UseItemGate)
        {
            if (ItemInventory.Instance == null || !ItemInventory.Instance.Has(ItemIds.PigmentNight)) return;
            ItemInventory.Instance.Consume(ItemIds.PigmentNight);
            Debug.Log("[P3] 청색 안료 사용 -> 밤 문양 조각이 채워짐, 원판 가동");
        }

        pigmentApplied = true;
        OnPigmentApplied?.Invoke();
    }

    // ================= 조작 =================
    public void RotatePiece(int index)
    {
        if (IsCleared) return;
        if (index < 0 || index >= states.Length) return;

        if (!pigmentApplied)
        {
            Debug.Log("[P3] 밤 문양 조각이 빠져 있어 원판이 움직이지 않는다.");
            return;
        }

        int step = Data.RotateClockwise ? 1 : 3; // 반시계 90도 = 시계 270도
        states[index] = (states[index] + step) % 4;
        hintActive = false; // 힌트는 "누른 순간"의 상태만 보여줌 -> 돌리면 꺼짐
        Debug.Log($"[P3] {RotatingLabels[index]} 회전 -> 상태 {states[index]}{(IsCorrect(index) ? " (연결됨!)" : "")}");
        OnStateChanged?.Invoke();

        Submit();
    }

    // ================= 힌트 버튼 (밤) =================
    public bool HintAvailable => !UseAutoHint && CanEnter() && pigmentApplied && !IsCleared && !hintActive;

    public string RequestHint()
    {
        if (!HintAvailable) return null;

        hintActive = true;
        Debug.Log("[P3] 힌트: 어긋난 원판이 붉게 빛남");
        OnHint?.Invoke();
        return "어긋난 원판의 윤곽이 붉게 빛난다. 원판을 돌리면 빛이 사라진다.";
    }

    // ================= PuzzleBase 구현 =================
    public override void Submit()
    {
        try
        {
            // 오답 개념 없음: 다 맞으면 성공, 아니면 아무 일 없음
            if (Validate()) OnSuccessInternal();
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] P3 판정 오류: {e}");
        }
    }

    protected override bool Validate()
    {
        for (int i = 0; i < states.Length; i++)
        {
            if (!IsCorrect(i)) return false;
        }
        return true;
    }

    protected override void OnSuccessInternal()
    {
        hintActive = false;
        base.OnSuccessInternal(); // IsCleared + clearFlag (낮 F_RA_P3_DONE / 밤 F_RA_NIGHT_P3_DONE)

        if (!GrantRewards) return; // 밤에는 보상 없음 (기획서 6장, 보상 미정)

        ItemInventory.Instance.Grant(ItemIds.Frag2);
        ItemInventory.Instance.Grant(ItemIds.Prism);
        Debug.Log("[노트 4] 밤의 태양은 죽지 않는다. 저승의 강을 항해해 다시 떠오른다.");
    }

    // 낮 -> 밤 전환: 안료 상태와 회전을 처음부터 다시
    protected override void ApplyPhase()
    {
        pigmentApplied = false;
        hintActive = false;
        RandomizeStates();
        OnStateChanged?.Invoke();
    }

    [ContextMenu("테스트: 정답으로 맞추기")]
    void TestSolve()
    {
        if (!pigmentApplied)
        {
            Debug.Log("[P3] 테스트 불가: 원판이 아직 안 움직임 (석탁을 먼저 열기)");
            return;
        }

        for (int i = 0; i < states.Length; i++)
        {
            while (!IsCorrect(i) && !IsCleared) RotatePiece(i);
        }
    }
}
/*부채꼴은 코드로 까다로워서 3x3 칸에 가운데를 뺀 8칸을 고리처럼 둠.
 아트가 나오면 원형으로 바꿀 예정.*/

/*각 회전 조각의 상태를 시계 방향 누적 회전수 0~3으로 저장.
 시계 방향이면 클릭마다 +1, 반시계면 +3 (= -1). 세 조각이 정답 값과 모두 같으면 즉시 성공.
 시작 값은 무작위지만 정답과 다른 값만 나오게 세팅*/