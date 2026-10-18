using UnityEngine;

// 바스테트 튜토리얼 제단: 가까이 가서 E -> 같은 오브젝트의 BastetTutorialPuzzle 화면을 엶
// 필요: Collider2D (InteractionSystem의 OverlapCircle에 잡혀야 함), BastetTutorialPuzzle
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(BastetTutorialPuzzle))]
public class BastetTutorialAltar : MonoBehaviour, IInteractable
{
    [Header("안내 문구")]
    [Tooltip("비우면 데이터 제목을 써서 'E: <제목> 조사'")]
    [SerializeField] private string promptOverride = "";

    [Header("클리어 표시")]
    [Tooltip("풀면 제단 그림을 이 색으로 (금빛)")]
    [SerializeField] private Color clearedTint = new Color(1f, 0.85f, 0.45f, 1f);
    [Tooltip("풀면 켤 오브젝트 (불꽃, 빛 등 / 선택)")]
    [SerializeField] private GameObject clearedVisual;

    private BastetTutorialPuzzle puzzle;
    private SpriteRenderer sr;
    private string prompt;

    private void Awake()
    {
        puzzle = GetComponent<BastetTutorialPuzzle>();
        sr = GetComponent<SpriteRenderer>();
        if (clearedVisual != null) clearedVisual.SetActive(false);

        // 문구는 한 번만 만들어 둠 (GetPrompt가 매 프레임 불려도 문자열 할당 없음)
        string title = puzzle.Data != null ? puzzle.Data.Title : name;
        prompt = string.IsNullOrEmpty(promptOverride) ? $"E: {title} 조사" : promptOverride;
    }

    private void OnEnable() => puzzle.OnSuccess += HandleSuccess;
    private void OnDisable() => puzzle.OnSuccess -= HandleSuccess;

    public string GetPrompt() => puzzle.IsCleared ? "통과한 시험" : prompt;

    public void Interact(GameObject player)
    {
        if (PuzzleOverlayFocus.IsActive || puzzle.IsOpen) return; // 다른 화면이 열려 있으면 무시
        Debug.Log($"[BastetTutorialAltar] {name} 상호작용");
        puzzle.Open();
    }

    private void HandleSuccess()
    {
        if (sr != null) sr.color = clearedTint;
        if (clearedVisual != null) clearedVisual.SetActive(true);
    }

    private void OnDrawGizmos()
    {
        // 상호작용 범위 (InteractionSystem 1.2)
        Gizmos.color = puzzle != null && puzzle.IsCleared ? Color.green : new Color(1f, 0.8f, 0.2f, 1f);
        Gizmos.DrawWireSphere(transform.position, 1.2f);
    }
}
