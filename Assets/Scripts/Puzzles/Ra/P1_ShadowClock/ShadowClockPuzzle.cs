using System;
using System.Collections.Generic;
using UnityEngine;

public enum TimeSymbol { Noon, Night, Dawn, Dusk }

// P1 태양 그림자 시계 - "그림자는 해의 반대쪽" (기획서 v4.0 4.1)
// - 다이얼에 그림자 1개 -> 벽 기호 4개 중 하나 클릭 -> 즉시 판정
// - 4문제 (새벽/정오/황혼/밤 한 번씩, 순서 랜덤), 전부 맞히면 클리어
// - 오답이면 같은 문제 다시, 같은 문제를 3번 틀리면 해 위치 힌트 (다음 문제로 가면 초기화)
// - 낮/밤 판정표는 ShadowClockPuzzleData에서 읽음 (밤 = 그림자가 해를 향함)
public class ShadowClockPuzzle : RaPhasedPuzzle
{
    // 동쪽 벽 기호 배치 순서 (화면 왼쪽부터)
    public static readonly TimeSymbol[] WallOrder =
    {
        TimeSymbol.Noon, TimeSymbol.Night, TimeSymbol.Dawn, TimeSymbol.Dusk
    };

    [Header("낮/밤 데이터 (밤 데이터가 비면 낮 데이터 사용)")]
    [SerializeField] private ShadowClockPuzzleData dayData;
    [SerializeField] private ShadowClockPuzzleData nightData;
    private ShadowClockPuzzleData fallbackData;

    [Header("튜닝 (기획서 4.1)")]
    [Tooltip("같은 문제를 이 횟수만큼 틀리면 해 위치 힌트 표시")]
    [SerializeField] private int hintAfterFails = 3;

    private readonly List<TimeSymbol> questions = new List<TimeSymbol>();
    private int currentIndex;
    private int failsOnCurrent;
    private bool hintActive;
    private bool lensMounted;
    private TimeSymbol? pendingInput;

    public event Action OnQuestionChanged;     // 문제 바뀜 / 같은 문제 다시 / 낮밤 전환
    public event Action<TimeSymbol> OnCorrect; // 한 문제 맞힘 (맞힌 시간 전달)
    public event Action OnLensMounted;         // 렌즈 장착됨
    public event Action OnHint;                // 해 위치 힌트 발동

    private ShadowClockPuzzleData Data => PickData(dayData, nightData, ref fallbackData);
    protected override RaPuzzleData CurrentData => Data;

    // 밤(아이템 게이트 꺼짐)에는 렌즈 없이도 보임
    public bool LensMounted => lensMounted || !UseItemGate;
    public bool HintActive => hintActive;
    public int QuestionCount => ShadowClockPuzzleData.RuleCount;
    public int SolvedCount => IsCleared ? QuestionCount : currentIndex;

    public TimeSymbol CurrentTime => QuestionAt(currentIndex);
    public ShadowRule CurrentRule => Data.GetRule(CurrentTime);

    public TimeSymbol QuestionAt(int index)
    {
        if (questions.Count == 0) return TimeSymbol.Dawn;
        return questions[Mathf.Clamp(index, 0, questions.Count - 1)];
    }

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
        if (!CanEnter()) return;

