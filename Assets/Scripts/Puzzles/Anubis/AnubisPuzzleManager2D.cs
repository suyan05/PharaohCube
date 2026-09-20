using System;
using UnityEngine;
using TMPro;

public class AnubisPuzzleManager2D : MonoBehaviour
{
    [Header("저울 컴포넌트 연결")]
    [SerializeField] private RectTransform scaleBeam;
    [SerializeField] private AnubisUIScalePan leftPan;
    [SerializeField] private AnubisUIScalePan rightPan;

    [Header("회전 연출")]
    [SerializeField] private float maxTiltAngle = 15f;
    [SerializeField] private float tiltSpeed = 5f;

    [Header("UI 연결")]
    [SerializeField] private TMP_Text weightText;
    [SerializeField] private GameObject clearText;

    public event Action OnPuzzleCleared;
    private bool isCleared = false;

    private float LeftWeight => leftPan != null ? leftPan.TotalWeight : 0f;
    private float RightWeight => rightPan != null ? rightPan.TotalWeight : 0f;

    private void Start()
    {
        if (clearText != null) clearText.SetActive(false);
    }

    private void Update()
    {
        UpdateBeamTilt();
        UpdateUI();
        CheckBalanceCondition();
    }

    private void UpdateBeamTilt()
    {
        if (scaleBeam == null) return;

        float diff = RightWeight - LeftWeight;
        float targetZ = Mathf.Clamp(diff * -4f, -maxTiltAngle, maxTiltAngle);

        Quaternion targetRot = Quaternion.Euler(0f, 0f, targetZ);
        scaleBeam.localRotation = Quaternion.Lerp(scaleBeam.localRotation, targetRot, Time.deltaTime * tiltSpeed);
    }

    private void UpdateUI()
    {
        if (weightText != null)
        {
            weightText.text = $"[좌측 접시: {LeftWeight:F1} kg]  VS  [우측 접시: {RightWeight:F1} kg]";
        }
    }

    private void CheckBalanceCondition()
    {
        if (isCleared) return;

        if (LeftWeight > 0.01f && RightWeight > 0.01f)
        {
            if (Mathf.Abs(LeftWeight - RightWeight) < 0.01f)
            {
                isCleared = true;
                Debug.Log("<color=yellow>[Anubis 2D] 완벽한 수평 달성! 아누비스의 방 클리어!</color>");
                OnPuzzleCleared?.Invoke();

                if (clearText != null) clearText.SetActive(true);
            }
        }
    }
}