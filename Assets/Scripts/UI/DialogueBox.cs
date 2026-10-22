using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 대사 한 줄 (말하는 사람 이름 + 내용 + 초상화는 선택)
[Serializable]
public class DialogueLine
{
    [Tooltip("비우면 이름 칸 숨김 (예: 발굴 일지 내용, 혼잣말)")]
    public string speaker = "";
    [TextArea(2, 4)] public string text = "";
    [Tooltip("비우면 초상화 없이 글만")]
    public Sprite portrait;

    public DialogueLine() { }

    public DialogueLine(string speaker, string text)
    {
        this.speaker = speaker;
        this.text = text;
    }
}

// 범용 대사창 (프롤로그, 조사 오브젝트, 중앙 홀이 같이 씀. 라의 성소는 기존 RaDialogue 그대로)
// - DialogueBox.Play(대사들, 끝나면 할 일)
// - DialogueBox.Ask(질문 대사, 선택지들, 고른 번호 받을 함수)  -> 스핑크스 수수께끼 같은 선택지
// - DialogueBox.ForceClose()  -> 연출 스킵 때 강제로 닫기 (끝나면 할 일은 부르지 않음)
// - 글자가 한 글자씩. E / Space / Enter / 클릭 = 다음 (나오는 중이면 한 번에 표시)
// - 선택지: W/S 또는 위/아래 화살표로 고르고 E / Enter로 확정, 마우스 클릭도 됨
// - 씬에 안 놓아도 처음 부를 때 자동으로 생김
public class DialogueBox : MonoBehaviour
{
    private static readonly Vector2 BoxPos = new Vector2(0f, -400f);
    private static readonly Vector2 BoxSize = new Vector2(1600f, 240f);
    private static readonly Vector2 PortraitSize = new Vector2(200f, 200f);

    private static DialogueBox instance;

    [Header("아트 (비우면 코드로 그린 틀)")]
    [SerializeField] private Sprite boxSprite;   // UI_Dialog_Box (9-slice)
    [SerializeField] private Sprite nextSprite;  // UI_Dialog_Next

    [Header("글자")]
    [SerializeField, Min(1f)] private float charsPerSecond = 30f;
    [SerializeField] private int sortingOrder = 100;

    private readonly List<Text> choiceTexts = new List<Text>();
    private GameObject canvasGo;
    private RectTransform choicePanel;
    private Image portraitImage, nextImage;
    private Text speakerText, bodyText;
    private Font font;

    private DialogueLine[] lines;
    private int index;
    private string fullText = "";
    private float visibleChars;
    private int openedFrame;
    private Action onFinished;

    private string[] choices;
    private int choiceIndex;
    private Action<int> onChosen;

    public static bool IsOpen => instance != null && instance.canvasGo != null && instance.canvasGo.activeSelf;

    // 닫힌 프레임 (닫을 때 누른 E로 바로 다시 조사되는 것 방지)
    public static int ClosedFrame { get; private set; } = -1;

    private static DialogueBox Instance
    {
        get
        {
            if (instance == null) instance = FindFirstObjectByType<DialogueBox>();
            if (instance == null) instance = new GameObject("DialogueBox").AddComponent<DialogueBox>();
            return instance;
        }
    }

    // ================= 밖에서 부르는 함수 =================
    public static void Play(DialogueLine[] newLines, Action finished = null)
    {
        if (newLines == null || newLines.Length == 0)
        {
            finished?.Invoke();
            return;
        }
        Instance.Open(newLines, finished, null, null);
    }

    public static void Ask(DialogueLine question, string[] options, Action<int> chosen)
    {
        if (options == null || options.Length == 0)
        {
            Debug.LogWarning("[DialogueBox] 선택지가 비어 있음");
            return;
        }
        Instance.Open(new[] { question }, null, options, chosen);
    }

    public static void ForceClose()
    {
        if (IsOpen) instance.Close(false);
    }

