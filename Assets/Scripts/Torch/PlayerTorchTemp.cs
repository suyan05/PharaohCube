using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

// [임시] 플레이어 횃불 (팀장 공통 횃불이 나오기 전까지 바스테트 퍼즐 테스트용)
// 붙이는 곳: Player (Light 2D가 없으면 자식 "TorchLight"를 실행 중에 만듦)
//
// 시야 규칙 (기본 반경 x 배율)
//  가진 것              | 횃불 켜짐 | 횃불 꺼짐
//  횃불만               | 1.0       | 0.4
//  횃불 + 라의 불씨      | 1.3       | 0.8 (불씨는 절대 안 꺼짐, [추가] 시간으로는 다 타지 않음 - 옵션)
//  횃불 + 바스테트 그릇   | 2.0       | 0.4  (타는 시간 2배)
//  횃불 + 불씨 + 그릇    | 3.0       | 0.8  (시간이 지나도 안 탐, 퍼즐 처벌로는 꺼짐)
//
// 다시 켜기
//  - 라의 불씨가 있으면: 어디서든 E를 2초 길게 누르기
//  - 없으면: 횃불이 재가 됨 -> 중앙 홀 화로(TorchRelightPoint)에서 다시 받기
public class PlayerTorchTemp : MonoBehaviour, ITorchLight
{
    [Header("아이템 ID (팀 공통 이름이 정해지면 바꾸기)")]
    [SerializeField] private string torchItemId = "ITEM_TORCH";          // [수정] 팀 ItemCatalog ID
    [SerializeField] private string emberItemId = "ITEM_RA_EMBER";       // [수정] 팀 ItemCatalog ID
    [SerializeField] private string vesselItemId = "ITEM_BASTET_VESSEL"; // [수정] ITEM_* 규칙
    [Tooltip("테스트용: 인벤토리에 횃불이 없어도 처음부터 들고 시작. 실제 게임(중앙 홀 횃불 제단에서 받기)은 끄기")]
    [SerializeField] private bool startWithTorch = true;
    private bool subscribed;

    [Header("빛 (Light 2D, 비우면 자동 생성)")]
    [SerializeField] private Light2D torchLight;
    [Tooltip("배율 1.0일 때 빛 반경 (유닛 = 칸)")]
    [Range(1f, 10f)] [SerializeField] private float baseRadius = 4f;
    [SerializeField] private Color flameColor = new Color(1f, 0.82f, 0.48f);
    [SerializeField] private Color emberColor = new Color(1f, 0.95f, 0.77f);
    [Range(0f, 3f)] [SerializeField] private float intensity = 1.1f;
    [Tooltip("반경이 바뀔 때 부드럽게 (초당 유닛)")]
    [SerializeField] private float radiusLerpSpeed = 6f;

    [Header("배율")]
    [SerializeField] private float litMul = 1.0f;
    [SerializeField] private float litEmberMul = 1.3f;
    [SerializeField] private float litVesselMul = 2.0f;
    [SerializeField] private float litBothMul = 3.0f;
    [SerializeField] private float outMul = 0.4f;
    [SerializeField] private float outEmberMul = 0.8f;

    [Header("타는 시간 (초)")]
    [Tooltip("횃불 하나가 다 타는 시간. 그릇이 있으면 2배, 불씨 + 그릇이면 무한")]
    [SerializeField] private float burnSeconds = 240f;
    [Tooltip("[추가] 라의 불씨가 있으면 시간이 지나도 다 타지 않음 (팀 횃불 HUD 규칙). 퍼즐 처벌로는 꺼짐")]
    [SerializeField] private bool emberNeverBurnsOut = true;

    [Header("라의 불씨로 다시 켜기")]
    [Range(0.5f, 5f)] [SerializeField] private float emberRelightHold = 2f;

    public event Action<bool> OnLitChanged; // 켜짐/꺼짐 (HUD, 연출 연결용)

    private bool torchOwned;    // 횃불이 손에 있는지 (재가 되면 false)
    private bool lit;
    private float burnLeft;
    private float holdTimer;
    private float currentRadius;

    public bool HasTorch => torchOwned;
    public bool IsLit => lit;
    public bool HasEmber => HasItem(emberItemId);
    public bool HasVessel => HasItem(vesselItemId);
    public float BurnRatio => BurnInfinite ? 1f : Mathf.Clamp01(burnLeft / Mathf.Max(1f, BurnTotal));

    private float BurnTotal => burnSeconds * (HasVessel ? 2f : 1f);
    private bool BurnInfinite => HasEmber && (HasVessel || emberNeverBurnsOut); // [수정] 불씨만 있어도 무한 (옵션)

    // ================= 초기화 =================
    private void Awake()
    {
        if (torchLight == null) torchLight = CreateLight();
    }

    private void OnEnable() => TorchLink.Register(this);

    private void OnDisable()
    {
        TorchLink.Unregister(this);
        if (subscribed && ItemInventory.Instance != null) ItemInventory.Instance.OnItemGranted -= HandleItem;
        subscribed = false;
    }

    // [추가] 횃불 아이템을 얻으면 바로 손에 들고 불이 붙음 (횃불 제단 TorchAltar)
    private void HandleItem(string itemId)
    {
        if (itemId == torchItemId && !lit) Relight("");
    }

