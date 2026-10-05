using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// H1 매의 반쪽 날개: 왼쪽 날개를 보고 오른쪽 날개를 대칭으로 그리는 퍼즐 (기획서 6.1)
public class MirrorWingPuzzle : PuzzleBase
{
    [Header("연결")]
    [SerializeField] private StarLineDrawer drawer;
    [SerializeField] private StarBoardView board;
    [SerializeField] private StarGuideView guide;

    [Header("진입: 매의 깃털 자동 사용 후 켤 플래그 (기획서 4.1)")]
    [SerializeField] private string unlockedFlag = "F_HO_H1_UNLOCKED";

    [Header("보상")]
    [SerializeField] private string rewardItem = HorusItemIds.StarFrag1;

    [Header("힌트 (기획서 6.1 튜닝)")]
    [SerializeField] private int failsForHint = 3;
    [SerializeField] private float hintDuration = 5f;

    [Header("색")]
    [SerializeField] private Color fixedColor = new Color(0.91f, 0.70f, 0.23f);
    [SerializeField] private Color playerColor = new Color(0.96f, 0.95f, 0.91f);
    [SerializeField] private Color wrongColor = new Color(0.75f, 0.22f, 0.17f);
    [SerializeField] private Color axisColor = new Color(1f, 1f, 1f, 0.9f);

    [Header("선 데이터 (남서쪽 = 1,1 / 대칭축 x=3)")]
    [SerializeField]
    private List<string> fixedEdges = new List<string>
    { "3,4-3,3", "3,3-3,2", "3,2-3,1", "3,4-2,3", "2,3-1,4", "2,3-1,2", "1,2-2,1", "2,1-3,1" };
    [SerializeField]
    private List<string> answerEdges = new List<string>
    { "3,4-4,3", "4,3-5,4", "4,3-5,2", "5,2-4,1", "4,1-3,1" };
    [SerializeField]
    private List<string> axisEdges = new List<string>
    { "3,1-3,2", "3,2-3,3", "3,3-3,4" };

    public event Action<string> OnMessage;
    public event Action<string> OnTitleChanged;

    private const string FixedLayer = "fixed";
    private const string AxisLayer = "axis";

    private HashSet<string> answer = new HashSet<string>();
    private HashSet<string> pendingWrong = new HashSet<string>();
    private int failCount;
    private bool busy;
    private bool justUnlocked;
    private bool subscribed;

    private void Awake()
    {
        answer = StarGraph.BuildEdgeSet(answerEdges);
    }

    private void OnEnable()
    {
        Subscribe(true);
    }

    private void OnDisable()
    {
        Subscribe(false);
    }

    private void Subscribe(bool on)
    {
        if (drawer == null || board == null || guide == null)
        {
            if (on) Debug.LogError("[날개] Drawer / Board / Guide 연결 필요");
            return;
        }
        if (on == subscribed) return;
        subscribed = on;
        if (on)
        {
            drawer.OnRejected += Message;
            board.OnBuilt += ApplyVisuals;
            return;
        }
        drawer.OnRejected -= Message;
        board.OnBuilt -= ApplyVisuals;
    }

    // ================= PuzzleBase =================
    public override void Open()
    {
        try
        {
            base.Open();
            OnTitleChanged?.Invoke("매의 반쪽 날개");
            TryUnlock();
            if (board.IsBuilt) ApplyVisuals();
        }
        catch (Exception e)
        {
            Debug.LogError($"[날개] Open 오류: {e}");
        }
    }

    // 확정 버튼
    public override void Submit()
    {
        try
        {
            if (IsCleared || busy || !IsUnlocked()) return;
            if (drawer.DrawnEdges.Count == 0)
            {
                Message("아직 아무 선도 긋지 않았다.");
                return;
            }
            if (Validate())
            {
                CompleteWing();
                return;
            }
            pendingWrong = WrongEdges();
            failCount++;
            Message(pendingWrong.Count > 0
                ? "거울에 비친 모양과 다른 선이 있다 - 붉은 선을 지운다."
                : "아직 비어 있는 깃이 있다. 왼쪽 날개를 다시 보자.");
            OnFailInternal();
        }
        catch (Exception e)
        {
            Debug.LogError($"[날개] Submit 오류: {e}");
        }
    }

    protected override bool Validate()
    {
        return answer.SetEquals(drawer.DrawnEdges);
    }

