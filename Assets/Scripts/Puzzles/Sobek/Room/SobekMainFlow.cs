using System;
using System.Collections;
using UnityEngine;

// 메인 씬(Sobek_Main) 흐름: 구역 1 범람기 -> 구역 2 건기 미궁 -> 구역 3 소베크-라의 탑 -> 제단 -> 허브로
// - 씬 시작하면 바로 첫 구역에 들어감 (판 밖으로 나가기 없음)
// - 구역을 풀면 화면이 어두워졌다가 다음 구역 시작 칸에서 다시 밝아짐 (지난 구역은 플래그로 기억 -> 다시 와도 이어서)
// - 마지막 구역을 풀면 형판 지급 + 플래그 -> 허브 씬으로 돌아가 물 빠짐 연출 (SobekHubState)
// 붙이는 곳: 메인 씬의 SobekMain 오브젝트
public class SobekMainFlow : MonoBehaviour
{
    [Serializable]
    public class Zone
    {
        public SobekBoard board;
        [Tooltip("이 구역에 들어갈 때 아래 알림으로 한 줄씩")]
        [TextArea(1, 2)] public string[] lines;
    }

    [Header("구역 (순서대로)")]
    [SerializeField] private Zone[] zones;

    [Header("보상")]
    [SerializeField] private string plateItemId = SobekIds.Plate;
    [Tooltip("팀 PlayerInventory의 소베크 형판 칸 번호 [확인 필요]")]
    [SerializeField] private int playerInventoryFace = 4;

    [Header("형판 공개 · 큐브 연출 (바스테트와 같은 연출, 칸만 소베크)")]
    [Tooltip("비우면 예전처럼 형판 팝업 후 허브로")]
    [SerializeField] private BastetEnding ending;

    [Header("이동")]
    [SerializeField] private string hubSceneName = SobekIds.HubScene;
    [Range(0.1f, 3f)] [SerializeField] private float fadeTime = 0.8f;

    [Header("마지막 제단 대사 (이름, 글 짝 / 대사창 + 초상화)")]
    [SerializeField] private string[] altarLines =
    {
        SobekStory.Me, "소베크-라의 태양 원반이 빛난다…",
        SobekStory.Sobek, "아펩이 강을 마시기 전, 나는 마지막 범람을 이 신전에 가둬 두었다.",
        SobekStory.Sobek, "물을 다스릴 줄 아는구나. 가둔 물을 강으로 돌려보내겠다.",
        SobekStory.Sobek, "그 형판을 가져가라. 큐브의 다섯 번째 면이 될 것이다.",
        SobekStory.Sobek, "물이 빠진 자리에 강의 비밀이 남는다. 나의 악어가 길을 알려 주리라.",
    };
    [Range(1f, 6f)] [SerializeField] private float lineTime = 2.8f;

    private GameObject player;
    private int current = -1;

    private static string ZoneFlag(int i) => "F_SOBEK_MAIN_ZONE_" + i; // i번 구역을 풀었음

    private void Start()
    {
        player = FindPlayer();
        if (zones == null || zones.Length == 0 || player == null)
        {
            Debug.LogError("[SobekMainFlow] 구역 또는 플레이어가 없음", this);
            return;
        }
        foreach (Zone z in zones) if (z.board != null) z.board.Cleared += HandleCleared;
        StartCoroutine(Begin());
    }

    private void OnDestroy()
    {
        if (zones != null) foreach (Zone z in zones) if (z.board != null) z.board.Cleared -= HandleCleared;
        PlayerLock.Unlock(this);
    }

    private static GameObject FindPlayer()
    {
        PlayerController2D p = FindFirstObjectByType<PlayerController2D>();
        return p != null ? p.gameObject : null;
    }

    private static bool HasFlag(string f) => GameManager.Instance != null && GameManager.Instance.HasFlag(f);

    private IEnumerator Begin()
    {
        yield return null; // 다른 Start가 끝난 뒤
        int first = 0;
        while (first < zones.Length - 1 && HasFlag(ZoneFlag(first))) first++; // 이미 푼 구역은 건너뜀
        yield return EnterZone(first, false);
    }

