using System;
using System.Collections;
using UnityEngine;

// 프롤로그 1 발굴 현장 진행 담당 (기획서 2.1)
// - 시작: Intro(DigIntroCutscene)가 있으면 오프닝 연출, 없으면 검은 화면 -> 1초 페이드 인
// - 목표 문구: 일지 -> 붓 -> 모래 더미 순서로 플래그를 보고 바뀜
//   F_PRO_OBJ_JOURNAL_DONE (일지 읽음), F_PRO_OBJ_CRATE_DONE (붓 얻음)
// - 연출(Cine)이 끝날 때마다 목표 문구를 다시 띄움 (연출 중에는 숨겨짐)
// - 스카라브 이후(붕괴)는 SandPileGroup + DigCollapseSequence가 맡음
public class DigSiteDirector : MonoBehaviour
{
    [Header("시작")]
    [SerializeField] private DigIntroCutscene intro;
    [SerializeField, Min(0f)] private float fadeInSeconds = 1f;

    [Header("플래그 (Inspectable이 자동으로 켜는 F_<Object Id>_DONE)")]
    [SerializeField] private string journalFlag = "F_PRO_OBJ_JOURNAL_DONE";
    [SerializeField] private string crateFlag = "F_PRO_OBJ_CRATE_DONE";
    [SerializeField] private string scarabFlag = "F_PRO_SCARAB_FOUND";

    [Header("목표 문구")]
    [SerializeField] private string objectiveJournal = "발굴 일지를 확인하자";
    [SerializeField] private string objectiveBrush = "붓을 챙겨 발굴 구역으로 가자";
    [Tooltip("기획서에 문구가 없어서 임시로 넣음")]
    [SerializeField] private string objectiveSand = "B구역의 모래 더미를 붓으로 털어보자";

    private bool subscribed;
    private bool started;

    private void Awake()
    {
        // 첫 화면이 그려지기 전에 검게 (번쩍임 방지)
        ScreenFader.SetBlack();
    }

    private IEnumerator Start()
    {
        if (intro != null) yield return intro.Play();
        else yield return PlainFadeIn();

        Subscribe();
        started = true;
        RefreshObjective();
    }

    private IEnumerator PlainFadeIn()
    {
        PlayerLock.Lock(this);
        yield return ScreenFader.FadeIn(fadeInSeconds);
        PlayerLock.Unlock(this);
    }

    private void Subscribe()
    {
        Cine.Ended += RefreshObjective;
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[DigSiteDirector] GameManager가 없음 -> Managers 프리팹을 씬에 놓았는지 확인");
            return;
        }
        GameManager.Instance.OnFlagChanged += HandleFlag;
        subscribed = true;
    }

    private void OnDestroy()
    {
        Cine.Ended -= RefreshObjective;
        if (subscribed && GameManager.Instance != null) GameManager.Instance.OnFlagChanged -= HandleFlag;
        PlayerLock.Unlock(this);
    }

    private void HandleFlag(string flag)
    {
        if (flag == journalFlag || flag == crateFlag) RefreshObjective();
    }

    // 붓을 먼저 얻어도(일지 안 읽고) 맞는 문구가 나오도록 "가장 앞선 단계" 기준
    private void RefreshObjective()
    {
        try
        {
            if (!started || Cine.Running || HasFlag(scarabFlag)) return;

            if (HasFlag(crateFlag)) ObjectiveBoxUI.Show(objectiveSand);
            else if (HasFlag(journalFlag)) ObjectiveBoxUI.Show(objectiveBrush);
            else ObjectiveBoxUI.Show(objectiveJournal);
        }
        catch (Exception e)
        {
            Debug.LogError($"[DigSiteDirector] 목표 갱신 오류: {e}");
        }
    }

    private static bool HasFlag(string flag)
    {
        return GameManager.Instance != null && GameManager.Instance.HasFlag(flag);
    }
}
