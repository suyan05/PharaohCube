using System;
using UnityEngine;

// 치트키: F12 = 다음 번호 씬으로 바로 이동 (시연 때 빠르게 넘기기용)
// - 씬에 붙일 필요 없음: 게임이 시작되면 알아서 하나 만들어지고 씬이 바뀌어도 유지됨
// - 정식 빌드에서 끄고 싶으면 아래 AllowInBuild를 false로
public class SceneSkipCheat : MonoBehaviour
{
    // 시연용이라 빌드에서도 켜둠. 최종 제출 빌드에서는 false로 바꾸기
    private const bool AllowInBuild = true;

    private const KeyCode SkipKey = KeyCode.F12;

    // 키를 꾹 누르거나 연타해서 씬이 두 번 넘어가는 것 방지 (초)
    private const float Cooldown = 1f;

    private static SceneSkipCheat instance;
    private float nextAllowedTime;

    // 첫 씬이 열리기 전에 자동으로 생성
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateOnStart()
    {
        if (!Application.isEditor && !Debug.isDebugBuild && !AllowInBuild) return;
        if (instance != null) return;

        var go = new GameObject("SceneSkipCheat");
        instance = go.AddComponent<SceneSkipCheat>();
        DontDestroyOnLoad(go);
    }

    private void Update()
    {
        if (!Input.GetKeyDown(SkipKey)) return;
        if (Time.unscaledTime < nextAllowedTime) return;

        try
        {
            nextAllowedTime = Time.unscaledTime + Cooldown;
            Debug.Log("[Cheat] F12 -> 다음 씬으로 건너뜀");
            SceneFlow.LoadNext();
        }
        catch (Exception e)
        {
            Debug.LogError($"[Cheat] 씬 건너뛰기 오류: {e}");
        }
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}