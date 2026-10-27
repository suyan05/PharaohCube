using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 바스테트 서브 방 입구 (봉인석 + 메인 방 바닥 문양)
// [수정] 자동으로 열리지 않음: 플레이어가 봉인석 앞에서 E -> 필요한 아이템을 넣으면(인벤토리에서 사라짐) 열림
// - 열리면 화면이 흔들리고 봉인석이 가라앉음 + 열림 플래그(F_BASTET_GATE_<방>_OPEN) 저장 -> 씬에 다시 들어와도 열린 상태
// - 바닥 문양 색: 잠김 회색 -> 넣을 아이템을 가짐/열림 연두 -> 그 방 클리어 금
// 붙이는 곳: SealStone 오브젝트 (단단한 BoxCollider2D가 길을 막고, E 대상이 됨)
// 처음부터 열린 방(방 1)은 Required Item Id를 비우면 시작하자마자 열림
[DisallowMultipleComponent]
public class BastetRoomGate : MonoBehaviour, IInteractable
{
    private enum GlyphState { Locked, Next, Cleared }

    [Header("방 정보")]
    [Tooltip("안내 문구에 쓰는 방 이름")]
    [SerializeField] private string roomName = "발자국의 방";
    [Tooltip("봉인석에 넣을 아이템 (비우면 처음부터 열림). 넣으면 인벤토리에서 사라짐")]
    [SerializeField] private string requiredItemId = "";
    [Tooltip("이 방 클리어 표시 플래그 (BastetRoomProgress가 켬, 예: F_BASTET_B2_CLEAR) -> 문양 금색")]
    [SerializeField] private string clearFlag = "";
    [Tooltip("넣은 아이템을 없앨지 (끄면 보여 주기만 하고 남김)")]
    [SerializeField] private bool consumeItem = true;

    [Header("봉인석 (이 오브젝트)")]
    [SerializeField] private Collider2D blocker;
    [SerializeField] private SpriteRenderer stoneRenderer;
    [Range(0.1f, 3f)] [SerializeField] private float openFadeTime = 1.0f;
    [TextArea(1, 2)]
    [SerializeField] private string lockedMessage = "봉인석에 고양이 문양이 파여 있다. 무언가를 끼워 넣어야 할 것 같다.";
    [TextArea(1, 2)]
    [SerializeField] private string insertMessage = "{0}을(를) 봉인석에 끼워 넣었다. 구르릉... 봉인석이 가라앉는다.";

    [Header("화면 흔들림")]
    [SerializeField] private CameraFollow2D cameraFollow;
    [Range(0f, 2f)] [SerializeField] private float shakeDuration = 0.6f;
    [Range(0f, 0.5f)] [SerializeField] private float shakeStrength = 0.15f;

    [Header("바닥 문양 (메인 방, 선택)")]
    [SerializeField] private SpriteRenderer glyph;
    [SerializeField] private Light2D glyphLight;
    [SerializeField] private Color lockedColor = new Color(0.55f, 0.52f, 0.48f);   // #8C857A
    [SerializeField] private Color nextColor = new Color(0.71f, 0.76f, 0.29f);     // #B5C24A
    [SerializeField] private Color clearedColor = new Color(0.91f, 0.70f, 0.23f);  // #E8B23A

    private bool opened;
    private bool subscribed;

    public bool IsOpen => opened;
    private string OpenFlag => $"F_BASTET_GATE_{name}_OPEN";

    // ================= 초기화 =================
    private void Awake()
    {
        if (blocker == null) blocker = GetComponent<Collider2D>();
        if (stoneRenderer == null) stoneRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        if (cameraFollow == null && Camera.main != null) cameraFollow = Camera.main.GetComponent<CameraFollow2D>();
        Subscribe();
        // 처음부터 열린 방 / 이미 넣은 적이 있는 문은 연출 없이 바로 열림
        if (string.IsNullOrEmpty(requiredItemId) || HasFlag(OpenFlag)) OpenNow(false);
        RefreshGlyph();
    }

    private void OnEnable() { if (ItemInventory.Instance != null) Subscribe(); }

    private void OnDisable()
    {
        if (subscribed && ItemInventory.Instance != null) ItemInventory.Instance.OnItemGranted -= HandleItem;
        if (subscribed && GameManager.Instance != null) GameManager.Instance.OnFlagChanged -= HandleFlag;
        subscribed = false;
    }

    private void Subscribe()
    {
        if (subscribed || ItemInventory.Instance == null) return;
        ItemInventory.Instance.OnItemGranted += HandleItem;
        if (GameManager.Instance != null) GameManager.Instance.OnFlagChanged += HandleFlag;
        subscribed = true;
    }

