using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 아펩의 그림자가 화면을 스치고 지나감 (최종 스토리: 발굴 현장이 무너지는 순간 아펩 첫 등장)
// - yield return ApepShadow.Sweep(초);   그림: Resources/Horus/Mural/MU_Apep_shadow (보랏빛 뱀 실루엣)
// - 대사창(100)보다 위, 검은 화면(ScreenFader 200)보다 아래(150)에 그림
public static class ApepShadow
{
    private const string ArtPath = "Horus/Mural/MU_Apep_shadow";
    private const int SortOrder = 150;

    public static IEnumerator Sweep(float seconds)
    {
        if (Cine.Skipping) yield break;
        Image img = Build(out GameObject canvasGo);
        if (img != null) yield return Slide(img, seconds);
        if (canvasGo != null) UnityEngine.Object.Destroy(canvasGo);
    }

    private static Image Build(out GameObject canvasGo)
    {
        canvasGo = null;
        try
        {
            Sprite s = Resources.Load<Sprite>(ArtPath);
            if (s == null)
            {
                Debug.LogWarning($"[아펩 그림자] 그림 없음: {ArtPath}");
                return null;
            }
            RectTransform root = UIBuild.CreateCanvas(null, "ApepShadowCanvas", SortOrder, out canvasGo);
            Image img = UIBuild.Image(root, "Apep", new Vector2(2200f, 0f), new Vector2(2400f, 1350f), s, Color.white);
            img.preserveAspect = false;
            img.color = new Color(1f, 1f, 1f, 0f);
            return img;
        }
        catch (Exception e)
        {
            Debug.LogError($"[아펩 그림자] 준비 오류: {e}");
            return null;
        }
    }

    // 오른쪽 밖 -> 왼쪽 밖으로 미끄러지며, 가운데에서 가장 진하게
    private static IEnumerator Slide(Image img, float seconds)
    {
        GameAudio.Play("sfx_dream_shimmer", 0.6f, 0.5f);
        float t = 0f;
        while (t < seconds && !Cine.Skipping && img != null)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / seconds);
            img.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(2200f, -2200f, k), Mathf.Sin(k * Mathf.PI * 2f) * 40f);
            img.color = new Color(1f, 1f, 1f, Mathf.Sin(k * Mathf.PI) * 0.85f);
            yield return null;
        }
    }
}
