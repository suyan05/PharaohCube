using System;
using UnityEngine;
using UnityEngine.UI;

// 연출 건너뛰기: ESC를 holdSeconds초 동안 누르고 있으면 Requested = true
// - Cine.Begin(true)일 때만 동작, 화면 오른쪽 아래에 "ESC 길게: 건너뛰기" 안내
// - 시연, 발표 때 프롤로그를 빨리 넘기는 용도 (기획서 5장 확인 필요 항목)
public class CineSkip : MonoBehaviour
{
    private const float HoldSeconds = 1f;
    private static CineSkip instance;

    private GameObject canvasGo;
    private Text hint;
    private bool armed;
    private float held;

    public static bool Requested { get; private set; }

    public static void Arm(bool allow)
    {
        Requested = false;
        if (!allow && instance == null) return;

        try
        {
            CineSkip skip = Get();
            skip.armed = allow;
            skip.held = 0f;
            skip.SetHint(allow);
        }
        catch (Exception e)
        {
            Debug.LogError($"[CineSkip] 스킵 준비 오류: {e}");
        }
    }

    private static CineSkip Get()
    {
        if (instance == null) instance = CineRunner.Instance.gameObject.AddComponent<CineSkip>();
        return instance;
    }

    private void Update()
    {
        if (!armed || Requested) return;

        held = Input.GetKey(KeyCode.Escape) ? held + Time.unscaledDeltaTime : 0f;
        if (hint != null) hint.text = held > 0f ? "건너뛰는 중..." : "ESC 길게: 건너뛰기";

        if (held < HoldSeconds) return;
        Requested = true;
        SetHint(false);
    }

    private void SetHint(bool show)
    {
        if (show) EnsureBuilt();
        if (canvasGo != null) canvasGo.SetActive(show);
    }

    private void EnsureBuilt()
    {
        if (canvasGo != null) return;

        Font font = PuzzleUITheme.GetBodyFont();
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        RectTransform root = UIBuild.CreateCanvas(transform, "SkipCanvas", 95, out canvasGo);
        hint = UIBuild.Text(root, "Hint", font, 22, new Vector2(760f, -510f), new Vector2(360f, 36f), new Color(1f, 1f, 1f, 0.6f));
        hint.alignment = TextAnchor.MiddleRight;
    }
}
