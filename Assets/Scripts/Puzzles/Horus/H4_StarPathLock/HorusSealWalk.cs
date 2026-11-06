using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// H4 세트의 봉인 (몸으로 푸는 퍼즐) - 예전 화면 퍼즐(StarPathLockPuzzle)을 바닥 별판 위에서 직접 걷는 방식으로 바꿈
// 신화: 세트가 호루스의 하늘을 붉은 봉인으로 덮고 지켜본다. 매는 동쪽(해 뜨는 곳)에서 날아올라 하늘 한가운데 앉는다
// 규칙
// 1. 매의 인장(H2-3 보상)으로 붉은 봉인을 풀면 별판이 깨어나고, 북쪽에서 '세트의 붉은 눈'이 좌우로 훑기 시작
// 2. 동쪽 끝 해 표시를 밟으면 매의 비행 시작 -> 벽화 '매의 비행' 순서대로 별 구멍 5개를 밟음 (밟은 별끼리 금빛 선)
// 3. 틀린 별을 밟거나, 걷는 도중 세트의 눈빛에 들키면 -> 붉게 번쩍, 처음부터. 별판 밖으로 나가면 조용히 취소
// 4. 3번 틀릴 때마다 벽화 첫 장면이 빛남 (예전 힌트 그대로)
// 정답 데이터, 보상(별 조각 3), 노트 5, 완료 플래그(F_HO_H4_DONE)는 예전 퍼즐과 같음
public class HorusSealWalk : PuzzleBase, IInteractable
{
    // H4_01 그림(768px) 기준 별 구멍 간격/보정 (HorusUiArt의 H4 오버레이 값과 같음)
    private const float ArtPx = 768f, GapX = 157f, GapY = 134f, OffX = 4.3f, OffY = -7.2f, SunX = 330f;

    [Header("연결 (빌더가 채움)")]
    [SerializeField] private SpriteRenderer board;
    [SerializeField] private Sprite sealSprite;
    [SerializeField] private SpriteRenderer mural;

    [Header("진행")]
    [SerializeField] private string unlockedFlag = "F_HO_H4_UNLOCKED";
    [SerializeField] private string rewardItem = HorusItemIds.StarFrag3;
    [SerializeField] private int failsForHint = 3;

    [Header("정답 순서 (3x3, 남서쪽 = 1,1, x3 = 동쪽) - 예전과 같음")]
    [SerializeField]
    private List<Vector2Int> answerPath = new List<Vector2Int>
    {
        new Vector2Int(3, 2), new Vector2Int(2, 3), new Vector2Int(1, 2),
        new Vector2Int(2, 1), new Vector2Int(2, 2)
    };

    [Header("세트의 붉은 눈")]
    [SerializeField] private float eyeAbove = 0.9f;
    [SerializeField] private float sweepDegrees = 55f;
    [SerializeField] private float sweepPeriod = 7f;
    [SerializeField] private float gazeHalfAngle = 15f;
    [SerializeField] private float gazeRange = 7.5f;

    private static readonly Color Gold = new Color(1f, 0.82f, 0.38f, 1f);
    private static readonly Color Red = new Color(1f, 0.25f, 0.2f, 1f);

    private readonly List<Vector2Int> walked = new List<Vector2Int>();
    private readonly Dictionary<Vector2Int, SpriteRenderer> glows = new Dictionary<Vector2Int, SpriteRenderer>();
    private SpriteRenderer sealCover, sunGlow, eyeGlow;
    private LineRenderer line;
    private Light2D gaze;
    private Transform player;
    private Vector2Int lastCell = new Vector2Int(-1, -1);
    private bool active, busy, done, toldRules, toldCaught;
    private int failCount;
    private float sweepTime;

    private bool Unlocked => IsFlagOn(unlockedFlag);
    private bool Paused => DialogueBox.IsOpen || Cine.Running || HorusFocus.PanelOpen;

    // ================= 준비 =================
    private void Start()
    {
        try
        {
            if (board == null) board = GetComponent<SpriteRenderer>();
            if (board == null)
            {
                Debug.LogError("[세트의 봉인] 별판 SpriteRenderer 연결 필요");
                enabled = false;
                return;
            }
            HorusFx.MatchPlayer();
            BuildVisuals();
            done = IsFlagOn(ClearFlag);
            if (done) ShowCleared();
            else if (!Unlocked) sealCover.enabled = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[세트의 봉인] 준비 오류: {e}");
        }
    }

