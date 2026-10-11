using UnityEngine;
using UnityEngine.SceneManagement;

// 플레이어가 닿으면 다음 씬으로 이동 (튜토리얼 → 실전 아누비스 방)
// Collider2D는 Is Trigger 체크 필수 (컴포넌트를 처음 붙일 때 자동 설정됨)
[RequireComponent(typeof(Collider2D))]
public class AnubisSceneExit : MonoBehaviour
{
    [Header("이동할 씬")]
    [Tooltip("Build Profiles의 Scene List에 등록된 씬 이름")]
    [SerializeField] private string nextSceneName = "Puzzle_Test_Anubis_Tilemap";
    [SerializeField] private string playerTag = "Player";

    [Header("연출")]
    [TextArea(1, 3)]
    [SerializeField] private string exitMessage = "진짜 심판의 신전으로 내려갑니다...";
    [Range(0f, 3f)]
    [SerializeField] private float loadDelay = 1.5f;

    private bool used;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (used || !other.CompareTag(playerTag)) return;

        if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            Debug.LogError($"[{name}] '{nextSceneName}' 씬이 Build Profiles의 Scene List에 없음", this);
            return;
        }

        used = true;
        AnubisToast.Show(exitMessage, loadDelay + 1f);
        Invoke(nameof(LoadNext), loadDelay);
    }

    private void LoadNext() => SceneManager.LoadScene(nextSceneName);

    // Scene 뷰: 출구 범위 (하늘색)
    private void OnDrawGizmos()
    {
        var col = GetComponent<BoxCollider2D>();
        if (col == null) return;
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
        Gizmos.DrawWireCube(transform.position + (Vector3)col.offset, col.size);
    }
}