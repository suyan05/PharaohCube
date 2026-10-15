using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 아누비스 방 "다음 제단 안내" (라의 방 RaZoneLighting과 같은 연출)
// - 서브 퍼즐 보상 아이템을 얻으면 다음 구역의 불이 켜지고, 카메라가 그곳을 비춰 어디로 갈지 보여줌
// - 라는 GameManager 플래그로 켜지지만, 아누비스는 기존 제단 잠금(AnubisAltarLock)과 똑같이 "아이템 획득"으로 켜짐
//   -> GameManager가 없는 아누비스 씬을 바로 실행해도 동작
// - 퍼즐 화면이 열려 있을 때 해금되면 화면이 닫힌 뒤에 연출 (PuzzleOverlayFocus)
// - 연출 순서: 플레이어 잠금 -> 카메라가 다음 제단으로 이동 -> 흔들림 -> 불빛 번쩍 + 안내
//              -> 잠깐 보여준 뒤 카메라 복귀 -> 플레이어 잠금 해제
// - 씬 시작 때 이미 가진 아이템이면 연출 없이 바로 켬 (치트 F5 / 재시작 대응)
public class AnubisZoneGuide : MonoBehaviour
{
    // 불빛이 번쩍이는 최고점 위치
    private const float FlashPeak = 0.35f;

    [Serializable]
    public class GuideZone
    {
        [Tooltip("Inspector에서 알아보기 위한 이름 (예: A2 톱니 구역)")]
        public string zoneName = "구역";

        [Tooltip("이 아이템을 얻으면 켜짐. 비우면 처음부터 켜짐\n예) A2 구역 = ITEM_ANUBIS_GOLD_JAR (A1 보상)")]
        public string unlockItem = "";

        [Tooltip("켜질 때 화면에 띄울 안내 (비우면 안내 없음)")]
        [TextArea] public string unlockMessage = "";

        [Tooltip("카메라가 비출 곳 (보통 다음 제단). 비우면 카메라 이동 없이 흔들림 + 불만 켬")]
        public Transform focusPoint;

        [Tooltip("켜질 때 활성화할 오브젝트 (구역 조명, 바닥 표시 등)")]
        public GameObject[] turnOn;

        [Tooltip("켜질 때 비활성화할 오브젝트 (막힌 통로, 어둠 가림막 등)")]
        public GameObject[] turnOff;

        [Tooltip("0에서 번쩍 하고 밝아질 Light 2D (제단 불빛 등)")]
        public Light2D[] fadeLights;

        [NonSerialized] public bool lit;
    }

    [Header("구역 목록 (위에서부터 순서대로 해금)")]
    [SerializeField] private GuideZone[] zones;

    [Header("연출 - 불빛")]
    [Tooltip("불빛이 0에서 원래 밝기까지 가는 시간 (초)")]
    [SerializeField, Range(0f, 3f)] private float fadeDuration = 1.2f;
    [Tooltip("번쩍일 때 원래 밝기의 몇 배까지 올라갈지 (1이면 번쩍임 없음)")]
    [SerializeField, Range(1f, 4f)] private float flashMultiplier = 2f;

    [Header("연출 - 타이밍")]
    [Tooltip("퍼즐 화면이 닫힌 뒤 연출 시작까지 기다리는 시간 (초)")]
    [SerializeField, Range(0f, 2f)] private float delayAfterClose = 0.4f;
    [Tooltip("여러 구역이 한 번에 켜질 때 사이 간격 (초)")]
    [SerializeField, Range(0f, 2f)] private float gapBetweenZones = 0.6f;
    [Tooltip("안내 문구 표시 시간 (초)")]
    [SerializeField, Range(1f, 8f)] private float messageTime = 4f;

    [Header("연출 - 카메라 이동")]
    [Tooltip("카메라가 다음 제단으로 가는 시간 (초)")]
    [SerializeField, Range(0f, 3f)] private float panTime = 0.9f;
    [Tooltip("다음 제단을 비추고 있는 시간 (초)")]
    [SerializeField, Range(0f, 5f)] private float holdTime = 1.4f;
    [Tooltip("카메라가 플레이어에게 돌아오는 시간 (초)")]
    [SerializeField, Range(0f, 3f)] private float returnTime = 0.7f;

