using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 아펩 첫 등장 (최종 스토리: 발굴 현장이 무너지는 순간) - yield return ApepShadow.Sweep(초);
// [수정 2] "너무 빨리 지나가고 뜬금없다" -> 3박자로 느리게, 무슨 일인지 알 수 있게 자막을 붙임
//   1박 (약 15%): 화면이 천천히 어두워짐 + 낮은 울림. 아직 아무것도 안 보임 (떨어지는 중이라는 느낌)
//   2박 (약 60%): 거대한 뱀이 똬리를 그리며 "천천히, 같은 빠르기로" 화면을 가로지름 + 자막
//   3박 (약 25%): 암전 -> 어둠 속에서 눈이 뜨이고 세로 동공이 좁아짐 -> 암전
// - 대사창(100)보다 위, 검은 화면(ScreenFader 200)보다 아래(150)에 그림
// - 그림: Resources/Horus/Mural/MU_Apep_seg, MU_Apep_head, MU_Apep_eye, MU_Apep_pupil
public static class ApepShadow
{
    private const string ArtPath = "Horus/Mural/MU_Apep_";
    private const int SortOrder = 150;
    private const string Caption = "모래 아래에서, 아주 오래 잠들어 있던 것이 몸을 뒤척인다.";
    private static readonly Color Veil = new Color(0.07f, 0.03f, 0.1f, 1f);
    private static readonly Color BodyTint = new Color(0.66f, 0.52f, 0.74f, 1f); // 어둠 속이라 금빛을 죽이고 보랏빛으로

    public static IEnumerator Sweep(float seconds)
    {
        if (Cine.Skipping) yield break;
        RectTransform root = Build(out GameObject canvasGo, out Image veil, out ApepSerpent serpent, out Text caption);
        if (root != null)
        {
            yield return Darken(veil, Mathf.Max(0.5f, seconds * 0.15f));
            if (serpent != null) yield return Crawl(serpent, caption, Mathf.Max(1.5f, seconds * 0.6f));
            yield return EyeCut(root, Mathf.Max(1f, seconds * 0.25f));
        }
        if (canvasGo != null) UnityEngine.Object.Destroy(canvasGo);
    }

    private static RectTransform Build(out GameObject canvasGo, out Image veil, out ApepSerpent serpent, out Text caption)
    {
        canvasGo = null;
        veil = null;
        serpent = null;
        caption = null;
        try
        {
            RectTransform root = UIBuild.CreateCanvas(null, "ApepShadowCanvas", SortOrder, out canvasGo);
            veil = UIBuild.Image(root, "Veil", Vector2.zero, new Vector2(6000f, 4000f), null, Veil);
            veil.color = new Color(Veil.r, Veil.g, Veil.b, 0f);
            serpent = ApepSerpent.Create(root, BodyTint);
            Font font = PuzzleUITheme.GetBodyFont();
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            caption = UIBuild.Text(root, "Caption", font, 36, new Vector2(0f, -440f), new Vector2(1500f, 60f), new Color(0.92f, 0.86f, 1f, 0f));
            caption.text = Caption;
            return root;
        }
        catch (Exception e)
        {
            Debug.LogError($"[아펩] 준비 오류: {e}");
            return null;
        }
    }

    // 1박: 어두워지기만 함
    private static IEnumerator Darken(Image veil, float seconds)
    {
        GameAudio.Play("sfx_rumble", 0.5f, 0.6f);
        float t = 0f;
        while (t < seconds && !Cine.Skipping && !Cine.Hurry && veil != null)
        {
            t += Time.deltaTime;
            veil.color = new Color(Veil.r, Veil.g, Veil.b, Mathf.Clamp01(t / seconds) * 0.82f);
            yield return null;
        }
    }

    // 2박: 뱀이 일정한 빠르기로 천천히 기어감. 자막은 서서히 떴다가 끝에 사라짐
    private static IEnumerator Crawl(ApepSerpent serpent, Text caption, float seconds)
    {
        GameAudio.Play("sfx_dream_shimmer", 0.6f, 0.45f);
        float travel = serpent.PathLength * 0.5f;
        float t = 0f;
        while (t < seconds && !Cine.Skipping && !Cine.Hurry && serpent != null)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / seconds);
            serpent.SetHead(200f + travel * k);
            float a = Mathf.Clamp01(Mathf.Min((t - 0.4f) / 0.6f, (seconds - t) / 0.5f));
            if (caption != null) caption.color = new Color(0.92f, 0.86f, 1f, a);
            CineCamera.Shake(0.1f, 0.025f);
            yield return null;
        }
        if (serpent != null) UnityEngine.Object.Destroy(serpent.gameObject);
        if (caption != null) UnityEngine.Object.Destroy(caption.gameObject);
    }

    // 3박: 암전 위에서 눈이 뜨임
    private static IEnumerator EyeCut(RectTransform root, float seconds)
    {
        if (Cine.Skipping) yield break;
        if (!BuildEye(root, out RectTransform eye, out RectTransform pupil, out RectTransform lidTop, out RectTransform lidBottom)) yield break;
        yield return Cine.Wait(0.35f); // 완전한 어둠으로 한 박자 쉼
        GameAudio.Play("sfx_sphinx_voice", 0.5f, 0.5f);
        float t = 0f;
        while (t < seconds && !Cine.Skipping && !Cine.Hurry)
        {
            t += Time.deltaTime;
            float open = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.45f));   // 눈꺼풀이 열림
            lidTop.anchoredPosition = new Vector2(0f, 540f + 560f * open);
            lidBottom.anchoredPosition = new Vector2(0f, -540f - 560f * open);
            pupil.localScale = new Vector3(Mathf.Lerp(1.7f, 0.42f, Mathf.Clamp01((t - 0.4f) / 0.35f)), 1f, 1f); // 동공이 좁아짐
            eye.localScale = Vector3.one * Mathf.Lerp(1.02f, 1.12f, Mathf.Clamp01(t / seconds)); // 아주 천천히 다가옴
            yield return null;
        }
    }

    private static bool BuildEye(RectTransform root, out RectTransform eye, out RectTransform pupil, out RectTransform lidTop, out RectTransform lidBottom)
    {
        eye = pupil = lidTop = lidBottom = null;
        try
        {
            Sprite eyeArt = Resources.Load<Sprite>(ArtPath + "eye");
            Sprite pupilArt = Resources.Load<Sprite>(ArtPath + "pupil");
            if (eyeArt == null || pupilArt == null) return false;
            UIBuild.Image(root, "EyeBack", Vector2.zero, new Vector2(6000f, 4000f), null, Color.black);
            Image e = UIBuild.Image(root, "Eye", Vector2.zero, new Vector2(1920f, 1080f), eyeArt, Color.white);
            eye = e.rectTransform;
            Image p = UIBuild.Image(eye, "Pupil", Vector2.zero, pupilArt.rect.size * 1.5f, pupilArt, Color.white);
            p.preserveAspect = false;
            pupil = p.rectTransform;
            lidTop = UIBuild.Image(root, "LidTop", new Vector2(0f, 540f), new Vector2(6000f, 1080f), null, Color.black).rectTransform;
            lidBottom = UIBuild.Image(root, "LidBottom", new Vector2(0f, -540f), new Vector2(6000f, 1080f), null, Color.black).rectTransform;
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[아펩] 눈 컷 준비 오류: {ex}");
            return false;
        }
    }
}
