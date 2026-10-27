using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 게임 전체 소리 (효과음 + 배경음악). 씬에 아무것도 안 놓아도 처음 부를 때 알아서 생김 (씬이 바뀌어도 유지)
// 소리 파일: Assets/Resources/Audio/<이름>.wav  (이름만으로 불러옴, Inspector 연결 필요 없음)
//
//   GameAudio.Play("sfx_rumble");                      // 한 번
//   GameAudio.PlayRandom("sfx_step_sand_", 3, 0.5f);   // _1 ~ _3 중 하나 (발소리)
//   GameAudio.PlayThrottled("sfx_brush", 0.24f);       // 너무 자주 겹치지 않게 (매 프레임 불러도 됨)
//   GameAudio.Blip("sfx_blip_mid");                    // 대사 삐리리 (글자 나올 때)
//   GameAudio.Music("bgm_ra_temple_day");              // 배경음악 (2초 동안 바꿔 끼움)
//   CineAudio.Play(GameAudio.Or(rumbleSfx, "sfx_rumble")); // Inspector 칸이 비었으면 기본 소리
//
// 소리는 전부 직접 만든 합성음(저작권 없음). 나중에 더 좋은 소리가 오면 같은 이름 파일로 덮어쓰면 끝
public class GameAudio : MonoBehaviour
{
    private const string Folder = "Audio/";
    private const int PoolSize = 10;

    public static float SfxVolume = 0.4f;
    public static float MusicVolume = 0.5f;

    private static GameAudio instance;
    private static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
    private static readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();
    private static readonly HashSet<string> missing = new HashSet<string>();

    private readonly List<AudioSource> pool = new List<AudioSource>();
    private AudioSource musicA, musicB;
    private string currentMusic;
    private Coroutine fading;
    private int next;

    private static GameAudio Instance
    {
        get
        {
            if (instance != null) return instance;
            var go = new GameObject("GameAudio");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<GameAudio>();
            instance.Build();
            return instance;
        }
    }

    // ================= 불러오기 =================
    public static AudioClip Clip(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (cache.TryGetValue(name, out AudioClip c) && c != null) return c;
        c = Resources.Load<AudioClip>(Folder + name);
        if (c == null && missing.Add(name)) Debug.LogWarning($"[GameAudio] 소리 파일 없음: Resources/{Folder}{name}");
        cache[name] = c;
        return c;
    }

    // Inspector 칸에 넣은 소리가 있으면 그것, 없으면 이름으로 찾은 기본 소리
    public static AudioClip Or(AudioClip clip, string fallbackName)
    {
        return clip != null ? clip : Clip(fallbackName);
    }

    // ================= 효과음 =================
    public static void Play(string name, float volume = 1f, float pitch = 1f)
    {
        try
        {
            AudioClip clip = Clip(name);
            if (clip == null) return;
            AudioSource s = Instance.NextSource();
            s.pitch = pitch;
            s.PlayOneShot(clip, volume * SfxVolume);
            lastPlayed[name] = Time.unscaledTime;
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameAudio] 효과음 오류 ({name}): {e}");
        }
    }

    public static void PlayRandom(string prefix, int count, float volume = 1f, float pitchJitter = 0.08f)
    {
        int pick = UnityEngine.Random.Range(1, count + 1);
        Play(prefix + pick, volume, 1f + UnityEngine.Random.Range(-pitchJitter, pitchJitter));
    }

    public static void PlayThrottled(string name, float minGap, float volume = 1f)
    {
        if (lastPlayed.TryGetValue(name, out float last) && Time.unscaledTime - last < minGap) return;
        Play(name, volume, 1f + UnityEngine.Random.Range(-0.06f, 0.06f));
    }

    // 대사 소리: 글자 하나 나올 때마다 부르면 0.05초 간격으로 삐리리 (음 높이 살짝 흔들림)
    public static void Blip(string voice, float volume = 0.35f)
    {
        if (lastPlayed.TryGetValue("blip", out float last) && Time.unscaledTime - last < 0.05f) return;
        lastPlayed["blip"] = Time.unscaledTime;
        Play(voice, volume, 1f + UnityEngine.Random.Range(-0.07f, 0.07f));
    }

    // ================= 배경음악 =================
    public static void Music(string name, float fadeSeconds = 2f)
    {
        try
        {
            GameAudio a = Instance;
            if (a.currentMusic == name) return;
            a.currentMusic = name;
            if (a.fading != null) a.StopCoroutine(a.fading);
            a.fading = a.StartCoroutine(a.CrossFade(Clip(name), fadeSeconds));
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameAudio] 배경음악 오류 ({name}): {e}");
        }
    }

    public static void StopMusic(float fadeSeconds = 1.5f) => Music(null, fadeSeconds);

    private IEnumerator CrossFade(AudioClip clip, float seconds)
    {
        AudioSource from = musicA, to = musicB;
        musicA = to;
        musicB = from;
        to.clip = clip;
        to.volume = 0f;
        if (clip != null) to.Play();

        float startFrom = from.volume;
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            float k = t / Mathf.Max(0.01f, seconds);
            from.volume = Mathf.Lerp(startFrom, 0f, k);
            to.volume = clip != null ? Mathf.Lerp(0f, MusicVolume, k) : 0f;
            yield return null;
        }
        from.Stop();
        to.volume = clip != null ? MusicVolume : 0f;
        fading = null;
    }

    // ================= 준비 =================
    private void Build()
    {
        for (int i = 0; i < PoolSize; i++) pool.Add(NewSource(false));
        musicA = NewSource(true);
        musicB = NewSource(true);
    }

    private AudioSource NewSource(bool music)
    {
        AudioSource s = gameObject.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.loop = music;
        s.spatialBlend = 0f;
        return s;
    }

    private AudioSource NextSource()
    {
        AudioSource s = pool[next];
        next = (next + 1) % pool.Count;
        return s;
    }
}
