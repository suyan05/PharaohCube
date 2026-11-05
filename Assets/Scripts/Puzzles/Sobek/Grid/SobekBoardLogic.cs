using System;
using System.Collections.Generic;
using System.Text;

// 소베크 칸 퍼즐의 규칙만 담은 순수 C# 클래스 (UnityEngine 없음 -> 화면 없이 테스트·정답 탐색 가능)
// 좌표: X = 왼쪽부터 칸, Y = 위쪽부터 줄 (기획서 그림과 같은 방향)
// 칸 문자: '#' 벽, '.' 땅(가장 높음), '0'~'2' 높이가 있는 땅, '~' 물(건기는 구덩이), 's' 디딤돌(높이 0),
//          'a' 계단 0-1, 'b' 계단 1-2, 'F' 뗏목(물 높이를 따라 뜸)
// 발판 4종: 회전(섬 돌리기) / 승강(같은 글자 다리 올리기) / 수위(물 높이 바꾸기) / 거울(방 좌우 반전)

public enum SobekPlateKind { Rotate, Lift, Water, Mirror }

[Serializable]
public struct SobekCell : IEquatable<SobekCell>
{
    public int X;
    public int Y;

    public SobekCell(int x, int y) { X = x; Y = y; }

    public bool Equals(SobekCell other) => X == other.X && Y == other.Y;
    public override bool Equals(object obj) => obj is SobekCell c && Equals(c);
    public override int GetHashCode() => X * 397 ^ Y;
    public override string ToString() => $"({X},{Y})";
    public static bool operator ==(SobekCell a, SobekCell b) => a.Equals(b);
    public static bool operator !=(SobekCell a, SobekCell b) => !a.Equals(b);
}

// 퍼즐 한 판의 설계 (SobekBoardData가 이걸 만들어 넘김)
public sealed class SobekBoardSpec
{
    public string[] Rows;
    public SobekCell Start;
    public SobekCell Goal;
    public List<SobekCell> Eggs = new List<SobekCell>();
    public List<Island> Islands = new List<Island>();
    public List<Plate> Plates = new List<Plate>();
    public List<Bridge> Bridges = new List<Bridge>();
    public int LevelCount = 1;   // 수위 단계 수 (1이면 수위 없음)
    public int StartLevel;
    public bool UseTerraces;     // 높이가 다른 땅은 계단으로만 오감 (S3)
    public bool AllowExit = true; // 판 밖으로 걸어 나가면 퍼즐에서 나감

    public sealed class Island
    {
        public SobekCell Center;
        public int Arms; // 비트: 1 북, 2 동, 4 남, 8 서
    }

    public sealed class Plate
    {
        public SobekCell Cell;
        public SobekPlateKind Kind;
        public string BridgeId = "";
        public List<Spin> Spins = new List<Spin>(); // 회전 발판: 어느 섬을 어느 방향으로
    }

    public struct Spin
    {
        public int Island;
        public int Dir; // +1 시계, -1 반시계
        public Spin(int island, int dir) { Island = island; Dir = dir; }
    }

    public sealed class Bridge
    {
        public SobekCell Cell;
        public string Id = "";
    }
}

public sealed class SobekBoardLogic
{
    public enum Outcome { Blocked, Moved, Exit, EggSunk, Goal }

    // 한 걸음의 결과 (화면 연출용 정보 포함)
    public struct StepResult
    {
        public Outcome Outcome;
        public SobekCell From;        // 이동 전 실제 칸
        public SobekCell To;          // 이동 후 실제 칸
        public int PushedEgg;         // 민 알 번호 (-1 없음)
        public SobekCell EggFrom;
        public SobekCell EggTo;
        public int SunkEgg;           // 가라앉은 알 번호 (-1 없음)
        public bool HasPlate;
        public SobekPlateKind PlateKind; // 밟은 발판 (회전/수위/거울만 기록, 승강은 상태로 판단)
    }

    // 방향 (화면 기준): 0 북, 1 동, 2 남, 3 서
    private static readonly int[] DX = { 0, 1, 0, -1 };
    private static readonly int[] DY = { -1, 0, 1, 0 };

    private readonly SobekBoardSpec spec;
    private readonly char[,] terrain;
    private readonly Dictionary<SobekCell, int> plateAt = new Dictionary<SobekCell, int>();
    private readonly Dictionary<SobekCell, string> bridgeAt = new Dictionary<SobekCell, string>();

