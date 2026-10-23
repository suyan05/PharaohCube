using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 라의 방 어둠 (씬에 1개. 메뉴로 붙이거나, 없으면 RaRuntimeBootstrap이 실행 중에 붙임)
// - 낮: 전역 조명을 아주 어둡게 (벽 윤곽만 희미하게)
// - 밤: 전역 조명 0 = 완전 깜깜. 내 횃불, 켜진 벽 횃불, 화로, 숨은 불빛만 보임
// - 매 프레임 마지막(LateUpdate)에 밝기를 다시 맞춤 -> 씬에 저장된 값, 밤 분위기 스크립트 값이 뭐든 이 값이 이김
//   (밤으로 바뀔 때는 nightFade초 동안 서서히 어두워짐. 색은 RaNightAtmosphere가 그대로 바꿈)
// - 실행 중에 만드는 빛 덩어리, 불똥은 어둠 속에서도 보여야 해서 Unlit 재질을 여기서 나눠 줌 (RaDarkness.Unlit)
[DefaultExecutionOrder(-500)]
public class RaDarkness : MonoBehaviour
{
    [Header("밝기 (0 = 완전 깜깜)")]
    [SerializeField, Range(0f, 1f)] private float dayIntensity = 0.03f;
    [SerializeField, Range(0f, 1f)] private float nightIntensity = 0f;
    [SerializeField] private Color dayColor = new Color(0.55f, 0.58f, 0.8f);
    [SerializeField, Min(0.1f)] private float nightFade = 2.5f;

    [Header("어둠 속에서도 보이는 그림용 재질 (Sprite-Unlit-Default)")]
    [SerializeField] private Material unlitMaterial;

    private static Material unlit;
    private Light2D[] globals = new Light2D[0];
    private float current = -1f;

    public static Material Unlit
    {
        get
        {
            if (unlit == null) unlit = FindUnlit();
            return unlit;
        }
    }

    private float Target => RaPhaseState.IsNight ? nightIntensity : dayIntensity;

    private void Awake()
    {
        try
        {
            if (unlitMaterial != null) unlit = unlitMaterial;
            CollectGlobals();
            current = Target;
            Apply(true);
        }
        catch (Exception e)
        {
            Debug.LogError($"[RaDarkness] 어둠 적용 오류: {e}");
        }
    }

    private void LateUpdate()
    {
        float speed = Mathf.Max(dayIntensity, 0.05f) / nightFade;
        current = Mathf.MoveTowards(current, Target, speed * Time.deltaTime);
        Apply(false);
    }

    private void CollectGlobals()
    {
        var list = new System.Collections.Generic.List<Light2D>();
        foreach (Light2D light in FindObjectsByType<Light2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (light.lightType == Light2D.LightType.Global) list.Add(light);
        }
        globals = list.ToArray();
        if (globals.Length == 0) Debug.LogWarning("[RaDarkness] Global Light 2D가 없음 -> 어둡게 못 함");
    }

    // 색은 낮에만 맞춤 (밤 색은 RaNightAtmosphere 담당)
    private void Apply(bool setColor)
    {
        foreach (Light2D light in globals)
        {
            if (light == null) continue;
            light.intensity = current;
            if (setColor && !RaPhaseState.IsNight) light.color = dayColor;
        }
    }

    private static Material FindUnlit()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null) Debug.LogWarning("[RaDarkness] Sprite-Unlit-Default를 못 찾음 -> 빛 덩어리가 어둡게 보일 수 있음");
        return shader != null ? new Material(shader) : null;
    }
}
