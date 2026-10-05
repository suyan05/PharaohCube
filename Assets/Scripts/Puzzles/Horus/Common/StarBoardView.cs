using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 별 격자를 코드로 자동 생성 (라의 방 CircuitBoardView처럼)
public class StarBoardView : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private StarLineDrawer drawer;
    [SerializeField] private RectTransform starContainer;

    [Header("격자")]
    [SerializeField] private int cols = 5;
    [SerializeField] private int rows = 5;
    [SerializeField] private float spacing = 90f;
    [SerializeField] private float starSize = 34f;

    [Header("먹구름 / 꺼진 별 좌표 (남서쪽 = 1,1)")]
    [SerializeField]
    private List<Vector2Int> cloudCoords = new List<Vector2Int>
    { new Vector2Int(5, 4), new Vector2Int(1, 2), new Vector2Int(4, 1) };
    [SerializeField]
    private List<Vector2Int> darkCoords = new List<Vector2Int>
    { new Vector2Int(3, 5), new Vector2Int(3, 3), new Vector2Int(5, 1) };

    [Header("색")]
    [SerializeField] private Color normalColor = new Color(0.91f, 0.70f, 0.23f);
    [SerializeField] private Color darkColor = new Color(0.35f, 0.39f, 0.47f);
    [SerializeField] private Color cloudColor = new Color(0.05f, 0.08f, 0.14f, 1f);

    public event Action OnBuilt;
    public bool IsBuilt { get; private set; }

    private readonly Dictionary<Vector2Int, StarPoint> points = new Dictionary<Vector2Int, StarPoint>();

    private void Start()
    {
        Build();
    }

    public void Build()
    {
        try
        {
            if (drawer == null || starContainer == null)
            {
                Debug.LogError("[StarBoardView] Drawer / Star Container 연결 필요");
                return;
            }
            ClearStars();
            for (int r = 1; r <= rows; r++)
            {
                for (int c = 1; c <= cols; c++) CreateStar(new Vector2Int(c, r));
            }
            drawer.RegisterStars(points.Values);
            IsBuilt = true;
            OnBuilt?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"[StarBoardView] Build 오류: {e}");
        }
    }

    public bool TryGetStar(Vector2Int coord, out StarPoint star)
    {
        return points.TryGetValue(coord, out star);
    }

    private void CreateStar(Vector2Int coord)
    {
        var go = new GameObject($"Star_{coord.x}_{coord.y}",
            typeof(RectTransform), typeof(Image), typeof(StarPoint));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(starContainer, false);
        rt.sizeDelta = new Vector2(starSize, starSize);
        rt.anchoredPosition = GridToLocal(coord);
        var star = go.GetComponent<StarPoint>();
        star.Init(coord, drawer);
        ApplyState(star, InitialState(coord));
        points[coord] = star;
    }

    private Vector2 GridToLocal(Vector2Int coord)
    {
        float x = (coord.x - 1 - (cols - 1) / 2f) * spacing;
        float y = (coord.y - 1 - (rows - 1) / 2f) * spacing;
        return new Vector2(x, y);
    }

    private StarState InitialState(Vector2Int coord)
    {
        if (cloudCoords.Contains(coord)) return StarState.Cloud;
        if (darkCoords.Contains(coord)) return StarState.Dark;
        return StarState.Normal;
    }

    private void ApplyState(StarPoint star, StarState state)
    {
        Color color = state == StarState.Cloud ? cloudColor
                    : state == StarState.Dark ? darkColor
                    : normalColor;
        star.SetState(state, color);
    }

    // 별 조각을 끼웠을 때 꺼진 별을 켬
    public void SetStarLit(Vector2Int coord)
    {
        if (!points.TryGetValue(coord, out StarPoint star))
        {
            Debug.LogWarning($"[StarBoardView] 없는 좌표: {coord}");
            return;
        }
        if (star.State == StarState.Dark) ApplyState(star, StarState.Normal);
    }

    private void ClearStars()
    {
        IsBuilt = false;
        for (int i = starContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(starContainer.GetChild(i).gameObject);
        }
        points.Clear();
    }
}