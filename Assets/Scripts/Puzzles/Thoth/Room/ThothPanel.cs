using System;
using UnityEngine;
using UnityEngine.UI;

// 토트의 방 퍼즐 화면 공통 (벽화 맞추기, 서기관 책상, 달 시계)
// - 화면은 코드로 만듦 (처음 열 때 한 번). Build()에서 root 밑에 그림을 놓으면 됨
// - 열면: 플레이어 잠금 + 퍼즐 화면 표시(PuzzleOverlayFocus) / E로 닫음 (ESC는 설정 키라 안 씀)
// - 닫힌 프레임(ClosedFrame)에는 다른 토트 물건이 E를 무시함 (닫는 E가 다시 여는 일 방지)
public abstract class ThothPanel : MonoBehaviour
{
    public static int ClosedFrame { get; private set; } = -1;

    protected RectTransform root;
    protected Font font;
    private GameObject canvasGo;
    private int openedFrame;

    public bool IsOpen => canvasGo != null && canvasGo.activeSelf;

    protected abstract void Build();
    protected virtual void OnOpened() { }
    protected virtual void OnClosed() { }
    protected virtual bool CanClose => true;

    protected void OpenPanel()
    {
        try
        {
            if (canvasGo == null) CreateCanvas();
            canvasGo.SetActive(true);
            openedFrame = Time.frameCount;
            PlayerLock.Lock(this);
            PuzzleOverlayFocus.Enter(this);
            ObjectiveBoxUI.Hide();
            GameAudio.Play("sfx_th_paper", 0.6f);
            OnOpened();
        }
        catch (Exception e)
        {
            Debug.LogError($"[토트 화면] 열기 오류 ({GetType().Name}): {e}");
            ClosePanel();
        }
    }

    protected void ClosePanel()
    {
        if (canvasGo != null) canvasGo.SetActive(false);
        PlayerLock.Unlock(this);
        PuzzleOverlayFocus.Exit(this);
        ClosedFrame = Time.frameCount;
        try { OnClosed(); }
        catch (Exception e) { Debug.LogError($"[토트 화면] 닫기 오류 ({GetType().Name}): {e}"); }
    }

    protected virtual void Update()
    {
        if (!IsOpen || Time.frameCount <= openedFrame || !CanClose) return;
        if (DialogueBox.IsOpen || ItemPopup.IsShowing) return;
        if (Input.GetKeyDown(KeyCode.E)) ClosePanel();
    }

    private void OnDisable()
    {
        if (IsOpen) ClosePanel();
    }

    private void CreateCanvas()
    {
        root = UIBuild.CreateCanvas(null, "ThothPanel_" + GetType().Name, 80, out canvasGo);
        font = ThothUI.Font();
        var dim = UIBuild.Stretch(root, "Dim").gameObject.AddComponent<Image>();
        dim.color = new Color(0.02f, 0.03f, 0.06f, 0.86f);
        Build();
    }
}

// 토트 화면들이 같이 쓰는 작은 도구
public static class ThothUI
{
    public static readonly Color Gold = new Color(0.93f, 0.78f, 0.38f, 1f);
    public static readonly Color Ink = new Color(0.16f, 0.11f, 0.06f, 1f);
    public static readonly Color Teal = new Color(0.38f, 0.82f, 0.78f, 1f);

    public static Font Font()
    {
        Font f = PuzzleUITheme.GetBodyFont();
        return f != null ? f : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    public static Image Pic(RectTransform parent, string name, string art, Vector2 pos, Vector2 size)
    {
        Image img = UIBuild.Image(parent, name, pos, size, ThothState.Art(art), new Color(0.3f, 0.3f, 0.35f, 1f));
        img.preserveAspect = false;
        return img;
    }

    public static Text Label(RectTransform parent, string name, Font font, int size, Vector2 pos, Vector2 box, Color color, string text)
    {
        Text t = UIBuild.Text(parent, name, font, size, pos, box, color);
        t.text = text;
        Outline o = t.gameObject.AddComponent<Outline>();
        o.effectColor = new Color(0f, 0f, 0f, 0.75f);
        return t;
    }

    // 누르면 action을 부르는 그림 버튼 (sprite가 없으면 색 상자)
    public static Button ButtonPic(RectTransform parent, string name, string art, Vector2 pos, Vector2 size, Action action)
    {
        Image img = Pic(parent, name, art, pos, size);
        img.raycastTarget = true;
        Button b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        b.onClick.AddListener(() => Safe(action, name));
        return b;
    }

    public static Button ButtonText(RectTransform parent, string name, Font font, string text, Vector2 pos, Vector2 size, Action action)
    {
        Button b = ButtonPic(parent, name, "UI/UI_Button_Wide", pos, size, action);
        Label((RectTransform)b.transform, "Text", font, 30, Vector2.zero, size, Ink, text).GetComponent<Outline>().enabled = false;
        return b;
    }

    public static void Safe(Action action, string where)
    {
        try { action?.Invoke(); }
        catch (Exception e) { Debug.LogError($"[토트 화면] 버튼 오류 ({where}): {e}"); }
    }
}
