using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// [추가] 바스테트 숨은 방 "향실" 입구 (목걸이의 방 위쪽 벽의 틈)
// 1) 씬의 바스테트 비석(BastetClueStele: 입구 비석 + B1~B5)을 모두 해독하기 전에는 아무것도 안 보임 (그냥 벽)
//    - 비석을 하나 해독할 때마다 "해독한 비석 n / 6" 알림
// 2) 모두 해독하는 순간: 벽에 틈이 드러나며 빛이 새어 나옴 + "향 냄새가..." 알림 (F_BASTET_VAULT_OPEN)
// 3) E: 틈을 살펴봄 -> 옆모습 통로 "향실" -> 끝 방 벽화 컷 -> 히든 유물 "바스테트의 그릇" 획득
// 4) 다시 E: 통로를 다시 걸어볼 수 있음 (그릇은 한 번만 줌)
// 붙이는 곳: BastetVaultSpawner가 씬이 열릴 때 Room3_Necklace 위쪽 벽에 자동으로 만듦
//            (Tools/Bastet/숨은 방 입구 배치 메뉴로 씬에 저장해 두면 위치를 직접 옮길 수 있음)
[RequireComponent(typeof(BoxCollider2D))]
public class BastetIncenseVault : MonoBehaviour, IInteractable
{
    private const string GateArt = "Horus/PixelFx/SW_Gate"; // 사랑님 숨은 길 그림 재사용 (어두운 문틈)

    [Header("보상")]
    [SerializeField] private string rewardItemId = BastetItemIds.Vessel;

    [Header("모양")]
    [Tooltip("문틈 그림 크기 배율 (SW_Gate 64x124, 32 PPU 기준)")]
    [Range(0.3f, 1.5f)] [SerializeField] private float gateScale = 0.7f;
    [SerializeField] private Color glowColor = new Color(1f, 0.74f, 0.42f);
    [Range(0.5f, 4f)] [SerializeField] private float glowRadius = 2.2f;

    [Header("알림 문구")]
    [SerializeField] private string progressFormat = "해독한 비석 {0} / {1}";
    [SerializeField] private string openMessage = "어디선가 향 냄새가 흘러나온다. 목걸이의 방 벽을 살펴보자.";

    private SpriteRenderer sr;
    private Light2D glow;
    private BoxCollider2D box;
    private BastetClueStele[] steles;
    private bool open, busy;
    private int lastCount = -1;
    private float pollTimer, clock, reveal;

    public bool IsOpen => open;

    // ================= 준비 =================
    private void Awake()
    {
        try
        {
            box = GetComponent<BoxCollider2D>();
            box.isTrigger = true; // 조사용 (길은 안 막음)
            BuildVisual();
            Show(false, false);
        }
        catch (Exception e)
        {
            Debug.LogError($"[바스테트 향실] 준비 오류: {e}", this);
        }
    }

    private void Start()
    {
        steles = FindObjectsByType<BastetClueStele>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (steles.Length == 0) Debug.LogWarning("[바스테트 향실] 씬에 비석(BastetClueStele)이 없음 -> 입구가 열리지 않음", this);
        lastCount = DecodedCount();
        // 이미 열린 판(이어하기)이면 연출 없이 바로 보임
        if (HasFlag(BastetItemIds.FlagVaultOpen) || (steles.Length > 0 && lastCount >= steles.Length)) Show(true, false);
        Debug.Log($"[바스테트 향실] 준비: 비석 {lastCount} / {steles.Length} 해독, 입구 {(open ? "열림" : "숨김")}", this);
    }

    private void BuildVisual()
    {
        sr = gameObject.GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        Sprite gate = Resources.Load<Sprite>(GateArt);
        if (gate != null) sr.sprite = gate;
        else Debug.LogWarning($"[바스테트 향실] 문틈 그림 없음: Resources/{GateArt}", this);
        transform.localScale = Vector3.one * gateScale;
        box.size = gate != null ? gate.bounds.size : new Vector2(2f, 3.8f);

        // 방의 어둠·조명을 플레이어와 똑같이 받게 (플레이어보다 한 칸 뒤)
        Transform player = CineActor.Player();
        SpriteRenderer p = player != null ? player.GetComponentInChildren<SpriteRenderer>() : null;
        if (p != null)
        {
            sr.sortingLayerID = p.sortingLayerID;
            sr.sortingOrder = p.sortingOrder - 1;
            sr.sharedMaterial = p.sharedMaterial;
        }

        var lightGo = new GameObject("VaultGlow", typeof(Light2D));
        lightGo.transform.SetParent(transform, false);
        lightGo.transform.localPosition = new Vector3(0f, -0.4f, 0f);
        glow = lightGo.GetComponent<Light2D>();
        glow.lightType = Light2D.LightType.Point;
        glow.color = glowColor;
        glow.pointLightOuterRadius = glowRadius;
        glow.pointLightInnerRadius = 0.2f;
        glow.intensity = 0f;
    }

