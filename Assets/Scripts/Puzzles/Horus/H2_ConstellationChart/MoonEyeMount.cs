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
        return Mounted ? "E: 은빛으로 빛나는 장착대" : "E: 달의 눈 장착대 조사";
    }

    public void Interact(GameObject player)
    {
        try
        {
            if (Mounted)
            {
                Debug.Log("[달의 눈] 이미 장착되어 있다. 성도판의 선이 은빛으로 그려진다.");
                return;
            }
            ItemInventory inventory = ItemInventory.Instance;
            if (inventory == null || !inventory.Consume(HorusItemIds.MoonEye))
            {
                Debug.Log("[달의 눈] 눈 모양 홈이 비어 있다. 무언가를 올려둘 수 있을 것 같다.");
                return;
            }
            GameManager.Instance.SetFlag(mountedFlag);
            Debug.Log("[달의 눈] 달의 눈을 장착했다. 이제 별을 잇는 선이 은빛으로 빛난다.");
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