using UnityEngine;

// 라의 방 퍼즐 데이터 공통 부분 (낮용, 밤용 파일을 퍼즐마다 하나씩 만듦)
// 정답, 규칙 같은 값은 각 퍼즐의 데이터 클래스(EclipsePuzzleData 등)에 있음
public abstract class RaPuzzleData : ScriptableObject
{
    [Header("공통 - 플래그 (비우면 퍼즐 Inspector 값 사용)")]
    [Tooltip("이 플래그가 켜져 있어야 퍼즐을 시작할 수 있음")]
    [SerializeField] private string requiredFlag = "";

    [Tooltip("클리어하면 켤 플래그")]
    [SerializeField] private string clearFlag = "";

    [Header("공통 - 진행")]
    [Tooltip("끄면 렌즈/안료/열쇠 같은 아이템 없이 바로 시작 (밤 버전용)")]
    [SerializeField] private bool useItemGate = true;

    [Tooltip("끄면 클리어 보상 아이템과 노트를 주지 않음 (밤 버전용)")]
    [SerializeField] private bool grantRewards = true;

    public string RequiredFlag => requiredFlag;
    public string ClearFlag => clearFlag;
    public bool UseItemGate => useItemGate;
    public bool GrantRewards => grantRewards;

    // 자식 데이터의 "기획서 값 채우기" 메뉴에서 공통 값을 한 번에 넣을 때 사용
    protected void SetCommon(string required, string clear, bool itemGate, bool rewards)
    {
        requiredFlag = required;
        clearFlag = clear;
        useItemGate = itemGate;
        grantRewards = rewards;
    }

    // 에디터에서 메뉴로 값을 바꾼 뒤 저장되게 표시
    protected void MarkDirty()
    {
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}
