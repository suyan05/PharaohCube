using System;
using UnityEngine;

// P4 동쪽에서 서쪽으로 - "그림자만 보고 읽기" (P1 응용)
// 벽화 패널 순서, 정답 방향(정순/역순)은 낮/밤 데이터(DayCyclePuzzleData)에서 읽음
public class DayCyclePuzzle : RaPhasedPuzzle
{
    // 데이터가 하나도 없을 때 화면에 보여줄 기본 패널 순서
    private static readonly TimeSymbol[] FallbackPanels =
    {
        TimeSymbol.Dawn, TimeSymbol.Noon, TimeSymbol.Dusk, TimeSymbol.Night
    };

    [Header("낮/밤 데이터 (밤 데이터가 비면 낮 데이터 사용)")]
    [SerializeField] private DayCyclePuzzleData dayData;
    [SerializeField] private DayCyclePuzzleData nightData;
    private DayCyclePuzzleData fallbackData;

    // 씬에 있는 P4 (화면 스크립트가 static AnswerAt으로 벽화를 그릴 때 사용)
    private static DayCyclePuzzle current;

    private TimeSymbol[] panels = new TimeSymbol[DayCyclePuzzleData.SlotCount];      // 벽화 패널 (왼쪽부터)
    private readonly TimeSymbol[] answer = new TimeSymbol[DayCyclePuzzleData.SlotCount];      // 정답 배치
    private readonly TimeSymbol[] arrangement = new TimeSymbol[DayCyclePuzzleData.SlotCount]; // 슬롯 0~3에 놓인 카드
    private bool unsealed;

    public event Action OnArrangementChanged;
    public event Action OnUnsealed;

    public bool Unsealed => unsealed;
    public int SlotCount => arrangement.Length;

    private DayCyclePuzzleData Data => PickData(dayData, nightData, ref fallbackData);
    protected override RaPuzzleData CurrentData => Data;

    // 벽화 표시용: 왼쪽부터 index번째 패널 (기존 화면 스크립트 호환용으로 이름 유지)
    // 밤에는 정답이 이 순서의 역순이라서, 정답이 필요하면 SolutionAt을 써야 함
    public static TimeSymbol AnswerAt(int index)
    {
        if (current != null) return current.PanelAt(index);
        return FallbackPanels[Mathf.Clamp(index, 0, FallbackPanels.Length - 1)];
    }

    public TimeSymbol PanelAt(int index) => panels[Mathf.Clamp(index, 0, panels.Length - 1)];
    public TimeSymbol SolutionAt(int index) => answer[Mathf.Clamp(index, 0, answer.Length - 1)];
    public int IndexOf(TimeSymbol symbol) => Array.IndexOf(arrangement, symbol);

    private void Awake()
    {
        current = this;
        BuildPanelsAndAnswer();
        ShuffleInternal();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (current == this) current = null;
    }

    // 데이터로 벽화 패널 순서와 정답을 만듦
    private void BuildPanelsAndAnswer()
    {
        panels = Data.CopyPanelOrder();
        if (Data.RandomizePanels) ShuffleArray(panels);

        for (int i = 0; i < answer.Length; i++)
        {
            int from = Data.ReverseAnswer ? panels.Length - 1 - i : i;
            answer[i] = panels[from];
        }
    }

    // ================= 진입 + 열쇠 게이트 =================
    public override void Open()
    {
        base.Open();
        if (!CanEnter()) return;

        TryUnseal();
        if (!unsealed)
        {
            Debug.Log("[P4] 봉인줄이 감겨 있다. 열쇠가 필요하다.");
        }
    }

    // 낮: 황동 열쇠를 갖고 있으면 자동 해제 (기획서 3장) / 밤: 아이템 없이 바로 해제
    private void TryUnseal()
    {
        if (unsealed) return;

        if (UseItemGate)
        {
            if (ItemInventory.Instance == null || !ItemInventory.Instance.Has(ItemIds.KeyBrass)) return;
            ItemInventory.Instance.Consume(ItemIds.KeyBrass);
            Debug.Log("[P4] 황동 열쇠 사용 -> 봉인줄 해제");
        }

        unsealed = true;
        OnUnsealed?.Invoke();
    }

    // ================= 조작 =================
    public void SwapSlots(int a, int b)
    {
        if (!unsealed || IsCleared || a == b) return;
        if (a < 0 || b < 0 || a >= arrangement.Length || b >= arrangement.Length) return;

        TimeSymbol temp = arrangement[a];
        arrangement[a] = arrangement[b];
        arrangement[b] = temp;

        Debug.Log($"[P4] 슬롯 {a + 1} <-> {b + 1} 교환");
        OnArrangementChanged?.Invoke();
    }

    // 취소 버튼: 무작위로 다시 섞기 (기획서 4.4 튜닝)
    public void ShuffleCards()
    {
        if (!unsealed || IsCleared) return;

        ShuffleInternal();
        Debug.Log("[P4] 카드를 다시 섞음");
        OnArrangementChanged?.Invoke();
    }

    // 정답 배치로는 시작하지 않도록 다시 섞기
    private void ShuffleInternal()
    {
        Array.Copy(answer, arrangement, answer.Length);
        do
        {
            ShuffleArray(arrangement);
        }
        while (Validate());
    }

    private static void ShuffleArray(TimeSymbol[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            TimeSymbol temp = array[i];
            array[i] = array[j];
            array[j] = temp;
        }
    }

    // ================= PuzzleBase 구현 =================
    // 확정 버튼을 누르면 호출
    public override void Submit()
    {
        if (!unsealed || IsCleared) return;

        if (Validate()) OnSuccessInternal();
        else OnFailInternal();
    }

    protected override bool Validate()
    {
        for (int i = 0; i < answer.Length; i++)
        {
            if (arrangement[i] != answer[i]) return false;
        }
        return true;
    }

    protected override void OnSuccessInternal()
    {
        base.OnSuccessInternal(); // IsCleared + clearFlag (낮 F_RA_P4_DONE / 밤 F_RA_NIGHT_P4_DONE)

        if (!GrantRewards) return; // 밤에는 보상 없음 (기획서 6장, 보상 미정)

        ItemInventory.Instance.Grant(ItemIds.Frag3);
        // 노트 5 수수께끼 초안 (기획서 5장, 문구는 스토리 작업 때 확정)
        Debug.Log("[노트 5] 서쪽에 누운 왕은 걸어서 돌아오지 않는다. 그는 무엇에 올라 어둠의 강을 건너는가?");
    }

    // 실패해도 배치 유지 (기획서 4.4: 실패 시 배치 유지)
    public override void Reset()
    {
        Debug.Log("[P4] 오답 - 배치는 그대로 유지");
    }

    // 낮 -> 밤 전환: 봉인, 벽화, 정답, 카드 배치를 처음부터 다시
    protected override void ApplyPhase()
    {
        unsealed = false;
        BuildPanelsAndAnswer();
        ShuffleInternal();
        OnArrangementChanged?.Invoke();
    }

    [ContextMenu("테스트: 정답 배치 후 확정")]
    void TestAnswer()
    {
        if (!unsealed)
        {
            Debug.Log("[P4] 테스트 불가: 봉인을 먼저 풀어야 함 (벤치 열기)");
            return;
        }

        Array.Copy(answer, arrangement, answer.Length);
        OnArrangementChanged?.Invoke();
        Submit();
    }
}
