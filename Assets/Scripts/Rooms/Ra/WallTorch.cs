using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 라의 방 벽 횃불 (처음엔 전부 꺼져 있음)
// - 내 횃불이 켜져 있으면: E 길게 -> 불을 옮겨 붙임 -> 그 방이 밝아짐
// - 그 방 퍼즐을 풀면 자동으로도 켜짐 (Day Auto Flag / Night Auto Flag)
// - 밤이 되면(F_RA_NIGHT_OPEN) RaTorchManager가 전부 끔 -> 밤에는 다시 붙여야 함 (밤 불은 푸른빛, RaNightAtmosphere)
// - 이미 켜진 벽 횃불에서 E: 꺼진 내 횃불에 다시 불 붙이기
// - 켜진 상태는 플래그로 저장 (F_RA_TORCH_<id>_D / _N) -> 씬에 다시 들어와도 유지
// - 꺼진 벽 횃불은 내 불빛이 가까이 오면 가끔 불똥이 반짝 (어둠 속에서 "여기 붙일 수 있어" 단서)
[RequireComponent(typeof(Collider2D))]
public class WallTorch : MonoBehaviour, IInteractable
{
    [Header("이름 (플래그 = F_RA_TORCH_<Id>_D 또는 _N)")]
    [SerializeField] private string torchId = "";

    [Header("그림, 빛")]
    [SerializeField] private SpriteRenderer target;
    [SerializeField] private Sprite offSprite;
    [SerializeField] private Sprite onSprite;
    [SerializeField] private Light2D lightSource;
    [SerializeField, Min(0.1f)] private float litIntensity = 1.2f;

    [Header("자동으로 켜지는 플래그 (그 방 퍼즐 완료)")]
    [SerializeField] private string dayAutoFlag = "";
    [SerializeField] private string nightAutoFlag = "";
    [SerializeField] private string nightFlag = "F_RA_NIGHT_OPEN";

    [Header("길게 누르기")]
    [SerializeField, Min(0.1f)] private float holdSeconds = 1f;
    [SerializeField] private float holdRange = 1.8f;

    [Header("꺼진 횃불 반짝임 (가까이 가면)")]
    [SerializeField] private float glintRange = 4.5f;
    [SerializeField, Min(0.2f)] private float glintEvery = 1.6f;
    [SerializeField] private Vector3 glintOffset = new Vector3(0f, 0.45f, 0f);

    [Header("대사")]
    [SerializeField] private DialogueLine[] noFireLines = { new DialogueLine("나", "불 꺼진 횃불이다. 불이 있으면 옮겨 붙일 수 있을 텐데.") };

    // 아무 벽 횃불이든 플레이어가 직접 붙였을 때 (첫 횃불 튜토리얼용)
    public static event Action<WallTorch> PlayerLit;

    private bool lit;
    private bool animating;
    private bool holding;
    private float progress;
    private Transform player;
    private bool subscribed;
    private float glintTimer;

    public bool IsLit => lit;
    private bool IsNight => HasFlag(nightFlag);
    private string LitFlag => $"F_RA_TORCH_{torchId}_{(IsNight ? "N" : "D")}";
    private string AutoFlag => IsNight ? nightAutoFlag : dayAutoFlag;
    private static bool PlayerFireLit => TorchLink.Current != null && TorchLink.Current.IsLit;

