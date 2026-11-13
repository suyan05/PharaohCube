using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// [추가] 바스테트 '이야기 비석' (퍼즐 단서가 아닌 신화·떡밥 비석, 3개)
// - 단서 비석(BastetClueStele)과 구분: 푸른빛, "!" 없음, 퍼즐 정답과 무관
// - 처음: 렌즈 해독(SteleDecodeUI, 라·프롤로그 공용) -> 해독 문장 -> (있으면) 반응 대사 1번
// - 두 번째부터: 해독된 문장만 다시 보여 줌
// - 방이 어두우면 못 읽음 (근처 벽 횃불이 켜져 있어야 함, 단서 비석과 같은 규칙)
// 붙이는 곳: StoryStele_Bastet_1~3 (Tools/Bastet/★ 스토리·검은 고양이 세팅 메뉴가 만듦)
public class BastetStoryStele : MonoBehaviour, IInteractable
{
    [Header("비석 번호 (1~3)")]
    [Range(1, 3)] [SerializeField] private int index = 1;

    [Header("내용")]
    [SerializeField] private string title = "고양이의 법";
    [TextArea(2, 4)] [SerializeField] private string text = "";
    [Tooltip("해독 후 한 번 나오는 반응 대사 (나, 케프리 등). 비우면 없음")]
    [SerializeField] private DialogueLine[] afterLines = new DialogueLine[0];
    [Range(6, 20)] [SerializeField] private int charsPerLine = 12;

    [Header("고고학 수첩 메모 (0이면 안 함)")]
    [SerializeField] private int noteNumber = 20;
    [TextArea(1, 3)] [SerializeField] private string noteText = "";

    [Header("빛이 있어야 읽힘")]
    [SerializeField] private bool needLight = true;
    [SerializeField] private float lightRadius = 6.5f;
    [SerializeField] private DialogueLine[] darkLines = { new DialogueLine("나", "오래된 비석이다. 너무 어두워서 글씨가 보이지 않는다.") };

    [Header("푸른빛 (단서 비석과 구분)")]
    [SerializeField] private Color glowColor = new Color(0.55f, 0.7f, 1f);
    [Range(0f, 2f)] [SerializeField] private float glowIntensity = 0.45f;

    private bool reading;
    private Light2D glow;

    private string ReadFlag => BastetStoryIds.StoryStele(index);
    private bool IsLit => !needLight || WallTorch.AnyLitNear(transform.position, lightRadius);

    private void Start()
    {
        glow = RaGlowFx.PointLight(transform, new Vector3(0f, 0.8f, 0f), glowColor, 2f, glowIntensity);
    }

    private void Update()
    {
        if (glow == null) return;
        // 아직 안 읽었으면 조금 더 밝게
        float target = BastetStoryIds.Has(ReadFlag) ? glowIntensity * 0.4f : glowIntensity * (0.85f + 0.15f * Mathf.Sin(Time.time * 1.6f));
        glow.intensity = Mathf.MoveTowards(glow.intensity, target, Time.deltaTime);
    }

    // ================= IInteractable =================
    public string GetPrompt()
    {
        if (reading || Cine.Running) return null;
        return IsLit ? "E: 오래된 비석 읽기" : "E: 살펴보기";
    }

    public void Interact(GameObject who)
    {
        try
        {
            if (reading || Cine.Running) return;
            if (DialogueBox.IsOpen || DialogueBox.ClosedFrame == Time.frameCount) return;
            if (!IsLit) { DialogueBox.Play(darkLines); return; }
            StartCoroutine(Read());
        }
        catch (Exception e)
        {
            Debug.LogError($"[{name}] 이야기 비석 오류: {e}", this);
        }
    }

    private IEnumerator Read()
    {
        reading = true;
        PlayerLock.Lock(this);
        bool first = !BastetStoryIds.Has(ReadFlag);
        string[] lines = RaNightClueText.ToLines(text.Replace("\n", " "), charsPerLine);

        if (first) yield return SteleDecodeUI.Decode(title, lines);
        else yield return SteleDecodeUI.ShowDecoded(title, lines);

        if (first)
        {
            BastetStoryIds.Set(ReadFlag);
            if (noteNumber > 0)
            {
                Debug.Log($"[노트 {noteNumber}] {(string.IsNullOrEmpty(noteText) ? text : noteText)}");
                AnubisToast.Show($"[노트 {noteNumber}] 고고학 수첩에 기록되었다 (J)", 2.5f);
            }
        }
        PlayerLock.Unlock(this);
        yield return null; // 해독 화면을 닫은 E로 대사가 바로 넘어가지 않게

        if (first && afterLines != null && afterLines.Length > 0)
        {
            bool done = false;
            DialogueBox.Play(afterLines, () => done = true);
            while (!done && DialogueBox.IsOpen) yield return null;
        }
        reading = false;
    }

    private void OnDisable()
    {
        if (reading) PlayerLock.Unlock(this);
        reading = false;
    }

    // ================= 테스트 =================
    [ContextMenu("테스트: 해독 화면 다시 보기 (처음처럼)")]
    private void TestDecode()
    {
        if (Application.isPlaying && !reading) StartCoroutine(TestRoutine());
    }

    private IEnumerator TestRoutine()
    {
        reading = true;
        yield return SteleDecodeUI.Decode(title, RaNightClueText.ToLines(text, charsPerLine));
        reading = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = glowColor;
        Gizmos.DrawWireSphere(transform.position, lightRadius);
    }

#if UNITY_EDITOR
    public void EditorSetup(int i, string t, string body, DialogueLine[] after, int note, string memo)
    {
        index = i; title = t; text = body; afterLines = after ?? new DialogueLine[0]; noteNumber = note; noteText = memo;
    }
#endif
}
