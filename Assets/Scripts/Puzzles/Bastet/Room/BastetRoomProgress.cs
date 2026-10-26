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
    [Tooltip("[추가] 보상을 팀 공용 획득 팝업(ItemPopup)으로 보여 줌. 이름·설명·아이콘은 Resources/ItemCatalog")]
    [SerializeField] private bool showItemPopup = true;

    // 람다 대신 핸들러를 저장해 두고 OnDestroy에서 정확히 해제
    private Action[] handlers;
    private bool[] granted; // [추가] 이미 보상을 준 항목 (두 번 주기 방지)

    private void Start()
    {
        handlers = new Action[entries.Length];
        granted = new bool[entries.Length];
        for (int i = 0; i < entries.Length; i++)
        {
            Entry e = entries[i];
            if (e == null || e.puzzle == null)
            {
                Debug.LogWarning($"[BastetRoomProgress] {i + 1}번 항목의 퍼즐이 비어 있음", this);
                continue;
            }
            int idx = i;
            handlers[i] = () => Give(idx);
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

    // [추가] 퍼즐이 풀렸는데 보상을 못 받은 경우 다시 확인 (이 오브젝트가 늦게 켜졌거나 이벤트를 놓쳤을 때)
    private void Update()
    {
        if (granted == null) return;
        for (int i = 0; i < entries.Length; i++)
        {
            if (granted[i] || entries[i] == null || entries[i].puzzle == null || !entries[i].puzzle.IsCleared) continue;
            Debug.LogWarning($"[BastetRoomProgress] {entries[i].puzzle.name}는 풀렸는데 보상을 안 줌 -> 지금 지급", this);
            Give(i);
        }
    }

    private void Give(int i)
    {
        if (granted != null && i < granted.Length)
        {
            if (granted[i]) return;
            granted[i] = true;
        }
        HandleSolved(entries[i]);
    }

    private void HandleSolved(Entry e)
    {
        Debug.Log($"[BastetRoomProgress] {e.puzzle.name} 클리어 -> 보상 {e.rewardItemId} / 플래그 {e.flag}", this);

        bool popupShown = false;
        if (!string.IsNullOrEmpty(e.rewardItemId))
        {
            if (showItemPopup)
            {
                // [수정] 팀 공용 획득 팝업 (인벤토리 지급 + 팝업). 팝업이 닫히면 안내 문구
                string toast = e.toast;
                ItemPopup.Grant(e.rewardItemId, () => { if (!string.IsNullOrEmpty(toast)) AnubisToast.Show(toast, toastTime); });
                popupShown = true;
            }
            else if (ItemInventory.Instance != null) ItemInventory.Instance.Grant(e.rewardItemId);
            else Debug.LogWarning($"[BastetRoomProgress] ItemInventory가 없어 {e.rewardItemId} 지급 못 함", this);
        }

        if (!string.IsNullOrEmpty(e.flag))
        {
            if (GameManager.Instance != null) GameManager.Instance.SetFlag(e.flag);
            else Debug.LogWarning($"[BastetRoomProgress] GameManager가 없어 {e.flag} 못 켬", this);
        }

        if (!popupShown && !string.IsNullOrEmpty(e.toast)) AnubisToast.Show(e.toast, toastTime);
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
