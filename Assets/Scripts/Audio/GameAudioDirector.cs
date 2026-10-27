using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// 씬마다 자동으로 소리 깔기 (씬에 아무것도 안 놓아도 됨)
// - 배경음악: 발굴 현장 = 사막 테마 / 스핑크스의 꿈 = 몽환 테마 / 중앙 홀, 라의 신전 = 신전 테마 (라는 밤이 되면 밤 테마로)
// - 소리를 듣는 귀(AudioListener)가 씬에 없으면 Main Camera에 붙임 (임시 중앙 홀 씬에 없었음)
// - 플레이어 발소리(FootstepAudio) 붙이기
// - 아이템 얻으면 반짝 소리 / 라의 방 문이 열리면 쿠르릉 ("[Door] ... 열렸다" 알림을 듣고)
public class GameAudioDirector : MonoBehaviour
{
    private static GameAudioDirector instance;
    private ItemInventory subscribedInventory;
    private GameManager subscribedGm;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        if (instance != null) return;
        var go = new GameObject("GameAudioDirector");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<GameAudioDirector>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleScene;
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleScene;
        Application.logMessageReceived -= HandleLog;
        Unsubscribe();
    }

    private void HandleScene(Scene scene, LoadSceneMode mode)
    {
        try
        {
            EnsureListener();
            PlaySceneMusic(scene.name);
            AddFootsteps(scene.name);
            Invoke(nameof(Subscribe), 0.2f); // Managers가 먼저 준비되게
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameAudioDirector] 씬 소리 준비 오류: {e}");
        }
    }

    // ================= 배경음악 =================
    private static void PlaySceneMusic(string scene)
    {
        if (scene.Contains("Prologue_Dig")) GameAudio.Music("bgm_prologue_desert");
        else if (scene.Contains("Sphinx")) GameAudio.Music("bgm_sphinx_dream");
        else if (scene.Contains("Ra_Tilemap")) GameAudio.Music(RaPhaseState.IsNight ? "bgm_ra_temple_night" : "bgm_ra_temple_day");
        else if (scene.Contains("CentralRoom") || scene.Contains("CentralHall")) GameAudio.Music("bgm_ra_temple_day");
    }

    private void HandleFlag(string flag)
    {
        if (flag == RaPhaseState.NightFlag && SceneManager.GetActiveScene().name.Contains("Ra_Tilemap"))
        {
            GameAudio.Music("bgm_ra_temple_night", 4f);
        }
    }

    // ================= 귀, 발소리 =================
    private static void EnsureListener()
    {
        if (FindFirstObjectByType<AudioListener>() != null) return;
        Camera cam = Camera.main;
        if (cam != null) cam.gameObject.AddComponent<AudioListener>();
    }

    private static void AddFootsteps(string scene)
    {
        PlayerController2D pc = FindFirstObjectByType<PlayerController2D>();
        if (pc == null || pc.GetComponent<FootstepAudio>() != null) return;
        FootstepAudio f = pc.gameObject.AddComponent<FootstepAudio>();
        f.Surface = scene.Contains("Prologue") ? "sand" : "stone";
    }

    // ================= 아이템, 플래그 =================
    private void Subscribe()
    {
        Unsubscribe();
        if (ItemInventory.Instance != null)
        {
            subscribedInventory = ItemInventory.Instance;
            subscribedInventory.OnItemGranted += HandleItem;
        }
        if (GameManager.Instance != null)
        {
            subscribedGm = GameManager.Instance;
            subscribedGm.OnFlagChanged += HandleFlag;
        }
    }

    private void Unsubscribe()
    {
        if (subscribedInventory != null) subscribedInventory.OnItemGranted -= HandleItem;
        if (subscribedGm != null) subscribedGm.OnFlagChanged -= HandleFlag;
        subscribedInventory = null;
        subscribedGm = null;
    }

    private static void HandleItem(string itemId)
    {
        GameAudio.Play("sfx_item_get", 0.8f);
    }

    // 라의 방 구역 해금 알림 "[Door] ... 문이 열렸다" -> 쿠르릉
    private static void HandleLog(string msg, string stack, LogType type)
    {
        if (type != LogType.Log || !msg.StartsWith("[Door] ")) return;
        if (msg.Contains("열렸")) GameAudio.Play("sfx_rumble", 0.8f);
    }
}
