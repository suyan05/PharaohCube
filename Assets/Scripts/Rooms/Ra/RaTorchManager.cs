using System;
using System.Collections;
using UnityEngine;

// 라의 방 횃불 전체 관리 (씬에 1개)
// - 첫 벽 횃불을 붙이면 F_RA_FIRST_TORCH_LIT (목표 문구 다음 단계로) + 짧은 대사
// - 밤이 되는 순간(F_RA_NIGHT_OPEN) 연출:
//   라의 대화가 끝나길 기다림 -> 검은 바 + 카메라가 넓게 물러남
//   -> 플레이어 가까운 횃불부터 차례로 깜빡이다 꺼짐 -> 쿵 -> "..." -> 혼잣말
//   -> (배 모형 글귀를 읽었고 불씨가 없으면) 불씨 힌트 혼잣말 한 줄 더
//   -> 라가 먼저 밤의 대사를 건넴 (성소에 다시 갈 필요 없음, F_RA_SHRINE_NIGHT_TALKED)
// - 이미 밤인 상태로 씬에 들어오면 WallTorch가 알아서 밤 기준으로 복원하므로 여기서는 연출만
public class RaTorchManager : MonoBehaviour
{
    // GameMessageUI가 이 말머리로 시작하는 로그를 오른쪽 위 알림으로 띄움
    private const string MessageTag = "[Door] ";

    [SerializeField] private string firstTorchFlag = "F_RA_FIRST_TORCH_LIT";
    [SerializeField] private string nightFlag = "F_RA_NIGHT_OPEN";
    [SerializeField, Min(0f)] private float delayBeforeOut = 1.0f;
    [SerializeField, Min(0f)] private float gapBetween = 0.28f;
    [SerializeField, Min(1f)] private float nightViewSize = 7f;

    [SerializeField] private string nightTalkedFlag = "F_RA_SHRINE_NIGHT_TALKED";

    [Header("불씨 힌트 (배 모형 글귀를 읽었을 때만)")]
    [SerializeField] private string boatSeenFlag = "F_RA_BOAT_SEEN";
    [SerializeField] private string emberItemId = "ITEM_RA_EMBER";

    [Header("대사, 알림")]
    [SerializeField] private DialogueLine[] firstTorchLines =
    {
        new DialogueLine("나", "좋아, 방이 밝아졌어."),
        new DialogueLine("나", "다른 방에 들어가면 벽 횃불부터 밝히자.")
    };
    [Tooltip("비우면 알림 없음 (라의 성소가 이미 밤 알림을 띄워서 기본은 비움)")]
    [SerializeField] private string nightMessage = "";
    [SerializeField] private DialogueLine[] nightLines = { new DialogueLine("나", "불이... 전부 꺼졌어. 다시 밝히며 가야겠다.") };

    private bool subscribed;
    private RaDialogue raDialogue;

    private void OnEnable()
    {
        WallTorch.PlayerLit += HandlePlayerLit;
    }

    private void OnDisable()
    {
        WallTorch.PlayerLit -= HandlePlayerLit;
    }

    private void Start()
    {
        raDialogue = FindFirstObjectByType<RaDialogue>(FindObjectsInactive.Include);
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnFlagChanged += HandleFlag;
        subscribed = true;
    }

    private void OnDestroy()
    {
        if (subscribed && GameManager.Instance != null) GameManager.Instance.OnFlagChanged -= HandleFlag;
    }

    // ================= 첫 횃불 =================
    private void HandlePlayerLit(WallTorch torch)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.HasFlag(firstTorchFlag)) return;

        gm.SetFlag(firstTorchFlag);
        StartCoroutine(FirstTorch(torch));
    }

    // 불이 붙은 횃불을 잠깐 보여주며 방 전체가 밝아진 걸 확인시킴
    private IEnumerator FirstTorch(WallTorch torch)
    {
        yield return new WaitForSeconds(0.6f);
        Cine.Begin(false);
        if (torch != null) CineCamera.Focus(torch.transform.position + Vector3.down * 1.5f);
        CineCamera.Zoom(5.5f, 1.4f);
        yield return Cine.Wait(1.2f);
        yield return Cine.Say(firstTorchLines);
        Cine.End();
    }

    // ================= 밤: 전부 꺼짐 =================
    private void HandleFlag(string flag)
    {
        if (flag == nightFlag) StartCoroutine(NightFalls());
    }

    private IEnumerator NightFalls()
    {
        yield return WaitUntilFree();
        Transform player = CineActor.Player();
        Cine.Begin(false);
        if (player != null) CineCamera.Focus(player.position);
        CineCamera.Zoom(nightViewSize, 1.6f);
        yield return Cine.Wait(0.8f);

        float total = ExtinguishAll(player);
        yield return Cine.Wait(total + 0.5f);
        CineCamera.Shake(0.35f, 0.05f);
        yield return Cine.Wait(0.9f);

        yield return NightTalk(player);
        Cine.End();

        yield return new WaitForSeconds(0.4f);
        AutoRaNightTalk(player);
    }

    // 라의 밤 대사 자동 (성소 단계가 "밤"일 때 Interact = 밤 대사 + 들었음 표시)
    private void AutoRaNightTalk(Transform player)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.HasFlag(nightTalkedFlag) || player == null) return;
        RaShrine shrine = FindFirstObjectByType<RaShrine>(FindObjectsInactive.Include);
        if (shrine != null) shrine.Interact(player.gameObject);
    }

    private IEnumerator NightTalk(Transform player)
    {
        if (player != null) yield return EmoteBalloonUI.Show(player, "...", 0.9f);
        yield return Cine.Say(nightLines);
        if (ShouldHintEmber()) yield return Cine.Say(RaSunBoatLines.NightHint);
        if (!string.IsNullOrEmpty(nightMessage)) Debug.Log(MessageTag + nightMessage);
    }

    // 라의 대화창, 다른 연출이 끝날 때까지 (최대 10초)
    private IEnumerator WaitUntilFree()
    {
        float t = 0f;
        while (t < 10f && (Cine.Running || DialogueBox.IsOpen || (raDialogue != null && raDialogue.IsPlaying)))
        {
            t += Time.deltaTime;
            yield return null;
        }
        yield return new WaitForSeconds(0.5f);
    }

    // 가까운 것부터 차례로 끔. 마지막 횃불이 꺼지는 데 걸리는 시간을 돌려줌
    private float ExtinguishAll(Transform player)
    {
        WallTorch[] torches = FindObjectsByType<WallTorch>(FindObjectsSortMode.None);
        Vector3 from = player != null ? player.position : Vector3.zero;
        Array.Sort(torches, (a, b) => Dist(a, from).CompareTo(Dist(b, from)));

        float delay = delayBeforeOut;
        foreach (WallTorch torch in torches)
        {
            if (!torch.IsLit) continue;
            StartCoroutine(torch.Extinguish(delay));
            delay += gapBetween;
        }
        return delay + 0.5f;
    }

    private bool ShouldHintEmber()
    {
        GameManager gm = GameManager.Instance;
        bool seen = gm != null && gm.HasFlag(boatSeenFlag);
        bool has = ItemInventory.Instance != null && ItemInventory.Instance.Has(emberItemId);
        return seen && !has;
    }

    private static float Dist(WallTorch t, Vector3 from)
    {
        return (t.transform.position - from).sqrMagnitude;
    }
}
