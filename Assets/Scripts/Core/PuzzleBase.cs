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

    // 낮/밤 데이터를 쓰는 퍼즐은 이 두 값을 데이터에서 가져오도록 override 함
    // (override 안 하면 기존처럼 Inspector 값 사용 -> 호루스 퍼즐 등 기존 퍼즐은 그대로 동작)
    protected virtual string RequiredFlag => requiredFlag;
    protected virtual string ClearFlag => clearFlag;

    // 진입 조건을 만족하는지 (자식 퍼즐이 Open 안에서 확인용)
    public bool CanEnter()
    {
        string need = RequiredFlag;
        return string.IsNullOrEmpty(need) || IsFlagOn(need);
    }

    // 퍼즐이 열릴 때 (방에 진입하거나, 상호작용 시작할 때) 호출
    public virtual void Open()
    {
        if (!CanEnter())
        {
            Debug.Log($"[{name}] 진입 조건 미충족: {RequiredFlag} 필요");
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

        string flag = ClearFlag;
        if (!string.IsNullOrEmpty(flag))
        {
            if (GameManager.Instance != null) GameManager.Instance.SetFlag(flag);
            else Debug.LogWarning($"[{name}] GameManager가 없어서 플래그 {flag}를 못 켬");
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
    // [Reset 가드] Unity는 에디터에서 컴포넌트를 붙이거나 우클릭 -> Reset 할 때
    // 이름이 Reset인 함수를 자동으로 부름 -> Play 중이 아니면 무시
    // override하는 자식 퍼즐도 첫 줄에 같은 가드를 꼭 넣어야 함 (Unity는 자식 함수를 바로 부르기 때문)
    public virtual void Reset()
    {
        if (!Application.isPlaying) return;
        Debug.Log($"[{name}] 리셋됨");
    }

    // 낮 -> 밤처럼 같은 퍼즐을 처음부터 다시 쓸 때 클리어 상태를 되돌림
    protected void ResetClearedState()
    {
        IsCleared = false;
    }

    protected static bool IsFlagOn(string flag)
    {
        return GameManager.Instance != null && GameManager.Instance.HasFlag(flag);
    }
}
/*Submit(), Validate()는 abstract(추상 함수)로 만들어서, 각 퍼즐이
 자기만의 방식으로 반드시 구현하게 강제성을 줌.*/