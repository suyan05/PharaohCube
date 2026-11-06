using System.Collections;
using UnityEngine;

// 밟으면 다음 씬으로 (허브 전실 남쪽 출구 -> 중앙 홀). 물이 빠진 뒤에 SobekHubState가 켬
// 붙이는 곳: 출구 오브젝트 (트리거 콜라이더). SobekHubState가 물이 빠진 뒤에 켬
[RequireComponent(typeof(Collider2D))]
public class SobekSceneExit : MonoBehaviour
{
    [SerializeField] private string nextSceneName = SobekIds.AnubisScene;
    [TextArea(1, 2)]
    [SerializeField] private string message = "심판의 신전으로 내려간다...";
    [Range(0.1f, 3f)] [SerializeField] private float fadeTime = 1.2f;

    private bool used;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (used || other.GetComponentInParent<PlayerController2D>() == null) return;
        used = true;
        StartCoroutine(Go());
    }

    private IEnumerator Go()
    {
        if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
            Debug.LogWarning($"[SobekSceneExit] '{nextSceneName}' 씬이 Scene List에 없음 -> 다음 번호 씬으로", this);
        PlayerLock.Lock(this);
        AnubisToast.Show(message, fadeTime + 1f);
        yield return ScreenFader.FadeOut(fadeTime);
        PlayerLock.Unlock(this);
        SceneFlow.LoadByNameOrNext(nextSceneName);
    }

    private void OnDestroy() => PlayerLock.Unlock(this);

#if UNITY_EDITOR
    public void EditorSetup(string scene, string msg = "") { nextSceneName = scene; if (!string.IsNullOrEmpty(msg)) message = msg; }
#endif
}
