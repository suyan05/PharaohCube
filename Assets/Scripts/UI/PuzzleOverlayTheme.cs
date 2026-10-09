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

    private Image backdrop;

    private PuzzleUITheme Theme => theme != null ? theme : PuzzleUITheme.Current;

    private void OnEnable()
    {
        PuzzleOverlayFocus.Enter(this);

        try
        {
            if (addBackdrop) EnsureBackdrop();
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