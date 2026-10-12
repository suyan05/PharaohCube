using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 라의 방 "빛의 길" (신전 발굴 지도 v1)
// - 구역마다 해금 플래그를 정하고, 플래그가 켜지면 그 구역 횃불을 켬
// - 퍼즐 화면이 열려 있을 때 해금되면 화면을 닫은 뒤에 연출 (닫아야 맵이 보이니까)
// - 연출 순서: 플레이어 잠금 -> 카메라가 그 방으로 이동 -> 흔들림 -> 횃불 번쩍 + 알림
//              -> 잠깐 보여준 뒤 카메라 복귀 -> 플레이어 잠금 해제
// - Focus Point가 비어 있으면 카메라 이동 없이 제자리에서 흔들림 + 불만 켬
// - 해금 전 구역은 어둡게 + Turn Off 오브젝트(잠긴 입구 등)는 켜둠
// - 씬에 다시 들어왔을 때 이미 켜진 플래그는 연출 없이 바로 복원
public class RaZoneLighting : MonoBehaviour
{
    // GameMessageUI가 이 말머리로 시작하는 로그를 오른쪽 위 알림으로 띄움
    private const string MessageTag = "[Door] ";

    // 번쩍임이 최고 밝기에 닿는 시점 (켜지는 시간 중 35% 지점)
    private const float FlashPeak = 0.35f;

    [Serializable]
    public class LightZone
    {
        [Tooltip("Inspector에서 알아보기 쉬운 이름 (예: LightObject_P1)")]
        public string zoneName = "구역";

        [Tooltip("이 플래그가 켜지면 불이 들어옴. 비우면 처음부터 켜짐")]
        public string unlockFlag = "";

        [Tooltip("불이 켜질 때 오른쪽 위에 띄울 알림 (비우면 알림 없음)")]
        [TextArea] public string unlockMessage = "";

        [Tooltip("불이 켜질 때 카메라가 비출 곳 (방 가운데 빈 오브젝트). 비우면 카메라 이동 없음")]
        public Transform focusPoint;

        [Tooltip("해금 때 켤 오브젝트 (Spot Light 2D 등)")]
        public GameObject[] turnOn;

        [Tooltip("해금 때 끌 오브젝트 (잠긴 입구, 어둠 덮개 등)")]
        public GameObject[] turnOff;

        [Tooltip("서서히 밝아질 Light2D (turnOn에 넣은 빛)")]
        public Light2D[] fadeLights;

        [NonSerialized] public bool lit;
    }

    [Header("구역 목록 (위에서부터 해금 순서대로)")]
    [SerializeField] private LightZone[] zones;

    [Header("연출 - 횃불")]
    [Tooltip("횃불이 0에서 원래 밝기까지 켜지는 시간 (초)")]
    [SerializeField] private float fadeDuration = 1.2f;

    [Tooltip("켜지는 순간 원래 밝기의 몇 배까지 번쩍일지 (1이면 번쩍임 없음)")]
    [SerializeField] private float flashMultiplier = 2f;

    [Header("연출 - 타이밍")]
    [Tooltip("퍼즐 화면을 닫은 뒤 연출이 시작되기까지 기다리는 시간 (초)")]
    [SerializeField] private float delayAfterClose = 0.4f;

    [Tooltip("한 번에 여러 구역이 열릴 때 구역 사이 간격 (초)")]
    [SerializeField] private float gapBetweenZones = 0.6f;

    [Header("연출 - 카메라 이동")]
    [Tooltip("카메라가 그 방까지 가는 시간 (초)")]
    [SerializeField] private float panTime = 0.9f;

    [Tooltip("불이 켜진 방을 보여주는 시간 (초)")]
    [SerializeField] private float holdTime = 1.4f;

    [Tooltip("카메라가 플레이어에게 돌아오는 시간 (초). 끝나면 조작 가능")]
    [SerializeField] private float returnTime = 0.7f;

    [Header("연출 - 화면 흔들림")]
    [Tooltip("비우면 Main Camera에서 자동으로 찾음")]
    [SerializeField] private CameraFollow2D cameraFollow;
    [SerializeField] private float shakeDuration = 0.6f;
    [Tooltip("흔들리는 폭 (유닛). 0.1 = 약 3픽셀")]
    [SerializeField] private float shakeStrength = 0.12f;

    // Inspector에 적힌 원래 밝기 (서서히 켤 때 목표값)
    private readonly Dictionary<Light2D, float> targetIntensity = new Dictionary<Light2D, float>();

    // 퍼즐 화면이 닫히길 기다리는 구역
    private readonly List<LightZone> waiting = new List<LightZone>();
    private bool playing;
    private bool subscribed;

    // 연출 중 잠근 플레이어
    private PlayerController2D playerController;
    private InteractionSystem playerInteraction;
    private Rigidbody2D playerRb;
    private bool playerLocked;

