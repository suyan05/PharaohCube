using UnityEngine;
using UnityEngine.Rendering.Universal;

// 제단 잠금 + 불빛 제어 (제단 오브젝트에 AnubisAltarStation과 함께 부착)
// - 잠김: 제단의 Light 2D 꺼짐, 그림 어둡게, E를 누르면 봉인 안내
// - requiredItem을 얻는 순간 자동 해금: 불빛 켜짐 + 안내 메시지
public class AnubisAltarLock : MonoBehaviour
{
    [Header("해금 조건")]
    [Tooltip("체크하면 항상 잠김 (튜토리얼의 메인 제단)")]
    [SerializeField] private bool alwaysLocked = false;
    [Tooltip("이 아이템이 있으면 열림. 비우면 처음부터 열림\n예) A2 = ITEM_ANUBIS_GOLD_JAR (A1 보상)")]
    [SerializeField] private string requiredItem = "";

    [Header("메시지")]
    [TextArea(1, 3)]
    [SerializeField] private string lockedMessage = "제단이 봉인되어 있습니다.\n앞선 시험을 먼저 통과하십시오.";
    [TextArea(1, 3)]
    [Tooltip("해금되는 순간 표시 (비우면 표시 안 함)")]
    [SerializeField] private string unlockMessage = "";

    [Header("불빛 / 연출")]
    [Tooltip("이 오브젝트(자식 포함)의 Light 2D를 잠금 상태에 맞춰 켜고 끔")]
    [SerializeField] private bool controlOwnLights = true;
    [Tooltip("해금되면 함께 켤 오브젝트 (방 조명 등, 비워도 됨)")]
    [SerializeField] private GameObject[] extraOnWhenUnlocked;
    [Tooltip("잠겨 있을 때 제단 그림 색")]
    [SerializeField] private Color lockedTint = new Color(0.45f, 0.45f, 0.5f, 1f);

    private Light2D[] lights;
    private SpriteRenderer[] sprites;
    private Color[] spriteColors;
    private bool unlocked;
    private bool subscribed;

    public bool IsLocked =>
        alwaysLocked ||
        (!string.IsNullOrEmpty(requiredItem) && (ItemInventory.Instance == null || !ItemInventory.Instance.Has(requiredItem)));

    private void Awake()
    {
        lights = GetComponentsInChildren<Light2D>(true);
        sprites = GetComponentsInChildren<SpriteRenderer>(true);
        spriteColors = new Color[sprites.Length];
        for (int i = 0; i < sprites.Length; i++) spriteColors[i] = sprites[i].color;
    }

    private void Start()
    {
        if (ItemInventory.Instance != null)
        {
            ItemInventory.Instance.OnItemGranted += HandleItemGranted;
            subscribed = true;
        }
        else if (!string.IsNullOrEmpty(requiredItem))
        {
            Debug.LogError($"[{name}] ItemInventory가 씬에 없어 해금할 수 없음 (Managers 확인)", this);
        }

        unlocked = !IsLocked;
        ApplyState(false);
    }

    private void OnDestroy()
    {
        if (subscribed && ItemInventory.Instance != null)
            ItemInventory.Instance.OnItemGranted -= HandleItemGranted;
    }

    // 아이템을 얻을 때마다 확인
    private void HandleItemGranted(string itemId)
    {
        if (unlocked || IsLocked) return;
        unlocked = true;
        ApplyState(true);
        Debug.Log($"[{name}] 해금됨 ({itemId})", this);
    }

    private void ApplyState(bool announce)
    {
        bool on = !IsLocked;

        if (controlOwnLights)
            foreach (var l in lights) if (l != null) l.enabled = on;

        if (extraOnWhenUnlocked != null)
            foreach (var go in extraOnWhenUnlocked) if (go != null) go.SetActive(on);

        for (int i = 0; i < sprites.Length; i++)
            if (sprites[i] != null) sprites[i].color = on ? spriteColors[i] : spriteColors[i] * lockedTint;

        if (on && announce) AnubisToast.Show(unlockMessage, 4f);
    }

    // AnubisAltarStation이 잠긴 상태에서 E를 누르면 호출
    public void NotifyLocked() => AnubisToast.Show(lockedMessage);

    [ContextMenu("테스트: 강제 해금")]
    private void TestUnlock()
    {
        alwaysLocked = false;
        requiredItem = "";
        unlocked = true;
        ApplyState(true);
    }

    // Scene 뷰: 잠금 = 빨강 / 열림 = 초록
    private void OnDrawGizmos()
    {
        bool locked = alwaysLocked || !string.IsNullOrEmpty(requiredItem);
        if (Application.isPlaying) locked = IsLocked;
        Gizmos.color = locked ? new Color(1f, 0.25f, 0.2f, 0.9f) : new Color(0.3f, 1f, 0.4f, 0.9f);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.8f, 0.25f);
    }
}