    // 바닥 효과는 별판의 자식으로 두지 않고 따로 둠: 이름에 Floor가 들어가면 YSortSystem이 '항상 아래(500 + 원래 순서)'로 보냄
    // -> 별판(500 + 0)보다 1~3칸 위, 플레이어(1000 이상)보다 아래에 그려짐
    private int baseOrder;

    private void BuildVisuals()
    {
        baseOrder = board.sortingOrder >= 100 ? board.sortingOrder - 500 : board.sortingOrder; // 이미 정렬이 옮겼으면 되돌려 계산
        sealCover = FloorFx("SetSeal", sealSprite, board.bounds.center, board.bounds.size.x * 0.82f, Color.white, 1);
        sealCover.enabled = false;
        foreach (Vector2Int cell in AllCells()) glows[cell] = FloorFx("StarGlow", FxSprites.Soft, HolePos(cell), Gap().x * 0.9f, Clear(Gold), 2);
        sunGlow = FloorFx("SunGlow", FxSprites.Soft, SunPos(), Gap().x * 1.1f, Clear(Gold), 2);
        line = BuildLine();
        BuildEye();
    }

    private SpriteRenderer FloorFx(string objName, Sprite sprite, Vector3 pos, float size, Color color, int above)
    {
        return HorusFx.Spawn("Floor_H4_" + objName, sprite, pos, size, color, baseOrder + above);
    }

    private LineRenderer BuildLine()
    {
        var go = new GameObject("Floor_H4_FlightLine");
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 0;
        lr.startWidth = lr.endWidth = 0.08f;
        lr.sortingLayerName = HorusFx.Layer;
        lr.sortingOrder = 500 + baseOrder + 3; // 선은 SpriteRenderer가 아니라 정렬 시스템이 안 건드림 -> 바닥 구간 번호를 직접
        if (RaDarkness.Unlit != null) lr.sharedMaterial = RaDarkness.Unlit;
        else lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = Gold;
        return lr;
    }

    // 별판 북쪽 위에 떠 있는 붉은 눈 + 아래로 비추는 원뿔 빛 (좌우로 훑음). 봉인이 풀리기 전엔 감겨 있음
    // [임시] 그림이 없어 붉은 빛 덩어리. 세트의 눈 아트가 오면 eyeGlow 대신 그 그림으로
    private void BuildEye()
    {
        Vector3 at = EyePos();
        eyeGlow = RaGlowFx.Glow(null, at, 1.2f, Clear(Red), HorusFx.Layer, HorusFx.Above - 20);
        eyeGlow.gameObject.name = "H4_SetEye";
        var go = new GameObject("H4_SetGaze", typeof(Light2D)); // 별판(크기가 줄어든 그림)의 자식이 아니게 -> 빛 크기가 그대로
        go.transform.position = at;
        gaze = go.GetComponent<Light2D>();
        gaze.lightType = Light2D.LightType.Point;
        gaze.color = new Color(1f, 0.3f, 0.25f);
        gaze.pointLightOuterRadius = gazeRange;
        gaze.pointLightInnerRadius = 0.5f;
        gaze.pointLightInnerAngle = gazeHalfAngle * 2f;
        gaze.pointLightOuterAngle = gazeHalfAngle * 2f + 12f;
        gaze.intensity = 0f;
    }

    // ================= 좌표 =================
    private Vector2 Gap()
    {
        Vector3 s = board.bounds.size;
        return new Vector2(s.x * GapX / ArtPx, s.y * GapY / ArtPx);
    }

    private Vector3 HolePos(Vector2Int cell)
    {
        Vector3 s = board.bounds.size;
        Vector2 g = Gap();
        return board.bounds.center + new Vector3((cell.x - 2) * g.x + s.x * OffX / ArtPx, (cell.y - 2) * g.y + s.y * OffY / ArtPx, 0f);
    }

    private Vector3 SunPos()
    {
        Vector3 s = board.bounds.size;
        return board.bounds.center + new Vector3(s.x * SunX / ArtPx, s.y * OffY / ArtPx, 0f);
    }