    // ================= 초기화 =================
    private void Awake()
    {
        try
        {
            CacheIntensities();
            DarkenAll();
            FindCamera();
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 조명 초기화 오류: {e}");
        }
    }

    private void OnEnable()
    {
        PuzzleOverlayFocus.OnFocusChanged += HandleFocusChanged;
    }

    private void OnDisable()
    {
        PuzzleOverlayFocus.OnFocusChanged -= HandleFocusChanged;
        EndCutscene(); // 꺼지면 코루틴도 멈추니까 카메라와 플레이어를 원래대로
        playing = false;
    }

    private void Start()
    {
        try
        {
            RestoreFromFlags();

            if (GameManager.Instance == null)
            {
                Debug.LogWarning($"[{name}] GameManager가 없어서 플래그로 불을 켤 수 없음");
                return;
            }
            GameManager.Instance.OnFlagChanged += HandleFlagChanged;
            subscribed = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 조명 구독 오류: {e}");
        }
    }

    private void OnDestroy()
    {
        if (subscribed && GameManager.Instance != null)
        {
            GameManager.Instance.OnFlagChanged -= HandleFlagChanged;
        }
    }

    private void CacheIntensities()
    {
        if (zones == null) return;

        foreach (LightZone zone in zones)
        {
            if (zone?.fadeLights == null) continue;

            foreach (Light2D light in zone.fadeLights)
            {
                if (light != null && !targetIntensity.ContainsKey(light))
                {
                    targetIntensity[light] = light.intensity;
                }
            }
        }
    }

    private void FindCamera()
    {
        if (cameraFollow != null) return;

        Camera main = Camera.main;
        if (main != null) cameraFollow = main.GetComponent<CameraFollow2D>();
        if (cameraFollow == null) Debug.LogWarning($"[{name}] CameraFollow2D를 못 찾아서 카메라 연출 없이 진행");
    }

