using System.Collections;
using UnityEngine;

// 수위계 기둥 = 소베크 숨은 방의 소켓 (기획서 4.5 / 8.3 ClueSocket)
// - 물이 차 있을 때: "물속에 눈금 기둥… 물이 빠져야 닿겠다" (소켓 발견, 화면 위 빈 홈 3개가 나타남)
// - 물이 빠진 뒤(메인 클리어): 주운 단서를 눈금에 끼움. 3칸이 다 차면 옆 돌판이 가라앉아 계단이 됨
//   -> 아펩 그림자가 계단 아래로 달아남 -> 숨은 방(물 빠진 하층) 열림
// - 단서가 모자라면 남은 단서가 있는 곳을 한 줄로 알려줌 (놓쳤을 때 안전장치)
// 붙이는 곳: 수위계 기둥 오브젝트 (트리거 콜라이더를 크게 잡아 물가에서도 E가 닿게)
[RequireComponent(typeof(Collider2D))]
public class SobekClueSocket : MonoBehaviour, IInteractable
{
    [Header("그림: 눈금 0~3칸 채움")]
    [SerializeField] private SpriteRenderer gaugeRenderer;
    [SerializeField] private Sprite[] gaugeSprites = new Sprite[4];

    [Header("열리는 것")]
    [Tooltip("가라앉을 돌판 (사라짐)")]
    [SerializeField] private GameObject slab;
    [Tooltip("드러날 계단 (켜짐)")]
    [SerializeField] private GameObject stairs;
    [Tooltip("달아나는 아펩 그림자 (비우면 연출 생략)")]
    [SerializeField] private SobekApepShadow apep;

    [Header("조건")]
    [Tooltip("이 플래그가 켜져야 손이 닿음 (물이 빠짐 = 메인 클리어)")]
    [SerializeField] private string reachFlag = SobekIds.FlagMainDone;

    [Header("문구")]
    [TextArea(1, 2)] [SerializeField] private string underwaterText = "물속에 눈금 기둥이 서 있다. 빈 눈금이 셋. 물이 빠져야 닿겠다.";
    [TextArea(1, 2)] [SerializeField] private string[] missingHints =
    {
        "전실 바닥의 범람 비석 조각이 아직 남아 있다.",
        "물 빠진 수로 바닥, 젖은 진흙 속에 하나가 더 있다.",
        "제단 옆 갈대 덤불 속에 하나가 더 있다.",
    };
    [SerializeField] private string[] openLines =
    {
        SobekStory.Me, "눈금이 다 찼다. 강의 높이를 맞췄다…",
        SobekStory.Khepri, "길이 열린다! 아펩의 그림자가 계단 아래로 달아났어.",
    };

    [Header("시간")]
    [Range(0.1f, 2f)] [SerializeField] private float slotTime = 0.6f;
    [Range(0.5f, 4f)] [SerializeField] private float sinkTime = 1.6f;

    private bool busy;

    public Vector3 SignalPoint => transform.position;

    private void Start() => Refresh(false);

    private int CountSet()
    {
        int n = 0;
        for (int i = 1; i <= 3; i++) if (SobekStory.HasFlag(SobekIds.ClueSet(i))) n++;
        return n;
    }

    private void Refresh(bool animateOpen)
    {
        int n = CountSet();
        if (gaugeRenderer != null && gaugeSprites != null && n < gaugeSprites.Length && gaugeSprites[n] != null) gaugeRenderer.sprite = gaugeSprites[n];
        bool open = SobekStory.HasFlag(SobekIds.FlagHiddenOpen);
        if (!animateOpen)
        {
            if (slab != null) slab.SetActive(!open);
            if (stairs != null) stairs.SetActive(open);
        }
    }

    // ================= IInteractable =================
    public string GetPrompt()
    {
        if (busy) return "";
        if (SobekStory.HasFlag(SobekIds.FlagHiddenOpen)) return "수위계 기둥";
        if (!SobekStory.HasFlag(reachFlag)) return "E: 물속 기둥 살펴보기";
        return $"E: 수위계에 단서 끼우기 ({CountSet()}/3)";
    }

