using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// H1 매의 거울 (빛 반사 퍼즐) - 예전 H1(거울 날개 그리기 화면)을 바꿈
// 신화: 호루스의 오른 눈은 해, 왼 눈은 달. 해의 빛을 매의 눈에 되돌려 주면 눈이 뜬다
// 규칙
// 1. 서쪽 관측대(매의 눈 받침)에 먼지가 쌓여 있음 -> 매의 깃털(H2-1 보상)로 털어내면 서쪽 벽 '날개 달린 해'가 빛을 쏨
// 2. 서쪽 구역에 거울 석상 5개. E를 누를 때마다 '/' -> '\' -> 눕힘(빛 통과) 순서로 바뀜
// 3. 빛이 관측대 매의 눈에 닿으면 성공 (별 조각 1 + 노트 2, 3)
// 4. 세트의 붉은 돌에 닿으면 빛이 붉게 꺼짐, 비석/기둥은 빛을 막음
// 설계: 3^5 = 243가지 중 정답 1개 (빛 길이 22칸, 자기 길을 한 번 가로지름 - 컴퓨터로 확인함)
public class HorusSunMirrors : PuzzleBase, IInteractable
{
    private const string DoneFlag = "F_HO_H1_DONE";
    private const int Slash = 0, Back = 1, Flat = 2;

    [Header("격자 (칸 (0,0) 왼쪽 아래 = origin)")]
    [SerializeField] private Vector2 origin = new Vector2(0f, 5f);
    [SerializeField] private Vector2Int size = new Vector2Int(8, 8);

    [Header("연결 (빌더가 채움)")]
    [SerializeField] private SpriteRenderer eyeStand;
    [SerializeField] private Sprite dustySprite, cleanSprite, litSprite;
    [SerializeField] private Sprite mirrorA, mirrorB, mirrorFlat, sunSprite, setStoneSprite, blockSprite;

    [Header("진행")]
    [SerializeField] private string unlockedFlag = "F_HO_H1_UNLOCKED";
    [SerializeField] private string rewardItem = HorusItemIds.StarFrag1;

    // 해는 서쪽 벽 (-1, 6) 칸에서 동쪽으로. 목표 = (3, 3) 칸 (관측대)
    private static readonly Vector2Int Sun = new Vector2Int(-1, 6), SunDir = new Vector2Int(1, 0), Target = new Vector2Int(3, 3);
    private static readonly Vector2Int[] MirrorCells = { new Vector2Int(5, 6), new Vector2Int(1, 1), new Vector2Int(3, 0), new Vector2Int(1, 0), new Vector2Int(5, 1) };
    private static readonly Vector2Int[] SetStones = { new Vector2Int(4, 0), new Vector2Int(1, 5) };
    // 빛을 막는 돌기둥 (예전 본당 비석/기둥 자리 그대로라 정답이 같음). [수정] 해의 방으로 옮기면서 실제 돌기둥 소품을 놓음
    private static readonly Vector2Int[] Blocks = { new Vector2Int(6, 5), new Vector2Int(7, 4), new Vector2Int(7, 5), new Vector2Int(7, 6), new Vector2Int(7, 0) };

    private readonly List<HorusMirror> mirrors = new List<HorusMirror>();
    private LineRenderer beam;
    private SpriteRenderer sun;
    private Transform root;
    private bool done, busy;

    private bool Unlocked => IsFlagOn(unlockedFlag);
    private static readonly Color Gold = new Color(1f, 0.86f, 0.4f, 0.95f);
    private static readonly Color Red = new Color(1f, 0.3f, 0.25f, 0.95f);

    // ================= 준비 =================
    private void Start()
    {
        try
        {
            HorusFx.MatchPlayer();
            root = new GameObject("H1_SunMirrors").transform;
            if (eyeStand == null) eyeStand = GetComponent<SpriteRenderer>();
            BuildProps();
            beam = BuildBeam();
            done = IsFlagOn(DoneFlag) || IsFlagOn(ClearFlag);
            RefreshStand();
            Trace();
        }
        catch (Exception e)
        {
            Debug.LogError($"[매의 거울] 준비 오류: {e}");
            enabled = false;
        }
    }

