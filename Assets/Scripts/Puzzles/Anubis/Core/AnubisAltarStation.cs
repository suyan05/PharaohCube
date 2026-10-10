using UnityEngine;
using UnityEngine.InputSystem;

// 아누비스 방 제단 공통 상호작용
// E키 → 오버레이 패널 열기 + 플레이어 이동 잠금
// Esc 또는 E키 → 닫기 (패널 내부 닫기 버튼으로 패널이 꺼져도 잠금 자동 해제)
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

    private void Start()
    {
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

        // 새 Input System 기준 Esc 닫기
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
    }

    public string GetPrompt() => isOpen ? "E: 닫기" : $"E: {promptName} 조사";

    // InteractionSystem이 E키로 호출
    public void Interact(GameObject player)
    {
        if (isOpen) Close();
        else Open(player);
    }

    private void Open(GameObject player)
    {
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
        overlayPanel.SetActive(false);
        Unlock();
        Debug.Log($"[{name}] 오버레이 닫힘", this);
    }

    private void Unlock()
    {
        isOpen = false;
        if (playerController != null) playerController.enabled = true;
    }

    // Scene 뷰에서 제단 콜라이더 범위를 노란 상자로 표시
    private void OnDrawGizmos()
    {
        var col = GetComponent<BoxCollider2D>();
        if (col == null) return;
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.8f);
        Gizmos.DrawWireCube(transform.position + (Vector3)col.offset, col.size);
    }
}