    private IEnumerator EnterZone(int i, bool fade)
    {
        if (fade)
        {
            PlayerLock.Lock(this);
            yield return ScreenFader.FadeOut(fadeTime);
        }
        if (current >= 0 && zones[current].board != null) zones[current].board.Release();
        current = i;
        zones[i].board.Enter(player);
        SnapCamera();
        if (fade)
        {
            yield return ScreenFader.FadeIn(fadeTime);
            PlayerLock.Unlock(this);
        }
        Debug.Log($"[SobekMainFlow] 구역 {i + 1}/{zones.Length} 시작", this);
        yield return Say(zones[i].lines);
    }

    private void HandleCleared(SobekBoard b)
    {
        int i = Array.FindIndex(zones, z => z.board == b);
        if (i < 0) return;
        if (GameManager.Instance != null) GameManager.Instance.SetFlag(ZoneFlag(i));
        if (i == SobekIds.MainZoneA) GameManager.Instance?.SetFlag(SobekIds.FlagMainADone);
        StartCoroutine(i + 1 < zones.Length ? NextZone(i + 1) : Finish());
    }

    private IEnumerator NextZone(int next)
    {
        AnubisToast.Show("소베크의 입이 열린다... 더 깊은 곳으로.", 2f);
        yield return new WaitForSeconds(0.6f);
        yield return EnterZone(next, true);
    }

    private IEnumerator Finish()
    {
        PlayerLock.Lock(this);
        if (Camera.main != null && Camera.main.TryGetComponent(out CameraFollow2D cam)) cam.Shake(1f, 0.12f);
        yield return new WaitForSeconds(0.8f);
        PlayerLock.Unlock(this);
        bool talking = true;
        SobekStory.Say(() => talking = false, altarLines);
        while (talking) yield return null;

        GameManager gm = GameManager.Instance;
        if (gm != null)
        {
            gm.SetFlag(SobekIds.FlagMainDone);
            gm.SetFlag(SobekIds.FlagTabletReady);
        }
        Debug.Log("[SobekMainFlow] 메인 클리어 -> 형판 공개 -> 큐브 -> 허브(물 빠짐)", this);

        if (ending != null)
        {
            // 형판 아이템은 조용히 넣고(큐브 칸 확인용), 공개·큐브 연출은 BastetEnding이 맡음 -> 끝나면 허브로
            if (ItemInventory.Instance != null && !ItemInventory.Instance.Has(plateItemId)) ItemInventory.Instance.Grant(plateItemId);
            ending.Play();
            yield break;
        }

        // 연출이 없을 때: 예전 방식 (형판 팝업 -> 허브)
        PlayerInventory inv = FindFirstObjectByType<PlayerInventory>();
        if (inv != null) inv.AddPlate(playerInventoryFace);
        if (gm != null) gm.SetFlag(SobekIds.FlagPlateGiven);
        bool closed = false;
        ItemPopup.Grant(plateItemId, () => closed = true);
        while (!closed) yield return null;

        PlayerLock.Lock(this);
        yield return ScreenFader.FadeOut(fadeTime);
        PlayerLock.Unlock(this);
        SceneFlow.LoadByNameOrNext(hubSceneName);
    }

    private IEnumerator Say(string[] lines)
    {
        if (lines == null) yield break;
        foreach (string line in lines)
        {
            if (string.IsNullOrEmpty(line)) continue;
            AnubisToast.Show(line, lineTime);
            yield return new WaitForSeconds(lineTime);
        }
    }

    private static void SnapCamera()
    {
        if (Camera.main != null && Camera.main.TryGetComponent(out CameraFollow2D cam)) cam.SnapToTarget();
    }

    [ContextMenu("테스트: 지금 구역 건너뛰기")]
    private void TestSkip()
    {
        if (!Application.isPlaying || current < 0) return;
        HandleCleared(zones[current].board);
    }

#if UNITY_EDITOR
    public void EditorSetupEnding(BastetEnding e) => ending = e;

    public void EditorSetup(SobekBoard[] boards, string[][] lines)
    {
        zones = new Zone[boards.Length];
        for (int i = 0; i < boards.Length; i++) zones[i] = new Zone { board = boards[i], lines = i < lines.Length ? lines[i] : null };
    }
#endif
}
