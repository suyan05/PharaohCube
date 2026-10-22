using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 바스테트 걷는 퍼즐 공용 부품
// ================= 퍼즐 공통 =================
public abstract class BastetWalkPuzzleBase : PuzzleBase
{
    [Header("횃불 처벌")]
    [Tooltip("틀리면 플레이어 횃불을 끔")]
    [SerializeField] protected bool extinguishOnFail = true;

    [Header("[추가] 난이도 완화 (난이도 검토 권장값)")]
    [Tooltip("이 퍼즐에서 처음 틀렸을 때는 횃불을 끄지 않고 경고만")]
    [SerializeField] protected bool firstFailWarningOnly = false; // [수정] 기본은 틀리면 바로 꺼짐
    [TextArea(1, 2)]
    [SerializeField] protected string firstFailWarning = "석상이 하악거린다! 다음에 틀리면 횃불을 빼앗길 것 같다.";
    [Tooltip("순서를 보고 입력하는 동안 횃불이 타는 시간을 멈춤")]
    [SerializeField] protected bool pauseTorchBurnWhileSolving = true;

    private int punishCount;   // 이 퍼즐에서 처벌받은 횟수

    [Header("알림")]
    [Range(1f, 6f)] [SerializeField] protected float toastTime = 3f;

    // 큰 석상(시작) 안내 문구
    public abstract string StarterPrompt { get; }

    // 큰 석상에서 E: 순서 보여 주기 (처음 또는 다시)
    public abstract void Begin();

    // PuzzleBase.Open은 시작과 같게
    public override void Open() => Begin();

    protected void Toast(string msg)
    {
        if (!string.IsNullOrEmpty(msg)) AnubisToast.Show(msg, toastTime);
    }

    // 틀렸을 때 처벌 (횃불 끄기)
    protected void PunishTorch(string reason)
    {
        punishCount++;
        // [추가] 첫 실수는 경고만
        if (firstFailWarningOnly && punishCount == 1)
        {
            Toast(firstFailWarning);
            Debug.Log($"[{name}] 첫 실수: 경고만 (횃불 유지)", this);
            return;
        }
        if (!extinguishOnFail) { Toast(reason); return; }
        if (!TorchLink.Extinguish(reason)) Toast(reason); // 이미 꺼져 있으면 문구만
    }

    // [추가] 퍼즐 진행 중 표시: 켜져 있는 동안 횃불 타는 시간 멈춤
    protected void SetSolving(bool solving)
    {
        TorchLink.SetBurnPaused(this, solving && pauseTorchBurnWhileSolving);
    }

    // [추가] 오브젝트가 꺼지거나 씬이 바뀌면 멈춤 요청 해제
    protected virtual void OnDisable() => SetSolving(false);
}
