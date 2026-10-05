using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 제단 별 하나 (각도: 0 = 동쪽, 반시계 방향 / radius 0 = 제단 중앙)
[Serializable]
public class SkyStar
{
    public string label = "";
    public bool isTrap;
    public float angle;
    public float radius = 210f;
}

// H5 호루스의 하늘: 성좌 카드 + 노트로 순서를 추론해 제단 별 5개를 잇는 메인 퍼즐 (기획서 6.4, 9.11)
public class SkyAlignPuzzle : PuzzleBase
{
    [Header("진입 조건: 성도판 성좌 4개 점등")]
    [SerializeField] private string gateFlag = "F_HO_CHART_4";

    [Header("단서 획득 플래그 (노트 4는 H3 클리어 또는 경고 석판 읽기)")]
    [SerializeField] private string note3Flag = "F_HO_H1_DONE";
    [SerializeField] private string note4Flag = "F_HO_H3_DONE";
    [SerializeField] private string note4SlabFlag = "F_HO_NOTE_4";
    [SerializeField] private string note5Flag = "F_HO_H4_DONE";

    [Header("힌트 (기획서 9.7)")]
    [SerializeField] private int failsForTrapHint = 3;
    [SerializeField] private int failsForNextHint = 5;

    [Header("제단 별 (기획서 6.4: 1 메스케티우 ~ 6 매)")]
    [SerializeField] private List<SkyStar> stars = DefaultStars();

    [Header("정답 순서 (stars 번호, 0부터) - 사흐, 소프데트, 우자트, 메스케티우, 매")]
    [SerializeField] private List<int> answer = new List<int> { 1, 2, 3, 0, 5 };

    public event Action OnStateChanged;
    public event Action<string> OnMessage;
    public event Action<string> OnTitleChanged;
    public event Action OnCompleted;

    public IReadOnlyList<SkyStar> Stars => stars;
    public IReadOnlyList<int> Input => input;
    public bool IsAwake => HasFlag(gateFlag);
    public bool WrongFlash { get; private set; }
    public bool TrapHint { get; private set; }
    public int NextHintStar { get; private set; } = -1;
    public string LastMessage { get; private set; } = "";
    public string LastTitle { get; private set; } = "";

    private readonly List<int> input = new List<int>();
    private int failCount;
    private bool busy;

