using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*바스테트의 방 (고양이의 방) — 패턴 기억 퍼즐에 중점을 두어 
 1. 먼저 패턴을 보여주고
 2. 그동안 플레이어는 입력 불가
 3. 패턴을 다 보고 난 후 입력 가능
 4. 틀리면 패던을 다시 보여주는 형식 입니다.*/

public class BastetPuzzleManager : MonoBehaviour
{
    [SerializeField] private List<int> correctSequence = new List<int> { 0, 1, 2 };
    [SerializeField] private float showStepInterval = 0.8f; // 각 칸을 보여주는 간격(초)

    private List<int> playerInput = new List<int>();

    public bool IsInputEnabled { get; private set; }

    public event Action OnPuzzleCleared;
    public event Action OnPuzzleFailed;
    public event Action<int> OnStepCorrect;
    public event Action<int> OnShowStep;       // 패턴 보여줄 때, 몇 번 버튼을 하이라이트할지
    public event Action OnShowPatternStart;    // 패턴 보여주기
    public event Action OnShowPatternEnd;      // 패턴 다 깜. 이제 입력 가능

    private void Start()
    {
        StartPuzzle();
    }

    public void StartPuzzle()
    {
        playerInput.Clear();
        StartCoroutine(ShowPatternRoutine());
    }

    private IEnumerator ShowPatternRoutine()
    {
        IsInputEnabled = false;
        OnShowPatternStart?.Invoke();

        yield return new WaitForSeconds(0.5f); // 시작 전 살짝 대기 (필요없으면 삭제 예정)

        foreach (int step in correctSequence)
        {
            OnShowStep?.Invoke(step);
            yield return new WaitForSeconds(showStepInterval);
        }

        IsInputEnabled = true;
        OnShowPatternEnd?.Invoke();
    }

    public void SubmitInput(int buttonIndex)
    {
        if (!IsInputEnabled)
        {
            Debug.Log("[BastetPuzzle] 아직 패턴을 보여주는 중이라 입력할 수 없습니다.");
            return;
        }

        int currentStep = playerInput.Count;

        if (currentStep >= correctSequence.Count)
        {
            Debug.LogWarning("[BastetPuzzle] 이미 입력이 끝난 상태입니다.");
            return;
        }

        playerInput.Add(buttonIndex);

        if (buttonIndex != correctSequence[currentStep])
        {
            Debug.Log($"[BastetPuzzle] 틀렸습니다! {currentStep}번째 입력 오답: {buttonIndex} (정답: {correctSequence[currentStep]})");
            IsInputEnabled = false;
            OnPuzzleFailed?.Invoke();
            StartCoroutine(RetryAfterDelay());
            return;
        }

        Debug.Log($"[BastetPuzzle] {currentStep}번째 입력 정답: {buttonIndex}");
        OnStepCorrect?.Invoke(currentStep);

        if (playerInput.Count == correctSequence.Count)
        {
            Debug.Log("[BastetPuzzle] 퍼즐 클리어!");
            IsInputEnabled = false;
            OnPuzzleCleared?.Invoke();
        }
    }

    private IEnumerator RetryAfterDelay()
    {
        yield return new WaitForSeconds(1f);
        StartPuzzle(); // 패턴 다시 보여주고 재시도
    }

    [ContextMenu("테스트: 패턴 다시 보여주기")]
    void TestReplay() => StartPuzzle();
}