using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// 고고학 수첩 화면 조각 만들기 (도형 버전, 1920x1080 기준)
// 색은 기획서 4.2 아트 요청 색을 그대로 씀 -> 아트가 와도 느낌이 비슷하게
public static class NotebookBuild
{
    public static readonly Color Leather = Hex("#5A3420");
    public static readonly Color PageL = Hex("#EFE2C4");
    public static readonly Color PageR = Hex("#E3D0A6");
    public static readonly Color Edge = Hex("#6E4A2A");

    // 뒤 배경 (누르면 닫힘)
    public static void Backdrop(RectTransform root, Action onClick)
    {
        RectTransform rt = UIBuild.Stretch(root, "Backdrop");
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.65f);
        Button b = rt.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        b.onClick.AddListener(() => onClick());
    }

    // 가죽 표지 + 왼쪽, 오른쪽 종이 + 가운데 접힌 그림자
    public static RectTransform Book(RectTransform root, out RectTransform left, out RectTransform right)
    {
        Image cover = Solid(root, "Cover", Vector2.zero, new Vector2(1640f, 940f), Leather, true);
        RectTransform book = cover.rectTransform;
        left = Solid(book, "PageL", new Vector2(-395f, 0f), new Vector2(780f, 880f), PageL, false).rectTransform;
        right = Solid(book, "PageR", new Vector2(395f, 0f), new Vector2(780f, 880f), PageR, false).rectTransform;
        Solid(book, "Spine", Vector2.zero, new Vector2(40f, 880f), new Color(Edge.r, Edge.g, Edge.b, 0.55f), false);
        return book;
    }

    // 왼쪽 가장자리 종이 태그 탭
    public static Button Tab(RectTransform book, string label, Vector2 pos, UnityAction onClick)
    {
        Image img = Solid(book, $"Tab_{label}", pos, new Vector2(120f, 80f), new Color(0.85f, 0.75f, 0.56f), true);
        img.rectTransform.SetAsFirstSibling(); // 표지 뒤에서 삐져나온 느낌
        Button b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        b.onClick.AddListener(onClick);
        Text t = UIBuild.Text(img.rectTransform, "Label", PuzzleUITheme.GetBodyFont(), 22, new Vector2(-14f, 0f), new Vector2(90f, 70f), Edge);
        t.text = label;
        return b;
    }

    public static Button Arrow(RectTransform parent, string label, Vector2 pos, UnityAction onClick)
    {
        Image img = Solid(parent, $"Arrow_{label}", pos, new Vector2(48f, 48f), Edge, true);
        img.sprite = UIBuild.Circle();
        Button b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        b.onClick.AddListener(onClick);
        UIBuild.Text(img.rectTransform, "Label", PuzzleUITheme.GetBodyFont(), 26, Vector2.zero, new Vector2(48f, 48f), PageL).text = label;
        return b;
    }

    public static Image Solid(RectTransform parent, string name, Vector2 pos, Vector2 size, Color color, bool clickable)
    {
        Image img = UIBuild.Image(parent, name, pos, size, null, color);
        img.raycastTarget = clickable;
        return img;
    }

    public static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }
}
