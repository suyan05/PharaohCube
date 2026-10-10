using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CanopicJarsPuzzle : MonoBehaviour
{
    [Header("4대 방위 단지 조작 버튼")]
    [SerializeField] private Button btnNorth;
    [SerializeField] private Button btnSouth;
    [SerializeField] private Button btnEast;
    [SerializeField] private Button btnWest;

    [Header("4대 방위 단지 신수 텍스트")]
    [SerializeField] private TMP_Text textNorth;
    [SerializeField] private TMP_Text textSouth;
    [SerializeField] private TMP_Text textEast;
    [SerializeField] private TMP_Text textWest;

    [Header("방위별 정렬 일치 인디케이터 (Image Glow)")]
    [SerializeField] private Image glowNorth;
    [SerializeField] private Image glowSouth;
    [SerializeField] private Image glowEast;
    [SerializeField] private Image glowWest;

    [Header("중앙 성물 개안 이펙트")]
    [SerializeField] private Image ankhCenterGlow;

    [Header("시스템 버튼 및 알림")]
    [SerializeField] private Button btnCheckSolution;
    [SerializeField] private Button btnReset;
    [SerializeField] private Button btnClose;
    [SerializeField] private GameObject successNotice;
    [SerializeField] private TMP_Text textFeedbackNotice;
    [SerializeField] private TMP_Text textHintNotice;

    private readonly string[] guardianNames = {
        "임세티\n<size=15><color=#D4AF37>[인간의 머리]</color></size>",
        "하피\n<size=15><color=#00FFFF>[원숭이 머리]</color></size>",
        "두아무테프\n<size=15><color=#E67E22>[자칼의 머리]</color></size>",
        "케베센우에프\n<size=15><color=#2ECC71>[매의 머리]</color></size>"
    };

    private const int TARGET_NORTH = 1;
    private const int TARGET_SOUTH = 0;
    private const int TARGET_EAST = 2;
    private const int TARGET_WEST = 3;

    private int curNorth = 0;
    private int curSouth = 2;
    private int curEast = 3;
    private int curWest = 1;

    private bool isCleared = false;
    private Coroutine feedbackCoroutine;

    private readonly Color colCorrect = new Color(1f, 0.85f, 0.2f, 1f);
    private readonly Color colIncorrect = new Color(0.3f, 0.25f, 0.2f, 0.5f);

    private void Start()
    {
        if (btnNorth != null) btnNorth.onClick.AddListener(() => CycleGuardian(ref curNorth));
        if (btnSouth != null) btnSouth.onClick.AddListener(() => CycleGuardian(ref curSouth));
        if (btnEast != null) btnEast.onClick.AddListener(() => CycleGuardian(ref curEast));
        if (btnWest != null) btnWest.onClick.AddListener(() => CycleGuardian(ref curWest));

        if (btnCheckSolution != null) btnCheckSolution.onClick.AddListener(CheckSolution);
        if (btnReset != null) btnReset.onClick.AddListener(ResetJars);
        if (btnClose != null) btnClose.onClick.AddListener(() => gameObject.SetActive(false));

        if (successNotice != null) successNotice.SetActive(false);
        if (textFeedbackNotice != null) textFeedbackNotice.text = "";

        if (textHintNotice != null)
        {
            textHintNotice.text = "<b>[호루스의 4대 카노푸스 단지]</b>\n단지를 터치하여 동·서·남·북 각 방위를 수호하는 신성한 신수 뚜껑을 일치시키십시오.\n<size=16>(북: 원숭이 / 남: 인간 / 동: 자칼 / 서: 매)</size>";
        }

        UpdateVisuals();
    }

    private void OnEnable()
    {
        ClearFeedback();
        UpdateVisuals();
    }

    private void CycleGuardian(ref int currentIdx)
    {
        if (isCleared) return;
        ClearFeedback();
        currentIdx = (currentIdx + 1) % guardianNames.Length;
        UpdateVisuals();
    }

    private void CheckSolution()
    {
        if (isCleared) return;

        bool northOk = (curNorth == TARGET_NORTH);
        bool southOk = (curSouth == TARGET_SOUTH);
        bool eastOk = (curEast == TARGET_EAST);
        bool westOk = (curWest == TARGET_WEST);

        if (northOk && southOk && eastOk && westOk)
        {
            isCleared = true;
            ClearFeedback();
            if (ankhCenterGlow != null) ankhCenterGlow.color = colCorrect;
            if (successNotice != null) successNotice.SetActive(true);

            if (AnubisNotebookManager.Instance != null)
            {
                AnubisNotebookManager.Instance.UnlockClue(6,
                    "네 수호신이 증명하니 마침내 모든 진실이 드러났도다. [황금 A=3kg], [청동 B=5kg], [흙 C=1kg], [흑요석 D=2kg]. 망자의 심장(4kg)과 진실의 깃털(4kg)을 들고 중앙 '최후의 영혼 심판대'로 나아가라!");
            }
        }
        else
        {
            int correctCount = (northOk ? 1 : 0) + (southOk ? 1 : 0) + (eastOk ? 1 : 0) + (westOk ? 1 : 0);
            ShowFeedback($"<color=#FF4444>방위 신수 불일치: 현재 {correctCount}개의 방위만 올바르게 정렬되었습니다.</color>");
        }
    }

    private void ResetJars()
    {
        if (isCleared) return;
        curNorth = 0;
        curSouth = 2;
        curEast = 3;
        curWest = 1;
        ClearFeedback();
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (textNorth != null) textNorth.text = guardianNames[curNorth];
        if (textSouth != null) textSouth.text = guardianNames[curSouth];
        if (textEast != null) textEast.text = guardianNames[curEast];
        if (textWest != null) textWest.text = guardianNames[curWest];

        if (glowNorth != null) glowNorth.color = (curNorth == TARGET_NORTH) ? colCorrect : colIncorrect;
        if (glowSouth != null) glowSouth.color = (curSouth == TARGET_SOUTH) ? colCorrect : colIncorrect;
        if (glowEast != null) glowEast.color = (curEast == TARGET_EAST) ? colCorrect : colIncorrect;
        if (glowWest != null) glowWest.color = (curWest == TARGET_WEST) ? colCorrect : colIncorrect;

        if (ankhCenterGlow != null && !isCleared)
            ankhCenterGlow.color = colIncorrect;
    }

    private void ShowFeedback(string msg)
    {
        ClearFeedback();
        feedbackCoroutine = StartCoroutine(FeedbackRoutine(msg));
    }

    private IEnumerator FeedbackRoutine(string msg)
    {
        if (textFeedbackNotice != null) textFeedbackNotice.text = msg;
        yield return new WaitForSeconds(2.5f);
        if (textFeedbackNotice != null) textFeedbackNotice.text = "";
    }

    private void ClearFeedback()
    {
        if (feedbackCoroutine != null)
        {
            StopCoroutine(feedbackCoroutine);
            feedbackCoroutine = null;
        }
        if (textFeedbackNotice != null) textFeedbackNotice.text = "";
    }
}