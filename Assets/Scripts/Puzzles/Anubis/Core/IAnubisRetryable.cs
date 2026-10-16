// 메인 저울 실패 처벌 때 "처음 상태로 되돌릴 수 있는" 퍼즐 (실전 서브 퍼즐 4개 + 메인 저울)
// AnubisJudgmentPenalty가 씬에서 이 인터페이스를 가진 컴포넌트를 모두 찾아 ResetForRetry()를 호출함
public interface IAnubisRetryable
{
    // 클리어 상태, 보상 지급 여부, 화면을 처음처럼 되돌림 (사자의 서 단서와 양피지는 건드리지 않음)
    void ResetForRetry();
}
