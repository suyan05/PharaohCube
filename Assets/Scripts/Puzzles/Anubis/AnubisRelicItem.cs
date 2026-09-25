using UnityEngine;
using UnityEngine.EventSystems;

public class AnubisRelicItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("유물 데이터")]
    public string relicName = "유물";
    public float actualWeight = 1.0f;  // 숨겨진 무게
    public bool isHeart = false;       // 심장 여부

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Transform originalParent;
    private Vector3 originalPosition;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        originalPosition = rectTransform.position;
        originalParent = transform.parent;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = false;
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        if (eventData.pointerEnter != null && eventData.pointerEnter.CompareTag("ScalePanLeft"))
        {
            transform.SetParent(eventData.pointerEnter.transform);
            rectTransform.position = eventData.pointerEnter.transform.position;
        }
        else
        {
            ReturnToSlot();
        }
    }

    public void ReturnToSlot()
    {
        transform.SetParent(originalParent);
        rectTransform.position = originalPosition;
    }
}