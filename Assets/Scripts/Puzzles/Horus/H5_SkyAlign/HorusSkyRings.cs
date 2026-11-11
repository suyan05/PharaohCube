using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// H5 하늘의 고리 (연동 회전 자물쇠) - 예전 H5(별 순서 누르기 화면 / 도는 별)를 바꿈
// [수정] 너무 어렵다는 의견: 별자리 이름(사흐/소프데트...)은 플레이어가 알 수 없음
//        -> 앞에서 이미 본 그림(해, 달, 매, 왕관 + 세트의 붉은 별)으로 바꾸고,
//           고리마다 북쪽 칸에 정답 그림을 흐리게 보여 줌. 시작 위치도 바꿔서 최소 9번 -> 5번 (컴퓨터로 확인)
//        신화: 해와 달은 호루스의 두 눈, 매는 호루스 자신, 왕관은 되찾은 왕좌 -> 안에서 밖으로
// (예전 신화 설정: 사흐 -> 소프데트 -> 우자트 -> 메스케티우. 지금은 위 그림으로 바뀜)
//       매(호루스)는 모든 그림이 모인 뒤 하늘 한가운데 앉는다 -> 고리 가운데가 매 자리
// 규칙
// 1. 제단 위 하늘 원판에 고리 4겹 (안 -> 밖). 고리마다 그림 6칸 (해/달/매/왕관 + 붉은 별 + 빈칸)
// 2. 제단 둘레 레버 4개. 레버 하나 = 고리 두 겹을 함께 한 칸(60도) 돌림 (1+2, 2+3, 3+4, 4+1) -> 하나만 돌릴 수 없음
// 3. 북쪽 금빛 선 위에 안쪽부터 해, 달, 매, 왕관이 서면 성공 (북쪽 칸에 정답 그림이 흐리게 보임)
// 4. 제단 E: 새겨진 글 (예전 성좌 카드/노트는 useOldClues를 켜야 붙음)
// 설계: 정답까지 최소 5번, 레버 4개를 다 써야 함 (컴퓨터로 확인함). 판정/보상/엔딩 플래그(F_HO_H5_DONE)는 예전과 같음
public class HorusSkyRings : PuzzleBase, IInteractable
{
    public const int Steps = 6;
    // 그림 번호 (값은 예전과 같음: 0 왕관(예전 메스케티우), 1 해(사흐), 2 달(소프데트), 3 매(우자트), 4 세트의 붉은 별)
    private const int Crown = 0, Sun = 1, Moon = 2, Falcon = 3, RedStar = 4, Blank = -1;

    // 고리별 6칸 (북쪽부터 시계 방향)
    private static readonly int[][] Contents =
    {
        new[] { Sun, RedStar, Moon, Blank, Crown, Falcon },
        new[] { Falcon, Moon, Blank, Sun, RedStar, Crown },
        new[] { Crown, Blank, Falcon, RedStar, Moon, Sun },
        new[] { Moon, Crown, RedStar, Sun, Blank, Falcon }
    };
    private static readonly int[] Want = { Sun, Moon, Falcon, Crown };
    private static readonly int[] StartOffset = { 3, 2, 2, 3 };
    private static readonly Vector2Int[] LeverRings = { new Vector2Int(0, 1), new Vector2Int(1, 2), new Vector2Int(2, 3), new Vector2Int(3, 0) };
    // [수정] 레버가 제단 양옆(기둥과 매 석상 사이)에 있어서 제단 둘레를 돌 때마다 몸이 걸렸음
    //        -> 네 개를 전부 제단 남쪽에 부채꼴로 모음. 가운데 길(폭 2.4칸)과 제단 양옆, 북쪽은 비움
    private static readonly Vector2[] LeverPos = { new Vector2(-2.5f, -2.3f), new Vector2(-1.2f, -2.8f), new Vector2(1.2f, -2.8f), new Vector2(2.5f, -2.3f) };
    private static readonly string[] Names = { "왕관", "해", "달", "매", "붉은 별" };

    [Header("연결 (빌더가 채움)")]
    [Tooltip("0 왕관, 1 해, 2 달, 3 매, 4 세트의 붉은 별")]
    [SerializeField] private Sprite[] starSprites = new Sprite[5];
    [SerializeField] private Sprite blankSprite;
    [SerializeField] private Sprite[] leverSprites = new Sprite[4];
    [SerializeField] private SkyAlignPuzzle cluesFrom; // 예전 H5의 성좌 카드/노트 글을 그대로 빌려 씀
    [Tooltip("켜면 예전 별자리 카드/노트도 제단 글에 붙임 (지금 그림과 안 맞아서 기본은 끔)")]
    [SerializeField] private bool useOldClues = false;
    [Tooltip("고리마다 북쪽 칸에 흐리게 보이는 정답 그림의 진하기 (0이면 안 보임)")]
    [SerializeField] private float targetAlpha = 0.35f;

