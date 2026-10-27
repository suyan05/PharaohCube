using System;
using UnityEngine;

// "1" 오브젝트 = 라의 성소. E로 라와 대화
// 단계별 대사:
//   (1) 낮 시련 전          : 짧은 안내 (처음 한 번 듣기 전까지 "!" 표시 - 입구 비석 역할)
//   (2) 낮 P5 클리어 후     : "!" 표시 -> 시련 통과 대사 -> 끝나면 밤 시작 (F_RA_NIGHT_OPEN)
//   (3) 밤 진행 중          : 밤 힌트 대사 (처음 한 번 듣기 전까지 "!" 표시)
//   (4) 밤 P5 클리어 후     : "!" 표시 -> 마무리 대사 -> 끝나면 태양 형판 지급 (한 번만, 북쪽 문 열림)
//       [수정] 이제 (4)는 RaFinale(라 일러스트 엔딩)이 자동으로 함. 엔딩이 없거나 이미 끝난 뒤에만 예전 대사
public class RaShrine : MonoBehaviour, IInteractable
{
    private const string MessageTag = "[Door] ";

    private enum Stage { BeforeTrial, TrialPassed, Night, NightCleared }

    [Header("연결 (비우면 자동으로 찾음)")]
    [SerializeField] private RaDialogue dialogue;
    [SerializeField] private CameraFollow2D cameraFollow;

    [Header("\"!\" 말풍선 (UI_Prompt_New를 넣은 자식 SpriteRenderer)")]
    [SerializeField] private SpriteRenderer newBubble;
    [SerializeField] private float bobHeight = 0.08f;
    [SerializeField] private float bobSpeed = 3f;

    [Header("플래그")]
    [Tooltip("처음 대화를 들으면 켜짐 (켜지기 전까지 \"!\" 표시)")]
    [SerializeField] private string firstTalkedFlag = "F_RA_SHRINE_FIRST_TALKED";
    [SerializeField] private string dayClearFlag = "F_RA_P5_DONE";
    [Tooltip("밤 대사를 처음 들으면 켜짐 (켜지기 전까지 밤에도 \"!\" 표시)")]
    [SerializeField] private string nightTalkedFlag = "F_RA_SHRINE_NIGHT_TALKED";
    [SerializeField] private string nightClearFlag = "F_RA_NIGHT_P5_DONE";
    [SerializeField] private string endTalkedFlag = "F_RA_SHRINE_END_TALKED";

    [Header("밤 마무리 - 형판 지급")]
    [SerializeField] private string plateItemId = "PLATE_RA";
    [Tooltip("형판을 주면 켜짐 (북쪽 문 개방 조건, 두 번 주기 방지)")]
    [SerializeField] private string plateGivenFlag = "F_RA_PLATE_GIVEN";

    [Header("밤 전환 연출")]
    [SerializeField, TextArea] private string nightMessage = "신전에 밤이 내려앉았다. 비석의 글이 바뀐 것 같다.";
    [SerializeField] private float shakeDuration = 0.8f;
    [SerializeField] private float shakeStrength = 0.15f;

    [Header("대사 (1) 낮 시련 전")]
    [SerializeField]
    private RaDialogueLine[] beforeTrialLines =
    {
        new RaDialogueLine(RaPortrait.Default, "나를 안다 말하려거든, 먼저 나의 하루를 걸어 보아라."),
        new RaDialogueLine(RaPortrait.Default, "빛이 닿는 곳부터 걸어라. 빛은 문을 연다.")
    };

    [Header("대사 (2) 낮 P5 클리어 후 -> 밤 시작")]
    [SerializeField]
    private RaDialogueLine[] trialPassedLines =
    {
        new RaDialogueLine(RaPortrait.Glow, "나의 하루를 끝까지 걸었구나, 고고학자여."),
        new RaDialogueLine(RaPortrait.Default, "허나 시련은 여기까지다. 낮은 내 얼굴의 절반일 뿐."),
        new RaDialogueLine(RaPortrait.Glow, "이제 진정으로 우리를 들여다보거라. 밤의 나를.")
    };

    [Header("대사 (3) 밤 진행 중")]
    [SerializeField]
    private RaDialogueLine[] nightLines =
    {
        new RaDialogueLine(RaPortrait.Night, "밤의 나는 걷지 않는다. 비석은 뒤집힌 진실을 말할 것이다."),
        new RaDialogueLine(RaPortrait.Night, "낮에 배운 것을 그대로 믿지 마라.")
    };

    [Header("대사 (4) 밤 P5 클리어 후")]
    [SerializeField]
    private RaDialogueLine[] nightClearedLines =
    {
        new RaDialogueLine(RaPortrait.Glow, "저승의 강을 건너 다시 떠올랐구나."),
        new RaDialogueLine(RaPortrait.Glow, "나의 형판을 가져가라. 북쪽 문이 너를 기다린다.")
    };