    // 상태
    private SobekCell player;            // 실제(거울 아닌) 좌표
    private readonly List<SobekCell> eggs = new List<SobekCell>();
    private int[] islandRot;
    private int level;
    private bool mirrored;

    public int Width { get; }
    public int Height { get; }
    public SobekBoardSpec Spec => spec;
    public SobekCell PlayerReal => player;
    public SobekCell PlayerView => ToView(player);
    public IReadOnlyList<SobekCell> Eggs => eggs;
    public int Level => level;
    public bool Mirrored => mirrored;
    public int IslandRotation(int i) => islandRot[i];
    public int TopHeight => Math.Max(0, spec.LevelCount - 1);

    public SobekBoardLogic(SobekBoardSpec spec)
    {
        this.spec = spec ?? throw new ArgumentNullException(nameof(spec));
        Height = spec.Rows.Length;
        Width = 0;
        foreach (string r in spec.Rows) Width = Math.Max(Width, r.Length);
        terrain = new char[Width, Height];
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                terrain[x, y] = x < spec.Rows[y].Length ? spec.Rows[y][x] : '#';
        for (int i = 0; i < spec.Plates.Count; i++) plateAt[spec.Plates[i].Cell] = i;
        foreach (var b in spec.Bridges) bridgeAt[b.Cell] = b.Id;
        Reset();
    }

    // ================= 상태 =================
    public void Reset()
    {
        player = spec.Start;
        eggs.Clear();
        eggs.AddRange(spec.Eggs);
        islandRot = new int[spec.Islands.Count];
        level = Clamp(spec.StartLevel, 0, Math.Max(0, spec.LevelCount - 1));
        mirrored = false;
    }

    public SobekCell ToView(SobekCell real) => mirrored ? new SobekCell(Width - 1 - real.X, real.Y) : real;

    public bool InBounds(SobekCell c) => c.X >= 0 && c.Y >= 0 && c.X < Width && c.Y < Height;
    public char TerrainAt(SobekCell c) => InBounds(c) ? terrain[c.X, c.Y] : '#';
    public bool IsGoal(SobekCell c) => c == spec.Goal;
    public bool TryGetPlate(SobekCell c, out SobekBoardSpec.Plate plate)
    {
        plate = null;
        if (!plateAt.TryGetValue(c, out int i)) return false;
        plate = spec.Plates[i];
        return true;
    }
    public bool TryGetBridge(SobekCell c, out string id) => bridgeAt.TryGetValue(c, out id);
    public int EggIndexAt(SobekCell c) => eggs.IndexOf(c);

    // 이 칸이 섬의 일부면 섬 번호 (아니면 -1)
    public int IslandAt(SobekCell c) => IslandAt(c, islandRot);

    private int IslandAt(SobekCell c, int[] rots)
    {
        for (int i = 0; i < spec.Islands.Count; i++)
        {
            var isl = spec.Islands[i];
            if (isl.Center == c) return i;
            int arms = RotateArms(isl.Arms, rots[i]);
            for (int d = 0; d < 4; d++)
            {
                if ((arms & (1 << d)) == 0) continue;
                if (isl.Center.X + DX[d] == c.X && isl.Center.Y + DY[d] == c.Y) return i;
            }
        }
        return -1;
    }

    public static int RotateArms(int arms, int rot)
    {
        rot = ((rot % 4) + 4) % 4;
        for (int k = 0; k < rot; k++)
        {
            int n = 0;
            for (int d = 0; d < 4; d++) if ((arms & (1 << d)) != 0) n |= 1 << ((d + 1) % 4); // 북->동->남->서
            arms = n;
        }
        return arms;
    }

    public bool IsBridgeRaised(string id) => Raised(id, player, eggs);

    private bool Raised(string id, SobekCell who, List<SobekCell> eggList)
    {
        foreach (var p in spec.Plates)
        {
            if (p.Kind != SobekPlateKind.Lift || p.BridgeId != id) continue;
            if (p.Cell == who || eggList.Contains(p.Cell)) return true;
        }
        return false;
    }

    // 땅 높이 (-1 = 땅 아님)
    public int HeightOf(char t)
    {
        switch (t)
        {
            case '.': return TopHeight;
            case '0': return 0;
            case '1': return 1;
            case '2': return 2;
            case 's': return 0;
            case 'a': return 0;
            case 'b': return 1;
            default: return -1;
        }
    }

