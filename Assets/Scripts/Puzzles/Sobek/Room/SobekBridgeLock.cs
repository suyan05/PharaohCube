using System.Collections;
using UnityEngine;

// [추가] 허브 섬 다리를 막은 통나무 (서브 퍼즐을 순서대로 풀게 함, 바스테트의 잠긴 제단과 같은 방식)
// - 앞 섬의 부적이 있어야 치울 수 있음: S1 부적 -> S2 길 / S2 부적 -> S3 길 / S3 부적 -> S4 길
// - 막힌 섬과 다리는 어둠(그림자)으로 덮여 안 보임 -> 통나무를 치우면 어둠이 걷히며 섬이 드러남
// - E: 부적이 없으면 안내, 있으면 부적이 빛나며 통나무가 물속으로 굴러 떨어짐 (화면 흔들림)
// - 저장: F_SOBEK_LOCK_{id}_OPEN (다시 들어와도 열린 채)
// - 부적은 쓰지 않고 그대로 가짐 (큰 문에 네 개를 넣어야 하므로)
// 붙이는 곳: Lock_S2 ~ Lock_S4 (Tools/Sobek/7 허브 보강 메뉴가 다리 안쪽 끝에 만듦). BoxCollider2D가 길을 막고 E 대상이 됨
[RequireComponent(typeof(BoxCollider2D))]
public class SobekBridgeLock : MonoBehaviour, IInteractable
{
    [Header("이 통나무가 막는 섬")]
    [SerializeField] private string lockId = "S2";
    [SerializeField] private string islandName = "악어 등 다리";

    [Header("열쇠 (앞 섬의 부적)")]
    [SerializeField] private string requiredItem = SobekIds.AmuletRotate;
    [Tooltip("부적이 인벤토리에 없어도 이 퍼즐을 이미 풀었으면 열 수 있음")]
    [SerializeField] private string requiredClearFlag = SobekIds.FlagS1Clear;
    [Tooltip("알림에 쓰는 이름 ('부적'으로 끝나게: 뒤에 '이', '으로'가 붙음)")]
    [SerializeField] private string keyName = "소용돌이 섬의 부적";

    [Header("그림")]
    [SerializeField] private SpriteRenderer logRenderer;
    [Tooltip("막힌 섬과 다리를 덮는 어둠 (따로 둔 오브젝트)")]
    [SerializeField] private SpriteRenderer fog;
    [Range(0f, 1f)] [SerializeField] private float fogAlpha = 0.92f;

    [Header("연출")]
    [Range(0.2f, 3f)] [SerializeField] private float rollTime = 0.9f;
    [Range(0.2f, 4f)] [SerializeField] private float fogFadeTime = 1.6f;
    [Tooltip("통나무가 굴러갈 방향 (다리 옆 물 쪽)")]
    [SerializeField] private Vector2 rollDirection = new Vector2(0f, -1.6f);
    [SerializeField] private CameraFollow2D cameraFollow;

    private BoxCollider2D box;
    private bool opening;
    private static bool hintShown; // 씬이 열릴 때마다 한 번만 안내

    public string FlagOpen => $"F_SOBEK_LOCK_{lockId}_OPEN";
    public bool IsOpen => SobekStory.HasFlag(FlagOpen);

    private bool HasKey =>
        (ItemInventory.Instance != null && !string.IsNullOrEmpty(requiredItem) && ItemInventory.Instance.Has(requiredItem))
        || SobekStory.HasFlag(requiredClearFlag);

