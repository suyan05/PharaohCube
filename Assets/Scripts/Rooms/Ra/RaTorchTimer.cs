using System;
using UnityEngine;

// 라의 방 횃불 시간 (총정리 문서 4.1)
// - 횃불 시간은 "어두운 곳"에 있을 때만 줄어듦. 켜진 벽 횃불 빛 안, 성소 화로 옆에서는 멈춤
//   (실제 시간 계산은 팀원 PlayerTorchTemp가 하고, 여기서는 TorchLink.SetBurnPaused로 멈춤만 요청)
// - 다 타서 꺼지면: 라의 방 기준 안내 문구로 바꿔 띄움 (켜진 벽 횃불 / 성소 화로에서 다시 붙이기)
// - 화면 왼쪽 위 불꽃 게이지는 RaTorchGaugeUI가 그림
public class RaTorchTimer : MonoBehaviour
{
    [SerializeField] private string outMessage = "횃불이 꺼져간다... 켜진 벽 횃불이나 성소 화로에서 다시 붙이자.";
    [SerializeField] private string emberMessage = "횃불이 꺼졌다. 하지만 라의 불씨는 아직 타오른다. (E 길게: 다시 붙이기)";

    private PlayerTorchTemp torch;
    private bool subscribed;
    private bool paused;

    public static bool InBrightPlace { get; private set; }

    private void Update()
    {
        try
        {
            if (torch == null) FindTorch();
            if (torch == null) return;

            bool bright = IsBright(torch.transform.position);
            InBrightPlace = bright;
            if (bright == paused) return;
            paused = bright;
            TorchLink.SetBurnPaused(this, bright);
        }
        catch (Exception e)
        {
            Debug.LogError($"[RaTorchTimer] 오류: {e}");
            enabled = false;
        }
    }

    private static bool IsBright(Vector3 pos)
    {
        return WallTorch.AnyLitNear(pos) || RaBrazier.AnyNear(pos);
    }

    private void FindTorch()
    {
        torch = FindFirstObjectByType<PlayerTorchTemp>();
        if (torch == null || subscribed) return;
        torch.OnLitChanged += HandleLitChanged;
        subscribed = true;
    }

    private void OnDisable()
    {
        TorchLink.SetBurnPaused(this, false);
        paused = false;
        InBrightPlace = false;
    }

    private void OnDestroy()
    {
        if (subscribed && torch != null) torch.OnLitChanged -= HandleLitChanged;
    }

    // 팀원 코드의 "중앙 홀 화로" 문구를 라의 방에 맞는 문구로 덮어씀 (같은 안내 상자라 바로 바뀜)
    private void HandleLitChanged(bool lit)
    {
        if (lit || torch == null) return;
        AnubisToast.Show(torch.HasEmber ? emberMessage : outMessage, 4f);
        CineCamera.Shake(0.15f, 0.03f);
    }
}
