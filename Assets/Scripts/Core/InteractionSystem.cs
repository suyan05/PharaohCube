using System;
using UnityEngine;

public class InteractionSystem : MonoBehaviour
{
    [Header("상호작용 범위 (기획서 기준 1.2m)")]
    [SerializeField] private float interactRange = 1.2f;

    [Header("상호작용 키")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private IInteractable currentTarget;

    // 화면 안내용: 지금 가까이 있는 대상의 안내 문구 (없으면 null)
    public string CurrentPrompt => currentTarget != null ? currentTarget.GetPrompt() : null;

    // [추가] 지금 가까이 있는 대상의 위치 (오브젝트 위에 E 말풍선 띄우기용, 없으면 null)
    public Transform CurrentTargetTransform => currentTarget is Component c && c != null ? c.transform : null;

    // E키로 무언가를 조사한 순간 (플레이어 조사 동작 애니메이션용)
    public event Action<IInteractable> OnInteracted;

    private void Update()
    {
        FindNearestTarget();

        if (currentTarget != null && Input.GetKeyDown(interactKey))
        {
            TryInteract(currentTarget);
        }
    }

    private void TryInteract(IInteractable target)
    {
        try
        {
            target.Interact(gameObject);
            OnInteracted?.Invoke(target);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Interaction] 상호작용 오류: {e}");
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