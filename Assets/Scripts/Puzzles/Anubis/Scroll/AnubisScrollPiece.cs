using UnityEngine;

// 맵에 놓이는 양피지 조각. 가까이 가서 E키로 줍기
[RequireComponent(typeof(Collider2D))]
public class AnubisScrollPiece : MonoBehaviour, IInteractable
{
    [Header("어떤 양피지의 몇 번 조각인지")]
    [SerializeField] private AnubisScrollData scroll;
    [Tooltip("조각 번호 (1부터 시작)")]
    [Min(1)]
    [SerializeField] private int pieceNumber = 1;

    [Header("연결 (비우면 자동으로 찾음)")]
    [SerializeField] private AnubisScrollCollection collection;

    private void Start()
    {
        if (collection == null) collection = FindFirstObjectByType<AnubisScrollCollection>();
        if (collection == null) Debug.LogError($"[{name}] AnubisScrollCollection이 씬에 없음 (Managers 확인)", this);
        if (scroll == null) Debug.LogError($"[{name}] 양피지 데이터(Scroll) 미연결", this);

        // 이미 주운 조각이면 숨김
        if (collection != null && collection.Has(scroll, pieceNumber - 1)) gameObject.SetActive(false);
    }

    public string GetPrompt() => "E: 양피지 조각 줍기";

    public void Interact(GameObject player)
    {
        if (collection == null || scroll == null) return;
        collection.Collect(scroll, pieceNumber - 1);
        gameObject.SetActive(false);
    }

    // Scene 뷰: 조각 위치와 번호 표시
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.95f, 0.85f, 0.6f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, 0.35f);
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, $"{(scroll != null ? scroll.Title : "?")} #{pieceNumber}");
#endif
    }
}