    // ================= PuzzleBase =================
    public override void Open()
    {
        try
        {
            base.Open();
            LastTitle = "호루스의 하늘";
            OnTitleChanged?.Invoke(LastTitle);
            Message(StatusMessage());
            OnStateChanged?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"[하늘] Open 오류: {e}");
        }
    }

    // 5개를 다 눌렀을 때 자동 호출
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
            Message(FailMessage());
            OnFailInternal();
        }
        catch (Exception e)
        {
            Debug.LogError($"[하늘] Submit 오류: {e}");
        }
    }

    protected override bool Validate()
    {
        if (input.Count != answer.Count) return false;
        for (int i = 0; i < answer.Count; i++)
        {
            if (input[i] != answer[i]) return false;
        }
        return true;
    }

    // Unity 에디터가 자동으로 부르는 Reset과 이름이 같아서, 게임 중이 아닐 땐 아무것도 안 함
    public override void Reset()
    {
        if (!Application.isPlaying) return;
        base.Reset();
        if (isActiveAndEnabled) StartCoroutine(WrongRoutine());
        else input.Clear();
    }

    // ================= 입력 =================
    public bool CanInput => IsAwake && !IsCleared && !busy;

    public void SelectStar(int index)
    {
        try
        {
            if (!CanInput || index < 0 || index >= stars.Count || input.Contains(index)) return;
            input.Add(index);
            UpdateNextHint();
            OnStateChanged?.Invoke();
            if (input.Count >= answer.Count) Submit();
            else Message($"{input.Count}/{answer.Count} - {stars[index].label}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[하늘] SelectStar 오류: {e}");
        }
    }

    public void Back()
    {
        if (!CanInput || input.Count == 0) return;
        input.RemoveAt(input.Count - 1);
        UpdateNextHint();
        Message(input.Count == 0 ? StatusMessage() : $"{input.Count}/{answer.Count} - 마지막 입력을 취소했다.");
        OnStateChanged?.Invoke();
    }

    // ================= 진행 =================
    private IEnumerator WrongRoutine()
    {
        busy = true;
        WrongFlash = true;
        OnStateChanged?.Invoke();
        yield return new WaitForSeconds(0.5f);
        input.Clear();
        WrongFlash = false;
        busy = false;
        TrapHint = failCount >= failsForTrapHint;
        UpdateNextHint();
        OnStateChanged?.Invoke();
    }

    private void UpdateNextHint()
    {
        bool show = failCount >= failsForNextHint && input.Count < answer.Count;
        NextHintStar = show ? answer[input.Count] : -1;
    }

    private string StatusMessage()
    {
        if (IsCleared) return "하늘은 다시 호루스의 것이 되었다.";
        if (!IsAwake) return "하늘이 아직 어둡다. 성도판의 성좌 네 개를 모두 밝혀야 한다.";
        return "단서를 엮어, 하늘에 별이 떠오르는 순서대로 다섯 별을 이어 보자.";
    }

    private string FailMessage()
    {
        bool touchedTrap = false;
        foreach (int i in input) if (stars[i].isTrap) touchedTrap = true;
        if (touchedTrap) return "붉은 별에 손이 닿자 하늘이 흔들린다. 이 별은 따르면 안 된다.";
        if (failCount >= failsForNextHint) return "별들이 흩어졌다... 다음에 이을 별이 희미하게 빛난다.";
        if (failCount >= failsForTrapHint) return "별들이 흩어졌다... 붉은 별이 떨고 있다.";
        return "별들이 흩어졌다. 순서가 맞지 않는다. 왼쪽 카드와 오른쪽 단서를 엮어 보자.";
    }

    private void Complete()
    {
        busy = true;
        NextHintStar = -1;
        TrapHint = false;
        OnSuccessInternal();
        Debug.Log("[노트 6] 하늘의 형판을 큐브의 면에 장착하라.");
        Message("하늘이 다시 호루스의 것이 되었다! 호루스의 형판을 얻었다.");
        OnCompleted?.Invoke();
        OnStateChanged?.Invoke();
    }

    // ================= 단서 (화면 표시용) =================
    public List<string> GetCardLines()
    {
        return new List<string>
        {
            CardLine("F_HO_CHART_1", "메스케티우 - 지지 않는 황소 다리"),
            CardLine("F_HO_CHART_2", "사흐 - 오시리스의 별 (아버지)"),
            CardLine("F_HO_CHART_3", "소프데트 - 이시스의 별 (어머니)"),
            CardLine("F_HO_CHART_4", "우자트 - 호루스의 눈 (아들)")
        };
    }

    public List<string> GetNoteLines()
    {
        bool hasNote4 = HasFlag(note4Flag) || HasFlag(note4SlabFlag);
        return new List<string>
        {
            HasFlag(note3Flag)
                ? "노트 3: 북쪽의 황소 다리는 지지 않는다 - 모든 별이 지나간 뒤에도 마지막까지 하늘에 남는다."
                : "노트 3: ??? (서쪽 관측대)",
            hasNote4
                ? "노트 4: 아버지의 별이 먼저 떠오르고, 어머니의 별이 그 뒤를 따른다. 붉은 별은 형제를 죽인 자의 것."
                : "노트 4: ??? (우자트 석탁 / 경고 석판)",
            HasFlag(note5Flag)
                ? "노트 5: 아들의 눈은 어머니의 별 뒤에 뜨고, 매는 모든 별이 모인 뒤 하늘 한가운데 앉는다."
                : "노트 5: ??? (바닥 별판)"
        };
    }

    private string CardLine(string flag, string text)
    {
        return HasFlag(flag) ? text : "??? (아직 밝히지 못한 성좌)";
    }

    private bool HasFlag(string flag)
    {
        return GameManager.Instance != null && !string.IsNullOrEmpty(flag) && GameManager.Instance.HasFlag(flag);
    }

    private void Message(string text)
    {
        LastMessage = text;
        Debug.Log($"[하늘] {text}");
        OnMessage?.Invoke(text);
    }

    // ================= 개발용 치트 (Inspector 우클릭) =================
    [ContextMenu("치트: 성좌 4개 점등 (F_HO_CHART_1~4)")]
    private void CheatCharts()
    {
        for (int i = 1; i <= 4; i++) GameManager.Instance.SetFlag($"F_HO_CHART_{i}");
        OnStateChanged?.Invoke();
    }

    [ContextMenu("치트: 노트 3, 4, 5 획득")]
    private void CheatNotes()
    {
        GameManager.Instance.SetFlag(note3Flag);
        GameManager.Instance.SetFlag(note4SlabFlag);
        GameManager.Instance.SetFlag(note5Flag);
        OnStateChanged?.Invoke();
    }

    // ================= 기본 데이터 (자리 배치가 정답 순서를 드러내지 않게 섞음) =================
    private static List<SkyStar> DefaultStars()
    {
        return new List<SkyStar>
        {
            new SkyStar { label = "메스케티우", angle = 90f },
            new SkyStar { label = "사흐", angle = -54f },
            new SkyStar { label = "소프데트", angle = 162f },
            new SkyStar { label = "우자트", angle = 18f },
            new SkyStar { label = "붉은 별", angle = -126f, isTrap = true },
            new SkyStar { label = "매", angle = 0f, radius = 0f }
        };
    }
}