    private static bool Has(string id) => !string.IsNullOrEmpty(id) && ItemInventory.Instance != null && ItemInventory.Instance.Has(id);
    private static bool HasFlag(string f) => !string.IsNullOrEmpty(f) && GameManager.Instance != null && GameManager.Instance.HasFlag(f);

    // 아이템을 얻거나 플래그가 바뀌면 문양 색만 갱신 (문은 플레이어가 직접 열어야 함)
    private void HandleItem(string itemId) { if (itemId == requiredItemId) RefreshGlyph(); }
    private void HandleFlag(string flag) { if (flag == clearFlag) RefreshGlyph(); }

    // ================= IInteractable =================
    public string GetPrompt()
    {
        if (opened) return "";
        return Has(requiredItemId) ? $"E: {ItemName(requiredItemId)} 넣기" : "E: 봉인석 살펴보기";
    }

    public void Interact(GameObject player)
    {
        if (opened) return;
        if (!Has(requiredItemId))
        {
            AnubisToast.Show($"{lockedMessage}\n(필요: {ItemName(requiredItemId)})", 3f);
            Debug.Log($"[BastetRoomGate] {roomName} 잠김: {requiredItemId} 없음", this);
            return;
        }

        // 아이템 넣기 -> 사라짐
        if (consumeItem && ItemInventory.Instance != null) ItemInventory.Instance.Consume(requiredItemId);
        if (GameManager.Instance != null) GameManager.Instance.SetFlag(OpenFlag);
        AnubisToast.Show(string.Format(insertMessage, ItemName(requiredItemId)), 3f);
        Debug.Log($"[BastetRoomGate] {roomName}: {requiredItemId} 넣음 -> 열림 (아이템 {(consumeItem ? "사라짐" : "유지")})", this);
        BastetSfx.Gate(); // [추가] 봉인석이 가라앉는 쿠르릉
        OpenNow(true);
        RefreshGlyph();
    }

    // ================= 열기 =================
    private void OpenNow(bool withFx)
    {
        if (opened) return;
        opened = true;
        if (blocker != null) blocker.enabled = false;
        if (withFx && stoneRenderer != null && gameObject.activeInHierarchy)
        {
            if (cameraFollow != null) cameraFollow.Shake(shakeDuration, shakeStrength);
            StartCoroutine(FadeStone());
        }
        else if (stoneRenderer != null) stoneRenderer.enabled = false;
    }

    private IEnumerator FadeStone()
    {
        Color c = stoneRenderer.color;
        Vector3 basePos = transform.localPosition;
        float t = 0f;
        while (t < openFadeTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / openFadeTime);
            c.a = 1f - k;
            stoneRenderer.color = c;
            transform.localPosition = basePos + new Vector3(Mathf.Sin(t * 50f) * 0.04f * (1f - k), -0.3f * k, 0f);
            yield return null;
        }
        transform.localPosition = basePos;
        stoneRenderer.enabled = false;
    }

    private void RefreshGlyph()
    {
        GlyphState s = HasFlag(clearFlag) ? GlyphState.Cleared
                     : (opened || Has(requiredItemId)) ? GlyphState.Next : GlyphState.Locked;
        Color c = s == GlyphState.Cleared ? clearedColor : (s == GlyphState.Next ? nextColor : lockedColor);
        if (glyph != null) glyph.color = c;
        if (glyphLight != null)
        {
            glyphLight.enabled = s != GlyphState.Locked;
            glyphLight.color = c;
        }
    }

    // 팀 ItemCatalog의 이름 (없으면 ID)
    private static string ItemName(string id)
    {
        if (string.IsNullOrEmpty(id)) return "";
        return ItemCatalog.Main.TryGet(id, out ItemDefinition def) ? def.displayName : id;
    }

    // ================= 테스트 =================
    [ContextMenu("테스트: 넣을 아이템 받기")]
    private void TestGrantRequired()
    {
        if (Application.isPlaying && ItemInventory.Instance != null && !string.IsNullOrEmpty(requiredItemId))
            ItemInventory.Instance.Grant(requiredItemId);
    }

    [ContextMenu("테스트: 바로 열기 (아이템 없이)")]
    private void TestOpen()
    {
        if (!Application.isPlaying) return;
        OpenNow(true);
        RefreshGlyph();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = opened ? new Color(0.91f, 0.7f, 0.23f) : new Color(0.55f, 0.52f, 0.48f);
        Gizmos.DrawWireCube(transform.position, new Vector3(1f, 1.5f, 0f));
        if (glyph != null) Gizmos.DrawLine(transform.position, glyph.transform.position);
    }

#if UNITY_EDITOR
    public void EditorSetup(string room, string required, string clear, SpriteRenderer g, Light2D gl)
    {
        roomName = room; requiredItemId = required; clearFlag = clear; glyph = g; glyphLight = gl;
    }
#endif
}
