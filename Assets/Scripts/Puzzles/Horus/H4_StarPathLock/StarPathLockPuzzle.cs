using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// H4 세트의 봉인: 벽화 '매의 비행' 순서대로 3x3 별 5개를 잇는 순서 잠금 퍼즐 (기획서 6.3)
public class StarPathLockPuzzle : PuzzleBase
{
    [Header("연결")]
    [SerializeField] private StarLineDrawer drawer;
    [SerializeField] private StarBoardView board;

    [Header("진입: 매 인장 자동 사용 후 켤 플래그 (기획서 4.1)")]
    [SerializeField] private string unlockedFlag = "F_HO_H4_UNLOCKED";

    [Header("보상")]
    [SerializeField] private string rewardItem = HorusItemIds.StarFrag3;

    [Header("힌트 (기획서 6.3: 오답 3회 -> 벽화 첫 컷 5초 발광)")]
    [SerializeField] private int failsForHint = 3;
    [SerializeField] private float hintDuration = 5f;

    [Header("색")]
    [SerializeField] private Color lineColor = new Color(0.91f, 0.70f, 0.23f);
    [SerializeField] private Color wrongColor = new Color(0.75f, 0.22f, 0.17f);

    [Header("정답 순서 (3x3, 남서쪽 = 1,1, x3 = 동쪽)")]
    [SerializeField]
    private List<Vector2Int> answerPath = new List<Vector2Int>
    {
        new Vector2Int(3, 2), new Vector2Int(2, 3), new Vector2Int(1, 2),
        new Vector2Int(2, 1), new Vector2Int(2, 2)
    };

    public event Action OnStateChanged;
    public event Action<string> OnMessage;
    public event Action<string> OnTitleChanged;

    public bool IsUnlocked => GameManager.Instance != null && GameManager.Instance.HasFlag(unlockedFlag);
    public int HintCut { get; private set; } = -1;
    public string LastMessage { get; private set; } = "";
    public string LastTitle { get; private set; } = "";

    private List<Vector2Int> lastPath;
    private int failCount;
    private bool busy;
    private bool justUnlocked;
    private bool subscribed;

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
        if (drawer == null || board == null)
        {
            if (on) Debug.LogError("[별길 봉인] Drawer / Board 연결 필요");
            return;
        }
        if (on == subscribed) return;
        subscribed = on;
        if (on)
        {
            drawer.OnStrokeEnded += HandleStroke;
            drawer.OnRejected += Message;
            board.OnBuilt += ApplyVisuals;
            return;
        }
        drawer.OnStrokeEnded -= HandleStroke;
        drawer.OnRejected -= Message;
        board.OnBuilt -= ApplyVisuals;
    }

    // ================= PuzzleBase =================
    public override void Open()
    {
        try
        {
            base.Open();
            LastTitle = "세트의 봉인 - 매의 비행을 따라";
            OnTitleChanged?.Invoke(LastTitle);
            TryUnlock();
            if (board.IsBuilt) ApplyVisuals();
            Message(StatusMessage());
            OnStateChanged?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"[별길 봉인] Open 오류: {e}");
        }
    }

    // 한 획 끝날 때마다 즉시 판정
    public override void Submit()
    {
        try
        {
            if (IsCleared || busy) return;
            if (Validate())
            {
                Complete();
                return;
            }
            failCount++;
            Message("매가 날아간 길과 다르다 - 봉인이 다시 닫힌다.");
            OnFailInternal();
        }
        catch (Exception e)
        {
            Debug.LogError($"[별길 봉인] Submit 오류: {e}");
        }
    }

    protected override bool Validate()
    {
        if (lastPath == null || lastPath.Count != answerPath.Count) return false;
        for (int i = 0; i < answerPath.Count; i++)
        {
            if (lastPath[i] != answerPath[i]) return false;
        }
        return true;
    }

    // 실패 시 선을 빨갛게 했다가 전부 지움
    // Unity 에디터는 컴포넌트를 붙이거나 Inspector에서 Reset을 누를 때도
    // 'Reset'이라는 이름의 함수를 자동으로 부름. 게임 중이 아닐 땐 아무것도 안 함
    public override void Reset()
    {
        if (!Application.isPlaying || drawer == null) return;
        base.Reset();
        if (isActiveAndEnabled) StartCoroutine(FlashAndClear());
        else drawer.ClearAll();
    }

    // ================= 진행 =================
    private void HandleStroke(List<string> edges)
    {
        lastPath = StarGraph.PathFromEdges(edges);
        Submit();
    }

    private void TryUnlock()
    {
        if (IsUnlocked) return;
        ItemInventory inventory = ItemInventory.Instance;
        if (inventory == null || !inventory.Consume(HorusItemIds.SealFalcon)) return;
        GameManager.Instance.SetFlag(unlockedFlag);
        justUnlocked = true;
    }

    private void ApplyVisuals()
    {
        drawer.SetLineColor(lineColor);
        drawer.SetInputEnabled(IsUnlocked && !IsCleared && !busy);
    }

    private string StatusMessage()
    {
        if (IsCleared) return "봉인이 풀린 별판이 고요히 빛난다.";
        if (!IsUnlocked) return "붉은 봉인이 별판을 덮고 있다. 매의 인장이 필요할 것 같다.";
        if (justUnlocked)
        {
            justUnlocked = false;
            return "매의 인장이 닿자 봉인이 풀렸다! 벽화 속 매의 길을 따라 별을 이어 보자.";
        }
        return "벽화 속 매가 날아간 순서대로, 별 다섯 개를 한 번에 이어 보자.";
    }

    private IEnumerator FlashAndClear()
    {
        busy = true;
        drawer.SetInputEnabled(false);
        drawer.TintEdges(new List<string>(drawer.DrawnEdges), wrongColor);
        yield return new WaitForSeconds(0.3f);
        drawer.ClearAll();
        drawer.SetInputEnabled(true);
        busy = false;
        if (failCount > 0 && failCount % failsForHint == 0) StartCoroutine(MuralHint());
    }

    private IEnumerator MuralHint()
    {
        HintCut = 0;
        Message("벽화의 첫 장면이 빛난다 - 매는 어느 쪽에서 떠올랐을까?");
        OnStateChanged?.Invoke();
        yield return new WaitForSeconds(hintDuration);
        HintCut = -1;
        OnStateChanged?.Invoke();
    }

    private void Complete()
    {
        drawer.SetInputEnabled(false);
        OnSuccessInternal();
        if (ItemInventory.Instance != null && !string.IsNullOrEmpty(rewardItem))
        {
            ItemInventory.Instance.Grant(rewardItem);
        }
        Debug.Log("[노트 5] 아들의 눈은 어머니의 별 뒤에 뜨고, 매는 모든 별이 모인 뒤 하늘 한가운데 앉는다.");
        Message("봉인이 풀렸다! 별 조각 3을 얻었다.");
        OnStateChanged?.Invoke();
    }

    private void Message(string text)
    {
        LastMessage = text;
        Debug.Log($"[별길 봉인] {text}");
        OnMessage?.Invoke(text);
    }

    // ================= 개발용 치트 (Inspector 우클릭) =================
    [ContextMenu("치트: 매 인장 지급")]
    private void CheatSeal()
    {
        if (ItemInventory.Instance != null) ItemInventory.Instance.Grant(HorusItemIds.SealFalcon);
    }
}