    private Vector3 EyePos() => new Vector3(board.bounds.center.x, board.bounds.max.y + eyeAbove, 0f);

    private static IEnumerable<Vector2Int> AllCells()
    {
        for (int x = 1; x <= 3; x++)
            for (int y = 1; y <= 3; y++) yield return new Vector2Int(x, y);
    }

    // 발이 별 구멍 가까이 있으면 그 칸 (없으면 -1,-1)
    private Vector2Int CellAt(Vector3 p)
    {
        float r = Mathf.Min(Gap().x, Gap().y) * 0.36f;
        foreach (Vector2Int cell in AllCells())
        {
            if (Vector2.Distance(p, HolePos(cell)) <= r) return cell;
        }
        return new Vector2Int(-1, -1);
    }

    private bool OnBoard(Vector3 p)
    {
        Bounds b = board.bounds;
        b.Expand(new Vector3(0.6f, 0.6f, 10f)); // 해 표시가 판 가장자리라 조금 여유
        return b.Contains(new Vector3(p.x, p.y, b.center.z));
    }

    private static Color Clear(Color c) => new Color(c.r, c.g, c.b, 0f);

    // ================= 매 프레임 =================
    private void Update()
    {
        try
        {
            if (board == null || done) return;
            bool awake = Unlocked;
            SweepEye(awake);
            if (!awake || busy || Paused) return;
            if (player == null) player = CineActor.Player();
            if (player != null) Track(player.position);
        }
        catch (Exception e)
        {
            Debug.LogError($"[세트의 봉인] 갱신 오류: {e}");
            enabled = false;
        }
    }

    private void Track(Vector3 p)
    {
        if (!active)
        {
            if (Vector2.Distance(p, SunPos()) <= Gap().x * 0.45f) Begin();
            return;
        }
        if (!OnBoard(p))
        {
            Cancel();
            return;
        }
        if (SeenBySet(p))
        {
            Caught();
            return;
        }
        Vector2Int cell = CellAt(p);
        if (cell.x < 0 || cell == lastCell) return;
        lastCell = cell;
        Step(cell);
    }

