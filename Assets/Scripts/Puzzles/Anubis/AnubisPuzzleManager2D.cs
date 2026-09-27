using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class AnubisPuzzleManager2D : MonoBehaviour
{
    [Header("저울 컴포넌트 연결")]
    [SerializeField] private RectTransform scaleBeam;
    [SerializeField] private AnubisUIScalePan leftPan;
    [SerializeField] private AnubisUIScalePan rightPan;

    [Header("판정 기준 (사자의 서 비문: 4.0kg)")]
    [SerializeField] private float targetFeatherWeight = 4.0f; // 목표 깃털 4kg

    [Header("심판 및 연출 UI")]
    [SerializeField] private Button btnJudgeLever;             // 아누비스 심판 레버 버튼
    [SerializeField] private GameObject ammitPenaltyPanel;     // 실패 시 붉은 암무트 패널
    [SerializeField] private TMP_Text weightText;              // 깃털 안내 텍스트
    [SerializeField] private GameObject clearText;             // 성공 텍스트/패널
    [SerializeField] private Button btnRetry;                  // (선택) 게임오버 시 재도전 버튼

    public event Action OnPuzzleCleared;
    private bool isCleared = false;
    private bool isJudging = false;
    private bool isGameOver = false;

    private float LeftWeight => leftPan != null ? leftPan.TotalWeight : 0f;

    private void Start()
    {
        if (clearText != null) clearText.SetActive(false);
        if (ammitPenaltyPanel != null) ammitPenaltyPanel.SetActive(false);
        if (btnRetry != null)
        {
            btnRetry.gameObject.SetActive(false);
            btnRetry.onClick.AddListener(RestartScene);
        }

        if (scaleBeam != null)
            scaleBeam.localRotation = Quaternion.identity;

        if (weightText != null)
            weightText.text = $"진실의 깃털(마아트): <b>{targetFeatherWeight:F1} kg</b>";

        if (btnJudgeLever != null)
            btnJudgeLever.onClick.AddListener(OnPullJudgmentLever);

    }

    public void OnPullJudgmentLever()
    {
        if (isCleared || isJudging || isGameOver) return;
        StartCoroutine(JudgmentRoutine());
    }

    private IEnumerator JudgmentRoutine()
    {
        isJudging = true;

        if (btnJudgeLever != null) btnJudgeLever.interactable = false;

        var placedItems = leftPan != null ? new List<AnubisUIDragItem>(leftPan.Items) : new List<AnubisUIDragItem>();
        bool hasHeart = false;

        foreach (var item in placedItems)
        {
            if (item != null && item.isHeart) hasHeart = true;
        }

        // 정답 판정: 망자의 심장(4.0kg)이 좌측 접시에 올라가 깃털(4.0kg)과 정확히 평형을 이룸
        bool isSuccess = (hasHeart && Mathf.Approximately(LeftWeight, targetFeatherWeight));

        float diff = LeftWeight - targetFeatherWeight;
        float targetAngle = isSuccess ? 0f : (diff > 0 ? -18f : 18f);

        float elapsed = 0f;
        float duration = 0.6f;
        Quaternion startRot = scaleBeam != null ? scaleBeam.localRotation : Quaternion.identity;
        Quaternion endRot = Quaternion.Euler(0f, 0f, targetAngle);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (scaleBeam != null)
                scaleBeam.localRotation = Quaternion.Slerp(startRot, endRot, elapsed / duration);
            yield return null;
        }

        if (scaleBeam != null) scaleBeam.localRotation = endRot;

        if (isSuccess)
        {
            isCleared = true;
            OnPuzzleCleared?.Invoke();

            if (clearText != null)
            {
                clearText.SetActive(true);
                TMP_Text tmp = clearText.GetComponentInChildren<TMP_Text>();
                if (tmp != null)
                {
                    tmp.text = "<color=#FFD700><b>[최후의 심판 통과]</b>\n망자의 심장이 진실의 깃털과 완벽한 평형(4.0kg)을 이루었도다!\n영혼이 아알루(낙원)로 승천합니다.</color>";
                }
            }
        }
        else
        {
            // [단 1번의 기회 - 게임 오버]
            isGameOver = true;

            if (ammitPenaltyPanel != null)
            {
                ammitPenaltyPanel.SetActive(true);
                TMP_Text penaltyTxt = ammitPenaltyPanel.GetComponentInChildren<TMP_Text>();
                if (penaltyTxt != null)
                {
                    penaltyTxt.text = "<color=#000000><b>[최후의 심판 실패]</b>\n\n불균형으로 인해 저울이 기울어졌습니다!\n괴수 암무트가 망자의 영혼을 영원히 집어삼켰습니다.\n\n<size=20><color=#FFFFFF>심판의 기회가 소진되었습니다.</color></size></color>";
                }
            }

            if (btnRetry != null) btnRetry.gameObject.SetActive(true);

            Debug.LogWarning("[Anubis 2D] 1회 기회 소진. 암무트에 의해 영혼이 파괴되었습니다.");
        }

        isJudging = false;
    }

    public void RestartScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
}