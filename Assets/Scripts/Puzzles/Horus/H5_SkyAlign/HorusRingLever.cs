using System;
using System.Collections;
using UnityEngine;

// H5 제단 둘레 레버 하나 (HorusSkyRings가 만듦). E: 연결된 고리 두 겹을 한 칸씩 돌림
public class HorusRingLever : MonoBehaviour, IInteractable
{
    private static readonly string[] RingNames = { "", "빨강(1)", "파랑(2)", "초록(3)", "노랑(4)" };

    private HorusSkyRings rings;
    private int index, ringA, ringB;

    public void Setup(HorusSkyRings owner, int leverIndex, int a, int b)
    {
        rings = owner;
        index = leverIndex;
        ringA = a;
        ringB = b;
    }

    public string GetPrompt()
    {
        if (rings == null || !rings.CanPull) return "하늘 고리 레버";
        return $"E: 레버 당기기 ({RingNames[ringA]} + {RingNames[ringB]} 고리)";
    }

    public void Interact(GameObject who)
    {
        try
        {
            if (rings == null || DialogueBox.IsOpen || Cine.Running) return;
            if (!rings.CanPull)
            {
                HorusWorld.Say("돌 레버가 꿈쩍도 하지 않는다. 제단의 하늘이 아직 어두워.");
                return;
            }
            rings.Pull(index);
            StartCoroutine(Bob());
        }
        catch (Exception e)
        {
            Debug.LogError($"[하늘 레버] Interact 오류: {e}");
        }
    }

    // 당길 때 살짝 내려갔다 올라옴
    private IEnumerator Bob()
    {
        Vector3 start = transform.position;
        for (float t = 0f; t < 0.25f; t += Time.deltaTime)
        {
            transform.position = start + Vector3.down * Mathf.Sin(t / 0.25f * Mathf.PI) * 0.08f;
            yield return null;
        }
        transform.position = start;
    }
}