    private void Open(DialogueLine[] newLines, Action finished, string[] newChoices, Action<int> chosen)
    {
        try
        {
            EnsureBuilt();
            lines = newLines;
            index = 0;
            onFinished = finished;
            choices = newChoices;
            onChosen = chosen;
            openedFrame = Time.frameCount;

            PlayerLock.Lock(this);
            canvasGo.SetActive(true);
            ShowLine();
        }
        catch (Exception e)
        {
            Debug.LogError($"[DialogueBox] 대사 열기 오류: {e}");
            Close(false);
        }
    }

    // ================= 진행 =================
    private void Update()
    {
        if (!IsOpen) return;

        try
        {
            TypeText();
            if (Time.frameCount == openedFrame) return; // 대화를 연 E키가 바로 넘어가지 않게

            if (choicePanel.gameObject.activeSelf) UpdateChoice();
            else if (PressedNext()) Advance();
        }
        catch (Exception e)
        {
            Debug.LogError($"[DialogueBox] 대사 진행 오류: {e}");
            Close(false);
        }
    }

    private static bool PressedNext()
    {
        return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space)
            || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)
            || Input.GetMouseButtonDown(0);
    }

    private void Advance()
    {
        if (!TextDone)
        {
            visibleChars = fullText.Length;
            bodyText.text = fullText;
            return;
        }

        bool last = index >= lines.Length - 1;
        if (last && choices != null)
        {
            OpenChoices();
            return;
        }

        index++;
        if (index >= lines.Length) Close(true);
        else ShowLine();
    }

    private bool TextDone => visibleChars >= fullText.Length;

    private void ShowLine()
    {
        DialogueLine line = lines[index] ?? new DialogueLine();
        fullText = line.text ?? "";
        visibleChars = 0f;
        bodyText.text = "";
        nextImage.enabled = false;
        choicePanel.gameObject.SetActive(false);

        speakerText.text = line.speaker;
        speakerText.enabled = !string.IsNullOrEmpty(line.speaker);
        SetPortrait(line.portrait);
    }

    // 초상화가 있으면 왼쪽에 두고 글을 오른쪽으로 밀어냄
    private void SetPortrait(Sprite sprite)
    {
        bool has = sprite != null;
        portraitImage.enabled = has;
        portraitImage.sprite = sprite;

        float left = has ? PortraitSize.x + 60f : 60f;
        bodyText.rectTransform.anchoredPosition = BoxPos + new Vector2(left * 0.5f, -20f);
        bodyText.rectTransform.sizeDelta = new Vector2(BoxSize.x - left - 80f, BoxSize.y - 90f);
        speakerText.rectTransform.anchoredPosition = BoxPos + new Vector2(-BoxSize.x * 0.5f + left + 200f, 78f);
    }

    private void TypeText()
    {
        if (TextDone)
        {
            bool waitingChoice = choices != null && index >= lines.Length - 1;
            nextImage.enabled = !waitingChoice && !choicePanel.gameObject.activeSelf;
            if (waitingChoice && !choicePanel.gameObject.activeSelf) OpenChoices();
            return;
        }

        visibleChars += charsPerSecond * Time.unscaledDeltaTime;
        int count = Mathf.Min(fullText.Length, Mathf.FloorToInt(visibleChars));
        bodyText.text = fullText.Substring(0, count);
    }

    // ================= 선택지 =================
    private void OpenChoices()
    {
        BuildChoiceButtons();
        choiceIndex = 0;
        choicePanel.gameObject.SetActive(true);
        RefreshChoices();
    }

    private void UpdateChoice()
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) MoveChoice(-1);
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) MoveChoice(1);
        else if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) Choose(choiceIndex);
    }

    private void MoveChoice(int delta)
    {
        choiceIndex = (choiceIndex + delta + choices.Length) % choices.Length;
        RefreshChoices();
    }

    private void RefreshChoices()
    {
        for (int i = 0; i < choiceTexts.Count; i++)
        {
            bool sel = i == choiceIndex;
            choiceTexts[i].text = (sel ? "> " : "   ") + choices[i];
            choiceTexts[i].color = sel ? Hex("#E07A3F") : Hex("#4A3423");
        }
    }

    private void Choose(int picked)
    {
        Action<int> callback = onChosen;
        onChosen = null;
        Close(false);
        callback?.Invoke(picked);
    }

    // 선택지 개수만큼 버튼 (말풍선 오른쪽 위)
    private void BuildChoiceButtons()
    {
        foreach (Text t in choiceTexts) Destroy(t.transform.parent.gameObject);
        choiceTexts.Clear();

        float h = 60f;
        choicePanel.sizeDelta = new Vector2(320f, choices.Length * h + 24f);
        for (int i = 0; i < choices.Length; i++)
        {
            float y = (choices.Length - 1) * h * 0.5f - i * h;
            choiceTexts.Add(BuildChoiceButton(i, y, h));
        }
    }

    private Text BuildChoiceButton(int i, float y, float h)
    {
        Image bg = UIBuild.Image(choicePanel, $"Choice_{i}", new Vector2(0f, y), new Vector2(300f, h - 8f), null, new Color(1f, 1f, 1f, 0.01f));
        bg.raycastTarget = true;
        int picked = i;
        bg.gameObject.AddComponent<Button>().onClick.AddListener(() => Choose(picked));

        Text t = UIBuild.Text(bg.rectTransform, "Label", font, 28, Vector2.zero, new Vector2(280f, h - 8f), Hex("#4A3423"));
        t.alignment = TextAnchor.MiddleLeft;
        return t;
    }

    // ================= 닫기 =================
    private void Close(bool invokeFinished)
    {
        if (canvasGo != null) canvasGo.SetActive(false);
        ClosedFrame = Time.frameCount;
        PlayerLock.Unlock(this);

        Action callback = onFinished;
        onFinished = null;
        lines = null;
        choices = null;
        if (invokeFinished) callback?.Invoke();
    }

    private void OnDisable()
    {
        if (IsOpen) Close(false);
    }

    // ================= 화면 만들기 =================
    private void EnsureBuilt()
    {
        if (canvasGo != null) return;

        font = PuzzleUITheme.GetBodyFont();
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        RectTransform root = UIBuild.CreateCanvas(transform, "DialogueCanvas", sortingOrder, out canvasGo);
        BuildBox(root);
        BuildChoicePanel(root);
        canvasGo.SetActive(false);
    }

    private void BuildBox(RectTransform root)
    {
        Image box = UIBuild.Image(root, "Box", BoxPos, BoxSize, boxSprite, Hex("#E8B23A"));
        box.preserveAspect = false;
        if (boxSprite != null && boxSprite.border != Vector4.zero) box.type = Image.Type.Sliced;
        UIBuild.Image(root, "Inner", BoxPos, BoxSize - new Vector2(12f, 12f), null, Hex("#F6F1E7")).enabled = boxSprite == null;

        Vector2 portraitPos = BoxPos + new Vector2(-BoxSize.x * 0.5f + 30f + PortraitSize.x * 0.5f, 0f);
        portraitImage = UIBuild.Image(root, "Portrait", portraitPos, PortraitSize, null, Color.white);

        speakerText = UIBuild.Text(root, "Speaker", font, 30, Vector2.zero, new Vector2(400f, 44f), Hex("#E07A3F"));
        speakerText.alignment = TextAnchor.MiddleLeft;
        speakerText.fontStyle = FontStyle.Bold;

        bodyText = UIBuild.Text(root, "Body", font, 30, BoxPos, BoxSize, Hex("#4A3423"));
        bodyText.alignment = TextAnchor.UpperLeft;
        bodyText.lineSpacing = 1.2f;

        Vector2 nextPos = BoxPos + new Vector2(BoxSize.x * 0.5f - 44f, -BoxSize.y * 0.5f + 34f);
        nextImage = UIBuild.Image(root, "Next", nextPos, new Vector2(24f, 24f), nextSprite, Hex("#C9A66B"));
    }

    private void BuildChoicePanel(RectTransform root)
    {
        Image bg = UIBuild.Image(root, "Choices", new Vector2(620f, -140f), new Vector2(320f, 200f), null, Hex("#F6F1E7"));
        choicePanel = bg.rectTransform;
        choicePanel.gameObject.SetActive(false);
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }
}