    private void Show(bool on, bool withFx)
    {
        open = on;
        if (sr != null) sr.enabled = on;
        if (glow != null) glow.gameObject.SetActive(on);
        if (box != null) box.enabled = on; // 숨어 있을 때는 E 조사를 가로채지 않음
        reveal = on && withFx ? 0f : 1f;
        if (!on || !withFx) return;

        // 틈이 드러나는 연출: 땅울림 + 반짝임 + 알림
        SetFlag(BastetItemIds.FlagVaultOpen);
        GameAudio.Play("sfx_rumble", 0.5f);
        for (int i = 0; i < 10; i++)
            RaGlowFx.Spark(transform.position + new Vector3(UnityEngine.Random.Range(-0.6f, 0.6f), UnityEngine.Random.Range(-0.8f, 0.8f), 0f), glowColor, sr.sortingLayerName, sr.sortingOrder + 1);
        AnubisToast.Show(openMessage, 3.5f);
        Debug.Log("[바스테트 향실] 비석을 모두 해독 -> 숨은 방 입구가 드러남", this);
    }

    // ================= 비석 확인 (0.5초마다) =================
    private void Update()
    {
        clock += Time.deltaTime;
        if (open)
        {
            reveal = Mathf.MoveTowards(reveal, 1f, Time.deltaTime * 0.8f); // 약 1초 동안 서서히 드러남
            if (sr != null) sr.color = new Color(1f, 1f, 1f, reveal);
            if (glow != null) glow.intensity = reveal * (0.6f + 0.3f * Mathf.PerlinNoise(Mathf.Floor(clock * 10f) * 0.37f, 0.5f));
            return;
        }

        pollTimer -= Time.deltaTime;
        if (pollTimer > 0f || steles == null || steles.Length == 0) return;
        pollTimer = 0.5f;
        int n = DecodedCount();
        if (n == lastCount) return;
        lastCount = n;
        if (n >= steles.Length) Show(true, true);
        else AnubisToast.Show(string.Format(progressFormat, n, steles.Length), 2.5f);
    }

    private int DecodedCount()
    {
        int n = 0;
        if (steles != null) foreach (BastetClueStele s in steles) if (s != null && s.IsDecoded) n++;
        return n;
    }

    // ================= IInteractable =================
    public string GetPrompt()
    {
        if (!open || busy || Cine.Running) return null;
        return HasFlag(BastetItemIds.FlagVaultSeen) ? "E: 향실로 다시 들어가기" : "E: 벽의 틈 살펴보기";
    }

    public void Interact(GameObject who)
    {
        if (!open || busy || Cine.Running || DialogueBox.IsOpen) return;
        busy = true;
        Cine.Run(Sequence());
    }

    private IEnumerator Sequence()
    {
        Cine.Begin(true);
        Vector3 at = transform.position;
        if (!HasFlag(BastetItemIds.FlagVaultSeen))
            yield return Cine.Say("나", "벽에 사람 하나 지나갈 틈이 벌어져 있어.", "안쪽에서 따뜻한 바람과 향 냄새가 흘러나와.");
        yield return CineShots.Crane(at + Vector3.down * 1.5f, at, 5f, 2.8f, 1.6f); // 아래에서 틈으로 다가감
        yield return HiddenVault.Play(BastetVaultScenes.Walk(), BastetVaultScenes.Reveal(), BastetItemIds.FlagVaultSeen, BastetVaultScenes.After);
        Cine.End();

        // 히든 유물은 한 번만 (획득 팝업이 닫힐 때까지 기다림)
        bool give = !string.IsNullOrEmpty(rewardItemId) && ItemInventory.Instance != null && !ItemInventory.Instance.Has(rewardItemId);
        if (give)
        {
            Debug.Log($"[바스테트 향실] 히든 유물 지급: {rewardItemId}", this);
            ItemPopup.Grant(rewardItemId, () => AnubisToast.Show("횃불이 두 배 밝고, 두 배 오래 탄다.", 3f));
            yield return null;
            while (ItemPopup.IsShowing) yield return null;
        }
        busy = false;
    }

    private void OnDisable()
    {
        busy = false;
    }

    private static bool HasFlag(string f) => !string.IsNullOrEmpty(f) && GameManager.Instance != null && GameManager.Instance.HasFlag(f);
    private static void SetFlag(string f) { if (GameManager.Instance != null && !GameManager.Instance.HasFlag(f)) GameManager.Instance.SetFlag(f); }

    // ================= 테스트 =================
    [ContextMenu("테스트: 비석 전부 해독한 것으로 하고 입구 열기")]
    private void TestOpen()
    {
        if (!Application.isPlaying) return;
        if (steles != null)
            foreach (BastetClueStele s in steles) if (s != null) s.TestMarkDecoded();
        lastCount = DecodedCount();
        Show(true, true);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.74f, 0.42f, 0.9f);
        Gizmos.DrawWireCube(transform.position, new Vector3(2f * gateScale, 3.8f * gateScale, 0f));
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.8f, "숨은 방(향실) 입구");
#endif
    }
}
