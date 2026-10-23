using System;
using System.Collections;
using UnityEngine;

// 꿈의 비석 (Inspectable 대신 이걸 씀 - 상태가 4가지라서)
// 1. 묻혀 있음 (F_PRO_STELE_SHOWN 전): "모래 속에 무언가 묻혀 있다."
// 2. 드러났는데 렌즈 없음: 상형문자만 보이는 비석 화면 -> "하나도 못 읽겠다"
// 3. 렌즈 있음: 렌즈로 비춘 만큼 해독 -> 다 읽으면 F_PRO_STELE_DECODED -> AfterDecode(계단 열기 등)
// 4. 이미 해독함: 다 읽은 비석 다시 보기
// 연출 중(Cine)에 상형문자만 보여줄 때는 ShowUnreadable()을 직접 부름
public class SteleReader : MonoBehaviour, IInteractable
{
    [Header("플래그, 아이템")]
    [SerializeField] private string shownFlag = "F_PRO_STELE_SHOWN";
    [SerializeField] private string decodedFlag = "F_PRO_STELE_DECODED";
    [SerializeField] private string lensItem = "TOOL_LENS";

    [Header("비석 글 (3.2의 6)")]
    [SerializeField] private string title = "꿈의 비석";
    [SerializeField] private string[] textLines = { "여섯 신의 시험을", "통과한 자만이", "큐브의 빛을 본다.", "", "첫 문은", "태양의 신이 지킨다." };
    [SerializeField] private string unreadableNote = "그림 같은 글자가 빼곡하다... 하나도 못 읽겠다.";

    [Header("대사")]
    [SerializeField] private DialogueLine[] buriedLines = { new DialogueLine("나", "모래 속에 무언가 묻혀 있다.") };
    [SerializeField] private DialogueLine[] noLensLines = { new DialogueLine("나", "상형문자... 닳아서 맨눈으로는 읽을 수가 없어.") };

    private bool busy;

    // 해독이 끝난 직후 Cine 연출 안에서 이어서 돌릴 것 (SphinxDreamDirector가 넣음)
    public Func<IEnumerator> AfterDecode { get; set; }

    // ================= IInteractable =================
    public string GetPrompt()
    {
        return HasFlag(shownFlag) ? "E: 비석 읽기" : "E: 살펴보기";
    }

    public void Interact(GameObject who)
    {
        try
        {
            if (busy || Cine.Running || DialogueBox.IsOpen || ItemPopup.IsShowing) return;
            if (DialogueBox.ClosedFrame == Time.frameCount) return;

            if (!HasFlag(shownFlag)) DialogueBox.Play(buriedLines);
            else StartCoroutine(Read());
        }
        catch (Exception e)
        {
            Debug.LogError($"[SteleReader] 비석 조사 오류: {e}");
        }
    }

    // ================= 읽기 =================
    private IEnumerator Read()
    {
        busy = true;
        Cine.Begin(false);

        if (HasFlag(decodedFlag)) yield return SteleDecodeUI.ShowDecoded(title, textLines);
        else if (!HasLens()) yield return ShowUnreadable();
        else yield return DecodeNow();

        Cine.End();
        busy = false;
    }

    // 연출에서도 부름 (Cine 상태는 부른 쪽이 관리)
    public IEnumerator ShowUnreadable()
    {
        yield return SteleDecodeUI.ShowUnreadable(title, textLines, unreadableNote);
        yield return Cine.Say(noLensLines);
    }

    private IEnumerator DecodeNow()
    {
        yield return SteleDecodeUI.Decode(title, textLines);
        if (GameManager.Instance != null && !GameManager.Instance.HasFlag(decodedFlag)) GameManager.Instance.SetFlag(decodedFlag);
        if (AfterDecode != null) yield return AfterDecode();
    }

    // ================= 도우미 =================
    private bool HasLens()
    {
        return ItemInventory.Instance != null && ItemInventory.Instance.Has(lensItem);
    }

    private static bool HasFlag(string flag)
    {
        return GameManager.Instance != null && GameManager.Instance.HasFlag(flag);
    }
}
