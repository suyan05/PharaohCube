using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 지혜의 문 주문 (몸으로 푸는 퍼즐) - 문 앞 바닥 글자판 5개를 정해진 순서로 밟고 E
// - 순서 단서: 벽화 C '되찾은 눈' 맨 아래 다섯 글자. 따오기가 오른쪽을 봄 -> 오른쪽부터 읽음
//   정답: 달 -> 눈 -> 갈대 펜 -> 저울 -> 따오기 (글자판은 섞여서 오각형으로 놓여 있음)
// - 맞으면 글자판이 금빛 + 벽 주문판의 칸이 하나씩 참 + 위쪽 HUD 고리에 글자가 들어감
// - 틀리면 전부 꺼지고 처음부터 (덜컹 + 화면 흔들림)
// - 벽화 C를 되살리기 전엔 글자판이 잠들어 있음
public class ThothGlyphSpell : MonoBehaviour
{
    public static readonly string[] Order = { "moon", "eye", "pen", "scale", "ibis" };
    private static readonly float[] SlotX = { -1.53f, -0.78f, -0.03f, 0.72f, 1.47f };
    private static readonly float[] HudX = { -182f, -92f, -2f, 88f, 180f };
    private const float SlotY = -0.53f;
    private const float HudRange = 5f;

    [SerializeField] private SpriteRenderer panelArt;   // 벽 주문판 (빌더가 채움)
    [SerializeField] private Transform plateCenter;     // 글자판 오각형 가운데 (HUD 보이는 거리 기준)

    private readonly SpriteRenderer[] slots = new SpriteRenderer[5];
    private readonly Image[] hudGlyphs = new Image[5];
    private ThothGlyphPlate[] plates = new ThothGlyphPlate[0];
    private GameObject hud;
    private Transform player;
    private int progress;
    private bool busy;

    public static string KoreanName(string glyph)
    {
        switch (glyph)
        {
            case "ibis": return "따오기";
            case "scale": return "저울";
            case "pen": return "갈대 펜";
            case "moon": return "달";
            case "eye": return "눈";
            default: return glyph;
        }
    }

    private void Start()
    {
        try
        {
            plates = FindObjectsByType<ThothGlyphPlate>(FindObjectsSortMode.None);
            for (int i = 0; i < 5; i++) slots[i] = CreateSlot(i);
            BuildHud();
            if (ThothState.Has(ThothIds.SpellDone)) ShowAllDone();
        }
        catch (Exception e)
        {
            Debug.LogError($"[주문] 준비 오류: {e}");
        }
    }

    private SpriteRenderer CreateSlot(int i)
    {
        var go = new GameObject("Slot" + i);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(SlotX[i], SlotY, 0f);
        go.transform.localScale = Vector3.one * 0.5f;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        if (panelArt != null)
        {
            sr.sortingLayerID = panelArt.sortingLayerID;
            sr.sortingOrder = panelArt.sortingOrder + 1;
        }
        return sr;
    }

    private void BuildHud()
    {
        RectTransform root = UIBuild.CreateCanvas(transform, "SpellHud", 60, out hud);
        ThothUI.Pic(root, "Panel", "UI/UI_MuralPanel", new Vector2(0f, 430f), new Vector2(480f, 200f));
        for (int i = 0; i < 5; i++)
        {
            hudGlyphs[i] = ThothUI.Pic(root, "Glyph" + i, "", new Vector2(HudX[i], 380f), new Vector2(54f, 54f));
            hudGlyphs[i].enabled = false;
        }
        hud.SetActive(false);
    }

    private void Update()
    {
        if (hud == null) return;
        if (player == null)
        {
            PlayerController2D p = FindFirstObjectByType<PlayerController2D>();
            if (p != null) player = p.transform;
        }
        bool near = player != null && plateCenter != null && Vector2.Distance(player.position, plateCenter.position) < HudRange;
        hud.SetActive(near && ThothState.Has(ThothIds.FrescoDone("C")) && !ThothState.Has(ThothIds.SpellDone) && !Cine.Running);
    }

    // ================= 글자판에서 부름 =================
    public void Recite(ThothGlyphPlate plate)
    {
        try
        {
            if (busy || ThothState.Has(ThothIds.SpellDone)) return;
            if (!ThothState.Has(ThothIds.FrescoDone("C"))) { Cine.Run(Talk(ThothLines.PlateSleeping)); return; }
            if (plate.Lit) return;
            if (plate.Glyph == Order[progress]) Advance(plate);
            else StartCoroutine(Wrong());
        }
        catch (Exception e)
        {
            Debug.LogError($"[주문] 읊기 오류: {e}");
        }
    }

    private void Advance(ThothGlyphPlate plate)
    {
        plate.SetLit(true);
        Sprite lit = ThothState.Art($"World/TH_Plate_{plate.Glyph}_Lit");
        slots[progress].sprite = lit;
        hudGlyphs[progress].sprite = ThothState.Art($"UI/UI_Glyph_{plate.Glyph}_Gold");
        hudGlyphs[progress].color = Color.white;
        hudGlyphs[progress].enabled = true;
        progress++;
        GameAudio.Play("sfx_th_chime", 0.7f, 0.75f + progress * 0.1f);
        if (progress < Order.Length) return;
        ThothState.Set(ThothIds.SpellDone);
        ShowAllDone();
    }

    private IEnumerator Wrong()
    {
        busy = true;
        GameAudio.Play("sfx_th_wrong", 0.8f);
        CineCamera.Shake(0.35f, 0.12f);
        ResetAll();
        Cine.Begin(false);
        yield return Cine.Say(ThothLines.Me, ThothLines.SpellWrong);
        Cine.End();
        busy = false;
    }

    private void ResetAll()
    {
        progress = 0;
        foreach (ThothGlyphPlate p in plates) if (p != null) p.SetLit(false);
        foreach (SpriteRenderer s in slots) if (s != null) s.sprite = null;
        foreach (Image g in hudGlyphs) if (g != null) g.enabled = false;
    }

    private void ShowAllDone()
    {
        for (int i = 0; i < 5; i++) if (slots[i] != null) slots[i].sprite = ThothState.Art($"World/TH_Plate_{Order[i]}_Lit");
        foreach (ThothGlyphPlate p in plates) if (p != null) p.SetLit(true);
        Sprite done = ThothState.Art("World/TH_Spell_Panel_Done");
        if (panelArt != null && done != null) panelArt.sprite = done;
    }

    private IEnumerator Talk(DialogueLine[] lines)
    {
        busy = true;
        Cine.Begin(false);
        yield return Cine.Say(lines);
        Cine.End();
        busy = false;
    }
}
