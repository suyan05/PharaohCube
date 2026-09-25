using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AnubisScaleManager : MonoBehaviour
{
    [Header("판정 기준")]
    public float targetFeatherWeight = 7.0f; // 목표 깃털 7kg
    public Transform leftPan;                // 좌측 접시 (Left_Pan)

    [Header("UI 연결")]
    public RectTransform balanceBeam;        // 가로 저울대 (Scale_Beam)
    public Button btnJudgeLever;             // 심판 레버 버튼
    public GameObject ammitPenaltyPanel;     // 실패 시 붉은 패널
    public TMP_Text clearText;               // 클리어 문구 (ClearText)

    private bool isCleared = false;
    private bool isJudging = false;

    private void Start()
    {
        if (btnJudgeLever != null)
            btnJudgeLever.onClick.AddListener(OnPullLever);

        if (ammitPenaltyPanel != null)
            ammitPenaltyPanel.SetActive(false);

        if (clearText != null)
            clearText.gameObject.SetActive(false);

        if (balanceBeam != null)
            balanceBeam.localRotation = Quaternion.Euler(0, 0, 0);
    }

    public void OnPullLever()
    {
        if (isCleared || isJudging) return;
        StartCoroutine(JudgmentRoutine());
    }

    private IEnumerator JudgmentRoutine()
    {
        isJudging = true;

        AnubisRelicItem[] items = leftPan.GetComponentsInChildren<AnubisRelicItem>();

        float totalWeight = 0f;
        bool hasHeart = false;

        foreach (var item in items)
        {
            totalWeight += item.actualWeight;
            if (item.isHeart) hasHeart = true;
        }

        bool isSuccess = (hasHeart && items.Length == 2 && Mathf.Approximately(totalWeight, targetFeatherWeight));

        if (isSuccess)
        {
            isCleared = true;
            if (clearText != null)
            {
                clearText.gameObject.SetActive(true);
                clearText.text = "<color=#FFD700>심판 통과: 영혼이 결백을 얻었도다!\n[심판의 형판 획득]</color>";
            }
        }
        else
        {
            float tilt = (totalWeight > targetFeatherWeight) ? 15f : -15f;
            if (balanceBeam != null)
                balanceBeam.localRotation = Quaternion.Euler(0, 0, tilt);

            yield return new WaitForSeconds(0.6f);

            if (ammitPenaltyPanel != null)
                ammitPenaltyPanel.SetActive(true);

            yield return new WaitForSeconds(1.2f);

            foreach (var item in items)
            {
                item.ReturnToSlot();
            }

            if (balanceBeam != null)
                balanceBeam.localRotation = Quaternion.Euler(0, 0, 0);

            if (ammitPenaltyPanel != null)
                ammitPenaltyPanel.SetActive(false);
        }

        isJudging = false;
    }
}