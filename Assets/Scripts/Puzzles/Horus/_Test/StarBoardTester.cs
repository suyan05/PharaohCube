using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 임시: H2 1단계(메스케티우) 판정만 확인. PuzzleBase 연결 후 삭제 예정
public class StarBoardTester : MonoBehaviour
{
    [SerializeField] private StarLineDrawer drawer;
    [SerializeField] private int maxStrokes = 1;
    [SerializeField]
    private List<string> answerEdges = new List<string>
    { "1,5-1,4", "1,4-2,4", "2,4-2,5", "2,5-1,5", "2,5-3,4", "3,4-4,4", "4,4-5,3" };

    private HashSet<string> answer;
    private bool cleared;

    private void Awake()
    {
        answer = StarGraph.BuildEdgeSet(answerEdges);
    }

    private void OnEnable()
    {
        if (drawer != null) drawer.OnStrokeEnded += HandleStrokeEnded;
    }

    private void OnDisable()
    {
        if (drawer != null) drawer.OnStrokeEnded -= HandleStrokeEnded;
    }

    private void HandleStrokeEnded(List<string> stroke)
    {
        try
        {
            if (cleared) return;
            Debug.Log($"[성도] 이번 획: {string.Join(" / ", stroke)}");
            if (answer.SetEquals(drawer.DrawnEdges))
            {
                cleared = true;
                drawer.SetInputEnabled(false);
                Debug.Log("[성도] 메스케티우 완성! 1단계 클리어");
                return;
            }
            if (drawer.StrokeCount >= maxStrokes) StartCoroutine(FailAndReset());
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[StarBoardTester] 판정 오류: {e}");
        }
    }

    private IEnumerator FailAndReset()
    {
        drawer.SetInputEnabled(false);
        LogDiff();
        Debug.Log("[성도] 획을 모두 썼다 - 시작 별을 바꿔 보자");
        yield return new WaitForSeconds(0.3f);
        drawer.ClearAll();
        drawer.SetInputEnabled(true);
    }

    // 정답 대비 빠진 선 / 더 그은 선
    private void LogDiff()
    {
        var missing = new List<string>();
        var extra = new List<string>();
        foreach (string e in answer) if (!ContainsDrawn(e)) missing.Add(e);
        foreach (string e in drawer.DrawnEdges) if (!answer.Contains(e)) extra.Add(e);
        Debug.Log($"[검증] 빠진 선: {(missing.Count == 0 ? "없음" : string.Join(" / ", missing))}");
        Debug.Log($"[검증] 더 그은 선: {(extra.Count == 0 ? "없음" : string.Join(" / ", extra))}");
    }

    private bool ContainsDrawn(string edge)
    {
        foreach (string d in drawer.DrawnEdges) if (d == edge) return true;
        return false;
    }

    [ContextMenu("힌트: 시작 가능한 별")]
    private void LogStartStars()
    {
        foreach (Vector2Int s in StarGraph.OddDegreeStars(answer)) Debug.Log($"[힌트] 시작 별 {s}");
    }

    [ContextMenu("검증: 최소 획 수")]
    private void LogMinStrokes()
    {
        Debug.Log($"[검증] 정답 선 {answer.Count}개, 최소 {StarGraph.MinStrokes(answer)}획");
    }

    [ContextMenu("다시 하기")]
    private void ResetBoard()
    {
        cleared = false;
        drawer.ClearAll();
        drawer.SetInputEnabled(true);
    }
}