    private void Start()
    {
        torchOwned = startWithTorch || HasItem(torchItemId);
        lit = torchOwned;
        if (ItemInventory.Instance != null && !subscribed) { ItemInventory.Instance.OnItemGranted += HandleItem; subscribed = true; } // [추가]
        burnLeft = BurnTotal;
        currentRadius = baseRadius * TargetMul();
        ApplyLight(true);
    }

    private Light2D CreateLight()
    {
        var go = new GameObject("TorchLight");
        go.transform.SetParent(transform, false);
        var l = go.AddComponent<Light2D>();
        l.lightType = Light2D.LightType.Point;
        l.falloffIntensity = 0.6f;
        return l;
    }

    // ================= 매 프레임 (할당 없음) =================
    private void Update()
    {
        // 시간에 따라 탐
        if (lit && !BurnInfinite && !TorchLink.BurnPaused) // [수정] 퍼즐 푸는 중에는 멈춤
        {
            burnLeft -= Time.deltaTime;
            if (burnLeft <= 0f) Extinguish("횃불이 다 타 버렸다.");
        }

        // 라의 불씨로 다시 켜기 (E 길게)
        // [수정] 대사창·획득 팝업·연출 중(PlayerLock)에는 E가 다른 용도라 막음
        bool canHold = !lit && torchOwned && HasEmber && !PuzzleOverlayFocus.IsActive && !PlayerLock.IsLocked
                       && !DialogueBox.IsOpen && !ItemPopup.IsShowing && Keyboard.current != null;
        if (canHold && Keyboard.current.eKey.isPressed)
        {
            holdTimer += Time.deltaTime;
            HoldRingUI.Show(transform.position, holdTimer / emberRelightHold); // [수정] 팀 공용 길게 누르기 링
            if (holdTimer >= emberRelightHold) { HoldRingUI.Hide(); Relight("라의 불씨가 횃불에 옮겨붙었다."); }
        }
        else if (holdTimer > 0f)
        {
            holdTimer = 0f;
            HoldRingUI.Hide();
        }

        ApplyLight(false);
    }

    private void ApplyLight(bool instant)
    {
        if (torchLight == null) return;
        float target = baseRadius * TargetMul();
        currentRadius = instant ? target : Mathf.MoveTowards(currentRadius, target, radiusLerpSpeed * Time.deltaTime);
        torchLight.pointLightOuterRadius = currentRadius;
        torchLight.pointLightInnerRadius = currentRadius * 0.35f;
        torchLight.color = lit ? flameColor : emberColor;
        torchLight.intensity = intensity;
    }

    private float TargetMul()
    {
        bool ember = HasEmber, vessel = HasVessel;
        if (!lit) return ember ? outEmberMul : outMul;
        if (ember && vessel) return litBothMul;
        if (vessel) return litVesselMul;
        if (ember) return litEmberMul;
        return litMul;
    }

    // ================= ITorchLight =================
    public void Extinguish(string reason)
    {
        if (!lit) return;
        lit = false;
        holdTimer = 0f;

        if (HasEmber)
        {
            AnubisToast.Show($"{reason}\n라의 불씨는 아직 타오른다. (E를 {emberRelightHold:0}초 눌러 다시 붙이기)", 4f);
        }
        else
        {
            torchOwned = false; // 재가 됨 -> 중앙 홀에서 다시 받아야 함
            AnubisToast.Show($"{reason}\n횃불이 재가 되었다. 중앙 홀 화로에서 다시 불을 붙여야 한다.", 4f);
        }
        Debug.Log($"[PlayerTorchTemp] 꺼짐: {reason} (불씨 {HasEmber}, 횃불 남음 {torchOwned})", this);
        OnLitChanged?.Invoke(false);
    }

    // 중앙 홀 화로 또는 불씨로 다시 켬
    public void Relight(string message)
    {
        torchOwned = true;
        lit = true;
        burnLeft = BurnTotal;
        holdTimer = 0f;
        if (!string.IsNullOrEmpty(message)) AnubisToast.Show(message, 2.5f);
        Debug.Log("[PlayerTorchTemp] 다시 켜짐", this);
        OnLitChanged?.Invoke(true);
    }

    private static bool HasItem(string id) =>
        !string.IsNullOrEmpty(id) && ItemInventory.Instance != null && ItemInventory.Instance.Has(id);

    // ================= 테스트 =================
    [ContextMenu("테스트: 횃불 끄기")]
    private void TestOff() => Extinguish("테스트로 껐다.");

    [ContextMenu("테스트: 횃불 다시 켜기")]
    private void TestOn() => Relight("테스트로 켰다.");

    [ContextMenu("테스트: 라의 불씨 얻기")]
    private void TestEmber() { if (ItemInventory.Instance != null) ItemInventory.Instance.Grant(emberItemId); }

    [ContextMenu("테스트: 바스테트 그릇 얻기")]
    private void TestVessel() { if (ItemInventory.Instance != null) ItemInventory.Instance.Grant(vesselItemId); }

    // Scene 뷰: 지금 시야 반경
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.7f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, Application.isPlaying ? currentRadius : baseRadius);
    }
}