    // ================= 초기화 =================
    private void Awake()
    {
        if (target == null) target = GetComponent<SpriteRenderer>();
        if (offSprite == null && target != null) offSprite = target.sprite;
        SetVisual(false, 0f);
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnFlagChanged += HandleFlag;
            subscribed = true;
        }
        if (HasFlag(LitFlag) || HasFlag(AutoFlag)) SetLitInstant();
    }

    private void OnDestroy()
    {
        if (subscribed && GameManager.Instance != null) GameManager.Instance.OnFlagChanged -= HandleFlag;
    }

    private void HandleFlag(string flag)
    {
        if (!lit && !animating && flag == AutoFlag && !string.IsNullOrEmpty(flag)) StartCoroutine(Ignite(false));
    }

    // ================= IInteractable =================
    public string GetPrompt()
    {
        if (animating) return null;
        if (lit) return PlayerFireLit || TorchLink.Current == null ? null : "E: 내 횃불에 불 옮겨 붙이기";
        return PlayerFireLit ? "E 길게: 횃불 붙이기" : "E: 살펴보기";
    }

    public void Interact(GameObject who)
    {
        try
        {
            if (animating || Cine.Running || DialogueBox.IsOpen || ItemPopup.IsShowing) return;
            if (DialogueBox.ClosedFrame == Time.frameCount) return;

            if (lit) RelightPlayer();
            else if (PlayerFireLit) StartHold(who);
            else DialogueBox.Play(noFireLines);
        }
        catch (Exception e)
        {
            Debug.LogError($"[WallTorch] 조사 오류: {e}");
        }
    }

    private void RelightPlayer()
    {
        if (TorchLink.Current == null || TorchLink.Current.IsLit) return;
        TorchLink.Current.Relight("벽 횃불의 불을 내 횃불에 옮겨 붙였다.");
    }

    private void StartHold(GameObject who)
    {
        player = who != null ? who.transform : null;
        holding = true;
    }

    // ================= 길게 누르기 =================
    private void Update()
    {
        if (lit && !animating) Flicker();
        else if (!lit && !animating) Glint();
        if (!holding && progress <= 0f) return;

        bool pressing = holding && Input.GetKey(KeyCode.E) && InRange() && !PlayerLock.IsLocked && PlayerFireLit;
        if (!pressing) holding = false;

        progress = Mathf.Clamp01(progress + (pressing ? 1f : -1.5f) * Time.deltaTime / holdSeconds);
        if (progress > 0f) HoldRingUI.Show(transform.position, progress);
        else HoldRingUI.Hide();

        if (progress < 1f) return;
        holding = false;
        progress = 0f;
        HoldRingUI.Hide();
        StartCoroutine(Ignite(true));
    }

    private bool InRange()
    {
        return player != null && Vector2.Distance(player.position, transform.position) <= holdRange;
    }

    // ================= 켜기 / 끄기 =================
    private IEnumerator Ignite(bool byPlayer)
    {
        animating = true;
        if (byPlayer && GameManager.Instance != null && !HasFlag(LitFlag)) GameManager.Instance.SetFlag(LitFlag);

        CineFX.Puff(FxSprites.Dots, transform.position + Vector3.up * 0.5f, 10, 1.2f, new Color(1f, 0.75f, 0.3f));
        float t = 0f;
        while (t < 0.8f)
        {
            t += Time.deltaTime;
            float p = t / 0.8f;
            SetVisual(true, litIntensity * (p < 0.35f ? Mathf.Lerp(0f, 2.2f, p / 0.35f) : Mathf.Lerp(2.2f, 1f, (p - 0.35f) / 0.65f)));
            yield return null;
        }

        lit = true;
        animating = false;
        if (byPlayer) PlayerLit?.Invoke(this);
    }

    // RaTorchManager가 밤이 될 때 부름 (delay 뒤 깜빡이다 꺼짐)
    public IEnumerator Extinguish(float delay)
    {
        if (!lit) yield break;
        animating = true;
        yield return new WaitForSeconds(delay);

        for (int i = 0; i < 3; i++)
        {
            SetVisual(true, litIntensity * 0.3f);
            yield return new WaitForSeconds(0.07f);
            SetVisual(true, litIntensity);
            yield return new WaitForSeconds(0.07f);
        }
        CineFX.Puff(FxSprites.Dots, transform.position + Vector3.up * 0.5f, 6, 0.6f, new Color(0.35f, 0.32f, 0.3f));
        SetVisual(false, 0f);
        lit = false;
        animating = false;
    }

    private void SetLitInstant()
    {
        lit = true;
        SetVisual(true, litIntensity);
    }

    private void SetVisual(bool on, float intensity)
    {
        if (target != null)
        {
            Sprite s = on ? onSprite : offSprite;
            if (s != null) target.sprite = s;
        }
        if (lightSource == null) return;
        lightSource.gameObject.SetActive(on);
        lightSource.intensity = intensity;
    }

    private void Flicker()
    {
        if (lightSource == null) return;
        float n = Mathf.PerlinNoise(Time.time * 3f, transform.position.x);
        lightSource.intensity = litIntensity * (0.85f + n * 0.3f);
    }

    // 내 횃불이 켜진 채로 가까이 있으면 가끔 불똥 2~3개
    private void Glint()
    {
        glintTimer -= Time.deltaTime;
        if (glintTimer > 0f) return;
        glintTimer = glintEvery * UnityEngine.Random.Range(0.7f, 1.3f);

        Component fire = TorchLink.Current as Component;
        if (fire == null || !PlayerFireLit || target == null) return;
        if (Vector2.Distance(fire.transform.position, transform.position) > glintRange) return;

        int count = UnityEngine.Random.Range(2, 4);
        for (int i = 0; i < count; i++)
        {
            RaGlowFx.Spark(transform.position + glintOffset, new Color(1f, 0.72f, 0.3f), target.sortingLayerName, target.sortingOrder + 1);
        }
    }

    private static bool HasFlag(string flag)
    {
        return !string.IsNullOrEmpty(flag) && GameManager.Instance != null && GameManager.Instance.HasFlag(flag);
    }
}
