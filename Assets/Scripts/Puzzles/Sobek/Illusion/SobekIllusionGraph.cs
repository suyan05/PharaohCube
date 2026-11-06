using System.Collections.Generic;
using UnityEngine;

// 착시 길 계산 (MonoBehaviour 아님 -> 에디터 정답 확인, 런타임 이동 둘 다 씀)
// 규칙 (모뉴먼트 밸리 방식):
// - 카메라는 정확한 아이소메트릭(위에서 35.264°, 옆 45°/135°/225°/315°) 직교 카메라
// - 이 시점에서는 "카메라가 보는 방향" 위의 점들이 화면에서 한 점으로 겹침
// - 그래서 화면에서 윗면끼리 변이 맞닿아 보이면, 실제로는 높이·거리가 달라도 이어진 길로 취급
// - 화면 한 칸에는 카메라에 가장 가까운 블록만 보임 (뒤에 숨은 블록은 못 감)
public class SobekIllusionGraph
{
    // 카메라 각도 4개(45, 135, 225, 315)에서 카메라가 보는 방향 (정수 벡터)
    public static readonly Vector3Int[] View =
    {
        new Vector3Int(1, -1, 1), new Vector3Int(1, -1, -1), new Vector3Int(-1, -1, -1), new Vector3Int(-1, -1, 1)
    };

