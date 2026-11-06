using System.Collections.Generic;
using UnityEngine;

// 착시 퍼즐 규칙 전체 (MonoBehaviour 아님: 런타임 · 에디터 정답 확인 · 오프라인 설계 도구가 같은 코드를 씀)
// - 고정 블록 + 장치(돌아가는 섬 / 미끄러지는 다리 / 물높이) + 발판
// - 장치 상태가 바뀌면 블록 배치가 바뀌고 -> 그 배치로 착시 길(SobekIllusionGraph)을 다시 계산
// - 발판 칸에 "들어서면" 그 발판의 장치가 한 단계 움직임 (제자리에서 다시 누르려면 내렸다가 다시 밟기)
public class SobekIllusionModel
{
    public enum Kind { Rotate = 0, Slide = 1, Water = 2 }

    public class Mech
    {
        public Kind kind;
        public List<Vector3Int> cells = new List<Vector3Int>(); // 상태 0일 때 블록 (Water는 뗏목)
        public Vector3Int pivot;                                 // Rotate: 이 칸의 세로줄을 축으로 90°씩
        public List<Vector3Int> offsets = new List<Vector3Int>(); // Slide: 상태별 이동량 (0번은 보통 0,0,0)
        public int count = 4;                                    // 상태 수 (Rotate 2~4, Slide = offsets 수, Water 물높이 단계 수)
        public int startState;
    }

    public struct Plate
    {
        public Vector3Int cell;
        public int mech;
    }

    public readonly HashSet<Vector3Int> Static;
    public readonly List<Mech> Mechs;
    public readonly List<Plate> Plates;
    public readonly Vector3Int Start, Goal;
    public readonly float BaseWaterY;

    private readonly Dictionary<int, SobekIllusionGraph> graphCache = new Dictionary<int, SobekIllusionGraph>();
    private readonly Dictionary<int, HashSet<Vector3Int>> blockCache = new Dictionary<int, HashSet<Vector3Int>>();

    public SobekIllusionModel(IEnumerable<Vector3Int> staticBlocks, List<Mech> mechs, List<Plate> plates, Vector3Int start, Vector3Int goal, float baseWaterY)
    {
        Static = new HashSet<Vector3Int>(staticBlocks);
        Mechs = mechs ?? new List<Mech>();
        Plates = plates ?? new List<Plate>();
        Start = start;
        Goal = goal;
        BaseWaterY = baseWaterY;
    }

    // ================= 상태 =================
    public int[] StartStates()
    {
        var s = new int[Mechs.Count];
        for (int i = 0; i < s.Length; i++) s[i] = Mathf.Clamp(Mechs[i].startState, 0, Mathf.Max(1, Mechs[i].count) - 1);
        return s;
    }

    // 장치 상태들을 정수 하나로 (캐시 키)
    public static int Key(int[] st)
    {
        int k = 0;
        for (int i = 0; i < st.Length; i++) k = k * 8 + st[i];
        return k;
    }

    public int PlateMechAt(Vector3Int c)
    {
        foreach (Plate p in Plates) if (p.cell == c) return p.mech;
        return -1;
    }

    // ================= 장치별 위치 계산 =================
    // 회전: 위에서 볼 때 시계 방향 90° = 유니티 Y축 +90° (x -> -z, z -> x)
    public static Vector3Int RotateAround(Vector3Int c, Vector3Int pivot, int quarter)
    {
        int dx = c.x - pivot.x, dz = c.z - pivot.z;
        for (int i = 0; i < (quarter & 3); i++) { int t = dx; dx = dz; dz = -t; }
        return new Vector3Int(pivot.x + dx, c.y, pivot.z + dz);
    }

    public Vector3Int MechCell(int m, Vector3Int baseCell, int state)
    {
        Mech mech = Mechs[m];
        switch (mech.kind)
        {
            case Kind.Rotate: return RotateAround(baseCell, mech.pivot, state);
            case Kind.Slide: return baseCell + (mech.offsets.Count > 0 ? mech.offsets[Mathf.Clamp(state, 0, mech.offsets.Count - 1)] : Vector3Int.zero);
            default: return baseCell + new Vector3Int(0, state, 0); // 뗏목은 물과 함께 오르내림
        }
    }

    public float WaterY(int[] st)
    {
        for (int i = 0; i < Mechs.Count; i++)
            if (Mechs[i].kind == Kind.Water) return BaseWaterY + st[i];
        return BaseWaterY;
    }

    // 이 상태의 모든 블록 (물에 잠긴 것 포함)
    public HashSet<Vector3Int> BlocksAt(int[] st)
    {
        int key = Key(st);
        if (blockCache.TryGetValue(key, out var set)) return set;
        set = new HashSet<Vector3Int>(Static);
        for (int m = 0; m < Mechs.Count; m++)
            foreach (Vector3Int c in Mechs[m].cells) set.Add(MechCell(m, c, st[m]));
        blockCache[key] = set;
        return set;
    }

