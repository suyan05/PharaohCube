using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 드래그 한 번 = 1획. 드래그 중 옆 별 위를 지나가면 선이 하나씩 생김
public class StarLineDrawer : MonoBehaviour
{
    [Header("선을 그릴 부모 (Hierarchy에서 별 부모보다 위에 둘 것)")]
    [SerializeField] private RectTransform lineContainer;

    [Header("선 프리팹 (Pivot X 0 / Y 0.5, Raycast Target 끄기)")]
    [SerializeField] private RectTransform linePrefab;

    [Header("드래그 중 미리보기 선")]
    [SerializeField] private RectTransform previewLine;

    [Header("별에 닿았다고 인정할 거리 (별 간격 대비 비율)")]
    [SerializeField, Range(0.2f, 0.45f)] private float snapRatio = 0.35f;

    [Header("선 색 (금빛 #E8B23A)")]
    [SerializeField] private Color lineColor = new Color(0.91f, 0.70f, 0.23f);

    public event Action<string> OnSegmentAdded;
    public event Action<List<string>> OnStrokeEnded;
    public event Action<string> OnRejected;
    public event Action<StarPoint> OnBlockedStarPressed; // 꺼진 별/먹구름을 눌렀을 때 (별 조각 삽입용)

    public int StrokeCount => strokeEdges.Count;
    public IReadOnlyCollection<string> DrawnEdges => usedEdges;

