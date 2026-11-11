using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 캐릭터 머리 위 감정 말풍선 (쯔꾸르 느낌표, 물음표, 말줄임)
// - yield return EmoteBalloonUI.Show(player, "!", 0.8f);   보여주고 사라질 때까지 기다림
// - EmoteBalloonUI.Pop(player, "...", 1.2f);               안 기다리고 띄우기
// 자주 쓰는 것: "!" 놀람, "?" 의문, "..." 한숨, 침묵, "!?" 당황, "~" 안도
// [수정] 위치: 예전엔 "발 기준 + 1.9칸" 고정이라 머리에서 멀리 떨어져 줌인하면 화면 밖으로 잘렸음
//        -> 지금은 캐릭터 그림의 실제 머리 꼭대기 바로 위. 화면 밖으로 나가려 하면 안쪽으로 끌어옴
// [수정] 모양: 흰 원 -> 꼬리 달린 도트 말풍선 (UI_Bubble). 아트가 오면 Bubble Sprite 칸에 넣기
public class EmoteBalloonUI : MonoBehaviour
{
    private static readonly Vector2 BubbleSize = new Vector2(96f, 96f);
    private static EmoteBalloonUI instance;

    [SerializeField] private Sprite bubbleSprite;
    [Tooltip("그림을 못 찾았을 때만 쓰는 높이 (캐릭터 위치 기준 위로, 유닛)")]
    [SerializeField] private float headHeight = 1.0f;
    [Tooltip("머리 꼭대기와 말풍선 꼬리 사이 틈 (유닛)")]
    [SerializeField] private float gap = 0.08f;
    [SerializeField] private int sortingOrder = 85;

    private RectTransform root;
    private Font font;

    private static EmoteBalloonUI Instance
    {
        get
        {
            if (instance == null) instance = FindFirstObjectByType<EmoteBalloonUI>();
            if (instance == null) instance = new GameObject("EmoteBalloonUI").AddComponent<EmoteBalloonUI>();
            return instance;
        }
    }

    public static IEnumerator Show(Transform target, string emote, float seconds)
    {
        if (Cine.Skipping || target == null) yield break;
        yield return Instance.Run(target, emote, seconds);
    }

    public static void Pop(Transform target, string emote, float seconds)
    {
        if (Cine.Skipping || target == null) return;
        Instance.StartCoroutine(Instance.Run(target, emote, seconds));
    }

    private IEnumerator Run(Transform target, string emote, float seconds)
    {
        RectTransform bubble = CreateBubble(emote);
        if (bubble == null) yield break;
        SpriteRenderer body = target.GetComponentInChildren<SpriteRenderer>();

        float t = 0f;
        while (t < seconds && target != null && !Cine.Skipping)
        {
            t += Time.deltaTime;
            Follow(bubble, target, body, t);
            yield return null;
        }
        Destroy(bubble.gameObject);
    }

    // 처음 0.15초는 톡 튀어나오고, 그 뒤 살짝 위아래로 흔들림
    private void Follow(RectTransform bubble, Transform target, SpriteRenderer body, float t)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 head = body != null ? new Vector3(body.bounds.center.x, body.bounds.max.y, 0f) : target.position + Vector3.up * headHeight;
        Vector3 screen = cam.WorldToScreenPoint(head + new Vector3(0f, gap + Mathf.Sin(t * 6f) * 0.03f, 0f));
        float half = BubbleSize.x * 0.5f * root.lossyScale.x, tall = BubbleSize.y * root.lossyScale.y;
        screen.x = Mathf.Clamp(screen.x, half, Screen.width - half);          // 화면 밖으로 안 나가게
        screen.y = Mathf.Clamp(screen.y, 0f, Screen.height - tall);
        bubble.position = screen;
        float pop = t < 0.15f ? Mathf.Lerp(0.3f, 1.15f, t / 0.15f) : Mathf.Lerp(1.15f, 1f, (t - 0.15f) / 0.1f);
        bubble.localScale = Vector3.one * Mathf.Max(0.3f, pop);
    }

    private RectTransform CreateBubble(string emote)
    {
        try
        {
            EnsureBuilt();
            RectTransform bubble = UIBuild.Rect(root, "Emote", Vector2.zero, BubbleSize);
            bubble.anchorMin = bubble.anchorMax = Vector2.zero;
            bubble.pivot = new Vector2(0.5f, 0f); // 꼬리 끝(아래 가운데)이 머리 위에 오게

            Sprite art = bubbleSprite != null ? bubbleSprite : UIFrames.Get("UI_Bubble");
            Image bg = UIBuild.Image(bubble, "Bg", new Vector2(0f, BubbleSize.y * 0.5f), BubbleSize, art != null ? art : UIBuild.Circle(), Color.white);
            if (art == null) bg.gameObject.AddComponent<Outline>().effectColor = new Color(0.29f, 0.2f, 0.14f);

            // 글자는 말풍선 몸통(꼬리 뺀 윗부분) 가운데
            Text label = UIBuild.Text(bubble, "Label", font, 40, new Vector2(0f, BubbleSize.y * 0.63f), new Vector2(BubbleSize.x, 60f), new Color(0.29f, 0.2f, 0.14f));
            label.fontStyle = FontStyle.Bold;
            label.text = emote;
            return bubble;
        }
        catch (Exception e)
        {
            Debug.LogError($"[EmoteBalloonUI] 말풍선 생성 오류: {e}");
            return null;
        }
    }

    private void EnsureBuilt()
    {
        if (root != null) return;

        font = PuzzleUITheme.GetBodyFont();
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        root = UIBuild.CreateCanvas(transform, "EmoteCanvas", sortingOrder, out _);
    }
}
