using System.Collections;
using UnityEngine;

// 메인 저울 통과를 플래그로 남김 (목표 안내, 힌트 버튼이 이 플래그를 봄)
// - AnubisPuzzleManager2D는 플래그를 쓰지 않아서, OnPuzzleCleared 이벤트를 받아 대신 켜 줌
// - [추가] 통과하면 아누비스 일러스트(Illustration/Anubis.png) -> 자칼 형판(PLATE_ANUBIS) 획득 (중앙 홀 큐브 여섯 번째 칸)
public class AnubisRoomProgress : MonoBehaviour
{
    [Header("연결 (비우면 씬에서 자동으로 찾음)")]
    [SerializeField] private AnubisPuzzleManager2D mainScale;

    [Header("플래그")]
    [SerializeField] private string mainClearFlag = AnubisObjectiveGuide.FlagMainDone;

    [Header("통과 알림 (비우면 없음)")]
    [TextArea(1, 3)]
    [SerializeField] private string clearToast = "심판을 통과했다! 영혼이 아알루로 향한다.";

    [Header("[추가] 보상")]
    [SerializeField] private string plateItemId = "PLATE_ANUBIS";
    [SerializeField] private int illustrationIndex = 5; // GodIllustration: 5 아누비스

    private bool subscribed;

    private void Start()
    {
        if (mainScale == null) mainScale = FindFirstObjectByType<AnubisPuzzleManager2D>(FindObjectsInactive.Include);
        if (mainScale == null)
        {
            Debug.LogWarning($"[{name}] AnubisPuzzleManager2D를 못 찾음 -> 메인 클리어 플래그를 못 켬", this);
            return;
        }
        mainScale.OnPuzzleCleared += HandleCleared;
        subscribed = true;
    }

    private void OnDestroy()
    {
        if (subscribed && mainScale != null) mainScale.OnPuzzleCleared -= HandleCleared;
    }

    private void HandleCleared()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogWarning($"[{name}] GameManager가 없어 '{mainClearFlag}'를 못 켬", this);
            return;
        }
        if (!gm.HasFlag(mainClearFlag)) gm.SetFlag(mainClearFlag);
        AnubisToast.Show(clearToast, 4f);
        StartCoroutine(RewardRoutine());
    }

    // [추가] 일러스트 -> 형판 (이미 있으면 건너뜀)
    private IEnumerator RewardRoutine()
    {
        yield return new WaitForSecondsRealtime(1.5f); // 통과 문구를 먼저 보여 줌
        yield return GodIllustration.Play(illustrationIndex);
        if (ItemInventory.Instance == null || ItemInventory.Instance.Has(plateItemId)) yield break;
        bool closed = false;
        ItemPopup.Grant(plateItemId, () => closed = true);
        while (!closed) yield return null;
    }

    [ContextMenu("테스트: 메인 저울 통과 처리 (Play 중)")]
    private void TestClear()
    {
        if (Application.isPlaying) HandleCleared();
    }
}
