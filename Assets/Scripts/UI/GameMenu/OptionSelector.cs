using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// [추가] "<  값  >" 고르기 (해상도, 화면 모드, 언어 등)
// - 마우스: 양쪽 화살표 클릭 / 키보드·패드: 이 줄을 고른 상태에서 좌우 방향키
public class OptionSelector : Selectable
{
    private TextMeshProUGUI valueLabel;
    private Func<int, string> labelOf;
    private Action<int> onChanged;
    private int count;
    private int index;

    public int Index => index;

    // 코드로 만들기 (MenuUI 스타일)
    public static OptionSelector Create(string name, Transform parent, int count, int start, Func<int, string> labelOf, Action<int> onChanged)
    {
        RectTransform rt = MenuUI.Rect(name, parent);
        MenuUI.Size(rt, 500f, 56f);
        var bg = rt.gameObject.AddComponent<Image>();
        bg.sprite = MenuUI.Theme.buttonSprite;
        bg.type = bg.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        bg.color = Color.white;

        var sel = rt.gameObject.AddComponent<OptionSelector>();
        sel.targetGraphic = bg;
        sel.colors = MenuUI.Colors();
        sel.count = Mathf.Max(1, count);
        sel.labelOf = labelOf;
        sel.onChanged = onChanged;
        rt.gameObject.AddComponent<MenuSelectSound>();

        Button prev = Arrow("Prev", rt, "<", new Vector2(0f, 0.5f), () => sel.Step(-1));
        Button next = Arrow("Next", rt, ">", new Vector2(1f, 0.5f), () => sel.Step(1));
        // 화살표 버튼은 키보드 이동 대상에서 뺌 (줄 전체가 하나의 칸)
        prev.navigation = new Navigation { mode = Navigation.Mode.None };
        next.navigation = new Navigation { mode = Navigation.Mode.None };

        sel.valueLabel = MenuUI.Text("Value", rt, null, 30f, MenuUI.Theme.textColor);
        MenuUI.Stretch(sel.valueLabel.rectTransform, 64f);
        sel.SetIndex(start, false);
        return sel;
    }

    private static Button Arrow(string name, RectTransform parent, string glyph, Vector2 anchor, Action onClick)
    {
        RectTransform rt = MenuUI.Rect(name, parent);
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.sizeDelta = new Vector2(56f, 56f);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0f); // 투명 (글자만 보임, 클릭은 됨)
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        b.onClick.AddListener(() => { MenuUI.Click(); onClick(); });
        TextMeshProUGUI t = MenuUI.Text("Glyph", rt, null, 34f, MenuUI.Theme.accentColor, TextAlignmentOptions.Center, glyph);
        MenuUI.Stretch(t.rectTransform);
        return b;
    }

    public void Step(int dir) => SetIndex((index + dir + count) % count, true);

    public void SetIndex(int i, bool notify)
    {
        index = Mathf.Clamp(i, 0, count - 1);
        RefreshLabel();
        if (notify) onChanged?.Invoke(index);
    }

    public void RefreshLabel()
    {
        if (valueLabel != null && labelOf != null) valueLabel.text = labelOf(index);
    }

    // 좌우 방향키 = 값 바꾸기, 위아래 = 다른 줄로 이동
    public override void OnMove(AxisEventData e)
    {
        if (e.moveDir == MoveDirection.Left) { MenuUI.Move(); Step(-1); e.Use(); return; }
        if (e.moveDir == MoveDirection.Right) { MenuUI.Move(); Step(1); e.Use(); return; }
        base.OnMove(e);
    }
}
