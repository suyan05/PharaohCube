using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 렌즈 해독 화면 - 임시판 (세부 아트 요청서: "렌즈 해독 화면은 다음 요청서", 그 전까지 코드가 그린 화면)
// - yield return LensDecodeUI.Show("꿈의 비석", "여섯 신의 시험을...");
// - 어두운 화면 가운데 큰 돋보기 -> 그 안에 글자가 한 글자씩 떠오름 -> E / Space / 클릭으로 닫기
//   (떠오르는 중에 누르면 한 번에 다 보임)
// 아트가 오면 Lens Sprite 칸에 돋보기 그림 넣기
public class LensDecodeUI : MonoBehaviour
{
    private const float LensSize = 640f;
    private static LensDecodeUI instance;

    [SerializeField] private Sprite lensSprite;
    [SerializeField, Min(1f)] private float charsPerSecond = 22f;
    [SerializeField] private int sortingOrder = 120;

    private GameObject canvasGo;
    private Text titleText, bodyText, hintText;
    private int openedFrame;

    private static LensDecodeUI Instance
    {
        get
        {
            if (instance == null) instance = FindFirstObjectByType<LensDecodeUI>();
            if (instance == null) instance = new GameObject("LensDecodeUI").AddComponent<LensDecodeUI>();
            return instance;
        }
    }

    public static IEnumerator Show(string title, string body)
    {
        LensDecodeUI ui = Instance;
        if (!ui.TryOpen(title)) yield break;

        yield return ui.Reveal(body ?? "");
        yield return ui.WaitClose();
        ui.canvasGo.SetActive(false);
    }

    // ================= 진행 =================
    private bool TryOpen(string title)
    {
        try
        {
            EnsureBuilt();
            titleText.text = title ?? "";
            bodyText.text = "";
            hintText.enabled = false;
            openedFrame = Time.frameCount;
            canvasGo.SetActive(true);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[LensDecodeUI] 해독 화면 열기 오류: {e}");
            return false;
        }
    }

    private IEnumerator Reveal(string body)
    {
        yield return new WaitForSecondsRealtime(0.5f); // 돋보기를 먼저 보여주고 글자 시작

        float shown = 0f;
        while (shown < body.Length)
        {
            shown += charsPerSecond * Time.unscaledDeltaTime;
            if (Time.frameCount > openedFrame + 1 && Pressed()) shown = body.Length;
            bodyText.text = body.Substring(0, Mathf.Min(body.Length, Mathf.FloorToInt(shown)));
            yield return null;
        }
        bodyText.text = body;
    }

    private IEnumerator WaitClose()
    {
        yield return null; // 다 보이게 만든 키가 바로 닫기로 이어지지 않게
        hintText.enabled = true;
        while (!Pressed()) yield return null;
        yield return null;
    }

    private static bool Pressed()
    {
        return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space)
            || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0);
    }

    // ================= 화면 만들기 =================
    private void EnsureBuilt()
    {
        if (canvasGo != null) return;

        Font font = PuzzleUITheme.GetBodyFont();
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        RectTransform root = UIBuild.CreateCanvas(transform, "LensDecodeCanvas", sortingOrder, out canvasGo);
        Image dim = UIBuild.Stretch(root, "Dim").gameObject.AddComponent<Image>();
        dim.color = new Color(0.04f, 0.035f, 0.06f, 0.92f);

        BuildLens(root);
        BuildTexts(root, font);
        canvasGo.SetActive(false);
    }

    private void BuildLens(RectTransform root)
    {
        if (lensSprite != null)
        {
            UIBuild.Image(root, "Lens", Vector2.zero, new Vector2(LensSize + 200f, LensSize + 200f), lensSprite, Color.white);
            return;
        }

        // 손잡이 -> 금테 -> 유리 (뒤에서부터)
        Image handle = UIBuild.Image(root, "Handle", new Vector2(300f, -300f), new Vector2(60f, 260f), null, Hex("#8A5A2B"));
        handle.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        UIBuild.Image(root, "Rim", Vector2.zero, new Vector2(LensSize + 28f, LensSize + 28f), UIBuild.Circle(), Color.white).color = Hex("#E8B23A");
        UIBuild.Image(root, "Glass", Vector2.zero, new Vector2(LensSize, LensSize), UIBuild.Circle(), Color.white).color = Hex("#2A2340");
        UIBuild.Image(root, "Shine", new Vector2(-150f, 170f), new Vector2(140f, 60f), UIBuild.Circle(), Color.white).color = new Color(0.75f, 0.88f, 0.92f, 0.15f);
    }

    private void BuildTexts(RectTransform root, Font font)
    {
        titleText = UIBuild.Text(root, "Title", font, 40, new Vector2(0f, 420f), new Vector2(1200f, 60f), Hex("#E8B23A"));
        titleText.fontStyle = FontStyle.Bold;

        bodyText = UIBuild.Text(root, "Body", font, 34, Vector2.zero, new Vector2(500f, 420f), Hex("#F6F1E7"));
        bodyText.lineSpacing = 1.4f;

        hintText = UIBuild.Text(root, "Hint", font, 24, new Vector2(0f, -440f), new Vector2(600f, 40f), new Color(1f, 1f, 1f, 0.6f));
        hintText.text = "E: 닫기";
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }
}