    [Header("모양")]
    [SerializeField] private Vector2 discCenter = new Vector2(0f, 0.15f);
    [SerializeField] private float[] ringRadius = { 0.34f, 0.58f, 0.82f, 1.06f };
    [SerializeField] private float squash = 0.72f; // 위에서 비스듬히 본 원판
    [SerializeField] private float badgeSize = 0.24f;

    [Header("진행")]
    [SerializeField] private string gateFlag = "F_HO_CHART_4";
    [SerializeField] private string plateItem = HorusItemIds.PlateHorus;

    private readonly int[] offset = new int[4];
    private readonly float[] shown = new float[4];
    private readonly List<SpriteRenderer>[] badges = { new List<SpriteRenderer>(), new List<SpriteRenderer>(), new List<SpriteRenderer>(), new List<SpriteRenderer>() };
    private LineRenderer northLine;
    private Transform root;
    private bool done, busy, closeUp;
    private float closeUpTimer;

    public bool IsAwake => IsFlagOn(gateFlag);
    private Vector3 Disc => transform.position + (Vector3)discCenter;

    // ================= 준비 =================
    private void Start()
    {
        try
        {
            HorusFx.MatchPlayer();
            root = new GameObject("H5_SkyRings").transform;
            done = IsFlagOn(ClearFlag);
            for (int i = 0; i < 4; i++) SetRing(i, done ? Solved(i) : StartOffset[i]);
            BuildBadges();
            BuildTargets();
            BuildLevers();
            BuildTracks();
            northLine = BuildNorthLine();
            Place();
        }
        catch (Exception e)
        {
            Debug.LogError($"[하늘 고리] 준비 오류: {e}");
            enabled = false;
        }
    }

    // [수정] int(칸)와 float(그려지는 각도)를 한 줄에 같이 넣어서 생긴 컴파일 오류 -> 따로 넣음
    private void SetRing(int ring, int steps)
    {
        offset[ring] = steps;
        shown[ring] = steps;
    }

    // 고리 i가 정답일 때의 칸 수
    private static int Solved(int ring) => (Steps - Array.IndexOf(Contents[ring], Want[ring])) % Steps;

    private void BuildBadges()
    {
        for (int r = 0; r < 4; r++)
        {
            foreach (int id in Contents[r])
            {
                Sprite s = id >= 0 && id < starSprites.Length ? starSprites[id] : blankSprite;
                float size = id == Blank ? badgeSize * 0.45f : badgeSize;
                badges[r].Add(HorusFx.Spawn($"H5_Ring{r + 1}_{(id >= 0 ? Names[id] : "빈칸")}", s, Disc, size, Color.white, HorusFx.Above - 40 + r));
            }
        }
    }

    // 정답 그림을 고리별 북쪽 칸에 흐리게 (별 그림 뒤에 조금 크게 깔림 -> 맞는 그림이 오면 겹쳐서 가려짐)
    private readonly List<SpriteRenderer> targets = new List<SpriteRenderer>();

    private void BuildTargets()
    {
        for (int r = 0; r < 4; r++)
        {
            int id = Want[r];
            Sprite s = id < starSprites.Length ? starSprites[id] : null;
            Vector3 north = Disc + new Vector3(0f, ringRadius[r] * squash, 0f);
            SpriteRenderer t = HorusFx.Spawn($"H5_Target{r + 1}_{Names[id]}", s, north, badgeSize * 1.3f, new Color(1f, 1f, 1f, targetAlpha), HorusFx.Above - 44);
            targets.Add(t);
        }
    }

    private void BuildLevers()
    {
        for (int i = 0; i < 4; i++)
        {
            Sprite s = leverSprites != null && i < leverSprites.Length ? leverSprites[i] : null;
            SpriteRenderer sr = HorusWorld.Prop(root, $"H5_Lever_{i + 1}", s, transform.position + (Vector3)LeverPos[i], 0.5f, new Vector2(0.4f, 0.3f));
            HorusRingLever lever = sr.gameObject.AddComponent<HorusRingLever>();
            lever.Setup(this, i, LeverRings[i].x + 1, LeverRings[i].y + 1);
            HorusWorld.AddBox(sr.gameObject, new Vector2(0.7f, 1.1f), sr.transform.localScale.x, true);
        }
    }

