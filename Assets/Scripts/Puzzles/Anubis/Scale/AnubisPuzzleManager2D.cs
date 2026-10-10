using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 아누비스의 심판 (메인 저울). 우측 깃털 7.0kg과 평형 맞추기
// 좌측 접시에 망자의 심장(4kg) + 황금 단지(3kg), 정확히 2개
// 패널 루트(Panel_MainScale)에 부착
public class AnubisPuzzleManager2D : MonoBehaviour
{
    [Header("저울 컴포넌트 연결")]
    [SerializeField] private RectTransform scaleBeam;
    [SerializeField] private AnubisUIScalePan leftPan;
    [SerializeField] private AnubisUIScalePan rightPan;

    [Header("판정 기준 (기획서 2.2: 깃털 7.0kg, 유물 정확히 2개)")]
    [Tooltip("우측 접시 마아트의 깃털 무게 (기존 4.0 → 7.0)")]
    [SerializeField] private float targetFeatherWeight = 7.0f;
    [Tooltip("좌측 접시에 올려야 하는 유물 개수 [추가] (심장 + 황금 단지)")]
    [SerializeField] private int requiredItemCount = 2;
    [Tooltip("float 비교 오차 허용치 [추가]")]
    [SerializeField] private float weightTolerance = 0.01f;

    [Header("심판 및 연출 UI")]
    [SerializeField] private Button btnJudgeLever;         // 심판 레버 버튼
    [SerializeField] private GameObject ammitPenaltyPanel; // 실패 시 암무트 패널
    [SerializeField] private TMP_Text weightText;          // 깃털 안내 텍스트
    [SerializeField] private GameObject clearText;         // 성공 텍스트/패널
    [SerializeField] private Button btnRetry;              // 게임오버 시 재도전 버튼

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

        var placedItems = leftPan != null
            ? new List<AnubisUIDragItem>(leftPan.Items)
            : new List<AnubisUIDragItem>();

        bool hasHeart = false;
        bool hasGold = false;

        foreach (var item in placedItems)
        {
            if (item == null) continue;
            if (item.isHeart) hasHeart = true;
            if (item.isGold) hasGold = true;
        }

        bool isSuccess = hasHeart && hasGold
                      && placedItems.Count == requiredItemCount
                      && Mathf.Abs(LeftWeight - targetFeatherWeight) < weightTolerance;

        Debug.Log($"[Anubis Scale] count={placedItems.Count} weight={LeftWeight:F2} heart={hasHeart} gold={hasGold} → {(isSuccess ? "성공" : "실패")}", this);

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
                    tmp.text = "<color=#FFD700><b>[최후의 심판 통과]</b>\n망자의 심장과 황금 단지가 진실의 깃털과 완벽한 평형(7.0kg)을 이루었도다!\n영혼이 아알루(낙원)로 승천합니다.</color>";
                }
            }
        }
        else
        {
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

            Debug.LogWarning("[Anubis Scale] 1회 기회 소진. 암무트에 의해 영혼이 파괴되었습니다.", this);
        }

        isJudging = false;
    }

    public void RestartScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
}