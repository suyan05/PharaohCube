using UnityEngine;
using UnityEngine.InputSystem;

// 바스테트 제단: E -> 연결된 퍼즐 팝업 열기
// [추가] 잠금: 앞 퍼즐 보상(requiredItemId)이 없으면 안내만 띄우고 열지 않음 (B1 -> B2 -> B3 -> B4 -> B5 순서)
// [추가] 팝업이 열려 있는 동안 플레이어 이동 잠금 + Esc로 닫기 + 다른 시스템에 "퍼즐 열림" 알림
[RequireComponent(typeof(Collider2D))]
public class BastetAltarInteractable : MonoBehaviour, IInteractable
{
    [Header("안내 문구 및 대상 퍼즐")]
    [SerializeField] private string promptText = "고양이 발자국 제단 조사";
    [SerializeField] private PuzzleBase targetPuzzle;
    [SerializeField] private GameObject puzzleUIPanel;

    [Header("[추가] 잠금 (비우면 처음부터 열림)")]
    [Tooltip("이 아이템이 있어야 열림. 예: B2 제단 = ITEM_BASTET_PAW (B1 보상)")]
    [SerializeField] private string requiredItemId = "";
    [TextArea(1, 2)]
    [SerializeField] private string lockedMessage = "제단이 아직 잠들어 있다. 앞의 시험을 먼저 통과하자.";

    [Header("[추가] 열려 있는 동안")]
    [Tooltip("팝업이 열려 있는 동안 플레이어 이동 잠금")]
    [SerializeField] private bool lockPlayerWhileOpen = true;
    [Tooltip("Esc로 팝업 닫기")]
    [SerializeField] private bool closeWithEscape = true;

    private PlayerController2D player;
    private Rigidbody2D playerRb;
    private bool isOpen;

    public bool IsUnlocked => string.IsNullOrEmpty(requiredItemId)
                              || (ItemInventory.Instance != null && ItemInventory.Instance.Has(requiredItemId));
    public PuzzleBase TargetPuzzle => targetPuzzle;

    public string GetPrompt()
    {
        if (targetPuzzle != null && targetPuzzle.IsCleared) return $"E: {promptText} (완료)";
        return IsUnlocked ? $"E: {promptText}" : $"E: {promptText} (잠김)";
    }

    public void Interact(GameObject player)
    {
        if (isOpen || PuzzleOverlayFocus.IsActive) return; // 이미 다른 화면이 열려 있으면 무시

        // [추가] 잠금
        if (!IsUnlocked)
        {
            AnubisToast.Show(lockedMessage);
            Debug.Log($"[BastetAltar] {name} 잠김: {requiredItemId} 필요", this);
            return;
        }

        if (puzzleUIPanel != null)
        {
            puzzleUIPanel.SetActive(true);
        }

        if (targetPuzzle != null)
        {
            targetPuzzle.Open();
        }

        SetOpen(true);
    }

    // [추가] 팝업이 닫혔는지는 이벤트가 없어서 열려 있는 동안만 bool 하나 확인 (할당 없음)
    private void Update()
    {
        if (!isOpen) return;

        if (closeWithEscape && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && puzzleUIPanel != null)
            puzzleUIPanel.SetActive(false);

        if (puzzleUIPanel == null || !puzzleUIPanel.activeInHierarchy) SetOpen(false);
    }

    private void OnDisable()
    {
        if (isOpen) SetOpen(false);
    }

    private void SetOpen(bool open)
    {
        if (isOpen == open) return;
        isOpen = open;

        if (open) PuzzleOverlayFocus.Enter(this);
        else PuzzleOverlayFocus.Exit(this);

        if (!lockPlayerWhileOpen) return;
        if (player == null)
        {
            player = FindAnyObjectByType<PlayerController2D>();
            if (player != null) playerRb = player.GetComponent<Rigidbody2D>();
        }
        if (player == null) return;

        player.enabled = !open;
        if (open && playerRb != null) playerRb.linearVelocity = Vector2.zero;
    }

#if UNITY_EDITOR
    // 에디터 세팅 메뉴용
    public void EditorSetup(string prompt, PuzzleBase puzzle, GameObject panel, string requiredItem, string lockedMsg)
    {
        promptText = prompt;
        targetPuzzle = puzzle;
        puzzleUIPanel = panel;
        requiredItemId = requiredItem;
        if (!string.IsNullOrEmpty(lockedMsg)) lockedMessage = lockedMsg;
    }
#endif
}