    private void Awake()
    {
        box = GetComponent<BoxCollider2D>();
        if (logRenderer == null) logRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable() => hintShown = false;

    private void Start()
    {
        if (cameraFollow == null && Camera.main != null) cameraFollow = Camera.main.GetComponent<CameraFollow2D>();
        if (IsOpen) { SetOpenImmediate(); return; }
        SetFogAlpha(fogAlpha);
        if (fog != null) fog.enabled = true;
        Debug.Log($"[SobekBridgeLock] {lockId} 잠김 (열쇠: {requiredItem} 또는 {requiredClearFlag}, 지금 {(HasKey ? "열 수 있음" : "없음")})", this);
        if (HasKey) StartCoroutine(HintLater());
    }

    // 퍼즐을 풀고 허브로 돌아왔을 때 한 번 알려 줌
    private IEnumerator HintLater()
    {
        yield return new WaitForSeconds(1.2f);
        while (SobekStory.Busy || Cine.Running) yield return null;
        if (hintShown || IsOpen) yield break;
        hintShown = true;
        AnubisToast.Show($"{keyName}이 떨린다... {islandName} 쪽 다리의 통나무를 치울 수 있을 것 같다.", 3.5f);
    }

    public string GetPrompt()
    {
        if (opening || IsOpen) return "";
        return HasKey ? $"E: {keyName}으로 통나무 치우기" : "E: 다리를 막은 통나무 살펴보기";
    }

    public void Interact(GameObject player)
    {
        if (opening || IsOpen || SobekStory.Busy) return;
        if (!HasKey)
        {
            AnubisToast.Show($"굵은 통나무가 다리를 막고 있다. 너머는 어두워 보이지 않는다.\n{keyName}이 있으면 치울 수 있을 것 같다.", 3.2f);
            Debug.Log($"[SobekBridgeLock] {lockId}: 열쇠 없음 ({requiredItem})", this);
            return;
        }
        StartCoroutine(Open());
    }

    private IEnumerator Open()
    {
        opening = true;
        PlayerLock.Lock(this);
        AnubisToast.Show($"{keyName}이 빛나자, 통나무가 굴러 물속으로 떨어진다.", 2.5f);
        if (cameraFollow != null) cameraFollow.Shake(rollTime, 0.12f);
        GameAudio.Play("sfx_rumble", 0.5f);

        Vector3 from = transform.position, to = from + (Vector3)rollDirection;
        Quaternion r0 = transform.rotation;
        Color c0 = logRenderer != null ? logRenderer.color : Color.white;
        for (float t = 0f; t < rollTime; t += Time.deltaTime)
        {
            float e = t / rollTime;
            transform.position = Vector3.Lerp(from, to, e * e);                       // 점점 빨리 굴러감
            transform.rotation = r0 * Quaternion.Euler(0f, 0f, -200f * e * Mathf.Sign(rollDirection.y == 0 ? 1f : rollDirection.y));
            if (logRenderer != null) logRenderer.color = new Color(c0.r, c0.g, c0.b, c0.a * (1f - Mathf.Clamp01((e - 0.6f) / 0.4f)));
            yield return null;
        }
        if (logRenderer != null) logRenderer.enabled = false;
        if (box != null) box.enabled = false;
        SobekStory.SetFlag(FlagOpen);

        // 어둠이 걷히며 섬이 드러남
        for (float t = 0f; t < fogFadeTime; t += Time.deltaTime)
        {
            SetFogAlpha(fogAlpha * (1f - t / fogFadeTime));
            yield return null;
        }
        if (fog != null) fog.enabled = false;
        PlayerLock.Unlock(this);
        opening = false;
        AnubisToast.Show($"{islandName} 쪽 길이 열렸다.", 2.5f);
        Debug.Log($"[SobekBridgeLock] {lockId} 열림 -> {islandName} 드러남", this);
    }

    private void SetOpenImmediate()
    {
        if (logRenderer != null) logRenderer.enabled = false;
        if (box != null) box.enabled = false;
        if (fog != null) fog.enabled = false;
    }

    private void SetFogAlpha(float a)
    {
        if (fog == null) return;
        Color c = fog.color; c.a = a; fog.color = c;
    }

    private void OnDisable()
    {
        if (opening) PlayerLock.Unlock(this);
        opening = false;
    }

    [ContextMenu("테스트: 지금 열기 (열쇠 무시)")]
    private void TestOpen()
    {
        if (Application.isPlaying && !IsOpen && !opening) StartCoroutine(Open());
    }

    [ContextMenu("테스트: 다시 잠그기 (플래그는 그대로, 이번 실행만)")]
    private void TestRelock()
    {
        if (!Application.isPlaying) return;
        if (logRenderer != null) { logRenderer.enabled = true; logRenderer.color = Color.white; }
        if (box != null) box.enabled = true;
        if (fog != null) { fog.enabled = true; SetFogAlpha(fogAlpha); }
    }

    // Scene 뷰: 막는 자리(주황) + 어둠 범위(회색)
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.9f);
        var b = GetComponent<BoxCollider2D>();
        if (b != null) Gizmos.DrawWireCube(transform.position + (Vector3)b.offset, b.size);
        if (fog != null)
        {
            Gizmos.color = new Color(0.4f, 0.4f, 0.5f, 0.6f);
            Gizmos.DrawWireCube(fog.bounds.center, fog.bounds.size);
        }
    }

#if UNITY_EDITOR
    public void EditorSetup(string id, string island, string item, string clearFlag, string key, SpriteRenderer log, SpriteRenderer fogRenderer, Vector2 roll)
    {
        lockId = id; islandName = island; requiredItem = item; requiredClearFlag = clearFlag; keyName = key;
        logRenderer = log; fog = fogRenderer; rollDirection = roll;
    }
#endif
}
