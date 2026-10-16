using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 라의 방 밤 분위기 (씬에 1개, 빈 오브젝트에 붙임)
// - F_RA_NIGHT_OPEN이 켜지면 몇 초에 걸쳐 색이 바뀜
//   전역 조명(Global Light 2D) -> 푸른 라벤더 톤 / 횃불 같은 다른 조명 -> 푸른 불꽃
//   불꽃 스프라이트 등은 색만 푸르게
// - 어둡게 누르는 게 아니라 색을 바꾸는 방식이라 맵이 계속 잘 보임
// - 씬에 다시 들어왔을 때 이미 밤이면 연출 없이 바로 밤 색
// - 밝기(intensity)는 전역 조명만 바꿈. 다른 조명은 색만 (RaZoneLighting의 밝기 연출과 안 부딪치게)
public class RaNightAtmosphere : MonoBehaviour
{
    [Header("전역 조명 (Global Light 2D, 비우면 자동으로 찾음)")]
    [SerializeField] private Light2D globalLight;
    [SerializeField] private Color nightGlobalColor = new Color(0.55f, 0.62f, 0.95f);
    [Tooltip("밤 전역 밝기. 너무 어두우면 올리고, 너무 밝으면 내리기")]
    [SerializeField, Min(0f)] private float nightGlobalIntensity = 0.8f;

    [Header("횃불 등 다른 조명 (색만 바뀜)")]
    [Tooltip("켜면 씬의 모든 Light 2D(꺼진 것 포함)를 자동으로 모음 (전역 조명 제외)")]
    [SerializeField] private bool autoFindLights = true;
    [Tooltip("자동으로 못 찾는 조명이 있으면 여기에 추가")]
    [SerializeField] private Light2D[] extraLights;
    [Tooltip("자동으로 모은 것 중 밤에도 색을 바꾸지 않을 조명 (예: 회로판 파란 빛)")]
    [SerializeField] private Light2D[] excludeLights;
    [SerializeField] private Color nightLightColor = new Color(0.45f, 0.7f, 1f);

    [Header("불꽃 스프라이트 등 색만 바꿀 것 (선택)")]
    [SerializeField] private SpriteRenderer[] tintSprites;
    [SerializeField] private Color nightSpriteTint = new Color(0.6f, 0.8f, 1f);

    [Header("전환")]
    [Tooltip("낮 색 -> 밤 색으로 바뀌는 시간 (초)")]
    [SerializeField, Min(0f)] private float transitionTime = 2.5f;

    private class LightState
    {
        public Light2D light;
        public Color dayColor;
    }

    private class SpriteState
    {
        public SpriteRenderer renderer;
        public Color dayColor;
    }

    private readonly List<LightState> lights = new List<LightState>();
    private readonly List<SpriteState> sprites = new List<SpriteState>();
    private Color dayGlobalColor = Color.white;
    private float dayGlobalIntensity = 1f;
    private bool subscribed;
    private bool appliedNight;

    // ================= 초기화 =================
    // 낮 색은 Awake에서 저장 (Inspector에 적힌 원래 색)
    private void Awake()
    {
        try
        {
            CollectGlobalLight();
            CollectLights();
            CollectSprites();
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 밤 분위기 초기화 오류: {e}");
        }
    }

    private void Start()
    {
        try
        {
            if (GameManager.Instance == null)
            {
                Debug.LogWarning($"[{name}] GameManager가 없어서 밤 전환을 못 받음");
                return;
            }

            GameManager.Instance.OnFlagChanged += HandleFlagChanged;
            subscribed = true;

            if (RaPhaseState.IsNight) ApplyNight(true);
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 밤 분위기 구독 오류: {e}");
        }
    }

    private void OnDestroy()
    {
        if (subscribed && GameManager.Instance != null) GameManager.Instance.OnFlagChanged -= HandleFlagChanged;
    }

    private void CollectGlobalLight()
    {
        if (globalLight == null)
        {
            foreach (Light2D l in FindObjectsByType<Light2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (l.lightType != Light2D.LightType.Global) continue;
                globalLight = l;
                break;
            }
        }

        if (globalLight == null)
        {
            Debug.LogWarning($"[{name}] Global Light 2D를 못 찾음 -> 전역 색은 안 바뀜");
            return;
        }
        dayGlobalColor = globalLight.color;
        dayGlobalIntensity = globalLight.intensity;
    }

    private void CollectLights()
    {
        var seen = new HashSet<Light2D>();
        var excluded = new HashSet<Light2D>();
        if (excludeLights != null) foreach (Light2D l in excludeLights) if (l != null) excluded.Add(l);

        if (autoFindLights)
        {
            foreach (Light2D l in FindObjectsByType<Light2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                AddLight(l, seen, excluded);
            }
        }

        if (extraLights == null) return;
        foreach (Light2D l in extraLights) AddLight(l, seen, excluded);
    }

    private void AddLight(Light2D l, HashSet<Light2D> seen, HashSet<Light2D> excluded)
    {
        if (l == null || l == globalLight || excluded.Contains(l)) return;
        if (l.lightType == Light2D.LightType.Global) return;
        if (!seen.Add(l)) return;

        lights.Add(new LightState { light = l, dayColor = l.color });
    }

    private void CollectSprites()
    {
        if (tintSprites == null) return;

        foreach (SpriteRenderer sr in tintSprites)
        {
            if (sr != null) sprites.Add(new SpriteState { renderer = sr, dayColor = sr.color });
        }
    }

    // ================= 밤 전환 =================
    private void HandleFlagChanged(string flag)
    {
        if (flag != RaPhaseState.NightFlag) return;
        ApplyNight(false);
    }

    private void ApplyNight(bool instant)
    {
        if (appliedNight) return;
        appliedNight = true;

        if (instant || transitionTime <= 0f || !isActiveAndEnabled)
        {
            Blend(1f);
            return;
        }
        StartCoroutine(Transition());
    }

    private IEnumerator Transition()
    {
        float t = 0f;
        while (t < transitionTime)
        {
            t += Time.deltaTime;
            Blend(Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / transitionTime)));
            yield return null;
        }
        Blend(1f);
    }

    // amount 0 = 낮 색, 1 = 밤 색
    private void Blend(float amount)
    {
        try
        {
            if (globalLight != null)
            {
                globalLight.color = Color.Lerp(dayGlobalColor, nightGlobalColor, amount);
                globalLight.intensity = Mathf.Lerp(dayGlobalIntensity, nightGlobalIntensity, amount);
            }

            foreach (LightState s in lights)
            {
                if (s.light != null) s.light.color = Color.Lerp(s.dayColor, nightLightColor, amount);
            }

            foreach (SpriteState s in sprites)
            {
                if (s.renderer != null) s.renderer.color = Color.Lerp(s.dayColor, nightSpriteTint, amount);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 밤 색 적용 오류: {e}");
        }
    }

    // ================= 테스트용 (Play 중에만) =================
    // 밤 플래그는 안 켜고 색만 미리 보기 (색 고를 때 Inspector 값 바꾸고 다시 누르면 됨)
    [ContextMenu("테스트: 밤 색 미리 보기 (색만, 플래그 X)")]
    private void TestPreviewNight()
    {
        if (!Application.isPlaying) return;
        StopAllCoroutines();
        Blend(1f);
    }

    [ContextMenu("테스트: 낮 색으로 되돌리기 (색만)")]
    private void TestPreviewDay()
    {
        if (!Application.isPlaying) return;
        StopAllCoroutines();
        Blend(0f);
        appliedNight = false;
    }
}