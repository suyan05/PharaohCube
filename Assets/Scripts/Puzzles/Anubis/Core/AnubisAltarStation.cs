using UnityEngine;
using UnityEngine.InputSystem;

// 아누비스 방 제단 공통 상호작용
// E키 → 오버레이 패널 열기 + 플레이어 이동 잠금 / Esc 또는 E키 → 닫기
// [추가] 같은 오브젝트에 AnubisAltarLock이 있고 잠겨 있으면 퍼즐을 열지 않음
[RequireComponent(typeof(Collider2D))]
public class AnubisAltarStation : MonoBehaviour, IInteractable
{
    [Header("연결")]
    [Tooltip("이 제단이 여는 퍼즐 패널 (퍼즐 스크립트가 붙은 루트 오브젝트)")]
    [SerializeField] private GameObject overlayPanel;

    [Header("안내 문구")]
    [Tooltip("말풍선에 표시될 이름. 예: 모래 분배기")]
    [SerializeField] private string promptName = "제단";

    private bool isOpen;
    private PlayerController2D playerController;
    private Rigidbody2D playerRb;
    private AnubisAltarLock altarLock; 

    private bool IsLocked => altarLock != null && altarLock.IsLocked; 

    private void Start()
    {
        altarLock = GetComponent<AnubisAltarLock>();

        if (overlayPanel == null)
        {
            Debug.LogError($"[{name}] overlayPanel 미연결", this);
            return;
        }
        overlayPanel.SetActive(false);
    }

    private void Update()
    {
        if (!isOpen) return;

        if (!overlayPanel.activeSelf)
        {
            Unlock();
            return;
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
    }

    public string GetPrompt()
    {
        if (isOpen) return "E: 닫기";
        if (IsLocked) return $"E: {promptName} (봉인됨)";
        return $"E: {promptName} 조사";
    }

    public void Interact(GameObject player)
    {
        if (isOpen)
        {
            Close();
            return;
        }

        if (IsLocked)
        {
            altarLock.NotifyLocked();
            return;
        }

        Open(player);
    }

    private void Open(GameObject player)
    {
        if (overlayPanel == null) return;

        playerController = player.GetComponent<PlayerController2D>();
        playerRb = player.GetComponent<Rigidbody2D>();

        if (playerController != null) playerController.enabled = false;

        if (playerRb != null) playerRb.linearVelocity = Vector2.zero;

        isOpen = true;
        overlayPanel.SetActive(true);
        Debug.Log($"[{name}] 오버레이 열림", this);
    }

    public void Close()
    {
        if (overlayPanel != null) overlayPanel.SetActive(false);
        Unlock();
        Debug.Log($"[{name}] 오버레이 닫힘", this);
    }

    private void Unlock()
    {
        isOpen = false;
        if (playerController != null) playerController.enabled = true;
    }

    private void OnDrawGizmos()
    {
        var col = GetComponent<BoxCollider2D>();
        if (col == null) return;
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.8f);
        Gizmos.DrawWireCube(transform.position + (Vector3)col.offset, col.size);
    }
}