    private Vector3 bubbleBasePos;

    // ================= 초기화 =================
    private void Start()
    {
        try
        {
            if (dialogue == null) dialogue = FindFirstObjectByType<RaDialogue>(FindObjectsInactive.Include);
            if (cameraFollow == null && Camera.main != null) cameraFollow = Camera.main.GetComponent<CameraFollow2D>();
            if (dialogue == null) Debug.LogError($"[{name}] 씬에 RaDialogue가 없음 -> RaCanvas 아래 Dialogue_Overlay에 RaDialogue 붙이기");

            if (newBubble != null) bubbleBasePos = newBubble.transform.localPosition;
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 라의 성소 초기화 오류: {e}");
        }
    }

    private void Update()
    {
        if (newBubble == null) return;

        bool show = ShouldShowBubble();
        newBubble.enabled = show;
        if (!show) return;

        float y = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        newBubble.transform.localPosition = bubbleBasePos + new Vector3(0f, y, 0f);
    }

    private bool ShouldShowBubble()
    {
        switch (CurrentStage())
        {
            case Stage.BeforeTrial: return !HasFlag(firstTalkedFlag);
            case Stage.TrialPassed: return true;
            case Stage.Night: return !HasFlag(nightTalkedFlag);
            case Stage.NightCleared: return !HasFlag(endTalkedFlag);
            default: return false;
        }
    }

    private Stage CurrentStage()
    {
        if (HasFlag(nightClearFlag)) return Stage.NightCleared;
        if (RaPhaseState.IsNight) return Stage.Night;
        if (HasFlag(dayClearFlag)) return Stage.TrialPassed;
        return Stage.BeforeTrial;
    }

    // ================= IInteractable =================
    public string GetPrompt()
    {
        return "E: 라의 성소 조사";
    }

    public void Interact(GameObject player)
    {
        try
        {
            if (dialogue == null || dialogue.IsPlaying) return;
            if (dialogue.ClosedFrame == Time.frameCount) return; // 대화를 닫은 E키로 다시 열리는 것 방지

            PlayForStage(CurrentStage());
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 라의 성소 상호작용 오류: {e}");
        }
    }

    private void PlayForStage(Stage stage)
    {
        switch (stage)
        {
            case Stage.TrialPassed: dialogue.Play(trialPassedLines, BeginNight); break;
            case Stage.Night: dialogue.Play(nightLines, MarkNightTalked); break;
            case Stage.NightCleared:
                if (!RaFinale.TryStart()) dialogue.Play(nightClearedLines, MarkEndTalked);
                break;
            default: dialogue.Play(beforeTrialLines, MarkFirstTalked); break;
        }
    }

    private void MarkFirstTalked()
    {
        SetFlagOnce(firstTalkedFlag);
    }

    private void MarkNightTalked()
    {
        SetFlagOnce(nightTalkedFlag);
    }

    private static void SetFlagOnce(string flag)
    {
        GameManager gm = GameManager.Instance;
        if (gm != null && !string.IsNullOrEmpty(flag) && !gm.HasFlag(flag)) gm.SetFlag(flag);
    }

    // ================= 밤 시작 =================
    private void BeginNight()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogError($"[{name}] GameManager가 없어서 밤으로 못 넘어감");
            return;
        }
        if (RaPhaseState.IsNight) return;

        if (cameraFollow != null) cameraFollow.Shake(shakeDuration, shakeStrength);
        if (!string.IsNullOrEmpty(nightMessage)) Debug.Log(MessageTag + nightMessage);

        gm.SetFlag(RaPhaseState.NightFlag); // 모든 라 퍼즐이 밤 데이터로 전환됨
    }

    // 마무리 대사가 끝나면: 형판 지급 -> 들었음 표시
    private void MarkEndTalked()
    {
        GivePlate();
        SetFlagOnce(endTalkedFlag);
    }

    private void GivePlate()
    {
        if (HasFlag(plateGivenFlag)) return;

        ItemInventory inventory = ItemInventory.Instance;
        if (inventory == null)
        {
            Debug.LogError($"[{name}] ItemInventory가 없어서 태양 형판을 못 줌");
            return;
        }

        inventory.Grant(plateItemId);
        Debug.Log("[노트 6] 태양의 형판을 큐브의 첫 면에 장착하라.");
        SetFlagOnce(plateGivenFlag); // 북쪽 문이 이 플래그를 보고 열림
    }

    private static bool HasFlag(string flag)
    {
        return !string.IsNullOrEmpty(flag) && GameManager.Instance != null && GameManager.Instance.HasFlag(flag);
    }
}