using UnityEngine;

// [추가] 호루스의 방 난이도 표 (레벨 디자인: 라 < 바스테트 < 토트 < 호루스 < 소베크 < 아누비스)
// 씬에 저장된 Inspector 값보다 "더 어려운 쪽"을 씀 -> 씬을 다시 만들지 않아도 바로 적용
// 숫자를 바꾸면 이 파일만 고치면 됨
//
// 호루스는 네 번째 신전: 힌트가 늦게, 짧게 나옴 + 토트의 방에서 배운 순서를 기억해야 함
// - H1 매의 날개: 3번 -> 5번 틀려야 축 힌트, 힌트 5초 -> 3초
// - H2 성도판: 2단계부터 흐린 길잡이 선이 잠깐만 보임 (예전 3단계), 4초 -> 3초
// - H3 우자트: 30초 -> 60초 뒤 붉은 테두리 힌트, 순서 규칙 노트는 3번 흩어진 뒤에
// - H4 별길 자물쇠 / 봉인 걷기: 3번 -> 5번 / 4번 틀려야 벽화 힌트, 감시 눈이 조금 더 멀리, 더 빨리 봄
// - H5 하늘 맞추기: 함정 힌트 3번 -> 4번, 다음 별 힌트 5번 -> 7번
public static class HorusDifficulty
{
    public const int H1FailsForHint = 5;
    public const float H1HintSeconds = 3f;

    public const int H2MemoryFromStage = 2;
    public const float H2MemorySeconds = 3f;

    public const float H3HintDelay = 60f;
    public const int H3ScattersForRule = 3;

    public const int H4FailsForHint = 5;
    public const float H4HintSeconds = 3f;
    public const int H4SealFailsForHint = 4;
    public const float H4SealGazeRange = 8.5f;
    public const float H4SealSweepPeriod = 6f;

    public const int H5FailsForTrapHint = 4;
    public const int H5FailsForNextHint = 7;

    // 힌트까지 필요한 횟수/시간은 큰 쪽, 힌트가 보이는 시간은 짧은 쪽
    public static int Later(int sceneValue, int table) => Mathf.Max(sceneValue, table);
    public static float Later(float sceneValue, float table) => Mathf.Max(sceneValue, table);
    public static float Shorter(float sceneValue, float table) => Mathf.Min(sceneValue, table);
}
