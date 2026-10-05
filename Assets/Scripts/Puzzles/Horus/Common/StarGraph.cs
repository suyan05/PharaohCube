using System.Collections.Generic;
using UnityEngine;

// 별 잇기 퍼즐 공통 계산. 좌표는 남서쪽 별 = (1,1)
public static class StarGraph
{
    // 두 별이 8방향으로 한 칸 붙어 있는지
    public static bool IsAdjacent(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return Mathf.Max(dx, dy) == 1;
    }

    // 선 이름을 "작은 좌표-큰 좌표"로 통일 (어느 쪽에서 그어도 같은 선이면 같은 키)
    public static string EdgeKey(Vector2Int a, Vector2Int b)
    {
        bool aFirst = a.x < b.x || (a.x == b.x && a.y <= b.y);
        Vector2Int p = aFirst ? a : b;
        Vector2Int q = aFirst ? b : a;
        return $"{p.x},{p.y}-{q.x},{q.y}";
    }

    // "1,5-1,4" -> 좌표 두 개
    public static bool TryParseEdge(string text, out Vector2Int a, out Vector2Int b)
    {
        a = Vector2Int.zero;
        b = Vector2Int.zero;
        if (string.IsNullOrWhiteSpace(text)) return false;
        string[] ends = text.Trim().Split('-');
        if (ends.Length != 2) return false;
        return TryParseCoord(ends[0], out a) && TryParseCoord(ends[1], out b);
    }

    // "1,5" -> 좌표
    public static bool TryParseCoord(string text, out Vector2Int coord)
    {
        coord = Vector2Int.zero;
        string[] xy = text.Trim().Split(',');
        if (xy.Length != 2) return false;
        if (!int.TryParse(xy[0], out int x) || !int.TryParse(xy[1], out int y)) return false;
        coord = new Vector2Int(x, y);
        return true;
    }

    // 정답 선 목록 -> 정리된 키 집합. 잘못된 줄은 경고만 찍고 건너뜀
    public static HashSet<string> BuildEdgeSet(IEnumerable<string> edges)
    {
        var set = new HashSet<string>();
        if (edges == null) return set;
        foreach (string e in edges)
        {
            if (!TryParseEdge(e, out Vector2Int a, out Vector2Int b) || !IsAdjacent(a, b))
            {
                Debug.LogWarning($"[StarGraph] 잘못된 선 데이터: {e}");
                continue;
            }
            set.Add(EdgeKey(a, b));
        }
        return set;
    }

    // 선이 홀수 개 모이는 별 = 한붓그리기를 시작할 수 있는 별 (힌트용)
    public static List<Vector2Int> OddDegreeStars(HashSet<string> edgeKeys)
    {
        var degree = new Dictionary<Vector2Int, int>();
        if (edgeKeys == null) return new List<Vector2Int>();
        foreach (string key in edgeKeys)
        {
            if (!TryParseEdge(key, out Vector2Int a, out Vector2Int b)) continue;
            degree[a] = degree.TryGetValue(a, out int da) ? da + 1 : 1;
            degree[b] = degree.TryGetValue(b, out int db) ? db + 1 : 1;
        }
        var result = new List<Vector2Int>();
        foreach (var pair in degree)
        {
            if (pair.Value % 2 == 1) result.Add(pair.Key);
        }
        return result;
    }

    // 최소 획 수 (선이 전부 이어져 있다는 가정)
    public static int MinStrokes(HashSet<string> edgeKeys)
    {
        if (edgeKeys == null || edgeKeys.Count == 0) return 0;
        int odd = OddDegreeStars(edgeKeys).Count;
        return Mathf.Max(1, odd / 2);
    }
}