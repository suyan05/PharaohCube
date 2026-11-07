using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 벽화 컷신 무대 - 이시스가 보여주는 기억을 이집트 벽화 인형극으로 보여줌
// - yield return MuralStage.Play(장면들);   (Cine.Begin ~ Cine.End 사이에서 부름)
// - 1280x720 벽화를 화면에 꽉 채움(1.5배) + 위아래 검은 띠, 아래 띠에 자막
// - 인물/배/눈 조각은 따로 떼어진 그림이라 종이 인형처럼 움직임 (MuralShot.Move)
// - 자막 넘기기: E / Space / Enter / 클릭. ESC 길게 = 연출 스킵 (Cine.Skipping)
// - 음성(선택): Resources/Voice/Mural/<장면 id>_<줄 번호(1부터)> 오디오가 있으면 그 줄에서 재생, 없으면 자막만
// - 그림: Resources/Horus/Mural/MU_<art>_bg, MU_<art>_<조각>
public class MuralStage : MonoBehaviour
{
    private const float ArtScale = 1.5f;
    private const float BarHeight = 150f;
    private const float FadeSeconds = 0.6f;
    private const float MinLineSeconds = 0.35f;
    private const int SortOrder = 95; // 검은 띠(90)보다 위, 대사창(100)보다 아래
    private const string ArtPath = "Horus/Mural/MU_";
    private const string VoicePath = "Voice/Mural/";
    private static readonly Color NameColor = new Color(0.93f, 0.74f, 0.32f, 1f);
    private static MuralStage instance;

    private GameObject canvasGo, frame;
    private RectTransform artRoot;
    private Image black;
    private Text nameText, lineText, hintText;
    private AudioSource voice;
    private readonly List<(MuralLayer layer, Image img)> live = new List<(MuralLayer, Image)>();
    private bool animDone, shotOver;

    public static bool IsPlaying { get; private set; }

    private static MuralStage Instance
    {
        get
        {
            if (instance == null) instance = FindFirstObjectByType<MuralStage>();
            if (instance == null) instance = new GameObject("MuralStage").AddComponent<MuralStage>();
            return instance;
        }
    }

    // ================= 재생 =================
    public static IEnumerator Play(MuralShot[] shots)
    {
        if (Cine.Skipping || shots == null || shots.Length == 0) yield break;
        MuralStage s = Instance;
        if (!s.TryOpen()) yield break;
        IsPlaying = true;
        yield return s.FadeBlack(0f, 1f);
        foreach (MuralShot shot in shots)
        {
            if (Cine.Skipping) break;
            if (!s.TryBuild(shot)) continue;
            s.frame.SetActive(true);
            yield return s.FadeBlack(1f, 0f);
            yield return s.RunShot(shot);
            yield return s.FadeBlack(0f, 1f);
        }
        s.Clear();
        s.frame.SetActive(false);
        yield return s.FadeBlack(1f, 0f);
        s.Close();
    }

    private bool TryOpen()
    {
        try
        {
            EnsureBuilt();
            SetBlack(0f);
            SetLine(null);
            frame.SetActive(false);
            canvasGo.SetActive(true);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[벽화] 무대 준비 오류: {e}");
            return false;
        }
    }

    private void Close()
    {
        IsPlaying = false;
        if (voice != null) voice.Stop();
        if (canvasGo != null) canvasGo.SetActive(false);
    }

    // 움직임(Animate)과 자막(Lines)을 같이 돌리고, 둘 다 끝나면 장면 끝
    private IEnumerator RunShot(MuralShot shot)
    {
        animDone = false;
        shotOver = false;
        Coroutine anim = StartCoroutine(Animate(shot));
        yield return Lines(shot);
        while (!animDone && !Cine.Skipping) yield return null;
        yield return Cine.Wait(0.4f);
        shotOver = true;
        if (anim != null) StopCoroutine(anim);
        SetLine(null);
    }

