using UnityEngine;

// 바스테트 5개 방 배치 크기 기록 (게임 동작 없음)
// Bastet_Area에 붙고, Tools/Bastet/5개 방 크기 조절 창이 이 값을 바꾼 뒤 배치를 다시 맞춤
public class BastetAreaLayout : MonoBehaviour
{
    [Tooltip("요청서 배치도(58 x 31칸) 대비 크기. 1 = 원래 크기, 0.7 = 70%")]
    [Range(0.5f, 1f)] [SerializeField] private float scale = 1f;
    [Tooltip("걷는 퍼즐(발자국·석상 간격)에 지금 적용된 배율 (자동 기록, 직접 바꾸지 않기)")]
    [SerializeField] private float puzzleScale = 1f;

    public float Scale => scale;
    public float PuzzleScale => puzzleScale;

#if UNITY_EDITOR
    public void EditorSetScale(float s) => scale = Mathf.Clamp(s, 0.5f, 1f);
    public void EditorSetPuzzleScale(float s) => puzzleScale = s;
#endif
}
