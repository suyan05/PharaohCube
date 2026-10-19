using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 밤 P5 클리어 컷 (P5_Night_Clear: 태양의 배가 저승의 강을 건너 떠오름)
// - 씬에 1개, 빈 오브젝트에 붙임 -> 처음 재생할 때 전용 Canvas를 코드로 만듦
// - SunCycleStation이 밤 클리어 때 Play(끝났을 때 할 일)로 부름
// - 페이드 인 -> 최소 시간 지나면 "E / 클릭: 계속" -> 누르면 페이드 아웃 -> 끝
// - 재생 중에는 이동(PlayerController2D)과 E키 조사(InteractionSystem)를 막음
public class RaNightFinale : MonoBehaviour
{
    [Header("컷 이미지")]
    [Tooltip("P5_Night_Clear (1920x1080). 비우면 컷 없이 바로 끝남")]
    [SerializeField] private Sprite cutSprite;
    [SerializeField, TextArea] private string caption = "태양의 배가 저승의 강을 건너, 다시 동쪽에서 떠오른다.";

    [Header("타이밍 (초)")]
    [SerializeField, Min(0.05f)] private float fadeTime = 0.8f;
    [Tooltip("이 시간이 지나야 넘길 수 있음 (바로 넘겨버리는 것 방지)")]
    [SerializeField, Min(0f)] private float minShowTime = 1.5f;

    [Header("화면")]
    [Tooltip("퍼즐 화면, 목표 문구보다 위에 그려지게")]
    [SerializeField] private int sortingOrder = 105;

    private GameObject canvasGo;
    private CanvasGroup group;
    private Text hintText;
    private Font font;
    private bool playing;

    private PlayerController2D playerController;
    private InteractionSystem playerInteraction;
    private Rigidbody2D playerRb;

    public bool IsPlaying => playing;

    // ================= 재생 =================
    public void Play(Action onDone)
    {
        try
        {
            if (cutSprite == null || playing)
            {
                if (cutSprite == null) Debug.LogWarning($"[{name}] Cut Sprite가 비어 있어서 컷 없이 진행");
                onDone?.Invoke();
                return;
            }

            EnsureBuilt();
            StartCoroutine(PlayRoutine(onDone));
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 클리어 컷 오류: {e}");
            onDone?.Invoke();
        }
    }

    private IEnumerator PlayRoutine(Action onDone)
    {
        playing = true;
        SetPlayerLocked(true);
        hintText.enabled = false;
        canvasGo.SetActive(true);

        yield return Fade(0f, 1f);
        yield return new WaitForSeconds(minShowTime);

        hintText.enabled = true;
        yield return null; // 힌트가 뜬 프레임의 입력은 무시
        while (!PressedContinue()) yield return null;

        yield return Fade(1f, 0f);
        canvasGo.SetActive(false);
        SetPlayerLocked(false);
        playing = false;
        onDone?.Invoke();
    }

    private IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / fadeTime));
            yield return null;
        }
        group.alpha = to;
    }

    private static bool PressedContinue()
    {
        return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space)
            || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0);
    }

    private void OnDisable()
    {
        if (!playing) return;
        StopAllCoroutines();
        if (canvasGo != null) canvasGo.SetActive(false);
        SetPlayerLocked(false);
        playing = false;
    }

    // ================= 플레이어 잠금 =================
    private void SetPlayerLocked(bool locked)
    {
        if (playerController == null)
        {
            playerController = FindAnyObjectByType<PlayerController2D>();
            if (playerController == null) return;
            playerInteraction = playerController.GetComponent<InteractionSystem>();
            playerRb = playerController.GetComponent<Rigidbody2D>();
        }

        playerController.enabled = !locked;
        if (playerInteraction != null) playerInteraction.enabled = !locked;
        if (locked && playerRb != null) playerRb.linearVelocity = Vector2.zero;
    }

    // ================= 화면 만들기 (처음 한 번) =================
    private void EnsureBuilt()
    {
        if (canvasGo != null) return;

        font = PuzzleUITheme.GetBodyFont();
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        RectTransform root = BuildCanvas();
        Image backdrop = CreateStretch(root, "Backdrop").AddComponent<Image>();
        backdrop.color = Color.black;
        backdrop.raycastTarget = true;

        Image cut = CreateStretch(root, "Cut").AddComponent<Image>();
        cut.sprite = cutSprite;
        cut.preserveAspect = true;
        cut.raycastTarget = false;

        BuildTexts(root);
        canvasGo.SetActive(false);
    }

    private RectTransform BuildCanvas()
    {
        canvasGo = new GameObject("NightFinaleCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        canvasGo.transform.SetParent(transform, false);

        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        group = canvasGo.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        return canvasGo.GetComponent<RectTransform>();
    }

    private void BuildTexts(RectTransform root)
    {
        Text cap = CreateText(root, "Caption", 34, new Vector2(0f, 120f), new Vector2(1600f, 60f), Hex("#F6F1E7"));
        cap.text = caption;

        hintText = CreateText(root, "Hint", 22, new Vector2(760f, 50f), new Vector2(320f, 36f), Hex("#E8B23A"));
        hintText.text = "E / 클릭: 계속";
    }

    // ================= 도우미 함수 =================
    private static GameObject CreateStretch(RectTransform parent, string objName)
    {
        var go = new GameObject(objName, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return go;
    }

    // 화면 아래 가운데 기준
    private Text CreateText(RectTransform parent, string objName, int fontSize, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(objName, typeof(RectTransform), typeof(Text));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var t = go.GetComponent<Text>();
        t.font = font;
        t.fontSize = fontSize;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.raycastTarget = false;
        return t;
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }
}