using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 플레이어 조작 잠금 (여러 UI가 동시에 잠가도 안전하게)
// - 대화창, 획득 팝업, 연출이 각자 Lock(자기 자신) / Unlock(자기 자신)을 부름
// - 잠근 쪽이 하나라도 남아 있으면 계속 잠김 -> 대화 끝나자마자 팝업이 떠도 중간에 풀리지 않음
// - 잠기면 이동(PlayerController2D) + E키 조사(InteractionSystem)가 같이 꺼짐
// - 씬이 바뀌면 잠금 목록을 비움
public static class PlayerLock
{
    private static readonly HashSet<object> owners = new HashSet<object>();
    private static PlayerController2D controller;
    private static InteractionSystem interaction;
    private static Rigidbody2D body;

    public static bool IsLocked => owners.Count > 0;

    public static void Lock(object owner)
    {
        if (owner == null) return;
        owners.Add(owner);
        Apply();
    }

    public static void Unlock(object owner)
    {
        if (owner == null) return;
        owners.Remove(owner);
        Apply();
    }

    private static void Apply()
    {
        FindPlayer();
        if (controller == null) return;

        bool locked = owners.Count > 0;
        controller.enabled = !locked;
        if (interaction != null) interaction.enabled = !locked;
        if (locked && body != null) body.linearVelocity = Vector2.zero;
    }

    // 씬이 바뀌면 예전 플레이어는 사라지므로 다시 찾음 (Unity의 null 검사로 판단)
    private static void FindPlayer()
    {
        if (controller != null) return;

        controller = Object.FindAnyObjectByType<PlayerController2D>();
        if (controller == null) return;

        interaction = controller.GetComponent<InteractionSystem>();
        body = controller.GetComponent<Rigidbody2D>();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        owners.Clear();
        controller = null;
        interaction = null;
        body = null;
    }
}