    // 물에 잠긴 땅인지 (화면 표시용)
    public bool IsSubmerged(SobekCell c)
    {
        int h = HeightOf(TerrainAt(c));
        return spec.LevelCount > 1 && h >= 0 && h < level;
    }

    private bool Walk(SobekCell q, SobekCell who, List<SobekCell> eggList, int[] rots, int lv)
    {
        if (!InBounds(q)) return false;
        if (IslandAt(q, rots) >= 0) return true;
        char t = terrain[q.X, q.Y];
        if (t == '#') return false;
        if (bridgeAt.TryGetValue(q, out string id)) return Raised(id, who, eggList);
        if (t == '~') return false;
        if (t == 'F') return true;
        int h = HeightOf(t);
        if (h < 0) return false;
        return spec.LevelCount <= 1 || h >= lv;
    }

    public bool IsWalkableNow(SobekCell q) => Walk(q, player, eggs, islandRot, level);

    // 계단 규칙 (S3): 두 칸이 같은 높이를 하나라도 나눠 가져야 오갈 수 있음
    private int HeightMask(char t, int lv)
    {
        switch (t)
        {
            case 'a': return 0b011;
            case 'b': return 0b110;
            case 'F': return 1 << lv;
            default:
                int h = HeightOf(t);
                return h < 0 ? 0 : 1 << h;
        }
    }

    // ================= 한 걸음 =================
    // dir: 0 북, 1 동, 2 남, 3 서 (화면 기준). 거울 상태면 실제 좌우가 반대
    public StepResult TryStep(int dir)
    {
        var res = new StepResult { Outcome = Outcome.Blocked, From = player, To = player, PushedEgg = -1, SunkEgg = -1 };
        int rdx = mirrored ? -DX[dir] : DX[dir];
        int dy = DY[dir];
        var nr = new SobekCell(player.X + rdx, player.Y + dy);

        // 1) 판 밖
        if (!InBounds(nr))
        {
            if (spec.AllowExit) res.Outcome = Outcome.Exit;
            return res;
        }

        // 2) 갈 수 있는 칸인지
        if (!Walk(nr, player, eggs, islandRot, level)) return res;
        if (spec.UseTerraces && (HeightMask(TerrainAt(player), level) & HeightMask(TerrainAt(nr), level)) == 0) return res;

        // 3) 알 밀기
        var newEggs = new List<SobekCell>(eggs);
        int egg = newEggs.IndexOf(nr);
        if (egg >= 0)
        {
            var er = new SobekCell(nr.X + rdx, nr.Y + dy);
            if (!InBounds(er) || TerrainAt(er) == '#' || newEggs.Contains(er)) return res; // 막힘
            res.PushedEgg = egg; res.EggFrom = nr; res.EggTo = er;
            if (!Walk(er, player, eggs, islandRot, level))
            {
                res.Outcome = Outcome.EggSunk; res.SunkEgg = egg; // 물·구덩이로 밀어 넣음
                return res;
            }
            newEggs[egg] = er;
        }

        // 4) 다리 위 알: 플레이어가 움직인 뒤에도 다리가 떠 있어야 함
        for (int i = 0; i < newEggs.Count; i++)
        {
            if (bridgeAt.TryGetValue(newEggs[i], out string bid) && !Raised(bid, nr, newEggs))
            {
                res.Outcome = Outcome.EggSunk; res.SunkEgg = i;
                return res;
            }
        }
        // 5) 플레이어가 올라설 다리도 떠 있어야 함 (승강 발판에서 내려서 바로 건너기 금지)
        if (bridgeAt.TryGetValue(nr, out string pb) && !Raised(pb, nr, newEggs)) return res;

        // 6) 발판 효과
        var rots = (int[])islandRot.Clone();
        int lv = level;
        bool mir = mirrored;
        if (plateAt.TryGetValue(nr, out int pi))
        {
            var plate = spec.Plates[pi];
            switch (plate.Kind)
            {
                case SobekPlateKind.Rotate:
                    foreach (var s in plate.Spins) rots[s.Island] = (((rots[s.Island] + s.Dir) % 4) + 4) % 4;
                    res.HasPlate = true; res.PlateKind = plate.Kind;
                    break;
                case SobekPlateKind.Water:
                    lv = (level + 1) % Math.Max(1, spec.LevelCount);
                    res.HasPlate = true; res.PlateKind = plate.Kind;
                    break;
                case SobekPlateKind.Mirror:
                    mir = !mirrored;
                    res.HasPlate = true; res.PlateKind = plate.Kind;
                    break;
            }
        }

        // 7) 거울을 바꿨으면 플레이어는 화면 자리 그대로 -> 실제 칸이 좌우로 바뀜
        var np = mir != mirrored ? new SobekCell(Width - 1 - nr.X, nr.Y) : nr;

        // 8) 바뀐 세계에서 플레이어가 설 수 있어야 함 (못 서면 이 걸음은 막힘)
        if (!Walk(np, np, newEggs, rots, lv)) return res;

        // 9) 바뀐 세계에서 알이 모두 설 수 있어야 함 (아니면 가라앉음)
        for (int i = 0; i < newEggs.Count; i++)
        {
            if (!Walk(newEggs[i], np, newEggs, rots, lv))
            {
                res.Outcome = Outcome.EggSunk; res.SunkEgg = i;
                return res;
            }
        }

        // 10) 확정
        player = np;
        eggs.Clear(); eggs.AddRange(newEggs);
        islandRot = rots;
        level = lv;
        mirrored = mir;
        res.To = np;
        res.Outcome = IsGoal(np) ? Outcome.Goal : Outcome.Moved;
        return res;
    }

