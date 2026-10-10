using System;
using System.Collections.Generic;
using UnityEngine;

public enum SunButton { Rising, Noon, Setting, Night, Boat, Return }

// P5 태양의 순환 - "종합 + 신화"
// 정답 순서, 제단이 깨어나는 조건은 낮/밤 데이터(SunCyclePuzzleData)에서 읽음
public class SunCyclePuzzle : RaPhasedPuzzle
{
    public const string CircuitFlag = "F_RA_CIRCUIT_4"; // 낮 기본 개방 조건 (기존 코드 호환용)
    public const string PlateRa = "PLATE_RA";            // 태양 형판

    // 데이터가 하나도 없을 때 쓰는 기본 정답 (낮 A안)
    private static readonly SunButton[] FallbackAnswer =
    {
        SunButton.Rising, SunButton.Noon, SunButton.Setting, SunButton.Boat, SunButton.Return
    };

    [Header("낮/밤 데이터 (밤 데이터가 비면 낮 데이터 사용)")]
    [SerializeField] private SunCyclePuzzleData dayData;
    [SerializeField] private SunCyclePuzzleData nightData;
    private SunCyclePuzzleData fallbackData;

    [Header("튜닝 (기획서 4.5)")]
    [SerializeField] private int hintAfterFails = 3;

    // 씬에 있는 P5 (화면 스크립트가 static AnswerAt으로 힌트를 그릴 때 사용)
    private static SunCyclePuzzle current;

    private readonly List<SunButton> inputs = new List<SunButton>();
    private int failCount;
    private bool hintShown;

    public event Action OnInputChanged;
    public event Action OnHint;

    private SunCyclePuzzleData Data => PickData(dayData, nightData, ref fallbackData);
    protected override RaPuzzleData CurrentData => Data;

    public bool IsAwake => string.IsNullOrEmpty(Data.WakeFlag) || IsFlagOn(Data.WakeFlag);
    public bool HintShown => hintShown;
    public IReadOnlyList<SunButton> Inputs => inputs;
    public int AnswerLength => Data.AnswerLength;

    // 기존 화면 스크립트 호환용 (static 유지)
    public static SunButton AnswerAt(int i)
    {
        if (current != null) return current.Data.AnswerAt(i);
        return FallbackAnswer[Mathf.Clamp(i, 0, FallbackAnswer.Length - 1)];
    }

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

    private void Awake()
    {
        current = this;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (current == this) current = null;
    }

    // ================= 진입 =================
    public override void Open()
    {
        base.Open();
        if (!CanEnter()) return;

        if (!IsAwake)
        {
            Debug.Log($"[P5] 제단이 아직 잠들어 있다. ({Data.WakeFlag} 필요)");
        }
    }

    // ================= 입력 =================
    public void Press(SunButton button)
    {
        if (IsCleared || !IsAwake) return;
        if (inputs.Count >= AnswerLength) return;

        inputs.Add(button);
        Debug.Log($"[P5] 입력 {inputs.Count}: {ToKorean(button)}");
        OnInputChanged?.Invoke();

        // 마지막 입력 순간 즉시 판정
        if (inputs.Count == AnswerLength)
        {
            Submit();
        }
    }

    // '뒤로' 버튼: 마지막 입력 취소 (기획서 4.5)
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
        if (inputs.Count != AnswerLength) return false;
        for (int i = 0; i < AnswerLength; i++)
        {
            if (inputs[i] != Data.AnswerAt(i)) return false;
        }
        return true;
    }

    protected override void OnSuccessInternal()
    {
        base.OnSuccessInternal(); // IsCleared + clearFlag (낮 F_RA_P5_DONE / 밤 F_RA_NIGHT_P5_DONE)

        if (!GrantRewards) return; // 밤에는 보상 없음 (기획서 6장, 보상 미정)

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
            Debug.Log("[P5] 힌트: 제단 벽면에 정답 실루엣이 떠오른다");
            OnHint?.Invoke();
        }
    }

    public override void Reset()
    {
        inputs.Clear();
        base.Reset();
        OnInputChanged?.Invoke();
    }

    // 낮 -> 밤 전환: 입력, 오답 횟수, 힌트를 처음부터 다시
    protected override void ApplyPhase()
    {
        inputs.Clear();
        failCount = 0;
        hintShown = false;
        OnInputChanged?.Invoke();
    }

    [ContextMenu("테스트: 제단 깨우기 (지금 데이터의 개방 플래그 켜기)")]
    void TestWake()
    {
        if (GameManager.Instance == null || string.IsNullOrEmpty(Data.WakeFlag)) return;
        GameManager.Instance.SetFlag(Data.WakeFlag);
    }
}
