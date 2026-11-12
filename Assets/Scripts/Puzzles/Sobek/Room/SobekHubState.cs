using System.Collections;
using UnityEngine;

// 허브 진행 상태 (씬 시작 때 한 번 확인)
// - 처음 들어오면: 케프리·나 대사로 방의 목표(부적 4개 -> 소베크의 입)와 물에 잠긴 수위계 기둥을 알려줌
// - 메인을 깼으면(F_SOBEK_MAIN_DONE): 큰 문 앞에 플레이어를 두고, 신전의 물이 빠지는 연출 1번
//   -> 수로 바닥(단서 2)·남은 웅덩이(악어)·중앙 홀로 가는 출구가 드러남
// - 다시 들어오면 연출 없이 바로 물 빠진 상태
// 붙이는 곳: 허브 씬의 SobekHub 오브젝트
public class SobekHubState : MonoBehaviour
{
    [Header("물 (빠지면 사라짐)")]
    [Tooltip("물 그림들 (알파가 줄어들며 사라짐)")]
    [SerializeField] private SpriteRenderer[] waterRenderers;
    [Tooltip("물이 막고 있던 길 (물이 빠지면 꺼짐)")]
    [SerializeField] private Collider2D[] waterBlockers;

    [Header("물이 빠지면 켜질 것 (아누비스 문, 길)")]
    [SerializeField] private GameObject[] showWhenDrained;

    [Header("메인에서 돌아왔을 때 설 자리")]
    [SerializeField] private Transform returnSpawn;

    [Header("연출")]
    [Range(0.5f, 6f)] [SerializeField] private float drainTime = 3f;
    [SerializeField] private CameraFollow2D cameraFollow;
    [TextArea(1, 2)]
    [SerializeField] private string drainMessage = "신전의 물이 빠진다… 물 밑에 잠겨 있던 수로 바닥과 아누비스의 문이 드러났다.";

    [Header("대사 (이름, 글 짝)")]
    [SerializeField] private string[] introLines =
    {
        SobekStory.Khepri, "여기는 소베크의 신전, 셰데트야. 신전이 온통 물에 잠겨 있어.",
        SobekStory.Me, "물 위로 좁은 길과 섬 네 개만 떠 있다… 저 큰 문은 악어 입 모양이다. 홈이 네 개 비어 있다.",
        SobekStory.Khepri, "섬마다 시험의 문이 있어. 네 시험을 지나면 부적을 줄 거야. 비석에 힌트가 새겨져 있어.",
    };
    [SerializeField] private string[] drainedLines =
    {
        SobekStory.Khepri, "물이 빠졌어! 물속에 잠겨 있던 문이 드러났어. 저 아래가 아누비스의 신전이야.",
        SobekStory.Khepri, "숨은 것을 더 찾고 싶으면 둘러보고, 다 됐으면 그 문으로 내려가자. (남쪽 출구는 중앙 홀)",
    };

    private static bool HasFlag(string f) => GameManager.Instance != null && GameManager.Instance.HasFlag(f);

    private void Start()
    {
        if (cameraFollow == null && Camera.main != null) cameraFollow = Camera.main.GetComponent<CameraFollow2D>();
        RestoreAmulets();

        bool done = HasFlag(SobekIds.FlagMainDone);
        SetDrained(false, 1f);
        if (!done)
        {
            if (!HasFlag(SobekIds.FlagHubIntro)) StartCoroutine(IntroRoutine());
            return;
        }

        if (returnSpawn != null) MovePlayer(returnSpawn.position);
        if (HasFlag(SobekIds.FlagDrainPlayed)) SetDrained(true, 0f);
        else StartCoroutine(DrainRoutine());
    }

    private IEnumerator DrainRoutine()
    {
        PlayerLock.Lock(this);
        yield return new WaitForSeconds(0.8f);
        if (cameraFollow != null) cameraFollow.Shake(drainTime, 0.08f);
        AnubisToast.Show(drainMessage, drainTime + 1.5f);
        for (float t = 0f; t < drainTime; t += Time.deltaTime)
        {
            SetAlpha(1f - t / drainTime);
            yield return null;
        }
        SetDrained(true, 0f);
        if (GameManager.Instance != null) GameManager.Instance.SetFlag(SobekIds.FlagDrainPlayed);
        PlayerLock.Unlock(this);
        Debug.Log("[SobekHubState] 물 빠짐 -> 수로 바닥·악어·출구 열림", this);
        yield return new WaitForSeconds(0.3f);
        SobekStory.Say(drainedLines);
    }

    // 서브 씬에서 받은 부적은 그 씬의 ItemInventory에 들어갔다가 씬과 함께 사라짐 (Managers는 DontDestroyOnLoad가 아님)
    // 플래그(GameManager)는 남아 있으니, 푼 서브의 부적을 허브 인벤토리에 다시 넣어 줌 (문에 넣기 전까지만)
    private static void RestoreAmulets()
    {
        ItemInventory inv = ItemInventory.Instance;
        if (inv == null || HasFlag(SobekIds.FlagDoorOpen)) return;
        for (int i = 0; i < SobekIds.Amulets.Length; i++)
        {
            if (!HasFlag(SobekIds.AmuletClearFlags[i]) || inv.Has(SobekIds.Amulets[i])) continue;
            inv.Grant(SobekIds.Amulets[i]);
            Debug.Log($"[SobekHubState] 부적 복구: {SobekIds.Amulets[i]} ({SobekIds.AmuletClearFlags[i]})");
        }
    }

    private IEnumerator IntroRoutine()
    {
        yield return new WaitForSeconds(0.6f);
        SobekStory.SetFlag(SobekIds.FlagHubIntro);
        SobekStory.Say(introLines);
    }

    private void SetDrained(bool drained, float alpha)
    {
        SetAlpha(alpha);
        if (waterRenderers != null) foreach (var r in waterRenderers) if (r != null) r.enabled = !drained;
        if (waterBlockers != null) foreach (var c in waterBlockers) if (c != null) c.enabled = !drained;
        if (showWhenDrained != null) foreach (var g in showWhenDrained) if (g != null) g.SetActive(drained);
    }

    private void SetAlpha(float a)
    {
        if (waterRenderers == null) return;
        foreach (var r in waterRenderers)
        {
            if (r == null) continue;
            Color c = r.color; c.a = a; r.color = c;
        }
    }

    private static void MovePlayer(Vector3 pos)
    {
        PlayerController2D p = FindFirstObjectByType<PlayerController2D>();
        if (p == null) return;
        var rb = p.GetComponent<Rigidbody2D>();
        if (rb != null) rb.position = pos;
        p.transform.position = new Vector3(pos.x, pos.y, p.transform.position.z);
        if (Camera.main != null && Camera.main.TryGetComponent(out CameraFollow2D cam)) cam.SnapToTarget();
    }

    private void OnDestroy() => PlayerLock.Unlock(this);

    [ContextMenu("테스트: 메인 클리어 처리 후 다시 시작")]
    private void TestDone()
    {
        if (!Application.isPlaying || GameManager.Instance == null) return;
        GameManager.Instance.SetFlag(SobekIds.FlagMainDone);
        StartCoroutine(DrainRoutine());
    }
}
