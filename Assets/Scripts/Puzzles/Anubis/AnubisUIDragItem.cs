using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class AnubisUIDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("아이템 설정")]
    public string itemName = "유물";
    public float weightValue = 1.0f;
    public bool isHeart = false; // [추가] 심장 여부 체크

    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private Vector2 originalAnchoredPos;
    private Coroutine dropRoutine;

    public AnubisUIScalePan CurrentPan { get; private set; }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void Start()
    {
        originalParent = transform.parent;
        originalAnchoredPos = rectTransform.anchoredPosition;
    }

    private void LateUpdate()
    {
        rectTransform.rotation = Quaternion.identity;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (dropRoutine != null) StopCoroutine(dropRoutine);

        if (CurrentPan != null)
        {
            CurrentPan.RemoveItem(this);
            CurrentPan = null;
        }

        canvasGroup.blocksRaycasts = false;
        transform.SetParent(canvas.transform);
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        AnubisUIScalePan targetPan = GetHoveredPan(eventData.position);

        if (targetPan != null)
        {
            targetPan.AddItem(this);
        }
        else
        {
            ReturnToOriginalSlot();
        }
    }

    // [추가] 오답 시 또는 놓쳤을 때 원래 슬롯으로 복귀하는 함수
    public void ReturnToOriginalSlot()
    {
        if (dropRoutine != null) StopCoroutine(dropRoutine);

        if (CurrentPan != null)
        {
            CurrentPan.RemoveItem(this);
            CurrentPan = null;
        }

        transform.SetParent(originalParent);
        rectTransform.anchoredPosition = originalAnchoredPos;
        rectTransform.localRotation = Quaternion.identity;
    }

    private AnubisUIScalePan GetHoveredPan(Vector2 mouseScreenPos)
    {
        AnubisUIScalePan[] pans = Object.FindObjectsByType<AnubisUIScalePan>(FindObjectsSortMode.None);
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        foreach (var pan in pans)
        {
            RectTransform panRect = pan.GetComponent<RectTransform>();
            Vector2 panScreenPos = RectTransformUtility.WorldToScreenPoint(cam, panRect.position);

            float deltaX = Mathf.Abs(mouseScreenPos.x - panScreenPos.x);
            float deltaY = mouseScreenPos.y - panScreenPos.y;

            if (deltaX < 140f * canvas.scaleFactor && deltaY > -30f * canvas.scaleFactor && deltaY < 350f * canvas.scaleFactor)
            {
                return pan;
            }
        }
        return null;
    }

    public void FallToPan(AnubisUIScalePan pan, Vector2 targetLocalPos)
    {
        CurrentPan = pan;
        transform.SetParent(pan.transform);
        rectTransform.localRotation = Quaternion.identity;

        if (dropRoutine != null) StopCoroutine(dropRoutine);
        dropRoutine = StartCoroutine(DropAnimation(targetLocalPos));
    }

    private IEnumerator DropAnimation(Vector2 target)
    {
        Vector2 start = rectTransform.anchoredPosition;
        float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = t * t;
            rectTransform.anchoredPosition = Vector2.Lerp(start, target, t);
            yield return null;
        }

        rectTransform.anchoredPosition = target;
        rectTransform.localRotation = Quaternion.identity;
    }
}