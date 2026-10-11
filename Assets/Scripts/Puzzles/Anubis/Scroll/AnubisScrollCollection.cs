using System;
using System.Collections.Generic;
using UnityEngine;

// 양피지 조각 수집 상태 관리 (Managers 오브젝트에 부착)
// 다른 스크립트는 이벤트로 결과를 받음 (싱글톤 아님)
public class AnubisScrollCollection : MonoBehaviour
{
    public event Action<AnubisScrollData, int, int> OnPieceCollected; // (양피지, 조각번호 0~, 모은 개수)
    public event Action<AnubisScrollData> OnScrollCompleted;

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

    [ContextMenu("테스트: 조각 전부 획득 (Debug Scroll)")]
    private void TestCollectAll()
    {
        if (debugScroll == null) { Debug.LogWarning("[Scroll] Debug Scroll을 지정하세요"); return; }
        for (int i = 0; i < debugScroll.PieceCount; i++) Collect(debugScroll, i);
    }
}