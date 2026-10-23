using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 업적(유물 카드) 획득 알림 + 오른쪽 위 "J 수첩" 키 안내 (기획서 3.1)
// - 연출, 대사, 팝업, 퍼즐 화면, 오른쪽 위 알림이 다 끝난 뒤에 뜸 (겹치지 않게)
// - 오른쪽에서 미끄러져 들어옴 -> 붉은 "발굴" 도장이 쾅 -> 3초 뒤 작아지며 J 안내 쪽으로 빨려 들어감
// - 게임은 멈추지 않음. 새 카드가 있으면 J 안내에 빨간 점
// - 지금은 도형. 아트(Common_UI_AchToast_Frame, _Stamp)가 오면 frameSprite / stampSprite 칸에 넣기
public class AchievementToastUI : MonoBehaviour
{
    private static readonly Vector2 PanelSize = new Vector2(500f, 140f);

    [SerializeField] private Sprite frameSprite;
    [SerializeField] private Sprite stampSprite;
    [SerializeField] private int sortingOrder = 105;

    private readonly Queue<AchievementDef> queue = new Queue<AchievementDef>();
    private RectTransform root, panel, keyHint, stamp;
    private Image card, dot;
    private Text nameText, titleText;
    private CanvasGroup keyGroup;
    private bool showing;

    private void Awake()
    {
        try
        {
            Build();
        }
        catch (Exception e)
        {
            Debug.LogError($"[AchievementToastUI] 만들기 오류: {e}");
            enabled = false;
        }
    }

    private void OnEnable() => AchievementSystem.Unlocked += Enqueue;

    private void OnDisable() => AchievementSystem.Unlocked -= Enqueue;

    private void Enqueue(AchievementDef def)
    {
        queue.Enqueue(def);
        if (!showing) StartCoroutine(ShowQueue());
    }

    private void Update()
    {
        if (keyGroup == null) return;
        keyGroup.alpha = Mathf.MoveTowards(keyGroup.alpha, NotebookUI.Blocked ? 0f : 1f, Time.unscaledDeltaTime * 6f);
        bool fresh = (AchievementSystem.Instance != null && AchievementSystem.Instance.HasNew) || NotebookNotes.HasNew;
        dot.enabled = fresh;
    }

    // ================= 보여주기 =================
    private IEnumerator ShowQueue()
    {
        showing = true;
        while (queue.Count > 0)
        {
            while (!CanShow()) yield return null;
            yield return ShowOne(queue.Dequeue());
        }
        showing = false;
    }

    private static bool CanShow()
    {
        if (NotebookUI.Blocked || NotebookUI.IsOpen) return false;
        GameObject alerts = GameObject.Find("GameMessageCanvas");
        Transform toastRoot = alerts != null ? alerts.transform.Find("ToastRoot") : null;
        return toastRoot == null || toastRoot.childCount == 0;
    }

    private IEnumerator ShowOne(AchievementDef def)
    {
        nameText.text = def.relic;
        titleText.text = def.title;
        card.color = NotebookUI.GradeColor(def.grade);
        panel.gameObject.SetActive(true);
        panel.localScale = Vector3.one;
        stamp.gameObject.SetActive(false);

        yield return Slide(new Vector2(PanelSize.x + 40f, -80f), new Vector2(-30f, -80f), 0.35f);
        yield return Stamp();
        yield return new WaitForSecondsRealtime(3f);
        yield return FlyToKey();
        panel.gameObject.SetActive(false);
    }

