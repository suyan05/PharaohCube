using System;
using System.Collections.Generic;
using UnityEngine;

public enum TimeSymbol { Noon, Night, Dawn, Dusk }

public class ShadowClockPuzzle : PuzzleBase
{
    // 동쪽 벽 기호 배치 순서 (기획서 6.1: 정오·밤·새벽·황혼 비순차)
    public static readonly TimeSymbol[] WallOrder =
    {
        TimeSymbol.Noon, TimeSymbol.Night, TimeSymbol.Dawn, TimeSymbol.Dusk
    };

    // 정답: 새벽 -> 정오 -> 황혼 -> 밤
    private static readonly TimeSymbol[] Answer =
    {
        TimeSymbol.Dawn, TimeSymbol.Noon, TimeSymbol.Dusk, TimeSymbol.Night
    };

    [Header("튜닝 (기획서 6.1)")]
    [SerializeField] private float shadowPeriod = 8f;  // 그림자 한 바퀴 (6~12초)
    [SerializeField] private int hintAfterFails = 3;   // 오답 몇 번마다 힌트

    private readonly List<TimeSymbol> inputs = new List<TimeSymbol>();
    private bool lensMounted;
    private int failCount;

    public event Action OnInputChanged;  // 입력 슬롯 바뀜
    public event Action OnLensMounted;   // 렌즈 장착됨
    public event Action OnHint;          // 힌트 발동

    public bool LensMounted => lensMounted;
    public IReadOnlyList<TimeSymbol> Inputs => inputs;
    public float ShadowPeriod => shadowPeriod;
    public int AnswerLength => Answer.Length;

    public static string ToKorean(TimeSymbol s)
    {
        switch (s)
        {
            case TimeSymbol.Noon: return "정오";
            case TimeSymbol.Night: return "밤";
            case TimeSymbol.Dawn: return "새벽";
            default: return "황혼";
        }
    }

    // ================= 진입 + 렌즈 게이트 =================
    public override void Open()
    {
        base.Open();
        TryMountLens();

        if (!lensMounted)
        {
            Debug.Log("[P1] 기호가 너무 어둡다. 읽을 수 없다.");
        }
    }

    // 주황 렌즈를 갖고 있으면 자동 장착 (기획서 4.1)
    private void TryMountLens()
    {
        if (lensMounted) return;
        if (!ItemInventory.Instance.Has(ItemIds.LensSun)) return;

        ItemInventory.Instance.Consume(ItemIds.LensSun);
        lensMounted = true;
        Debug.Log("[P1] 주황 렌즈 자동 장착 -> 벽 기호 발광");
        OnLensMounted?.Invoke();
    }

    // ================= 입력 =================
    public void PressSymbol(TimeSymbol symbol)
    {
        if (IsCleared) return;

        if (!lensMounted)
        {
            Debug.Log("[P1] 기호가 너무 어둡다. 읽을 수 없다.");
            return;
        }

        if (inputs.Count >= Answer.Length) return;

        inputs.Add(symbol);
        Debug.Log($"[P1] 입력 {inputs.Count}: {ToKorean(symbol)}");
        OnInputChanged?.Invoke();

        // 4번째 입력 순간 즉시 판정
        if (inputs.Count == Answer.Length)
        {
            Submit();
        }
    }

    // ================= PuzzleBase 구현 =================
    public override void Submit()
    {
        if (Validate()) OnSuccessInternal();
        else OnFailInternal();
    }

    protected override bool Validate()
    {
        if (inputs.Count != Answer.Length) return false;

        for (int i = 0; i < Answer.Length; i++)
        {
            if (inputs[i] != Answer[i]) return false;
        }
        return true;
    }

    protected override void OnSuccessInternal()
    {
        base.OnSuccessInternal(); // IsCleared + clearFlag(F_RA_P1_DONE)

        ItemInventory.Instance.Grant(ItemIds.Frag1);
        Debug.Log("[노트 2] 태양의 그림자는 동에서 서로 도는 시계다.");
        Debug.Log("[노트 3] 하루: 새벽 -> 정오 -> 황혼 -> 밤.");
    }

    protected override void OnFailInternal()
    {
        failCount++;
        base.OnFailInternal(); // OnFail 이벤트 -> Reset()

        if (failCount % hintAfterFails == 0)
        {
            Debug.Log("[P1] 힌트: '새벽' 기호가 빛난다");
            OnHint?.Invoke();
        }
    }

    public override void Reset()
    {
        inputs.Clear();
        base.Reset();
        OnInputChanged?.Invoke();
    }

    [ContextMenu("테스트: 정답 입력")]
    void TestAnswer()
    {
        foreach (var s in Answer) PressSymbol(s);
    }
}