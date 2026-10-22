using System;
using System.Collections;
using UnityEngine;

// 연출용 카메라 조작 (Main Camera의 CameraFollow2D 기능을 짧게 부르는 것)
// - Focus(위치): 플레이어 대신 그곳을 비춤 / Release(): 다시 플레이어 + 원래 줌
// - Zoom(크기, 걸리는 정도): Size가 작을수록 가까이 (평소 6)
// - Shake(초, 세기) / Rumble(초, 시작 세기, 끝 세기): 점점 강해지는 흔들림
// CameraFollow2D가 없으면 경고만 하고 넘어감 (연출이 멈추지 않게)
public static class CineCamera
{
    private static bool warned;

    private static CameraFollow2D Follow
    {
        get
        {
            Camera cam = Camera.main;
            CameraFollow2D follow = cam != null ? cam.GetComponent<CameraFollow2D>() : null;
            if (follow == null && !warned) Debug.LogWarning("[CineCamera] Main Camera에 CameraFollow2D가 없음 -> 카메라 연출 생략");
            warned = follow == null;
            return follow;
        }
    }

    public static void Focus(Vector3 worldPoint)
    {
        Call(f => f.FocusOn(worldPoint));
    }

    public static void Release()
    {
        Call(f =>
        {
            f.ReleaseFocus();
            f.ResetZoom(Cine.Skipping ? 0.05f : 1.2f);
        });
    }

    public static void Zoom(float size, float smooth = -1f)
    {
        Call(f => f.SetZoom(size, Cine.Skipping ? 0.05f : smooth));
    }

    public static void SnapZoom()
    {
        Call(f => f.SnapZoom());
    }

    public static void Shake(float seconds, float strength)
    {
        Call(f => f.Shake(seconds, strength));
    }

    // 맵 전체가 들어오는 Size (Bounds Tilemap이 없으면 fallback)
    public static float WideSize(float padding, float fallback)
    {
        CameraFollow2D f = Follow;
        return f != null && f.HasMapBounds ? f.GetFitMapSize(padding) : fallback;
    }

    public static Vector3 MapCenter(Vector3 fallback)
    {
        CameraFollow2D f = Follow;
        return f != null && f.HasMapBounds ? f.GetMapCenter() : fallback;
    }

    // seconds 동안 from -> to 세기로 계속 흔들림 (붕괴, 땅울림)
    public static IEnumerator Rumble(float seconds, float from, float to)
    {
        float t = 0f;
        while (t < seconds && !Cine.Skipping)
        {
            t += Time.deltaTime;
            Shake(0.12f, Mathf.Lerp(from, to, t / Mathf.Max(0.01f, seconds)));
            yield return null;
        }
    }

    private static void Call(Action<CameraFollow2D> action)
    {
        try
        {
            CameraFollow2D f = Follow;
            if (f != null) action(f);
        }
        catch (Exception e)
        {
            Debug.LogError($"[CineCamera] 카메라 연출 오류: {e}");
        }
    }
}
