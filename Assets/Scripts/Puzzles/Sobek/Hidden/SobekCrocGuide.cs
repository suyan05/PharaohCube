using System;
using System.Collections;
using UnityEngine;

// 소베크의 악어 (통합 길잡이 기획서 8.2 AnimalGuide, 소베크판)
// 상태: Hidden -> (물이 빠지면 웅덩이에 눈 두 개 -> 첫 만남) Idle -> E: Leading -> 멀어지면 Waiting -> 도착 Signaling -> 다음 구간 ... -> Following
//  구간 0: 웅덩이 -> 제단 옆 갈대 (꼬리로 갈대를 쳐서 단서 3 반짝) / 완료: 단서 3 주움
//  구간 1: 갈대 -> 수위계 기둥 (꼬리로 물보라) / 완료: 숨은 방 열림
// - 동물은 말하지 않음: 이모트(!, ?)와 소리 대신 알림 한 줄, 해석은 케프리가 함
// - 단서 3을 혼자 주웠으면 구간 0을 건너뜀, 숨은 방을 이미 열었으면 바로 따라다님
// 붙이는 곳: 악어 오브젝트 (SpriteRenderer + 트리거 콜라이더). 경로 점은 자식 Transform
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class SobekCrocGuide : MonoBehaviour, IInteractable
{
    public enum State { Hidden, Idle, Leading, Waiting, Signaling, Following }

    [Serializable]
    public class Leg
    {
        [Tooltip("지나갈 점들 (마지막 점이 도착)")]
        public Transform[] path;
        [Tooltip("도착해서 신호를 보낼 단서 (반짝임). 비우면 물보라 알림만")]
        public SobekClueItem clue;
        [TextArea(1, 2)] public string signalText = "악어가 꼬리로 물을 친다.";
    }

    [Header("나타나는 조건 (물 빠짐 연출이 끝난 뒤)")]
    [SerializeField] private string appearFlag = SobekIds.FlagDrainPlayed;

    [Header("구간")]
    [SerializeField] private Leg[] legs;

    [Header("움직임")]
    [Range(0.5f, 6f)] [SerializeField] private float speed = 2.6f;
    [Tooltip("플레이어가 이만큼 멀어지면 멈추고 기다림")]
    [Range(1f, 8f)] [SerializeField] private float waitDistance = 3.5f;
    [Tooltip("이만큼 다가오면 다시 감")]
    [Range(0.5f, 6f)] [SerializeField] private float resumeDistance = 2.2f;
    [Range(1f, 15f)] [SerializeField] private float signalInterval = 5f;
    [Tooltip("다 끝난 뒤 플레이어 뒤를 따라오는 거리")]
    [Range(0.5f, 4f)] [SerializeField] private float followDistance = 1.6f;

    [Header("그림")]
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private Sprite[] walkFrames;
    [Tooltip("첫 만남 전 웅덩이에 떠 있는 눈 두 개")]
    [SerializeField] private Sprite eyesSprite;
    [Tooltip("머리 위 이모트 (!, ?)")]
    [SerializeField] private TMPro.TextMeshPro emote;

    [Header("첫 만남 (케프리 해석)")]
    [SerializeField] private string[] meetLines =
    {
        SobekStory.Khepri, "물이 빠졌는데도 악어가 남았군. 강의 비밀은 강이 지킨다.",
        SobekStory.Khepri, "저 악어를 따라가 봐. 남은 웅덩이를 헤엄쳐 어딘가로 데려가려 해.",
    };
    [SerializeField] private string[] afterClue3Lines =
    {
        SobekStory.Khepri, "밤의 일곱째 시간… 아펩이 강을 마셨다고? 이 신전의 물이 마른 것도 그놈 짓이다.",
    };

    private SpriteRenderer sr;
    private Transform player;
    private State state = State.Hidden;
    private int leg = -1;
    private int point;
    private float signalTimer;
    private float animTimer;
    private bool meeting;

    public State Current => state;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        PlayerController2D pc = FindAnyObjectByType<PlayerController2D>();
        if (pc != null) player = pc.transform;
        if (GameManager.Instance != null) GameManager.Instance.OnFlagChanged += HandleFlag;
        SetEmote("");
        if (SobekStory.HasFlag(appearFlag)) Appear(false);
        else SetVisible(false);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnFlagChanged -= HandleFlag;
    }

    private void HandleFlag(string f)
    {
        if (f == appearFlag && state == State.Hidden) Appear(true);
        if (f == SobekIds.ClueGot(3) && leg == 0) { FinishLeg(); SobekStory.Say(afterClue3Lines); }
        if (f == SobekIds.FlagHiddenOpen && leg == 1) FinishLeg();
    }

    private void SetVisible(bool v)
    {
        sr.enabled = v;
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = v;
    }

    // ================= 첫 만남 =================
    private void Appear(bool withMeeting)
    {
        SetVisible(true);
        state = State.Idle;
        if (SobekStory.HasFlag(SobekIds.FlagHiddenOpen)) { state = State.Following; return; }
        if (!SobekStory.HasFlag(SobekIds.FlagGuideMet)) StartCoroutine(Meet());
    }

    private IEnumerator Meet()
    {
        meeting = true;
        // 웅덩이에 눈 두 개만 -> 잠시 뒤 올라옴
        if (eyesSprite != null) sr.sprite = eyesSprite;
        AnubisToast.Show("남은 웅덩이 수면에 눈 두 개가 떴다…", 2f);
        yield return new WaitForSeconds(1.6f);
        if (Camera.main != null && Camera.main.TryGetComponent(out CameraFollow2D cam)) cam.Shake(0.3f, 0.05f);
        if (idleFrames != null && idleFrames.Length > 0) sr.sprite = idleFrames[0];
        SetEmote("!");
        SobekStory.SetFlag(SobekIds.FlagGuideMet);
        while (DialogueBox.IsOpen) yield return null; // 물 빠짐 대사가 끝난 뒤
        SobekStory.Say(() => meeting = false, meetLines);
        while (meeting) yield return null;
        SetEmote("");
    }

    // ================= 조사 =================
    public string GetPrompt()
    {
        if (state == State.Hidden || meeting) return "";
        if (state == State.Idle) return NextLeg() >= 0 ? "E: 악어 따라가기" : "E: 쓰다듬기";
        if (state == State.Following) return "E: 쓰다듬기";
        return "";
    }

    public void Interact(GameObject who)
    {
        if (meeting || SobekStory.Busy) return;
        if (state == State.Idle)
        {
            int next = NextLeg();
            if (next >= 0) { StartLeg(next); return; }
        }
        if (state == State.Idle || state == State.Following)
        {
            SetEmote("~"); // 쓰다듬기: 기능 없음, 정이 붙음
            AnubisToast.Show("악어가 눈을 가늘게 뜨고 낮게 그르렁거린다.", 1.8f);
            CancelInvoke(nameof(ClearEmote));
            Invoke(nameof(ClearEmote), 1.2f);
        }
    }

    private void ClearEmote() => SetEmote("");

    private int NextLeg()
    {
        if (legs == null) return -1;
        if (!SobekStory.HasFlag(SobekIds.ClueGot(3)) && legs.Length > 0) return 0;
        if (!SobekStory.HasFlag(SobekIds.FlagHiddenOpen) && legs.Length > 1) return 1;
        return -1;
    }

    private void StartLeg(int i)
    {
        leg = i;
        point = 0;
        state = State.Leading;
        SetEmote("");
        Debug.Log($"[SobekCrocGuide] 구간 {i} 안내 시작", this);
    }

    private void FinishLeg()
    {
        if (leg < 0) return;
        SobekStory.SetFlag(SobekIds.GuideLegDone(leg));
        Debug.Log($"[SobekCrocGuide] 구간 {leg} 완료", this);
        leg = -1;
        SetEmote("");
        state = NextLeg() >= 0 ? State.Idle : State.Following;
    }

    // ================= 움직임 =================
    private void Update()
    {
        if (state == State.Hidden || meeting || player == null) return;
        float dist = Vector2.Distance(transform.position, player.position);

        switch (state)
        {
            case State.Leading:
                if (dist > waitDistance) { state = State.Waiting; SetEmote("?"); break; }
                MoveAlong();
                break;
            case State.Waiting:
                if (dist < resumeDistance) { state = State.Leading; SetEmote(""); }
                break;
            case State.Signaling:
                signalTimer -= Time.deltaTime;
                if (signalTimer <= 0f) Signal();
                break;
            case State.Following:
                if (dist > followDistance)
                {
                    Vector3 target = player.position + (transform.position - player.position).normalized * followDistance;
                    Step(target);
                }
                else Animate(false);
                break;
            default:
                Animate(false);
                break;
        }
    }

    private void MoveAlong()
    {
        Leg l = legs[leg];
        if (l.path == null || point >= l.path.Length) { BeginSignal(); return; }
        Transform p = l.path[point];
        if (p == null) { point++; return; }
        if (Step(p.position)) point++;
    }

    // 목표 쪽으로 한 프레임 이동 (도착하면 true)
    private bool Step(Vector3 target)
    {
        Vector3 pos = transform.position;
        Vector3 d = target - pos;
        d.z = 0f;
        float m = d.magnitude;
        float stepLen = speed * Time.deltaTime;
        if (m <= stepLen) { transform.position = new Vector3(target.x, target.y, pos.z); Animate(true); return true; }
        transform.position = pos + d / m * stepLen;
        if (Mathf.Abs(d.x) > 0.01f) sr.flipX = d.x < 0f; // 그림은 오른쪽을 봄
        Animate(true);
        return false;
    }

    private void BeginSignal()
    {
        state = State.Signaling;
        signalTimer = 0f;
    }

    private void Signal()
    {
        signalTimer = signalInterval;
        Leg l = legs[leg];
        if (l.clue != null) l.clue.Sparkle();
        AnubisToast.Show(l.signalText, 1.8f);
        SetEmote("!");
        CancelInvoke(nameof(ClearEmote));
        Invoke(nameof(ClearEmote), 1f);
    }

    private void Animate(bool walking)
    {
        Sprite[] f = walking ? walkFrames : idleFrames;
        if (f == null || f.Length == 0) return;
        animTimer += Time.deltaTime * (walking ? 6f : 1.5f);
        sr.sprite = f[Mathf.FloorToInt(animTimer) % f.Length];
    }

    private void SetEmote(string s)
    {
        if (emote == null) return;
        emote.text = s;
        emote.enabled = !string.IsNullOrEmpty(s);
    }

    [ContextMenu("테스트: 바로 나타나기")]
    private void TestAppear()
    {
        if (Application.isPlaying) Appear(true);
    }

    private void OnDrawGizmosSelected()
    {
        if (legs == null) return;
        Gizmos.color = new Color(0.3f, 0.9f, 0.4f);
        foreach (Leg l in legs)
        {
            if (l.path == null) continue;
            Vector3 prev = transform.position;
            foreach (Transform t in l.path) { if (t == null) continue; Gizmos.DrawLine(prev, t.position); prev = t.position; }
        }
    }

#if UNITY_EDITOR
    public void EditorSetup(Leg[] l, Sprite[] idle, Sprite[] walk, Sprite eyes, TMPro.TextMeshPro emoteText)
    {
        legs = l; idleFrames = idle; walkFrames = walk; eyesSprite = eyes; emote = emoteText;
    }
#endif
}