    // 이 상태에서 보이는 블록 = 윗면이 물 위에 있는 것 -> 착시 길 계산
    public SobekIllusionGraph GraphAt(int[] st)
    {
        int key = Key(st);
        if (graphCache.TryGetValue(key, out var g)) return g;
        float w = WaterY(st);
        var vis = new List<Vector3Int>();
        foreach (Vector3Int b in BlocksAt(st)) if (b.y + 0.5f > w) vis.Add(b);
        g = new SobekIllusionGraph(vis);
        graphCache[key] = g;
        return g;
    }

    // 장치 m을 한 단계 움직였을 때의 상태. 막히면 false (다른 블록과 겹침 / 플레이어 머리 위를 덮음 / 내 발판이 물에 잠김)
    public bool TryAdvance(int[] st, int m, Vector3Int player, out int[] next, out string reason)
    {
        next = (int[])st.Clone();
        reason = "";
        Mech mech = Mechs[m];
        int n = Mathf.Max(1, mech.count);
        next[m] = (st[m] + 1) % n;

        // 겹침 검사 (장치 블록끼리 + 고정 블록)
        var occupied = new HashSet<Vector3Int>(Static);
        for (int i = 0; i < Mechs.Count; i++)
        {
            if (i == m) continue;
            foreach (Vector3Int c in Mechs[i].cells) occupied.Add(MechCell(i, c, next[i]));
        }
        foreach (Vector3Int c in mech.cells)
        {
            Vector3Int p = MechCell(m, c, next[m]);
            if (!occupied.Add(p)) { reason = "다른 돌에 막혀 움직이지 않는다."; return false; }
            if (p == player + Vector3Int.up) { reason = "내가 서 있는 자리를 덮게 되어 멈췄다."; return false; }
        }
        if (!GraphAt(next).IsWalkable(player)) { reason = "발밑이 물에 잠긴다! 장치가 멈췄다."; return false; }
        return true;
    }

    // ================= 정답 탐색 =================
    public struct Step
    {
        public Vector3Int cell;
        public int yaw;
        public int[] mechs;
        public string action; // "rotate", "walk", "illusion", "plate"
    }

    // 상태 = (칸, 카메라 각도, 장치 상태들). 한 번의 행동 = 회전 ±90° / 한 칸 걷기(발판이면 장치 작동 포함)
    public List<Step> Solve(int startYaw, out int states, int maxStates = 400000, int frozenMech = -1)
    {
        int[] st0 = StartStates();
        var startNode = new Node(Start, startYaw, st0);
        var prev = new Dictionary<Node, (Node from, string act)> { [startNode] = (startNode, "") };
        var q = new Queue<Node>();
        q.Enqueue(startNode);
        while (q.Count > 0)
        {
            Node s = q.Dequeue();
            if (s.cell == Goal)
            {
                states = prev.Count;
                var path = new List<Step>();
                for (Node c = s; ; c = prev[c].from)
                {
                    path.Add(new Step { cell = c.cell, yaw = c.yaw, mechs = c.st, action = prev[c].act });
                    if (c.Equals(startNode)) break;
                }
                path.Reverse();
                return path;
            }
            if (prev.Count > maxStates) break;

            foreach (int ny in new[] { (s.yaw + 1) & 3, (s.yaw + 3) & 3 })
                Visit(new Node(s.cell, ny, s.st), s, "rotate", prev, q);

            SobekIllusionGraph g = GraphAt(s.st);
            foreach (SobekIllusionGraph.Edge e in g.Neighbors(s.cell, s.yaw))
            {
                int[] st = s.st;
                string act = e.IsIllusion ? "illusion" : "walk";
                int pm = PlateMechAt(e.To);
                if (pm >= 0 && pm != frozenMech && TryAdvance(st, pm, e.To, out int[] nx, out _)) { st = nx; act += "+plate"; }
                Visit(new Node(e.To, s.yaw, st), s, act, prev, q);
            }
        }
        states = prev.Count;
        return null;
    }

    private static void Visit(Node n, Node from, string act, Dictionary<Node, (Node, string)> prev, Queue<Node> q)
    {
        if (prev.ContainsKey(n)) return;
        prev[n] = (from, act);
        q.Enqueue(n);
    }

    private readonly struct Node : System.IEquatable<Node>
    {
        public readonly Vector3Int cell;
        public readonly int yaw;
        public readonly int[] st;
        private readonly int key;

        public Node(Vector3Int c, int y, int[] s)
        {
            cell = c; yaw = y; st = s; key = Key(s);
        }

        public bool Equals(Node o) => cell == o.cell && yaw == o.yaw && key == o.key;
        public override bool Equals(object obj) => obj is Node o && Equals(o);
        public override int GetHashCode() => (cell.GetHashCode() * 397 ^ yaw) * 31 + key;
    }
}
