using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GearPulleyPuzzle : MonoBehaviour
{
    [Header("시각 그래픽 트랜스폼")]
    [SerializeField] private RectTransform driverGearTransform; // 구동 기어 (회전 및 크기)
    [SerializeField] private RectTransform drivenGearTransform; // 종동 기어 (회전 및 크기)
    [SerializeField] private RectTransform relicPlateTransform; // 들어올려질 4kg 유물 석판
    [SerializeField] private Image imgPulleyIndicator;         // 도르래 상태 그래픽

    [Header("UI 텍스트")]
    [SerializeField] private TMP_Text textDriverGear;
    [SerializeField] private TMP_Text textDrivenGear;
    [SerializeField] private TMP_Text textPulleyMode;
    [SerializeField] private TMP_Text textRatioCalc;
    [SerializeField] private TMP_Text textResultNotice;

    [Header("조작 버튼")]
    [SerializeField] private Button btnDriverCycle;
    [SerializeField] private Button btnDrivenCycle;
    [SerializeField] private Button btnPulleyToggle;
    [SerializeField] private Button btnOperateCrank;
    [SerializeField] private Button btnClose;

    [Header("성공 패널")]
    [SerializeField] private GameObject successNotice;

    private readonly int[] gearTeethOptions = { 12, 24, 36 };
    private readonly float[] gearScaleMultipliers = { 0.75f, 1.1f, 1.45f };

    private int driverIndex = 0;
    private int drivenIndex = 0;
    private int pulleyMultiplier = 1;

    private bool isCleared = false;
    private bool isOperating = false;
    private Vector2 relicInitialPos;
    private bool isPosInitialized = false;
    private Coroutine failureMsgCoroutine;

    private void Awake()
    {
        if (relicPlateTransform != null && !isPosInitialized)
        {
            relicInitialPos = relicPlateTransform.anchoredPosition;
            isPosInitialized = true;
        }
    }

    private void Start()
    {
        if (btnDriverCycle != null) btnDriverCycle.onClick.AddListener(CycleDriverGear);
        if (btnDrivenCycle != null) btnDrivenCycle.onClick.AddListener(CycleDrivenGear);
        if (btnPulleyToggle != null) btnPulleyToggle.onClick.AddListener(TogglePulleyMode);
        if (btnOperateCrank != null) btnOperateCrank.onClick.AddListener(OperateCrank);
        if (btnClose != null) btnClose.onClick.AddListener(() => gameObject.SetActive(false));

        if (successNotice != null) successNotice.SetActive(false);
        if (textResultNotice != null) textResultNotice.text = "";

        UpdateVisuals();
    }

    private void OnEnable()
    {
        ClearFailureMessage();

        if (relicPlateTransform != null && isPosInitialized && !isCleared)
        {
            relicPlateTransform.anchoredPosition = relicInitialPos;
        }
    }

    private void CycleDriverGear()
    {
        if (isCleared || isOperating) return;
        ClearFailureMessage();
        driverIndex = (driverIndex + 1) % gearTeethOptions.Length;
        UpdateVisuals();
    }

    private void CycleDrivenGear()
    {
        if (isCleared || isOperating) return;
        ClearFailureMessage();
        drivenIndex = (drivenIndex + 1) % gearTeethOptions.Length;
        UpdateVisuals();
    }

    private void TogglePulleyMode()
    {
        if (isCleared || isOperating) return;
        ClearFailureMessage();
        pulleyMultiplier = (pulleyMultiplier == 1) ? 2 : 1;
        UpdateVisuals();
    }

    private void OperateCrank()
    {
        if (isCleared || isOperating) return;
        StartCoroutine(CrankOperationRoutine());
    }

    private IEnumerator CrankOperationRoutine()
    {
        isOperating = true;
        ClearFailureMessage();

        int driverT = gearTeethOptions[driverIndex];
        int drivenT = gearTeethOptions[drivenIndex];
        float gearRatio = (float)drivenT / driverT;
        float totalAdvantage = gearRatio * pulleyMultiplier;

        float duration = 1.2f;
        float elapsed = 0f;

        float driverTotalRotate = -360f;
        float drivenTotalRotate = 360f * ((float)driverT / drivenT);

        Vector2 startRelicPos = relicInitialPos;
        Vector2 targetRelicPos = relicInitialPos + new Vector2(0, Mathf.Approximately(totalAdvantage, 4.0f) ? 70f : 20f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float step = Time.deltaTime / duration;

            if (driverGearTransform != null)
                driverGearTransform.Rotate(0, 0, driverTotalRotate * step);

            if (drivenGearTransform != null)
                drivenGearTransform.Rotate(0, 0, drivenTotalRotate * step);

            if (relicPlateTransform != null)
                relicPlateTransform.anchoredPosition = Vector2.Lerp(startRelicPos, targetRelicPos, elapsed / duration);

            yield return null;
        }

        if (Mathf.Approximately(totalAdvantage, 4.0f))
        {
            isCleared = true;
            if (textRatioCalc != null) textRatioCalc.gameObject.SetActive(false);
            if (successNotice != null) successNotice.SetActive(true);

            if (AnubisNotebookManager.Instance != null)
            {
                AnubisNotebookManager.Instance.UnlockClue(1,
                    "청동의 그릇(B)과 흙(C)의 무게 합은, 밤의 흑요석(D)과 황금(A)의 합보다 가장 작은 무게 단위(1kg)만큼 무겁도다. ((B + C) - (D + A) = 1)");
            }
        }
        else
        {
            if (relicPlateTransform != null)
                relicPlateTransform.anchoredPosition = relicInitialPos;

            string failMsg = (totalAdvantage < 4.0f)
                ? $"<color=#FF4444>동력 부족: 유물이 무거워 들어 올리지 못했습니다. (현재 배율: {totalAdvantage:F2}배 / 필요: 4.00배)</color>"
                : $"<color=#FF4444>과부하: 와이어 텐션 한계를 초과했습니다! (현재 배율: {totalAdvantage:F2}배 / 필요: 4.00배)</color>";

            ShowFailureMessage(failMsg);
        }

        isOperating = false;
    }

    private void ShowFailureMessage(string msg)
    {
        ClearFailureMessage();
        failureMsgCoroutine = StartCoroutine(FailureRoutine(msg));
    }

    private IEnumerator FailureRoutine(string msg)
    {
        if (textResultNotice != null) textResultNotice.text = msg;
        yield return new WaitForSeconds(2.5f);
        if (textResultNotice != null) textResultNotice.text = "";
    }

    private void ClearFailureMessage()
    {
        if (failureMsgCoroutine != null)
        {
            StopCoroutine(failureMsgCoroutine);
            failureMsgCoroutine = null;
        }
        if (textResultNotice != null) textResultNotice.text = "";
    }

    private void UpdateVisuals()
    {
        int driverT = gearTeethOptions[driverIndex];
        int drivenT = gearTeethOptions[drivenIndex];
        float gearRatio = (float)drivenT / driverT;
        float totalAdvantage = gearRatio * pulleyMultiplier;

        if (driverGearTransform != null)
            driverGearTransform.localScale = Vector3.one * gearScaleMultipliers[driverIndex];

        if (drivenGearTransform != null)
            drivenGearTransform.localScale = Vector3.one * gearScaleMultipliers[drivenIndex];

        if (imgPulleyIndicator != null)
            imgPulleyIndicator.color = (pulleyMultiplier == 2) ? new Color(1f, 0.85f, 0.3f, 1f) : new Color(0.6f, 0.6f, 0.6f, 1f);

        if (textDriverGear != null) textDriverGear.text = $"구동 기어\n<b>{driverT} T</b>";
        if (textDrivenGear != null) textDrivenGear.text = $"종동 기어\n<b>{drivenT} T</b>";
        if (textPulleyMode != null)
            textPulleyMode.text = (pulleyMultiplier == 1) ? "도르래 장치\n<b>고정 (1배)</b>" : "도르래 장치\n<b>움직 (2배)</b>";

        if (textRatioCalc != null)
        {
            textRatioCalc.text = $"기어비: <b>{gearRatio:F2}배</b> × 도르래: <b>{pulleyMultiplier}배</b> = 총 배율: <b>{totalAdvantage:F2}배</b>\n<size=17>(목표: 1kg 추로 4kg 유물 들어올리기)</size>";
        }
    }
}