    // ================= 플래그 / 퍼즐 화면 =================
    private void HandleFlagChanged(string flag)
    {
        try
        {
            foreach (LightZone zone in zones)
            {
                if (zone != null && zone.unlockFlag == flag) RequestUnlock(zone);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 조명 전환 오류: {e}");
        }
    }

    // 퍼즐 화면이 열려 있으면 기다렸다가, 닫혀 있으면 바로 연출
    private void RequestUnlock(LightZone zone)
    {
        if (zone.lit || waiting.Contains(zone)) return;

        waiting.Add(zone);
        Debug.Log($"[{name}] {zone.zoneName} 해금 대기 (퍼즐 화면 닫히면 연출)");

        if (!PuzzleOverlayFocus.IsActive) StartPlayWaiting();
    }

    private void HandleFocusChanged(bool puzzleOpen)
    {
        if (!puzzleOpen && waiting.Count > 0) StartPlayWaiting();
    }

    private void StartPlayWaiting()
    {
        if (playing || !isActiveAndEnabled) return;
        StartCoroutine(PlayWaiting());
    }

    private IEnumerator PlayWaiting()
    {
        playing = true;
        yield return new WaitForSeconds(delayAfterClose);

        while (waiting.Count > 0)
        {
            if (PuzzleOverlayFocus.IsActive) break; // 다시 퍼즐을 열면 멈췄다가 닫을 때 이어서

            LightZone zone = waiting[0];
            waiting.RemoveAt(0);
            yield return PlayUnlock(zone);

            if (waiting.Count > 0) yield return new WaitForSeconds(gapBetweenZones);
        }
        playing = false;
    }

    // (카메라 이동) -> 흔들림 -> 흔들림 중간쯤 불이 붙음 + 알림 -> (보여주고 복귀)
    private IEnumerator PlayUnlock(LightZone zone)
    {
        bool pan = zone.focusPoint != null && cameraFollow != null;
        if (pan)
        {
            BeginCutscene(zone.focusPoint.position);
            yield return new WaitForSeconds(panTime);
        }

        Shake();
        yield return new WaitForSeconds(shakeDuration * 0.5f);

        LightZoneOn(zone, false);
        ShowMessage(zone);
        Debug.Log($"[{name}] {zone.zoneName} 불 켜짐 ({zone.unlockFlag})");

        if (!pan) yield break;

        yield return new WaitForSeconds(holdTime);
        if (cameraFollow != null) cameraFollow.ReleaseFocus();
        yield return new WaitForSeconds(returnTime);
        EndCutscene();
    }

    // ================= 연출 중 플레이어 잠금 + 카메라 =================
    private void BeginCutscene(Vector3 focus)
    {
        try
        {
            SetPlayerLocked(true);
            cameraFollow.FocusOn(focus);
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 카메라 연출 시작 오류: {e}");
        }
    }

    private void EndCutscene()
    {
        try
        {
            if (cameraFollow != null) cameraFollow.ReleaseFocus();
            SetPlayerLocked(false);
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 카메라 연출 종료 오류: {e}");
        }
    }

    // 이동(PlayerController2D)과 E키(InteractionSystem)를 같이 막음
    private void SetPlayerLocked(bool locked)
    {
        if (locked == playerLocked) return;
        if (locked) FindPlayer();

        if (playerController != null) playerController.enabled = !locked;
        if (playerInteraction != null) playerInteraction.enabled = !locked;
        if (locked && playerRb != null) playerRb.linearVelocity = Vector2.zero;

        playerLocked = locked;
    }

    private void FindPlayer()
    {
        if (playerController != null) return;

        playerController = FindAnyObjectByType<PlayerController2D>();
        if (playerController == null) return;

        playerInteraction = playerController.GetComponent<InteractionSystem>();
        playerRb = playerController.GetComponent<Rigidbody2D>();
    }

    private void Shake()
    {
        try
        {
            if (cameraFollow != null) cameraFollow.Shake(shakeDuration, shakeStrength);
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 화면 흔들림 오류: {e}");
        }
    }

    private static void ShowMessage(LightZone zone)
    {
        if (string.IsNullOrEmpty(zone.unlockMessage)) return;
        Debug.Log(MessageTag + zone.unlockMessage);
    }

    // 이미 켜진 플래그 + 플래그가 빈 구역은 연출 없이 바로 켬
    private void RestoreFromFlags()
    {
        if (zones == null) return;

        foreach (LightZone zone in zones)
        {
            if (zone != null && IsUnlocked(zone)) LightZoneOn(zone, true);
        }
    }

    private static bool IsUnlocked(LightZone zone)
    {
        if (string.IsNullOrEmpty(zone.unlockFlag)) return true;
        return GameManager.Instance != null && GameManager.Instance.HasFlag(zone.unlockFlag);
    }

    // ================= 켜기 / 끄기 =================
    private void LightZoneOn(LightZone zone, bool instant)
    {
        if (zone.lit) return; // 두 번 켜기 방지
        zone.lit = true;

        SetObjects(zone.turnOn, true);
        SetObjects(zone.turnOff, false);

        if (instant || fadeDuration <= 0f || !isActiveAndEnabled)
        {
            SetLights(zone, 1f);
            return;
        }

        SetLights(zone, 0f);
        StartCoroutine(FadeIn(zone));
    }

    private void DarkenAll()
    {
        if (zones == null) return;

        foreach (LightZone zone in zones)
        {
            if (zone == null) continue;
            zone.lit = false;
            SetObjects(zone.turnOn, false);
            SetObjects(zone.turnOff, true);
        }
    }

    private IEnumerator FadeIn(LightZone zone)
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            SetLights(zone, FlashRatio(Mathf.Clamp01(t / fadeDuration)));
            yield return null;
        }
        SetLights(zone, 1f);
    }

    // 0 -> 번쩍(flashMultiplier) -> 원래 밝기(1)
    private float FlashRatio(float progress)
    {
        float peak = Mathf.Max(1f, flashMultiplier);
        if (progress < FlashPeak) return Mathf.Lerp(0f, peak, progress / FlashPeak);
        return Mathf.Lerp(peak, 1f, (progress - FlashPeak) / (1f - FlashPeak));
    }

    // ratio 0 = 꺼짐, 1 = Inspector에 적은 원래 밝기
    private void SetLights(LightZone zone, float ratio)
    {
        if (zone.fadeLights == null) return;

        foreach (Light2D light in zone.fadeLights)
        {
            if (light == null) continue;
            if (!targetIntensity.TryGetValue(light, out float target)) target = light.intensity;
            light.intensity = target * ratio;
        }
    }

    private static void SetObjects(GameObject[] list, bool active)
    {
        if (list == null) return;

        foreach (GameObject go in list)
        {
            if (go != null) go.SetActive(active);
        }
    }

    // ================= 테스트용 (Play 중에만) =================
    [ContextMenu("테스트: 다음 구역 열기 (연출 포함, 플래그는 안 켬)")]
    private void TestLightNext()
    {
        if (!CheckPlaying()) return;

        foreach (LightZone zone in zones)
        {
            if (zone == null || zone.lit || waiting.Contains(zone)) continue;

            RequestUnlock(zone);
            return;
        }
        Debug.Log($"[{name}] 테스트: 이미 전부 켜져 있음");
    }

    [ContextMenu("테스트: 플래그 기준으로 다시 맞추기")]
    private void TestResetToFlags()
    {
        if (!CheckPlaying()) return;

        StopAllCoroutines();
        EndCutscene();
        playing = false;
        waiting.Clear();
        DarkenAll();
        RestoreFromFlags();
        Debug.Log($"[{name}] 테스트: 지금 플래그 기준으로 다시 맞춤");
    }

    // 에디터에서 누르면 씬 오브젝트가 꺼진 채로 저장될 수 있어서 막음
    private bool CheckPlaying()
    {
        if (Application.isPlaying) return true;
        Debug.LogWarning($"[{name}] 테스트 메뉴는 Play 중에만 써야 함");
        return false;
    }
}