using System;
using UnityEngine;

public class CubeFaceManager : MonoBehaviour
{
    public int currentFace = 0; // 0~5, 어느 면을 보고 있는지

    public event Action<int> OnFaceChanged;

    public void MoveRight()
    {
        currentFace = (currentFace + 1) % 6;
        NotifyFaceChanged();
    }

    public void MoveLeft()
    {
        currentFace = (currentFace + 5) % 6; // -1과 같은 효과 (음수 방지)
        NotifyFaceChanged();
    }

    public void MoveUp()
    {
        currentFace = (currentFace + 5) % 6;
        NotifyFaceChanged();
    }

    public void MoveDown()
    {
        currentFace = (currentFace + 1) % 6;
        NotifyFaceChanged();
    }

    [Header("참조")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private CubeFaceState cubeFaceState;

    // 지금 보고 있는 면에 장착 가능한지 (UI가 버튼 활성/ 비활성 판단할 때 씀)
    public bool CanEquipCurrentFace()
    {
        return playerInventory.hasPlate[currentFace] && !cubeFaceState.faceActivated[currentFace];
    }

    // 장착 버튼 클릭 시 호출될 함수
    public void EquipCurrentFace()
    {
        if (!CanEquipCurrentFace())
        {
            Debug.Log("장착 불가: 형판이 없거나 이미 장착된 면입니다.");
            return;
        }
        cubeFaceState.ActivateFace(currentFace);
    }
    [ContextMenu("테스트: 현재 면 장착 시도")]
    void TestEquip() => EquipCurrentFace();

    private void NotifyFaceChanged()
    {
        Debug.Log($"[CubeFaceManager] 현재 면 변경됨 → {currentFace}번");
        OnFaceChanged?.Invoke(currentFace);
    }

    [ContextMenu("테스트: 다음 면으로 (Right)")]
    void TestMoveRight() => MoveRight();

    [ContextMenu("테스트: 이전 면으로 (Left)")]
    void TestMoveLeft() => MoveLeft();
}