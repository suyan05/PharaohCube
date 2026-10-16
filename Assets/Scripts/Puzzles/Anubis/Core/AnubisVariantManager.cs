using System.Collections.Generic;
using UnityEngine;

// 아누비스 서브 퍼즐 변형 관리
// - 새 게임(씬 시작)마다: 퍼즐마다 변형을 무작위로 고름
// - 메인 저울 실패(AnubisJudgmentPenalty.OnPenaltyApplied)마다: 모든 퍼즐을 다음 변형으로 바꿈
//   -> 같은 답을 외워서 다시 푸는 것을 막음
// - 변형이 바뀌면 그 방 양피지 글(푸는 방법)도 같이 바뀜 (AnubisScrollData 런타임 글)
public class AnubisVariantManager : MonoBehaviour
{
    [Header("변형 선택")]
    [Tooltip("켜면 새 게임마다 무작위 변형, 끄면 항상 0번(기본 문제)으로 시작")]
    [SerializeField] private bool randomOnStart = true;
    [Tooltip("비우면 씬에서 자동으로 찾음")]
    [SerializeField] private AnubisJudgmentPenalty penalty;

    [Header("안내")]
    [TextArea(2, 4)]
    [SerializeField] private string changedMessage = "시험의 문양이 뒤바뀌었습니다.\n양피지의 글귀도 달라졌으니 다시 읽어 보십시오.";
    [Tooltip("처벌 안내가 사라진 뒤 띄우도록 기다리는 시간 (초)")]
    [SerializeField, Range(0f, 8f)] private float messageDelay = 5.2f;

    private readonly List<IAnubisVariant> puzzles = new List<IAnubisVariant>();

    private void Start()
    {
        // 퍼즐 패널은 꺼져 있으니 비활성 포함으로 찾음
        foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mb is IAnubisVariant v) puzzles.Add(v);
        }

        foreach (IAnubisVariant v in puzzles)
        {
            int index = (randomOnStart && v.VariantCount > 1) ? Random.Range(0, v.VariantCount) : 0;
            v.ApplyVariant(index);
            Debug.Log($"[{name}] {v} 시작 변형 = {index}", this);
        }

        if (penalty == null) penalty = FindFirstObjectByType<AnubisJudgmentPenalty>(FindObjectsInactive.Include);
        if (penalty != null) penalty.OnPenaltyApplied += HandlePenalty;
        else Debug.LogWarning($"[{name}] AnubisJudgmentPenalty가 없어 실패 시 변형 교체를 못 함", this);
    }

    private void OnDestroy()
    {
        if (penalty != null) penalty.OnPenaltyApplied -= HandlePenalty;
        ClearAllScrollTexts(); // 에디터에서 다음 Play에 바뀐 글이 남지 않게
    }

    // 실패할 때마다 다음 변형 (마지막이면 0번으로)
    private void HandlePenalty(int failCount)
    {
        foreach (IAnubisVariant v in puzzles)
        {
            if (v.VariantCount <= 1) continue;
            int next = (v.CurrentVariant + 1) % v.VariantCount;
            v.ApplyVariant(next);
            Debug.Log($"[{name}] 실패 {failCount}번째 -> {v} 변형 {next}", this);
        }

        if (!string.IsNullOrEmpty(changedMessage)) Invoke(nameof(ShowChangedMessage), messageDelay);
    }

    private void ShowChangedMessage() => AnubisToast.Show(changedMessage, 4f);

    [ContextMenu("테스트: 모든 퍼즐 다음 변형으로 (Play 중)")]
    private void TestNext()
    {
        if (!Application.isPlaying) return;
        HandlePenalty(0);
    }

    // ================= 양피지 글 교체 도우미 =================
    // 퍼즐이 ApplyVariant 안에서 호출. text가 비면 에셋에 적힌 원래 글로 되돌림
    public static void SetScrollText(string scrollId, string text)
    {
        foreach (var data in Resources.FindObjectsOfTypeAll<AnubisScrollData>())
        {
            if (data.ScrollId == scrollId) data.SetRuntimeText(text);
        }
    }

    private static void ClearAllScrollTexts()
    {
        foreach (var data in Resources.FindObjectsOfTypeAll<AnubisScrollData>())
            data.SetRuntimeText(null);
    }
}
