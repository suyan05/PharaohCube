using System;
using UnityEngine;

// OBJ_H04 달의 눈 장착대: 달의 눈을 올려두면 이후 성도판 선이 은빛이 됨
public class MoonEyeMount : MonoBehaviour, IInteractable
{
    [Header("장착 시 켤 플래그 (ConstellationChartPuzzle의 Moon Eye Flag와 같아야 함)")]
    [SerializeField] private string mountedFlag = "F_HO_MOON_EYE";

    [Header("색")]
    [SerializeField] private Color emptyColor = new Color(0.35f, 0.39f, 0.47f);
    [SerializeField] private Color mountedColor = new Color(0.79f, 0.83f, 0.88f);

    private SpriteRenderer spriteRenderer;

    private bool Mounted => GameManager.Instance != null && GameManager.Instance.HasFlag(mountedFlag);

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        RefreshVisual();
    }

    public string GetPrompt()
    {
        if (Mounted) return "E: 은빛으로 빛나는 장착대";
        bool has = ItemInventory.Instance != null && ItemInventory.Instance.Has(HorusItemIds.MoonEye);
        return has ? "E: 달의 눈 올려두기" : "E: 빈 장착대 조사 (달의 눈을 두는 곳)";
    }

    private static void Say(params string[] lines)
    {
        var d = new DialogueLine[lines.Length];
        for (int i = 0; i < lines.Length; i++) d[i] = new DialogueLine("나", lines[i]);
        DialogueBox.Play(d);
    }

    public void Interact(GameObject player)
    {
        try
        {
            if (DialogueBox.IsOpen) return;
            if (Mounted)
            {
                Say("달의 눈이 장착대에서 은은하게 빛난다. 성도판의 선이 은빛으로 그려질 거야.");
                return;
            }
            ItemInventory inventory = ItemInventory.Instance;
            if (inventory == null || !inventory.Consume(HorusItemIds.MoonEye))
            {
                // [수정] 예전엔 로그만 남아서 플레이어는 '주워지지 않는 눈알'로 보였음 -> 대사로 알려줌
                Say("주워지지 않는다... 줍는 게 아니라 눈 모양 홈이 파인 장착대야.", "우자트를 복원하면 얻는 '달의 눈'을 여기에 올려 두면 될 것 같아.");
                return;
            }
            GameManager.Instance.SetFlag(mountedFlag);
            Say("달의 눈을 장착했다. 이제 별을 잇는 선이 은빛으로 빛난다.");
            RefreshVisual();
        }
        catch (Exception e)
        {
            Debug.LogError($"[달의 눈] Interact 오류: {e}");
        }
    }

    private void RefreshVisual()
    {
        if (spriteRenderer != null) spriteRenderer.color = Mounted ? mountedColor : emptyColor;
    }
}