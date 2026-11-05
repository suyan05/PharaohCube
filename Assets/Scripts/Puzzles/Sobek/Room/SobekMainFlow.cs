using System.Collections;
using UnityEngine;

// 메인 씬(Sobek_Main) 흐름: A구역 범람기 -> 소베크의 입(게이트) -> B구역 건기 -> 소베크-라 제단 -> 허브로
// - 씬 시작하면 바로 A판에 들어감 (판 밖으로 나가기 없음)
// - A를 풀면 화면이 어두워졌다가 B판 시작 칸에서 다시 밝아짐
// - B를 풀면 형판 지급 + 플래그 -> 허브 씬으로 돌아가 물 빠짐 연출 (SobekHubState)
// 붙이는 곳: 메인 씬의 SobekMain 오브젝트
public class SobekMainFlow : MonoBehaviour
{
    [Header("판")]
    [SerializeField] private SobekBoard zoneA;
    [SerializeField] private SobekBoard zoneB;

    [Header("보상")]
    [SerializeField] private string plateItemId = SobekIds.Plate;
    [Tooltip("팀 PlayerInventory의 소베크 형판 칸 번호 [확인 필요]")]
    [SerializeField] private int playerInventoryFace = 4;

    [Header("이동")]
    [SerializeField] private string hubSceneName = SobekIds.HubScene;
    [Range(0.1f, 3f)] [SerializeField] private float fadeTime = 0.8f;

    [Header("대사 (아래 알림, 한 줄씩)")]
    [TextArea(1, 2)]
    [SerializeField] private string[] introLines =
    {
        "아펩이 저승의 강을 삼키기 전, 소베크는 마지막 범람의 물을 이 신전에 가둬 두었다.",
        "물이 찬 신전을 지나 소베크의 입으로 들어가자.",
    };
    [TextArea(1, 2)]
    [SerializeField] private string[] gateLines =
    {
        "소베크의 입을 지나 물이 빠진 아래층으로 내려왔다.",
        "악어 무덤의 미궁... 제단은 벽 너머에 있다. 수면에 비친 길을 찾자.",
    };
    [TextArea(1, 2)]
    [SerializeField] private string[] altarLines =
    {
        "소베크-라의 태양 원반이 빛난다.",
        "\"물을 다스릴 줄 아는구나. 가둬 둔 물을 강으로 돌려보내겠다.\"",
        "\"그 형판을 가져가라. 물이 빠진 자리에 심판의 문이 있다.\"",
    };
    [Range(1f, 6f)] [SerializeField] private float lineTime = 2.8f;

    private GameObject player;

    private void Start()
    {
        player = FindPlayer();
        if (zoneA == null || zoneB == null || player == null)
        {
            Debug.LogError("[SobekMainFlow] 판 A/B 또는 플레이어가 없음", this);
            return;
        }
        zoneA.Cleared += HandleACleared;
        zoneB.Cleared += HandleBCleared;
        StartCoroutine(Begin());
    }

    private void OnDestroy()
    {
        if (zoneA != null) zoneA.Cleared -= HandleACleared;
        if (zoneB != null) zoneB.Cleared -= HandleBCleared;
        PlayerLock.Unlock(this);
    }

    private static GameObject FindPlayer()
    {
        PlayerController2D p = FindFirstObjectByType<PlayerController2D>();
        return p != null ? p.gameObject : null;
    }

    private IEnumerator Begin()
    {
        yield return null; // 다른 Start가 끝난 뒤
        // A를 이미 지났으면 (B에서 실패 후 다시 들어온 경우 등) B부터
        bool aDone = GameManager.Instance != null && GameManager.Instance.HasFlag(SobekIds.FlagMainADone);
        SobekBoard first = aDone ? zoneB : zoneA;
        first.Enter(player);
        SnapCamera();
        yield return Say(aDone ? gateLines : introLines);
    }

    private void HandleACleared(SobekBoard b) => StartCoroutine(GoToB());
    private void HandleBCleared(SobekBoard b) => StartCoroutine(Finish());

    private IEnumerator GoToB()
    {
        PlayerLock.Lock(this);
        if (GameManager.Instance != null) GameManager.Instance.SetFlag(SobekIds.FlagMainADone);
        AnubisToast.Show("소베크의 입이 열린다...", 2f);
        yield return new WaitForSeconds(0.5f);
        yield return ScreenFader.FadeOut(fadeTime);
        zoneA.Release();
        zoneB.Enter(player);
        SnapCamera();
        yield return ScreenFader.FadeIn(fadeTime);
        PlayerLock.Unlock(this);
        yield return Say(gateLines);
    }

    private IEnumerator Finish()
    {
        PlayerLock.Lock(this);
        if (Camera.main != null && Camera.main.TryGetComponent(out CameraFollow2D cam)) cam.Shake(1f, 0.12f);
        yield return Say(altarLines);
        PlayerLock.Unlock(this);

        GameManager gm = GameManager.Instance;
        if (gm != null)
        {
            gm.SetFlag(SobekIds.FlagMainDone);
            gm.SetFlag(SobekIds.FlagPlateGiven);
        }
        PlayerInventory inv = FindFirstObjectByType<PlayerInventory>();
        if (inv != null) inv.AddPlate(playerInventoryFace);

        bool closed = false;
        ItemPopup.Grant(plateItemId, () => closed = true);
        while (!closed) yield return null;

        PlayerLock.Lock(this);
        yield return ScreenFader.FadeOut(fadeTime);
        PlayerLock.Unlock(this);
        Debug.Log("[SobekMainFlow] 메인 클리어 -> 허브로", this);
        SceneFlow.LoadByNameOrNext(hubSceneName);
    }

    private IEnumerator Say(string[] lines)
    {
        if (lines == null) yield break;
        foreach (string line in lines)
        {
            AnubisToast.Show(line, lineTime);
            yield return new WaitForSeconds(lineTime);
        }
    }

    private static void SnapCamera()
    {
        if (Camera.main != null && Camera.main.TryGetComponent(out CameraFollow2D cam)) cam.SnapToTarget();
    }

    [ContextMenu("테스트: A 건너뛰고 B로")]
    private void TestSkipA()
    {
        if (Application.isPlaying) StartCoroutine(GoToB());
    }

#if UNITY_EDITOR
    public void EditorSetup(SobekBoard a, SobekBoard b) { zoneA = a; zoneB = b; }
#endif
}
