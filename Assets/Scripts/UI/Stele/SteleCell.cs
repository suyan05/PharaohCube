using UnityEngine;
using UnityEngine.UI;

// 비석 글자 칸 하나: 상형문자 그림(glyph) 위에 한글 글자(letter)가 겹쳐 있음
// - Decode(): 해독됨 표시 -> Tick이 0.35초 동안 상형문자를 지우고 한글을 금빛으로 떠올림
// - 해독된 칸은 다시 안 돌아감 (비춘 만큼 쌓임)
public class SteleCell
{
    private const float RevealSeconds = 0.35f;

    private readonly Image glyph;
    private readonly Text letter;
    private readonly Color glyphColor;
    private readonly Color letterColor;
    private float reveal; // 0 = 상형문자, 1 = 한글

    public RectTransform Rect { get; }
    public bool Decoded { get; private set; }

    public SteleCell(RectTransform rect, Image glyph, Text letter)
    {
        Rect = rect;
        this.glyph = glyph;
        this.letter = letter;
        glyphColor = glyph.color;
        letterColor = letter.color;
        Apply();
    }

    public void Decode()
    {
        Decoded = true;
    }

    public void ForceDecoded()
    {
        Decoded = true;
        reveal = 1f;
        Apply();
    }

    // 렌즈 안에 있으면 상형문자를 살짝 밝게 (아직 해독 전이라도 "비추고 있다" 느낌)
    public void Tick(float dt, bool underLens)
    {
        if (Decoded && reveal < 1f) reveal = Mathf.Min(1f, reveal + dt / RevealSeconds);
        Apply();
        if (!Decoded) glyph.color = underLens ? Color.Lerp(glyphColor, Color.white, 0.5f) : glyphColor;
    }

    private void Apply()
    {
        Color g = glyphColor;
        g.a *= 1f - reveal;
        glyph.color = g;

        Color l = letterColor;
        l.a *= reveal;
        letter.color = l;
        Rect.localScale = Vector3.one * (1f + Mathf.Sin(reveal * Mathf.PI) * 0.25f); // 떠오를 때 톡
    }
}
