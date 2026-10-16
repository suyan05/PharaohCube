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
public class AnubisPuzzleManager2D : MonoBehaviour, IAnubisRetryable
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
    [Tooltip("[추가] 켜면 무게 합이 깃털과 같은 조합은 모두 정답 (복수 정답)\n" +
             "7kg 조합: 심장+황금 단지(H+A) / 청동 그릇+흑요석(B+D) / 심장+흙의 단지+흑요석(H+C+D)\n" +
             "끄면 기존처럼 심장 + 황금 단지, 정확히 2개만 정답")]
    [SerializeField] private bool acceptAnyCombination = true;

    [Header("심판 및 연출 UI")]
    [SerializeField] private Button btnJudgeLever;         // 심판 레버 버튼
    [SerializeField] private GameObject ammitPenaltyPanel; // 실패 시 암무트 패널
    [SerializeField] private TMP_Text weightText;          // 깃털 안내 텍스트
    [SerializeField] private GameObject clearText;         // 성공 텍스트/패널
    [SerializeField] private Button btnRetry;              // 게임오버 시 재도전 버튼

    [Header("실패 처벌 [추가] (비우면 기존처럼 씬 재시작)")]
    [Tooltip("연결하면 다시 도전 시 씬을 다시 불러오지 않고 보상 회수 + 서브 퍼즐 재잠금")]
    [SerializeField] private AnubisJudgmentPenalty penalty;

    [Header("닫기 [추가]")]
    [Tooltip("닫기 버튼. 비우면 패널 안의 'Btn_Close'를 자동으로 찾음")]
    [SerializeField] private Button btnClose;

    [Header("모두 내리기 [추가]")]
    [Tooltip("접시에 올린 유물을 전부 진열대로 돌리는 버튼. 비우면 'Btn_ClearPan'을 찾고, 없으면 심판 레버 버튼을 복사해 아래에 만듦")]
    [SerializeField] private Button btnClearPans;
    [SerializeField] private bool autoCreateClearButton = true;
    [SerializeField] private string clearButtonLabel = "모두 내리기";

    public event Action OnPuzzleCleared;
    public event Action OnPuzzleFailed; 

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
            btnRetry.onClick.AddListener(OnClickRetry);  
        }

        if (scaleBeam != null)
            scaleBeam.localRotation = Quaternion.identity;

        if (weightText != null)
            weightText.text = $"진실의 깃털(마아트): <b>{targetFeatherWeight:F1} kg</b>";

        if (btnJudgeLever != null)
            btnJudgeLever.onClick.AddListener(OnPullJudgmentLever);

        // [추가] 닫기 버튼: 씬에 클릭 연결이 비어 있어서 눌러도 안 닫히던 문제
        if (btnClose == null)
        {
            Transform found = transform.Find("Btn_Close");
            if (found != null) btnClose = found.GetComponent<Button>();
        }
        if (btnClose != null) btnClose.onClick.AddListener(ClosePanel);
        else Debug.LogWarning($"[{name}] 닫기 버튼(Btn_Close)을 찾지 못함", this);

        SetupClearButton(); // [추가]

        // [추가] 그림용 복사본(꺼진 접시)이 연결돼 있으면 진짜 접시로 바꿈
        leftPan = ResolvePan(leftPan);
        rightPan = ResolvePan(rightPan);
    }

    private static AnubisUIScalePan ResolvePan(AnubisUIScalePan pan)
    {
        if (pan == null || pan.enabled || pan.transform.parent == null) return pan;
        var real = pan.transform.parent.GetComponentInParent<AnubisUIScalePan>();
        return real != null ? real : pan;
    }

    // [추가] '모두 내리기' 버튼 준비 (씬에 없으면 심판 레버 버튼을 복사해서 바로 아래에 만듦)
    private void SetupClearButton()
    {
        if (btnClearPans == null)
        {
            Transform found = transform.Find("Btn_ClearPan");
            if (found != null) btnClearPans = found.GetComponent<Button>();
        }

        if (btnClearPans == null && autoCreateClearButton && btnJudgeLever != null)
        {
            GameObject clone = Instantiate(btnJudgeLever.gameObject, btnJudgeLever.transform.parent);
            clone.name = "Btn_ClearPan";
            btnClearPans = clone.GetComponent<Button>();
            btnClearPans.onClick = new Button.ButtonClickedEvent(); // 레버 동작은 복사하지 않음
            btnClearPans.interactable = true;

            var src = (RectTransform)btnJudgeLever.transform;
            var dst = (RectTransform)clone.transform;
            dst.anchoredPosition = src.anchoredPosition + new Vector2(0f, -(src.rect.height + 16f));

            TMP_Text label = clone.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = clearButtonLabel;
        }

        if (btnClearPans != null) btnClearPans.onClick.AddListener(ClearPans);
    }

    // [추가] 접시 위 유물을 전부 원래 자리로
    public void ClearPans()
    {
        if (isJudging || isCleared) return;
        // [수정] 패널 안의 모든 접시(복사본 포함)를 비움
        foreach (var pan in GetComponentsInChildren<AnubisUIScalePan>(true)) ReturnItems(pan);
        Debug.Log($"[{name}] 접시의 유물을 모두 내림", this);
    }

    // [추가] 패널을 끄면 AnubisAltarStation이 알아채고 플레이어 이동 잠금을 풂
    public void ClosePanel()
    {
        if (isJudging) return; // 저울이 기우는 연출 중에는 닫지 않음
        gameObject.SetActive(false);
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

        // [수정] 복수 정답: 무게만 맞으면 통과 (피드백). 끄면 기존 규칙
        bool weightMatch = placedItems.Count > 0 && Mathf.Abs(LeftWeight - targetFeatherWeight) < weightTolerance;
        bool isSuccess = acceptAnyCombination
            ? weightMatch
            : hasHeart && hasGold && placedItems.Count == requiredItemCount && weightMatch;

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
                    // [수정] 복수 정답이면 어떤 보물을 올렸든 맞는 문구
                    tmp.text = acceptAnyCombination
                        ? $"<color=#FFD700><b>[최후의 심판 통과]</b>\n올린 보물이 진실의 깃털과 완벽한 평형({targetFeatherWeight:F1}kg)을 이루었도다!\n영혼이 아알루(낙원)로 승천합니다.</color>"
                        : "<color=#FFD700><b>[최후의 심판 통과]</b>\n망자의 심장과 황금 단지가 진실의 깃털과 완벽한 평형(7.0kg)을 이루었도다!\n영혼이 아알루(낙원)로 승천합니다.</color>";
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
                    
                    penaltyTxt.text = penalty != null
                        ? "<color=#000000><b>[최후의 심판 실패]</b>\n\n불균형으로 인해 저울이 기울어졌습니다!\n괴수 암무트가 네 개의 보물을 집어삼켰습니다.\n\n<size=20><color=#FFFFFF>다시 도전하면 첫 번째 시험부터 다시 시작합니다.</color></size></color>"
                        : "<color=#000000><b>[최후의 심판 실패]</b>\n\n불균형으로 인해 저울이 기울어졌습니다!\n괴수 암무트가 망자의 영혼을 영원히 집어삼켰습니다.\n\n<size=20><color=#FFFFFF>심판의 기회가 소진되었습니다.</color></size></color>";
                }
            }

            if (btnRetry != null) btnRetry.gameObject.SetActive(true);
            OnPuzzleFailed?.Invoke();

            Debug.LogWarning("[Anubis Scale] 1회 기회 소진. 암무트에 의해 영혼이 파괴되었습니다.", this);
        }

        isJudging = false;
    }

    
    private void OnClickRetry()
    {
        if (penalty != null) penalty.ApplyPenalty();
        else RestartScene();
    }

    
    public void ResetForRetry()
    {
        StopAllCoroutines();
        isCleared = false;
        isJudging = false;
        isGameOver = false;

        ReturnItems(leftPan);
        ReturnItems(rightPan);

        if (scaleBeam != null) scaleBeam.localRotation = Quaternion.identity;
        if (ammitPenaltyPanel != null) ammitPenaltyPanel.SetActive(false);
        if (clearText != null) clearText.SetActive(false);
        if (btnRetry != null) btnRetry.gameObject.SetActive(false);
        if (btnJudgeLever != null) btnJudgeLever.interactable = true;
    }

    private static void ReturnItems(AnubisUIScalePan pan)
    {
        if (pan == null) return;
        var copy = new List<AnubisUIDragItem>(pan.Items);
        foreach (var item in copy)
            if (item != null) item.ReturnToOriginalSlot();
    }

    public void RestartScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
}