using System;
using UnityEngine;

public class DayCyclePuzzle : PuzzleBase
{
    // 정답: 왼쪽(동)부터 새벽 -> 정오 -> 황혼 -> 밤 (기획서 6.3)
    private static readonly TimeSymbol[] Answer =
    {
        TimeSymbol.Dawn, TimeSymbol.Noon, TimeSymbol.Dusk, TimeSymbol.Night
    };

    private readonly TimeSymbol[] arrangement = new TimeSymbol[4]; // 슬롯 0~3에 놓인 카드
    private bool unsealed;

    public event Action OnArrangementChanged;
    public event Action OnUnsealed;

    public bool Unsealed => unsealed;
    public int SlotCount => arrangement.Length;

    public static TimeSymbol AnswerAt(int index) => Answer[index]; // 벽화 단서 표시용
    public int IndexOf(TimeSymbol symbol) => Array.IndexOf(arrangement, symbol);

    private void Awake()
    {
        ShuffleInternal();
    }

    // ================= 진입 + 열쇠 게이트 =================
    public override void Open()
    {
        base.Open();
        TryUnseal();

        if (!unsealed)
        {
            Debug.Log("[P4] 봉인줄이 감겨 있다. 열쇠가 필요하다.");
        }
    }

    // 황동 열쇠를 갖고 있으면 자동 해제 (기획서 4.1)
    private void TryUnseal()
    {
        if (unsealed) return;
        if (!ItemInventory.Instance.Has(ItemIds.KeyBrass)) return;

        ItemInventory.Instance.Consume(ItemIds.KeyBrass);
        unsealed = true;
        Debug.Log("[P4] 황동 열쇠 사용 → 봉인줄 해제");
        OnUnsealed?.Invoke();
    }

    // ================= 조작 =================
    public void SwapSlots(int a, int b)
    {
        if (!unsealed || IsCleared || a == b) return;

        TimeSymbol temp = arrangement[a];
        arrangement[a] = arrangement[b];
        arrangement[b] = temp;

        Debug.Log($"[P4] 슬롯 {a + 1} ↔ {b + 1} 교환");
        OnArrangementChanged?.Invoke();
    }

    // 취소 버튼: 무작위로 다시 섞기 (기획서 6.3 튜닝)
    public void ShuffleCards()
    {
        if (!unsealed || IsCleared) return;

        ShuffleInternal();
        Debug.Log("[P4] 카드를 다시 섞음");
        OnArrangementChanged?.Invoke();
    }

    private void ShuffleInternal()
    {
        Array.Copy(Answer, arrangement, Answer.Length);

        // 정답 배치로는 시작하지 않도록 다시 섞기
        do
        {
            for (int i = arrangement.Length - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                TimeSymbol temp = arrangement[i];
                arrangement[i] = arrangement[j];
                arrangement[j] = temp;
            }
        }
        while (Validate());
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
        for (int i = 0; i < Answer.Length; i++)
        {
            if (arrangement[i] != Answer[i]) return false;
        }
        return true;
    }

    protected override void OnSuccessInternal()
    {
        base.OnSuccessInternal(); // IsCleared + clearFlag(F_RA_P4_DONE)

        ItemInventory.Instance.Grant(ItemIds.Frag3);
        Debug.Log("[노트 5] 하루의 순서가 곧 제단의 입력 순서다.");
    }

    // 실패해도 배치 유지 (기획서 6.3: 실패 시 배치 유지)
    public override void Reset()
    {
        Debug.Log("[P4] 오답 — 배치는 그대로 유지");
    }

    [ContextMenu("테스트: 정답 배치 후 확정")]
    void TestAnswer()
    {
        if (!unsealed)
        {
            Debug.Log("[P4] 테스트 불가: 황동 열쇠로 먼저 봉인 해제 (벤치 열기)");
            return;
        }

        Array.Copy(Answer, arrangement, Answer.Length);
        OnArrangementChanged?.Invoke();
        Submit();
    }
    /*힘들당*/
}