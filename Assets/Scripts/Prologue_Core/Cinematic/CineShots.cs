using System;
using System.Collections;
using UnityEngine;

// 영화식 카메라 무빙 모음 (CineCamera 위에 얹어 쓰는 것). 전부 Cine.Begin ~ Cine.End 사이에서 씀
//   yield return CineShots.Glide(A, B, 2f);              // 팬/틸트/트래킹: 보는 곳이 A -> B 로 미끄러짐
//   yield return CineShots.Crane(A, B, 3f, 7f, 2.5f);    // 크레인: 움직이면서 동시에 멀어지거나 다가감
//   yield return CineShots.Dolly(3.2f, 1.5f);            // 돌리 인/아웃: 제자리에서 다가가거나 물러남
//   yield return CineShots.Handheld(P, 1.5f, 0.12f);     // 핸드헬드: 손으로 든 것처럼 살짝 흔들리며 P를 봄
//   yield return CineShots.Roll(-6f, 0.6f);              // 롤: 화면이 기울어짐 (불안, 추락)
//   yield return CineShots.Vertigo(P, 2.6f, 0.9f);       // 급하게 빨려 들어가며 기울어짐 (충격 순간)
// - 연출이 끝나면(Cine.End) 기울기는 자동으로 0으로 돌아옴
// - 탑다운 2D(직교 카메라)라서 로우 앵글, 랙 포커스, 진짜 돌리 줌(원근 왜곡)은 못 함
//   -> 그 느낌은 SideWalkStage(옆에서 본 통로)와 MuralStage(등 뒤 시점 벽화)로 대신함
public static class CineShots
{
    private static bool hooked;

    private static void Hook()
    {
        if (hooked) return;
        hooked = true;
        Cine.Ended += ResetRoll;
    }

    // ================= 팬 / 틸트 / 트래킹 =================
    public static IEnumerator Glide(Vector3 from, Vector3 to, float seconds)
    {
        float t = 0f;
        while (t < seconds && !Cine.Skipping)
        {
            t += Time.deltaTime;
            CineCamera.Focus(Vector3.Lerp(from, to, Ease(t / Mathf.Max(0.01f, seconds))));
            yield return null;
        }
        CineCamera.Focus(to);
    }

    // ================= 크레인 (이동 + 줌 동시에) =================
    public static IEnumerator Crane(Vector3 from, Vector3 to, float sizeFrom, float sizeTo, float seconds)
    {
        float t = 0f;
        while (t < seconds && !Cine.Skipping)
        {
            t += Time.deltaTime;
            float e = Ease(t / Mathf.Max(0.01f, seconds));
            CineCamera.Focus(Vector3.Lerp(from, to, e));
            CineCamera.Zoom(Mathf.Lerp(sizeFrom, sizeTo, e), 0.08f);
            yield return null;
        }
        CineCamera.Focus(to);
        CineCamera.Zoom(sizeTo, 0.08f);
    }

    // ================= 돌리 인 / 아웃 =================
    public static IEnumerator Dolly(float size, float seconds)
    {
        CineCamera.Zoom(size, Mathf.Max(0.05f, seconds * 0.45f));
        yield return Cine.Wait(seconds);
    }

    // ================= 핸드헬드 =================
    public static IEnumerator Handheld(Vector3 center, float seconds, float amount)
    {
        float t = 0f;
        float seed = UnityEngine.Random.value * 50f;
        while (t < seconds && !Cine.Skipping)
        {
            t += Time.deltaTime;
            float x = (Mathf.PerlinNoise(seed, t * 1.7f) - 0.5f) * 2f * amount;
            float y = (Mathf.PerlinNoise(seed + 9f, t * 2.1f) - 0.5f) * 2f * amount;
            CineCamera.Focus(center + new Vector3(x, y, 0f));
            yield return null;
        }
        CineCamera.Focus(center);
    }

    // ================= 롤 (기울기) =================
    public static IEnumerator Roll(float toAngle, float seconds)
    {
        Hook();
        Transform cam = CamTransform();
        if (cam == null) yield break;
        float from = Mathf.DeltaAngle(0f, cam.eulerAngles.z);
        float t = 0f;
        while (t < seconds && !Cine.Skipping && cam != null)
        {
            t += Time.deltaTime;
            SetRoll(cam, Mathf.Lerp(from, toAngle, Ease(t / Mathf.Max(0.01f, seconds))));
            yield return null;
        }
        if (cam != null) SetRoll(cam, Cine.Skipping ? 0f : toAngle);
    }

    public static void ResetRoll()
    {
        try
        {
            Transform cam = CamTransform();
            if (cam != null) SetRoll(cam, 0f);
        }
        catch (Exception e)
        {
            Debug.LogError($"[CineShots] 기울기 되돌리기 오류: {e.Message}");
        }
    }

    // ================= 충격 순간: 확 다가가며 기울어짐 =================
    public static IEnumerator Vertigo(Vector3 point, float size, float seconds)
    {
        CineCamera.Focus(point);
        CineCamera.Zoom(size, Mathf.Max(0.05f, seconds * 0.3f));
        CineCamera.Shake(seconds, 0.05f);
        yield return Roll(4f, seconds * 0.5f);
        yield return Roll(0f, seconds * 0.5f);
    }

    // ================= 도우미 =================
    private static Transform CamTransform()
    {
        Camera cam = Camera.main;
        return cam != null ? cam.transform : null;
    }

    private static void SetRoll(Transform cam, float angle)
    {
        Vector3 e = cam.eulerAngles;
        cam.rotation = Quaternion.Euler(e.x, e.y, angle);
    }

    private static float Ease(float k)
    {
        return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(k));
    }
}
