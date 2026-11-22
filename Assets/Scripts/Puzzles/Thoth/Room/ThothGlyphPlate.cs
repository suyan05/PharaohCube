using System;
using UnityEngine;

// 지혜의 문 앞 바닥 글자판 하나 (따오기 / 저울 / 갈대 펜 / 달 / 눈) - 위에 서서 E = 이 글자 읊기
// 잠들어 있을 땐 어둡게, 깨어나면 천천히 숨 쉬듯 밝아졌다 어두워짐, 맞게 읊으면 금빛
public class ThothGlyphPlate : MonoBehaviour, IInteractable
{
    [SerializeField] private string glyph = "moon";
    [SerializeField] private SpriteRenderer art;

    private ThothGlyphSpell spell;

    public string Glyph => glyph;
    public bool Lit { get; private set; }

    private void Start()
    {
        spell = FindFirstObjectByType<ThothGlyphSpell>();
        SetLit(Lit);
    }

    private void Update()
    {
        if (art == null || Lit) return;
        bool awake = ThothState.Has(ThothIds.FrescoDone("C"));
        float v = awake ? 0.8f + Mathf.Sin(Time.time * 2f + transform.position.x) * 0.2f : 0.45f;
        art.color = new Color(v, v, v, 1f);
    }

    public void SetLit(bool on)
    {
        Lit = on;
        if (art == null) return;
        Sprite s = ThothState.Art($"World/TH_Plate_{glyph}{(on ? "_Lit" : "")}");
        if (s != null) art.sprite = s;
        art.color = Color.white;
    }

    public string GetPrompt()
    {
        if (!ThothState.Has(ThothIds.FrescoDone("C"))) return "E: 글자판 살펴보기";
        return $"E: '{ThothGlyphSpell.KoreanName(glyph)}' 글자 읊기";
    }

    public void Interact(GameObject who)
    {
        try
        {
            if (ThothState.Busy || spell == null) return;
            spell.Recite(this);
        }
        catch (Exception e)
        {
            Debug.LogError($"[글자판 {glyph}] Interact 오류: {e}");
        }
    }
}
