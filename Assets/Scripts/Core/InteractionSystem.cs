using UnityEngine;

public class InteractionSystem : MonoBehaviour
{
    [Header("상호작용 범위 (기획서 기준 1.2m)")]
    [SerializeField] private float interactRange = 1.2f;

    [Header("상호작용 키")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private IInteractable currentTarget;

    private void Update()
    {
        FindNearestTarget();

        if (currentTarget != null && Input.GetKeyDown(interactKey))
        {
            currentTarget.Interact(gameObject);
        }
    }

    private void FindNearestTarget()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactRange);

        IInteractable nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            IInteractable interactable = hit.GetComponent<IInteractable>();
            if (interactable == null) continue; // 플레이어 자신, 바닥 등은 무시

            float dist = Vector2.Distance(transform.position, hit.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = interactable;
            }
        }

        // 대상이 바뀌었을 때만 로그 (매 프레임 도배 방지)
        if (nearest != currentTarget)
        {
            currentTarget = nearest;
            if (currentTarget != null)
                Debug.Log($"[Interaction] {currentTarget.GetPrompt()}");
            else
                Debug.Log("[Interaction] 범위 내 대상 없음");
        }
    }

    // Scene 뷰에서 Player 선택하면 1.2m 범위가 노란 원으로 보임
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}