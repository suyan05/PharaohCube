using UnityEngine;

// 테스트용: 씬 아무 오브젝트에 붙이고
// - 플레이 중 F9: 밤으로 전환 (Simulate Day Clear가 켜져 있으면 낮을 다 깬 상태로 만든 뒤 밤)
// - 플레이 중 F10: 지금 낮/밤 출력
// - 또는 컴포넌트 제목줄 우클릭 메뉴로도 가능
// 에디터와 개발용 빌드에서만 키가 동작 (정식 빌드에서는 무시)
public class RaPhaseDebug : MonoBehaviour
{
    [Header("테스트 키")]
    [SerializeField] private KeyCode nightKey = KeyCode.F9;
    [SerializeField] private KeyCode printKey = KeyCode.F10;

    [Header("밤 전환 전에 낮 진행 플래그를 전부 켤지")]
    [Tooltip("켜면 F9 = 낮 P1~P5, 회로 4단계, 시작 연출, 성소 첫 대화까지 끝난 것으로 처리한 뒤 밤")]
    [SerializeField] private bool simulateDayClear = true;

    [Tooltip("Simulate Day Clear 때 켤 낮 플래그들")]
    [SerializeField]
    private string[] dayFlags =
    {
        "F_RA_INTRO_PLAYED", "F_RA_SHRINE_FIRST_TALKED", "F_RA_P2_TUTORIAL_SEEN",
        "F_RA_CIRCUIT_1", "F_RA_P1_DONE", "F_RA_CIRCUIT_2", "F_RA_P3_DONE",
        "F_RA_CIRCUIT_3", "F_RA_P4_DONE", "F_RA_CIRCUIT_4", "F_RA_P5_DONE"
    };

    private void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Input.GetKeyDown(nightKey)) SwitchToNight();
        if (Input.GetKeyDown(printKey)) PrintPhase();
#endif
    }

    [ContextMenu("테스트: 밤으로 전환 (F_RA_NIGHT_OPEN 켜기)")]
    private void SwitchToNight()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogError("[RaPhaseDebug] GameManager가 없음 (플레이 중에만 사용)");
            return;
        }

        if (RaPhaseState.IsNight)
        {
            Debug.Log("[RaPhaseDebug] 이미 밤이야");
            return;
        }

        if (simulateDayClear) SetDayFlags(gm);

        Debug.Log("[RaPhaseDebug] 밤으로 전환!");
        gm.SetFlag(RaPhaseState.NightFlag);
    }

    // 낮 진행 플래그를 순서대로 켬 (이미 켜진 건 건너뜀)
    private void SetDayFlags(GameManager gm)
    {
        if (dayFlags == null) return;

        foreach (string flag in dayFlags)
        {
            if (string.IsNullOrEmpty(flag) || gm.HasFlag(flag)) continue;
            gm.SetFlag(flag);
        }
        Debug.Log("[RaPhaseDebug] 낮 진행 플래그를 전부 켬 (낮 클리어 상태로 처리)");
    }

    [ContextMenu("테스트: 지금 낮/밤 출력")]
    private void PrintPhase()
    {
        Debug.Log($"[RaPhaseDebug] 지금은 {RaPhaseState.Current}");
    }
}