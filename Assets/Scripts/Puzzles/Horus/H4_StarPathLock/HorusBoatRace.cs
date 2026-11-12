using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// H4 세트와의 돌배 경주 (소코반) - 예전 H4(세트의 봉인 / 별판 걷기)를 바꿈
// 신화: 호루스와 세트가 '돌로 만든 배' 경주를 했다. 호루스는 나무배에 돌색을 칠해 띄웠고, 진짜 돌배를 만든 세트는 가라앉았다
// 규칙
// 1. 남동쪽 '나일 강' 경기장 입구는 세트의 붉은 봉인으로 막혀 있음 -> 매의 인장(H2-3 보상)으로 풂
// 2. 배에 몸을 대고 그 방향으로 계속 밀면 한 칸 미끄러짐 (당기기 없음)
// 3. 호루스의 나무배(흰 돌색): 물에 못 들어감 / 세트의 돌배(붉은): 물 칸에 밀어 넣으면 가라앉아 그 칸이 땅이 됨
// 4. 나무배 두 척을 별 자리 두 곳에 대면 승리 -> 별 조각 3 + 노트 5
// 5. R: 한 수 되돌리기 (사람도 그 자리로) / 경기장 옆 비석 E: 처음부터
// 판 데이터는 Rows (설계: 최소 9번 밀기, 세트의 돌배로 강에 다리를 놓지 않으면 못 품 - 컴퓨터로 확인함)
// [수정] 너무 어려웠던 판(15번)을 쉽게: 강(물 한 줄)이 경기장을 가르고, 돌배 1척 = 다리 1개
//        가라앉은 돌배 칸은 땅 그림으로 바뀌어서 사람도 배도 지나갈 수 있음 (처음 가라앉을 때 대사로 알려줌)
// 경기장은 본당 동쪽 '나일의 방' 안 (HorusRoomMap.NileRoom)
public class HorusBoatRace : PuzzleBase, IInteractable
{
    // 위가 북쪽. 글자 하나 = 1칸. 왼쪽 아래 칸 = (Origin.x, Origin.y)
    // '#' 바위, '.' 땅, '~' 물, 'o' 별 자리, 'B' 호루스 나무배, 'S' 세트 돌배, 'G' 입구(봉인), 'p' 기둥 자리(바위 안 놓음)
    private static readonly string[] Rows =
    {
        "########",
        "#..~.o.#",
        "#.B~...#",
        "#..~.B.#",
        "#.S~...#",
        "G..~.o.#",
        "#..~...#"
    };

    [Header("연결 (빌더가 채움)")]
    [SerializeField] private Vector2 origin = new Vector2(27f, 1f);
    [Tooltip("[수정] 칸 하나의 크기 (월드). 1이면 배/바위 사이 길이 좁아서 지나가다 걸림 -> 1.5")]
    [SerializeField] private float cell = 1.5f;
    [SerializeField] private Sprite rockSprite, waterSprite, sunkSprite, socketSprite, woodSprite, setSprite, sealSprite;

    [Header("진행")]
    [SerializeField] private string unlockedFlag = "F_HO_H4_UNLOCKED";
    [SerializeField] private string rewardItem = HorusItemIds.StarFrag3;
    [SerializeField] private float pushHold = 0.18f;
    [SerializeField] private float slideSeconds = 0.16f;

    private class Boat { public SpriteRenderer sr; public Vector2Int cell; public bool set; public bool sunk; public Vector3 scale; }
    private class Snapshot { public Vector2Int[] cells; public bool[] sunk; public bool[] water; public Vector3 player; }

