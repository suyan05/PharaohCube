using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// 연출(컷신) 공용 도구 - 쯔꾸르식 이벤트를 코루틴 한 줄씩 쓰기 위한 것
//
//   Cine.Begin(true);                                 // 조작 잠금 + 위아래 검은 바 + (ESC 길게 스킵 허용)
//   yield return Cine.Say("나", "후우...");           // 대사 (끝날 때까지 기다림)
//   yield return Cine.Wait(0.5f);                     // 잠깐 쉬기
//   yield return CineActor.WalkTo(player, 목표, 2f);  // 걷기 (걷기 애니메이션 자동)
//   CineCamera.Focus(위치); CineCamera.Zoom(4f);      // 카메라
//   Cine.End();                                       // 원래대로
//
// 스킵: Begin(true)일 때 ESC를 1초 누르면 Cine.Skipping = true
//   -> Wait / Say / WalkTo 등이 바로 끝나서 남은 연출이 순식간에 지나감
// 씬이 바뀌면 상태가 자동으로 초기화됨
public static class Cine
{
    private static readonly object LockKey = new object();

    public static bool Running { get; private set; }
    public static bool Skipping => CineSkip.Requested;

    // 연출이 끝날 때 (목표 문구 다시 띄우기 등)
    public static event Action Ended;

    // ================= 시작 / 끝 =================
    public static void Begin(bool allowSkip)
    {
        try
        {
            Running = true;
            CineFX.MatchPlayer();
            PlayerLock.Lock(LockKey);
            ObjectiveBoxUI.Hide();
            LetterboxUI.Show(true);
            CineSkip.Arm(allowSkip);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Cine] 연출 시작 오류: {e}");
        }
    }

    public static void End()
    {
        try
        {
            LetterboxUI.Show(false);
            CineSkip.Arm(false);
            CineCamera.Release();
            PlayerLock.Unlock(LockKey);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Cine] 연출 끝내기 오류: {e}");
        }
        Running = false;
        Ended?.Invoke();
    }

    // ================= 기다리기 / 대사 =================
    public static IEnumerator Wait(float seconds)
    {
        float t = 0f;
        while (t < seconds && !Skipping)
        {
            t += Time.deltaTime;
            yield return null;
        }
    }

    public static IEnumerator Say(string speaker, params string[] texts)
    {
        if (texts == null || texts.Length == 0) yield break;

        var lines = new DialogueLine[texts.Length];
        for (int i = 0; i < texts.Length; i++) lines[i] = new DialogueLine(speaker, texts[i]);
        yield return Say(lines);
    }

    public static IEnumerator Say(DialogueLine[] lines)
    {
        if (Skipping || lines == null || lines.Length == 0) yield break;

        bool closed = false;
        DialogueBox.Play(lines, () => closed = true);
        while (!closed && DialogueBox.IsOpen)
        {
            if (Skipping)
            {
                DialogueBox.ForceClose();
                yield break;
            }
            yield return null;
        }
    }

    // 기다리지 않고 따로 돌리기 (걸으면서 페이드, 걷는 동안 먼지 등)
    public static Coroutine Run(IEnumerator routine)
    {
        return routine != null ? CineRunner.Instance.StartCoroutine(routine) : null;
    }

    // ================= 씬 바뀔 때 초기화 =================
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Running = false;
        CineSkip.Arm(false);
    }
}
