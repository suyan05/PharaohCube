using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

// 연출용 전체 조명 밝기 (씬의 Global Light 2D)
// - Dim(0.65f, 0.4f): 원래 밝기의 65%로 0.4초 동안 (스핑크스가 말할 때 화면 살짝 어둡게)
// - Restore(0.4f):    원래 밝기로
// Global Light 2D가 없으면 아무것도 안 함
public static class CineLight
{
    private static Light2D global;
    private static float baseIntensity = 1f;
    private static Coroutine running;

    public static void Dim(float multiplier, float seconds)
    {
        Light2D light = Global();
        if (light != null) FadeTo(light, baseIntensity * Mathf.Clamp01(multiplier), seconds);
    }

    public static void Restore(float seconds)
    {
        Light2D light = Global();
        if (light != null) FadeTo(light, baseIntensity, seconds);
    }

    // 씬이 바뀌면 예전 조명은 사라지므로 다시 찾고 그때의 밝기를 "원래 밝기"로 기억
    private static Light2D Global()
    {
        if (global != null) return global;

        running = null;
        foreach (Light2D light in Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None))
        {
            if (light.lightType != Light2D.LightType.Global) continue;
            global = light;
            baseIntensity = light.intensity;
            break;
        }
        return global;
    }

    private static void FadeTo(Light2D light, float target, float seconds)
    {
        try
        {
            CineRunner runner = CineRunner.Instance;
            if (running != null) runner.StopCoroutine(running);
            running = runner.StartCoroutine(Fade(light, target, seconds));
        }
        catch (Exception e)
        {
            Debug.LogError($"[CineLight] 조명 변경 오류: {e}");
        }
    }

    private static IEnumerator Fade(Light2D light, float target, float seconds)
    {
        float start = light.intensity;
        float t = 0f;
        while (t < seconds && light != null)
        {
            t += Time.unscaledDeltaTime;
            light.intensity = Mathf.Lerp(start, target, t / seconds);
            yield return null;
        }
        if (light != null) light.intensity = target;
        running = null;
    }
}