    private void BuildProps()
    {
        sun = HorusWorld.Prop(root, "H1_WingedSun", sunSprite, World(Sun) + new Vector3(0.45f, 0.2f, 0f), 1.4f, Vector2.zero);
        sun.sortingOrder = 25; // 벽에 걸린 장식 (항상 위)
        for (int i = 0; i < MirrorCells.Length; i++)
        {
            HorusMirror m = HorusMirror.Create(this, root, i, World(MirrorCells[i]), mirrorA, mirrorB, mirrorFlat);
            mirrors.Add(m);
        }
        foreach (Vector2Int c in SetStones) HorusWorld.Prop(root, "H1_SetStone", setStoneSprite, World(c), 0.75f, new Vector2(0.6f, 0.4f));
        foreach (Vector2Int c in Blocks) HorusWorld.Prop(root, "H1_StoneColumn", blockSprite, World(c), 0.7f, new Vector2(0.6f, 0.4f));
    }

    private LineRenderer BuildBeam()
    {
        var go = new GameObject("H1_SunBeam");
        go.transform.SetParent(root, false);
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.startWidth = lr.endWidth = 0.12f;
        lr.sortingLayerName = HorusFx.Layer;
        lr.sortingOrder = 30010; // 물건 위로 지나가는 빛줄기
        if (RaDarkness.Unlit != null) lr.sharedMaterial = RaDarkness.Unlit;
        else lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.positionCount = 0;
        return lr;
    }

    private Vector3 World(Vector2Int c) => new Vector3(origin.x + c.x + 0.5f, origin.y + c.y + 0.5f, 0f);

    // ================= 빛 따라가기 =================
    // 거울을 돌릴 때마다 다시 계산. 결과: 목표에 닿음 / 붉은 돌 / 막힘 / 밖
    private enum Hit { None, Target, Red }

    public void Trace()
    {
        if (beam == null) return;
        if (!Unlocked && !done)
        {
            beam.positionCount = 0;
            return;
        }
        var pts = new List<Vector3> { World(Sun) + Vector3.right * 0.45f };
        Hit hit = Walk(pts);
        beam.positionCount = pts.Count;
        beam.SetPositions(pts.ToArray());
        beam.startColor = beam.endColor = hit == Hit.Red ? Red : Gold;
        if (hit == Hit.Target && !done) Submit();
    }

    private Hit Walk(List<Vector3> pts)
    {
        Vector2Int p = Sun, d = SunDir;
        for (int step = 0; step < 200; step++)
        {
            p += d;
            if (p.x < 0 || p.y < 0 || p.x >= size.x || p.y >= size.y) { pts.Add(World(p - d) + (Vector3)(Vector2)d * 0.5f); return Hit.None; }
            if (p == Target) { pts.Add(World(p)); return Hit.Target; }
            if (Array.IndexOf(Blocks, p) >= 0) { pts.Add(World(p) - (Vector3)(Vector2)d * 0.45f); return Hit.None; }
            if (Array.IndexOf(SetStones, p) >= 0) { pts.Add(World(p)); return Hit.Red; }
            int m = Array.IndexOf(MirrorCells, p);
            if (m < 0 || mirrors[m].State == Flat) continue;
            pts.Add(World(p));
            d = mirrors[m].State == Slash ? new Vector2Int(d.y, d.x) : new Vector2Int(-d.y, -d.x);
        }
        return Hit.None;
    }

    // ================= 판정 =================
    public bool CanTurn => Unlocked && !done && !busy;

    public override void Submit()
    {
        try
        {
            if (done || busy) return;
            if (Validate()) StartCoroutine(Complete());
        }
        catch (Exception e)
        {
            Debug.LogError($"[매의 거울] Submit 오류: {e}");
        }
    }

    protected override bool Validate()
    {
        var pts = new List<Vector3>();
        return Unlocked && Walk(pts) == Hit.Target;
    }

