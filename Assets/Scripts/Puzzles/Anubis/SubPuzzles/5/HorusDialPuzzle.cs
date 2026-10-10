using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HorusDialPuzzle : MonoBehaviour
{
    [Header("회전 링 트랜스폼")]
    [SerializeField] private RectTransform ringOuter;   // 외륜 (청동)
    [SerializeField] private RectTransform ringMiddle;  // 중륜 (석조)
    [SerializeField] private RectTransform ringInner;   // 내륜 (황금)

    [Header("중앙 개안 이펙트")]
    [SerializeField] private Image eyeCenterGlow;       // 정렬 시 황금빛으로 점등하는 호루스의 눈

    [Header("조작 레버 버튼 3개")]
    [SerializeField] private Button btnLeverA;          // 외륜 + 중륜 회전
    [SerializeField] private Button btnLeverB;          // 중륜 + 내륜 회전
    [SerializeField] private Button btnLeverC;          // 내륜 + 외륜 회전

    [Header("시스템 버튼 & 패널")]
    [SerializeField] private Button btnReset;
    [SerializeField] private Button btnClose;
    [SerializeField] private GameObject successNotice;
    [SerializeField] private TMP_Text textHintNotice;

    private int stepOuter = 1;  // 90도
    private int stepMiddle = 3; // 270도
    private int stepInner = 0;  // 0도

    private bool isCleared = false;
    private bool isRotating = false;

    private readonly Color eyeDimColor = new Color(0.3f, 0.35f, 0.4f, 0.6f);
    private readonly Color eyeGlowColor = new Color(1f, 0.85f, 0.2f, 1f);

    private void Start()
    {
        if (btnLeverA != null) btnLeverA.onClick.AddListener(() => RotateRings(true, true, false));
        if (btnLeverB != null) btnLeverB.onClick.AddListener(() => RotateRings(false, true, true));
        if (btnLeverC != null) btnLeverC.onClick.AddListener(() => RotateRings(true, false, true));

        if (btnReset != null) btnReset.onClick.AddListener(ResetDial);
        if (btnClose != null) btnClose.onClick.AddListener(() => gameObject.SetActive(false));

        if (successNotice != null) successNotice.SetActive(false);

        if (textHintNotice != null)
        {
            textHintNotice.text = "<b>[호루스의 천문 회전 다이얼]</b>\n연동된 신전 레버를 조작하여 3개의 신성한 룬을 12시 천문 축에 일렬로 정렬하십시오.";
        }

        ApplyRotationsInstant();
    }

    private void OnEnable()
    {
        if (!isCleared)
        {
            ApplyRotationsInstant();
        }
    }

    private void RotateRings(bool rotateOuter, bool rotateMiddle, bool rotateInner)
    {
        if (isCleared || isRotating) return;

        if (rotateOuter) stepOuter = (stepOuter + 1) % 4;
        if (rotateMiddle) stepMiddle = (stepMiddle + 1) % 4;
        if (rotateInner) stepInner = (stepInner + 1) % 4;

        StartCoroutine(AnimateRingRotations());
    }

    private IEnumerator AnimateRingRotations()
    {
        isRotating = true;

        float duration = 0.25f;
        float elapsed = 0f;

        Quaternion startRotO = ringOuter != null ? ringOuter.localRotation : Quaternion.identity;
        Quaternion startRotM = ringMiddle != null ? ringMiddle.localRotation : Quaternion.identity;
        Quaternion startRotI = ringInner != null ? ringInner.localRotation : Quaternion.identity;

        Quaternion targetRotO = Quaternion.Euler(0, 0, -stepOuter * 90f);
        Quaternion targetRotM = Quaternion.Euler(0, 0, -stepMiddle * 90f);
        Quaternion targetRotI = Quaternion.Euler(0, 0, -stepInner * 90f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            if (ringOuter != null) ringOuter.localRotation = Quaternion.Slerp(startRotO, targetRotO, t);
            if (ringMiddle != null) ringMiddle.localRotation = Quaternion.Slerp(startRotM, targetRotM, t);
            if (ringInner != null) ringInner.localRotation = Quaternion.Slerp(startRotI, targetRotI, t);

            yield return null;
        }

        ApplyRotationsInstant();
        CheckAlignment();
        isRotating = false;
    }

    private void ApplyRotationsInstant()
    {
        if (ringOuter != null) ringOuter.localRotation = Quaternion.Euler(0, 0, -stepOuter * 90f);
        if (ringMiddle != null) ringMiddle.localRotation = Quaternion.Euler(0, 0, -stepMiddle * 90f);
        if (ringInner != null) ringInner.localRotation = Quaternion.Euler(0, 0, -stepInner * 90f);

        if (eyeCenterGlow != null)
            eyeCenterGlow.color = isCleared ? eyeGlowColor : eyeDimColor;
    }

    private void CheckAlignment()
    {
        if (stepOuter == 0 && stepMiddle == 0 && stepInner == 0)
        {
            isCleared = true;
            if (eyeCenterGlow != null) eyeCenterGlow.color = eyeGlowColor;
            if (successNotice != null) successNotice.SetActive(true);

            if (AnubisNotebookManager.Instance != null)
            {
                AnubisNotebookManager.Instance.UnlockClue(4,
                    "심판의 깃털(F)은 청동의 그릇(B)에서 가장 비천한 흙(C)을 덜어낸 무게와 같도다. (F = B - C)");
            }
        }
    }

    public void ResetDial()
    {
        if (isRotating) return;

        stepOuter = 1;
        stepMiddle = 3;
        stepInner = 0;
        isCleared = false;

        if (successNotice != null) successNotice.SetActive(false);
        ApplyRotationsInstant();
    }
}