    private readonly List<Boat> boats = new List<Boat>();
    private readonly Dictionary<Vector2Int, char> map = new Dictionary<Vector2Int, char>();
    private readonly Dictionary<Vector2Int, SpriteRenderer> waterTiles = new Dictionary<Vector2Int, SpriteRenderer>();
    private readonly Dictionary<Vector2Int, Collider2D> waterBlocks = new Dictionary<Vector2Int, Collider2D>();
    private readonly HashSet<Vector2Int> water = new HashSet<Vector2Int>();
    private readonly List<Vector2Int> sockets = new List<Vector2Int>();
    private readonly Stack<Snapshot> history = new Stack<Snapshot>();
    private Snapshot startState;
    private SpriteRenderer gate;
    private Transform root, player;
    private float holdTimer;
    private Vector2Int holdDir;
    private bool sliding, done, toldRules, toldBridge;

    private bool Unlocked => IsFlagOn(unlockedFlag);
    private bool Paused => DialogueBox.IsOpen || Cine.Running || HorusFocus.PanelOpen || ItemPopup.IsShowing;

    // ================= 준비 =================
    private void Start()
    {
        try
        {
            HorusFx.MatchPlayer();
            root = new GameObject("H4_BoatRace").transform;
            BuildMap();
            startState = Capture();
            done = IsFlagOn(ClearFlag);
            if (done) ShowCleared();
            if (Unlocked || done) OpenGate(false);
        }
        catch (Exception e)
        {
            Debug.LogError($"[돌배 경주] 준비 오류: {e}");
            enabled = false;
        }
    }

    private void BuildMap()
    {
        int h = Rows.Length;
        for (int r = 0; r < h; r++)
        {
            for (int c = 0; c < Rows[r].Length; c++) BuildCell(new Vector2Int(c, h - 1 - r), Rows[r][c]);
        }
    }

    private void BuildCell(Vector2Int cell, char ch)
    {
        Vector3 pos = World(cell);
        map[cell] = ch == 'B' || ch == 'S' || ch == 'G' ? '.' : ch;
        if (ch == '#') HorusWorld.Prop(root, "H4_Rock", rockSprite, pos, this.cell * 0.95f, new Vector2(this.cell, this.cell)); // 바위 사이로 빠져나가지 않게 충돌은 한 칸 꽉
        else if (ch == '~') AddWater(cell, pos);
        else if (ch == 'o')
        {
            sockets.Add(cell);
            HorusWorld.Floor(root, "H4_Socket", socketSprite, pos, this.cell * 0.9f, 1);
        }
        else if (ch == 'B' || ch == 'S') AddBoat(cell, ch == 'S');
        else if (ch == 'G') BuildGate(pos);
    }

    private void AddWater(Vector2Int cell, Vector3 pos)
    {
        water.Add(cell);
        SpriteRenderer sr = HorusWorld.Floor(root, "H4_Water", waterSprite, pos, this.cell, 0);
        waterTiles[cell] = sr;
        waterBlocks[cell] = HorusWorld.AddBox(sr.gameObject, new Vector2(this.cell, this.cell), sr.transform.localScale.x, false);
        ((BoxCollider2D)waterBlocks[cell]).offset = Vector2.zero;
    }

    private void AddBoat(Vector2Int cell, bool set)
    {
        SpriteRenderer sr = HorusWorld.Prop(root, set ? "H4_SetBoat" : "H4_HorusBoat", set ? setSprite : woodSprite, World(cell), this.cell * 0.85f, Vector2.zero);
        BoxCollider2D box = sr.gameObject.AddComponent<BoxCollider2D>();
        box.size = new Vector2(this.cell * 0.8f, this.cell * 0.8f) / sr.transform.localScale.x;
        boats.Add(new Boat { sr = sr, cell = cell, set = set, scale = sr.transform.localScale });
    }

    // 입구를 막은 세트의 봉인 (매의 인장으로 풂)
    private void BuildGate(Vector3 pos)
    {
        gate = HorusWorld.Prop(root, "H4_SetSealGate", sealSprite, pos, cell * 0.8f, new Vector2(cell, cell));
        RaGlowFx.Glow(gate.transform, Vector3.up * 0.5f / gate.transform.localScale.x, 1.6f / gate.transform.localScale.x, new Color(1f, 0.25f, 0.2f, 0.35f), HorusFx.Layer, 1);
    }