    // 실패 시: 틀린 선만 빨갛게 했다가 지움, 맞은 선은 유지
    // Unity 에디터가 자동으로 부르는 Reset과 이름이 같아서, 게임 중이 아닐 땐 아무것도 안 함
    public override void Reset()
    {
        if (!Application.isPlaying || drawer == null) return;
        base.Reset();
        if (isActiveAndEnabled) StartCoroutine(RemoveWrongRoutine());
    }

    // ================= 진행 =================
    private bool TryUnlock()
    {
        if (IsUnlocked()) return false;
        ItemInventory inventory = ItemInventory.Instance;
        if (inventory == null || !inventory.Consume(HorusItemIds.Feather)) return false;
        GameManager.Instance.SetFlag(unlockedFlag);
        justUnlocked = true;
        return true;
    }

    private void ApplyVisuals()
    {
        try
        {
            if (!board.IsBuilt) return;
            if (!IsUnlocked())
            {
                guide.HideAll();
                drawer.SetInputEnabled(false);
                Message("먼지가 두껍게 쌓여 있다. 무언가로 털어내야 할 것 같다.");
                return;
            }
            guide.ShowLayer(FixedLayer, StarGraph.BuildEdgeSet(fixedEdges), fixedColor, 8f);
            drawer.SetLineColor(playerColor);
            drawer.SetInputEnabled(!IsCleared);
            Message(VisualMessage());
            justUnlocked = false;
        }
        catch (Exception e)
        {
            Debug.LogError($"[날개] 화면 갱신 오류: {e}");
        }
    }

    private string VisualMessage()
    {
        if (IsCleared) return "완성된 매의 날개가 빛나고 있다.";
        if (justUnlocked) return "매의 깃털로 먼지를 털어냈다! 반쪽 날개가 드러난다.";
        return "가운데 몸통선을 기준으로, 오른쪽 날개를 거울처럼 그려 보자. 다 그리면 확정.";
    }

    private HashSet<string> WrongEdges()
    {
        var wrong = new HashSet<string>(drawer.DrawnEdges);
        wrong.ExceptWith(answer);
        return wrong;
    }

    private IEnumerator RemoveWrongRoutine()
    {
        busy = true;
        drawer.SetInputEnabled(false);
        drawer.TintEdges(pendingWrong, wrongColor);
        yield return new WaitForSeconds(0.6f);
        drawer.RemoveEdges(pendingWrong);
        pendingWrong.Clear();
        drawer.SetInputEnabled(true);
        busy = false;
        if (failCount > 0 && failCount % failsForHint == 0) StartCoroutine(AxisHintRoutine());
    }

    private IEnumerator AxisHintRoutine()
    {
        guide.ShowLayer(AxisLayer, StarGraph.BuildEdgeSet(axisEdges), axisColor, 14f);
        Message("대칭축이 빛난다 - 이 선을 기준으로 왼쪽 날개를 뒤집어 보자.");
        yield return new WaitForSeconds(hintDuration);
        guide.HideLayer(AxisLayer);
    }

    private void CompleteWing()
    {
        drawer.SetInputEnabled(false);
        guide.HideLayer(AxisLayer);
        OnSuccessInternal();
        if (ItemInventory.Instance != null && !string.IsNullOrEmpty(rewardItem))
        {
            ItemInventory.Instance.Grant(rewardItem);
        }
        Debug.Log("[노트 2] 별은 한 번 걸은 길을 다시 걷지 않는다.");
        Debug.Log("[노트 3] 북쪽의 황소 다리는 지지 않는다 - 모든 별이 지나간 뒤에도 마지막까지 하늘에 남는다.");
        Message("반쪽 날개가 완성됐다! 별 조각 1을 얻었다.");
    }

    // ================= 외부 호출 =================
    public void Undo()
    {
        if (busy || IsCleared) return;
        drawer.UndoLastStroke();
    }

    private bool IsUnlocked()
    {
        return GameManager.Instance != null && GameManager.Instance.HasFlag(unlockedFlag);
    }

    private void Message(string text)
    {
        Debug.Log($"[날개] {text}");
        OnMessage?.Invoke(text);
    }

    // ================= 개발용 치트 (Inspector 우클릭) =================
    [ContextMenu("치트: 매의 깃털 지급")]
    private void CheatFeather()
    {
        if (ItemInventory.Instance != null) ItemInventory.Instance.Grant(HorusItemIds.Feather);
    }
}