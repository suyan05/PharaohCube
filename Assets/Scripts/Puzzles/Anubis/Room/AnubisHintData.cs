using UnityEngine;

// 퍼즐 하나의 힌트 문구 목록 (누를 때마다 다음 힌트, 마지막 힌트는 계속 반복)
// 만들기: Project 창 우클릭 -> Create -> PharaohCube -> Anubis -> Hint Data
[CreateAssetMenu(fileName = "Hint_Anubis_", menuName = "PharaohCube/Anubis/Hint Data")]
public class AnubisHintData : ScriptableObject
{
    [Tooltip("위에서부터 차례로 보여 줌. 뒤로 갈수록 정답에 가깝게")]
    [TextArea(2, 4)]
    [SerializeField] private string[] hints =
    {
        "양피지를 다시 읽어 보자.",
    };

    public int Count => hints != null ? hints.Length : 0;

    public string Get(int index)
    {
        if (Count == 0) return null;
        return hints[Mathf.Clamp(index, 0, Count - 1)];
    }

#if UNITY_EDITOR
    // 에디터 자동 세팅용 (코드로 문구 채우기)
    public void EditorSetHints(string[] newHints) => hints = newHints;
#endif
}
