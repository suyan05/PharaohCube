using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// [추가] "예 / 아니요" 확인 창 (새 게임 덮어쓰기, 종료, 시작 화면으로 등)
// Esc = 아니요
public class ConfirmDialog : MonoBehaviour
{
    private static ConfirmDialog current;
    private Action onYes, onNo;
    private Selectable restoreSelection;
    private int openedFrame;

    public static bool IsOpen => current != null;
    public static int ClosedFrame { get; private set; } = -1;

    public static void Show(string messageKey, Action yes, Action no = null, Selectable returnTo = null)
    {
        if (current != null) Destroy(current.gameObject);
        Canvas canvas = MenuUI.NewCanvas("ConfirmDialog", 1500);
        var d = canvas.gameObject.AddComponent<ConfirmDialog>();
        d.onYes = yes;
        d.onNo = no;
        d.restoreSelection = returnTo;
        d.openedFrame = Time.frameCount;
        current = d;

        MenuUI.FullScreen("Dim", canvas.transform, MenuUI.Theme.dimColor);
        Image panel = MenuUI.Panel("Panel", canvas.transform, new Vector2(820f, 380f));
        MenuUI.Column(panel.transform, 30f, new RectOffset(50, 50, 50, 40), TextAnchor.MiddleCenter);
        var msg = MenuUI.Text("Message", panel.transform, messageKey, 34f, MenuUI.Theme.textColor);
        MenuUI.Size(msg, 0f, 170f);

        RectTransform buttons = MenuUI.Rect("Buttons", panel.transform);
        MenuUI.Size(buttons, 0f, 80f);
        var h = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 40f;
        h.childAlignment = TextAnchor.MiddleCenter;
        h.childControlWidth = true; h.childControlHeight = true;
        h.childForceExpandWidth = false; h.childForceExpandHeight = true;
        Button noButton = MenuUI.Button("No", buttons, "common.no", 32f, () => d.Close(false)); // [수정] 매개변수 no와 이름 겹침
        Button yesB = MenuUI.Button("Yes", buttons, "common.yes", 32f, () => d.Close(true));
        MenuUI.Size(noButton, 260f, 0f);
        MenuUI.Size(yesB, 260f, 0f);
        MenuUI.Select(noButton); // 실수로 Enter를 눌러도 안전한 쪽
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame && Time.frameCount != openedFrame) Close(false);
    }

    private void Close(bool yes)
    {
        ClosedFrame = Time.frameCount;
        current = null;
        Action a = yes ? onYes : onNo;
        Selectable back = restoreSelection;
        Destroy(gameObject);
        if (back != null) MenuUI.Select(back);
        a?.Invoke();
    }
}
