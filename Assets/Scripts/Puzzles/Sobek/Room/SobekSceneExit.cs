using System.Collections;
using UnityEngine;

// 밟으면 다음 씬으로 (허브 전실 남쪽 출구 -> 중앙 홀). 물이 빠진 뒤에 SobekHubState가 켬
// 붙이는 곳: 출구 오브젝트 (트리거 콜라이더). SobekHubState가 물이 빠진 뒤에 켬
// [추가] 처음 떠날 때 소베크의 남은 이야기(SobekFarewell)를 먼저 보여 줌. 이빨 없이 처음 떠나면 한 번 붙잡음
[RequireComponent(typeof(Collider2D))]
public class SobekSceneExit : MonoBehaviour
{
    [SerializeField] private string nextSceneName = SobekIds.AnubisScene;
    [TextArea(1, 2)]
    [SerializeField] private string message = "심판의 신전으로 내려간다...";
    [Range(0.1f, 3f)] [SerializeField] private float fadeTime = 1.2f;
    [Tooltip("[추가] 처음 떠날 때 소베크 남은 이야기 보기")]
    [SerializeField] private bool farewellFirst = true;

    private bool used;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (used || other.GetComponentInParent<PlayerController2D>() == null) return;
        if (Cine.Running || SobekStory.Busy) return;
        used = true;
        if (farewellFirst && (SobekFarewell.Pending || SobekFarewell.ShouldWarn))
        {
            bool toHall = nextSceneName == SobekIds.CentralHallScene;
            StartCoroutine(SobekFarewell.Play(toHall, stay =>
            {
                if (stay) { used = false; Debug.Log("[SobekSceneExit] 이빨 힌트 -> 이번에는 떠나지 않음", this); }
                else StartCoroutine(Go());
            }));
            return;
        }
        StartCoroutine(Go());
    }

    // [추가] 붙잡혔을 때: 출구에서 한 번 나갔다가 다시 들어오면 떠남
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController2D>() == null) return;
        if (!used) Debug.Log("[SobekSceneExit] 출구에서 벗어남 (다시 들어오면 떠남)", this);
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
