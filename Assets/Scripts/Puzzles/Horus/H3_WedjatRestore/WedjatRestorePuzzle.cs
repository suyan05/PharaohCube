using System;
using System.Collections.Generic;
using UnityEngine;

// 우자트 조각 하나의 데이터 (위치/크기는 오버레이 중심 기준, 캔버스 1920x1080 단위)
[Serializable]
public class WedjatPieceDef
{
    public string fraction = "";
    public string partName = "";
    public Vector2 slotPos;
    public Vector2 size = new Vector2(100f, 100f);
    public int startRotation;
    public Vector2 trayPos;
}

// H3 우자트 복원: 흩어진 눈 조각 6개를 모양이 같은 자리에 맞추는 퍼즐 (기획서 6.2)
public class WedjatRestorePuzzle : PuzzleBase
{
    [Header("진입: 은빛 실 자동 사용 후 켤 플래그 (기획서 4.1)")]
    [SerializeField] private string unlockedFlag = "F_HO_H3_UNLOCKED";

    [Header("보상")]
    [SerializeField] private string rewardFragment = HorusItemIds.StarFrag2;
    [SerializeField] private string rewardMoonEye = HorusItemIds.MoonEye;

    [Header("힌트 (기획서 6.2: 30초 경과 시 틀린 조각 붉은 윤곽)")]
    [SerializeField] private float hintDelay = 30f;

    [Header("조각 데이터 (조각 i의 정답 자리 = 슬롯 i)")]
    [SerializeField] private List<WedjatPieceDef> pieces = DefaultPieces();

    public event Action OnStateChanged;
    public event Action<string> OnMessage;
    public event Action<string> OnTitleChanged;

    public IReadOnlyList<WedjatPieceDef> Pieces => pieces;
    public bool HintActive { get; private set; }
    public string LastMessage { get; private set; } = "";
    public string LastTitle { get; private set; } = "";
    public bool IsUnlocked => GameManager.Instance != null && GameManager.Instance.HasFlag(unlockedFlag);

    private int[] slotOf;
    private int[] rotation;
    private float hintTimer;
    private bool timerRunning;
    private bool justUnlocked;

    private void Awake()
    {
        slotOf = new int[pieces.Count];
        rotation = new int[pieces.Count];
        for (int i = 0; i < pieces.Count; i++)
        {
            slotOf[i] = -1;
            rotation[i] = ((pieces[i].startRotation % 4) + 4) % 4;
        }
    }

    // 오버레이가 열려 있는 동안만 힌트 시간이 흐름
    private void Update()
    {
        if (!timerRunning || HintActive || IsCleared || !IsUnlocked) return;
        hintTimer += Time.deltaTime;
        if (hintTimer < hintDelay) return;
        HintActive = true;
        Message("어긋난 조각의 테두리가 붉게 빛난다...");
        OnStateChanged?.Invoke();
    }

