using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// 횃불 빛을 "좁고 진하게"에서 "넓고 은은하게"로 (라의 방, 호루스의 방 공통)
// - 씬이 열릴 때 벽 횃불(WallTorch)과 호루스 장식 횃대의 빛 반지름을 넓히고, 가장자리로 갈수록 부드럽게 옅어지게 함
// - 퍼즐 판정에 쓰는 밝은 범위(WallTorch의 Bright Radius)는 안 건드림. 보이는 빛만 바뀜
// - 한 번 넓힌 빛에는 표시를 남겨서 두 번 넓히지 않음
public static class TorchGlowTune
{
    private const float RadiusScale = 1.6f;
    private const float Falloff = 0.85f;
    private const string Mark = "GlowTuned";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Apply();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Apply();

    private static void Apply()
    {
        try
        {
            foreach (WallTorch t in UnityEngine.Object.FindObjectsByType<WallTorch>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Widen(t.GetComponentInChildren<Light2D>(true));
            foreach (HorusPixelFlame f in UnityEngine.Object.FindObjectsByType<HorusPixelFlame>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Widen(f.GetComponentInChildren<Light2D>(true));
        }
        catch (Exception e)
        {
            Debug.LogError($"[횃불 빛 조정] 오류: {e.Message}");
        }
    }

    public static void Widen(Light2D light)
    {
        if (light == null || light.lightType != Light2D.LightType.Point) return;
        if (light.transform.Find(Mark) != null) return;
        light.pointLightOuterRadius *= RadiusScale;
        light.pointLightInnerRadius = Mathf.Max(light.pointLightInnerRadius, light.pointLightOuterRadius * 0.12f);
        light.falloffIntensity = Falloff;
        new GameObject(Mark).transform.SetParent(light.transform, false);
    }
}
