using UnityEngine;

// [추가] 배경음(BGM) / 효과음(SFX) 재생 - 씬이 바뀌어도 유지, 설정 창 음량을 따라감
// 사용법
//  - 배경음: SoundManager.PlayBgm(clip)  (씬마다 SceneBgm 컴포넌트를 두면 자동)
//  - 효과음: SoundManager.PlaySfx(clip)
// 클립이 비어 있으면 조용히 넘어감 (사운드가 오기 전에도 오류 없음)
public class SoundManager : MonoBehaviour
{
    private static SoundManager instance;

    private AudioSource bgmA, bgmB, sfx;
    private AudioSource activeBgm;           // 지금 들리는 배경음 소스
    private float fadeSpeedA, fadeSpeedB;    // 1초에 바뀌는 페이드 양
    private float mulA, mulB;                // 페이드 배율 (0~1)
    private float targetA, targetB;
    private float baseVolA = 1f, baseVolB = 1f;

    public static SoundManager Ensure()
    {
        if (instance != null) return instance;
        var go = new GameObject("SoundManager");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<SoundManager>();
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        bgmA = NewSource(true);
        bgmB = NewSource(true);
        sfx = NewSource(false);
        activeBgm = bgmA;
        GameSettings.AudioChanged += ApplyVolumes;
    }

    private void OnDestroy()
    {
        if (instance == this) GameSettings.AudioChanged -= ApplyVolumes;
    }

    private AudioSource NewSource(bool loop)
    {
        var s = gameObject.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.loop = loop;
        s.ignoreListenerPause = true;
        return s;
    }

    // ================= 배경음 =================
    public static void PlayBgm(AudioClip clip, float volume = 1f, float fadeSeconds = 1f)
    {
        if (clip == null) return;
        Ensure().CrossFade(clip, volume, fadeSeconds);
    }

    public static void StopBgm(float fadeSeconds = 1f)
    {
        if (instance == null) return;
        instance.FadeTo(instance.activeBgm, 0f, fadeSeconds);
    }

    private void CrossFade(AudioClip clip, float volume, float fade)
    {
        if (activeBgm.clip == clip && activeBgm.isPlaying) // 같은 곡이면 이어서
        {
            SetBase(activeBgm, volume);
            FadeTo(activeBgm, 1f, fade);
            return;
        }
        AudioSource next = activeBgm == bgmA ? bgmB : bgmA;
        FadeTo(activeBgm, 0f, fade);
        next.clip = clip;
        SetBase(next, volume);
        SetMul(next, 0f);
        next.Play();
        FadeTo(next, 1f, fade);
        activeBgm = next;
        Debug.Log($"[SoundManager] 배경음: {clip.name}");
    }

    // ================= 효과음 =================
    public static void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        SoundManager m = Ensure();
        m.sfx.PlayOneShot(clip, Mathf.Clamp01(volume) * GameSettings.Sfx);
    }

    // ================= 페이드 (시간 정지 중에도 진행) =================
    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        mulA = Step(bgmA, mulA, targetA, fadeSpeedA, dt);
        mulB = Step(bgmB, mulB, targetB, fadeSpeedB, dt);
    }

    private float Step(AudioSource s, float mul, float target, float speed, float dt)
    {
        if (Mathf.Approximately(mul, target)) return mul;
        mul = Mathf.MoveTowards(mul, target, speed * dt);
        s.volume = Base(s) * mul * GameSettings.Bgm;
        if (mul <= 0f && target <= 0f && s.isPlaying) s.Stop();
        return mul;
    }

    private void FadeTo(AudioSource s, float target, float seconds)
    {
        float speed = seconds > 0.01f ? 1f / seconds : 1000f;
        if (s == bgmA) { targetA = target; fadeSpeedA = speed; }
        else { targetB = target; fadeSpeedB = speed; }
    }

    private void SetMul(AudioSource s, float m) { if (s == bgmA) mulA = m; else mulB = m; s.volume = Base(s) * m * GameSettings.Bgm; }
    private void SetBase(AudioSource s, float v) { if (s == bgmA) baseVolA = Mathf.Clamp01(v); else baseVolB = Mathf.Clamp01(v); }
    private float Base(AudioSource s) => s == bgmA ? baseVolA : baseVolB;

    private void ApplyVolumes()
    {
        bgmA.volume = baseVolA * mulA * GameSettings.Bgm;
        bgmB.volume = baseVolB * mulB * GameSettings.Bgm;
    }
}
