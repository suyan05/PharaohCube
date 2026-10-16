// 힌트 버튼(PuzzleHintButton)에 힌트를 줄 수 있는 퍼즐
// 라의 방 밤 퍼즐: 자동 힌트 대신 플레이어가 버튼을 눌러서 받음 (쿨타임은 버튼이 관리)
public interface IPuzzleHintProvider
{
    // 지금 힌트 버튼을 보여줄지 (예: 밤 + 아직 안 풂 + 시작 가능 + 남은 힌트 있음)
    bool HintAvailable { get; }

    // 힌트 실행. 버튼 옆에 띄울 문구를 돌려줌 (null이면 줄 힌트가 없다는 뜻 -> 쿨타임 안 돎)
    string RequestHint();
}