    // 세트의 눈: 좌우로 천천히 훑음 (봉인이 풀리기 전엔 감겨 있음)
    private void SweepEye(bool awake)
    {
        float target = awake ? 1f : 0f;
        gaze.intensity = Mathf.MoveTowards(gaze.intensity, target * 1.3f, Time.deltaTime);
        eyeGlow.color = new Color(Red.r, Red.g, Red.b, Mathf.MoveTowards(eyeGlow.color.a, target * 0.85f, Time.deltaTime));
        if (!awake || Paused) return;
        sweepTime += Time.deltaTime;
        float angle = 180f + sweepDegrees * Mathf.Sin(sweepTime / sweepPeriod * Mathf.PI * 2f);
        gaze.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private bool SeenBySet(Vector3 p)
    {
        Vector2 v = p - gaze.transform.position;
        if (v.magnitude > gazeRange) return false;
        return Vector2.Angle(gaze.transform.up, v) <= gazeHalfAngle;
    }

    // ================= 진행 =================
    // 해 표시를 밟음 -> 매의 비행 시작
    private void Begin()
    {
        active = true;
        walked.Clear();
        lastCell = new Vector2Int(-1, -1);
        line.positionCount = 0;
        GameAudio.Play("sfx_dream_shimmer", 0.5f, 1.2f);
        StartCoroutine(Flash(sunGlow, Gold, 0.9f));
        HorusFx.StarBurst(SunPos(), new Vector2(0.3f, 0.3f), 8, Gold);
        if (toldRules) return;
        toldRules = true;
        DialogueBox.Play(new[]
        {
            new DialogueLine("나", "해가 떠오르는 자리야. 여기서부터 매가 날아오른 길을 밟아 가면 되겠지."),
            new DialogueLine("나", "...저 붉은 눈. 세트가 지켜보고 있어. 걷는 동안 저 빛에 들키면 안 되겠다.")
        });
    }

    private void Step(Vector2Int cell)
    {
        if (walked.Count >= answerPath.Count) return;
        if (walked.Contains(cell) || cell != answerPath[walked.Count])
        {
            walked.Add(cell);
            failCount++;
            OnFailInternal();
            return;
        }
        walked.Add(cell);
        glows[cell].color = new Color(Gold.r, Gold.g, Gold.b, 0.85f);
        line.positionCount = walked.Count;
        line.SetPosition(walked.Count - 1, HolePos(cell));
        GameAudio.Play("sfx_blip_horus", 0.5f, 0.8f + walked.Count * 0.12f);
        HorusFx.StarBurst(HolePos(cell), new Vector2(0.2f, 0.2f), 5, Gold);
        if (walked.Count >= answerPath.Count) Submit();
    }

    private void Cancel()
    {
        active = false;
        ClearPath();
    }

    private void Caught()
    {
        failCount++;
        if (player != null) EmoteBalloonUI.Pop(player, "!", 0.9f);
        GameAudio.Play("sfx_rumble", 0.5f, 1.4f);
        OnFailInternal();
        if (toldCaught) return;
        toldCaught = true;
        DialogueBox.Play(new[] { new DialogueLine("나", "들켰다! 세트의 눈빛이 지나갈 때는 멈추거나 피해야 해.") });
    }

    public override void Submit()
    {
        try
        {
            if (done || busy) return;
            if (Validate()) Complete();
            else
            {
                failCount++;
                OnFailInternal();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[세트의 봉인] Submit 오류: {e}");
        }
    }

    protected override bool Validate()
    {
        if (walked.Count != answerPath.Count) return false;
        for (int i = 0; i < answerPath.Count; i++) if (walked[i] != answerPath[i]) return false;
        return true;
    }

    // 틀림 / 들킴 (PuzzleBase.OnFailInternal이 부름). 게임 중이 아닐 땐 Unity가 부르는 Reset이라 무시
    public override void Reset()
    {
        if (!Application.isPlaying) return;
        base.Reset();
        if (isActiveAndEnabled) StartCoroutine(FailRoutine());
    }

    private IEnumerator FailRoutine()
    {
        busy = true;
        active = false;
        line.startColor = line.endColor = Red;
        foreach (SpriteRenderer g in glows.Values) if (g.color.a > 0.1f) g.color = new Color(Red.r, Red.g, Red.b, 0.85f);
        CineCamera.Shake(0.25f, 0.05f);
        yield return ScreenTintUI.Flash(new Color(1f, 0.2f, 0.15f, 0.3f), 0.03f, 0.05f, 0.35f);
        ClearPath();
        busy = false;
        if (failCount > 0 && failCount % failsForHint == 0) StartCoroutine(MuralHint());
    }

    private void ClearPath()
    {
        walked.Clear();
        lastCell = new Vector2Int(-1, -1);
        line.positionCount = 0;
        line.startColor = line.endColor = Gold;
        foreach (SpriteRenderer g in glows.Values) g.color = Clear(Gold);
    }

    // 3번 틀릴 때마다: 벽화 첫 장면(왼쪽)이 잠깐 빛남 (예전 힌트와 같음)
    private IEnumerator MuralHint()
    {
        if (mural == null) yield break;
        Bounds b = mural.bounds;
        Vector3 first = new Vector3(b.min.x + b.size.x / 6f, b.center.y, 0f);
        StartCoroutine(HorusFx.Pulse(first, b.size.y * 2.2f, Gold, 5f));
        HorusFx.StarBurst(first, new Vector2(0.3f, 0.3f), 10, Gold);
        yield return null;
        DialogueBox.Play(new[] { new DialogueLine("나", "벽화의 첫 장면이 빛난다... 매는 어디에서 떠올라 어디로 날아갔더라?") });
    }

    private void Complete()
    {
        done = true;
        active = false;
        OnSuccessInternal();
        if (ItemInventory.Instance != null && !string.IsNullOrEmpty(rewardItem)) ItemInventory.Instance.Grant(rewardItem);
        Debug.Log("[노트 5] 아들의 눈은 어머니의 별 뒤에 뜨고, 매는 모든 별이 모인 뒤 하늘 한가운데 앉는다.");
        StartCoroutine(SuccessRoutine());
    }

    private IEnumerator SuccessRoutine()
    {
        GameAudio.Play("sfx_discover", 0.8f);
        HorusFx.StarBurst(board.bounds.center, board.bounds.extents * 0.8f, 30, Gold);
        yield return ScreenTintUI.Flash(new Color(1f, 0.85f, 0.5f, 0.4f), 0.04f, 0.1f, 0.5f);
        ShowCleared();
        DialogueBox.Play(new[]
        {
            new DialogueLine("나", "세트의 눈이 감겼다... 매가 하늘 한가운데 앉았어."),
            new DialogueLine("나", "별판 가운데서 별 조각이 떠올랐다. 별 조각 3을 얻었다.")
        });
    }

    // 다 푼 모습: 별 5개 + 금빛 길이 켜져 있고 세트의 눈은 감김
    private void ShowCleared()
    {
        if (sealCover != null) sealCover.enabled = false;
        line.positionCount = answerPath.Count;
        for (int i = 0; i < answerPath.Count; i++)
        {
            line.SetPosition(i, HolePos(answerPath[i]));
            glows[answerPath[i]].color = new Color(Gold.r, Gold.g, Gold.b, 0.7f);
        }
        gaze.intensity = 0f;
        eyeGlow.color = Clear(Red);
    }

    private IEnumerator Flash(SpriteRenderer sr, Color c, float seconds)
    {
        float t = 0f;
        while (t < seconds && sr != null)
        {
            t += Time.deltaTime;
            sr.color = new Color(c.r, c.g, c.b, Mathf.Sin(t / seconds * Mathf.PI) * 0.9f);
            yield return null;
        }
        if (sr != null) sr.color = Clear(c);
    }

    // ================= E 상호작용 (별판 조사 / 봉인 풀기) =================
    public string GetPrompt()
    {
        if (done) return "E: 별판 보기";
        if (Unlocked) return "E: 별판 살펴보기";
        return HasSeal() ? "E: 매의 인장 대기" : "E: 붉은 봉인 조사";
    }

    public void Interact(GameObject who)
    {
        try
        {
            if (DialogueBox.IsOpen || Cine.Running || busy) return;
            if (done) Say("봉인이 풀린 별판이 고요히 빛난다.");
            else if (Unlocked) Say("동쪽 끝 해 표시를 밟으면 매의 비행이 시작돼. 벽화 '매의 비행' 순서대로 별을 밟자.");
            else if (HasSeal()) StartCoroutine(UnlockRoutine());
            else Say("붉은 봉인이 별판을 덮고 있다. 세트의 표식... 매의 인장이 있으면 풀 수 있을 것 같아.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[세트의 봉인] Interact 오류: {e}");
        }
    }

    private bool HasSeal() => ItemInventory.Instance != null && ItemInventory.Instance.Has(HorusItemIds.SealFalcon);

    private static void Say(string text)
    {
        DialogueBox.Play(new[] { new DialogueLine("나", text) });
    }

    // 인장을 대면: 봉인이 붉게 떨다 깨짐 -> 세트의 눈이 뜸
    private IEnumerator UnlockRoutine()
    {
        if (!ItemInventory.Instance.Consume(HorusItemIds.SealFalcon)) yield break;
        busy = true;
        GameAudio.Play("sfx_rumble", 0.6f, 0.9f);
        yield return CineCamera.Rumble(0.7f, 0.02f, 0.08f);
        HorusFx.StarBurst(board.bounds.center, board.bounds.extents * 0.6f, 26, Red);
        yield return ScreenTintUI.Flash(new Color(1f, 0.3f, 0.2f, 0.45f), 0.03f, 0.08f, 0.5f);
        sealCover.enabled = false;
        if (GameManager.Instance != null) GameManager.Instance.SetFlag(unlockedFlag);
        busy = false;
        DialogueBox.Play(new[]
        {
            new DialogueLine("나", "봉인이 깨졌다! ...그런데 저 위에서 붉은 눈이 떴어."),
            new DialogueLine("나", "벽화 속 매가 날아간 길대로 별을 밟아야 해. 해가 뜨는 쪽에서부터.")
        });
    }

    // ================= 개발용 치트 (Inspector 우클릭) =================
    [ContextMenu("치트: 매 인장 지급")]
    private void CheatSeal()
    {
        if (ItemInventory.Instance != null) ItemInventory.Instance.Grant(HorusItemIds.SealFalcon);
    }
}
