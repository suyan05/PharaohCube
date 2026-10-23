using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 꿈의 비석 화면 (기획서 3.1의 7: 렌즈 해독 튜토리얼)
// - ShowUnreadable: 렌즈 없이 볼 때. 상형문자만 빼곡 -> 못 읽음
// - Decode:        렌즈로 비춘 칸만 한글로 바뀜 (비춘 만큼 쌓임). 다 비추면 해독 완료
//                  렌즈 움직이기 = 마우스 또는 WASD / 화살표
// - ShowDecoded:   다 읽은 비석 다시 보기
// 셋 다 끝나면 E / Space / 클릭으로 닫힘. 상형문자 그림은 GlyphFactory가 코드로 만듦 (아트 오면 교체)
public class SteleDecodeUI : MonoBehaviour
{
    private const float CellWidth = 48f;
    private const float RowHeight = 80f;
    private const float LensSize = 230f;
    private static readonly Vector2 PanelSize = new Vector2(760f, 820f);
    private static SteleDecodeUI instance;

    [SerializeField, Min(1f)] private float lensSpeed = 650f;
    [SerializeField, Range(0.2f, 0.6f)] private float revealRadius = 0.45f; // 렌즈 크기 대비
    [SerializeField] private int sortingOrder = 120;

    private readonly List<SteleCell> cells = new List<SteleCell>();
    private GameObject canvasGo;
    private RectTransform root, panel, textArea, lens;
    private Text titleText, noteText, hintText;
    private Font font;
    private Vector3 lastMouse;

    private static SteleDecodeUI Instance
    {
        get
        {
            if (instance == null) instance = FindFirstObjectByType<SteleDecodeUI>();
            if (instance == null) instance = new GameObject("SteleDecodeUI").AddComponent<SteleDecodeUI>();
            return instance;
        }
    }

    // ================= 밖에서 부르는 함수 =================
    public static IEnumerator ShowUnreadable(string title, string[] lines, string note)
    {
        SteleDecodeUI ui = Instance;
        if (!ui.Open(title, lines, false, note)) yield break;
        yield return ui.WaitClose("E: 닫기");
    }

    public static IEnumerator Decode(string title, string[] lines)
    {
        SteleDecodeUI ui = Instance;
        if (!ui.Open(title, lines, true, "렌즈를 움직여 글자를 비춰보자  (마우스 / WASD)")) yield break;

        yield return ui.DecodeLoop();
        ui.lens.gameObject.SetActive(false);
        ui.noteText.text = "해독 완료";
        yield return ui.WaitClose("E: 닫기");
    }

    public static IEnumerator ShowDecoded(string title, string[] lines)
    {
        SteleDecodeUI ui = Instance;
        if (!ui.Open(title, lines, false, "")) yield break;
        foreach (SteleCell cell in ui.cells) cell.ForceDecoded();
        yield return ui.WaitClose("E: 닫기");
    }

