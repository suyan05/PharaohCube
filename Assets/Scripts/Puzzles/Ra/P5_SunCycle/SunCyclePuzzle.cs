using System;
using System.Collections.Generic;
using UnityEngine;

public enum SunButton { Rising, Noon, Setting, Night, Boat, Return }

public class SunCyclePuzzle : PuzzleBase
{
    public const string CircuitFlag = "F_RA_CIRCUIT_4"; // 4문양 점등 = 제단 개방 조건
    public const string PlateRa = "PLATE_RA";            // 태양 형판

    // 정답: (1) -> (2) -> (3) -> (4) -> (6) (기획서 6.4)
    private static readonly SunButton[] Answer =
    {
        SunButton.Rising, SunButton.Noon, SunButton.Setting, SunButton.Night, SunButton.Return
    };

    [Header("튜닝 (기획서 6.4)")]
    [SerializeField] private int hintAfterFails = 3;

    private readonly List<SunButton> inputs = new List<SunButton>();
    private int failCount;
    private bool hintShown;

    public event Action OnInputChanged;
    public event Action OnHint;

    public bool IsAwake => GameManager.Instance != null && GameManager.Instance.HasFlag(CircuitFlag);
    public bool HintShown => hintShown;
    public IReadOnlyList<SunButton> Inputs => inputs;
    public int AnswerLength => Answer.Length;
    public static SunButton AnswerAt(int i) => Answer[i];

    public static string ToKorean(SunButton b)
    {
        switch (b)
        {
            case SunButton.Rising: return "떠오르는 태양";
            case SunButton.Noon: return "정오";
            case SunButton.Setting: return "지는 태양";
            case SunButton.Night: return "밤";
            case SunButton.Boat: return "태양의 배";
            default: return "재림";
        }
    }

    // ================= 진입 =================
    public override void Open()
    {
        base.Open();
        if (!IsAwake)
        {
            Debug.Log("[P5] 제단이 아직 잠들어 있다. (회로판 4문양 점등 필요)");
        }
    }

    // ================= 입력 =================
    public void Press(SunButton button)
    {
        if (IsCleared || !IsAwake) return;
        if (inputs.Count >= Answer.Length) return;

        inputs.Add(button);
        Debug.Log($"[P5] 입력 {inputs.Count}: {ToKorean(button)}");
        OnInputChanged?.Invoke();

        // 5번째 입력 순간 즉시 판정
        if (inputs.Count == Answer.Length)
        {
            Submit();
        }
    }

    // '뒤로' 버튼: 마지막 입력 취소 (기획서 6.4 튜닝)
    public void Undo()
    {
        if (IsCleared || inputs.Count == 0) return;

        inputs.RemoveAt(inputs.Count - 1);
        Debug.Log("[P5] 마지막 입력 취소");
        OnInputChanged?.Invoke();
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
        base.OnSuccessInternal(); // IsCleared + clearFlag(F_RA_P5_DONE)

        ItemInventory.Instance.Grant(PlateRa);
        Debug.Log("[P5] 태양 형판(PLATE_RA) 획득 + '태양' 키워드");
        Debug.Log("[노트 6] 태양의 형판을 큐브의 첫 면에 장착하라.");
    }

    protected override void OnFailInternal()
    {
        failCount++;
        base.OnFailInternal(); // OnFail 이벤트 -> Reset()

        if (failCount >= hintAfterFails && !hintShown)
        {
            hintShown = true;
            Debug.Log("[P5] 힌트: 제단 벽면에 5단계 실루엣이 떠오른다");
            OnHint?.Invoke();
        }
    }

    public override void Reset()
    {
        inputs.Clear();
        base.Reset();
        OnInputChanged?.Invoke();
    }

    [ContextMenu("테스트: 제단 깨우기 (F_RA_CIRCUIT_4 켜기)")]
    void TestWake() => GameManager.Instance.SetFlag(CircuitFlag);
}