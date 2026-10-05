using System.Collections.Generic;
using UnityEngine;

public class HorusPuzzleUI : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private HorusPuzzleManager puzzleManager;

    [Header("별 오브젝트들 (index 순서대로)")]
    [SerializeField] private RectTransform[] starPoints;

    [Header("드래그 중 보여줄 미리보기 선")]
    [SerializeField] private RectTransform dragPreviewLine;

    [Header("완성된 선을 그릴 부모 (드래그 선이랑 같은 부모여야 함)")]
    [SerializeField] private RectTransform lineContainer;

    [Header("선 프리팹 (가로로 긴 이미지)")]
    [SerializeField] private RectTransform linePrefab;

    [Header("별에 가까워졌다고 인정할 거리(픽셀)")]
    [SerializeField] private float snapDistance = 60f;

    private int? dragStartIndex = null;
    private List<RectTransform> drawnLines = new List<RectTransform>();
    private Canvas canvas;

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
    }

    private void Start()
    {
        if (dragPreviewLine != null) dragPreviewLine.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        puzzleManager.OnConnectionMade += HandleConnectionMade;
        puzzleManager.OnPuzzleReset += HandleReset;
        puzzleManager.OnPuzzleCleared += HandleCleared;
    }

    private void OnDisable()
    {
        puzzleManager.OnConnectionMade -= HandleConnectionMade;
        puzzleManager.OnPuzzleReset -= HandleReset;
        puzzleManager.OnPuzzleCleared -= HandleCleared;
    }

    public void BeginDrag(int index)
    {
        dragStartIndex = index;
        if (dragPreviewLine != null) dragPreviewLine.gameObject.SetActive(true);
    }

    private void Update()
    {
        if (dragStartIndex == null) return;

        if (dragPreviewLine != null)
        {
            DrawLine(dragPreviewLine, starPoints[dragStartIndex.Value].position, Input.mousePosition);
        }

        if (Input.GetMouseButtonUp(0))
        {
            EndDrag();
        }
    }

    private void EndDrag()
    {
        if (dragPreviewLine != null) dragPreviewLine.gameObject.SetActive(false);

        int? targetIndex = FindNearestStar(Input.mousePosition);

        if (targetIndex.HasValue && targetIndex.Value != dragStartIndex.Value)
        {
            puzzleManager.TryConnect(dragStartIndex.Value, targetIndex.Value);
        }
        else
        {
            Debug.Log("[HorusPuzzleUI] 별 위에서 손을 떼지 않아서 연결 취소됨");
        }

        dragStartIndex = null;
    }

    private int? FindNearestStar(Vector2 screenPosition)
    {
        for (int i = 0; i < starPoints.Length; i++)
        {
            float dist = Vector2.Distance(starPoints[i].position, screenPosition);
            if (dist <= snapDistance)
            {
                return i;
            }
        }
        return null;
    }

    private void HandleConnectionMade(int fromIndex, int toIndex)
    {
        RectTransform newLine = Instantiate(linePrefab, lineContainer);
        newLine.gameObject.SetActive(true);
        DrawLine(newLine, starPoints[fromIndex].position, starPoints[toIndex].position);
        drawnLines.Add(newLine);
    }

    private void HandleReset()
    {
        foreach (var line in drawnLines)
        {
            Destroy(line.gameObject);
        }
        drawnLines.Clear();
    }

    private void HandleCleared()
    {
        Debug.Log("[HorusPuzzleUI] 별자리 완성! (연출은 나중에 추가 예정)");
    }

    private void DrawLine(RectTransform line, Vector3 fromScreen, Vector3 toScreen)
    {
        Vector2 fromLocal = ScreenToLocal(fromScreen);
        Vector2 toLocal = ScreenToLocal(toScreen);

        Vector2 diff = toLocal - fromLocal;
        float distance = diff.magnitude;
        float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

        line.anchoredPosition = fromLocal;
        line.sizeDelta = new Vector2(distance, line.sizeDelta.y);
        line.localRotation = Quaternion.Euler(0, 0, angle);
    }

    private Vector2 ScreenToLocal(Vector3 screenPosition)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            lineContainer, screenPosition,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out Vector2 localPoint);
        return localPoint;
    }
}