    // ================= 진행 =================
    private bool Open(string title, string[] lines, bool withLens, string note)
    {
        try
        {
            EnsureBuilt();
            BuildCells(lines ?? new string[0]);
            titleText.text = title ?? "";
            noteText.text = note ?? "";
            hintText.text = "";
            lens.gameObject.SetActive(withLens);
            lens.anchoredPosition = new Vector2(0f, -40f);
            lastMouse = Input.mousePosition;
            canvasGo.SetActive(true);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SteleDecodeUI] 비석 화면 열기 오류: {e}");
            return false;
        }
    }

    private IEnumerator DecodeLoop()
    {
        int total = Mathf.Max(1, cells.Count);
        int decoded = 0;
        while (decoded < total)
        {
            MoveLens(Time.unscaledDeltaTime);
            decoded = TickCells(Time.unscaledDeltaTime, true);
            noteText.text = $"해독 {decoded * 100 / total}%";
            yield return null;
        }
        yield return new WaitForSecondsRealtime(0.5f); // 마지막 글자가 떠오르는 것까지 보여줌
    }

    // 마우스를 움직였으면 마우스 위치로, 아니면 키보드로
    private void MoveLens(float dt)
    {
        Vector2 pos = lens.anchoredPosition;
        if ((Input.mousePosition - lastMouse).sqrMagnitude > 1f)
        {
            lastMouse = Input.mousePosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, Input.mousePosition, null, out pos);
        }
        else
        {
            Vector2 dir = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            pos += Vector2.ClampMagnitude(dir, 1f) * lensSpeed * dt;
        }

        Vector2 half = PanelSize * 0.5f;
        lens.anchoredPosition = new Vector2(Mathf.Clamp(pos.x, -half.x, half.x), Mathf.Clamp(pos.y, -half.y, half.y));
    }

    // 렌즈 안에 들어온 칸은 해독. 해독된 칸 수를 돌려줌
    private int TickCells(float dt, bool lensOn)
    {
        float radius = LensSize * revealRadius * root.lossyScale.x;
        int decoded = 0;
        foreach (SteleCell cell in cells)
        {
            bool under = lensOn && Vector2.Distance(cell.Rect.position, lens.position) <= radius;
            if (under && !cell.Decoded) cell.Decode();
            cell.Tick(dt, under);
            if (cell.Decoded) decoded++;
        }
        return decoded;
    }

    private IEnumerator WaitClose(string hint)
    {
        yield return null;
        hintText.text = hint;
        while (!Pressed())
        {
            TickCells(Time.unscaledDeltaTime, false);
            yield return null;
        }
        yield return null;
        canvasGo.SetActive(false);
    }

    private static bool Pressed()
    {
        return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space)
            || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0);
    }

    // ================= 글자 칸 =================
    private void BuildCells(string[] lines)
    {
        foreach (Transform child in textArea) Destroy(child.gameObject);
        cells.Clear();

        float top = (lines.Length - 1) * RowHeight * 0.5f;
        for (int row = 0; row < lines.Length; row++)
        {
            string line = lines[row] ?? "";
            float left = -(line.Length - 1) * CellWidth * 0.5f;
            for (int i = 0; i < line.Length; i++)
            {
                if (char.IsWhiteSpace(line[i])) continue;
                Vector2 pos = new Vector2(left + i * CellWidth, top - row * RowHeight);
                cells.Add(CreateCell(line[i], pos, row * 100 + i));
            }
        }
    }

    private SteleCell CreateCell(char c, Vector2 pos, int seed)
    {
        RectTransform rt = UIBuild.Rect(textArea, $"Cell_{seed}", pos, new Vector2(CellWidth, RowHeight));
        Image glyph = UIBuild.Image(rt, "Glyph", Vector2.zero, new Vector2(40f, 40f), GlyphFactory.Get(seed + c), Color.white);
        glyph.color = Hex("#4A3A28");

        Text letter = UIBuild.Text(rt, "Letter", font, 38, Vector2.zero, new Vector2(CellWidth, RowHeight), Hex("#6B3E10"));
        letter.fontStyle = FontStyle.Bold;
        letter.text = c.ToString();
        return new SteleCell(rt, glyph, letter);
    }

    // ================= 화면 만들기 =================
    private void EnsureBuilt()
    {
        if (canvasGo != null) return;

        font = PuzzleUITheme.GetBodyFont();
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        root = UIBuild.CreateCanvas(transform, "SteleCanvas", sortingOrder, out canvasGo);
        Image dim = UIBuild.Stretch(root, "Dim").gameObject.AddComponent<Image>();
        dim.color = new Color(0.04f, 0.035f, 0.06f, 0.88f);

        BuildPanel();
        BuildTexts();
        lens = SteleLensVisual.Build(root, LensSize);
        canvasGo.SetActive(false);
    }

    // 윗부분이 둥근 석판 + 날개 달린 태양 원반 (세부 아트 요청서 2.6 비석 모양)
    private void BuildPanel()
    {
        Vector2 center = new Vector2(0f, -20f);
        Image top = UIBuild.Image(root, "Top", center + new Vector2(0f, PanelSize.y * 0.5f - 40f), new Vector2(PanelSize.x, 300f), UIBuild.Circle(), Color.white);
        top.preserveAspect = false; // 납작한 타원으로 둥근 윗부분
        top.color = Hex("#A89C86");
        panel = UIBuild.Image(root, "Panel", center - new Vector2(0f, 40f), PanelSize - new Vector2(0f, 80f), null, Hex("#A89C86")).rectTransform;
        UIBuild.Image(panel, "Inner", Vector2.zero, PanelSize - new Vector2(40f, 120f), null, Hex("#C2B8A4"));

        Vector2 sun = center + new Vector2(0f, PanelSize.y * 0.5f - 60f);
        UIBuild.Image(root, "WingL", sun + new Vector2(-95f, 0f), new Vector2(130f, 16f), null, Hex("#3C5C8A"));
        UIBuild.Image(root, "WingR", sun + new Vector2(95f, 0f), new Vector2(130f, 16f), null, Hex("#3C5C8A"));
        UIBuild.Image(root, "Sun", sun, new Vector2(60f, 60f), UIBuild.Circle(), Color.white).color = Hex("#E8B23A");

        textArea = UIBuild.Rect(root, "TextArea", center - new Vector2(0f, 50f), PanelSize);
    }

    private void BuildTexts()
    {
        titleText = UIBuild.Text(root, "Title", font, 40, new Vector2(0f, 470f), new Vector2(1200f, 60f), Hex("#E8B23A"));
        titleText.fontStyle = FontStyle.Bold;
        noteText = UIBuild.Text(root, "Note", font, 28, new Vector2(0f, -462f), new Vector2(1400f, 44f), Hex("#F6F1E7"));
        hintText = UIBuild.Text(root, "Hint", font, 24, new Vector2(0f, -505f), new Vector2(600f, 36f), new Color(1f, 1f, 1f, 0.6f));
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }
}
