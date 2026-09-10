using System;
using UnityEngine;

public class CubeFaceState : MonoBehaviour
{
    public bool[] faceActivated = new bool[6];

    // 다른 스크립트(UI, 봉인 매니저 등)가 구독할 이벤트
    public event Action<int> OnFaceActivated;
    public event Action OnAllFacesActivated;

    public void ActivateFace(int faceIndex)
    {
        if (faceIndex < 0 || faceIndex >= faceActivated.Length) return;
        if (faceActivated[faceIndex]) return; // 이미 활성화된 면이면 무시

        faceActivated[faceIndex] = true;
        Debug.Log($"{faceIndex}번 면 활성화됨! 현재 상태: {string.Join(",", faceActivated)}");

        OnFaceActivated?.Invoke(faceIndex);

        if (CheckAllFacesActivated())
        {
            Debug.Log("6개 면 전부 활성화! 최종 봉인 퍼즐 오픈 조건 충족");
            OnAllFacesActivated?.Invoke();
        }
    }

    private bool CheckAllFacesActivated()
    {
        foreach (bool activated in faceActivated)
        {
            if (!activated) return false;
        }
        return true;
    }

    [ContextMenu("테스트: 0번 면 활성화")]
    void TestActivateFace0() => ActivateFace(0);

    [ContextMenu("테스트: 전체 면 활성화")]
    void TestActivateAllFaces()
    {
        for (int i = 0; i < faceActivated.Length; i++)
            ActivateFace(i);
    }
}