    [Header("연출 - 화면 흔들림")]
    [Tooltip("비우면 Main Camera에서 자동으로 찾음")]
    [SerializeField] private CameraFollow2D cameraFollow;
    [SerializeField, Range(0f, 2f)] private float shakeDuration = 0.6f;
    [Tooltip("흔들리는 세기 (유닛). 0.1 = 약 3픽셀")]
    [SerializeField, Range(0f, 0.5f)] private float shakeStrength = 0.12f;

    // Inspector에서 정한 원래 밝기
    private readonly Dictionary<Light2D, float> targetIntensity = new Dictionary<Light2D, float>();

    // 퍼즐 화면이 닫히길 기다리는 구역
    private readonly List<GuideZone> waiting = new List<GuideZone>();
    private bool playing;
    private bool subscribed;

    // 연출 중 잠글 플레이어
    private PlayerController2D playerController;
    private InteractionSystem playerInteraction;
    private Rigidbody2D playerRb;
    private bool playerLocked;

    // 코루틴 대기 객체 재사용 
    private WaitForSeconds waitAfterClose, waitGap, waitPan, waitHalfShake, waitHold, waitReturn;

    // ================= 초기화 =================
    private void Awake()
    {
        try
        {
            CacheWaits();
            CacheIntensities();
            DarkenAll();
            FindCamera();
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 안내 초기화 오류: {e}");
        }
    }

    private void OnEnable()
    {
        PuzzleOverlayFocus.OnFocusChanged += HandleFocusChanged;
    }

    private void OnDisable()
    {
        PuzzleOverlayFocus.OnFocusChanged -= HandleFocusChanged;
        EndCutscene();
        playing = false;
    }

    private void Start()
    {
        try
        {
            if (ItemInventory.Instance == null)
            {
                Debug.LogError($"[{name}] ItemInventory가 씬에 없음 -> 구역을 켤 수 없음 (Managers 확인)", this);
                return;
            }

            RestoreFromInventory();
            ItemInventory.Instance.OnItemGranted += HandleItemGranted;
            subscribed = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 안내 시작 오류: {e}");
        }
    }

    private void OnDestroy()
    {
        if (subscribed && ItemInventory.Instance != null)
        {
            ItemInventory.Instance.OnItemGranted -= HandleItemGranted;
        }
    }

    private void CacheWaits()
    {
        waitAfterClose = new WaitForSeconds(delayAfterClose);
        waitGap = new WaitForSeconds(gapBetweenZones);
        waitPan = new WaitForSeconds(panTime);
        waitHalfShake = new WaitForSeconds(shakeDuration * 0.5f);
        waitHold = new WaitForSeconds(holdTime);
        waitReturn = new WaitForSeconds(returnTime);
    }

    private void CacheIntensities()
    {
        if (zones == null) return;

        foreach (GuideZone zone in zones)
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
        if (cameraFollow == null) Debug.LogWarning($"[{name}] CameraFollow2D를 못 찾음 -> 카메라 이동 없이 불만 켬");
    }

    // ================= 아이템 / 퍼즐 화면 =================
    private void HandleItemGranted(string itemId)
    {
        try
        {
            foreach (GuideZone zone in zones)
            {
                if (zone != null && zone.unlockItem == itemId) RequestUnlock(zone);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 구역 해금 오류: {e}");
        }
    }

    // 퍼즐 화면이 열려 있으면 기다렸다가, 닫혀 있으면 바로 연출
    private void RequestUnlock(GuideZone zone)
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
        yield return waitAfterClose;

        while (waiting.Count > 0)
        {
            if (PuzzleOverlayFocus.IsActive) break; // 다시 퍼즐을 열면 멈췄다가 닫힐 때 이어서

            GuideZone zone = waiting[0];
            waiting.RemoveAt(0);
            yield return PlayUnlock(zone);

            if (waiting.Count > 0) yield return waitGap;
        }
        playing = false;
    }

    // 카메라 이동 -> 흔들림 -> 흔들림 중간에 불 켜기 + 안내 -> 보여주고 복귀
    private IEnumerator PlayUnlock(GuideZone zone)
    {
        bool pan = zone.focusPoint != null && cameraFollow != null;
        if (pan)
        {
            BeginCutscene(zone.focusPoint.position);
            yield return waitPan;
        }

        Shake();
        yield return waitHalfShake;

        LightZoneOn(zone, false);
        ShowMessage(zone);
        Debug.Log($"[{name}] {zone.zoneName} 불 켜짐 ({zone.unlockItem})");

        if (!pan) yield break;

        yield return waitHold;
        if (cameraFollow != null) cameraFollow.ReleaseFocus();
        yield return waitReturn;
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
        if (cameraFollow != null) cameraFollow.Shake(shakeDuration, shakeStrength);
    }

