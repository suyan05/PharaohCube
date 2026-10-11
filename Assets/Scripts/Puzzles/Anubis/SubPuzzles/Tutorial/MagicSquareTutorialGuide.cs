using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 마방진 튜토리얼 가이드 (튜토리얼 씬의 Panel_MagicSquare에 함께 부착)
// 1) 깜빡이는 빈칸을 클릭하라고 안내
// 2) 클릭하면 오른쪽에서 누를 숫자를 깜빡이며 안내 (왜 그 숫자인지 계산식 표시)
// 3) 모두 채우면 '봉인 판정' 버튼을 깜빡이며 안내

[RequireComponent(typeof(MaatMagicSquarePuzzle))]
public class MagicSquareTutorialGuide : MonoBehaviour
{
    [Serializable]
    public class GuideStep
    {
        [Range(0, 8)] public int slot;          // 채울 칸 (0 1 2 / 3 4 5 / 6 7 8)
        [Range(1, 9)] public int number;        // 넣을 숫자
        [TextArea(1, 2)] public string reason;  // 계산 설명

        public GuideStep() { }
        public GuideStep(int slot, int number, string reason)
        {
            this.slot = slot;
            this.number = number;
            this.reason = reason;
        }
    }

    [Header("튜토리얼 보드 (0 = 빈칸)")]
    [Tooltip("정답: 2 7 6 / 9 5 1 / 4 3 8")]
    [SerializeField] private int[] tutorialPreset = { 2, 0, 6, 0, 5, 0, 4, 0, 8 };

    [Header("안내 순서")]
    [SerializeField]
    private GuideStep[] steps =
    {
        new GuideStep(1, 7, "맨 윗줄:  2 + ? + 6 = 15  →  ? = 7"),
        new GuideStep(3, 9, "왼쪽 세로줄:  2 + ? + 4 = 15  →  ? = 9"),
        new GuideStep(5, 1, "오른쪽 세로줄:  6 + ? + 8 = 15  →  ? = 1"),
        new GuideStep(7, 3, "맨 아랫줄:  4 + ? + 8 = 15  →  ? = 3"),
    };

    [Header("깜빡임")]
    [SerializeField] private Color blinkColor = new Color(1f, 0.85f, 0.3f, 1f);
    [Range(0.5f, 6f)]
    [SerializeField] private float blinkSpeed = 2.5f;

    private const string Intro = "<b>[튜토리얼] 마방진</b>  가로·세로·대각선의 합이 모두 15가 되도록 채웁니다.";

    private MaatMagicSquarePuzzle puzzle;
    private Image blink;
    private int stepIndex;
    private bool solved;

    private bool AllStepsDone => stepIndex >= steps.Length;

    private void Awake()
    {
        puzzle = GetComponent<MaatMagicSquarePuzzle>();
        puzzle.SetPreset(tutorialPreset);
        CreateBlink();
    }

    private void OnEnable()
    {
        puzzle.SlotFilter = AllowSlot;
        puzzle.InputFilter = AllowInput;
        puzzle.OnSlotSelected += HandleSlotSelected;
        puzzle.OnSlotRejected += HandleSlotRejected;
        puzzle.OnNumberPlaced += HandleNumberPlaced;
        puzzle.OnInputRejected += HandleInputRejected;
        puzzle.OnBoardReset += HandleReset;
        puzzle.OnSolved += HandleSolved;
        StartCoroutine(ShowNextFrame());
    }

    private void OnDisable()
    {
        puzzle.SlotFilter = null;
        puzzle.InputFilter = null;
        puzzle.OnSlotSelected -= HandleSlotSelected;
        puzzle.OnSlotRejected -= HandleSlotRejected;
        puzzle.OnNumberPlaced -= HandleNumberPlaced;
        puzzle.OnInputRejected -= HandleInputRejected;
        puzzle.OnBoardReset -= HandleReset;
        puzzle.OnSolved -= HandleSolved;
        HideBlink();
    }

    private void Update()
    {
        if (blink == null || !blink.gameObject.activeSelf) return;
        float a = Mathf.Lerp(0.15f, 0.7f, Mathf.PingPong(Time.unscaledTime * blinkSpeed, 1f));
        blink.canvasRenderer.SetAlpha(a);
    }

    private IEnumerator ShowNextFrame()
    {
        yield return null;
        ShowStep();
    }

    private bool AllowSlot(int slot) => solved || AllStepsDone || slot == steps[stepIndex].slot;
    private bool AllowInput(int slot, int number) =>
        solved || AllStepsDone || (slot == steps[stepIndex].slot && number == steps[stepIndex].number);

    private void ShowStep()
    {
        if (solved || puzzle.IsCleared)
        {
            HideBlink();
            return;
        }

        if (AllStepsDone)
        {
            puzzle.SetHint($"{Intro}\n모든 칸이 채워졌습니다! 깜빡이는 <b>'봉인 판정'</b> 버튼을 누르세요.");
            BlinkOn(puzzle.GetCheckRect());
            return;
        }

        puzzle.SetHint($"{Intro}\n<b>[{stepIndex + 1}/{steps.Length}]</b> 왼쪽에서 <b>깜빡이는 빈칸</b>을 클릭하세요.");
        BlinkOn(puzzle.GetSlotRect(steps[stepIndex].slot));
    }

    private void HandleSlotSelected(int slot)
    {
        if (AllStepsDone) return;
        var s = steps[stepIndex];
        puzzle.SetHint($"{Intro}\n{s.reason}\n오른쪽에서 깜빡이는 <b>{s.number}</b>을(를) 누르세요.");
        BlinkOn(puzzle.GetNumberRect(s.number));
    }

    private void HandleSlotRejected(int slot)
    {
        puzzle.SetHint($"{Intro}\n그 칸이 아니에요! <b>깜빡이는 빈칸</b>을 클릭하세요.");
        BlinkOn(puzzle.GetSlotRect(steps[stepIndex].slot));
    }

    private void HandleInputRejected(int slot, int number)
    {
        var s = steps[stepIndex];
        puzzle.SetHint($"{Intro}\n{s.reason}\n{number}이(가) 아니에요. 깜빡이는 <b>{s.number}</b>을(를) 눌러 주세요.");
    }

    private void HandleNumberPlaced(int slot, int number)
    {
        if (AllStepsDone) return;
        stepIndex++;
        ShowStep();
    }

    private void HandleReset()
    {
        stepIndex = 0;
        solved = false;
        ShowStep();
    }

    private void HandleSolved()
    {
        solved = true;
        HideBlink();
        puzzle.SetHint("<b>[튜토리얼 완료]</b>\n가로·세로·대각선이 모두 15가 되었습니다! 실전에서는 숫자 대신 신들의 상징이 쓰입니다.");
    }

    private void CreateBlink()
    {
        var go = new GameObject("GuideBlink", typeof(RectTransform), typeof(Image));
        go.layer = 5;
        go.transform.SetParent(transform, false);
        blink = go.GetComponent<Image>();
        blink.color = blinkColor;
        blink.raycastTarget = false;
        go.SetActive(false);
    }

    private void BlinkOn(RectTransform target)
    {
        if (target == null)
        {
            HideBlink();
            return;
        }

        var rt = (RectTransform)blink.transform;
        rt.SetParent(target, false);
        rt.SetAsFirstSibling();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(-8, -8);
        rt.offsetMax = new Vector2(8, 8);
        blink.gameObject.SetActive(true);
    }

    private void HideBlink()
    {
        if (blink != null) blink.gameObject.SetActive(false);
    }
}