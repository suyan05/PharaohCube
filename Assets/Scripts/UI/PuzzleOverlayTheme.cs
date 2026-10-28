using System;
using UnityEngine;
using UnityEngine.UI;

// 퍼즐 오버레이(P1_Overlay ~ P5_Overlay)에 붙이는 컴포넌트
// 켜질 때: 배경판 깔기 + 글자를 테마 폰트로 교체 + "퍼즐 열림" 알림
// 꺼질 때: "퍼즐 닫힘" 알림
// 각 퍼즐 화면 스크립트는 수정할 필요 없음
[DisallowMultipleComponent]
public class PuzzleOverlayTheme : MonoBehaviour
{
    [Tooltip("비워두면 Resources/PuzzleUITheme 사용")]
    [SerializeField] private PuzzleUITheme theme;

    [Tooltip("글자를 테마 폰트로 바꾸기")]
    [SerializeField] private bool applyFont = true;

    [Tooltip("화면 전체 어두운 배경판 깔기 (P1처럼 화면 스크립트가 이미 배경을 그리면 끄기)")]
    [SerializeField] private bool addBackdrop = true;

    [Tooltip("이 크기 이상인 글자는 제목 폰트(Bold) 사용")]
    [SerializeField] private int titleFontSizeMin = 40;

    [Header("[추가] 파피루스 배경판 (뒤에 배경 그림이 없는 퍼즐 화면용: P1, P4)")]
    [Tooltip("배경판 그림 (지금은 단서 화면과 같은 두루마리 UI_Journal_Frame). 비우면 배경판 없음")]
    [SerializeField] private Sprite papyrusSprite;
    [SerializeField] private Vector2 papyrusSize = new Vector2(1500f, 1100f);
    [SerializeField] private Vector2 papyrusPos = Vector2.zero;
    [Tooltip("파피루스 위에서 흰 글자가 안 읽히니까, 어두운 판 위가 아닌 밝은 글자를 진한 갈색으로 바꿈")]
    [SerializeField] private bool inkLightText = true;

    private static readonly Color Ink = new Color(0.23f, 0.14f, 0.08f);      // #3A2414
    private static readonly Color InkGold = new Color(0.48f, 0.29f, 0.06f);  // #7A4A10

    private Image backdrop;
    private Image papyrus;

    private PuzzleUITheme Theme => theme != null ? theme : PuzzleUITheme.Current;

    private void OnEnable()
    {
        PuzzleOverlayFocus.Enter(this);

        try
        {
            if (addBackdrop) EnsureBackdrop();
            if (papyrusSprite != null) EnsurePapyrus();
            if (applyFont) ApplyFonts();
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 오버레이 테마 적용 오류: {e}");
        }
    }

    private void OnDisable()
    {
        PuzzleOverlayFocus.Exit(this);
    }

    // 배경판은 항상 맨 뒤 (화면 스크립트가 나중에 만든 UI보다 뒤에 있게 매번 순서 정리)
    private void EnsureBackdrop()
    {
        if (backdrop == null) backdrop = CreateBackdrop();

        PuzzleUITheme t = Theme;
        backdrop.color = t != null ? t.BackdropColor : new Color(0f, 0f, 0f, 0.88f);
        backdrop.transform.SetAsFirstSibling();
    }

    private Image CreateBackdrop()
    {
        var go = new GameObject("ThemeBackdrop", typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(transform, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = go.GetComponent<Image>();
        img.raycastTarget = true; // 뒤쪽 맵 클릭 막기
        return img;
    }

    // [추가] 파피루스 판: 어두운 배경판 바로 위, 퍼즐 내용보다 뒤
    private void EnsurePapyrus()
    {
        if (papyrus == null)
        {
            var go = new GameObject("PapyrusPanel", typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(transform, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            papyrus = go.GetComponent<Image>();
            papyrus.raycastTarget = false;
        }
        papyrus.sprite = papyrusSprite;
        // [수정] 두루마리 틀처럼 테두리(9-slice)가 정해진 그림이면 막대는 그대로 두고 가운데만 늘림
        papyrus.type = papyrusSprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
        papyrus.rectTransform.sizeDelta = papyrusSize;
        papyrus.rectTransform.anchoredPosition = papyrusPos;
        papyrus.transform.SetSiblingIndex(BackgroundCount());
        if (inkLightText) InkLightTexts();
    }

    // 맨 앞쪽에 깔린 화면 전체 배경판(어두운 판 등) 개수 -> 파피루스는 그 바로 위에
    private int BackgroundCount()
    {
        int n = 0;
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform c = transform.GetChild(i);
            if (c == papyrus.transform) continue;
            var rt = c as RectTransform;
            bool full = rt != null && rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one && c.GetComponent<Image>() != null;
            if (!full) break;
            n++;
        }
        return n;
    }

    private void InkLightTexts()
    {
        foreach (Text t in GetComponentsInChildren<Text>(true))
        {
            float lum = Luminance(t.color);
            if (lum < 0.6f || !Inside(papyrus.rectTransform, t.transform.position) || OnDarkPlate(t.transform)) continue; // 파피루스 밖 글자는 그대로
            Color ink = lum > 0.8f ? Ink : InkGold;
            t.color = new Color(ink.r, ink.g, ink.b, t.color.a);
        }
    }

    // 글자가 어떤 판 "안"에 놓여 있으면 원래 색 그대로 (버튼, 그림 판, 어두운 판 위의 흰 글자)
    // 판 밖으로 나와 파피루스 위에 놓인 글자(카드 아래 이름표 등)만 진하게 바꿈
    private bool OnDarkPlate(Transform t)
    {
        for (Transform p = t.parent; p != null && p != transform; p = p.parent)
        {
            if (!(p is RectTransform prt) || !Inside(prt, t.position)) continue;
            if (p.GetComponent<Selectable>() != null) return true;
            Image img = p.GetComponent<Image>();
            if (img == null || !img.enabled || img.color.a < 0.5f) continue;
            if (img.sprite != null || Luminance(img.color) < 0.6f) return true;
        }
        return false;
    }

    private static bool Inside(RectTransform rt, Vector3 worldPos)
    {
        Vector2 local = rt.InverseTransformPoint(worldPos);
        return rt.rect.Contains(local);
    }

    private static float Luminance(Color c)
    {
        return 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
    }

    private void ApplyFonts()
    {
        Font body = PuzzleUITheme.GetBodyFont();
        Font title = PuzzleUITheme.GetTitleFont();

        foreach (Text t in GetComponentsInChildren<Text>(true))
        {
            t.font = t.fontSize >= titleFontSizeMin ? title : body;
        }
    }
}