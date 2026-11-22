using System;
using System.Collections;
using UnityEngine;

// 서고에서 줍는 것 (흩어진 벽화 조각, 달 조각) - 빌더가 놓고 칸을 채움
// - fresco + piece: 벽화 조각 (플래그 F_TH_P_A1 등) / itemId: 아이템 (달 조각 등)
// - requireFlag: 이 플래그가 켜져야 줄 수 있음 (따오기 상: 벽화 A 뒤) -> 그 전엔 lockedLines
// - hideUntilFlag: 켜기 전엔 아예 안 보임 (달빛 속 조각: 달 시계 뒤)
// - hold: E를 길게 눌러 먼지 털기 (붓이 있으면 더 빨리)
// - keepObject: 주운 뒤에도 물건은 남음 (따오기 상, 책장) -> 그 뒤엔 afterLines
public class ThothPickup : MonoBehaviour, IInteractable
{
    [Header("무엇을 주나")]
    [SerializeField] private string fresco = "";
    [SerializeField] private int piece = -1;
    [SerializeField] private string itemId = "";

    [Header("조건")]
    [SerializeField] private string requireFlag = "";
    [SerializeField] private bool hideUntilFlag;

    [Header("모습")]
    [SerializeField] private string prompt = "E: 살펴보기";
    [SerializeField] private bool hold;
    [SerializeField] private float holdSeconds = 1.4f;
    [SerializeField] private bool keepObject;
    [SerializeField] private SpriteRenderer art;
    [SerializeField] private SpriteRenderer glint;

    [Header("대사 (화자, 글 순서로)")]
    [SerializeField] private string[] lines = new string[0];
    [SerializeField] private string[] lockedLines = new string[0];
    [SerializeField] private string[] afterLines = new string[0];

    private const string Brush = "TOOL_BRUSH";
    private const float HoldRange = 1.6f;
    private bool busy;
    private Collider2D col;

    private bool IsPiece => !string.IsNullOrEmpty(fresco) && piece >= 0;
    private string TakenFlag => IsPiece ? ThothIds.Piece(fresco, piece) : "F_TH_GOT_" + itemId;
    private bool Taken => ThothState.Has(TakenFlag) || (!IsPiece && ThothState.HasItem(itemId));
    private bool Unlocked => string.IsNullOrEmpty(requireFlag) || ThothState.Has(requireFlag);

    // 빌더가 부름 (Inspector 칸 채우기)
    public void Setup(string fresco, int piece, string itemId, string prompt, string[] lines)
    {
        this.fresco = fresco;
        this.piece = piece;
        this.itemId = itemId;
        this.prompt = prompt;
        this.lines = lines;
    }

    private void Awake()
    {
        col = GetComponent<Collider2D>();
    }

    private void Update()
    {
        bool visible = !(hideUntilFlag && !Unlocked) && (keepObject || !Taken);
        if (art != null) art.enabled = visible;
        if (col != null) col.enabled = visible;
        if (glint == null) return;
        glint.enabled = visible && !Taken && Unlocked;
        if (glint.enabled) glint.color = new Color(1f, 0.9f, 0.55f, 0.35f + Mathf.Sin(Time.time * 3f + transform.position.x) * 0.25f);
    }

    public string GetPrompt()
    {
        if (Taken || !Unlocked) return "E: 살펴보기";
        return hold ? "E 길게: " + prompt.Replace("E: ", "") : prompt;
    }

    public void Interact(GameObject who)
    {
        try
        {
            if (busy || ThothState.Busy) return;
            if (Taken) Cine.Run(Talk(afterLines));
            else if (!Unlocked) Cine.Run(Talk(lockedLines));
            else if (hold) StartCoroutine(HoldThenTake(who != null ? who.transform : null));
            else Cine.Run(Take());
        }
        catch (Exception e)
        {
            Debug.LogError($"[토트 줍기] Interact 오류 ({name}): {e}");
        }
    }

    private IEnumerator Talk(string[] pairs)
    {
        if (pairs == null || pairs.Length < 2) yield break;
        busy = true;
        Cine.Begin(false);
        yield return Cine.Say(ThothState.Lines(pairs));
        Cine.End();
        busy = false;
    }

    // E를 누르고 있는 동안 링이 참 (손을 떼거나 멀어지면 취소)
    private IEnumerator HoldThenTake(Transform player)
    {
        busy = true;
        float need = ThothState.HasItem(Brush) ? holdSeconds * 0.6f : holdSeconds, t = 0f;
        while (Input.GetKey(KeyCode.E) && t < need && InRange(player))
        {
            t += Time.deltaTime;
            HoldRingUI.Show(transform.position + Vector3.up * 0.6f, t / need);
            GameAudio.PlayThrottled("sfx_brush", 0.35f, 0.5f);
            yield return null;
        }
        HoldRingUI.Hide();
        busy = false;
        if (t < need) yield break;
        GameAudio.Play("sfx_brush_done", 0.6f);
        Cine.Run(Take());
    }

    private bool InRange(Transform player)
    {
        return player == null || Vector2.Distance(player.position, transform.position) <= HoldRange;
    }

    private IEnumerator Take()
    {
        busy = true;
        Cine.Begin(false);
        yield return Cine.Say(ThothState.Lines(lines));
        ThothState.Set(TakenFlag);
        if (IsPiece) yield return ThothFrescoWall.ShowPopup(ThothIds.PieceItem(fresco));
        else yield return ThothFrescoWall.GrantPopup(itemId);
        Cine.End();
        busy = false;
    }
}
