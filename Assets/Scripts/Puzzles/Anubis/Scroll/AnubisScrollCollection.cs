using System;
using System.Collections.Generic;
using UnityEngine;

// 양피지 조각 수집 상태 관리 (Managers 오브젝트에 부착)
// 다른 스크립트는 이벤트로 결과를 받음 (싱글톤 아님)
// [추가] 메인 저울 실패 처벌(IAnubisRetryable) 때 모은 조각을 전부 비움 -> 처음부터 다시 주움
public class AnubisScrollCollection : MonoBehaviour, IAnubisRetryable
{
    public event Action<AnubisScrollData, int, int> OnPieceCollected; // (양피지, 조각번호 0~, 모은 개수)
    public event Action<AnubisScrollData> OnScrollCompleted;
    public event Action OnCollectionReset; // [추가] 처벌로 조각이 비워졌을 때 (양피지 창, 힌트 상자 갱신)

    [Header("완성 시 처리")]
    [Tooltip("완성되면 양피지 ID를 ItemInventory에 지급 (나중에 해금 조건으로 사용 가능)")]
    [SerializeField] private bool grantIdOnComplete = true;

    [Header("테스트용")]
    [Tooltip("우클릭 메뉴 '테스트: 조각 전부 획득'에 사용할 양피지")]
    [SerializeField] private AnubisScrollData debugScroll;

    private readonly Dictionary<AnubisScrollData, HashSet<int>> collected = new Dictionary<AnubisScrollData, HashSet<int>>();

    public AnubisScrollData LastScroll { get; private set; }

    public void Collect(AnubisScrollData scroll, int pieceIndex)
    {
        if (scroll == null) return;

        if (!collected.TryGetValue(scroll, out var set))
        {
            set = new HashSet<int>();
            collected[scroll] = set;
        }
        if (!set.Add(pieceIndex)) return; // 이미 주운 조각

        LastScroll = scroll;
        int count = set.Count;
        Debug.Log($"[Scroll] {scroll.Title} 조각 {pieceIndex + 1} 획득 ({count}/{scroll.PieceCount})");
        OnPieceCollected?.Invoke(scroll, pieceIndex, count);

        if (count >= scroll.PieceCount)
        {
            Debug.Log($"[Scroll] {scroll.Title} 완성!");
            if (grantIdOnComplete && ItemInventory.Instance != null)
                ItemInventory.Instance.Grant(scroll.ScrollId);
            OnScrollCompleted?.Invoke(scroll);
        }
    }

    public int GetCount(AnubisScrollData scroll) =>
        (scroll != null && collected.TryGetValue(scroll, out var set)) ? set.Count : 0;

    public bool Has(AnubisScrollData scroll, int pieceIndex) =>
        scroll != null && collected.TryGetValue(scroll, out var set) && set.Contains(pieceIndex);

    public bool IsComplete(AnubisScrollData scroll) =>
        scroll != null && GetCount(scroll) >= scroll.PieceCount;

    // [추가] 메인 저울 실패 처벌: 모은 조각 + 완성 표시(양피지 ID 아이템)를 비움
    // 맵의 조각은 각 AnubisScrollPiece가 스스로 다시 나타남
    public void ResetForRetry()
    {
        if (grantIdOnComplete && ItemInventory.Instance != null)
        {
            foreach (var pair in collected)
            {
                AnubisScrollData scroll = pair.Key;
                if (scroll != null && ItemInventory.Instance.Has(scroll.ScrollId))
                    ItemInventory.Instance.Consume(scroll.ScrollId);
            }
        }

        collected.Clear();
        LastScroll = null;
        Debug.Log("[Scroll] 처벌로 양피지 조각을 모두 잃음 -> 처음부터 다시 줍기");
        OnCollectionReset?.Invoke();
    }

    [ContextMenu("테스트: 조각 전부 획득 (Debug Scroll)")]
    private void TestCollectAll()
    {
        if (debugScroll == null) { Debug.LogWarning("[Scroll] Debug Scroll을 지정하세요"); return; }
        for (int i = 0; i < debugScroll.PieceCount; i++) Collect(debugScroll, i);
    }
}