        try
        {
            EnsureQuestions();
            TryMountLens();

            if (!LensMounted) Debug.Log("[P1] 너무 어두워 아무것도 보이지 않는다.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] P1 열기 오류: {e}");
        }
    }

    // 주황 렌즈를 갖고 있으면 자동 장착 (낮만, 밤은 UseItemGate가 꺼져 있어서 생략)
    private void TryMountLens()
    {
        if (LensMounted) return;

        var inventory = ItemInventory.Instance;
        if (inventory == null)
        {
            Debug.LogWarning($"[{name}] ItemInventory가 없어서 렌즈 확인을 못 함");
            return;
        }
        if (!inventory.Has(ItemIds.LensSun)) return;

        inventory.Consume(ItemIds.LensSun);
        lensMounted = true;
        Debug.Log("[P1] 주황 렌즈 자동 장착 -> 다이얼에 그림자가 드러남");
        OnLensMounted?.Invoke();
    }

    // ================= 출제 =================
    private void EnsureQuestions()
    {
        if (questions.Count == QuestionCount) return;
        BuildQuestions();
    }

    // 4개 시간을 한 번씩 넣고 순서만 섞음 (피셔-예이츠 셔플)
    private void BuildQuestions()
    {
        questions.Clear();
        questions.AddRange(WallOrder);

        for (int i = questions.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (questions[i], questions[j]) = (questions[j], questions[i]);
        }

        currentIndex = 0;
        failsOnCurrent = 0;
        hintActive = false;
        Debug.Log($"[P1] 출제 순서: {string.Join(", ", questions.ConvertAll(ToKorean))}");
    }

    // ================= 입력 =================
    public void PressSymbol(TimeSymbol symbol)
    {
        if (IsCleared || !CanEnter()) return;

        if (!LensMounted)
        {
            Debug.Log("[P1] 너무 어두워 아무것도 보이지 않는다.");
            return;
        }

        EnsureQuestions();
        pendingInput = symbol;
        Debug.Log($"[P1] {currentIndex + 1}번 문제 입력: {ToKorean(symbol)}");
        Submit();
    }

    // ================= PuzzleBase 구현 =================
    public override void Submit()
    {
        try
        {
            if (Validate()) HandleCorrect();
            else OnFailInternal();
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] P1 판정 오류: {e}");
        }
        finally
        {
            pendingInput = null;
        }
    }

    protected override bool Validate()
    {
        return pendingInput.HasValue && pendingInput.Value == CurrentTime;
    }

    private void HandleCorrect()
    {
        TimeSymbol solved = CurrentTime;
        currentIndex++;
        failsOnCurrent = 0;
        hintActive = false;

        Debug.Log($"[P1] 정답: {ToKorean(solved)} ({currentIndex}/{QuestionCount})");
        OnCorrect?.Invoke(solved);

        if (currentIndex >= QuestionCount) OnSuccessInternal();
        else OnQuestionChanged?.Invoke();
    }

    protected override void OnSuccessInternal()
    {
        base.OnSuccessInternal(); // IsCleared + clearFlag (낮 F_RA_P1_DONE / 밤 F_RA_NIGHT_P1_DONE)

        if (!GrantRewards) return; // 밤에는 보상 없음

        var inventory = ItemInventory.Instance;
        if (inventory != null) inventory.Grant(ItemIds.Frag1);
        else Debug.LogWarning($"[{name}] ItemInventory가 없어서 거울 조각 1을 못 줌");

        Debug.Log("[노트 2] 그림자는 언제나 해의 반대쪽에 눕는다. 해가 동쪽이면 그림자는 서쪽으로 간다.");
        Debug.Log("[노트 3] 해가 머리 위에 서면 그림자는 발밑에 숨고, 해가 없으면 그림자도 없다.");
    }

    // 오답: 같은 문제 다시. 같은 문제를 hintAfterFails번 틀리면 해 위치 힌트
    protected override void OnFailInternal()
    {
        failsOnCurrent++;
        base.OnFailInternal(); // OnFail 이벤트 -> Reset()

        if (failsOnCurrent >= hintAfterFails && !hintActive)
        {
            hintActive = true;
            Debug.Log("[P1] 힌트: 해의 위치가 어렴풋이 보인다");
            OnHint?.Invoke();
        }
    }

    // 진행은 그대로 두고 지금 문제만 다시 (기획서 4.1 "해당 문제만 다시")
    public override void Reset()
    {
        if (!Application.isPlaying) return; // [Reset 가드] 에디터에서 Unity가 자동으로 부르는 Reset 무시

        pendingInput = null;
        base.Reset();
        OnQuestionChanged?.Invoke();
    }

    // 낮 -> 밤 전환: 문제를 새로 섞고 처음부터
    protected override void ApplyPhase()
    {
        pendingInput = null;
        BuildQuestions();
        OnQuestionChanged?.Invoke();
    }

    // ================= 테스트용 =================
    [ContextMenu("테스트: 지금 문제 정답 입력")]
    private void TestAnswerCurrent()
    {
        PressSymbol(CurrentTime);
    }

    [ContextMenu("테스트: 전부 정답 입력")]
    private void TestAnswerAll()
    {
        for (int guard = 0; guard < QuestionCount && !IsCleared; guard++)
        {
            PressSymbol(CurrentTime);
        }
    }

    [ContextMenu("테스트: 문제 새로 섞기")]
    private void TestReshuffle()
    {
        if (IsCleared) return;
        BuildQuestions();
        OnQuestionChanged?.Invoke();
    }
}