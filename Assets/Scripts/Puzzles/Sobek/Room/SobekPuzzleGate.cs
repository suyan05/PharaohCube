using System.Collections;
using TMPro;
using UnityEngine;

// 허브 섬 위의 퍼즐 게이트 (E: 들어가기 -> 착시 퍼즐 씬)
// - 퍼즐 씬에서 돌아오면 같은 게이트 앞(spawnPoint)에 다시 섬
// - 푼 퍼즐은 이름 옆에 ✓ (다시 들어가서 또 풀 수 있음)
// 붙이는 곳: 허브의 Gate_S1 ~ Gate_S4 (Collider2D = E 대상)
[RequireComponent(typeof(Collider2D))]
public class SobekPuzzleGate : MonoBehaviour, IInteractable
{
    [Tooltip("돌아올 때 찾는 이름 (씬마다 하나)")]
    [SerializeField] private string gateId = "S1";
    [SerializeField] private string sceneName = "Sobek_S1";
    [SerializeField] private string displayName = "소용돌이 섬";
    [Tooltip("이 퍼즐을 풀면 켜지는 플래그 (✓ 표시용)")]
    [SerializeField] private string clearFlag = SobekIds.FlagS1Clear;
    [Tooltip("돌아왔을 때 설 자리")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private TextMeshPro label;
    [Range(0.1f, 3f)] [SerializeField] private float fadeTime = 0.7f;

    // 퍼즐 씬에서 돌아올 게이트 (씬이 바뀌어도 남도록 static)
    private static string pendingReturn = "";
    public static void SetReturn(string id) => pendingReturn = id ?? "";

    private bool loading;

    private static bool HasFlag(string f) => !string.IsNullOrEmpty(f) && GameManager.Instance != null && GameManager.Instance.HasFlag(f);

    private void Start()
    {
        RefreshLabel();
        if (!string.IsNullOrEmpty(pendingReturn) && pendingReturn == gateId)
        {
            pendingReturn = "";
            StartCoroutine(PlaceBack());
        }
    }

    private IEnumerator PlaceBack()
    {
        yield return null; // 플레이어 Start 뒤
        var p = FindFirstObjectByType<PlayerController2D>();
        if (p == null || spawnPoint == null) yield break;
        var rb = p.GetComponent<Rigidbody2D>();
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.position = spawnPoint.position; }
        p.transform.position = new Vector3(spawnPoint.position.x, spawnPoint.position.y, p.transform.position.z);
        if (Camera.main != null && Camera.main.TryGetComponent(out CameraFollow2D cam)) cam.SnapToTarget();
        Debug.Log($"[SobekPuzzleGate] {displayName} 게이트 앞으로 돌아옴", this);
    }

    private void RefreshLabel()
    {
        if (label != null) label.text = HasFlag(clearFlag) ? $"{displayName} <color=#FFD27A>✓</color>" : displayName;
    }

    public string GetPrompt() => loading ? "" : HasFlag(clearFlag) ? $"E: {displayName} 다시 풀기" : $"E: {displayName} 들어가기";

    public void Interact(GameObject player)
    {
        if (loading) return;
        StartCoroutine(Load());
    }

    private IEnumerator Load()
    {
        loading = true;
        PlayerLock.Lock(this);
        AnubisToast.Show($"{displayName}… 섬의 문이 열린다.", fadeTime + 1f);
        SetReturn(gateId);
        yield return ScreenFader.FadeOut(fadeTime);
        PlayerLock.Unlock(this);
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
            Debug.LogWarning($"[SobekPuzzleGate] '{sceneName}' 씬이 Scene List에 없음 (Tools/Sobek/5 실행)", this);
        SceneFlow.LoadByNameOrNext(sceneName);
    }

    private void OnDestroy() => PlayerLock.Unlock(this);

#if UNITY_EDITOR
    public void EditorSetup(string id, string scene, string title, string flag, Transform spawn, TextMeshPro text)
    {
        gateId = id; sceneName = scene; displayName = title; clearFlag = flag; spawnPoint = spawn; label = text;
    }
#endif
}
