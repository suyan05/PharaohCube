using System;
using System.Collections;
using UnityEngine;

// 라의 방 횃불 전체 관리 (씬에 1개)
// - 첫 벽 횃불을 붙이면 F_RA_FIRST_TORCH_LIT (목표 문구 다음 단계로) + 짧은 대사
// - 밤이 되는 순간(F_RA_NIGHT_OPEN): 플레이어 가까운 횃불부터 차례로 깜빡이다 꺼짐 + 알림
//   (이미 밤인 상태로 씬에 들어오면 WallTorch가 알아서 밤 기준으로 복원하므로 여기서는 연출만)
public class RaTorchManager : MonoBehaviour
{
    // GameMessageUI가 이 말머리로 시작하는 로그를 오른쪽 위 알림으로 띄움
    private const string MessageTag = "[Door] ";

    [SerializeField] private string firstTorchFlag = "F_RA_FIRST_TORCH_LIT";
    [SerializeField] private string nightFlag = "F_RA_NIGHT_OPEN";
    [SerializeField, Min(0f)] private float delayBeforeOut = 1.2f;
    [SerializeField, Min(0f)] private float gapBetween = 0.18f;

    [Header("대사, 알림")]
    [SerializeField] private DialogueLine[] firstTorchLines =
    {
        new DialogueLine("나", "좋아, 방이 밝아졌어."),
        new DialogueLine("나", "다른 방에 들어가면 벽 횃불부터 밝히자.")
    };
    [SerializeField] private string nightMessage = "해가 지자 신전의 횃불이 모두 꺼졌다";
    [SerializeField] private DialogueLine[] nightLines = { new DialogueLine("나", "불이... 전부 꺼졌어. 다시 밝히며 가야겠다.") };

    private bool subscribed;

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
        StartCoroutine(Cine.Say(firstTorchLines));
    }

    // ================= 밤: 전부 꺼짐 =================
    private void HandleFlag(string flag)
    {
        if (flag == nightFlag) StartCoroutine(AllOut());
    }

    private IEnumerator AllOut()
    {
        WallTorch[] torches = FindObjectsByType<WallTorch>(FindObjectsSortMode.None);
        Vector3 from = PlayerPos();
        Array.Sort(torches, (a, b) => Dist(a, from).CompareTo(Dist(b, from)));

        float delay = delayBeforeOut;
        foreach (WallTorch torch in torches)
        {
            if (!torch.IsLit) continue;
            StartCoroutine(torch.Extinguish(delay));
            delay += gapBetween;
        }

        yield return new WaitForSeconds(delay + 0.5f);
        CineCamera.Shake(0.3f, 0.04f);
        Debug.Log(MessageTag + nightMessage);
        if (!Cine.Running && !DialogueBox.IsOpen) yield return Cine.Say(nightLines);
    }

    private static Vector3 PlayerPos()
    {
        Transform p = CineActor.Player();
        return p != null ? p.position : Vector3.zero;
    }

    private static float Dist(WallTorch t, Vector3 from)
    {
        return (t.transform.position - from).sqrMagnitude;
    }
}
