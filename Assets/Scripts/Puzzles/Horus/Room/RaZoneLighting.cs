using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 라의 방 "빛의 길" (신전 발굴 지도 v1)
// - 구역마다 해금 플래그를 정하고, 플래그가 켜지는 순간 그 구역 횃불을 서서히 켬
// - 해금 전 구역은 어둡게 (횃불 꺼짐 + 어둠 덮개 켜짐)
// - 씬에 다시 들어왔을 때 이미 켜진 플래그는 연출 없이 바로 켜진 상태로 복원
public class RaZoneLighting : MonoBehaviour
{
    [Serializable]
    public class LightZone
    {
        [Tooltip("Inspector에서 알아보기 쉬운 이름 (예: 동쪽 날개 P1)")]
        public string zoneName = "구역";

        [Tooltip("이 플래그가 켜지면 불이 들어옴. 비우면 처음부터 켜짐")]
        public string unlockFlag = "";

        [Tooltip("해금 때 켤 오브젝트 (횃불 묶음 등)")]
        public GameObject[] turnOn;

        [Tooltip("해금 때 끌 오브젝트 (어둠 덮개 등, 없으면 비워도 됨)")]
        public GameObject[] turnOff;

        [Tooltip("서서히 밝아질 Light2D (turnOn 안에 있는 횃불 빛)")]
        public Light2D[] fadeLights;

        [NonSerialized] public bool lit;
    }

    [Header("구역 목록 (위에서부터 해금 순서대로)")]
    [SerializeField] private LightZone[] zones;

    [Header("연출")]
    [Tooltip("횃불이 0에서 원래 밝기까지 켜지는 시간 (초)")]
    [SerializeField] private float fadeDuration = 1.2f;

    // Inspector에 적힌 원래 밝기 (서서히 켤 때 목표값)
    private readonly Dictionary<Light2D, float> targetIntensity = new Dictionary<Light2D, float>();
    private bool subscribed;

    // ================= 초기화 =================
    private void Awake()
    {
        try
        {
            CacheIntensities();
            DarkenAll();
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 조명 초기화 오류: {e}");
        }
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

    // ================= 플래그 =================
    private void HandleFlagChanged(string flag)
    {
        try
        {
            foreach (LightZone zone in zones)
            {
                if (zone == null || zone.unlockFlag != flag) continue;

                LightZoneOn(zone, false);
                Debug.Log($"[{name}] {zone.zoneName} 불 켜짐 ({flag})");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 조명 전환 오류: {e}");
        }
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
            SetLights(zone, Mathf.Clamp01(t / fadeDuration));
            yield return null;
        }
        SetLights(zone, 1f);
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
    [ContextMenu("테스트: 다음 구역 불만 켜기 (플래그는 안 켬)")]
    private void TestLightNext()
    {
        if (!CheckPlaying()) return;

        foreach (LightZone zone in zones)
        {
            if (zone == null || zone.lit) continue;

            LightZoneOn(zone, false);
            Debug.Log($"[{name}] 테스트: {zone.zoneName} 불 켜짐");
            return;
        }
        Debug.Log($"[{name}] 테스트: 이미 전부 켜져 있음");
    }

    [ContextMenu("테스트: 플래그 기준으로 다시 맞추기")]
    private void TestResetToFlags()
    {
        if (!CheckPlaying()) return;

        StopAllCoroutines();
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