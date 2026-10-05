using System;
using System.Collections.Generic;
using UnityEngine;

public class HorusPuzzleManager : MonoBehaviour
{
    [Header("정답 순서 (별 번호)")]
    [SerializeField] private List<int> correctPath = new List<int> { 0, 1, 2, 3 };

    private List<int> playerPath = new List<int>();

    public bool IsCleared { get; private set; }

    public event Action OnPuzzleCleared;
    public event Action OnConnectionFailed;
    public event Action<int, int> OnConnectionMade; // 성공한 연결(시작별, 도착별) - 선 그리기용
    public event Action OnPuzzleReset;

    private void Start()
    {
        StartPuzzle();
    }

    public void StartPuzzle()
    {
        playerPath.Clear();
        playerPath.Add(correctPath[0]); // 시작점은 항상 정답의 첫 번째 별
        IsCleared = false;
    }

    // 플레이어가 fromIndex 별에서 드래그를 시작해서 toIndex 별에 놓았을 때 호출
    public void TryConnect(int fromIndex, int toIndex)
    {
        if (IsCleared) return;

        int expectedFrom = playerPath[playerPath.Count - 1];

        if (fromIndex != expectedFrom)
        {
            Debug.Log($"[HorusPuzzle] 시작점이 잘못됨. 지금 위치: {expectedFrom}, 시도: {fromIndex}");
            Fail();
            return;
        }

        int nextStepInPath = playerPath.Count;
        if (nextStepInPath >= correctPath.Count)
        {
            Debug.Log("[HorusPuzzle] 이미 연결이 다 끝났습니다.");
            return;
        }

        int expectedTo = correctPath[nextStepInPath];

        if (toIndex != expectedTo)
        {
            Debug.Log($"[HorusPuzzle] 잘못된 연결: {fromIndex} → {toIndex} (정답: {fromIndex} → {expectedTo})");
            Fail();
            return;
        }

        playerPath.Add(toIndex);
        Debug.Log($"[HorusPuzzle] 연결 성공: {fromIndex} → {toIndex}");
        OnConnectionMade?.Invoke(fromIndex, toIndex);

        if (playerPath.Count == correctPath.Count)
        {
            IsCleared = true;
            Debug.Log("[HorusPuzzle] 별자리 완성! 퍼즐 클리어!");
            OnPuzzleCleared?.Invoke();
        }
    }

    private void Fail()
    {
        OnConnectionFailed?.Invoke();
        OnPuzzleReset?.Invoke();
        StartPuzzle();
    }

    [ContextMenu("테스트: 정답 순서로 자동 연결")]
    void TestCorrectPath()
    {
        StartPuzzle();
        for (int i = 0; i < correctPath.Count - 1; i++)
        {
            TryConnect(correctPath[i], correctPath[i + 1]);
        }
    }

    [ContextMenu("테스트: 틀린 순서로 연결")]
    void TestWrongPath()
    {
        StartPuzzle();
        TryConnect(correctPath[0], correctPath[correctPath.Count - 1]); // 순서 무시하고 끝 별로 바로
    }
}