    public void Interact(GameObject player)
    {
        if (busy || SobekStory.Busy || SobekStory.HasFlag(SobekIds.FlagHiddenOpen)) return;
        if (!SobekStory.HasFlag(reachFlag))
        {
            AnubisToast.Show(underwaterText, 3f);
            SobekStory.SetFlag(SobekIds.FlagSocketSeen);
            SobekClueHud.Refresh();
            return;
        }
        StartCoroutine(InsertRoutine());
    }

    private IEnumerator InsertRoutine()
    {
        busy = true;
        PlayerLock.Lock(this);
        int inserted = 0;
        for (int i = 1; i <= 3; i++)
        {
            if (!SobekStory.HasFlag(SobekIds.ClueGot(i)) || SobekStory.HasFlag(SobekIds.ClueSet(i))) continue;
            SobekStory.SetFlag(SobekIds.ClueSet(i));
            inserted++;
            Refresh(true);
            SobekClueHud.Refresh();
            ShakeCamera(0.15f, 0.04f);
            Debug.Log($"[SobekClueSocket] 단서 {i} 끼움 ({CountSet()}/3)", this);
            yield return new WaitForSeconds(slotTime);
        }

        int n = CountSet();
        if (n < 3)
        {
            PlayerLock.Unlock(this);
            busy = false;
            if (inserted == 0) AnubisToast.Show($"빈 눈금이 {3 - n}개 남았다. " + NextMissingHint(), 3.5f);
            else AnubisToast.Show($"눈금이 {n}/3 찼다. " + NextMissingHint(), 3.5f);
            yield break;
        }

        // 3칸 다 참 -> 돌판이 가라앉아 계단이 됨
        ShakeCamera(sinkTime, 0.1f);
        if (slab != null)
        {
            var sr = slab.GetComponent<SpriteRenderer>();
            Vector3 p0 = slab.transform.position;
            for (float t = 0f; t < sinkTime; t += Time.deltaTime)
            {
                float k = t / sinkTime;
                slab.transform.position = p0 + Vector3.down * (0.3f * k);
                if (sr != null) { Color c = sr.color; c.a = 1f - k; sr.color = c; }
                yield return null;
            }
            slab.SetActive(false);
            slab.transform.position = p0;
        }
        if (stairs != null) stairs.SetActive(true);
        if (apep != null) yield return apep.Flee();
        SobekStory.SetFlag(SobekIds.FlagHiddenOpen);
        SobekClueHud.Refresh();
        Debug.Log("[SobekClueSocket] 숨은 방(물 빠진 하층) 열림", this);
        PlayerLock.Unlock(this);
        busy = false;
        SobekStory.Say(openLines);
    }

    private string NextMissingHint()
    {
        for (int i = 1; i <= 3; i++)
            if (!SobekStory.HasFlag(SobekIds.ClueGot(i)) && i - 1 < missingHints.Length) return missingHints[i - 1];
        return "";
    }

    private static void ShakeCamera(float d, float s)
    {
        if (Camera.main != null && Camera.main.TryGetComponent(out CameraFollow2D cam)) cam.Shake(d, s);
    }

    private void OnDisable()
    {
        if (busy) PlayerLock.Unlock(this);
        busy = false;
    }

    [ContextMenu("테스트: 단서 3개 다 가진 상태로")]
    private void TestAll()
    {
        if (!Application.isPlaying) return;
        for (int i = 1; i <= 3; i++) SobekStory.SetFlag(SobekIds.ClueGot(i));
        SobekStory.SetFlag(reachFlag);
        SobekClueHud.Refresh();
    }

#if UNITY_EDITOR
    public void EditorSetup(SpriteRenderer gauge, Sprite[] sprites, GameObject slabObj, GameObject stairsObj, SobekApepShadow shadow)
    {
        gaugeRenderer = gauge; gaugeSprites = sprites; slab = slabObj; stairs = stairsObj; apep = shadow;
    }
#endif
}