    // 고리 4겹의 궤도 (레버에 칠한 색과 같은 색: 안쪽부터 빨강, 파랑, 초록, 노랑)
    private static readonly Color[] RingColors =
    {
        new Color(1f, 0.47f, 0.35f, 0.55f), new Color(0.35f, 0.78f, 1f, 0.55f),
        new Color(0.47f, 0.9f, 0.47f, 0.55f), new Color(0.94f, 0.78f, 0.31f, 0.55f)
    };
    private readonly LineRenderer[] tracks = new LineRenderer[4];

    private void BuildTracks()
    {
        for (int r = 0; r < 4; r++)
        {
            LineRenderer lr = NewLine($"H5_RingTrack_{r + 1}", 0.025f, HorusFx.Above - 45);
            lr.loop = true;
            lr.positionCount = 36;
            for (int k = 0; k < 36; k++)
            {
                float a = k * 10f * Mathf.Deg2Rad;
                lr.SetPosition(k, Disc + new Vector3(Mathf.Cos(a) * ringRadius[r], Mathf.Sin(a) * ringRadius[r] * squash, 0f));
            }
            lr.startColor = lr.endColor = RingColors[r];
            tracks[r] = lr;
        }
    }

    private LineRenderer NewLine(string objName, float width, int order)
    {
        var go = new GameObject(objName);
        go.transform.SetParent(root, false);
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.startWidth = lr.endWidth = width;
        lr.sortingLayerName = HorusFx.Layer;
        lr.sortingOrder = order;
        if (RaDarkness.Unlit != null) lr.sharedMaterial = RaDarkness.Unlit;
        else lr.material = new Material(Shader.Find("Sprites/Default"));
        return lr;
    }

    private LineRenderer BuildNorthLine()
    {
        LineRenderer lr = NewLine("H5_NorthLine", 0.05f, HorusFx.Above - 41);
        lr.positionCount = 2;
        lr.SetPosition(0, Disc);
        lr.SetPosition(1, Disc + new Vector3(0f, (ringRadius[3] + 0.2f) * squash, 0f));
        lr.endWidth = 0.02f;
        lr.startColor = lr.endColor = new Color(1f, 0.85f, 0.4f, 0.8f);
        return lr;
    }

    // ================= 그리기 =================
    private void Update()
    {
        try
        {
            if (badges[0].Count == 0) return;
            for (int i = 0; i < 4; i++) shown[i] = Mathf.MoveTowards(shown[i], offset[i], Time.deltaTime * 3f);
            Place();
            TickCloseUp();
        }
        catch (Exception e)
        {
            Debug.LogError($"[하늘 고리] 갱신 오류: {e}");
            enabled = false;
        }
    }

    private void Place()
    {
        bool lit = IsAwake || done;
        for (int r = 0; r < 4; r++)
        {
            for (int k = 0; k < Steps; k++)
            {
                float a = (90f - (k + shown[r]) * 60f) * Mathf.Deg2Rad; // 북쪽부터 시계 방향
                SpriteRenderer b = badges[r][k];
                b.transform.position = Disc + new Vector3(Mathf.Cos(a) * ringRadius[r], Mathf.Sin(a) * ringRadius[r] * squash, 0f);
                b.color = new Color(1f, 1f, 1f, lit ? 1f : 0.25f);
            }
        }
        if (northLine != null) northLine.enabled = lit;
        foreach (SpriteRenderer t in targets) if (t != null) t.enabled = lit && !done && targetAlpha > 0f;
        foreach (LineRenderer t in tracks) if (t != null) t.enabled = lit;
    }

    // 레버를 당기면 잠깐 제단을 가까이 비춤 (2초 동안 레버를 안 만지면 원래대로)
    private void TickCloseUp()
    {
        if (!closeUp || Cine.Running) return;
        closeUpTimer -= Time.deltaTime;
        if (closeUpTimer > 0f) return;
        closeUp = false;
        CineCamera.Release();
    }

    private void CloseUp(float seconds)
    {
        if (Cine.Running) return;
        HorusWorld.SafeFocus(Disc, 2.6f);
        closeUp = true;
        closeUpTimer = seconds;
    }

    // ================= 레버 (HorusRingLever가 부름) =================
    public bool CanPull => IsAwake && !done && !busy;

