using UnityEngine;

// 화로: E로 횃불을 다시 받거나 다시 붙임 (중앙 홀 화로 / 테스트용 화로)
// 라의 불씨가 없는 플레이어는 여기서만 횃불을 되찾을 수 있음
[RequireComponent(typeof(Collider2D))]
public class TorchRelightPoint : MonoBehaviour, IInteractable
{
    [SerializeField] private string prompt = "화로에서 횃불 붙이기";
    [TextArea(1, 2)] [SerializeField] private string message = "화로의 불이 횃불에 옮겨붙었다.";

    public string GetPrompt()
    {
        ITorchLight t = TorchLink.Current;
        return (t != null && t.IsLit) ? "" : $"E: {prompt}";
    }

    public void Interact(GameObject player)
    {
        var torch = player.GetComponentInParent<PlayerTorchTemp>();
        if (torch == null) torch = FindAnyObjectByType<PlayerTorchTemp>();
        if (torch == null) { Debug.LogWarning($"[{name}] 플레이어에 PlayerTorchTemp가 없음", this); return; }
        if (torch.IsLit) return;
        torch.Relight(message);
    }
}
