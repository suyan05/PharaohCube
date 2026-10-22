using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 캐릭터 머리 위 감정 말풍선 (쯔꾸르 느낌표, 물음표, 말줄임)
// - yield return EmoteBalloonUI.Show(player, "!", 0.8f);   보여주고 사라질 때까지 기다림
// - EmoteBalloonUI.Pop(player, "...", 1.2f);               안 기다리고 띄우기
// 자주 쓰는 것: "!" 놀람, "?" 의문, "..." 한숨, 침묵, "!?" 당황, "~" 안도
// 아트가 오면 Bubble Sprite 칸에 말풍선 그림 넣기 (비우면 코드로 그린 흰 원)
public class EmoteBalloonUI : MonoBehaviour
{
    private static readonly Vector2 BubbleSize = new Vector2(84f, 70f);
    private static EmoteBalloonUI instance;

    [SerializeField] private Sprite bubbleSprite;
    [Tooltip("캐릭터 발 기준 위로 띄울 높이 (유닛, 플레이어 키 1.5칸 + 여유)")]
    [SerializeField] private float headHeight = 1.9f;
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

        float t = 0f;
        while (t < seconds && target != null && !Cine.Skipping)
        {
            t += Time.deltaTime;
            Follow(bubble, target, t);
            yield return null;
        }
        Destroy(bubble.gameObject);
    }

    // 처음 0.15초는 톡 튀어나오고, 그 뒤 살짝 위아래로 흔들림
    private void Follow(RectTransform bubble, Transform target, float t)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 world = target.position + new Vector3(0f, headHeight + Mathf.Sin(t * 6f) * 0.04f, 0f);
        bubble.position = cam.WorldToScreenPoint(world);
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
            bubble.pivot = new Vector2(0.5f, 0f);

            Image bg = UIBuild.Image(bubble, "Bg", new Vector2(0f, BubbleSize.y * 0.5f), BubbleSize, bubbleSprite != null ? bubbleSprite : UIBuild.Circle(), Color.white);
            if (bubbleSprite == null) bg.gameObject.AddComponent<Outline>().effectColor = new Color(0.29f, 0.2f, 0.14f);

            Text label = UIBuild.Text(bubble, "Label", font, 40, new Vector2(0f, BubbleSize.y * 0.5f), BubbleSize, new Color(0.29f, 0.2f, 0.14f));
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
