using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AnubisPuzzleManager2D : MonoBehaviour
{
    [Header("저울 컴포넌트 연결")]
    [SerializeField] private RectTransform scaleBeam;
    [SerializeField] private AnubisUIScalePan leftPan;
    [SerializeField] private AnubisUIScalePan rightPan;

    [Header("판정 기준")]
    [SerializeField] private float targetFeatherWeight = 7.0f; // 목표 깃털 무게 (7kg)

    [Header("심판 및 연출 UI")]
    [SerializeField] private Button btnJudgeLever;             // 아누비스 심판 레버 버튼
    [SerializeField] private GameObject ammitPenaltyPanel;     // 실패 시 붉은 암전문 패널
    [SerializeField] private TMP_Text weightText;              // 깃털 안내 텍스트
    [SerializeField] private GameObject clearText;             // 성공 텍스트

    public event Action OnPuzzleCleared;
    private bool isCleared = false;
    private bool isJudging = false;

    private float LeftWeight => leftPan != null ? leftPan.TotalWeight : 0f;

    private void Start()
    {
        if (clearText != null) clearText.SetActive(false);
        if (ammitPenaltyPanel != null) ammitPenaltyPanel.SetActive(false);

        if (scaleBeam != null) scaleBeam.localRotation = Quaternion.identity;

        if (weightText != null) weightText.text = $"진실의 깃털: {targetFeatherWeight:F1} kg";

        if (btnJudgeLever != null)
            btnJudgeLever.onClick.AddListener(OnPullJudgmentLever);
    }


    public void OnPullJudgmentLever()
    {
        if (isCleared || isJudging) return;
        StartCoroutine(JudgmentRoutine());
    }

    private IEnumerator JudgmentRoutine()
    {
        isJudging = true;

        var placedItems = leftPan != null ? new List<AnubisUIDragItem>(leftPan.Items) : new List<AnubisUIDragItem>();
        bool hasHeart = false;

        foreach (var item in placedItems)
        {
            if (item.isHeart) hasHeart = true;
        }

        // 성공 조건: 심장 포함 + 정확히 2개 적재 + 합계 7.0kg
        bool isSuccess = (hasHeart && placedItems.Count == 2 && Mathf.Approximately(LeftWeight, targetFeatherWeight));

        if (isSuccess)
        {
            isCleared = true;
            Debug.Log("<color=yellow>[Anubis 2D] 심판 통과! 영혼이 결백을 얻었도다!</color>");
            OnPuzzleCleared?.Invoke();

            if (clearText != null) clearText.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[Anubis 2D] 심판 실패! 불균형으로 인해 암무트가 영혼을 물어뜯습니다.");

            // 실패 연출: 저울이 기울어짐
            float tiltZ = (LeftWeight > targetFeatherWeight) ? 15f : -15f;
            if (scaleBeam != null)
                scaleBeam.localRotation = Quaternion.Euler(0f, 0f, tiltZ);

            yield return new WaitForSeconds(0.6f);

            // 암무트 패널 점멸
            if (ammitPenaltyPanel != null)
                ammitPenaltyPanel.SetActive(true);

            yield return new WaitForSeconds(1.2f);

            // 올려둔 유물 모두 바닥 원래 자리로 강제 복귀
            foreach (var item in placedItems)
            {
                if (item != null)
                    item.ReturnToOriginalSlot();
            }

            // 저울대 다시 수평(0도) 복구
            if (scaleBeam != null)
                scaleBeam.localRotation = Quaternion.identity;

            if (ammitPenaltyPanel != null)
                ammitPenaltyPanel.SetActive(false);
        }

        isJudging = false;
    }
}