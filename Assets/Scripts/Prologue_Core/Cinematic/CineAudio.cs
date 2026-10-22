using System;
using System.Collections;
using UnityEngine;

// 연출용 소리 (클립이 비어 있으면 조용히 넘어감 -> 사운드 오기 전에도 연출은 그대로 돌아감)
// - Play(클립, 볼륨): 한 번 (무전 치직, 땅울림, 반짝)
// - Loop(클립, 볼륨): 배경 소리 계속 (바람) / StopLoop(초): 서서히 끄기
public static class CineAudio
{
    private static AudioSource oneShot;
    private static AudioSource loop;

    public static void Play(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        try
        {
            Sources();
            oneShot.PlayOneShot(clip, volume);
        }
        catch (Exception e)
        {
            Debug.LogError($"[CineAudio] 효과음 오류: {e}");
        }
    }

    public static void Loop(AudioClip clip, float volume)
    {
        if (clip == null) return;
        try
        {
            Sources();
            loop.clip = clip;
            loop.volume = volume;
            if (!loop.isPlaying) loop.Play();
        }
        catch (Exception e)
        {
            Debug.LogError($"[CineAudio] 배경 소리 오류: {e}");
        }
    }

    public static void SetLoopVolume(float volume)
    {
        if (loop != null) loop.volume = volume;
    }

    public static IEnumerator StopLoop(float seconds)
    {
        if (loop == null || !loop.isPlaying) yield break;

        float start = loop.volume;
        float t = 0f;
        while (t < seconds && loop != null)
        {
            t += Time.unscaledDeltaTime;
            loop.volume = Mathf.Lerp(start, 0f, t / seconds);
            yield return null;
        }
        if (loop != null) loop.Stop();
    }

    // 씬이 바뀌면 CineRunner와 같이 사라지므로 null이면 다시 만듦
    private static void Sources()
    {
        if (oneShot != null && loop != null) return;

        GameObject host = CineRunner.Instance.gameObject;
        oneShot = host.AddComponent<AudioSource>();
        oneShot.playOnAwake = false;
        loop = host.AddComponent<AudioSource>();
        loop.playOnAwake = false;
        loop.loop = true;
    }
}
