using System.Collections;
using UnityEngine;

// 허브 진행 상태 (씬 시작 때 한 번 확인)
// - 메인을 깼으면(F_SOBEK_MAIN_DONE): 큰 문 앞에 플레이어를 두고, 신전의 물이 빠지는 연출 1번 -> 아누비스 문 열림
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
    [SerializeField] private string drainMessage = "신전의 물이 빠진다... 물 밑에 숨어 있던 아누비스의 문이 드러났다.";

    private static bool HasFlag(string f) => GameManager.Instance != null && GameManager.Instance.HasFlag(f);

    private void Start()
    {
        if (cameraFollow == null && Camera.main != null) cameraFollow = Camera.main.GetComponent<CameraFollow2D>();

        bool done = HasFlag(SobekIds.FlagMainDone);
        SetDrained(false, 1f);
        if (!done) return;

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
        Debug.Log("[SobekHubState] 물 빠짐 -> 아누비스 문 열림", this);
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
