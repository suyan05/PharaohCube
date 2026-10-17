using UnityEngine;

// 라 방 힌트 버튼(PuzzleHintButton)에 아누비스 퍼즐 힌트를 넘겨주는 컴포넌트
// - 퍼즐 패널(Panel_Sand 등) 안의 HintButton 오브젝트에 PuzzleHintButton과 같이 붙임
// - 퍼즐을 이미 풀었으면(보상 아이템 보유 or 플래그 켜짐) 버튼이 사라짐
// - 문구는 AnubisHintData(ScriptableObject)에서 읽음 -> 코드 수정 없이 문구 교체
public class AnubisPuzzleHints : MonoBehaviour, IPuzzleHintProvider
{
    [Header("힌트 문구")]
    [SerializeField] private AnubisHintData data;

    [Header("푼 것으로 보는 조건 (둘 중 하나라도 맞으면 버튼 숨김)")]
    [Tooltip("이 퍼즐 보상 아이템 (예: ITEM_ANUBIS_GOLD_JAR)")]
    [SerializeField] private string doneItemId = "";
    [Tooltip("이 플래그가 켜지면 푼 것 (메인 저울: F_ANUBIS_MAIN_DONE)")]
    [SerializeField] private string doneFlag = "";

    [Tooltip("힌트 문구 앞에 '힌트 1/3' 붙이기")]
    [SerializeField] private bool showCounter = true;

    private int given; // 지금까지 준 힌트 수

    public bool HintAvailable => data != null && data.Count > 0 && !IsDone();

    public string RequestHint()
    {
        if (!HintAvailable) return null;

        int index = Mathf.Min(given, data.Count - 1);
        given = Mathf.Min(given + 1, data.Count);
        string hint = data.Get(index);
        Debug.Log($"[{name}] 힌트 {index + 1}/{data.Count}: {hint}", this);
        return showCounter ? $"힌트 {index + 1}/{data.Count}  {hint}" : hint;
    }

    private bool IsDone()
    {
        if (!string.IsNullOrEmpty(doneItemId) && ItemInventory.Instance != null && ItemInventory.Instance.Has(doneItemId))
            return true;
        return !string.IsNullOrEmpty(doneFlag) && GameManager.Instance != null && GameManager.Instance.HasFlag(doneFlag);
    }

    [ContextMenu("테스트: 힌트 처음부터")]
    private void ResetHints() => given = 0;
}
