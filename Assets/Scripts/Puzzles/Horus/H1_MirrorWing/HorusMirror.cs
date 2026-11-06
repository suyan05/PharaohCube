using System;
using UnityEngine;

// H1 거울 석상 하나 (HorusSunMirrors가 만듦). E: '/' -> '\' -> 눕힘(빛 통과) -> '/' ...
public class HorusMirror : MonoBehaviour, IInteractable
{
    private HorusSunMirrors puzzle;
    private SpriteRenderer sr;
    private Sprite[] sprites;
    private int index;

    public int State { get; private set; } = 2; // 처음엔 모두 눕힘 (빛이 그냥 지나감)

    public static HorusMirror Create(HorusSunMirrors puzzle, Transform parent, int index, Vector3 pos, Sprite a, Sprite b, Sprite flat)
    {
        SpriteRenderer sr = HorusWorld.Prop(parent, $"H1_Mirror_{index + 1}", flat, pos, 0.8f, new Vector2(0.62f, 0.35f));
        HorusMirror m = sr.gameObject.AddComponent<HorusMirror>();
        m.puzzle = puzzle;
        m.sr = sr;
        m.index = index;
        m.sprites = new[] { a, b, flat };
        BoxCollider2D reach = sr.gameObject.AddComponent<BoxCollider2D>(); // E가 잘 닿게 그림 전체 크기의 조사 상자
        reach.isTrigger = true;
        return m;
    }

    public string GetPrompt()
    {
        if (!puzzle.CanTurn) return "거울 석상";
        string now = State == 0 ? "/" : (State == 1 ? "\\" : "눕힘");
        return $"E: 거울 돌리기 (지금: {now})";
    }

    public void Interact(GameObject who)
    {
        try
        {
            if (DialogueBox.IsOpen || Cine.Running) return;
            if (!puzzle.CanTurn)
            {
                HorusWorld.Say("청동 거울이 달린 매 석상이다. 아직 비출 빛이 없어.");
                return;
            }
            State = (State + 1) % 3;
            if (sprites[State] != null) sr.sprite = sprites[State];
            GameAudio.Play("sfx_brush_done", 0.4f, 1.5f);
            puzzle.Trace();
        }
        catch (Exception e)
        {
            Debug.LogError($"[거울 석상] Interact 오류: {e}");
        }
    }
}
