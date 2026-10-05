using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 임시: H2 1단계(메스케티우) 판정 + 튜토리얼 안내 확인. PuzzleBase 연결 후 정식 퍼즐로 옮길 예정
public class StarBoardTester : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private StarLineDrawer drawer;
    [SerializeField] private StarBoardView board;
    [SerializeField] private StarGuideView guide;
    [SerializeField] private Text instructionText;

    [Header("규칙")]
    [SerializeField] private int maxStrokes = 1;
    [SerializeField] private bool tutorialMode = true;
    [SerializeField]
    private List<string> answerEdges = new List<string>
    { "1,5-1,4", "1,4-2,4", "2,4-2,5", "2,5-1,5", "2,5-3,4", "3,4-4,4", "4,4-5,3" };

    private const string GuideMessage = "빛나는 별에서 시작해서, 흐린 선을 손 떼지 말고 한 번에 따라 그려 보세요.";
    private const string RetryMessage = "한 번에 이어지지 않았어요. 빛나는 별 중 하나에서 다시 시작해 보세요.";

    private HashSet<string> answer;
    private bool cleared;

    private void Awake()
    {
        answer = StarGraph.BuildEdgeSet(answerEdges);
    }

    private void OnEnable()
    {
        if (drawer != null)
        {
            drawer.OnStrokeEnded += HandleStrokeEnded;
            drawer.OnRejected += SetText;
        }
        if (board != null) board.OnBuilt += ShowTutorial;
    }

    private void OnDisable()
    {
        if (drawer != null)
        {
            drawer.OnStrokeEnded -= HandleStrokeEnded;
            drawer.OnRejected -= SetText;
        }
        if (board != null) board.OnBuilt -= ShowTutorial;
    }

    private void Start()
    {
        if (board != null && board.IsBuilt) ShowTutorial();
    }

    private void ShowTutorial()
    {
        if (!tutorialMode || guide == null) return;
        guide.ShowGuide(answer);
        guide.ShowStartMarkers(StarGraph.OddDegreeStars(answer));
        SetText(GuideMessage);
    }

    private void HandleStrokeEnded(List<string> stroke)
    {
        try
        {
            if (cleared) return;
            Debug.Log($"[성도] 이번 획: {string.Join(" / ", stroke)}");
            if (answer.SetEquals(drawer.DrawnEdges))
            {
                OnCleared();
                return;
            }
            if (drawer.StrokeCount >= maxStrokes) StartCoroutine(FailAndReset());
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[StarBoardTester] 판정 오류: {e}");
        }
    }

    private void OnCleared()
    {
        cleared = true;
        drawer.SetInputEnabled(false);
        if (guide != null) guide.HideAll();
        SetText("메스케티우 완성!");
        Debug.Log("[성도] 메스케티우 완성! 1단계 클리어");
    }

    private IEnumerator FailAndReset()
    {
        drawer.SetInputEnabled(false);
        LogDiff();
        SetText(RetryMessage);
        yield return new WaitForSeconds(0.3f);
        drawer.ClearAll();
        drawer.SetInputEnabled(true);
    }

    private void SetText(string message)
    {
        if (instructionText != null) instructionText.text = message;
        Debug.Log($"[안내] {message}");
    }

    // 정답 대비 빠진 선 / 더 그은 선
    private void LogDiff()
    {
        var missing = new List<string>();
        var extra = new List<string>();
        var drawn = new HashSet<string>(drawer.DrawnEdges);
        foreach (string e in answer) if (!drawn.Contains(e)) missing.Add(e);
        foreach (string e in drawn) if (!answer.Contains(e)) extra.Add(e);
        Debug.Log($"[검증] 빠진 선: {(missing.Count == 0 ? "없음" : string.Join(" / ", missing))}");
        Debug.Log($"[검증] 더 그은 선: {(extra.Count == 0 ? "없음" : string.Join(" / ", extra))}");
    }

    [ContextMenu("힌트: 시작 가능한 별")]
    private void LogStartStars()
    {
        foreach (Vector2Int s in StarGraph.OddDegreeStars(answer)) Debug.Log($"[힌트] 시작 별 {s}");
    }

    [ContextMenu("다시 하기")]
    private void ResetBoard()
    {
        cleared = false;
        drawer.ClearAll();
        drawer.SetInputEnabled(true);
        ShowTutorial();
    }
}