    private Vector3 World(Vector2Int c) => new Vector3(origin.x + (c.x + 0.5f) * cell, origin.y + (c.y + 0.5f) * cell, 0f);

    private Vector2Int CellOf(Vector3 p) => new Vector2Int(Mathf.FloorToInt((p.x - origin.x) / cell), Mathf.FloorToInt((p.y - origin.y) / cell));

    // ================= 매 프레임: 밀기 / 되돌리기 =================
    private void Update()
    {
        try
        {
            if (done || !Unlocked || sliding || Paused) return;
            if (player == null) player = HorusWorld.Player();
            if (player == null || !InArena(player.position)) return;
            ExplainOnce();
            if (Input.GetKeyDown(KeyCode.R))
            {
                Undo();
                return;
            }
            TryPush();
        }
        catch (Exception e)
        {
            Debug.LogError($"[돌배 경주] 갱신 오류: {e}");
            enabled = false;
        }
    }

    private bool InArena(Vector3 p)
    {
        Vector2Int c = CellOf(p);
        return c.x >= 0 && c.y >= 0 && c.x < Rows[0].Length && c.y < Rows.Length;
    }

    private void ExplainOnce()
    {
        if (toldRules) return;
        toldRules = true;
        HorusWorld.Say("호루스와 세트의 '돌배 경주'... 흰 나무배 두 척을 강 건너 금빛 별 자리 두 곳까지 밀어 보내면 돼.",
            "강물은 사람도 배도 못 건너. 붉은 배는 세트의 진짜 돌배라 물에 밀어 넣으면 가라앉아 다리가 되겠지.",
            "배는 몸을 대고 그 방향키를 꾹 누르면 한 칸씩 밀려. 당길 수는 없어. (R: 한 수 되돌리기 / 비석 E: 처음부터)");
    }

    // 배에 붙어서 그 방향으로 pushHold초 동안 밀면 한 칸
    private void TryPush()
    {
        Vector2Int dir = HorusWorld.InputDir();
        Boat boat = dir == Vector2Int.zero ? null : BoatAhead(player.position, dir);
        if (boat == null || dir != holdDir)
        {
            holdDir = dir;
            holdTimer = 0f;
            return;
        }
        holdTimer += Time.deltaTime;
        if (holdTimer < pushHold) return;
        holdTimer = 0f;
        Vector2Int to = boat.cell + dir;
        if (CanEnter(boat, to)) StartCoroutine(Push(boat, to));
    }

    // 플레이어 바로 앞(그 방향 1칸 이내, 옆으로 0.45칸 이내)에 있는 배
    private Boat BoatAhead(Vector3 p, Vector2Int dir)
    {
        foreach (Boat b in boats)
        {
            if (b.sunk) continue;
            Vector2 d = (Vector2)(World(b.cell) - p);
            float along = Vector2.Dot(d, dir), side = Mathf.Abs(Vector2.Dot(d, new Vector2(dir.y, dir.x)));
            if (along > 0.2f && along < cell * 0.5f + 0.75f && side < cell * 0.35f) return b;
        }
        return null;
    }

    private bool CanEnter(Boat boat, Vector2Int to)
    {
        if (!map.TryGetValue(to, out char ch) || ch == '#' || ch == 'p' || to.x == 0) return false; // 맨 왼쪽 줄 = 입구/벽 줄
        foreach (Boat b in boats) if (!b.sunk && b.cell == to) return false;
        if (water.Contains(to)) return boat.set; // 나무배는 물에 못 들어감
        return true;
    }

