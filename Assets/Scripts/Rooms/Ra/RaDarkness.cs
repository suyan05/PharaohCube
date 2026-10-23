using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 라의 방 어둠 (씬에 1개, RaTemple에 붙음)
// - 전역 조명(Global Light 2D)을 아주 어둡게 -> 내 횃불, 벽 횃불, 숨은 불빛만 보임
// - 다른 스크립트보다 먼저 실행(-500): RaNightAtmosphere가 "낮 밝기"를 저장하기 전에 먼저 어둡게 맞춤
// - 실행 중에 만드는 빛 덩어리, 불똥은 어둠 속에서도 보여야 해서 Unlit 재질을 여기서 나눠 줌 (RaDarkness.Unlit)
[DefaultExecutionOrder(-500)]
public class RaDarkness : MonoBehaviour
{
    [Header("낮 어둠 (밤 밝기는 RaNightAtmosphere의 Night Global Intensity)")]
    [Tooltip("0 = 완전 깜깜. 0.05 정도면 벽 윤곽만 아주 희미하게")]
    [SerializeField, Range(0f, 1f)] private float dayIntensity = 0.05f;
    [SerializeField] private Color dayColor = new Color(0.55f, 0.58f, 0.8f);

    [Header("어둠 속에서도 보이는 그림용 재질 (Sprite-Unlit-Default)")]
    [SerializeField] private Material unlitMaterial;

    private static Material unlit;

    public static Material Unlit
    {
        get
        {
            if (unlit == null) unlit = FindUnlit();
            return unlit;
        }
    }

    private void Awake()
    {
        try
        {
            if (unlitMaterial != null) unlit = unlitMaterial;
            ApplyGlobal();
        }
        catch (Exception e)
        {
            Debug.LogError($"[RaDarkness] 어둠 적용 오류: {e}");
        }
    }

    private void ApplyGlobal()
    {
        int count = 0;
        foreach (Light2D light in FindObjectsByType<Light2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (light.lightType != Light2D.LightType.Global) continue;
            light.intensity = dayIntensity;
            light.color = dayColor;
            count++;
        }
        if (count == 0) Debug.LogWarning("[RaDarkness] Global Light 2D가 없음 -> 어둡게 못 함");
    }

    private static Material FindUnlit()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null) Debug.LogWarning("[RaDarkness] Sprite-Unlit-Default를 못 찾음 -> 빛 덩어리가 어둡게 보일 수 있음");
        return shader != null ? new Material(shader) : null;
    }
}