    public static readonly Vector3Int[] Dirs =
    {
        new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1)
    };

    public struct Edge
    {
        public Vector3Int To;   // 도착 블록
        public Vector3Int Dir;  // 화면에서 걷는 방향 (가로 한 칸)
        public int K;           // 0이면 실제로 붙은 길, 아니면 착시로 이어진 길 (보는 방향으로 K칸 차이)
        public bool IsIllusion => K != 0;
    }

    private const int MaxK = 12;
    private static readonly Vector2[] Samples = { new Vector2(0f, 0f), new Vector2(0.3f, 0.3f), new Vector2(-0.3f, 0.3f), new Vector2(0.3f, -0.3f), new Vector2(-0.3f, -0.3f) };

    private readonly HashSet<Vector3Int> blocks;
    private readonly HashSet<Vector3Int> walkable = new HashSet<Vector3Int>();
    private readonly Dictionary<long, bool> occludedCache = new Dictionary<long, bool>();
    private readonly Dictionary<long, List<Edge>> edgeCache = new Dictionary<long, List<Edge>>();

    public SobekIllusionGraph(IEnumerable<Vector3Int> blockCells)
    {
        blocks = new HashSet<Vector3Int>(blockCells);
        foreach (var b in blocks)
            if (!blocks.Contains(b + Vector3Int.up)) walkable.Add(b); // 위가 비어 있어야 설 수 있음
    }

    public IReadOnlyCollection<Vector3Int> Blocks => blocks;
    public IReadOnlyCollection<Vector3Int> Walkable => walkable;
    public bool IsBlock(Vector3Int c) => blocks.Contains(c);
    public bool IsWalkable(Vector3Int c) => walkable.Contains(c);

    private static long Key(Vector3Int c, int yaw) =>
        ((long)(c.x + 512) << 32) | ((long)(c.y + 512) << 21) | ((long)(c.z + 512) << 10) | (long)(yaw & 3);

    // 윗면 5점에서 카메라 쪽으로 광선을 쏴 다른 블록에 막히면 가려진 것 (파이썬 검증 도구와 같은 계산)
    public bool Occluded(Vector3Int p, int yaw)
    {
        long key = Key(p, yaw);
        if (occludedCache.TryGetValue(key, out bool hit)) return hit;
        hit = ComputeOccluded(p, yaw);
        occludedCache[key] = hit;
        return hit;
    }

    private bool ComputeOccluded(Vector3Int p, int yaw)
    {
        Vector3Int v = View[yaw];
        double n = System.Math.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
        double dx = -v.x / n, dy = -v.y / n, dz = -v.z / n;
        foreach (Vector2 o in Samples)
        {
            double x = p.x + o.x, y = p.y + 0.5, z = p.z + o.y;
            for (double t = 0.05; t < 14.0; t += 0.05)
            {
                double px = x + dx * t, py = y + dy * t, pz = z + dz * t;
                var q = new Vector3Int((int)System.Math.Round(px), (int)System.Math.Round(py), (int)System.Math.Round(pz));
                if (q == p || !blocks.Contains(q)) continue;
                if (System.Math.Abs(px - q.x) < 0.5 && System.Math.Abs(py - q.y) < 0.5 && System.Math.Abs(pz - q.z) < 0.5) return true;
            }
        }
        return false;
    }

    // 이 각도에서 p에서 한 걸음에 갈 수 있는 곳
    public List<Edge> Neighbors(Vector3Int p, int yaw)
    {
        long key = Key(p, yaw);
        if (edgeCache.TryGetValue(key, out var list)) return list;
        list = new List<Edge>(4);
        Vector3Int v = View[yaw];
        bool visible = !Occluded(p, yaw);
        foreach (Vector3Int d in Dirs)
        {
            Vector3Int t = p + d;
            if (!visible)
            {
                // 내가 가려져 있으면 착시는 없음: 실제로 붙은 길만
                if (walkable.Contains(t)) list.Add(new Edge { To = t, Dir = d, K = 0 });
                continue;
            }
            // 화면에서 옆 칸(t)과 겹쳐 보이는 블록 중 카메라에 가장 가까운 것 하나만 보임
            for (int k = -MaxK; k <= MaxK; k++)
            {
                Vector3Int q = t + v * k;
                if (!blocks.Contains(q)) continue;
                if (walkable.Contains(q) && !Occluded(q, yaw)) list.Add(new Edge { To = q, Dir = d, K = k });
                break;
            }
        }
        edgeCache[key] = list;
        return list;
    }

    // 각도를 고정했을 때 from -> to 길 (BFS). 없으면 null
    public List<Edge> FindPath(Vector3Int from, Vector3Int to, int yaw)
    {
        if (from == to) return new List<Edge>();
        var prev = new Dictionary<Vector3Int, Edge>();
        var prevNode = new Dictionary<Vector3Int, Vector3Int>();
        var q = new Queue<Vector3Int>();
        q.Enqueue(from);
        prevNode[from] = from;
        while (q.Count > 0)
        {
            Vector3Int c = q.Dequeue();
            foreach (Edge e in Neighbors(c, yaw))
            {
                if (prevNode.ContainsKey(e.To)) continue;
                prevNode[e.To] = c;
                prev[e.To] = e;
                if (e.To == to)
                {
                    var path = new List<Edge>();
                    for (Vector3Int n = to; n != from; n = prevNode[n]) path.Add(prev[n]);
                    path.Reverse();
                    return path;
                }
                q.Enqueue(e.To);
            }
        }
        return null;
    }

    // 이 각도에서 from에서 갈 수 있는 모든 칸
    public HashSet<Vector3Int> Reachable(Vector3Int from, int yaw)
    {
        var seen = new HashSet<Vector3Int> { from };
        var q = new Queue<Vector3Int>();
        q.Enqueue(from);
        while (q.Count > 0)
        {
            Vector3Int c = q.Dequeue();
            foreach (Edge e in Neighbors(c, yaw))
                if (seen.Add(e.To)) q.Enqueue(e.To);
        }
        return seen;
    }

    // 정답 탐색: 상태 = (서 있는 칸, 카메라 각도). 회전(±90°) 1번 = 걸음 1번으로 계산
    // 반환: 상태 목록 (null이면 정답 없음)
    public List<(Vector3Int cell, int yaw)> Solve(Vector3Int start, Vector3Int goal, int startYaw, out int states)
    {
        var s0 = (start, startYaw);
        var prev = new Dictionary<(Vector3Int, int), (Vector3Int, int)> { [s0] = s0 };
        var q = new Queue<(Vector3Int cell, int yaw)>();
        q.Enqueue(s0);
        while (q.Count > 0)
        {
            var s = q.Dequeue();
            if (s.cell == goal)
            {
                states = prev.Count;
                var path = new List<(Vector3Int, int)>();
                for (var c = s; ; c = prev[c]) { path.Add(c); if (c.Equals(s0)) break; }
                path.Reverse();
                return path;
            }
            foreach (int ny in new[] { (s.yaw + 1) & 3, (s.yaw + 3) & 3 })
                if (!prev.ContainsKey((s.cell, ny))) { prev[(s.cell, ny)] = s; q.Enqueue((s.cell, ny)); }
            foreach (Edge e in Neighbors(s.cell, s.yaw))
                if (!prev.ContainsKey((e.To, s.yaw))) { prev[(e.To, s.yaw)] = s; q.Enqueue((e.To, s.yaw)); }
        }
        states = prev.Count;
        return null;
    }

    // 각도 index <-> 도(°)
    public static float YawDegrees(int yaw) => 45f + 90f * (yaw & 3);
    public static int YawIndex(float degrees) => (((Mathf.RoundToInt((degrees - 45f) / 90f)) % 4) + 4) % 4;
}
