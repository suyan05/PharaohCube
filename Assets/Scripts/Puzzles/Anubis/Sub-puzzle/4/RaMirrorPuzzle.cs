using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RaMirrorPuzzle : MonoBehaviour
{
    [Header("거울 회전 트랜스폼 및 버튼")]
    [SerializeField] private Button btnMirrorA;
    [SerializeField] private RectTransform mirrorTransA;
    [SerializeField] private Button btnMirrorB;
    [SerializeField] private RectTransform mirrorTransB;
    [SerializeField] private Button btnMirrorC;
    [SerializeField] private RectTransform mirrorTransC;

    [Header("광선 빔 세그먼트 (Image)")]
    [SerializeField] private Image beamSegment1; // 광원 -> 거울 A (항상 점등)
    [SerializeField] private Image beamSegment2; // 거울 A -> 거울 B
    [SerializeField] private Image beamSegment3; // 거울 B -> 거울 C
    [SerializeField] private Image beamSegment4; // 거울 C -> 제단

    [Header("수광 제단 및 이펙트")]
    [SerializeField] private Image receptorGlow; // 제단 도달 시 황금 점등

    [Header("안내 및 피드백 텍스트")]
    [SerializeField] private TMP_Text textHintNotice;

    [Header("공통 버튼 및 성공 패널")]
    [SerializeField] private Button btnReset;
    [SerializeField] private Button btnClose;
    [SerializeField] private GameObject successNotice;

    private readonly float[] angles = { 45f, 135f, 225f, 315f };
    private int indexA = 0;
    private int indexB = 0;
    private int indexC = 0;

    // 정답 각도 인덱스
    // A: 135도 (인덱스 1), B: 225도 (인덱스 2), C: 135도 (인덱스 1)
    private readonly int targetA = 1;
    private readonly int targetB = 2;
    private readonly int targetC = 1;

    private bool isCleared = false;
    private readonly Color colBeamOff = new Color(0.25f, 0.25f, 0.25f, 0.4f);
    private readonly Color colBeamOn = new Color(1f, 0.9f, 0.2f, 1f);

    private void Start()
    {
        if (btnMirrorA != null) btnMirrorA.onClick.AddListener(() => RotateMirror(0));
        if (btnMirrorB != null) btnMirrorB.onClick.AddListener(() => RotateMirror(1));
        if (btnMirrorC != null) btnMirrorC.onClick.AddListener(() => RotateMirror(2));
        if (btnReset != null) btnReset.onClick.AddListener(ResetPuzzle);
        if (btnClose != null) btnClose.onClick.AddListener(() => gameObject.SetActive(false));

        if (successNotice != null) successNotice.SetActive(false);

        if (textHintNotice != null)
        {
            textHintNotice.text = "<b>[태양신 라의 프리즘 거울]</b>\n거울을 터치하여 각도를 회전시키고, 신성한 태양빛을 황금 수광 제단으로 인도하십시오.";
        }

        UpdateBeamVisuals();
    }

    private void OnEnable()
    {
        if (!isCleared) UpdateBeamVisuals();
    }

    private void RotateMirror(int mirrorId)
    {
        if (isCleared) return;

        if (mirrorId == 0)
        {
            indexA = (indexA + 1) % 4;
            if (mirrorTransA != null) mirrorTransA.localRotation = Quaternion.Euler(0, 0, angles[indexA]);
        }
        else if (mirrorId == 1)
        {
            indexB = (indexB + 1) % 4;
            if (mirrorTransB != null) mirrorTransB.localRotation = Quaternion.Euler(0, 0, angles[indexB]);
        }
        else if (mirrorId == 2)
        {
            indexC = (indexC + 1) % 4;
            if (mirrorTransC != null) mirrorTransC.localRotation = Quaternion.Euler(0, 0, angles[indexC]);
        }

        UpdateBeamVisuals();
        CheckClearCondition();
    }

    private void UpdateBeamVisuals()
    {
        if (beamSegment1 != null) beamSegment1.color = colBeamOn;

        bool aConnected = (indexA == targetA);
        if (beamSegment2 != null) beamSegment2.color = aConnected ? colBeamOn : colBeamOff;

        bool bConnected = aConnected && (indexB == targetB);
        if (beamSegment3 != null) beamSegment3.color = bConnected ? colBeamOn : colBeamOff;

        bool cConnected = bConnected && (indexC == targetC);
        if (beamSegment4 != null) beamSegment4.color = cConnected ? colBeamOn : colBeamOff;

        if (receptorGlow != null) receptorGlow.color = cConnected ? colBeamOn : colBeamOff;
    }

    private void CheckClearCondition()
    {
        if (indexA == targetA && indexB == targetB && indexC == targetC)
        {
            isCleared = true;
            if (successNotice != null) successNotice.SetActive(true);

            // 사자의 서 4번 단서 자동 해금
            if (AnubisNotebookManager.Instance != null)
            {
                AnubisNotebookManager.Instance.UnlockClue(3,
                    "태양의 황금(A)은 가장 비천한 흙의 단지(C) 3개의 무게와 정확히 같도다. (A = 3C)");
            }
        }
    }

    public void ResetPuzzle()
    {
        indexA = 0;
        indexB = 0;
        indexC = 0;
        isCleared = false;

        if (mirrorTransA != null) mirrorTransA.localRotation = Quaternion.Euler(0, 0, angles[0]);
        if (mirrorTransB != null) mirrorTransB.localRotation = Quaternion.Euler(0, 0, angles[0]);
        if (mirrorTransC != null) mirrorTransC.localRotation = Quaternion.Euler(0, 0, angles[0]);

        if (successNotice != null) successNotice.SetActive(false);
        UpdateBeamVisuals();
    }
}