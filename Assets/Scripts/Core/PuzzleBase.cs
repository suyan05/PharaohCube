using System;
using UnityEngine;

public abstract class PuzzleBase : MonoBehaviour
{
    [Header("이 퍼즐의 진입 조건 플래그 (비어있으면 조건 없음)")]
    [SerializeField] private string requiredFlag = "";

    [Header("클리어 시 켤 플래그")]
    [SerializeField] private string clearFlag = "";

    public bool IsCleared { get; private set; }

    public event Action OnSuccess;
    public event Action OnFail;

    // 퍼즐이 열릴 때 (방에 진입하거나, 상호작용 시작할 때) 호출
    public virtual void Open()
    {
        if (!string.IsNullOrEmpty(requiredFlag) && !GameManager.Instance.HasFlag(requiredFlag))
        {
            Debug.Log($"[{name}] 진입 조건 미충족: {requiredFlag} 필요");
            return;
        }

        Debug.Log($"[{name}] 퍼즐 열림");
    }

    // 플레이어의 입력을 제출했을 때 호출 (자식 클래스가 구체적으로 구현)
    public abstract void Submit();

    // 정답인지 판정 (자식 클래스가 구체적으로 구현)
    protected abstract bool Validate();

    protected virtual void OnSuccessInternal()
    {
        IsCleared = true;

        if (!string.IsNullOrEmpty(clearFlag))
        {
            GameManager.Instance.SetFlag(clearFlag);
        }

        Debug.Log($"[{name}] 성공!");
        OnSuccess?.Invoke();
    }

    protected virtual void OnFailInternal()
    {
        Debug.Log($"[{name}] 실패!");
        OnFail?.Invoke();
        Reset();
    }

    // 기본 리셋 동작. 퍼즐마다 다르면 override해서 바꿀 수 있음
    public virtual void Reset()
    {
        Debug.Log($"[{name}] 리셋됨");
    }
}
/*Submit(), Validate()는 abstract(추상 함수)로 만ㄷ러서, 각 퍼즐이
 자기만의 방식으로 반드시 구현하게 강제성을 줌.*/