    private IEnumerator Slide(Vector2 from, Vector2 to, float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            panel.anchoredPosition = Vector2.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / seconds));
            yield return null;
        }
        panel.anchoredPosition = to;
    }

    private IEnumerator Stamp()
    {
        stamp.gameObject.SetActive(true);
        for (float t = 0f; t < 0.18f; t += Time.unscaledDeltaTime)
        {
            stamp.localScale = Vector3.one * Mathf.Lerp(1.4f, 1f, t / 0.18f);
            yield return null;
        }
        stamp.localScale = Vector3.one;
    }

    private IEnumerator FlyToKey()
    {
        Vector2 start = panel.anchoredPosition;
        Vector2 end = keyHint.anchoredPosition;
        for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime)
        {
            float k = t / 0.4f;
            panel.anchoredPosition = Vector2.Lerp(start, end, k * k);
            panel.localScale = Vector3.one * Mathf.Lerp(1f, 0.1f, k);
            yield return null;
        }
    }

    // ================= 화면 만들기 =================
    private void Build()
    {
        root = UIBuild.CreateCanvas(transform, "AchievementCanvas", sortingOrder, out GameObject canvasGo);
        canvasGo.GetComponent<GraphicRaycaster>().enabled = false;
        BuildKeyHint();
        BuildPanel();
    }

    private void BuildKeyHint()
    {
        keyHint = UIBuild.Rect(root, "KeyHint", Vector2.zero, new Vector2(200f, 40f));
        keyHint.anchorMin = keyHint.anchorMax = keyHint.pivot = new Vector2(1f, 1f);
        keyHint.anchoredPosition = new Vector2(-30f, -28f);
        keyGroup = keyHint.gameObject.AddComponent<CanvasGroup>();
        Image bg = keyHint.gameObject.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.07f, 0.04f, 0.8f);
        bg.raycastTarget = false;
        Text label = UIBuild.Text(keyHint, "Label", PuzzleUITheme.GetBodyFont(), 20, Vector2.zero, keyHint.sizeDelta, new Color(0.91f, 0.85f, 0.72f));
        label.horizontalOverflow = HorizontalWrapMode.Overflow; // 한 줄로
        label.text = "J  고고학 수첩";
        dot = UIBuild.Image(keyHint, "NewDot", new Vector2(96f, 18f), new Vector2(16f, 16f), null, new Color(0.85f, 0.2f, 0.15f));
        dot.sprite = UIBuild.Circle();
    }

    private void BuildPanel()
    {
        panel = UIBuild.Rect(root, "Toast", Vector2.zero, PanelSize);
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(1f, 1f);
        Image frame = panel.gameObject.AddComponent<Image>();
        frame.sprite = frameSprite;
        frame.color = frameSprite != null ? Color.white : new Color(0.91f, 0.85f, 0.72f);
        frame.raycastTarget = false;

        card = UIBuild.Image(panel, "Card", new Vector2(-190f, 0f), new Vector2(76f, 112f), null, Color.white);
        Font font = PuzzleUITheme.GetBodyFont();
        // [수정] 세 줄을 칸을 나눠 겹치지 않게: 머리말 / 유물 이름(길면 글자 크기 자동으로 줄임) / 업적 이름
        UIBuild.Text(panel, "Head", font, 20, new Vector2(-6f, 44f), new Vector2(250f, 26f), new Color(0.64f, 0.23f, 0.15f)).text = "유물 발굴!";
        nameText = UIBuild.Text(panel, "Name", font, 26, new Vector2(-6f, 6f), new Vector2(250f, 40f), new Color(0.29f, 0.2f, 0.14f));
        nameText.horizontalOverflow = HorizontalWrapMode.Wrap;
        nameText.verticalOverflow = VerticalWrapMode.Truncate;
        nameText.resizeTextForBestFit = true;
        nameText.resizeTextMinSize = 16;
        nameText.resizeTextMaxSize = 26;
        titleText = UIBuild.Text(panel, "Title", font, 18, new Vector2(-6f, -38f), new Vector2(250f, 26f), new Color(0.42f, 0.3f, 0.2f));

        Image s = UIBuild.Image(panel, "Stamp", new Vector2(190f, 0f), new Vector2(80f, 80f), stampSprite, new Color(0.64f, 0.23f, 0.15f, 0.9f));
        if (stampSprite == null) s.sprite = UIBuild.Circle();
        UIBuild.Text(s.rectTransform, "Label", font, 22, Vector2.zero, new Vector2(80f, 80f), new Color(0.98f, 0.92f, 0.85f)).text = "발굴";
        stamp = s.rectTransform;
        panel.gameObject.SetActive(false);
    }
}
