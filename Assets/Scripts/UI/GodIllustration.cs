using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// [추가] 신전을 끝내고 형판을 받기 직전에 그 신의 일러스트를 화면 가득 보여 주는 공용 연출
// - 그림: Assets/image/Illustration/{Ra, Bastet, Thoth, Horus, Sobek, Anubis}.png
//   -> 메뉴 PharaohCube -> Hall -> 신 일러스트 연결 이 Resources/GodIllustrations.asset에 묶어 둠
// - 번호: 0 라, 1 바스테트, 2 토트, 3 호루스, 4 소베크, 5 아누비스 (HallCubeInsert.GodNames와 같은 순서)
// - 쓰는 곳: 라(RaFinaleCut), 바스테트/소베크(BastetEnding), 토트(ThothCutscenes), 호루스(HorusCutscenes), 아누비스(AnubisRoomProgress)
// - 그림이 없으면 아무것도 안 하고 바로 끝남 (각 방의 예전 연출 그대로)
[CreateAssetMenu(fileName = "GodIllustrations", menuName = "PharaohCube/God Illustrations")]
public class GodIllustration : ScriptableObject
{
    public const int Count = 6;
    private const string ResourcePath = "GodIllustrations";
    private const int SortingOrder = 1500; // 각 방 엔딩 화면(900~1000)보다 위
    private static GodIllustration cached;

    [Tooltip("0 라, 1 바스테트, 2 토트, 3 호루스, 4 소베크, 5 아누비스")]
    public Sprite[] sprites = new Sprite[Count];

    private static readonly string[] Titles =
    {
        "태양의 신 라", "고양이 여신 바스테트", "지혜의 서기관 토트", "하늘의 매 호루스", "나일의 주인 소베크", "심판의 신 아누비스"
    };

    public static GodIllustration Main
    {
        get
        {
            if (cached == null) cached = Resources.Load<GodIllustration>(ResourcePath);
            return cached;
        }
    }

    public static Sprite Get(int god)
    {
        GodIllustration set = Main;
        if (set == null || set.sprites == null || god < 0 || god >= set.sprites.Length) return null;
        return set.sprites[god];
    }

    // 일러스트 + 이름 -> E / Space / 클릭 (또는 holdSeconds가 지나면) -> 사라짐
    public static IEnumerator Play(int god, float holdSeconds = 6f)
    {
        Sprite art = Get(god);
        if (art == null)
        {
            Debug.LogWarning($"[GodIllustration] {god}번 일러스트 없음 -> 메뉴 PharaohCube -> Hall -> 신 일러스트 연결");
            yield break;
        }
        GameObject go = null;
        Image black = null, pic = null;
        Text title = null;
        try
        {
            go = Build(art, god, out black, out pic, out title);
            GameAudio.Play("sfx_dream_shimmer", 0.8f, 0.7f);
        }
        catch (Exception e)
        {
            Debug.LogError($"[GodIllustration] 화면 만들기 오류: {e}");
            if (go != null) Destroy(go);
            yield break;
        }
        yield return Fade(black, pic, title, 0f, 1f, 1f);
        yield return WaitPress(holdSeconds);
        yield return Fade(black, pic, title, 1f, 0f, 0.7f);
        Destroy(go);
    }

    private static GameObject Build(Sprite art, int god, out Image black, out Image pic, out Text title)
    {
        RectTransform root = UIBuild.CreateCanvas(null, "GodIllustration", SortingOrder, out GameObject go);
        black = UIBuild.Stretch(root, "Black").gameObject.AddComponent<Image>();
        black.color = new Color(0f, 0f, 0f, 0f);
        pic = UIBuild.Stretch(root, "Art").gameObject.AddComponent<Image>();
        pic.sprite = art;
        pic.preserveAspect = true;
        pic.color = new Color(1f, 1f, 1f, 0f);
        Font font = PuzzleUITheme.GetTitleFont() ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        title = UIBuild.Text(root, "Title", font, 44, new Vector2(0f, -470f), new Vector2(1600f, 70f), new Color(0.95f, 0.8f, 0.45f, 0f));
        title.alignment = TextAnchor.MiddleCenter;
        title.text = god >= 0 && god < Titles.Length ? Titles[god] : "";
        return go;
    }

    private static IEnumerator Fade(Image black, Image pic, Text title, float from, float to, float seconds)
    {
        for (float t = 0f; t < seconds && !Cine.Skipping; t += Time.unscaledDeltaTime)
        {
            SetAlpha(black, pic, title, Mathf.Lerp(from, to, t / seconds));
            yield return null;
        }
        SetAlpha(black, pic, title, to);
    }

    private static void SetAlpha(Image black, Image pic, Text title, float a)
    {
        black.color = new Color(0f, 0f, 0f, a * 0.92f);
        pic.color = new Color(1f, 1f, 1f, a);
        Color c = title.color;
        title.color = new Color(c.r, c.g, c.b, a);
    }

    private static IEnumerator WaitPress(float holdSeconds)
    {
        yield return new WaitForSecondsRealtime(0.5f); // 바로 넘어가지 않게
        for (float t = 0f; t < holdSeconds && !Cine.Skipping; t += Time.unscaledDeltaTime)
        {
            if (Pressed()) yield break;
            yield return null;
        }
    }

    private static bool Pressed()
    {
        Keyboard kb = Keyboard.current;
        Mouse mouse = Mouse.current;
        bool key = kb != null && (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame);
        return key || (mouse != null && mouse.leftButton.wasPressedThisFrame);
    }

#if UNITY_EDITOR
    public void EditorSetup(Sprite[] list) => sprites = list;
#endif
}
