using UnityEngine;

// 소베크 판 입구 돌 (E: 들어가기)
// 붙이는 곳: 판 바깥 입구 칸 (Tools/Sobek 메뉴가 Board Data의 Entrance 칸에 자동 배치)
// 트리거 콜라이더라 위에 서 있어도 막지 않음 -> InteractionSystem이 E 대상으로 찾음
[RequireComponent(typeof(Collider2D))]
public class SobekBoardEntrance : MonoBehaviour, IInteractable
{
    [SerializeField] private SobekBoard board;
    [Tooltip("다 푼 판의 돌 색")]
    [SerializeField] private Color solvedColor = new Color(0.91f, 0.70f, 0.23f);

    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (board == null) board = GetComponentInParent<SobekBoard>();
    }

    private void Start() => RefreshColor();

    private void OnEnable()
    {
        if (board != null) board.Cleared += HandleCleared;
    }

    private void OnDisable()
    {
        if (board != null) board.Cleared -= HandleCleared;
    }

    private void HandleCleared(SobekBoard b) => RefreshColor();

    private void RefreshColor()
    {
        if (sr != null && board != null && board.IsSolved) sr.color = solvedColor;
    }

    public string GetPrompt()
    {
        if (board == null || board.Data == null) return "";
        if (board.IsActive) return "";
        return board.IsSolved ? $"{board.Data.DisplayName} (풀었음)" : $"E: {board.Data.DisplayName} 들어가기";
    }

    public void Interact(GameObject player)
    {
        if (board == null || board.IsActive) return;
        board.Enter(player);
    }

#if UNITY_EDITOR
    public void EditorSetup(SobekBoard target) => board = target;
#endif
}
