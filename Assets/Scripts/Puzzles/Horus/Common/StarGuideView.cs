using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 판 위에 선 묶음(레이어)을 깔고, 시작 별을 깜빡여 주는 안내 (튜토리얼 / 고정선 / 힌트 공용)
public class StarGuideView : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private StarBoardView board;
    [SerializeField] private RectTransform guideContainer;
    [SerializeField] private RectTransform linePrefab;

    [Header("기본 흐린 선")]
    [SerializeField] private Color guideColor = new Color(1f, 1f, 1f, 0.25f);
    [SerializeField] private float guideWidth = 6f;

    [Header("시작 별 표시 (Sprite 비우면 네모)")]
    [SerializeField] private Sprite markerSprite;
    [SerializeField] private Color markerColor = new Color(1f, 0.95f, 0.6f, 1f);
    [SerializeField] private float markerSize = 60f;
    [SerializeField] private float pulseSpeed = 3f;

    private const string GuideLayer = "guide";

    private readonly Dictionary<string, List<GameObject>> layers = new Dictionary<string, List<GameObject>>();
    private readonly List<Image> markers = new List<Image>();

    // 기본 흐린 선 (H2 목표 성좌)
    public void ShowGuide(IEnumerable<string> edgeKeys)
    {
        ShowLayer(GuideLayer, edgeKeys, guideColor, guideWidth);
    }

    public void HideGuide()
    {
        HideLayer(GuideLayer);
    }

    // 이름 붙인 선 묶음 표시 (예: "fixed" 고정선, "axis" 대칭축 힌트)
    public void ShowLayer(string layer, IEnumerable<string> edgeKeys, Color color, float width)
    {
        try
        {
            HideLayer(layer);
            if (!CheckRefs() || edgeKeys == null) return;
            var created = new List<GameObject>();
            foreach (string key in edgeKeys)
            {
                GameObject go = CreateLine(key, color, width);
                if (go != null) created.Add(go);
            }
            layers[layer] = created;
        }
        catch (Exception e)
        {
            Debug.LogError($"[StarGuideView] ShowLayer({layer}) 오류: {e}");
        }
    }

    public void HideLayer(string layer)
    {
        if (!layers.TryGetValue(layer, out List<GameObject> list)) return;
        foreach (GameObject go in list) if (go != null) Destroy(go);
        layers.Remove(layer);
    }

    private GameObject CreateLine(string key, Color color, float width)
    {
        if (!StarGraph.TryParseEdge(key, out Vector2Int a, out Vector2Int b)) return null;
        if (!board.TryGetStar(a, out StarPoint sa) || !board.TryGetStar(b, out StarPoint sb)) return null;
        RectTransform line = Instantiate(linePrefab, guideContainer);
        line.gameObject.SetActive(true);
        line.sizeDelta = new Vector2(line.sizeDelta.x, width);
        Image img = line.GetComponent<Image>();
        if (img != null)
        {
            img.color = color;
            img.raycastTarget = false;
        }
        PlaceLine(line, LocalOf(sa.Rect), LocalOf(sb.Rect));
        return line.gameObject;
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

    public void HideStartMarkers()
    {
        foreach (Image img in markers) if (img != null) Destroy(img.gameObject);
        markers.Clear();
    }

    public void HideAll()
    {
        var names = new List<string>(layers.Keys);
        foreach (string layer in names) HideLayer(layer);
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