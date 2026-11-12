using System.Collections;
using UnityEngine;

// 허브의 큰 문 "소베크의 입" (E: 악어 부적 넣기 -> 메인 씬)
// - 부적 4개(서브 보상)가 모두 있어야 열림. 넣으면 인벤토리에서 사라지고 F_SOBEK_DOOR_OPEN 저장
// - 한 번 연 뒤에는 E만 누르면 바로 메인 씬으로
// 붙이는 곳: 큰 문 오브젝트 (BoxCollider2D가 길을 막고 E 대상이 됨)
[RequireComponent(typeof(Collider2D))]
public class SobekHubDoor : MonoBehaviour, IInteractable
{
    [Header("필요한 부적 (서브 S1~S4 보상)")]
    [SerializeField] private string[] requiredItems =
    {
        SobekIds.AmuletRotate, SobekIds.AmuletLift, SobekIds.AmuletWater, SobekIds.AmuletMirror
    };

    [Header("이동")]
    [SerializeField] private string mainSceneName = SobekIds.MainScene;
    [Range(0.1f, 3f)] [SerializeField] private float fadeTime = 0.8f;

    [Header("화면 흔들림")]
    [SerializeField] private CameraFollow2D cameraFollow;

    private bool loading;

    private void Start()
    {
        if (cameraFollow == null && Camera.main != null) cameraFollow = Camera.main.GetComponent<CameraFollow2D>();
    }

    private static bool HasFlag(string f) => GameManager.Instance != null && GameManager.Instance.HasFlag(f);
    private static bool Has(string id) => ItemInventory.Instance != null && ItemInventory.Instance.Has(id);

    // 부적이 인벤토리에 있거나, 그 부적을 주는 서브 퍼즐을 이미 풀었으면 가진 것으로 셈
    private static bool HasAmulet(string id)
    {
        if (Has(id)) return true;
        int i = System.Array.IndexOf(SobekIds.Amulets, id);
        return i >= 0 && HasFlag(SobekIds.AmuletClearFlags[i]);
    }

    private int CountHave()
    {
        int n = 0;
        foreach (string id in requiredItems) if (HasAmulet(id)) n++;
        return n;
    }

    public string GetPrompt()
    {
        if (loading) return "";
        if (HasFlag(SobekIds.FlagMainDone)) return "물이 빠진 소베크의 입";
        if (HasFlag(SobekIds.FlagDoorOpen)) return "E: 셰데트 신전으로 들어가기";
        return CountHave() == requiredItems.Length ? "E: 악어 부적 4개 넣기" : $"E: 큰 문 살펴보기 (부적 {CountHave()}/{requiredItems.Length})";
    }

    public void Interact(GameObject player)
    {
        if (loading || HasFlag(SobekIds.FlagMainDone)) return;

        if (!HasFlag(SobekIds.FlagDoorOpen))
        {
            int have = CountHave();
            if (have < requiredItems.Length)
            {
                AnubisToast.Show($"악어 입 모양의 큰 문. 부적을 끼우는 홈이 네 개 있다. ({have}/{requiredItems.Length})", 3f);
                return;
            }
            if (ItemInventory.Instance != null) foreach (string id in requiredItems) if (Has(id)) ItemInventory.Instance.Consume(id);
            if (GameManager.Instance != null) GameManager.Instance.SetFlag(SobekIds.FlagDoorOpen);
            if (cameraFollow != null) cameraFollow.Shake(0.8f, 0.15f);
            AnubisToast.Show("부적 네 개가 빛나며 소베크의 입이 열린다...", 2.5f);
            Debug.Log("[SobekHubDoor] 부적 4개 넣음 -> 문 열림", this);
        }
        StartCoroutine(LoadMain());
    }

    private IEnumerator LoadMain()
    {
        loading = true;
        PlayerLock.Lock(this);
        yield return new WaitForSeconds(0.6f);
        yield return ScreenFader.FadeOut(fadeTime);
        PlayerLock.Unlock(this);
        SceneFlow.LoadByNameOrNext(mainSceneName);
    }

    private void OnDestroy() => PlayerLock.Unlock(this);

    [ContextMenu("테스트: 부적 4개 받기")]
    private void TestGrant()
    {
        if (!Application.isPlaying || ItemInventory.Instance == null) return;
        foreach (string id in requiredItems) ItemInventory.Instance.Grant(id);
    }
}
