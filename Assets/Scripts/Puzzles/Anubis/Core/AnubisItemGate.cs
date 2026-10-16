using UnityEngine;

// 석문: 필요한 아이템을 모두 얻으면 열림 (실전 G 석문 / 튜토리얼 출구 문 공용)
// 닫혀 있는 동안 Collider2D가 길을 막고, 열리면 Collider2D를 끄고 그림을 숨김
[RequireComponent(typeof(Collider2D))]
public class AnubisItemGate : MonoBehaviour, IInteractable
{
    [Header("열림 조건 (서브 퍼즐 보상 4개)")]
    [SerializeField]
    private string[] requiredItems =
    {
        AnubisItemIds.GoldJar, AnubisItemIds.ClayPot, AnubisItemIds.Obsidian, AnubisItemIds.BronzeBowl
    };

    [Header("메시지")]
    [TextArea(1, 3)]
    [SerializeField] private string lockedMessage = "네 개의 시험을 모두 통과해야 석문이 열립니다.";
    [TextArea(1, 3)]
    [SerializeField] private string openMessage = "육중한 석문이 열립니다!";

    [Header("연출 (선택)")]
    [Tooltip("열리면 끌 오브젝트. 비우면 이 오브젝트의 SpriteRenderer를 끔")]
    [SerializeField] private GameObject closedVisual;
    [Tooltip("열리면 켤 오브젝트")]
    [SerializeField] private GameObject openVisual;

    private Collider2D blocker;
    private SpriteRenderer sr;
    private bool isOpen;
    private bool subscribed;

    private int RequiredCount => requiredItems != null ? requiredItems.Length : 0;

    private void Awake()
    {
        blocker = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
        if (openVisual != null) openVisual.SetActive(false);
    }

    private void Start()
    {
        if (ItemInventory.Instance == null)
        {
            Debug.LogError($"[{name}] ItemInventory가 씬에 없음 (Managers 확인)", this);
            return;
        }
        ItemInventory.Instance.OnItemGranted += HandleItemGranted;
        ItemInventory.Instance.OnItemConsumed += HandleItemConsumed; // [추가] 실패 처벌로 회수되면 다시 닫힘
        subscribed = true;
        Refresh(false);
    }

    private void OnDestroy()
    {
        if (subscribed && ItemInventory.Instance != null)
        {
            ItemInventory.Instance.OnItemGranted -= HandleItemGranted;
            ItemInventory.Instance.OnItemConsumed -= HandleItemConsumed;
        }
    }

    private void HandleItemGranted(string itemId) => Refresh(true);

    // [추가] 보상을 잃어 개수가 모자라면 석문을 다시 닫음
    private void HandleItemConsumed(string itemId)
    {
        if (isOpen && CountOwned() < RequiredCount) Close();
    }

    private void Close()
    {
        isOpen = false;
        blocker.enabled = true;

        if (closedVisual != null) closedVisual.SetActive(true);
        else if (sr != null) sr.enabled = true;
        if (openVisual != null) openVisual.SetActive(false);

        Debug.Log($"[{name}] 석문이 다시 닫힘", this);
    }

    private int CountOwned()
    {
        if (ItemInventory.Instance == null || requiredItems == null) return 0;
        int n = 0;
        foreach (var id in requiredItems) if (ItemInventory.Instance.Has(id)) n++;
        return n;
    }

    private void Refresh(bool announce)
    {
        if (isOpen) return;
        int owned = CountOwned();
        Debug.Log($"[{name}] 진행 {owned}/{RequiredCount}", this);
        if (owned >= RequiredCount) Open(announce);
    }

    private void Open(bool announce)
    {
        isOpen = true;
        blocker.enabled = false;

        if (closedVisual != null) closedVisual.SetActive(false);
        else if (sr != null) sr.enabled = false;
        if (openVisual != null) openVisual.SetActive(true);

        if (announce) AnubisToast.Show(openMessage, 4f);
    }

    public string GetPrompt() => isOpen ? "" : $"E: 봉인된 석문 ({CountOwned()}/{RequiredCount})";

    public void Interact(GameObject player)
    {
        if (!isOpen) AnubisToast.Show($"{lockedMessage}  ({CountOwned()}/{RequiredCount})");
    }

    [ContextMenu("테스트: 석문 강제 열기")]
    private void TestOpen() => Open(true);

    // Scene 뷰: 닫힘 = 빨강 / 열림 = 초록
    private void OnDrawGizmos()
    {
        var col = GetComponent<BoxCollider2D>();
        if (col == null) return;
        Gizmos.color = isOpen ? new Color(0.2f, 1f, 0.3f, 0.8f) : new Color(1f, 0.2f, 0.2f, 0.8f);
        Gizmos.DrawWireCube(transform.position + (Vector3)col.offset, col.size);
    }
}