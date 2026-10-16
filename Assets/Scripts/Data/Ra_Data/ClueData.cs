using System;
using UnityEngine;

// 비석 단서 1개 (낮 / 밤 버전)
// 만들기: Project 창 우클릭 -> Create -> PharaohCube -> Clue Data
[CreateAssetMenu(fileName = "Clue_", menuName = "PharaohCube/Clue Data")]
public class ClueData : ScriptableObject
{
    [Serializable]
    public class ClueContent
    {
        [Tooltip("패널 맨 위 제목 (예: 남쪽 벽화 '하루의 순서')")]
        public string title = "";

        [TextArea(3, 8)]
        [Tooltip("단서 본문")]
        public string body = "";

        [Tooltip("벽화, 스케치 같은 삽화 (비우면 글만 크게 표시)")]
        public Sprite illustration;

        [Tooltip("삽화 색. 흐린 스케치처럼 보이게 하려면 알파(A)를 0.5 정도로")]
        public Color illustrationTint = Color.white;

        public bool HasContent => !string.IsNullOrEmpty(body) || illustration != null;
    }

    [Tooltip("읽음 플래그 이름에 쓰임 (예: RA_P4). 영어 대문자, 숫자, _ 만 사용")]
    [SerializeField] private string clueId = "";

    [SerializeField] private ClueContent day = new ClueContent();

    [Tooltip("비워두면 밤에도 낮 내용을 보여줌")]
    [SerializeField] private ClueContent night = new ClueContent();

    public string ClueId => clueId;

    public ClueContent Get(bool isNight)
    {
        if (isNight && night != null && night.HasContent) return night;
        return day;
    }
}