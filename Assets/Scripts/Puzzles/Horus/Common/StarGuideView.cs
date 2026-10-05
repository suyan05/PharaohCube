using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 목표 성좌를 흐린 선으로 깔고, 시작 별을 깜빡여 주는 안내 (튜토리얼 / 힌트 공용)
public class StarGuideView : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private StarBoardView board;
    [SerializeField] private RectTransform guideContainer;
    [SerializeField] private RectTransform linePrefab;

    [Header("흐린 선")]
    [SerializeField] private Color guideColor = new Color(1f, 1f, 1f, 0.25f);
    [SerializeField] private float guideWidth = 6f;

    [Header("시작 별 표시 (Sprite 비우면 네모)")]
    [SerializeField] private Sprite markerSprite;
    [SerializeField] private Color markerColor = new Color(1f, 0.95f, 0.6f, 1f);
    [SerializeField] private float markerSize = 60f;
    [SerializeField] private float pulseSpeed = 3f;

    private readonly List<GameObject> guideLines = new List<GameObject>();
    private readonly List<Image> markers = new List<Image>();

    public void ShowGuide(IEnumerable<string> edgeKeys)
    {
        try
        {
            HideGuide();
            if (!CheckRefs() || edgeKeys == null) return;
            foreach (string key in edgeKeys) CreateGuideLine(key);
        }
        catch (Exception e)
        {
            Debug.LogError($"[StarGuideView] ShowGuide 오류: {e}");
        }
    }

    private void CreateGuideLine(string key)
    {
        if (!StarGraph.TryParseEdge(key, out Vector2Int a, out Vector2Int b)) return;
        if (!board.TryGetStar(a, out StarPoint sa) || !board.TryGetStar(b, out StarPoint sb)) return;
        RectTransform line = Instantiate(linePrefab, guideContainer);
        line.gameObject.SetActive(true);
        line.sizeDelta = new Vector2(line.sizeDelta.x, guideWidth);
        Image img = line.GetComponent<Image>();
        if (img != null)
        {
            img.color = guideColor;
            img.raycastTarget = false;
        }
        PlaceLine(line, LocalOf(sa.Rect), LocalOf(sb.Rect));
        guideLines.Add(line.gameObject);
    }

    public void ShowStartMarkers(IEnumerable<Vector2Int> coords)
    {
        try
        {
            HideStartMarkers();
            if (!CheckRefs() || coords == null) return;
            foreach (Vector2Int c in coords) CreateMarker(c);
        }
        catch (Exception e)
        {
            Debug.LogError($"[StarGuideView] ShowStartMarkers 오류: {e}");
        }
    }

    private void CreateMarker(Vector2Int coord)
    {
        if (!board.TryGetStar(coord, out StarPoint star)) return;
        var go = new GameObject($"StartMarker_{coord.x}_{coord.y}", typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(guideContainer, false);
        rt.sizeDelta = new Vector2(markerSize, markerSize);
        rt.anchoredPosition = LocalOf(star.Rect);
        var img = go.GetComponent<Image>();
        img.sprite = markerSprite;
        img.color = markerColor;
        img.raycastTarget = false;
        markers.Add(img);
    }

    public void HideGuide()
    {
        foreach (GameObject go in guideLines) if (go != null) Destroy(go);
        guideLines.Clear();
    }

    public void HideStartMarkers()
    {
        foreach (Image img in markers) if (img != null) Destroy(img.gameObject);
        markers.Clear();
    }

    public void HideAll()
    {
        HideGuide();
        HideStartMarkers();
    }

    // 시작 별 표시가 천천히 밝아졌다 어두워졌다 함
    private void Update()
    {
        if (markers.Count == 0) return;
        float t = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * pulseSpeed);
        Color c = markerColor;
        c.a = Mathf.Lerp(0.25f, 1f, t);
        foreach (Image img in markers) if (img != null) img.color = c;
    }

    private Vector2 LocalOf(RectTransform rt)
    {
        return guideContainer.InverseTransformPoint(rt.position);
    }

    private void PlaceLine(RectTransform line, Vector2 from, Vector2 to)
    {
        Vector2 diff = to - from;
        float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        line.anchoredPosition = from;
        line.sizeDelta = new Vector2(diff.magnitude, line.sizeDelta.y);
        line.localRotation = Quaternion.Euler(0, 0, angle);
    }

    private bool CheckRefs()
    {
        if (board != null && guideContainer != null && linePrefab != null) return true;
        Debug.LogError("[StarGuideView] Board / Guide Container / Line Prefab 연결 필요");
        return false;
    }
}