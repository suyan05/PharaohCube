using System;
using System.Collections;
using UnityEngine;

// 신전 북쪽 문 -> 중앙 홀로 돌아가기 (라의 방 NorthDoor, 호루스의 방 HorusNorthDoor가 부름)
// [수정] 예전 "시현 데모 종료" 화면(가짜 중앙방 + 형판 장착 버튼)은 지움
//   -> 이제 라의 방은 RaFinale(라 일러스트 엔딩)이 끝나면 바로 중앙 홀로 감
//   -> 이 문은 엔딩 뒤 라의 방에 다시 들어왔을 때 등 예비용 (누르면 암전 -> 중앙 홀)
//   -> 큐브에 형판이 박히는 연출은 중앙 홀(HallTempDirector)에서 함
// 형판 등록(F_RA_PLATE_GIVEN -> PlayerInventory 0번)과 실제 큐브 장착(CubeFaceManager)은 그대로 유지
public class DemoEnding : MonoBehaviour
{
    // 라의 방을 끝내고 나갔다는 표시 (이름은 예전 그대로 둠 - 다른 곳이 이 이름을 봄)
    public const string DemoEndFlag = "F_RA_DEMO_END";

    // 중앙방에 들어간 순간 (RaObjectiveGuide 마지막 단계가 이 플래그로 끝남 -> 목표 문구 숨김)
    public const string CentralEnteredFlag = "F_RA_CENTRAL_ENTERED";

    // 중앙 홀 씬 이름 (아래 칸이 비어 있을 때)
    public const string DefaultHallScene = "CentralRoom_Temp";

    [Header("연결 (예전 데모 화면 - 이제 안 씀, 비워도 됨)")]
    [SerializeField] private GameObject endingPanel;

    [Header("기존 큐브 시스템 (비어 있어도 진행됨)")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private CubeFaceManager cubeFaceManager;

    [Header("형판 등록 (이 플래그가 켜지면 PlayerInventory에 0번 = 라 형판 등록)")]
    [SerializeField] private string plateGivenFlag = "F_RA_PLATE_GIVEN";

    [Header("갈 곳")]
    [Tooltip("비우면 중앙 홀(CentralRoom_Temp). 이름을 적으면 그 씬으로")]
    // [수정] 예전 칸(nextSceneName)은 다음 신전(바스테트)으로 바로 가게 적혀 있어서 새 칸으로 바꿈 -> 무조건 중앙 홀을 거침
    [SerializeField] private string hallSceneName = "";

    [Header("연출")]
    [SerializeField] private float fadeTime = 0.8f;

    private bool playing;
    private bool plateRegistered;
    private bool subscribed;

    private void Start()
    {
        try
        {
            if (endingPanel != null) endingPanel.SetActive(false);
            SubscribePlateFlag();
        }
        catch (Exception e)
        {
            Debug.LogError($"[Ending] 초기화 오류: {e}");
        }
    }

    private void OnDestroy()
    {
        if (subscribed && GameManager.Instance != null) GameManager.Instance.OnFlagChanged -= HandleFlagChanged;
    }

    // ================= 형판 등록 (성소에서 받은 순간) =================
    private void SubscribePlateFlag()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogWarning("[Ending] GameManager가 없어서 형판 등록을 못 받음");
            return;
        }

        gm.OnFlagChanged += HandleFlagChanged;
        subscribed = true;
        if (gm.HasFlag(plateGivenFlag)) RegisterPlate(); // 이미 받은 상태로 씬에 들어온 경우
    }

    private void HandleFlagChanged(string flag)
    {
        if (flag == plateGivenFlag) RegisterPlate();
    }

    private void RegisterPlate()
    {
        if (plateRegistered) return;
        plateRegistered = true;

        if (playerInventory != null)
        {
            playerInventory.AddPlate(0); // 0번 = 라 (hasPlate[0] = true)
            Debug.Log("[Ending] 라 형판 등록 (PlayerInventory 0번)");
        }
        else
        {
            Debug.Log("[Ending] PlayerInventory 미연결 - 형판은 ItemInventory(PLATE_RA)에만 기록됨");
        }
    }

    // ================= 북문에서 호출 =================
    public void Play(GameObject player)
    {
        if (playing) return;
        playing = true;

        var controller = player.GetComponent<PlayerController2D>();
        var interaction = player.GetComponent<InteractionSystem>();
        var rb = player.GetComponent<Rigidbody2D>();
        if (controller != null) controller.enabled = false;
        if (interaction != null) interaction.enabled = false; // 암전 중에 E로 북문이 다시 눌리지 않게
        if (rb != null) rb.linearVelocity = Vector2.zero;

        GameManager gm = GameManager.Instance;
        if (gm != null && !gm.HasFlag(CentralEnteredFlag)) gm.SetFlag(CentralEnteredFlag);

        MountToCube();
        StartCoroutine(GoToHall());
    }

    // 기존 CubeFaceManager로 실제 장착 (에러가 나도 이동은 계속 진행)
    private void MountToCube()
    {
        if (cubeFaceManager == null)
        {
            Debug.Log("[Ending] CubeFaceManager 미연결 - 장착 건너뜀");
            return;
        }

        try
        {
            if (cubeFaceManager.currentFace != 0)
            {
                Debug.Log($"[Ending] 현재 앞면이 {cubeFaceManager.currentFace}번 - 0번(라)이 아님");
            }

            if (cubeFaceManager.CanEquipCurrentFace())
            {
                cubeFaceManager.EquipCurrentFace();
                Debug.Log("[Ending] 큐브 면 0(라) 장착 완료");
            }
            else
            {
                Debug.Log("[Ending] 장착 조건 미충족 (형판 없음 또는 이미 장착)");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Ending] 큐브 장착 중 오류, 이동은 계속: {e.Message}");
        }
    }

    // 암전 -> 중앙 홀
    private IEnumerator GoToHall()
    {
        GameAudio.Play("sfx_rumble", 0.7f);
        yield return ScreenFader.FadeOut(fadeTime);

        GameManager gm = GameManager.Instance;
        if (gm != null && !gm.HasFlag(DemoEndFlag)) gm.SetFlag(DemoEndFlag);

        try
        {
            string scene = string.IsNullOrEmpty(hallSceneName) ? DefaultHallScene : hallSceneName;
            Debug.Log($"[Ending] 중앙 홀로 이동: {scene}");
            SceneFlow.LoadByNameOrNext(scene);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Ending] 중앙 홀 이동 오류: {e}");
            playing = false;
        }
    }
}
