using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// 꿈 화면 효과 (URP 후처리 Volume을 코드로 움직임)
// - 평소: 화면이 아주 천천히 숨 쉬듯 일렁임 (Lens Distortion) + 은은한 번짐(Bloom) + 보라 테두리(Vignette)
// - SetHaze(0~1) / HazeTo(값, 초): 1이면 막 깨어난 눈처럼 뿌옇고 번지고 색이 갈라짐, 0이면 또렷
// - Ripple(초, 세기): 화면이 물결치듯 출렁 (꿈 전환, 땅울림)
// 이 오브젝트에 Global Volume + 프로필(Bloom, Vignette, Chromatic Aberration, Lens Distortion)이 있어야 함
// -> 배치 도구가 다 만들어 줌. Main Camera의 Post Processing도 켜져 있어야 보임
[RequireComponent(typeof(Volume))]
public class DreamFX : MonoBehaviour
{
    private static DreamFX instance;

    [Header("평소 값")]
    [SerializeField] private float baseDistortion = -0.06f;
    [SerializeField] private float baseChroma = 0.12f;
    [SerializeField] private float baseBloom = 1.1f;
    [SerializeField] private float baseVignette = 0.4f;

    [Header("숨 쉬기 (일렁임)")]
    [SerializeField] private float breathSeconds = 6f;
    [SerializeField] private float breathAmount = 0.05f;

    private LensDistortion lens;
    private ChromaticAberration chroma;
    private Bloom bloom;
    private Vignette vignette;
    private float haze;
    private float rippleTime, rippleDuration, rippleStrength;

    // ================= 밖에서 부르는 함수 =================
    public static void SetHaze(float value)
    {
        if (instance != null) instance.haze = Mathf.Clamp01(value);
    }

    public static IEnumerator HazeTo(float target, float seconds)
    {
        if (instance == null) yield break;

        float start = instance.haze;
        float t = 0f;
        while (t < seconds && instance != null && !Cine.Skipping)
        {
            t += Time.deltaTime;
            instance.haze = Mathf.Lerp(start, target, t / seconds);
            yield return null;
        }
        SetHaze(target);
    }

    public static void Ripple(float seconds, float strength)
    {
        if (instance == null || seconds <= 0f) return;
        instance.rippleDuration = instance.rippleTime = seconds;
        instance.rippleStrength = strength;
    }

    // ================= 내부 =================
    private void Awake()
    {
        instance = this;
        try
        {
            VolumeProfile p = GetComponent<Volume>().profile; // 실행 중 복사본 (원본 에셋은 안 바뀜)
            p.TryGet(out lens);
            p.TryGet(out chroma);
            p.TryGet(out bloom);
            p.TryGet(out vignette);
            if (lens == null || bloom == null) Debug.LogWarning("[DreamFX] Volume 프로필에 Lens Distortion / Bloom이 없음 -> 배치 도구로 다시 만들기");
        }
        catch (Exception e)
        {
            Debug.LogError($"[DreamFX] 후처리 준비 오류: {e}");
        }
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Update()
    {
        float breath = Mathf.Sin(Time.time * Mathf.PI * 2f / Mathf.Max(0.5f, breathSeconds)) * breathAmount;
        float ripple = RippleValue();

        if (lens != null) lens.intensity.value = Mathf.Clamp(baseDistortion + breath + ripple - haze * 0.3f, -1f, 1f);
        if (chroma != null) chroma.intensity.value = Mathf.Clamp01(baseChroma + haze * 0.85f + Mathf.Abs(ripple) * 1.5f);
        if (bloom != null) bloom.intensity.value = baseBloom + haze * 3f;
        if (vignette != null) vignette.intensity.value = Mathf.Clamp01(baseVignette + haze * 0.3f);
    }

    // 시작할 때 크게 출렁이다가 점점 잦아듦
    private float RippleValue()
    {
        if (rippleTime <= 0f) return 0f;

        rippleTime -= Time.deltaTime;
        float p = 1f - Mathf.Clamp01(rippleTime / rippleDuration);
        return Mathf.Sin(p * Mathf.PI * 4f) * (1f - p) * rippleStrength;
    }
}
