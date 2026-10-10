using UnityEngine;

// 테스트용: 씬 아무 오브젝트에 붙이고
// - 플레이 중 F9: 밤으로 전환
// - 플레이 중 F10: 지금 낮/밤 출력
// - 또는 컴포넌트 제목줄 우클릭 메뉴로도 가능
// 에디터와 개발용 빌드에서만 키가 동작 (정식 빌드에서는 무시)
public class RaPhaseDebug : MonoBehaviour
{
    [Header("테스트 키")]
    [SerializeField] private KeyCode nightKey = KeyCode.F9;
    [SerializeField] private KeyCode printKey = KeyCode.F10;

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
        if (GameManager.Instance == null)
        {
            Debug.LogError("[RaPhaseDebug] GameManager가 없음 (플레이 중에만 사용)");
            return;
        }

        if (RaPhaseState.IsNight)
        {
            Debug.Log("[RaPhaseDebug] 이미 밤이야");
            return;
        }

        Debug.Log("[RaPhaseDebug] 밤으로 전환!");
        GameManager.Instance.SetFlag(RaPhaseState.NightFlag);
    }

    [ContextMenu("테스트: 지금 낮/밤 출력")]
    private void PrintPhase()
    {
        Debug.Log($"[RaPhaseDebug] 지금은 {RaPhaseState.Current}");
    }
}