    private readonly Dictionary<Vector2Int, StarPoint> stars = new Dictionary<Vector2Int, StarPoint>();
    private readonly HashSet<string> usedEdges = new HashSet<string>();
    private readonly List<List<string>> strokeEdges = new List<List<string>>();
    private readonly List<List<GameObject>> strokeLines = new List<List<GameObject>>();
    private List<string> currentEdges;
    private List<GameObject> currentLines;
    private StarPoint currentStar;
    private string lastRejectKey;
    private bool inputEnabled = true;
    private Vector2 lastMouse;
    private Canvas canvas;

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null) Debug.LogError("[StarLineDrawer] 부모에 Canvas가 없음");
        if (lineContainer == null || linePrefab == null) Debug.LogError("[StarLineDrawer] Line Container / Line Prefab 연결 필요");
        SetPreviewActive(false);
    }

    private void OnDisable()
    {
        CancelStroke();
    }

    public void RegisterStars(IEnumerable<StarPoint> list)
    {
        stars.Clear();
        foreach (StarPoint s in list)
        {
            if (s != null) stars[s.Coord] = s;
        }
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
        if (!enabled) CancelStroke();
    }

    public void SetLineColor(Color color)
    {
        lineColor = color;
    }

    public void BeginStroke(StarPoint start)
    {
        try
        {
            if (!inputEnabled || start == null || currentStar != null) return;
            lastRejectKey = null;
            if (!start.IsPassable)
            {
                // 퍼즐이 별 조각을 끼워 별을 켤 기회를 먼저 줌
                OnBlockedStarPressed?.Invoke(start);
                if (!start.IsPassable) Reject(ReasonFor(start), "start" + start.Coord);
                return;
            }
            currentStar = start;
            currentEdges = new List<string>();
            currentLines = new List<GameObject>();
            lastMouse = Input.mousePosition;
            SetPreviewActive(true);
        }
        catch (Exception e)
        {
            Debug.LogError($"[StarLineDrawer] BeginStroke 오류: {e}");
        }
    }

    private void Update()
    {
        if (currentStar == null) return;
        try
        {
            TrackPointer();
            UpdatePreview();
            if (Input.GetMouseButtonUp(0)) EndStroke();
        }
        catch (Exception e)
        {
            Debug.LogError($"[StarLineDrawer] Update 오류: {e}");
            CancelStroke();
        }
    }

    // 지난 프레임 마우스 위치 -> 지금 위치 사이를 잘게 나눠서 지나친 별도 검사
    private void TrackPointer()
    {
        Vector2 now = Input.mousePosition;
        float spacing = ScreenSpacing();
        if (spacing <= 0f) return;
        float step = Mathf.Max(1f, spacing * 0.2f);
        int samples = Mathf.CeilToInt(Vector2.Distance(lastMouse, now) / step);
        for (int i = 1; i <= samples; i++)
        {
            if (currentStar == null) break;
            Vector2 point = Vector2.Lerp(lastMouse, now, (float)i / samples);
            TryExtendAt(point, spacing * snapRatio);
        }
        lastMouse = now;
    }

    private void TryExtendAt(Vector2 point, float snapPx)
    {
        StarPoint hit = FindStarNear(point, snapPx);
        if (hit == null || hit == currentStar) return;
        if (!StarGraph.IsAdjacent(currentStar.Coord, hit.Coord)) return;
        if (!hit.IsPassable)
        {
            Reject(ReasonFor(hit), "star" + hit.Coord);
            return;
        }
        string key = StarGraph.EdgeKey(currentStar.Coord, hit.Coord);
        if (usedEdges.Contains(key))
        {
            Reject("같은 선은 두 번 지날 수 없다", "edge" + key);
            return;
        }
        AddSegment(currentStar, hit, key);
        currentStar = hit;
    }

    private void AddSegment(StarPoint from, StarPoint to, string key)
    {
        RectTransform line = Instantiate(linePrefab, lineContainer);
        line.gameObject.SetActive(true);
        SetImageColor(line.gameObject, lineColor);
        DrawLine(line, ScreenPos(from.Rect), ScreenPos(to.Rect));
        usedEdges.Add(key);
        currentEdges.Add(key);
        currentLines.Add(line.gameObject);
        OnSegmentAdded?.Invoke(key);
    }

    private void EndStroke()
    {
        SetPreviewActive(false);
        currentStar = null;
        if (currentEdges == null || currentEdges.Count == 0) return;
        strokeEdges.Add(currentEdges);
        strokeLines.Add(currentLines);
        var finished = new List<string>(currentEdges);
        currentEdges = null;
        currentLines = null;
        OnStrokeEnded?.Invoke(finished);
    }

    private void CancelStroke()
    {
        SetPreviewActive(false);
        if (currentEdges != null) RemoveStroke(currentEdges, currentLines);
        currentEdges = null;
        currentLines = null;
        currentStar = null;
    }

    public void UndoLastStroke()
    {
        try
        {
            if (currentStar != null || strokeEdges.Count == 0) return;
            int last = strokeEdges.Count - 1;
            RemoveStroke(strokeEdges[last], strokeLines[last]);
            strokeEdges.RemoveAt(last);
            strokeLines.RemoveAt(last);
        }
        catch (Exception e)
        {
            Debug.LogError($"[StarLineDrawer] Undo 오류: {e}");
        }
    }

    public void ClearAll()
    {
        CancelStroke();
        for (int i = 0; i < strokeEdges.Count; i++)
        {
            RemoveStroke(strokeEdges[i], strokeLines[i]);
        }
        strokeEdges.Clear();
        strokeLines.Clear();
        usedEdges.Clear();
    }

    // 지정한 선들만 색을 바꿈 (예: 틀린 선을 빨갛게)
    public void TintEdges(IEnumerable<string> keys, Color color)
    {
        if (keys == null) return;
        var set = new HashSet<string>(keys);
        for (int s = 0; s < strokeEdges.Count; s++)
        {
            for (int i = 0; i < strokeEdges[s].Count; i++)
            {
                if (set.Contains(strokeEdges[s][i])) SetImageColor(strokeLines[s][i], color);
            }
        }
    }

    // 지정한 선들만 지움 (나머지 선은 유지)
    public void RemoveEdges(IEnumerable<string> keys)
    {
        if (keys == null) return;
        var set = new HashSet<string>(keys);
        for (int s = strokeEdges.Count - 1; s >= 0; s--)
        {
            RemoveFromStroke(strokeEdges[s], strokeLines[s], set);
            if (strokeEdges[s].Count > 0) continue;
            strokeEdges.RemoveAt(s);
            strokeLines.RemoveAt(s);
        }
    }

    private void RemoveFromStroke(List<string> edges, List<GameObject> lines, HashSet<string> set)
    {
        for (int i = edges.Count - 1; i >= 0; i--)
        {
            if (!set.Contains(edges[i])) continue;
            usedEdges.Remove(edges[i]);
            if (lines[i] != null) Destroy(lines[i]);
            edges.RemoveAt(i);
            lines.RemoveAt(i);
        }
    }

    private void RemoveStroke(List<string> edges, List<GameObject> lines)
    {
        if (edges != null)
        {
            foreach (string e in edges) usedEdges.Remove(e);
        }
        if (lines != null)
        {
            foreach (GameObject go in lines) if (go != null) Destroy(go);
        }
    }

    private void SetImageColor(GameObject go, Color color)
    {
        if (go == null) return;
        Image img = go.GetComponent<Image>();
        if (img != null) img.color = color;
    }

    private string ReasonFor(StarPoint star)
    {
        if (star.State == StarState.Dark) return "빛이 꺼져 있다 - 별 조각이 필요하다";
        if (star.State == StarState.Cloud) return "먹구름은 지날 수 없다";
        return "지날 수 없는 별이다";
    }

    // 같은 이유로 매 프레임 메시지가 반복되지 않게 막음
    private void Reject(string reason, string key)
    {
        if (key == lastRejectKey) return;
        lastRejectKey = key;
        Debug.Log($"[별 잇기] {reason}");
        OnRejected?.Invoke(reason);
    }

    private StarPoint FindStarNear(Vector2 point, float snapPx)
    {
        StarPoint best = null;
        float bestDist = snapPx;
        foreach (StarPoint s in stars.Values)
        {
            float d = Vector2.Distance(ScreenPos(s.Rect), point);
            if (d <= bestDist)
            {
                bestDist = d;
                best = s;
            }
        }
        return best;
    }

    // 지금 화면에서 가로로 붙은 두 별 사이 거리(픽셀). 화면 크기가 바뀌어도 자동으로 맞춰짐
    private float ScreenSpacing()
    {
        foreach (StarPoint s in stars.Values)
        {
            Vector2Int right = new Vector2Int(s.Coord.x + 1, s.Coord.y);
            if (stars.TryGetValue(right, out StarPoint other))
            {
                return Vector2.Distance(ScreenPos(s.Rect), ScreenPos(other.Rect));
            }
        }
        return 0f;
    }

    private void UpdatePreview()
    {
        if (previewLine == null || currentStar == null) return;
        DrawLine(previewLine, ScreenPos(currentStar.Rect), Input.mousePosition);
    }

    private void SetPreviewActive(bool active)
    {
        if (previewLine != null) previewLine.gameObject.SetActive(active);
    }

    private Camera UICamera()
    {
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;
        return canvas.worldCamera;
    }

    private Vector2 ScreenPos(RectTransform rt)
    {
        return RectTransformUtility.WorldToScreenPoint(UICamera(), rt.position);
    }

    private void DrawLine(RectTransform line, Vector2 fromScreen, Vector2 toScreen)
    {
        Vector2 fromLocal = ScreenToLocal(fromScreen);
        Vector2 toLocal = ScreenToLocal(toScreen);
        Vector2 diff = toLocal - fromLocal;
        float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        line.anchoredPosition = fromLocal;
        line.sizeDelta = new Vector2(diff.magnitude, line.sizeDelta.y);
        line.localRotation = Quaternion.Euler(0, 0, angle);
    }

    private Vector2 ScreenToLocal(Vector2 screenPosition)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            lineContainer, screenPosition, UICamera(), out Vector2 localPoint);
        return localPoint;
    }
}