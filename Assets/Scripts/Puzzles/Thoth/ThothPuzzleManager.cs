using System;
using System.Collections.Generic;
using UnityEngine;

public class ThothPuzzleManager : MonoBehaviour
{
    // 정답 주문 순서: [눈] -> [물] -> [새] -> [뱀]
    private readonly string[] correctSequence = { "눈", "물", "새", "뱀" };
    private readonly List<string> currentInput = new List<string>();

    [Header("서브 퍼즐")]
    public bool IsPapyrusRestored { get; private set; } = false;

    public event Action<List<string>> OnInputChanged;
    public event Action OnPuzzleCleared;
    public event Action OnInputFailed;

    public int MaxSlots => correctSequence.Length;
    public bool IsCleared { get; private set; } = false;

    public void InputGlyph(string glyphName)
    {
        if (IsCleared || currentInput.Count >= MaxSlots) return;

        currentInput.Add(glyphName);
        OnInputChanged?.Invoke(new List<string>(currentInput));

        if (currentInput.Count == MaxSlots)
        {
            ValidateSequence();
        }
    }

    private void ValidateSequence()
    {
        bool isSuccess = true;
        for (int i = 0; i < MaxSlots; i++)
        {
            if (currentInput[i] != correctSequence[i])
            {
                isSuccess = false;
                break;
            }
        }

        if (isSuccess)
        {
            IsCleared = true;
            Debug.Log("<color=cyan>[ThothPuzzle] 상형문자 주문 영창 완료! 토트의 방 클리어!</color>");
            OnPuzzleCleared?.Invoke();
        }
        else
        {
            Debug.LogWarning("[ThothPuzzle] 잘못된 주문 순서입니다! 입력을 초기화합니다.");
            OnInputFailed?.Invoke();
            ResetInput();
        }
    }

    public void ResetInput()
    {
        if (IsCleared) return;
        currentInput.Clear();
        OnInputChanged?.Invoke(new List<string>(currentInput));
    }

    public void RestorePapyrus()
    {
        IsPapyrusRestored = true;
        Debug.Log("<color=yellow>[서브 클리어] 파피루스 조각을 맞춰 올바른 주문 순서를 알아냈습니다!</color>");
    }
}