    private IEnumerator Push(Boat boat, Vector2Int to)
    {
        sliding = true;
        history.Push(Capture());
        Vector3 from = boat.sr.transform.position, target = World(to);
        GameAudio.Play("sfx_sand_collapse", 0.35f, 1.4f);
        float t = 0f;
        while (t < slideSeconds)
        {
            t += Time.deltaTime;
            boat.sr.transform.position = Vector3.Lerp(from, target, t / slideSeconds);
            yield return null;
        }
        boat.sr.transform.position = target;
        boat.cell = to;
        if (water.Contains(to) && boat.set) yield return Sink(boat, to);
        sliding = false;
        CheckWin();
    }

    // 세트의 돌배가 물에 가라앉음 -> 그 칸은 땅(밟을 수 있음)
    private IEnumerator Sink(Boat boat, Vector2Int cell)
    {
        GameAudio.Play("sfx_rumble", 0.4f, 1.3f);
        HorusFx.StarBurst(World(cell), new Vector2(0.4f, 0.3f), 10, new Color(0.6f, 0.8f, 1f));
        Vector3 s = boat.sr.transform.localScale;
        for (float t = 0f; t < 0.5f; t += Time.deltaTime)
        {
            boat.sr.transform.localScale = s * (1f - t);
            boat.sr.color = new Color(1f, 1f, 1f, 1f - t * 2f);
            yield return null;
        }
        SetSunk(boat, cell, true);
        if (toldBridge) yield break;
        toldBridge = true;
        HorusWorld.Say("세트의 돌배가 가라앉아 강 위에 다리가 됐다! 이제 그 위로 걸어서도, 배를 밀어서도 건널 수 있어.");
    }

    private void SetSunk(Boat boat, Vector2Int cell, bool sunk)
    {
        boat.sunk = sunk;
        boat.sr.gameObject.SetActive(!sunk);
        if (sunk) water.Remove(cell);
        else water.Add(cell);
        if (waterTiles.TryGetValue(cell, out SpriteRenderer tile)) tile.sprite = sunk ? sunkSprite : waterSprite;
        if (waterBlocks.TryGetValue(cell, out Collider2D col)) col.enabled = !sunk;
    }

    private void CheckWin()
    {
        foreach (Boat b in boats)
        {
            if (!b.set && !sockets.Contains(b.cell)) return;
        }
        Submit();
    }

    // ================= 되돌리기 / 처음부터 =================
    private Snapshot Capture()
    {
        var s = new Snapshot { cells = new Vector2Int[boats.Count], sunk = new bool[boats.Count] };
        for (int i = 0; i < boats.Count; i++)
        {
            s.cells[i] = boats[i].cell;
            s.sunk[i] = boats[i].sunk;
        }
        s.player = player != null ? player.position : World(new Vector2Int(1, 1));
        return s;
    }

    private void Undo()
    {
        if (history.Count == 0) return;
        Restore(history.Pop(), true);
        GameAudio.Play("sfx_dream_shimmer", 0.3f, 1.6f);
    }

    private void Restore(Snapshot s, bool movePlayer)
    {
        for (int i = 0; i < boats.Count; i++)
        {
            Boat b = boats[i];
            if (b.sunk != s.sunk[i]) SetSunk(b, b.cell, s.sunk[i]);
            b.cell = s.cells[i];
            b.sr.transform.position = World(b.cell);
            b.sr.transform.localScale = b.scale; // 가라앉으며 줄었던 크기를 원래대로
            b.sr.color = Color.white;
        }
        if (movePlayer) HorusWorld.Teleport(player, s.player);
    }

    // ================= 판정 =================
    public override void Submit()
    {
        try
        {
            if (done) return;
            if (Validate()) Complete();
        }
        catch (Exception e)
        {
            Debug.LogError($"[돌배 경주] Submit 오류: {e}");
        }
    }

    protected override bool Validate()
    {
        foreach (Boat b in boats) if (!b.set && !sockets.Contains(b.cell)) return false;
        return true;
    }

    private void Complete()
    {
        done = true;
        OnSuccessInternal();
        if (ItemInventory.Instance != null && !string.IsNullOrEmpty(rewardItem)) ItemInventory.Instance.Grant(rewardItem);
        Debug.Log("[노트 5] 아들의 눈은 어머니의 별 뒤에 뜨고, 매는 모든 별이 모인 뒤 하늘 한가운데 앉는다.");
        StartCoroutine(WinRoutine());
    }

