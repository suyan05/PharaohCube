using System;
using UnityEngine;

public class CubeFaceManager : MonoBehaviour
{
    // 면 번호: 0=라, 1=아누비스, 2=토트, 3=바스테트, 4=소베크, 5=호루스
    // 면 번호에 대응하는 신 이름 (디버그 로그용)
    private static readonly string[] FaceNames = { "라", "아누비스", "토트", "바스테트", "소베크", "호루스" };
    [Header("큐브 방향 상태 (디버그용)")]
    [SerializeField] private int front = 0; // 라
    [SerializeField] private int right = 1; // 아누비스
    [SerializeField] private int back = 2;  // 토트
    [SerializeField] private int left = 3;  // 바스테트
    [SerializeField] private int up = 4;    // 소베크
    [SerializeField] private int down = 5;  // 호루스

    public int currentFace => front;
    public event Action<int> OnFaceChanged;

    [Header("참조")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private CubeFaceState cubeFaceState;

    public void MoveRight()
    {
        (front, right, back, left) = (right, back, left, front);
        NotifyFaceChanged();
    }

    public void MoveLeft()
    {
        (front, left, back, right) = (left, back, right, front);
        NotifyFaceChanged();
    }

    public void MoveUp()
    {
        (front, up, back, down) = (up, back, down, front);
        NotifyFaceChanged();
    }

    public void MoveDown()
    {
        (front, down, back, up) = (down, back, up, front);
        NotifyFaceChanged();
    }

    private void NotifyFaceChanged()
    {
        Debug.Log($"[CubeFaceManager] 현재 면 변경됨 → {front}번({FaceNames[front]}) " +
                  $"(위:{up}({FaceNames[up]}) 아래:{down}({FaceNames[down]}) " +
                  $"좌:{left}({FaceNames[left]}) 우:{right}({FaceNames[right]}) " +
                  $"뒤:{back}({FaceNames[back]}))");
        OnFaceChanged?.Invoke(front);
    }

    // 지금 보고 있는 면에 장착 가능한지 (UI가 버튼 활성/비활성 판단할 때 씀)
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

    [ContextMenu("테스트: 오른쪽으로 (Right)")]
    void TestMoveRight() => MoveRight();

    [ContextMenu("테스트: 왼쪽으로 (Left)")]
    void TestMoveLeft() => MoveLeft();

    [ContextMenu("테스트: 위로 (Up)")]
    void TestMoveUp() => MoveUp();

    [ContextMenu("테스트: 아래로 (Down)")]
    void TestMoveDown() => MoveDown();

    [ContextMenu("테스트: 현재 면 장착 시도")]
    void TestEquip() => EquipCurrentFace();
}