    private IEnumerator Complete()
    {
        busy = true;
        done = true;
        OnSuccessInternal();
        if (GameManager.Instance != null && !GameManager.Instance.HasFlag(DoneFlag)) GameManager.Instance.SetFlag(DoneFlag);
        if (ItemInventory.Instance != null && !string.IsNullOrEmpty(rewardItem)) ItemInventory.Instance.Grant(rewardItem);
        Debug.Log("[노트 2] 별은 한 번 걸은 길을 다시 걷지 않는다.");
        Debug.Log("[노트 3] 북쪽의 황소 다리는 지지 않는다 - 모든 별이 지나간 뒤에도 마지막까지 하늘에 남는다.");
        GameAudio.Play("sfx_discover", 0.8f);
        RefreshStand();
        HorusFx.StarBurst(World(Target), new Vector2(0.6f, 0.6f), 20, Gold);
        yield return ScreenTintUI.Flash(new Color(1f, 0.9f, 0.55f, 0.45f), 0.04f, 0.1f, 0.5f);
        HorusWorld.Say("해의 빛이 매의 눈에 닿았다... 눈이 떴어!", "관측대 위로 별 조각이 떠올랐다. 별 조각 1을 얻었다.");
        busy = false;
    }

    private void RefreshStand()
    {
        if (eyeStand == null) return;
        Sprite s = done ? litSprite : (Unlocked ? cleanSprite : dustySprite);
        if (s != null) eyeStand.sprite = s;
        if (sun != null) sun.color = Unlocked || done ? Color.white : new Color(0.45f, 0.42f, 0.4f);
    }

    // ================= 관측대 E: 먼지 털기 =================
    public string GetPrompt()
    {
        if (done) return "E: 매의 눈 보기";
        if (Unlocked) return "E: 매의 눈 살펴보기";
        return HasFeather() ? "E: 매의 깃털로 먼지 털기" : "E: 먼지 쌓인 관측대 조사";
    }

    public void Interact(GameObject who)
    {
        try
        {
            if (DialogueBox.IsOpen || Cine.Running || busy) return;
            if (done) HorusWorld.Say("매의 눈이 해의 빛을 머금고 있다.");
            else if (Unlocked) HorusWorld.Say("서쪽 벽의 해가 빛을 쏘고 있어. 거울 석상을 돌려서(E) 이 매의 눈까지 빛을 데려오자.",
                "붉은 돌은 세트의 것... 빛을 삼켜 버려. 거울은 눕혀 두면 빛이 그냥 지나가.");
            else if (HasFeather()) StartCoroutine(Dust());
            else HorusWorld.Say("눈 모양 받침에 먼지가 두껍게 쌓였다. 부드러운 깃털 같은 걸로 털어내야 할 것 같아.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[매의 거울] Interact 오류: {e}");
        }
    }

    private bool HasFeather() => ItemInventory.Instance != null && ItemInventory.Instance.Has(HorusItemIds.Feather);

    private IEnumerator Dust()
    {
        if (!ItemInventory.Instance.Consume(HorusItemIds.Feather)) yield break;
        busy = true;
        GameAudio.Play("sfx_brush", 0.8f);
        HorusFx.StarBurst(World(Target), new Vector2(0.5f, 0.5f), 16, new Color(0.8f, 0.72f, 0.6f));
        yield return new WaitForSeconds(0.6f);
        if (GameManager.Instance != null) GameManager.Instance.SetFlag(unlockedFlag);
        RefreshStand();
        GameAudio.Play("sfx_fire_ignite", 0.6f, 1.2f);
        Trace();
        busy = false;
        HorusWorld.Say("먼지를 털자 서쪽 벽의 날개 달린 해가 빛을 쏘기 시작했다!",
            "거울 석상을 돌려서(E) 빛을 이 매의 눈까지 데려오자. 거울은 / -> \\ -> 눕힘 순서로 바뀌어.");
    }

    // ================= 개발용 치트 (Inspector 우클릭) =================
    [ContextMenu("치트: 매의 깃털 지급")]
    private void CheatFeather()
    {
        if (ItemInventory.Instance != null) ItemInventory.Instance.Grant(HorusItemIds.Feather);
    }
}