    // ================= 정답 탐색 (BFS) =================
    // 지금 상태에서 목표까지 가장 짧은 방향 목록 (0 북, 1 동, 2 남, 3 서). 없으면 null
    public List<int> Solve(int maxStates = 500000)
    {
        var start = Snapshot();
        var prev = new Dictionary<string, KeyValuePair<string, int>>();
        var states = new Dictionary<string, Snap>();
        var q = new Queue<string>();
        string k0 = start.Key();
        prev[k0] = new KeyValuePair<string, int>(null, -1);
        states[k0] = start;
        q.Enqueue(k0);
        string found = null;

        while (q.Count > 0 && prev.Count < maxStates)
        {
            string k = q.Dequeue();
            for (int d = 0; d < 4; d++)
            {
                Restore(states[k]);
                var r = TryStep(d);
                if (r.Outcome != Outcome.Moved && r.Outcome != Outcome.Goal) continue;
                var s = Snapshot();
                string nk = s.Key();
                if (prev.ContainsKey(nk)) continue;
                prev[nk] = new KeyValuePair<string, int>(k, d);
                states[nk] = s;
                if (r.Outcome == Outcome.Goal) { found = nk; q.Clear(); break; }
                q.Enqueue(nk);
            }
        }

        Restore(start);
        if (found == null) return null;
        var path = new List<int>();
        for (string c = found; prev[c].Key != null; c = prev[c].Key) path.Add(prev[c].Value);
        path.Reverse();
        return path;
    }

    private struct Snap
    {
        public SobekCell Player;
        public SobekCell[] Eggs;
        public int[] Rots;
        public int Level;
        public bool Mirror;

        public string Key()
        {
            var sb = new StringBuilder();
            sb.Append(Player.X).Append(',').Append(Player.Y).Append('|');
            var sorted = (SobekCell[])Eggs.Clone();
            Array.Sort(sorted, (a, b) => a.X != b.X ? a.X - b.X : a.Y - b.Y);
            foreach (var e in sorted) sb.Append(e.X).Append(',').Append(e.Y).Append(';');
            sb.Append('|');
            foreach (int r in Rots) sb.Append(r);
            sb.Append('|').Append(Level).Append(Mirror ? 'M' : '-');
            return sb.ToString();
        }
    }

    private Snap Snapshot() => new Snap
    {
        Player = player, Eggs = eggs.ToArray(), Rots = (int[])islandRot.Clone(), Level = level, Mirror = mirrored
    };

    private void Restore(Snap s)
    {
        player = s.Player;
        eggs.Clear(); eggs.AddRange(s.Eggs);
        islandRot = (int[])s.Rots.Clone();
        level = s.Level;
        mirrored = s.Mirror;
    }

    private static int Clamp(int v, int lo, int hi) => v < lo ? lo : (v > hi ? hi : v);
}