    private IEnumerator WinRoutine()
    {
        GameAudio.Play("sfx_discover", 0.8f);
        foreach (Vector2Int s in sockets) HorusFx.StarBurst(World(s), new Vector2(0.4f, 0.4f), 14, new Color(1f, 0.85f, 0.45f));
        yield return ScreenTintUI.Flash(new Color(1f, 0.85f, 0.5f, 0.4f), 0.04f, 0.1f, 0.5f);
        ShowCleared();
        HorusWorld.Say("호루스의 배가 결승점에 닿았다! 세트의 돌배는 강바닥에 가라앉았고...",
            "별 자리 사이에서 별 조각이 떠올랐다. 별 조각 3을 얻었다.");
    }

    private void ShowCleared()
    {
        int k = 0;
        foreach (Boat b in boats)
        {
            if (b.set) continue;
            if (k < sockets.Count) b.cell = sockets[k++];
            b.sr.transform.position = World(b.cell);
            b.sr.color = new Color(1f, 0.95f, 0.8f);
        }
    }

    // ================= 비석 E: 봉인 풀기 / 처음부터 =================
    public string GetPrompt()
    {
        if (done) return "E: 경주의 비석 보기";
        if (Unlocked) return "E: 처음부터 다시 (경주 되돌리기)";
        return HasSeal() ? "E: 매의 인장으로 봉인 풀기" : "E: 경주의 비석 조사";
    }

    public void Interact(GameObject who)
    {
        try
        {
            if (DialogueBox.IsOpen || Cine.Running || sliding) return;
            if (done) HorusWorld.Say("\"호루스는 나무배에 돌색을 칠했고, 세트의 돌배는 가라앉았다.\"");
            else if (Unlocked) ResetRace();
            else if (HasSeal()) StartCoroutine(UnlockRoutine());
            else HorusWorld.Say("\"두 신은 돌로 만든 배로 나일을 건너기로 했다.\" 경기장 입구를 붉은 봉인이 막고 있어. 매의 인장이 필요해.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[돌배 경주] Interact 오류: {e}");
        }
    }

    private void ResetRace()
    {
        history.Clear();
        Restore(startState, false);
        HorusFx.StarBurst(World(new Vector2Int(3, 3)), new Vector2(3f, 3f), 16, new Color(0.6f, 0.8f, 1f));
        HorusWorld.Say("강물이 되돌아오고 배들이 처음 자리로 돌아갔다.");
    }

    private bool HasSeal() => ItemInventory.Instance != null && ItemInventory.Instance.Has(HorusItemIds.SealFalcon);

    private IEnumerator UnlockRoutine()
    {
        if (!ItemInventory.Instance.Consume(HorusItemIds.SealFalcon)) yield break;
        GameAudio.Play("sfx_rumble", 0.6f, 0.9f);
        yield return CineCamera.Rumble(0.6f, 0.02f, 0.08f);
        if (GameManager.Instance != null) GameManager.Instance.SetFlag(unlockedFlag);
        OpenGate(true);
        HorusWorld.Say("봉인이 부서지고 경기장 입구가 열렸다!");
    }

    private void OpenGate(bool fx)
    {
        if (gate == null) return;
        if (fx) HorusFx.StarBurst(gate.transform.position + Vector3.up * 0.5f, new Vector2(0.5f, 0.5f), 18, new Color(1f, 0.3f, 0.25f));
        gate.gameObject.SetActive(false);
    }

    // ================= 개발용 치트 (Inspector 우클릭) =================
    [ContextMenu("치트: 매 인장 지급")]
    private void CheatSeal()
    {
        if (ItemInventory.Instance != null) ItemInventory.Instance.Grant(HorusItemIds.SealFalcon);
    }
}