    private void ShowMessage(GuideZone zone)
    {
        if (string.IsNullOrEmpty(zone.unlockMessage)) return;
        AnubisToast.Show(zone.unlockMessage, messageTime);
    }

    // ================= 켜기 / 끄기 =================
    // 이미 가진 아이템 + 아이템 조건이 없는 구역은 연출 없이 바로 켬
    private void RestoreFromInventory()
    {
        if (zones == null) return;

        foreach (GuideZone zone in zones)
        {
            if (zone != null && IsUnlocked(zone)) LightZoneOn(zone, true);
        }
    }

    private static bool IsUnlocked(GuideZone zone)
    {
        if (string.IsNullOrEmpty(zone.unlockItem)) return true;
        return ItemInventory.Instance != null && ItemInventory.Instance.Has(zone.unlockItem);
    }

    private void LightZoneOn(GuideZone zone, bool instant)
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

        foreach (GuideZone zone in zones)
        {
            if (zone == null) continue;
            zone.lit = false;
            SetObjects(zone.turnOn, false);
            SetObjects(zone.turnOff, true);
            SetLights(zone, 0f);
        }
    }

    private IEnumerator FadeIn(GuideZone zone)
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

    private float FlashRatio(float progress)
    {
        if (progress < FlashPeak) return Mathf.Lerp(0f, flashMultiplier, progress / FlashPeak);
        return Mathf.Lerp(flashMultiplier, 1f, (progress - FlashPeak) / (1f - FlashPeak));
    }

    private void SetLights(GuideZone zone, float ratio)
    {
        if (zone.fadeLights == null) return;

        foreach (Light2D light in zone.fadeLights)
        {
            if (light == null) continue;
            if (!targetIntensity.TryGetValue(light, out float target)) target = light.intensity;
            light.enabled = true;
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

    // ================= 외부 호출 =================
    // 메인 저울 실패로 아이템을 회수한 뒤 호출하면, 지금 인벤토리 기준으로 구역을 다시 맞춤
    public void ResyncWithInventory()
    {
        StopAllCoroutines();
        EndCutscene();
        playing = false;
        waiting.Clear();
        DarkenAll();
        RestoreFromInventory();
    }

    // ================= 테스트용 =================
    [ContextMenu("테스트: 다음 구역 켜기 (연출 포함, 아이템은 안 줌)")]
    private void TestLightNext()
    {
        if (!CheckPlaying()) return;

        foreach (GuideZone zone in zones)
        {
            if (zone == null || zone.lit || waiting.Contains(zone)) continue;

            RequestUnlock(zone);
            return;
        }
        Debug.Log($"[{name}] 테스트: 이미 전부 켜져 있음");
    }

    [ContextMenu("테스트: 인벤토리 기준으로 다시 맞추기")]
    private void TestResync()
    {
        if (!CheckPlaying()) return;
        ResyncWithInventory();
        Debug.Log($"[{name}] 테스트: 지금 아이템 기준으로 다시 맞춤");
    }

    private bool CheckPlaying()
    {
        if (Application.isPlaying) return true;
        Debug.LogWarning($"[{name}] 테스트 메뉴는 Play 중에만 사용");
        return false;
    }

#if UNITY_EDITOR
    public void SetZonesForEditor(GuideZone[] newZones)
    {
        zones = newZones;
    }
#endif

    // ================= Scene 뷰 표시 =================
    // 안내 순서대로 노란 선 + 원: 플레이어가 어떤 순서로 이동하게 되는지 한눈에 확인
    private void OnDrawGizmos()
    {
        if (zones == null) return;

        Vector3? prev = null;
        foreach (GuideZone zone in zones)
        {
            if (zone?.focusPoint == null) continue;

            Vector3 p = zone.focusPoint.position;
            bool lit = Application.isPlaying && zone.lit;
            Gizmos.color = lit ? new Color(1f, 0.85f, 0.2f, 0.9f) : new Color(0.6f, 0.6f, 0.7f, 0.9f);
            Gizmos.DrawWireSphere(p, 0.6f);

            if (prev.HasValue)
            {
                Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.5f);
                Gizmos.DrawLine(prev.Value, p);
            }
            prev = p;
        }
    }
}