    public void Pull(int lever)
    {
        if (!CanPull) return;
        Vector2Int rings = LeverRings[lever];
        offset[rings.x] = (offset[rings.x] + 1) % Steps;
        offset[rings.y] = (offset[rings.y] + 1) % Steps;
        if (shown[rings.x] > offset[rings.x]) shown[rings.x] -= Steps; // 5 -> 0으로 넘어갈 때 거꾸로 돌지 않게
        if (shown[rings.y] > offset[rings.y]) shown[rings.y] -= Steps;
        GameAudio.Play("sfx_rumble", 0.25f, 1.6f);
        CloseUp(2.2f);
        if (Validate()) Submit();
    }

    // 지금 북쪽 선 위에 선 별 (고리 i)
    private int NorthOf(int ring) => Contents[ring][(Steps - offset[ring]) % Steps];

    public override void Submit()
    {
        try
        {
            if (done || busy || !Validate()) return;
            StartCoroutine(Complete());
        }
        catch (Exception e)
        {
            Debug.LogError($"[하늘 고리] Submit 오류: {e}");
        }
    }

    protected override bool Validate()
    {
        for (int i = 0; i < 4; i++) if (NorthOf(i) != Want[i]) return false;
        return true;
    }

    // 매가 날아와 가운데 앉음 -> 성공 (엔딩 연출은 F_HO_H5_DONE을 본 HorusDirector가 이어서)
    private IEnumerator Complete()
    {
        busy = true;
        done = true;
        yield return new WaitForSeconds(0.6f);
        HorusFalconGuide falcon = FindFirstObjectByType<HorusFalconGuide>();
        if (falcon != null) yield return falcon.PerchAt(Disc + Vector3.up * 0.25f, false);
        GameAudio.Play("sfx_dream_shimmer", 0.9f);
        HorusFx.StarBurst(Disc, new Vector2(1.2f, 0.9f), 30, new Color(1f, 0.85f, 0.45f));
        if (ItemInventory.Instance != null && !string.IsNullOrEmpty(plateItem)) ItemInventory.Instance.Grant(plateItem);
        Debug.Log("[노트 6] 하늘의 형판을 큐브의 면에 장착하라.");
        closeUp = false;
        CineCamera.Release();
        OnSuccessInternal();
        busy = false;
    }

    // ================= 제단 E: 새겨진 글 =================
    public string GetPrompt()
    {
        if (done) return "E: 제단 보기";
        return IsAwake ? "E: 제단의 글 읽기" : "E: 매의 제단 조사";
    }

    public void Interact(GameObject who)
    {
        try
        {
            if (DialogueBox.IsOpen || Cine.Running) return;
            if (done) HorusWorld.Say("하늘은 다시 호루스의 것이 되었다.");
            else if (!IsAwake) HorusWorld.Say("제단 위 하늘 원판이 어둡다. 성도판의 성좌 네 개를 모두 밝혀야 할 것 같아.");
            else
            {
                CloseUp(6f);
                DialogueBox.Play(Inscription());
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[하늘 고리] Interact 오류: {e}");
        }
    }

    private DialogueLine[] Inscription()
    {
        var lines = new List<DialogueLine>
        {
            new DialogueLine("제단", "\"해와 달은 나의 두 눈, 매는 나, 왕관은 되찾은 나의 자리. 안에서 밖으로, 북쪽 선 위에 세워라.\""),
            new DialogueLine("제단", "\"세트의 붉은 별은 하늘에 설 자리가 없다.\""),
            new DialogueLine("나", "북쪽 칸마다 흐릿하게 그림이 비쳐 보여. 저 그림이 오게 돌리면 되겠어."),
            new DialogueLine("나", "그런데 레버 하나가 고리 두 겹을 같이 돌려. 레버의 색 두 개가 어느 고리인지 알려 주는 것 같아.")
        };
        if (useOldClues && cluesFrom != null)
        {
            foreach (string c in cluesFrom.GetCardLines()) lines.Add(new DialogueLine("제단", c));
            foreach (string n in cluesFrom.GetNoteLines()) lines.Add(new DialogueLine("수첩", n));
        }
        return lines.ToArray();
    }

    // ================= 개발용 치트 (Inspector 우클릭) =================
    [ContextMenu("치트: 성좌 4개 점등")]
    private void CheatCharts()
    {
        for (int i = 1; i <= 4; i++) GameManager.Instance.SetFlag($"F_HO_CHART_{i}");
    }
}
