using System;
using UnityEngine;

public class EclipsePuzzle : PuzzleBase
{
    // 원판 8칸 (0=위, 시계 방향으로 1=오른쪽 위 ~~ 7=왼쪽 위)
    public const int SlotCount = 8;
    public const int NightSlot = 4; // 밤 문양 칸 (아래) ? 청색 안료로 채워짐

    // 회전 조각 A, B, C가 있는 칸 번호
    public static readonly int[] RotatingSlots = { 6, 7, 1 }; // A=왼쪽, B=왼쪽 위, C=오른쪽 위
    public static readonly string[] RotatingLabels = { "A", "B", "C" };

    // 정답 누적 회전수 (기획서 6.2: A=1회, B=2회, C=3회)
    private static readonly int[] Targets = { 1, 2, 3 };

    private readonly int[] states = new int[3]; // 현재 누적 회전수 (0~3)
    private bool pigmentApplied;

    public event Action OnStateChanged;
    public event Action OnPigmentApplied;

    public bool PigmentApplied => pigmentApplied;

    private void Awake()
    {
        // 초기 무작위 회전, 단 정답 값으로는 시작 안 함 (기획서 6.2)
        for (int i = 0; i < states.Length; i++)
        {
            do { states[i] = UnityEngine.Random.Range(0, 4); }
            while (states[i] == Targets[i]);
        }
    }

    // 정답까지 몇 칸 어긋났는지 (0이면 정답) 화면에서 바늘 방향 계산용
    public int GetOffset(int index) => (states[index] - Targets[index] + 4) % 4;
    public bool IsCorrect(int index) => states[index] == Targets[index];

    // ================= 진입 + 안료 게이트 =================
    public override void Open()
    {
        base.Open();
        TryApplyPigment();

        if (!pigmentApplied)
        {
            Debug.Log("[P3] 밤 문양 조각이 빠져 있다. 원판이 움직이지 않는다.");
        }
    }

    // 청색 안료를 갖고 있으면 자동 사용 (기획서 4.1)
    private void TryApplyPigment()
    {
        if (pigmentApplied) return;
        if (!ItemInventory.Instance.Has(ItemIds.PigmentNight)) return;

        ItemInventory.Instance.Consume(ItemIds.PigmentNight);
        pigmentApplied = true;
        Debug.Log("[P3] 청색 안료 사용 -> 밤 문양 조각이 채워짐, 원판 가동");
        OnPigmentApplied?.Invoke();
    }

    // ================= 조작 =================
    public void RotatePiece(int index)
    {
        if (IsCleared) return;

        if (!pigmentApplied)
        {
            Debug.Log("[P3] 밤 문양 조각이 빠져 있어 원판이 움직이지 않는다.");
            return;
        }

        states[index] = (states[index] + 1) % 4; // 90° 시계 방향
        Debug.Log($"[P3] {RotatingLabels[index]} 회전 -> 누적 {states[index]}회{(IsCorrect(index) ? " (연결됨!)" : "")}");
        OnStateChanged?.Invoke();

        Submit();
    }

    // ================= PuzzleBase 구현 =================
    public override void Submit()
    {
        // 오답 개념 없음: 다 맞으면 성공, 아니면 아무 일 없음
        if (Validate()) OnSuccessInternal();
    }

    protected override bool Validate()
    {
        for (int i = 0; i < states.Length; i++)
        {
            if (states[i] != Targets[i]) return false;
        }
        return true;
    }

    protected override void OnSuccessInternal()
    {
        base.OnSuccessInternal(); // IsCleared + clearFlag(F_RA_P3_DONE)

        ItemInventory.Instance.Grant(ItemIds.Frag2);
        ItemInventory.Instance.Grant(ItemIds.Prism);
        Debug.Log("[노트 4] 밤의 태양은 죽지 않는다. 저승의 강을 항해해 다시 떠오른다.");
    }

    [ContextMenu("테스트: 정답으로 맞추기")]
    void TestSolve()
    {
        if (!pigmentApplied)
        {
            Debug.Log("[P3] 테스트 불가: 청색 안료를 먼저 적용해야 함 (다이얼 열기)");
            return;
        }

        for (int i = 0; i < states.Length; i++)
        {
            while (!IsCorrect(i) && !IsCleared) RotatePiece(i);
        }
    }
}