    // ================= PuzzleBase =================
    public override void Open()
    {
        try
        {
            base.Open();
            LastTitle = "우자트 복원";
            OnTitleChanged?.Invoke(LastTitle);
            TryUnlock();
            timerRunning = true;
            Message(StatusMessage());
            OnStateChanged?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"[우자트] Open 오류: {e}");
        }
    }

    public void Close()
    {
        timerRunning = false;
    }

    // 조각이 움직일 때마다 자동 호출. 오답 개념 없음 (시행 자유)
    public override void Submit()
    {
        try
        {
            if (IsCleared || !Validate()) return;
            Complete();
        }
        catch (Exception e)
        {
            Debug.LogError($"[우자트] Submit 오류: {e}");
        }
    }

    protected override bool Validate()
    {
        for (int i = 0; i < pieces.Count; i++) if (!IsCorrect(i)) return false;
        return true;
    }

    // ================= 조작 =================
    // slot = -1이면 자리에서 빼기. 다른 조각이 있던 자리면 그 조각은 밀려남
    public bool TryPlace(int piece, int slot)
    {
        if (!CanEdit(piece) || slot >= pieces.Count) return false;
        if (slot >= 0)
        {
            int occupant = PieceInSlot(slot);
            if (occupant >= 0 && occupant != piece)
            {
                if (IsCorrect(occupant)) return false;
                slotOf[occupant] = -1;
            }
        }
        slotOf[piece] = slot;
        AfterChange();
        return true;
    }

    public void Rotate(int piece)
    {
        if (!CanEdit(piece)) return;
        rotation[piece] = (rotation[piece] + 1) % 4;
        AfterChange();
    }

    private void AfterChange()
    {
        OnStateChanged?.Invoke();
        Submit();
    }

    // ================= 상태 조회 =================
    public bool CanEdit(int piece)
    {
        return IsValidPiece(piece) && IsUnlocked && !IsCleared && !IsCorrect(piece);
    }

    public bool IsCorrect(int piece)
    {
        return IsValidPiece(piece) && slotOf[piece] == piece && rotation[piece] == 0;
    }

    public int SlotOf(int piece) => IsValidPiece(piece) ? slotOf[piece] : -1;

    public int RotationOf(int piece) => IsValidPiece(piece) ? rotation[piece] : 0;

    public int PieceInSlot(int slot)
    {
        for (int i = 0; i < slotOf.Length; i++) if (slotOf[i] == slot) return i;
        return -1;
    }

    private bool IsValidPiece(int piece) => slotOf != null && piece >= 0 && piece < slotOf.Length;

    // ================= 진행 =================
    private void TryUnlock()
    {
        if (IsUnlocked) return;
        ItemInventory inventory = ItemInventory.Instance;
        if (inventory == null || !inventory.Consume(HorusItemIds.SilverThread)) return;
        GameManager.Instance.SetFlag(unlockedFlag);
        justUnlocked = true;
    }

    private string StatusMessage()
    {
        if (IsCleared) return "복원된 우자트가 은은하게 빛난다.";
        if (!IsUnlocked) return "눈 조각들이 흩어져 있다. 어디에 맞춰야 할지 윤곽이 보이지 않는다.";
        if (justUnlocked)
        {
            justUnlocked = false;
            return "은빛 실이 조각 사이를 잇자 눈의 윤곽이 드러났다!";
        }
        if (HintActive) return "어긋난 조각의 테두리가 붉게 빛난다...";
        return "조각을 모양이 같은 자리로 끌어다 놓자. 클릭하면 90도 회전한다.";
    }

    private void Complete()
    {
        timerRunning = false;
        OnSuccessInternal();
        GrantItem(rewardFragment);
        GrantItem(rewardMoonEye);
        Debug.Log("[노트 4] 아버지의 별이 먼저 떠오르고, 어머니의 별이 그 뒤를 따른다. 붉은 별은 형제를 죽인 자의 것.");
        Message("우자트가 복원됐다! 별 조각 2와 달의 눈을 얻었다.");
        OnStateChanged?.Invoke();
    }

    private void GrantItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId) || ItemInventory.Instance == null) return;
        ItemInventory.Instance.Grant(itemId);
    }

    private void Message(string text)
    {
        LastMessage = text;
        Debug.Log($"[우자트] {text}");
        OnMessage?.Invoke(text);
    }

    // ================= 개발용 치트 (Inspector 우클릭) =================
    [ContextMenu("치트: 은빛 실 지급")]
    private void CheatThread()
    {
        if (ItemInventory.Instance != null) ItemInventory.Instance.Grant(HorusItemIds.SilverThread);
    }

    // ================= 기본 데이터 (기획서 6.2 정답표) =================
    private static List<WedjatPieceDef> DefaultPieces()
    {
        return new List<WedjatPieceDef>
        {
            MakePiece("1/2", "눈 앞쪽(코 쪽) 흰자", new Vector2(140, 40), new Vector2(150, 90), 0, new Vector2(-420, -240)),
            MakePiece("1/4", "눈동자", new Vector2(0, 40), new Vector2(100, 100), 0, new Vector2(-290, -240)),
            MakePiece("1/8", "눈썹", new Vector2(0, 160), new Vector2(320, 40), 2, new Vector2(-60, -240)),
            MakePiece("1/16", "눈 뒤쪽(귀 쪽) 흰자", new Vector2(-130, 40), new Vector2(110, 90), 0, new Vector2(170, -240)),
            MakePiece("1/32", "나선 꼬리", new Vector2(120, -80), new Vector2(130, 60), 3, new Vector2(320, -240)),
            MakePiece("1/64", "눈물선", new Vector2(-30, -95), new Vector2(40, 120), 0, new Vector2(450, -240))
        };
    }

    private static WedjatPieceDef MakePiece(string fraction, string partName, Vector2 slotPos,
        Vector2 size, int startRotation, Vector2 trayPos)
    {
        return new WedjatPieceDef
        {
            fraction = fraction,
            partName = partName,
            slotPos = slotPos,
            size = size,
            startRotation = startRotation,
            trayPos = trayPos
        };
    }
}