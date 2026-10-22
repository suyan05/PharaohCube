using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

// 횃불 제단 (프리팹: Assets/Prefabs/Common/TorchAltar.prefab, 중앙 홀 등에 배치)
// 1) 제단 앞에서 E를 길게 누름 (머리 위 링이 참)
// 2) 다 차면 "횃불을 가져갈까?" 질문 (가져간다 / 그만둔다)
// 3) 가져가면 획득 팝업(팀 ItemPopup) + 안내 문구, 횃불에 불이 붙음
// - 이미 횃불이 있는데 꺼졌으면: 길게 누르면 질문 없이 바로 다시 붙임
// - 불이 켜져 있으면 안내 문구가 사라짐 (할 일 없음)
[RequireComponent(typeof(Collider2D))]
public class TorchAltar : MonoBehaviour, IInteractable
{
    [Header("아이템")]
    [Tooltip("팀 ItemCatalog 횃불 ID")]
    [SerializeField] private string torchItemId = "ITEM_TORCH";

    [Header("길게 누르기")]
    [Range(0.3f, 5f)] [SerializeField] private float holdSeconds = 1.5f;
    [Tooltip("손을 떼면 줄어드는 속도 (채우는 속도 대비 배율)")]
    [Range(0f, 3f)] [SerializeField] private float holdDecayRate = 1.5f;
    [Tooltip("플레이어가 이 거리(칸)보다 멀어지면 중단")]
    [SerializeField] private float holdRange = 1.8f;

    [Header("문구")]
    [SerializeField] private string takePrompt = "E 길게: 횃불 얻기";
    [SerializeField] private string relightPrompt = "E 길게: 횃불 다시 붙이기";
    [TextArea(1, 2)] [SerializeField] private string askText = "제단의 불이 타오른다. 횃불을 가져갈까?";
    [SerializeField] private string yesOption = "가져간다";
    [SerializeField] private string noOption = "그만둔다";
    [TextArea(1, 2)] [SerializeField] private string acquiredToast = "횃불을 얻었다! 어두운 신전을 밝힐 수 있다.";
    [TextArea(1, 2)] [SerializeField] private string relightToast = "제단의 불을 횃불에 다시 옮겨 붙였다.";
    [TextArea(1, 2)] [SerializeField] private string declineToast = "아직은 두고 가자.";

    [Header("연출 (선택)")]
    [Tooltip("제단 불꽃 빛 (밝기를 살짝 흔들어 살아 있는 불처럼)")]
    [SerializeField] private Light2D flameLight;
    [Range(0f, 0.5f)] [SerializeField] private float flicker = 0.15f;

    private bool holding;
    private bool busy;
    private float progress;
    private float baseIntensity = 1f;
    private Transform player;

    private ITorchLight Torch => TorchLink.Current;
    private bool HasTorchItem => ItemInventory.Instance != null && ItemInventory.Instance.Has(torchItemId);
    private bool TorchIsLit => Torch != null && Torch.IsLit;

    private void Awake()
    {
        if (flameLight != null) baseIntensity = flameLight.intensity;
    }

    // ================= IInteractable =================
    public string GetPrompt()
    {
        if (busy || TorchIsLit) return "";
        return HasTorchItem ? relightPrompt : takePrompt;
    }

    public void Interact(GameObject who)
    {
        if (busy || TorchIsLit || DialogueBox.IsOpen || ItemPopup.IsShowing) return;
        if (DialogueBox.ClosedFrame == Time.frameCount) return; // 대사 닫을 때 누른 E로 바로 다시 시작 방지
        player = who != null ? who.transform : null;
        holding = true;
        Debug.Log($"[TorchAltar] 길게 누르기 시작 (횃불 {(HasTorchItem ? "있음 -> 다시 붙이기" : "없음 -> 얻기")})", this);
    }

    // ================= 매 프레임 (할당 없음) =================
    private void Update()
    {
        if (flameLight != null && flicker > 0f)
            flameLight.intensity = baseIntensity * (1f + (Mathf.PerlinNoise(Time.time * 6f, 0.3f) - 0.5f) * 2f * flicker);

        if (!holding) return;

        bool pressed = Keyboard.current != null && Keyboard.current.eKey.isPressed;
        bool inRange = player == null || Vector2.Distance(player.position, transform.position) <= holdRange;
        if (!inRange) { CancelHold(); return; }

        progress += (pressed ? 1f : -holdDecayRate) * Time.deltaTime / holdSeconds;
        if (progress <= 0f && !pressed) { CancelHold(); return; }
        progress = Mathf.Clamp01(progress);
        HoldRingUI.Show(transform.position, progress);

        if (progress >= 1f)
        {
            holding = false;
            progress = 0f;
            HoldRingUI.Hide();
            StartCoroutine(Complete());
        }
    }

    private void CancelHold()
    {
        holding = false;
        progress = 0f;
        HoldRingUI.Hide();
    }

    // ================= 완료 =================
    private IEnumerator Complete()
    {
        busy = true;

        // 이미 횃불이 있으면 질문 없이 다시 붙임
        if (HasTorchItem)
        {
            if (Torch != null) Torch.Relight(relightToast);
            else AnubisToast.Show(relightToast, 2.5f);
            Debug.Log("[TorchAltar] 횃불 다시 붙임", this);
            busy = false;
            yield break;
        }

        // 처음이면 가져갈지 물어봄
        int choice = -1;
        DialogueBox.Ask(new DialogueLine("", askText), new[] { yesOption, noOption }, i => choice = i);
        while (choice < 0) yield return null;

        if (choice == 0)
        {
            // 팀 획득 팝업 (인벤토리 지급 + 팝업). 팝업을 닫으면 안내 문구
            ItemPopup.Grant(torchItemId, () => AnubisToast.Show(acquiredToast, 3f));
            if (Torch != null && !Torch.IsLit) Torch.Relight(""); // PlayerTorchTemp는 아이템 이벤트로도 켜짐
            Debug.Log($"[TorchAltar] {torchItemId} 획득", this);
        }
        else
        {
            AnubisToast.Show(declineToast, 2f);
        }
        busy = false;
    }

    private void OnDisable()
    {
        if (holding) HoldRingUI.Hide();
        holding = false;
    }

    // ================= 테스트 =================
    [ContextMenu("테스트: 바로 완료 (길게 누르기 생략)")]
    private void TestComplete()
    {
        if (Application.isPlaying && !busy) StartCoroutine(Complete());
    }

    // Scene 뷰: 길게 누르기 거리
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, holdRange);
    }
}
