using System;
using UnityEngine;

// 바스테트 실전 방 진행: 퍼즐이 풀리면(PuzzleBase.OnSuccess) 보상 지급 + 알림
// - B1~B4 보상 아이템이 다음 제단 잠금, 다음 비석 "!", 목표 띠를 함께 움직임 (각자 ItemInventory 이벤트로 반응)
// - B5(메인)를 풀면 F_BASTET_MAIN_DONE 플래그 -> 목표 띠 마지막 단계, 토트 출구 안내
// 붙이는 곳: BastetRoomUI 오브젝트 (Tools/Bastet/실전 방 세팅 메뉴가 자동으로 붙임)
public class BastetRoomProgress : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        [Tooltip("풀리면 보상을 줄 퍼즐")]
        public PuzzleBase puzzle;
        [Tooltip("지급할 아이템 (비우면 지급 안 함)")]
        public string rewardItemId = "";
        [Tooltip("켤 플래그 (비우면 안 켬)")]
        public string flag = "";
        [TextArea(1, 2)] public string toast = "";
    }

    [Header("퍼즐별 보상 (B1 -> B5 순서)")]
    [SerializeField] private Entry[] entries = new Entry[0];

    [Range(1f, 8f)] [SerializeField] private float toastTime = 4f;

    // 람다 대신 핸들러를 저장해 두고 OnDestroy에서 정확히 해제
    private Action[] handlers;

    private void Start()
    {
        handlers = new Action[entries.Length];
        for (int i = 0; i < entries.Length; i++)
        {
            Entry e = entries[i];
            if (e == null || e.puzzle == null)
            {
                Debug.LogWarning($"[BastetRoomProgress] {i + 1}번 항목의 퍼즐이 비어 있음", this);
                continue;
            }
            handlers[i] = () => HandleSolved(e);
            e.puzzle.OnSuccess += handlers[i];
        }
    }

    private void OnDestroy()
    {
        if (handlers == null) return;
        for (int i = 0; i < entries.Length; i++)
            if (handlers[i] != null && entries[i] != null && entries[i].puzzle != null)
                entries[i].puzzle.OnSuccess -= handlers[i];
    }

    private void HandleSolved(Entry e)
    {
        Debug.Log($"[BastetRoomProgress] {e.puzzle.name} 클리어 -> 보상 {e.rewardItemId} / 플래그 {e.flag}", this);

        if (!string.IsNullOrEmpty(e.rewardItemId))
        {
            if (ItemInventory.Instance != null) ItemInventory.Instance.Grant(e.rewardItemId);
            else Debug.LogWarning($"[BastetRoomProgress] ItemInventory가 없어 {e.rewardItemId} 지급 못 함", this);
        }

        if (!string.IsNullOrEmpty(e.flag))
        {
            if (GameManager.Instance != null) GameManager.Instance.SetFlag(e.flag);
            else Debug.LogWarning($"[BastetRoomProgress] GameManager가 없어 {e.flag} 못 켬", this);
        }

        if (!string.IsNullOrEmpty(e.toast)) AnubisToast.Show(e.toast, toastTime);
    }

    // ================= 테스트 =================
    [ContextMenu("테스트: 다음 퍼즐 바로 클리어 처리 (보상만 지급)")]
    private void TestGrantNext()
    {
        if (!Application.isPlaying || ItemInventory.Instance == null) return;
        foreach (Entry e in entries)
        {
            if (e == null || string.IsNullOrEmpty(e.rewardItemId) || ItemInventory.Instance.Has(e.rewardItemId)) continue;
            HandleSolved(e);
            return;
        }
        Debug.Log("[BastetRoomProgress] 보상을 모두 갖고 있음");
    }

#if UNITY_EDITOR
    public void EditorSetEntries(Entry[] newEntries) => entries = newEntries;
#endif
}