    private IEnumerator Lines(MuralShot shot)
    {
        for (int i = 0; i < shot.lines.Count && !Cine.Skipping; i++)
        {
            SetLine(shot.lines[i]);
            PlayVoice(shot.id, i + 1);
            float t = 0f;
            while (!Cine.Skipping && (t < MinLineSeconds || !NextPressed()))
            {
                t += Time.deltaTime;
                yield return null;
            }
            yield return null; // 같은 키 입력이 다음 줄을 바로 넘기지 않게 한 프레임 쉼
        }
    }

    private static bool NextPressed()
    {
        return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space)
            || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0);
    }

    private void PlayVoice(string shotId, int lineNo)
    {
        try
        {
            if (voice == null) return;
            voice.Stop();
            AudioClip clip = Resources.Load<AudioClip>($"{VoicePath}{shotId}_{lineNo}");
            if (clip == null) return;
            voice.clip = clip;
            voice.Play();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[벽화] 음성 재생 실패 {shotId}_{lineNo}: {e.Message}");
        }
    }

    // ================= 움직임 =================
    private IEnumerator Animate(MuralShot shot)
    {
        float t = 0f;
        while (!shotOver)
        {
            t += Time.deltaTime;
            float u = Cine.Skipping ? 1f : Mathf.Clamp01(t / Mathf.Max(0.1f, shot.duration));
            ApplyCamera(shot, u);
            foreach (var pair in live) ApplyLayer(pair.layer, pair.img, u, t);
            if (u >= 1f) animDone = true;
            yield return null;
        }
    }

    private void ApplyCamera(MuralShot shot, float u)
    {
        float e = Mathf.SmoothStep(0f, 1f, u);
        float zoom = Mathf.Lerp(shot.zoomFrom, shot.zoomTo, e);
        Vector2 pan = Vector2.Lerp(shot.panFrom, shot.panTo, e);
        artRoot.localScale = Vector3.one * zoom;
        artRoot.anchoredPosition = new Vector2(-pan.x, pan.y) * ArtScale * zoom;
    }

    private static void ApplyLayer(MuralLayer l, Image img, float u, float time)
    {
        if (img == null) return;
        MuralPose p = MuralShot.Evaluate(l, u);
        float bob = l.bobAmp > 0f ? Mathf.Sin(time * l.bobSpeed + l.pos.x * 0.01f) * l.bobAmp : 0f;
        Vector2 art = l.pos + p.offset + new Vector2(0f, bob);
        img.rectTransform.anchoredPosition = ToUi(art);
        img.rectTransform.localRotation = Quaternion.Euler(0f, 0f, p.rot);
        Color c = img.color;
        c.a = p.alpha;
        img.color = c;
    }

    // 벽화 픽셀(왼쪽 위 0,0, 아래로 +y) -> 화면 UI 좌표(가운데 0,0, 위로 +y)
    private static Vector2 ToUi(Vector2 art) => new Vector2(art.x - 640f, 360f - art.y) * ArtScale;

    // ================= 장면 그림 깔기 =================
    private bool TryBuild(MuralShot shot)
    {
        try
        {
            Clear();
            Sprite bg = Resources.Load<Sprite>($"{ArtPath}{shot.art}_bg");
            if (bg == null) Debug.LogWarning($"[벽화] 배경 그림 없음: {ArtPath}{shot.art}_bg (빌더 실행/임포트 확인)");
            UIBuild.Image(artRoot, "BG", Vector2.zero, new Vector2(1280f, 720f) * ArtScale, bg, new Color(0.85f, 0.72f, 0.5f));
            foreach (MuralLayer l in shot.layers) live.Add((l, MakeLayer(shot, l)));
            ApplyCamera(shot, 0f);
            foreach (var pair in live) ApplyLayer(pair.layer, pair.img, 0f, 0f);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[벽화] 장면 '{shot.id}' 준비 오류: {e}");
            return false;
        }
    }

    private Image MakeLayer(MuralShot shot, MuralLayer l)
    {
        Sprite s = Resources.Load<Sprite>($"{ArtPath}{shot.art}_{l.name}");
        if (s == null)
        {
            Debug.LogWarning($"[벽화] 조각 그림 없음: {ArtPath}{shot.art}_{l.name}");
            return null;
        }
        Image img = UIBuild.Image(artRoot, l.name, ToUi(l.pos), s.rect.size * ArtScale, s, Color.white);
        img.preserveAspect = false;
        return img;
    }

    private void Clear()
    {
        live.Clear();
        if (artRoot == null) return;
        for (int i = artRoot.childCount - 1; i >= 0; i--) Destroy(artRoot.GetChild(i).gameObject);
    }

    // ================= 화면 틀 (검은 띠 + 자막 + 암전) =================
    private void EnsureBuilt()
    {
        if (canvasGo != null) return;
        Font font = PuzzleUITheme.GetBodyFont();
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RectTransform root = UIBuild.CreateCanvas(transform, "MuralCanvas", SortOrder, out canvasGo);
        RectTransform group = UIBuild.Stretch(root, "Frame"); // 배경+벽화+띠를 한 번에 켜고 끔 (암전은 밖에 둠)
        frame = group.gameObject;
        UIBuild.Image(group, "Back", Vector2.zero, new Vector2(6000f, 4000f), null, Color.black);
        artRoot = UIBuild.Rect(group, "Art", Vector2.zero, new Vector2(1920f, 1080f));
        Bar(group, true);
        BuildSubtitle(Bar(group, false), font);
        black = UIBuild.Image(root, "Fade", Vector2.zero, new Vector2(6000f, 4000f), null, Color.black);
        voice = gameObject.AddComponent<AudioSource>();
        voice.playOnAwake = false;
        canvasGo.SetActive(false);
    }

    // 화면 위/아래 끝에 붙는 검은 띠 (화면 비율이 달라도 끝에 붙게 앵커 사용)
    private static RectTransform Bar(RectTransform root, bool top)
    {
        RectTransform rt = UIBuild.Rect(root, top ? "BarTop" : "BarBottom", Vector2.zero, Vector2.zero);
        rt.anchorMin = new Vector2(0f, top ? 1f : 0f);
        rt.anchorMax = new Vector2(1f, top ? 1f : 0f);
        rt.pivot = new Vector2(0.5f, top ? 1f : 0f);
        rt.sizeDelta = new Vector2(0f, BarHeight);
        rt.anchoredPosition = Vector2.zero;
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = false;
        return rt;
    }

    private void BuildSubtitle(RectTransform bar, Font font)
    {
        nameText = UIBuild.Text(bar, "Name", font, 30, new Vector2(0f, 42f), new Vector2(1400f, 40f), NameColor);
        lineText = UIBuild.Text(bar, "Line", font, 34, new Vector2(0f, -6f), new Vector2(1500f, 90f), new Color(0.96f, 0.94f, 0.9f));
        hintText = UIBuild.Text(bar, "Hint", font, 22, new Vector2(860f, -52f), new Vector2(160f, 30f), new Color(1f, 1f, 1f, 0.5f));
        hintText.text = "E >";
    }

    private void SetLine(DialogueLine line)
    {
        if (nameText == null) return;
        nameText.text = line != null ? line.speaker : "";
        lineText.text = line != null ? line.text : "";
        hintText.enabled = line != null;
    }

    private IEnumerator FadeBlack(float from, float to)
    {
        float t = 0f;
        while (t < FadeSeconds && !Cine.Skipping)
        {
            t += Time.deltaTime;
            SetBlack(Mathf.Lerp(from, to, t / FadeSeconds));
            yield return null;
        }
        SetBlack(to);
    }

    private void SetBlack(float a)
    {
        if (black != null) black.color = new Color(0f, 0f, 0f, a);
    }
}
