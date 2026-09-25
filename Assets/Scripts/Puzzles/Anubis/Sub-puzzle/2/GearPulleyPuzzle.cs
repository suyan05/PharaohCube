using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GearPulleyPuzzle : MonoBehaviour
{
    [Header("UI 텍스트 연결")]
    [SerializeField] private TMP_Text textDriverGear;  // 구동 기어 표시
    [SerializeField] private TMP_Text textDrivenGear;  // 종동 기어 표시
    [SerializeField] private TMP_Text textPulleyMode;   // 도르래 모드 표시
    [SerializeField] private TMP_Text textRatioCalc;    // 현재 계산 배율 표시
    [SerializeField] private TMP_Text textResultNotice; // 실패 경고 안내 (자동 사라짐)

    [Header("버튼 연결")]
    [SerializeField] private Button btnDriverCycle;    // 구동 기어 교체 버튼
    [SerializeField] private Button btnDrivenCycle;    // 종동 기어 교체 버튼
    [SerializeField] private Button btnPulleyToggle;   // 도르래 토글 버튼
    [SerializeField] private Button btnOperateCrank;   // 크랭크 레버 당기기 버튼
    [SerializeField] private Button btnClose;          // 나가기 버튼

    [Header("성공 패널")]
    [SerializeField] private GameObject successNotice;

    // 기어 잇수 옵션 (12T, 24T, 36T)
    private readonly int[] gearTeethOptions = { 12, 24, 36 };
    private int driverIndex = 0;
    private int drivenIndex = 0;
    private int pulleyMultiplier = 1;

    private bool isCleared = false;
    private Coroutine failureMsgCoroutine;

    private void Start()
    {
        if (btnDriverCycle != null) btnDriverCycle.onClick.AddListener(CycleDriverGear);
        if (btnDrivenCycle != null) btnDrivenCycle.onClick.AddListener(CycleDrivenGear);
        if (btnPulleyToggle != null) btnPulleyToggle.onClick.AddListener(TogglePulleyMode);
        if (btnOperateCrank != null) btnOperateCrank.onClick.AddListener(CheckMechanicalAdvantage);
        if (btnClose != null) btnClose.onClick.AddListener(() => gameObject.SetActive(false));

        if (successNotice != null) successNotice.SetActive(false);
        if (textResultNotice != null) textResultNotice.text = "";

        UpdateUI();
    }

    private void OnEnable()
    {
        ClearFailureMessage();
    }

    private void CycleDriverGear()
    {
        if (isCleared) return;
        ClearFailureMessage();
        driverIndex = (driverIndex + 1) % gearTeethOptions.Length;
        UpdateUI();
    }

    private void CycleDrivenGear()
    {
        if (isCleared) return;
        ClearFailureMessage();
        drivenIndex = (drivenIndex + 1) % gearTeethOptions.Length;
        UpdateUI();
    }

    private void TogglePulleyMode()
    {
        if (isCleared) return;
        ClearFailureMessage();
        pulleyMultiplier = (pulleyMultiplier == 1) ? 2 : 1;
        UpdateUI();
    }

    private void CheckMechanicalAdvantage()
    {
        if (isCleared) return;

        float gearRatio = (float)gearTeethOptions[drivenIndex] / gearTeethOptions[driverIndex];
        float totalAdvantage = gearRatio * pulleyMultiplier;

        if (Mathf.Approximately(totalAdvantage, 4.0f))
        {
            isCleared = true;
            ClearFailureMessage();

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
            string failMsg = (totalAdvantage < 4.0f)
                ? $"<color=#FF4444>장치 작동 실패: 힘이 부족하여 추(1kg)가 들리지 않습니다.\n(현재 배율: {totalAdvantage:F2}배 / 필요: 4.00배)</color>"
                : $"<color=#FF4444>장치 작동 실패: 힘이 과도하여 와이어가 끊어지려 합니다!\n(현재 배율: {totalAdvantage:F2}배 / 필요: 4.00배)</color>";

            ShowFailureMessage(failMsg);
        }
    }

    private void ShowFailureMessage(string msg)
    {
        ClearFailureMessage();
        failureMsgCoroutine = StartCoroutine(FailureRoutine(msg));
    }

    private IEnumerator FailureRoutine(string msg)
    {
        if (textResultNotice != null) textResultNotice.text = msg;
        yield return new WaitForSeconds(2.5f); // 2.5초 후 자동 삭제
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

    private void UpdateUI()
    {
        int driverT = gearTeethOptions[driverIndex];
        int drivenT = gearTeethOptions[drivenIndex];
        float gearRatio = (float)drivenT / driverT;
        float totalAdvantage = gearRatio * pulleyMultiplier;

        if (textDriverGear != null)
            textDriverGear.text = $"구동 기어 (입력)\n<b>{driverT} T</b>";

        if (textDrivenGear != null)
            textDrivenGear.text = $"종동 기어 (출력)\n<b>{drivenT} T</b>";

        if (textPulleyMode != null)
        {
            string modeName = (pulleyMultiplier == 1) ? "고정 도르래 (1배)" : "움직도르래 (2배)";
            textPulleyMode.text = $"도르래 장치\n<b>{modeName}</b>";
        }

        if (textRatioCalc != null)
        {
            textRatioCalc.text = $"기어비: <b>{gearRatio:F2}배</b>  ×  도르래: <b>{pulleyMultiplier}배</b>  =  총 배율: <b>{totalAdvantage:F2}배</b>\n<size=18>(요구치: 1kg 추로 4kg 유물 들어올리기)</size>";
        }
    }
}