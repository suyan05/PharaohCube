using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// 메인 저울에 올리는 유물 아이템. 드래그하면 AnubisUIScalePan 위에 놓을 수 있음
// [수정] 잘못 올린 유물을 다시 뺄 수 있게
// - 클릭: 접시 위에 있으면 원래 자리로 / 진열대에 있으면 왼쪽 접시로 (드래그가 어려울 때도 동작)
// - 드래그: 접시 그림 위에 놓을 때만 올라가고, 그 밖에 놓으면 원래 자리로 돌아감
//   (기존에는 접시 위쪽 넓은 범위에 놓으면 다시 접시로 올라가서 빼기가 어려웠음)
public class AnubisUIDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [Header("아이템 설정")]
    public string itemName = "유물";
    public float weightValue = 1.0f;
    public bool isHeart = false;
    public bool isGold = false;

    [Header("놓기 판정 [추가]")]
    [Tooltip("접시 그림 둘레로 이만큼(px)까지는 접시에 놓은 것으로 봄")]
    [SerializeField] private float dropPadding = 40f;
    [Tooltip("클릭했을 때 올릴 접시. 비우면 이름에 Left가 들어간 접시를 자동으로 찾음")]
    [SerializeField] private AnubisUIScalePan clickTargetPan;

    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private Vector2 originalAnchoredPos;
    private int originalSiblingIndex;
    private Coroutine dropRoutine;

    private AnubisUIScalePan[] pans;                                  // 씬의 접시 (Start에서 한 번만 찾음)
    private readonly List<RectTransform> rectBuffer = new List<RectTransform>(16); // 판정용 재사용 목록
    private readonly Vector3[] corners = new Vector3[4];

    public AnubisUIScalePan CurrentPan { get; private set; }

    // [추가] 유물 높이의 절반 (월드 단위). 접시가 그릇 바닥 위에 올려놓을 때 사용
    public float WorldHalfHeight
    {
        get
        {
            rectTransform.GetWorldCorners(corners);
            return Mathf.Abs(corners[1].y - corners[0].y) * 0.5f;
        }
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void Start()
    {
        originalParent = transform.parent;
        originalAnchoredPos = rectTransform.anchoredPosition;
        originalSiblingIndex = transform.GetSiblingIndex();

        pans = FindObjectsByType<AnubisUIScalePan>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (clickTargetPan == null)
        {
            foreach (var p in pans)
                if (p.name.Contains("Left")) { clickTargetPan = p; break; }
            if (clickTargetPan == null && pans.Length > 0) clickTargetPan = pans[0];
        }
    }

    private void LateUpdate()
    {
        rectTransform.rotation = Quaternion.identity;
    }

    // ================= 클릭 [추가] =================
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.dragging) return; // 드래그 끝에 오는 클릭은 무시

        if (CurrentPan != null)
        {
            Debug.Log($"[DragItem] {itemName} 클릭 -> 접시에서 내림", this);
            ReturnToOriginalSlot();
        }
        else if (clickTargetPan != null)
        {
            Debug.Log($"[DragItem] {itemName} 클릭 -> {clickTargetPan.name}에 올림", this);
            clickTargetPan.AddItem(this);
        }
    }

    // ================= 드래그 =================
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
        Debug.Log($"[DragItem] {itemName} 드래그 시작", this);
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

    // 오답 시, 놓쳤을 때, 클릭으로 내릴 때 원래 슬롯으로 복귀
    public void ReturnToOriginalSlot()
    {
        if (dropRoutine != null) StopCoroutine(dropRoutine);

        if (CurrentPan != null)
        {
            CurrentPan.RemoveItem(this);
            CurrentPan = null;
        }

        canvasGroup.blocksRaycasts = true;
        transform.SetParent(originalParent);
        transform.SetSiblingIndex(Mathf.Min(originalSiblingIndex, originalParent.childCount - 1)); // [추가] 원래 그리는 순서
        rectTransform.anchoredPosition = originalAnchoredPos;
        rectTransform.localRotation = Quaternion.identity;
    }

    // [수정] 접시(와 접시 그림 자식들)가 화면에서 차지하는 범위 + 여유 안에 놓았을 때만 접시로 판정
    private AnubisUIScalePan GetHoveredPan(Vector2 mouseScreenPos)
    {
        if (pans == null) return null;
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        float pad = dropPadding * canvas.scaleFactor;

        foreach (var pan in pans)
        {
            if (pan == null || !pan.isActiveAndEnabled) continue;
            if (GetScreenBounds(pan, cam, out Rect r) &&
                mouseScreenPos.x >= r.xMin - pad && mouseScreenPos.x <= r.xMax + pad &&
                mouseScreenPos.y >= r.yMin - pad && mouseScreenPos.y <= r.yMax + pad)
            {
                return pan;
            }
        }
        return null;
    }

    // 접시와 그 아래 그림(사슬, 판)의 화면 범위. 올라가 있는 유물은 제외
    private bool GetScreenBounds(AnubisUIScalePan pan, Camera cam, out Rect bounds)
    {
        bounds = default;
        bool any = false;
        float xMin = float.MaxValue, yMin = float.MaxValue, xMax = float.MinValue, yMax = float.MinValue;

        pan.GetComponentsInChildren(false, rectBuffer);
        foreach (RectTransform rt in rectBuffer)
        {
            if (rt.GetComponentInParent<AnubisUIDragItem>() != null) continue;

            rt.GetWorldCorners(corners);
            for (int i = 0; i < 4; i++)
            {
                Vector2 s = RectTransformUtility.WorldToScreenPoint(cam, corners[i]);
                xMin = Mathf.Min(xMin, s.x); xMax = Mathf.Max(xMax, s.x);
                yMin = Mathf.Min(yMin, s.y); yMax = Mathf.Max(yMax, s.y);
                any = true;
            }
        }
        if (any) bounds = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        return any;
    }

    // [수정] 화면(월드) 좌표 목표로 이동 -> 접시 크기, 기준점, 앵커가 달라도 그릇 안에 정확히 놓임
    public void FallToPan(AnubisUIScalePan pan, Vector3 targetWorldPos)
    {
        CurrentPan = pan;
        transform.SetParent(pan.transform, true); // 지금 화면 위치 유지한 채 접시 밑으로
        transform.SetAsLastSibling();             // 접시 그림보다 위에 그려서 다시 잡을 수 있게
        canvasGroup.blocksRaycasts = true;
        rectTransform.localRotation = Quaternion.identity;

        // 접시 안 좌표로 바꿔서 이동 (저울이 기울면 접시와 같이 움직임)
        Vector3 targetLocal = pan.transform.InverseTransformPoint(targetWorldPos);

        if (dropRoutine != null) StopCoroutine(dropRoutine);
        dropRoutine = StartCoroutine(DropAnimation(targetLocal));
    }

    private IEnumerator DropAnimation(Vector3 targetLocal)
    {
        Vector3 start = rectTransform.localPosition;
        targetLocal.z = start.z;
        float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t;
            rectTransform.localPosition = Vector3.Lerp(start, targetLocal, t);
            yield return null;
        }

        rectTransform.localPosition = targetLocal;
        rectTransform.localRotation = Quaternion.identity;
        dropRoutine = null;
    }
}
